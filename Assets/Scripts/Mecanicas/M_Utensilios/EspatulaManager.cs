using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona el intercambio de ingredientes con la espátula
/// ‡‡</summary>_PLACEHOLDER‡‡
public class EspatulaManager : NetworkBehaviour, IUtensilioManager
{
    private GameObject nodo1;
    private GameObject nodo2;
    private Espatula espatula;

    // Implementación de IUtensilioManager

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Configura el manager con los datos del utensilio específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ConfigurarConUtensilio(Utensilio utensilio)
    {
        if (utensilio is Espatula espatulaUtensilio)
        {
            espatula = espatulaUtensilio;
        }
        else
        {
            Debug.LogError("Se intentó configurar EspatulaManager con un utensilio que no es Espatula");
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Valida si los nodos seleccionados son adecuados para la espátula
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public bool ValidarNodos(List<GameObject> nodosSeleccionados)
    {
        // La espátula necesita exactamente 2 nodos, ambos con ingredientes
        if (nodosSeleccionados == null || nodosSeleccionados.Count != 2)
            return false;

        Node nodo1 = nodosSeleccionados[0].GetComponent<Node>();
        Node nodo2 = nodosSeleccionados[1].GetComponent<Node>();

        if (nodo1 == null || nodo2 == null)
            return false;

        return nodo1.hasIngredient.Value && nodo2.hasIngredient.Value;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Ejecuta la acción de intercambio con la espátula
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void EjecutarAccion(List<GameObject> nodosSeleccionados)
    {
        if (!ValidarNodos(nodosSeleccionados))
            return;

        nodo1 = nodosSeleccionados[0];
        nodo2 = nodosSeleccionados[1];

        // Asegurar que tiene NetworkObject
        if (GetComponent<NetworkObject>() == null)
        {
            gameObject.AddComponent<NetworkObject>();
        }

        // Iniciar intercambio usando el método existente
        IniciarIntercambio(nodo1, nodo2, espatula);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Limpia los efectos visuales y recursos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void LimpiarEfecto()
    {
        // El EspatulaManager ya se destruye automáticamente al completar su acción
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el intercambio entre dos nodos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarIntercambio(GameObject primerNodo, GameObject segundoNodo, Espatula espatulaObj)
    {
        nodo1 = primerNodo;
        nodo2 = segundoNodo;
        espatula = espatulaObj;

        // Spawnear objeto de red si no está spawneado ya
        if (!GetComponent<NetworkObject>().IsSpawned)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Solicitar intercambio al servidor
        IntercambiarIngredientesServerRpc(
            primerNodo.GetComponent<NetworkObject>().NetworkObjectId,
            segundoNodo.GetComponent<NetworkObject>().NetworkObjectId
        );
    }

    [ServerRpc(RequireOwnership = false)]
    private void IntercambiarIngredientesServerRpc(ulong nodoId1, ulong nodoId2)
    {
        // Obtener referencias a los nodos de forma segura
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodoId1, out NetworkObject nodo1NetObj) ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodoId2, out NetworkObject nodo2NetObj))
        {
            Debug.LogError("EspatulaManager: No se pudieron encontrar los nodos por ID");
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

        if (ingrediente1 == null || ingrediente2 == null)
        {
            Debug.LogError("Error: Ingredientes nulos aunque hasIngredient=true");
            Destroy(gameObject);
            return;
        }

        ResourcesSO recurso1 = ingrediente1.GetComponent<componente>().data;
        ResourcesSO recurso2 = ingrediente2.GetComponent<componente>().data;

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
        // Obtener referencias a los nodos de forma segura
        NetworkObject nodo1NetObj = null;
        NetworkObject nodo2NetObj = null;

        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodoId1, out nodo1NetObj);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodoId2, out nodo2NetObj);

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