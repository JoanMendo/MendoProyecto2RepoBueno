using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manager for handling Queso effects, which make all neighboring ingredients immobile.
/// </summary>
public class QuesoEffectManager : NetworkBehaviour, IEffectManager
{
    // List of affected ingredients
    private List<GameObject> ingredientesAfectados = new List<GameObject>();

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
            Debug.Log($"QuesoEffectManager configurado con: {ingrediente.name}");
        }

        // Queso doesn't have configurable parameters from the ScriptableObject,
        // but if it did in the future, we could set them here
    }

    /// <summary>
    /// Starts the effect on the specified nodes
    /// </summary>
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Simply delegate to the specific existing method
        IniciarEfectoQueso(nodoOrigen, nodosAfectados);
    }

    /// <summary>
    /// Initiates the immobilization effect on all neighboring ingredients
    /// </summary>
    public void IniciarEfectoQueso(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Make sure we have a NetworkObject and it's spawned
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Clear previous lists (in case the manager is reused)
        ingredientesAfectados.Clear();
        networkIds.Clear();

        // Collect ingredients from affected nodes and network IDs
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
                Debug.Log("No hay ingredientes afectados por Queso");
            }

            FinalizarEfecto();
            return;
        }

        // Start the effect on the server
        AplicarEfectoServerRpc(networkIds.ToArray());

        if (mostrarDebug)
        {
            Debug.Log($"Efecto Queso iniciado en {ingredientesAfectados.Count} ingredientes");
        }
    }

    /// <summary>
    /// Server RPC to apply the effect to all targets
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong[] objetivosIds)
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
            StartCoroutine(AplicarEfectoConDelay(objetivoNetObj.gameObject, id));
        }
    }

    /// <summary>
    /// Applies the effect with a delay to ensure synchronization
    /// </summary>
    private IEnumerator AplicarEfectoConDelay(GameObject objetivo, ulong id)
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

        // Check the state before applying the effect
        bool movibleAntes = mod.EsMovible();

        // Apply the effect - make it immobile
        mod.HacerInmovil();

        if (mostrarDebug)
        {
            Debug.Log($"Queso: Aplicado efecto a {objetivo.name}. Movible: {movibleAntes} -> {mod.EsMovible()}");
        }

        // Notify the client individually
        AplicarEfectoASingleObjetivoClientRpc(id);
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
            Debug.Log($"[CLIENTE] Preparado objeto {objetivoNetObj.name} para recibir efecto Queso");
        }
    }

    /// <summary>
    /// Notifies a client that an effect has been applied to a specific object
    /// </summary>
    [ClientRpc]
    private void AplicarEfectoASingleObjetivoClientRpc(ulong id)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        if (mostrarDebug)
        {
            Debug.Log($"[CLIENTE] Efecto Queso aplicado a {objetivoNetObj.name}");
        }
    }

    /// <summary>
    /// Notifies all clients that effects have been applied
    /// </summary>
    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong[] objetivosIds)
    {
        if (mostrarDebug)
        {
            Debug.Log($"Efecto Queso aplicado a {objetivosIds.Length} ingredientes");
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
            Debug.Log("QuesoEffectManager: LimpiarEfecto llamado");
        }

        if (IsSpawned)
        {
            LimpiarEfectoServerRpc();
        }
    }

    /// <summary>
    /// Server RPC to clean up the effect
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void LimpiarEfectoServerRpc()
    {
        // For Queso, the immobility effect typically remains until countered by another effect

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
            Debug.Log("Limpiando efecto Queso");
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