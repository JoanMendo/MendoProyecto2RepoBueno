using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Utensilio que mueve un ingrediente de un nodo a otro
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Pinzas", menuName = "CookingGame/Resources/Utensilios/Pinzas")]
public class Pinzas : Utensilio
{
    [Tooltip("Prefab para efecto visual de movimiento")]
    public GameObject prefabEfectoMovimiento;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Verificar que tenemos exactamente 2 nodos
        if (nodosAfectados.Count != 2)
        {
            Debug.LogWarning("Las pinzas requieren exactamente 2 nodos seleccionados");
            return;
        }

        // Crear gestor para el movimiento
        GameObject gestorObj = new GameObject("PinzasManager");
        PinzasManager gestor = gestorObj.AddComponent<PinzasManager>();

        // Iniciar movimiento
        gestor.IniciarMovimiento(nodosAfectados[0], nodosAfectados[1], this);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Valida si los nodos seleccionados son válidos para estas pinzas
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public override bool ValidarColocacion(List<GameObject> nodos)
    {
        if (nodos.Count != 2) return false;

        // Verificar que el primer nodo tenga ingrediente y el segundo esté vacío
        Node nodo1 = nodos[0].GetComponent<Node>();
        Node nodo2 = nodos[1].GetComponent<Node>();

        if (nodo1 == null || nodo2 == null) return false;

        return nodo1.hasIngredient.Value && !nodo2.hasIngredient.Value;
    }
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona el movimiento de ingredientes con las pinzas
/// ‡‡</summary>_PLACEHOLDER‡‡
public class PinzasManager : NetworkBehaviour
{
    private GameObject nodoOrigen;
    private GameObject nodoDestino;
    private Pinzas pinzas;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el movimiento de un ingrediente entre dos nodos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarMovimiento(GameObject origen, GameObject destino, Pinzas pinzasObj)
    {
        nodoOrigen = origen;
        nodoDestino = destino;
        pinzas = pinzasObj;

        // Spawnear objeto de red
        GetComponent<NetworkObject>().Spawn();

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
        ResourcesSO recursoActual = ingredienteActual.GetComponent<ResourcesSO>();

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