using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class OlivaNEffectManager : NetworkBehaviour, IEffectManager
{
    private List<GameObject> ingredientesAfectados = new List<GameObject>();
    private NetworkVariable<int> cantidadAumento = new NetworkVariable<int>(1);
    private List<ulong> networkIds = new List<ulong>();
    private bool efectoAplicado = false;
    private IngredientesSO _ingredienteConfigurado;

    // Implementación de ConfigurarConIngrediente de IEffectManager
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
        Debug.Log($"OlivaNEffectManager configurado con: {ingrediente.name}");

        // Si el ingrediente es Oliva_N, podemos obtener el aumento de rango directamente
        if (ingrediente is Oliva_N olivaN)
        {
            cantidadAumento.Value = olivaN.aumentoRango;
        }
    }

    // Implementación de IniciarEfecto de IEffectManager
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Obtener el aumento de rango del ingrediente configurado, o usar valor por defecto
        int aumentoRango = 1;

        // Si tenemos la referencia al ingrediente específico, usar su valor
        if (_ingredienteConfigurado is Oliva_N olivaN)
        {
            aumentoRango = olivaN.aumentoRango;
        }

        // Delegar al método específico
        IniciarEfectoOlivaN(nodoOrigen, nodosAfectados, aumentoRango);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el efecto de aumento de rango en los ingredientes adyacentes
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarEfectoOlivaN(GameObject nodoOrigen, List<GameObject> nodosAfectados, int aumentoRango)
    {
        // Convertirse en objeto de red para poder usar RPC
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Configurar cantidad de aumento
        cantidadAumento.Value = aumentoRango;

        // Limpiar listas previas (por si se reutiliza el manager)
        ingredientesAfectados.Clear();
        networkIds.Clear();

        // Recolectar ingredientes e IDs de red
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
            Debug.Log("No hay ingredientes afectados por Oliva_N");
            FinalizarEfecto();
            return;
        }

        // Iniciar efecto en el servidor
        AplicarEfectoServerRpc(networkIds.ToArray(), cantidadAumento.Value);

        Debug.Log($"Efecto Oliva_N iniciado en {ingredientesAfectados.Count} ingredientes con aumento de {cantidadAumento.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong[] objetivosIds, int aumento)
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

            mod.AumentarRango(aumento);
        }

        // Notificar a todos los clientes
        AplicarEfectoClientRpc(objetivosIds, aumento);
    }

    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong[] objetivosIds, int aumento)
    {
        Debug.Log($"Efecto Oliva_N aplicado a {objetivosIds.Length} ingredientes con aumento de rango de {aumento}");

        // Aquí podrías añadir efectos visuales adicionales
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
        // Si se aplicó el efecto, permanece activo para mantener el aumento de rango
        // Se destruirá al final del juego o cuando otro sistema lo determine
    }

    // Implementación de LimpiarEfecto de IEffectManager
    public void LimpiarEfecto()
    {
        Debug.Log("OlivaNEffectManager: LimpiarEfecto llamado");
        if (IsSpawned)
        {
            LimpiarEfectoServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LimpiarEfectoServerRpc()
    {
        // Aquí podrías revertir los efectos si es necesario (restar el rango añadido)

        // Notificar a los clientes y eliminar el objeto
        LimpiarEfectoClientRpc();

        // Auto-destrucción después de un pequeño retraso
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        Debug.Log("Limpiando efecto Oliva_N");
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