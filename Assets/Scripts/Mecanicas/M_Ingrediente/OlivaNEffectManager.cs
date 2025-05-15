using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Manager for handling Oliva_N effects, which increase the range of nearby ingredients.
/// </summary>
public class OlivaNEffectManager : NetworkBehaviour, IEffectManager
{
    // List of affected ingredients
    private List<GameObject> ingredientesAfectados = new List<GameObject>();

    // Network synchronized variables
    private NetworkVariable<int> cantidadAumento = new NetworkVariable<int>(1);

    // List of NetworkObjectIds for tracking
    private List<ulong> networkIds = new List<ulong>();

    // Reference to the configured ingredient
    private IngredientesSO _ingredienteConfigurado;

    // Variables for temporal effect management
    private GameObject nodoOrigenAlmacenado;
    private float intervaloComprobacion = 0.5f; // Check every half second
    private bool efectoActivo = false;

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
            Debug.Log($"OlivaNEffectManager configurado con: {ingrediente.name}");
        }

        // If the ingredient is Oliva_N, we can get the range increase directly
        if (ingrediente is Oliva_N olivaN)
        {
            cantidadAumento.Value = olivaN.aumentoRango;
        }
    }

    /// <summary>
    /// Starts the effect on the specified nodes
    /// </summary>
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Get the range increase from the configured ingredient, or use default value
        int aumentoRango = 1;

        // If we have a reference to the specific ingredient, use its value
        if (_ingredienteConfigurado is Oliva_N olivaN)
        {
            aumentoRango = olivaN.aumentoRango;
        }

        // Delegate to the specific method
        IniciarEfectoOlivaN(nodoOrigen, nodosAfectados, aumentoRango);
    }

    /// <summary>
    /// Starts the range increase effect on adjacent ingredients
    /// </summary>
    public void IniciarEfectoOlivaN(GameObject nodoOrigen, List<GameObject> nodosAfectados, int aumentoRango)
    {
        // Store the origin node to continue checking
        nodoOrigenAlmacenado = nodoOrigen;

        // Make sure we have a NetworkObject and it's spawned
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Configure the increase amount
        cantidadAumento.Value = aumentoRango;

        // Clear previous lists (in case the manager is reused)
        ingredientesAfectados.Clear();
        networkIds.Clear();

        // Perform the initial check of ingredients in range
        ActualizarIngredientesEnRango(nodosAfectados);

        // Start periodic checking
        efectoActivo = true;
        InvokeRepeating(nameof(ComprobarIngredientesEnRango), intervaloComprobacion, intervaloComprobacion);

        if (mostrarDebug)
        {
            Debug.Log($"Efecto Oliva_N iniciado en {ingredientesAfectados.Count} ingredientes con aumento de {cantidadAumento.Value}");
        }
    }

    /// <summary>
    /// Updates the list of affected ingredients and applies/removes effects as needed
    /// </summary>
    private void ActualizarIngredientesEnRango(List<GameObject> nodosActuales)
    {
        // Prepare a set of IDs currently in range
        HashSet<ulong> idsEnRango = new HashSet<ulong>();

        // Check ingredients in current nodes
        foreach (var nodoVecino in nodosActuales)
        {
            if (nodoVecino == null) continue;

            Node nodo = nodoVecino.GetComponent<Node>();
            if (nodo == null || !nodo.hasIngredient.Value || nodo.currentIngredient == null)
                continue;

            // Add to the list of affected ingredients if it's new
            if (!ingredientesAfectados.Contains(nodo.currentIngredient))
            {
                ingredientesAfectados.Add(nodo.currentIngredient);
            }

            // Get NetworkObjectId and add to the set of IDs in range
            NetworkObject netObj = nodo.currentIngredient.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                idsEnRango.Add(netObj.NetworkObjectId);

                // If it wasn't already in our list, apply the effect
                if (!networkIds.Contains(netObj.NetworkObjectId))
                {
                    networkIds.Add(netObj.NetworkObjectId);

                    if (IsServer)
                    {
                        // First prepare the target, then apply the effect
                        PrepararObjetivoClientRpc(netObj.NetworkObjectId);
                        StartCoroutine(AplicarEfectoConDelay(netObj.gameObject, cantidadAumento.Value, netObj.NetworkObjectId));
                    }
                }
            }
        }

        // Check which ingredients are no longer in range
        if (IsServer)
        {
            List<ulong> idsARemover = new List<ulong>();

            foreach (ulong id in networkIds)
            {
                if (!idsEnRango.Contains(id))
                {
                    // This ingredient is no longer in range, remove the effect
                    idsARemover.Add(id);

                    // Find the object and remove the effect
                    if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
                    {
                        RemoverEfectoIngrediente(objetivoNetObj.gameObject, cantidadAumento.Value);
                    }

                    // Notify clients
                    EliminarEfectoDeNetworkIdClientRpc(id);
                }
            }

            // Remove IDs that are no longer in range from our list
            foreach (ulong id in idsARemover)
            {
                networkIds.Remove(id);
            }
        }
    }

    /// <summary>
    /// Periodically checks which ingredients are in range and updates effects
    /// </summary>
    private void ComprobarIngredientesEnRango()
    {
        if (!efectoActivo || nodoOrigenAlmacenado == null) return;

        // Verify that the Oliva_N still exists
        Node nodoOliva = nodoOrigenAlmacenado.GetComponent<Node>();
        if (nodoOliva == null || !nodoOliva.hasIngredient.Value)
        {
            // If the olive no longer exists, deactivate the entire effect
            LimpiarEfecto();
            return;
        }

        // Get the current neighbor nodes
        List<GameObject> nodosVecinosActuales = new List<GameObject>();

        // If we have a reference to the ScriptableObject, use it directly
        if (_ingredienteConfigurado != null)
        {
            NodeMap mapa = nodoOliva.nodeMap;
            if (mapa != null)
            {
                nodosVecinosActuales = _ingredienteConfigurado.CalcularNodosAfectados(nodoOrigenAlmacenado, mapa);
            }
        }

        // Update ingredients in range with the current nodes
        ActualizarIngredientesEnRango(nodosVecinosActuales);
    }

    /// <summary>
    /// Applies the effect to a specific ingredient with a delay to ensure synchronization
    /// </summary>
    private IEnumerator AplicarEfectoConDelay(GameObject objetivo, int aumento, ulong id)
    {
        // Wait to ensure the component has been registered
        yield return new WaitForSeconds(0.1f);

        if (objetivo == null) yield break;

        // Get or add the ModificadorRecurso component
        ModificadorRecurso mod = objetivo.GetComponent<ModificadorRecurso>();
        if (mod == null)
        {
            mod = objetivo.AddComponent<ModificadorRecurso>();

            // Assign the base resource
            componente ingrediente = objetivo.GetComponent<componente>();
            if (ingrediente != null && ingrediente.data != null)
            {
                mod.SetRecursoBase(ingrediente.data);
            }
        }

        // Check BEFORE the increase
        int rangoAntes = mod.GetRangoActual();

        // Use direct modification on the server
        mod.AumentarRango(aumento);

        // Check AFTER the increase
        int rangoDespues = mod.GetRangoActual();

        if (mostrarDebug)
        {
            Debug.Log($"Oliva_N: Aplicado aumento a {objetivo.name}. Rango: {rangoAntes} -> {rangoDespues} (+{aumento})");
        }

        // Notify the client for visual effects
        AplicarEfectoClientRpc(id, aumento);
    }

    /// <summary>
    /// Removes the effect from an ingredient
    /// </summary>
    private void RemoverEfectoIngrediente(GameObject objetivo, int cantidad)
    {
        if (objetivo == null) return;

        // Get the ModificadorRecurso component
        ModificadorRecurso mod = objetivo.GetComponent<ModificadorRecurso>();
        if (mod != null)
        {
            int rangoAntes = mod.GetRangoActual();

            // Use direct modification
            mod.DisminuirRango(cantidad);

            int rangoDespues = mod.GetRangoActual();

            if (mostrarDebug)
            {
                Debug.Log($"Oliva_N: Eliminado aumento de {objetivo.name}. Rango: {rangoAntes} -> {rangoDespues} (-{cantidad})");
            }
        }
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
            Debug.Log($"[CLIENTE] Preparado objeto {objetivoNetObj.name} para recibir efecto");
        }
    }

    /// <summary>
    /// Notifies clients that an effect has been applied
    /// </summary>
    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong id, int aumento)
    {
        // Locate the target object on the client
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        if (mostrarDebug)
        {
            Debug.Log($"[CLIENTE] Efecto aplicado a {objetivoNetObj.name}, aumento: {aumento}");
        }
    }

    /// <summary>
    /// Notifies clients that an effect has been removed
    /// </summary>
    [ClientRpc]
    private void EliminarEfectoDeNetworkIdClientRpc(ulong id)
    {
        if (mostrarDebug)
        {
            Debug.Log($"Cliente: Oliva_N eliminó aumento de rango de objeto con ID {id}");
        }

        // Here you could remove any visual effects associated with the range increase
    }

    /// <summary>
    /// Cleans up the effect and all associated resources
    /// </summary>
    public void LimpiarEfecto()
    {
        if (mostrarDebug)
        {
            Debug.Log("OlivaNEffectManager: LimpiarEfecto llamado");
        }

        // Stop the periodic check
        CancelInvoke(nameof(ComprobarIngredientesEnRango));
        efectoActivo = false;

        if (IsServer)
        {
            // Remove the effect from all affected ingredients
            foreach (ulong id in networkIds)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
                {
                    RemoverEfectoIngrediente(objetivoNetObj.gameObject, cantidadAumento.Value);
                }
                EliminarEfectoDeNetworkIdClientRpc(id);
            }

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
            Debug.Log("Limpiando efecto Oliva_N");
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

    private void OnDestroy()
    {
        // Make sure everything is cleaned up
        CancelInvoke(nameof(ComprobarIngredientesEnRango));

        // Only clean up effects if we're spawned (to avoid duplicate calls)
        if (IsSpawned)
        {
            LimpiarEfecto();
        }
    }
}