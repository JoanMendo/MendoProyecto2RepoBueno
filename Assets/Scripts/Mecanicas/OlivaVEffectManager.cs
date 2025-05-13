using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class OlivaVEffectManager : NetworkBehaviour, IEffectManager
{
    private List<GameObject> ingredientesAfectados = new List<GameObject>();
    private NetworkVariable<int> cantidadReduccion = new NetworkVariable<int>(1);
    private List<ulong> networkIds = new List<ulong>();
    private bool efectoAplicado = false;
    private IngredientesSO _ingredienteConfigurado;

    // Implementaci�n de ConfigurarConIngrediente de IEffectManager
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
        Debug.Log($"OlivaVEffectManager configurado con: {ingrediente.name}");

        // Si el ingrediente es Oliva_V, podemos obtener la reducci�n de rango directamente
        if (ingrediente is Oliva_V olivaV)
        {
            cantidadReduccion.Value = olivaV.reduccionRango;
        }
    }

    // Implementaci�n de IniciarEfecto de IEffectManager
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Obtener la reducci�n de rango del ingrediente configurado, o usar valor por defecto
        int reduccionRango = 1;

        // Si tenemos la referencia al ingrediente espec�fico, usar su valor
        if (_ingredienteConfigurado is Oliva_V olivaV)
        {
            reduccionRango = olivaV.reduccionRango;
        }

        // Delegar al m�todo espec�fico
        IniciarEfectoOlivaV(nodoOrigen, nodosAfectados, reduccionRango);
    }

    /// ��<summary>_PLACEHOLDER��
    /// Inicia el efecto de reducci�n de rango en los ingredientes adyacentes
    /// ��</summary>_PLACEHOLDER��
    public void IniciarEfectoOlivaV(GameObject nodoOrigen, List<GameObject> nodosAfectados, int reduccionRango)
    {
        // Convertirse en objeto de red para poder usar RPC
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Configurar cantidad de reducci�n
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

        Debug.Log($"Efecto Oliva_V iniciado en {ingredientesAfectados.Count} ingredientes con reducci�n de {cantidadReduccion.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong[] objetivosIds, int reduccion)
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

            mod.DisminuirRango(reduccion);
        }

        // Notificar a todos los clientes
        AplicarEfectoClientRpc(objetivosIds, reduccion);
    }

    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong[] objetivosIds, int reduccion)
    {
        Debug.Log($"Efecto Oliva_V aplicado a {objetivosIds.Length} ingredientes con reducci�n de rango de {reduccion}");

        // Aqu� podr�as a�adir efectos visuales adicionales
    }

    private void FinalizarEfecto()
    {
        if (!efectoAplicado)
        {
            // Destruir este gestor si no se aplic� ning�n efecto
            if (IsSpawned && GetComponent<NetworkObject>() != null)
            {
                GetComponent<NetworkObject>().Despawn(true);
            }
            else if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        // Si se aplic� el efecto, permanece activo para mantener la reducci�n de rango
        // Se destruir� al final del juego o cuando otro sistema lo determine
    }

    // Implementaci�n de LimpiarEfecto de IEffectManager
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
        // Aqu� podr�as revertir los efectos si es necesario (a�adir el rango eliminado)

        // Notificar a los clientes y eliminar el objeto
        LimpiarEfectoClientRpc();

        // Auto-destrucci�n despu�s de un peque�o retraso
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
