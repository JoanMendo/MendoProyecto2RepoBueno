using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class OlivaNEffectManager : NetworkBehaviour, IEffectManager
{
    private List<GameObject> ingredientesAfectados = new List<GameObject>();
    private NetworkVariable<int> cantidadAumento = new NetworkVariable<int>(1);
    private List<ulong> networkIds = new List<ulong>();
    private IngredientesSO _ingredienteConfigurado;

    // Variables para manejo temporal del efecto
    private GameObject nodoOrigenAlmacenado;
    private float intervaloComprobacion = 0.5f; // Comprueba cada medio segundo
    private bool efectoActivo = false;

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
        // Almacenar el nodo origen para poder seguir comprobando
        nodoOrigenAlmacenado = nodoOrigen;

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

        // Realizar comprobación inicial de ingredientes en rango
        ActualizarIngredientesEnRango(nodosAfectados);

        // Iniciar comprobación periódica
        efectoActivo = true;
        InvokeRepeating("ComprobarIngredientesEnRango", intervaloComprobacion, intervaloComprobacion);

        Debug.Log($"Efecto Oliva_N iniciado en {ingredientesAfectados.Count} ingredientes con aumento de {cantidadAumento.Value}");
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Actualiza la lista de ingredientes afectados y aplica/elimina efectos según sea necesario
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void ActualizarIngredientesEnRango(List<GameObject> nodosActuales)
    {
        // Preparar un conjunto de IDs que están actualmente en rango
        HashSet<ulong> idsEnRango = new HashSet<ulong>();

        // Verificar ingredientes en nodos actuales
        foreach (var nodoVecino in nodosActuales)
        {
            Node nodo = nodoVecino.GetComponent<Node>();
            if (nodo == null || !nodo.hasIngredient.Value || nodo.currentIngredient == null)
                continue;

            // Añadir a la lista de ingredientes afectados si es nuevo
            if (!ingredientesAfectados.Contains(nodo.currentIngredient))
            {
                ingredientesAfectados.Add(nodo.currentIngredient);
            }

            // Obtener NetworkObjectId y añadir al conjunto de IDs en rango
            NetworkObject netObj = nodo.currentIngredient.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                idsEnRango.Add(netObj.NetworkObjectId);

                // Si no estaba ya en nuestra lista, aplicar efecto
                if (!networkIds.Contains(netObj.NetworkObjectId))
                {
                    networkIds.Add(netObj.NetworkObjectId);

                    if (IsServer)
                    {
                        AplicarEfectoASingleNetworkIdServerRpc(netObj.NetworkObjectId, cantidadAumento.Value);
                    }
                }
            }
        }

        // Comprobar qué ingredientes ya no están en rango
        if (IsServer)
        {
            List<ulong> idsARemover = new List<ulong>();

            foreach (ulong id in networkIds)
            {
                if (!idsEnRango.Contains(id))
                {
                    // Este ingrediente ya no está en rango, eliminar efecto
                    idsARemover.Add(id);
                    EliminarEfectoDeNetworkIdServerRpc(id);
                }
            }

            // Eliminar IDs que ya no están en rango de nuestra lista
            foreach (ulong id in idsARemover)
            {
                networkIds.Remove(id);
            }
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Comprueba periódicamente qué ingredientes están en rango y actualiza efectos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void ComprobarIngredientesEnRango()
    {
        if (!efectoActivo || nodoOrigenAlmacenado == null) return;

        // Verificar que la Oliva_N sigue existiendo
        Node nodoOliva = nodoOrigenAlmacenado.GetComponent<Node>();
        if (nodoOliva == null || !nodoOliva.hasIngredient.Value)
        {
            // Si la oliva ya no existe, desactivar todo el efecto
            LimpiarEfecto();
            return;
        }

        // Obtener los nodos vecinos usando el método existente en el ScriptableObject
        List<GameObject> nodosVecinosActuales = new List<GameObject>();

        // Si tenemos referencia al ScriptableObject, usarla directamente
        if (_ingredienteConfigurado != null)
        {
            NodeMap mapa = nodoOliva.GetComponentInParent<NodeMap>();
            if (mapa != null)
            {
                nodosVecinosActuales = _ingredienteConfigurado.CalcularNodosAfectados(nodoOrigenAlmacenado, mapa);
            }
        }

        // Actualizar ingredientes en rango con los nodos actuales
        ActualizarIngredientesEnRango(nodosVecinosActuales);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoASingleNetworkIdServerRpc(ulong id, int aumento)
    {
        // Primero preparar el componente en los clientes para evitar errores
        PrepararObjetivoClientRpc(id);

        // Localizar objeto
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        GameObject objetivo = objetivoNetObj.gameObject;

        // Iniciar la coroutine para esperar a que todo esté registrado
        StartCoroutine(AplicarEfectoConDelay(objetivo, aumento, id));
    }

    private IEnumerator AplicarEfectoConDelay(GameObject objetivo, int aumento, ulong id)
    {
        // Esperar para asegurar que el componente se haya registrado
        yield return new WaitForSeconds(0.1f);

        // Asegurarse de que tiene el componente ModificadorRecurso
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

        // Verificar ANTES del aumento
        int rangoAntes = mod.GetRangoActual();

        // Modificación DIRECTA en el servidor (no usar AumentarRango)
        mod.modificacionRango.Value += aumento;

        // Verificar DESPUÉS del aumento
        int rangoDespues = mod.GetRangoActual();

        Debug.Log($"Oliva_N: Aplicado aumento a {objetivo.name}. Rango: {rangoAntes} -> {rangoDespues} (+{aumento})");

        // Notificar al cliente para efectos visuales
        AplicarEfectoClientRpc(id, aumento);
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

        Debug.Log($"[CLIENTE] Preparado objeto {objetivoNetObj.name} para recibir efecto");
    }

    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong id, int aumento)
    {
        // Localizar objeto objetivo en el cliente
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        Debug.Log($"[CLIENTE] Efecto aplicado a {objetivoNetObj.name}, aumento: {aumento}");
    }

    [ClientRpc]
    private void AplicarEfectoASingleNetworkIdClientRpc(ulong id, int aumento)
    {
        // Localizar objeto objetivo en el cliente
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        // Verificamos si ya existe el ModificadorRecurso en el cliente
        ModificadorRecurso mod = objetivoNetObj.gameObject.GetComponent<ModificadorRecurso>();
        if (mod == null)
        {
            Debug.LogWarning($"[CLIENTE] ModificadorRecurso no encontrado en {objetivoNetObj.name}, añadiendo...");
            mod = objetivoNetObj.gameObject.AddComponent<ModificadorRecurso>();

            // Obtener recurso del componente
            componente comp = objetivoNetObj.gameObject.GetComponent<componente>();
            if (comp != null && comp.data != null)
            {
                mod.SetRecursoBase(comp.data);
            }
        }

        Debug.Log($"[CLIENTE] Efecto aplicado a {objetivoNetObj.name}, aumento: {aumento}");
    }


    // Reemplazar este otro método también
    [ServerRpc(RequireOwnership = false)]
    private void EliminarEfectoDeNetworkIdServerRpc(ulong id)
    {
        // Localizar objeto objetivo
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        // Eliminar modificador
        ModificadorRecurso mod = objetivoNetObj.gameObject.GetComponent<ModificadorRecurso>();
        if (mod != null)
        {
            int rangoAntes = mod.GetRangoActual();

            // Usar modificación directa en lugar de RPC
            mod.DisminuirRango(cantidadAumento.Value);

            int rangoDespues = mod.GetRangoActual();
            Debug.Log($"Oliva_N: Eliminado aumento de {objetivoNetObj.name}. Rango: {rangoAntes} -> {rangoDespues}");
        }

        // Notificar al cliente
        EliminarEfectoDeNetworkIdClientRpc(id);
    }

    [ClientRpc]
    private void EliminarEfectoDeNetworkIdClientRpc(ulong id)
    {
        Debug.Log($"Cliente: Oliva_N eliminó aumento de rango de objeto con ID {id}");

        // Aquí podrías eliminar efectos visuales asociados
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
        {
            // Ejemplo: Eliminar efecto visual
            // QuitarEfectoVisual(objetivoNetObj.gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong[] objetivosIds, int aumento)
    {
        foreach (ulong id in objetivosIds)
        {
            // Localizar objeto objetivo
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
                continue;

            // Aplicar modificador
            ModificadorRecurso mod = objetivoNetObj.gameObject.GetComponent<ModificadorRecurso>();
            if (mod == null)
                mod = objetivoNetObj.gameObject.AddComponent<ModificadorRecurso>();

            mod.AumentarRangoServerRpc(aumento);
        }

        // Notificar a todos los clientes
        AplicarEfectoClientRpc(objetivosIds, aumento);
    }

    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong[] objetivosIds, int aumento)
    {
        Debug.Log($"Efecto Oliva_N aplicado a {objetivosIds.Length} ingredientes con aumento de rango de {aumento}");
    }

    // Implementación de LimpiarEfecto de IEffectManager
    public void LimpiarEfecto()
    {
        Debug.Log("OlivaNEffectManager: LimpiarEfecto llamado");

        // Detener la comprobación periódica
        CancelInvoke("ComprobarIngredientesEnRango");
        efectoActivo = false;

        if (IsServer)
        {
            // Eliminar el efecto de todos los ingredientes afectados
            foreach (ulong id in networkIds)
            {
                EliminarEfectoDeNetworkIdServerRpc(id);
            }

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

    private void OnDestroy()
    {
        // Asegurarse de que se limpie todo
        CancelInvoke("ComprobarIngredientesEnRango");

        // Solo limpiar efectos si estamos spawneados (para evitar llamadas duplicadas)
        if (IsSpawned)
        {
            LimpiarEfecto();
        }
    }
}