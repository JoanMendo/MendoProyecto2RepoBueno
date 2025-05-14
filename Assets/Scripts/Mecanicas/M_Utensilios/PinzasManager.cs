using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona el movimiento de ingredientes con las pinzas
/// ‡‡</summary>_PLACEHOLDER‡‡
public class PinzasManager : NetworkBehaviour, IUtensilioManager
{
    private GameObject nodoOrigen;
    private GameObject nodoDestino;
    private Pinzas pinzas;

    // Implementación de IUtensilioManager

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Configura el manager con los datos del utensilio específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ConfigurarConUtensilio(Utensilio utensilio)
    {
        if (utensilio is Pinzas pinzasUtensilio)
        {
            pinzas = pinzasUtensilio;
        }
        else
        {
            Debug.LogError("Se intentó configurar PinzasManager con un utensilio que no es Pinzas");
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Valida si los nodos seleccionados son adecuados para las pinzas
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public bool ValidarNodos(List<GameObject> nodosSeleccionados)
    {
        // Las pinzas necesitan exactamente 2 nodos
        if (nodosSeleccionados == null || nodosSeleccionados.Count != 2)
            return false;

        // Primero debe tener ingrediente, segundo debe estar vacío
        Node nodo1 = nodosSeleccionados[0].GetComponent<Node>();
        Node nodo2 = nodosSeleccionados[1].GetComponent<Node>();

        if (nodo1 == null || nodo2 == null)
            return false;

        return nodo1.hasIngredient.Value && !nodo2.hasIngredient.Value;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Ejecuta la acción de las pinzas (mover ingrediente)
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void EjecutarAccion(List<GameObject> nodosSeleccionados)
    {
        if (!ValidarNodos(nodosSeleccionados))
            return;

        nodoOrigen = nodosSeleccionados[0];
        nodoDestino = nodosSeleccionados[1];

        // Asegurar que tiene NetworkObject
        if (GetComponent<NetworkObject>() == null)
        {
            gameObject.AddComponent<NetworkObject>();
        }

        // Iniciar movimiento usando el método existente
        IniciarMovimiento(nodoOrigen, nodoDestino, pinzas);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Limpia los efectos visuales y recursos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void LimpiarEfecto()
    {
        // El PinzasManager ya se destruye automáticamente al completar su acción
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el movimiento de un ingrediente entre dos nodos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarMovimiento(GameObject origen, GameObject destino, Pinzas pinzasObj)
    {
        nodoOrigen = origen;
        nodoDestino = destino;
        pinzas = pinzasObj;

        // Spawnear objeto de red si no está spawneado ya
        if (!GetComponent<NetworkObject>().IsSpawned)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Solicitar movimiento al servidor
        MoverIngredienteServerRpc(
            origen.GetComponent<NetworkObject>().NetworkObjectId,
            destino.GetComponent<NetworkObject>().NetworkObjectId
        );
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoverIngredienteServerRpc(ulong origenId, ulong destinoId)
    {
        // Obtener referencias a los nodos
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        NetworkObject destinoNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[destinoId];

        if (origenNetObj == null || destinoNetObj == null)
        {
            Destroy(gameObject);
            return;
        }

        nodoOrigen = origenNetObj.gameObject;
        nodoDestino = destinoNetObj.gameObject;

        // Obtener componentes Node
        Node origenComp = nodoOrigen.GetComponent<Node>();
        Node destinoComp = nodoDestino.GetComponent<Node>();

        if (origenComp == null || destinoComp == null || !origenComp.hasIngredient.Value || destinoComp.hasIngredient.Value)
        {
            Debug.LogWarning("Las pinzas requieren un nodo origen con ingrediente y un nodo destino vacío");
            Destroy(gameObject);
            return;
        }

        // Obtener información del ingrediente
        GameObject ingredienteActual = origenComp.currentIngredient;
        ResourcesSO recursoActual = ingredienteActual.GetComponent<componente>().data;

        if (recursoActual == null || recursoActual.prefab3D == null)
        {
            Debug.LogError("Error: el ingrediente no tiene los componentes esperados");
            Destroy(gameObject);
            return;
        }

        // Mover ingrediente
        origenComp.ClearNodeIngredient();
        destinoComp.SetNodeIngredient(recursoActual.prefab3D);

        // Mostrar efecto visual
        MostrarEfectoMovimientoClientRpc(origenId, destinoId);

        // Destruir después de completar
        Destroy(gameObject, 0.5f);
    }

    [ClientRpc]
    private void MostrarEfectoMovimientoClientRpc(ulong origenId, ulong destinoId)
    {
        // Obtener referencias a los nodos
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        NetworkObject destinoNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[destinoId];

        if (origenNetObj == null || destinoNetObj == null) return;

        // Calcular punto medio para el efecto
        Vector3 puntoMedio = Vector3.Lerp(
            origenNetObj.transform.position,
            destinoNetObj.transform.position,
            0.5f
        );

        // Mostrar efecto visual si está definido
        if (pinzas != null && pinzas.prefabEfectoMovimiento != null)
        {
            Instantiate(pinzas.prefabEfectoMovimiento, puntoMedio, Quaternion.identity);
        }
    }
}
