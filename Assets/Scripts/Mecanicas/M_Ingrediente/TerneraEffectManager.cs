using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TerneraEffectManager : NetworkBehaviour, IEffectManager
{
    [SerializeField] private float aumentoVida = 1f;
    private GameObject ingredienteObjetivo;
    private NetworkVariable<bool> efectoAplicado = new NetworkVariable<bool>(false);
    private IngredientesSO _ingredienteConfigurado;

    // Implementación de ConfigurarConIngrediente de IEffectManager
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
        Debug.Log($"TerneraEffectManager configurado con: {ingrediente.name}");

        // Si el ingrediente es Ternera, podemos obtener el aumento de vida directamente
        if (ingrediente is Ternera terneraIngrediente)
        {
            aumentoVida = terneraIngrediente.aumentoVida;
        }
    }

    // Implementación de IniciarEfecto de IEffectManager
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Verificar si hay al menos un nodo afectado
        if (nodosAfectados == null || nodosAfectados.Count == 0)
        {
            Debug.LogWarning("TerneraEffectManager: No hay nodos afectados para aplicar el efecto");
            return;
        }

        // La ternera solo afecta al primer nodo vecino
        GameObject primerVecino = nodosAfectados[0];

        // Obtener el aumento de vida del ingrediente configurado, o usar valor por defecto
        float cantidadVida = aumentoVida;

        // Si tenemos la referencia al ingrediente específico, usar su valor
        if (_ingredienteConfigurado is Ternera terneraIngrediente)
        {
            cantidadVida = terneraIngrediente.aumentoVida;
        }

        // Delegar al método específico
        IniciarEfectoTernera(nodoOrigen, primerVecino, cantidadVida);
    }

    public void IniciarEfectoTernera(GameObject nodoOrigen, GameObject nodoObjetivo, float cantidadVida)
    {
        // 1. Inicializar valores
        aumentoVida = cantidadVida;

        // 2. Obtener referencia al ingrediente objetivo
        Node nodoVecinoComp = nodoObjetivo.GetComponent<Node>();
        if (nodoVecinoComp == null || !nodoVecinoComp.hasIngredient.Value)
        {
            Debug.LogWarning("TerneraEffectManager: El nodo objetivo no tiene un ingrediente");
            return;
        }

        ingredienteObjetivo = nodoVecinoComp.currentIngredient;

        // 3. Verificar que tenemos un NetworkObject y hacer Spawning
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }
        else if (GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("TerneraEffectManager: Falta componente NetworkObject");
            return;
        }

        // 4. Verificar que el ingrediente objetivo tiene NetworkObject
        NetworkObject objetivoNetObj = ingredienteObjetivo.GetComponent<NetworkObject>();
        if (objetivoNetObj == null || !objetivoNetObj.IsSpawned)
        {
            Debug.LogError("TerneraEffectManager: El ingrediente objetivo no tiene NetworkObject válido");
            return;
        }

        // 5. Aplicar el efecto en el servidor
        AplicarEfectoServerRpc(
            objetivoNetObj.NetworkObjectId,
            aumentoVida
        );

        Debug.Log($"Efecto de Ternera iniciado con aumento de vida: {aumentoVida}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong objetivoId, float cantidad)
    {
        // Preparar cliente primero
        PrepararObjetivoClientRpc(objetivoId);

        // Localizar objeto objetivo
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objetivoId, out NetworkObject objetivoNetObj))
        {
            Debug.LogError($"TerneraEffectManager: No se encontró objeto con ID {objetivoId}");
            return;
        }

        // Usar coroutine para dar tiempo a la sincronización
        StartCoroutine(AplicarEfectoConDelay(objetivoNetObj.gameObject, cantidad, objetivoId));
    }

    private IEnumerator AplicarEfectoConDelay(GameObject objetivo, float cantidad, ulong id)
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

        // Modificar directamente las variables de red
        float vidaAntes = mod.GetVidaActual();
        mod.modificacionVida.Value += cantidad;
        float vidaDespues = mod.GetVidaActual();

        bool movibleAntes = mod.EsMovible();
        mod.esMovible.Value = false;

        Debug.Log($"Ternera: Aplicado efecto a {objetivo.name}. " +
                  $"Vida: {vidaAntes} -> {vidaDespues} (+{cantidad}), " +
                  $"Movible: {movibleAntes} -> {mod.EsMovible()}");

        // Actualizar estado y notificar a clientes
        efectoAplicado.Value = true;
        AplicarEfectoASingleObjetivoClientRpc(id);
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

        Debug.Log($"[CLIENTE] Preparado objeto {objetivoNetObj.name} para recibir efecto Ternera");
    }

    [ClientRpc]
    private void AplicarEfectoASingleObjetivoClientRpc(ulong id)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject objetivoNetObj))
            return;

        Debug.Log($"[CLIENTE] Efecto Ternera aplicado a {objetivoNetObj.name}");
    }

    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong objetivoId)
    {
        Debug.Log($"Efecto de Ternera aplicado al objeto {objetivoId}");
    }

    // Implementación de LimpiarEfecto de IEffectManager
    public void LimpiarEfecto()
    {
        Debug.Log("TerneraEffectManager: LimpiarEfecto llamado");
        if (IsSpawned)
        {
            LimpiarEfectoServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LimpiarEfectoServerRpc()
    {
        // Aquí podrías revertir los efectos si es necesario
        // Por ejemplo, quitar la inmovilidad, aunque esto podría no tener sentido
        // para el aumento de vida que ya se aplicó

        // Notificar a los clientes y eliminar el objeto
        LimpiarEfectoClientRpc();

        // Auto-destrucción después de un pequeño retraso
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        Debug.Log("Limpiando efecto Ternera");
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