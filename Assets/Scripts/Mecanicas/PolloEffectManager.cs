using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class PolloEffectManager : NetworkBehaviour, IEffectManager
{
    [SerializeField] private float velocidadRotacion = 30f;
    [SerializeField] private float duracionEfecto = 3f;
    private float tiempoRestante;
    private List<GameObject> ingredientesAfectados = new List<GameObject>();
    private bool efectoActivo = false;
    private IngredientesSO _ingredienteConfigurado;

    // Implementación de ConfigurarConIngrediente de IEffectManager
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
        Debug.Log($"PolloEffectManager configurado con: {ingrediente.name}");

        // Aquí podrías configurar velocidadRotacion o duracionEfecto si tuvieran
        // valores personalizables en el ScriptableObject Pollo
    }

    // Implementación de IniciarEfecto de IEffectManager
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Simplemente delegar al método específico que ya tenemos
        IniciarRotacion(nodoOrigen, nodosAfectados);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el efecto de rotación en los ingredientes afectados
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarRotacion(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Convertirse en objeto de red para poder usar RPC
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Limpiar la lista primero (por si se reutiliza el manager)
        ingredientesAfectados.Clear();

        // Recolectar ingredientes de los nodos afectados
        foreach (var nodo in nodosAfectados)
        {
            Node componenteNodo = nodo.GetComponent<Node>();
            if (componenteNodo != null && componenteNodo.hasIngredient.Value && componenteNodo.currentIngredient != null)
            {
                ingredientesAfectados.Add(componenteNodo.currentIngredient);
            }
        }

        // Si no hay ingredientes afectados, terminar
        if (ingredientesAfectados.Count == 0)
        {
            Debug.Log("No hay ingredientes afectados por el Pollo");
            FinalizarEfectoLogica();
            return;
        }

        // Iniciar efecto
        tiempoRestante = duracionEfecto;
        efectoActivo = true;

        // Notificar al servidor
        IniciarEfectoServerRpc();

        Debug.Log($"Efecto Pollo iniciado en {ingredientesAfectados.Count} ingredientes");
    }

    [ServerRpc(RequireOwnership = false)]
    private void IniciarEfectoServerRpc()
    {
        // Notificar a todos los clientes
        IniciarEfectoClientRpc();
    }

    [ClientRpc]
    private void IniciarEfectoClientRpc()
    {
        // Cualquier efecto visual adicional que desees aplicar
    }

    private void Update()
    {
        if (efectoActivo && tiempoRestante > 0)
        {
            tiempoRestante -= Time.deltaTime;

            // Rotar los ingredientes afectados
            foreach (var ingrediente in ingredientesAfectados)
            {
                if (ingrediente != null)
                {
                    ingrediente.transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime);
                }
            }

            if (tiempoRestante <= 0)
            {
                FinalizarEfectoLogica();
            }
        }
    }

    // Implementación de LimpiarEfecto de IEffectManager
    public void LimpiarEfecto()
    {
        Debug.Log("PolloEffectManager: LimpiarEfecto llamado");
        FinalizarEfectoLogica();
    }

    // Renombrado de FinalizarEfecto a FinalizarEfectoLogica para evitar confusión
    private void FinalizarEfectoLogica()
    {
        if (!efectoActivo) return;

        efectoActivo = false;

        // Notificar al servidor
        if (IsSpawned)
        {
            FinalizarEfectoServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void FinalizarEfectoServerRpc()
    {
        // Notificar a todos los clientes
        FinalizarEfectoClientRpc();

        // Destruir después de un pequeño retraso
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    [ClientRpc]
    private void FinalizarEfectoClientRpc()
    {
        Debug.Log("Efecto Pollo finalizado");
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
