using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

/// ��<summary>_PLACEHOLDER��
/// Componente que gestiona el movimiento de ingredientes en columna con el rodillo
/// ��</summary>_PLACEHOLDER��
public class RodilloManager : NetworkBehaviour, IUtensilioManager
{
    private GameObject nodoOrigen;
    private Rodillo rodillo;

    // Implementaci�n de la interfaz IUtensilioManager

    /// ��<summary>_PLACEHOLDER��
    /// Configura el manager con los datos del utensilio espec�fico
    /// ��</summary>_PLACEHOLDER��
    public void ConfigurarConUtensilio(Utensilio utensilio)
    {
        if (utensilio is Rodillo rodilloUtensilio)
        {
            rodillo = rodilloUtensilio;
        }
        else
        {
            Debug.LogError("Se intent� configurar RodilloManager con un utensilio que no es Rodillo");
        }
    }

    /// ��<summary>_PLACEHOLDER��
    /// Valida si los nodos seleccionados son adecuados para el rodillo
    /// ��</summary>_PLACEHOLDER��
    public bool ValidarNodos(List<GameObject> nodosSeleccionados)
    {
        // El rodillo requiere exactamente 1 nodo
        if (nodosSeleccionados == null || nodosSeleccionados.Count != 1)
            return false;

        // Verificar que el nodo tenga el componente necesario
        GameObject nodo = nodosSeleccionados[0];
        Node nodoComp = nodo.GetComponent<Node>();
        if (nodoComp == null)
            return false;

        // Verificar que hay espacio para mover
        NodeMap nodeMap = FindObjectOfType<NodeMap>();
        if (nodeMap == null)
            return false;

        int columnaX = Mathf.RoundToInt(nodoComp.position.x);
        int destinoX = rodillo.moverDerecha ? columnaX + 1 : columnaX - 1;

        // Verificar si hay al menos un nodo destino disponible
        bool existeDestino = false;
        foreach (var candidato in nodeMap.nodesList)
        {
            Node compCandidato = candidato.GetComponent<Node>();
            if (compCandidato != null && Mathf.RoundToInt(compCandidato.position.x) == destinoX)
            {
                existeDestino = true;
                break;
            }
        }

        return existeDestino;
    }

    /// ��<summary>_PLACEHOLDER��
    /// Ejecuta la acci�n del rodillo sobre los nodos seleccionados
    /// ��</summary>_PLACEHOLDER��
    public void EjecutarAccion(List<GameObject> nodosSeleccionados)
    {
        if (!ValidarNodos(nodosSeleccionados))
            return;

        nodoOrigen = nodosSeleccionados[0];

        // Asegurar que tiene NetworkObject
        if (GetComponent<NetworkObject>() == null)
        {
            gameObject.AddComponent<NetworkObject>();
        }

        // Iniciar movimiento usando el m�todo existente
        IniciarMovimientoColumna(nodoOrigen, rodillo);
    }

    /// ��<summary>_PLACEHOLDER��
    /// Limpia los efectos visuales y recursos utilizados
    /// ��</summary>_PLACEHOLDER��
    public void LimpiarEfecto()
    {
        // El RodilloManager ya se destruye autom�ticamente despu�s de completar su acci�n
        // No se necesita implementaci�n adicional
    }

    /// ��<summary>_PLACEHOLDER��
    /// Inicia el movimiento de ingredientes en una columna
    /// ��</summary>_PLACEHOLDER��
    public void IniciarMovimientoColumna(GameObject origen, Rodillo rodilloObj)
    {
        nodoOrigen = origen;
        rodillo = rodilloObj;

        // Spawnear objeto de red
        if (!GetComponent<NetworkObject>().IsSpawned)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Solicitar movimiento al servidor
        MoverColumnaServerRpc(origen.GetComponent<NetworkObject>().NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoverColumnaServerRpc(ulong origenId)
    {
        // Obtener referencia al nodo origen
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        if (origenNetObj == null)
        {
            Destroy(gameObject);
            return;
        }

        nodoOrigen = origenNetObj.gameObject;

        // Obtener componente Node
        Node origenComp = nodoOrigen.GetComponent<Node>();
        if (origenComp == null)
        {
            Destroy(gameObject);
            return;
        }

        // Obtener NodeMap
        NodeMap nodeMap = FindObjectOfType<NodeMap>();
        if (nodeMap == null)
        {
            Destroy(gameObject);
            return;
        }

        // Obtener coordenada X de la columna
        int columnaX = Mathf.RoundToInt(origenComp.position.x);
        bool haciaDerecha = rodillo.moverDerecha;

        Debug.Log($"Activando efecto de rodillo en columna {columnaX}, direcci�n: {(haciaDerecha ? "derecha" : "izquierda")}");

        // Obtener todos los nodos en la columna X, ordenados por Y ascendente
        List<GameObject> nodosColumna = new List<GameObject>();
        foreach (var nodo in nodeMap.nodesList)
        {
            Node comp = nodo.GetComponent<Node>();
            if (comp != null && Mathf.RoundToInt(comp.position.x) == columnaX)
            {
                nodosColumna.Add(nodo);
            }
        }

        // Ordenar por Y ascendente
        nodosColumna = nodosColumna.OrderBy(n => n.GetComponent<Node>().position.y).ToList();

        if (nodosColumna.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        // Planificar el movimiento
        Dictionary<GameObject, ResourcesSO> ingredientesDestino = new Dictionary<GameObject, ResourcesSO>();
        List<GameObject> nodosOrigenALimpiar = new List<GameObject>();

        // Para cada nodo en la columna, verificar si tiene ingrediente y buscar nodo destino
        foreach (var nodo in nodosColumna)
        {
            Node nodoComp = nodo.GetComponent<Node>();
            if (nodoComp == null || !nodoComp.hasIngredient.Value) continue;

            // Calcular posici�n destino
            int destinoX = haciaDerecha ? columnaX + 1 : columnaX - 1;
            Vector2 posDestino = new Vector2(destinoX, nodoComp.position.y);

            // Buscar nodo destino
            GameObject nodoDestino = null;
            foreach (var candidato in nodeMap.nodesList)
            {
                Node compCandidato = candidato.GetComponent<Node>();
                if (compCandidato != null && compCandidato.position == posDestino)
                {
                    nodoDestino = candidato;
                    break;
                }
            }

            // Si hay nodo destino, planificar movimiento
            if (nodoDestino != null)
            {
                GameObject ingredienteActual = nodoComp.currentIngredient;
                ResourcesSO recursoActual = ingredienteActual.GetComponent<componente>().data;

                if (recursoActual != null && recursoActual.prefab3D != null)
                {
                    ingredientesDestino[nodoDestino] = recursoActual;
                    nodosOrigenALimpiar.Add(nodo);
                }
            }
        }

        // Ejecutar el movimiento (primero limpiar, luego colocar)
        foreach (var nodo in nodosOrigenALimpiar)
        {
            nodo.GetComponent<Node>().ClearNodeIngredient();
        }

        foreach (var kvp in ingredientesDestino)
        {
            GameObject nodoDestino = kvp.Key;
            ResourcesSO recurso = kvp.Value;

            Node destinoComp = nodoDestino.GetComponent<Node>();
            if (destinoComp != null && !destinoComp.hasIngredient.Value)
            {
                destinoComp.SetNodeIngredient(recurso.prefab3D);
            }
        }

        // Mostrar efecto visual
        MostrarEfectoColumnaClientRpc(origenId, haciaDerecha);

        // Destruir despu�s de completar
        Destroy(gameObject, 0.5f);
    }

    [ClientRpc]
    private void MostrarEfectoColumnaClientRpc(ulong origenId, bool derecha)
    {
        // Obtener referencia al nodo origen
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        if (origenNetObj == null) return;

        // Mostrar efecto visual si est� definido
        if (rodillo != null && rodillo.prefabEfectoMovimiento != null)
        {
            GameObject efecto = Instantiate(rodillo.prefabEfectoMovimiento, origenNetObj.transform.position, Quaternion.identity);

            // Orientar seg�n direcci�n
            Vector3 escala = efecto.transform.localScale;
            if (!derecha) escala.x *= -1;
            efecto.transform.localScale = escala;
        }
    }
}
