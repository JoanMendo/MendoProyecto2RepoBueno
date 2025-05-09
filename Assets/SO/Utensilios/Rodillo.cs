using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Utensilio que mueve todos los ingredientes de una columna hacia un lado
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Rodillo", menuName = "CookingGame/Resources/Utensilios/Rodillo")]
public class Rodillo : Utensilio
{
    [Tooltip("Prefab para efecto visual de movimiento")]
    public GameObject prefabEfectoMovimiento;

    [Tooltip("Dirección del movimiento (true = derecha, false = izquierda)")]
    public bool moverDerecha = true;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Crear gestor para el movimiento de columna
        GameObject gestorObj = new GameObject("RodilloManager");
        RodilloManager gestor = gestorObj.AddComponent<RodilloManager>();

        // Iniciar movimiento
        gestor.IniciarMovimientoColumna(nodoOrigen, this);
    }
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona el movimiento de ingredientes en columna con el rodillo
/// ‡‡</summary>_PLACEHOLDER‡‡
public class RodilloManager : NetworkBehaviour
{
    private GameObject nodoOrigen;
    private Rodillo rodillo;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el movimiento de ingredientes en una columna
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarMovimientoColumna(GameObject origen, Rodillo rodilloObj)
    {
        nodoOrigen = origen;
        rodillo = rodilloObj;

        // Spawnear objeto de red
        GetComponent<NetworkObject>().Spawn();

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

        Debug.Log($"Activando efecto de rodillo en columna {columnaX}, dirección: {(haciaDerecha ? "derecha" : "izquierda")}");

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

            // Calcular posición destino
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
                ResourcesSO recursoActual = ingredienteActual.GetComponent<ResourcesSO>();

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

        // Destruir después de completar
        Destroy(gameObject, 0.5f);
    }

    [ClientRpc]
    private void MostrarEfectoColumnaClientRpc(ulong origenId, bool derecha)
    {
        // Obtener referencia al nodo origen
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        if (origenNetObj == null) return;

        // Mostrar efecto visual si está definido
        if (rodillo != null && rodillo.prefabEfectoMovimiento != null)
        {
            GameObject efecto = Instantiate(rodillo.prefabEfectoMovimiento, origenNetObj.transform.position, Quaternion.identity);

            // Orientar según dirección
            Vector3 escala = efecto.transform.localScale;
            if (!derecha) escala.x *= -1;
            efecto.transform.localScale = escala;
        }
    }
}