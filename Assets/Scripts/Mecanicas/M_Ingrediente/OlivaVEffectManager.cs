using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manager for handling Oliva_V effects, which decrease the range of neighboring ingredients.
/// </summary>
public class OlivaVEffectManager : NetworkBehaviour, IEffectManager
{
    // List of affected ingredients
    private List<GameObject> ingredientesAfectados = new List<GameObject>();

    // Network synchronized variable for the range reduction amount
    private NetworkVariable<int> cantidadReduccion = new NetworkVariable<int>(1);

    // List of NetworkObjectIds for tracking
    private List<ulong> networkIds = new List<ulong>();

    // State tracking
    private bool efectoAplicado = false;

    // Reference to the configured ingredient
    private IngredientesSO _ingredienteConfigurado;

    // Debug flag
    [SerializeField] private bool mostrarDebug = false;

    /// <summary>
    /// Configures the manager with the ingredient data
    /// </summary>
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;

        if (mostrarDebug)
        {
            Debug.Log($"OlivaVEffectManager configurado con: {ingrediente.name}");
        }

        // If the ingredient is Oliva_V, we can get the range reduction directly
        if (ingrediente is Oliva_V olivaV)
        {
            cantidadReduccion.Value = olivaV.reduccionRango;
        }
    }

    /// <summary>
    /// Starts the effect on the specified nodes
    /// </summary>
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Get the range reduction from the configured ingredient, or use default value
        int reduccionRango = 1;

        // If we have a reference to the specific ingredient, use its value
        if (_ingredienteConfigurado is Oliva_V olivaV)
        {
            reduccionRango = olivaV.reduccionRango;
        }

        // Delegate to the specific method
        IniciarEfectoOlivaV(nodoOrigen, nodosAfectados, reduccionRango);
    }

    /// <summary>
    /// Starts the range reduction effect on adjacent ingredients
    /// </summary>
    public void IniciarEfectoOlivaV(GameObject nodoOrigen, List<GameObject> nodosAfectados, int reduccionRango)
    {
        // Make sure we have a NetworkObject and it's spawned
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Configure amount of reduction
        cantidadReduccion.Value = reduccionRango;

        // Clear previous lists (in case the manager is reused)
        ingredientesAfectados.Clear();
        networkIds.Clear();

        // Collect ingredients and network IDs
        foreach (var nodoVecino in nodosAfectados)
        {
            if (nodoVecino == null) continue;

            Node nodo = nodoVecino.GetComponent<Node>();
            if (nodo == null || !nodo.hasIngredient.Value || nodo.currentIngredient == null)
                continue;

            ingredientesAfectados.Add(nodo.currentIngredient);

            // Get NetworkObjectId of each affected ingredient
            NetworkObject netObj = nodo.currentIngredient.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                networkIds.Add(netObj.NetworkObjectId);
            }
        }

        // If there are no affected ingredients, finish
        if (ingredientesAfectados.Count == 0)
        {
            if (mostrarDebug)
            {
                Debug.Log("No hay ingredientes afectados por Oliva_V");
            }

            FinalizarEfecto();
            return;
        }

        // Start the effect on the server
        AplicarEfectoServerRpc(networkIds.ToArray(), cantidadReduccion.Value);

        if (mostrarDebug)
        {
            Debug.Log($"Efecto Oliva_V iniciado en {ingredientesAfectados.Count} ingredientes con reducción de {cantidadReduccion.Value}");
        }
    }

    /// <summary>
    /// Server RPC to apply the effect to all targets
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong[] objetivosIds, int reduccion)
    {
        efectoAplicado = true;

        foreach (ulong id in objetivosIds)
        {
            // Prepare clients first
            PrepararObjetivoClientRpc(id);

            // Find target object
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
                continue;

            // Use coroutine to give time for synchronization
            StartCoroutine(AplicarEfectoConDelay(objetivoNetObj.gameObject, reduccion, id));
        }
    }

    /// <summary>
    /// Applies the effect with a delay to ensure synchronization
    /// </summary>
    private IEnumerator AplicarEfectoConDelay(GameObject objetivo, int reduccion, ulong id)
    {
        yield return new WaitForSeconds(0.1f);

        if (objetivo == null) yield break;

        // Get or add the ModificadorRecurso component
        ModificadorRecurso mod = objetivo.GetComponent<ModificadorRecurso>();
        if (mod == null)
        {
            mod = objetivo.AddComponent<ModificadorRecurso>();

            // Assign base resource
            componente ingrediente = objetivo.GetComponent<componente>();
            if (ingrediente != null && ingrediente.data != null)
            {
                mod.SetRecursoBase(ingrediente.data);
            }
        }

        // Directly modify the network variable instead of using ServerRpc
        int rangoAntes = mod.GetRangoActual();
        mod.DisminuirRango(reduccion);
        int rangoDespues = mod.GetRangoActual();

        if (mostrarDebug)
        {
            Debug.Log($"Oliva_V: Reducido rango de {objetivo.name}. Rango: {rangoAntes} -> {rangoDespues} (-{reduccion})");
        }

        // Notify the client individually
        AplicarEfectoASingleObjetivoClientRpc(id, reduccion);
    }

    /// <summary>
    /// Prepares clients to receive the effect by ensuring the ModificadorRecurso component exists
    /// </summary>
    [ClientRpc]
    private void PrepararObjetivoClientRpc(ulong id)
    {
        // Locate the target object on the client
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        // Check if the ModificadorRecurso component already exists on the client
        ModificadorRecurso mod = objetivoNetObj.gameObject.GetComponent<ModificadorRecurso>();
        if (mod == null)
        {
            mod = objetivoNetObj.gameObject.AddComponent<ModificadorRecurso>();

            // Get the resource from the component
            componente comp = objetivoNetObj.gameObject.GetComponent<componente>();
            if (comp != null && comp.data != null)
            {
                mod.SetRecursoBase(comp.data);
            }
        }

        if (mostrarDebug)
        {
            Debug.Log($"[CLIENTE] Preparado objeto {objetivoNetObj.name} para recibir efecto Oliva_V");
        }
    }

    /// <summary>
    /// Notifies a client that an effect has been applied to a specific object
    /// </summary>
    [ClientRpc]
    private void AplicarEfectoASingleObjetivoClientRpc(ulong id, int reduccion)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        if (mostrarDebug)
        {
            Debug.Log($"[CLIENTE] Efecto Oliva_V aplicado a {objetivoNetObj.name}, reducción: {reduccion}");
        }
    }

    /// <summary>
    /// Notifies all clients that effects have been applied
    /// </summary>
    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong[] objetivosIds, int reduccion)
    {
        if (mostrarDebug)
        {
            Debug.Log($"Efecto Oliva_V aplicado a {objetivosIds.Length} ingredientes con reducción de rango de {reduccion}");
        }
    }

    /// <summary>
    /// Finalizes the effect when no ingredients are affected
    /// </summary>
    private void FinalizarEfecto()
    {
        if (!efectoAplicado)
        {
            // Destroy this manager if no effect was applied
            if (IsSpawned && GetComponent<NetworkObject>() != null)
            {
                GetComponent<NetworkObject>().Despawn(true);
            }
            else if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Cleans up the effect and all associated resources
    /// </summary>
    public void LimpiarEfecto()
    {
        if (mostrarDebug)
        {
            Debug.Log("OlivaVEffectManager: LimpiarEfecto llamado");
        }

        if (IsServer)
        {
            // If we want to revert effects when cleaning up, we would do it here
            // But for Oliva_V, the range reduction typically remains until countered by another effect

            LimpiarEfectoServerRpc();
        }
    }

    /// <summary>
    /// Server RPC to clean up the effect
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void LimpiarEfectoServerRpc()
    {
        // Notify clients and destroy the object
        LimpiarEfectoClientRpc();

        // Self-destruct after a small delay
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    /// <summary>
    /// Client RPC to clean up the effect
    /// </summary>
    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        if (mostrarDebug)
        {
            Debug.Log("Limpiando efecto Oliva_V");
        }
    }

    /// <summary>
    /// Destroys the manager after a delay
    /// </summary>
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Despawn(true); // true to destroy the object on all clients
        }
        else if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}