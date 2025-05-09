using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Utensilio que intercambia los ingredientes de dos nodos
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Espatula", menuName = "CookingGame/Resources/Utensilios/Espatula")]
public class Espatula : Utensilio
{
    [Tooltip("Prefab para efecto visual de intercambio")]
    public GameObject prefabEfectoIntercambio;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Verificar que tenemos exactamente 2 nodos
        if (nodosAfectados.Count != 2)
        {
            Debug.LogWarning("La espátula requiere exactamente 2 nodos seleccionados");
            return;
        }

        // Crear gestor para el intercambio
        GameObject gestorObj = new GameObject("EspatulaManager");
        EspatulaManager gestor = gestorObj.AddComponent<EspatulaManager>();

        // Iniciar intercambio
        gestor.IniciarIntercambio(nodosAfectados[0], nodosAfectados[1], this);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Valida si los nodos seleccionados son válidos para esta espátula
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public override bool ValidarColocacion(List<GameObject> nodos)
    {
        if (nodos.Count != 2) return false;

        // Verificar que ambos nodos tengan ingrediente
        Node nodo1 = nodos[0].GetComponent<Node>();
        Node nodo2 = nodos[1].GetComponent<Node>();

        if (nodo1 == null || nodo2 == null) return false;

        return nodo1.hasIngredient.Value && nodo2.hasIngredient.Value;
    }
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona el intercambio de ingredientes con la espátula
/// ‡‡</summary>_PLACEHOLDER‡‡
public class EspatulaManager : NetworkBehaviour
{
    private GameObject nodo1;
    private GameObject nodo2;
    private Espatula espatula;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el intercambio entre dos nodos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarIntercambio(GameObject primerNodo, GameObject segundoNodo, Espatula espactulaObj)
    {
        nodo1 = primerNodo;
        nodo2 = segundoNodo;
        espatula = espactulaObj;

        // Spawnear objeto de red
        GetComponent<NetworkObject>().Spawn();

        // Solicitar intercambio al servidor
        IntercambiarIngredientesServerRpc(
            primerNodo.GetComponent<NetworkObject>().NetworkObjectId,
            segundoNodo.GetComponent<NetworkObject>().NetworkObjectId
        );
    }

    [ServerRpc(RequireOwnership = false)]
    private void IntercambiarIngredientesServerRpc(ulong nodoId1, ulong nodoId2)
    {
        // Obtener referencias a los nodos
        NetworkObject nodo1NetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[nodoId1];
        NetworkObject nodo2NetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[nodoId2];

        if (nodo1NetObj == null || nodo2NetObj == null)
        {
            Destroy(gameObject);
            return;
        }

        nodo1 = nodo1NetObj.gameObject;
        nodo2 = nodo2NetObj.gameObject;

        // Obtener componentes Node
        Node nodo1Comp = nodo1.GetComponent<Node>();
        Node nodo2Comp = nodo2.GetComponent<Node>();

        if (nodo1Comp == null || nodo2Comp == null || !nodo1Comp.hasIngredient.Value || !nodo2Comp.hasIngredient.Value)
        {
            Debug.LogWarning("Ambos nodos deben tener ingredientes para usar la espátula");
            Destroy(gameObject);
            return;
        }

        // Guardar información de los ingredientes
        GameObject ingrediente1 = nodo1Comp.currentIngredient;
        GameObject ingrediente2 = nodo2Comp.currentIngredient;

        ResourcesSO recurso1 = ingrediente1.GetComponent<ResourcesSO>();
        ResourcesSO recurso2 = ingrediente2.GetComponent<ResourcesSO>();

        if (recurso1 == null || recurso2 == null || recurso1.prefab3D == null || recurso2.prefab3D == null)
        {
            Debug.LogError("Error: los ingredientes no tienen los componentes esperados");
            Destroy(gameObject);
            return;
        }

        // Limpiar nodos
        nodo1Comp.ClearNodeIngredient();
        nodo2Comp.ClearNodeIngredient();

        // Intercambiar ingredientes
        nodo1Comp.SetNodeIngredient(recurso2.prefab3D);
        nodo2Comp.SetNodeIngredient(recurso1.prefab3D);

        // Mostrar efecto visual
        MostrarEfectoIntercambioClientRpc(nodoId1, nodoId2);

        // Destruir después de completar
        Destroy(gameObject, 0.5f);
    }

    [ClientRpc]
    private void MostrarEfectoIntercambioClientRpc(ulong nodoId1, ulong nodoId2)
    {
        // Obtener referencias a los nodos
        NetworkObject nodo1NetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[nodoId1];
        NetworkObject nodo2NetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[nodoId2];

        if (nodo1NetObj == null || nodo2NetObj == null) return;

        // Calcular punto medio para el efecto
        Vector3 puntoMedio = Vector3.Lerp(
            nodo1NetObj.transform.position,
            nodo2NetObj.transform.position,
            0.5f
        );

        // Mostrar efecto visual si está definido
        if (espatula != null && espatula.prefabEfectoIntercambio != null)
        {
            Instantiate(espatula.prefabEfectoIntercambio, puntoMedio, Quaternion.identity);
        }
    }
}