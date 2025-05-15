using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manager for handling Ternera effects, which increase life and make the first neighbor immobile.
/// </summary>
public class TerneraEffectManager : NetworkBehaviour, IEffectManager
{
    // Configuration for the life increase amount
    [SerializeField] private float aumentoVida = 1f;

    // References to affected objects
    private GameObject ingredienteObjetivo;

    // Network synchronized state variables
    private NetworkVariable<bool> efectoAplicado = new NetworkVariable<bool>(false);

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
            Debug.Log($"TerneraEffectManager configurado con: {ingrediente.name}");
        }

        // If the ingredient is Ternera, we can get the life increase directly
        if (ingrediente is Ternera terneraIngrediente)
        {
            aumentoVida = terneraIngrediente.aumentoVida;
        }
    }

    /// <summary>
    /// Starts the effect on the specified nodes
    /// </summary>
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Check if there's at least one affected node
        if (nodosAfectados == null || nodosAfectados.Count == 0)
        {
            if (mostrarDebug)
            {
                Debug.LogWarning("TerneraEffectManager: No hay nodos afectados para aplicar el efecto");
            }
            return;
        }

        // Ternera only affects the first neighboring node
        GameObject primerVecino = nodosAfectados[0];

        // Get the life increase from the configured ingredient, or use default value
        float cantidadVida = aumentoVida;

        // If we have a reference to the specific ingredient, use its value
        if (_ingredienteConfigurado is Ternera terneraIngrediente)
        {
            cantidadVida = terneraIngrediente.aumentoVida;
        }

        // Delegate to the specific method
        IniciarEfectoTernera(nodoOrigen, primerVecino, cantidadVida);
    }

    /// <summary>
    /// Initiates the Ternera effect on a specific target node
    /// </summary>
    public void IniciarEfectoTernera(GameObject nodoOrigen, GameObject nodoObjetivo, float cantidadVida)
    {
        // 1. Initialize values
        aumentoVida = cantidadVida;

        // 2. Get reference to the target ingredient
        Node nodoVecinoComp = nodoObjetivo.GetComponent<Node>();
        if (nodoVecinoComp == null || !nodoVecinoComp.hasIngredient.Value)
        {
            if (mostrarDebug)
            {
                Debug.LogWarning("TerneraEffectManager: El nodo objetivo no tiene un ingrediente");
            }
            return;
        }

        ingredienteObjetivo = nodoVecinoComp.currentIngredient;

        // 3. Verify that we have a NetworkObject and spawn it
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }
        else if (GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("TerneraEffectManager: Falta componente NetworkObject");
            return;
        }

        // 4. Verify that the target ingredient has a NetworkObject
        NetworkObject objetivoNetObj = ingredienteObjetivo.GetComponent<NetworkObject>();
        if (objetivoNetObj == null || !objetivoNetObj.IsSpawned)
        {
            Debug.LogError("TerneraEffectManager: El ingrediente objetivo no tiene NetworkObject válido");
            return;
        }

        // 5. Apply the effect on the server
        AplicarEfectoServerRpc(
            objetivoNetObj.NetworkObjectId,
            aumentoVida
        );

        if (mostrarDebug)
        {
            Debug.Log($"Efecto de Ternera iniciado con aumento de vida: {aumentoVida}");
        }
    }

    /// <summary>
    /// Server RPC to apply the effect to the target
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong objetivoId, float cantidad)
    {
        // Prepare client first
        PrepararObjetivoClientRpc(objetivoId);

        // Find target object
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objetivoId, out NetworkObject objetivoNetObj))
        {
            Debug.LogError($"TerneraEffectManager: No se encontró objeto con ID {objetivoId}");
            return;
        }

        // Use coroutine to give time for synchronization
        StartCoroutine(AplicarEfectoConDelay(objetivoNetObj.gameObject, cantidad, objetivoId));
    }

    /// <summary>
    /// Applies the effect with a delay to ensure synchronization
    /// </summary>
    private IEnumerator AplicarEfectoConDelay(GameObject objetivo, float cantidad, ulong id)
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

        // Directly modify the network variables
        float vidaAntes = mod.GetVidaActual();
        mod.AumentarVida(cantidad);
        float vidaDespues = mod.GetVidaActual();

        bool movibleAntes = mod.EsMovible();
        mod.HacerInmovil();

        if (mostrarDebug)
        {
            Debug.Log($"Ternera: Aplicado efecto a {objetivo.name}. " +
                      $"Vida: {vidaAntes} -> {vidaDespues} (+{cantidad}), " +
                      $"Movible: {movibleAntes} -> {mod.EsMovible()}");
        }

        // Update state and notify clients
        efectoAplicado.Value = true;
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
            Debug.Log($"[CLIENTE] Preparado objeto {objetivoNetObj.name} para recibir efecto Ternera");
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
            Debug.Log($"[CLIENTE] Efecto Ternera aplicado a {objetivoNetObj.name}");
        }
    }

    /// <summary>
    /// Notifies all clients that the effect has been applied
    /// </summary>
    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong objetivoId)
    {
        if (mostrarDebug)
        {
            Debug.Log($"Efecto de Ternera aplicado al objeto {objetivoId}");
        }
    }

    /// <summary>
    /// Cleans up the effect and all associated resources
    /// </summary>
    public void LimpiarEfecto()
    {
        if (mostrarDebug)
        {
            Debug.Log("TerneraEffectManager: LimpiarEfecto llamado");
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
        // For Ternera, we typically don't revert the effects since they are permanent
        // (increased life and immobility)

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
            Debug.Log("Limpiando efecto Ternera");
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