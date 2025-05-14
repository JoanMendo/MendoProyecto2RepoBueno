using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OlivaVEffectManager : NetworkBehaviour, IEffectManager
{
    private List<GameObject> ingredientesAfectados = new List<GameObject>();
    private NetworkVariable<int> cantidadReduccion = new NetworkVariable<int>(1);
    private List<ulong> networkIds = new List<ulong>();
    private bool efectoAplicado = false;
    private IngredientesSO _ingredienteConfigurado;

    // Implementación de ConfigurarConIngrediente de IEffectManager
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
        Debug.Log($"OlivaVEffectManager configurado con: {ingrediente.name}");

        // Si el ingrediente es Oliva_V, podemos obtener la reducción de rango directamente
        if (ingrediente is Oliva_V olivaV)
        {
            cantidadReduccion.Value = olivaV.reduccionRango;
        }
    }

    // Implementación de IniciarEfecto de IEffectManager
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Obtener la reducción de rango del ingrediente configurado, o usar valor por defecto
        int reduccionRango = 1;

        // Si tenemos la referencia al ingrediente específico, usar su valor
        if (_ingredienteConfigurado is Oliva_V olivaV)
        {
            reduccionRango = olivaV.reduccionRango;
        }

        // Delegar al método específico
        IniciarEfectoOlivaV(nodoOrigen, nodosAfectados, reduccionRango);
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Inicia el efecto de reducción de rango en los ingredientes adyacentes
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void IniciarEfectoOlivaV(GameObject nodoOrigen, List<GameObject> nodosAfectados, int reduccionRango)
    {
        // Convertirse en objeto de red para poder usar RPC
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Configurar cantidad de reducción
        cantidadReduccion.Value = reduccionRango;

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
            Debug.Log("No hay ingredientes afectados por Oliva_V");
            FinalizarEfecto();
            return;
        }

        // Iniciar efecto en el servidor
        AplicarEfectoServerRpc(networkIds.ToArray(), cantidadReduccion.Value);

        Debug.Log($"Efecto Oliva_V iniciado en {ingredientesAfectados.Count} ingredientes con reducción de {cantidadReduccion.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong[] objetivosIds, int reduccion)
    {
        efectoAplicado = true;

        foreach (ulong id in objetivosIds)
        {
            // Preparar clientes primero
            PrepararObjetivoClientRpc(id);

            // Localizar objeto objetivo
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
                continue;

            // Usar coroutine para dar tiempo a la sincronización
            StartCoroutine(AplicarEfectoConDelay(objetivoNetObj.gameObject, reduccion, id));
        }
    }

    private IEnumerator AplicarEfectoConDelay(GameObject objetivo, int reduccion, ulong id)
    {
        yield return new WaitForSeconds(0.1f);

        // Aplicar modificador
        ModificadorRecurso mod = objetivo.GetComponent<ModificadorRecurso>();
        if (mod == null)
        {
            mod = objetivo.AddComponent<ModificadorRecurso>();

            // Asignar recurso base
            componente ingrediente = objetivo.GetComponent<componente>();
            if (ingrediente != null && ingrediente.data != null)
            {
                mod.SetRecursoBase(ingrediente.data);
            }
        }

        // Modificar directamente la variable de red en lugar de usar ServerRpc
        int rangoAntes = mod.GetRangoActual();
        mod.modificacionRango.Value -= reduccion;
        int rangoDespues = mod.GetRangoActual();

        Debug.Log($"Oliva_V: Reducido rango de {objetivo.name}. Rango: {rangoAntes} -> {rangoDespues} (-{reduccion})");

        // Notificar al cliente individualmente
        AplicarEfectoASingleObjetivoClientRpc(id, reduccion);
    }

    [ClientRpc]
    private void PrepararObjetivoClientRpc(ulong id)
    {
        // Localizar objeto objetivo en el cliente
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        // Verificamos si ya existe el ModificadorRecurso en el cliente
        ModificadorRecurso mod = objetivoNetObj.gameObject.GetComponent<ModificadorRecurso>();
        if (mod == null)
        {
            mod = objetivoNetObj.gameObject.AddComponent<ModificadorRecurso>();

            // Obtener recurso del componente
            componente comp = objetivoNetObj.gameObject.GetComponent<componente>();
            if (comp != null && comp.data != null)
            {
                mod.SetRecursoBase(comp.data);
            }
        }

        Debug.Log($"[CLIENTE] Preparado objeto {objetivoNetObj.name} para recibir efecto Oliva_V");
    }

    [ClientRpc]
    private void AplicarEfectoASingleObjetivoClientRpc(ulong id, int reduccion)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        Debug.Log($"[CLIENTE] Efecto Oliva_V aplicado a {objetivoNetObj.name}, reducción: {reduccion}");
    }

    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong[] objetivosIds, int reduccion)
    {
        Debug.Log($"Efecto Oliva_V aplicado a {objetivosIds.Length} ingredientes con reducción de rango de {reduccion}");

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
        // Si se aplicó el efecto, permanece activo para mantener la reducción de rango
        // Se destruirá al final del juego o cuando otro sistema lo determine
    }

    // Implementación de LimpiarEfecto de IEffectManager
    public void LimpiarEfecto()
    {
        Debug.Log("OlivaVEffectManager: LimpiarEfecto llamado");
        if (IsSpawned)
        {
            LimpiarEfectoServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LimpiarEfectoServerRpc()
    {
        // Aquí podrías revertir los efectos si es necesario (añadir el rango eliminado)

        // Notificar a los clientes y eliminar el objeto
        LimpiarEfectoClientRpc();

        // Auto-destrucción después de un pequeño retraso
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        Debug.Log("Limpiando efecto Oliva_V");
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