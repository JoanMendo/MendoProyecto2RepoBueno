using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

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
        // Localizar objeto objetivo
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objetivoId, out NetworkObject objetivoNetObj))
        {
            Debug.LogError($"TerneraEffectManager: No se encontró objeto con ID {objetivoId}");
            return;
        }

        // Aplicar modificador
        ModificadorRecurso mod = objetivoNetObj.gameObject.GetComponent<ModificadorRecurso>();
        if (mod == null)
            mod = objetivoNetObj.gameObject.AddComponent<ModificadorRecurso>();

        mod.AumentarVida(cantidad);
        mod.HacerInmovil();

        // Actualizar estado y notificar a clientes
        efectoAplicado.Value = true;
        AplicarEfectoClientRpc(objetivoId);
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
