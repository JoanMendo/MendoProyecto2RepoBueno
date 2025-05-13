using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class QuesoEffectManager : NetworkBehaviour, IEffectManager
{
    private List<GameObject> ingredientesAfectados = new List<GameObject>();
    private List<ulong> networkIds = new List<ulong>();
    private bool efectoAplicado = false;
    private IngredientesSO _ingredienteConfigurado;

    // Implementación de ConfigurarConIngrediente de IEffectManager
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
        Debug.Log($"QuesoEffectManager configurado con: {ingrediente.name}");

        // El queso no tiene parámetros configurables desde el ScriptableObject,
        // pero si los tuviera en el futuro, se podrían configurar aquí
    }

    // Implementación de IniciarEfecto de IEffectManager
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Simplemente delegar al método específico existente
        IniciarEfectoQueso(nodoOrigen, nodosAfectados);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el efecto de inmovilización en todos los ingredientes adyacentes
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarEfectoQueso(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Convertirse en objeto de red para poder usar RPC
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Limpiar listas previas (por si se reutiliza el manager)
        ingredientesAfectados.Clear();
        networkIds.Clear();

        // Recolectar ingredientes de los nodos afectados e IDs de red
        foreach (var nodoVecino in nodosAfectados)
        {
            Node nodo = nodoVecino.GetComponent<Node>();
            if (nodo == null || !nodo.hasIngredient.Value || nodo.currentIngredient == null)
                continue;

            ingredientesAfectados.Add(nodo.currentIngredient);

            // Obtener NetworkObjectId de cada ingrediente afectado
            NetworkObject netObj = nodo.currentIngredient.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                networkIds.Add(netObj.NetworkObjectId);
            }
        }

        // Si no hay ingredientes afectados, terminar
        if (ingredientesAfectados.Count == 0)
        {
            Debug.Log("No hay ingredientes afectados por Queso");
            FinalizarEfecto();
            return;
        }

        // Iniciar efecto en el servidor
        AplicarEfectoServerRpc(networkIds.ToArray());

        Debug.Log($"Efecto Queso iniciado en {ingredientesAfectados.Count} ingredientes");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong[] objetivosIds)
    {
        efectoAplicado = true;

        foreach (ulong id in objetivosIds)
        {
            // Localizar objeto objetivo
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
                continue;

            // Aplicar modificador
            ModificadorRecurso mod = objetivoNetObj.gameObject.GetComponent<ModificadorRecurso>();
            if (mod == null)
                mod = objetivoNetObj.gameObject.AddComponent<ModificadorRecurso>();

            mod.HacerInmovil();
        }

        // Notificar a todos los clientes
        AplicarEfectoClientRpc(objetivosIds);
    }

    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong[] objetivosIds)
    {
        Debug.Log($"Efecto Queso aplicado a {objetivosIds.Length} ingredientes");

        // Aquí podrías añadir efectos visuales adicionales si lo deseas
    }

    private void FinalizarEfecto()
    {
        if (!efectoAplicado)
        {
            // Destruir este gestor si no se aplicó ningún efecto
            if (IsSpawned && GetComponent<NetworkObject>() != null)
            {
                GetComponent<NetworkObject>().Despawn(true);
            }
            else if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        // Si se aplicó el efecto, permanece activo para mantener la inmovilidad
        // Se destruirá al final del juego o cuando otro sistema lo determine
    }

    // Implementación de LimpiarEfecto de IEffectManager
    public void LimpiarEfecto()
    {
        Debug.Log("QuesoEffectManager: LimpiarEfecto llamado");
        if (IsSpawned)
        {
            LimpiarEfectoServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LimpiarEfectoServerRpc()
    {
        // Notificar a los clientes y eliminar el objeto
        LimpiarEfectoClientRpc();

        // Auto-destrucción después de un pequeño retraso
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        Debug.Log("Limpiando efecto Queso");
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Despawn(true); // true para destruir el objeto en todos los clientes
        }
        else if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}