using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public class Node : NetworkBehaviour, IInteractuable
{
    [Header("Posición")]
    public Vector2 position;

    [Header("Estado")]
    public NetworkVariable<bool> hasIngredient = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hasUtensilio = new NetworkVariable<bool>(false);

    [Header("Referencias")]
    public GameObject currentIngredient;
    public GameObject currentUtensilio;
    public List<GameObject> vecinos = new List<GameObject>();

    [Header("Componentes")]
    public MeshRenderer meshRenderer;
    public BoxCollider nodeCollider;

    // Referencia a NodeMap padre
    private NodeMap nodeMap;

    // Materiales para estados visuales
    private Material materialNormal;
    private Material materialResaltado;

    // Propiedades para modificaciones
    private NetworkVariable<int> modificacionRango = new NetworkVariable<int>(0);
    private NetworkVariable<float> modificacionVida = new NetworkVariable<float>(0);
    private NetworkVariable<bool> esMovible = new NetworkVariable<bool>(true);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicializa el nodo con referencias necesarias
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void Initialize(NodeMap map)
    {
        nodeMap = map;

        // Obtener componentes si no están asignados
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (nodeCollider == null)
            nodeCollider = GetComponent<BoxCollider>();

        if (meshRenderer != null)
            materialNormal = meshRenderer.material;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Configura el material cuando el nodo está resaltado
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void SetHighlightMaterial(Material material)
    {
        materialResaltado = material;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Resalta visualmente el nodo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void Highlight(bool isHighlighted)
    {
        if (meshRenderer == null) return;

        if (isHighlighted && materialResaltado != null)
        {
            meshRenderer.material = materialResaltado;
        }
        else
        {
            meshRenderer.material = materialNormal;
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Método para interacción (implementa IInteractuable)
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void Interactuar()
    {
        // Implementar según las reglas del juego
        // Por ejemplo, seleccionar el nodo para colocar ingredientes
        SeleccionarNodoServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SeleccionarNodoServerRpc()
    {
        // Lógica de servidor para selección
        Debug.Log($"Nodo {name} seleccionado en posición {position}");
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Limpia cualquier ingrediente en este nodo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ClearNodeIngredient()
    {
        if (!IsServer) return;

        hasIngredient.Value = false;

        if (currentIngredient != null)
        {
            // Despawnear ingrediente actual si es objeto de red
            NetworkObject netObj = currentIngredient.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            currentIngredient = null;
        }

        // Notificar a clientes
        LimpiarIngredienteClientRpc();
    }

    [ClientRpc]
    private void LimpiarIngredienteClientRpc()
    {
        if (IsServer) return; // El servidor ya lo hizo

        // Destruir localmente si existe
        if (currentIngredient != null)
        {
            Destroy(currentIngredient);
            currentIngredient = null;
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Coloca un ingrediente en este nodo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void SetNodeIngredient(GameObject prefabIngrediente)
    {
        if (!IsServer) return;

        // Si ya hay ingrediente, limpiarlo primero
        if (hasIngredient.Value)
        {
            ClearNodeIngredient();
        }

        // Instanciar nuevo ingrediente
        GameObject nuevoIngrediente = Instantiate(prefabIngrediente, transform.position, Quaternion.identity);
        NetworkObject netObj = nuevoIngrediente.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();
            currentIngredient = nuevoIngrediente;
            hasIngredient.Value = true;

            // Activar efecto si aplica
            ActivarEfectoIngrediente();
        }
        else
        {
            Debug.LogError("Error: El prefab de ingrediente debe tener componente NetworkObject");
            Destroy(nuevoIngrediente);
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Activa el efecto del ingrediente colocado
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void ActivarEfectoIngrediente()
    {
        if (!IsServer || currentIngredient == null) return;

        ResourcesSO recurso = currentIngredient.GetComponent<ResourcesSO>();
        if (recurso != null && nodeMap != null)
        {
            // Activar efecto pasando el NodeMap
            recurso.ActivarEfecto(gameObject, nodeMap);
        }
    }


    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Aplica un efecto visual de utensilio sobre este nodo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void SetUtensilioVisual(ResourcesSO utensilio)
    {
        if (!IsServer) return;

        // Limpiar utensilio actual si existe
        if (currentUtensilio != null)
        {
            NetworkObject netObj = currentUtensilio.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            currentUtensilio = null;
            hasUtensilio.Value = false;
        }

        // Si tenemos un nuevo utensilio, mostrarlo
        if (utensilio != null && utensilio.prefab3D != null)
        {
            GameObject visual = Instantiate(utensilio.prefab3D, transform.position, Quaternion.identity);
            NetworkObject netObj = visual.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                netObj.Spawn();
                currentUtensilio = visual;
                hasUtensilio.Value = true;
            }
            else
            {
                Debug.LogError("Error: El prefab de utensilio debe tener componente NetworkObject");
                Destroy(visual);
            }
        }

        // Notificar a clientes
        ActualizarUtensilioClientRpc();
    }

    [ClientRpc]
    private void ActualizarUtensilioClientRpc()
    {
        // Actualizar estado visual si es necesario
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Modifica el rango de efectos en este nodo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ModificarRango(int cantidad)
    {
        if (!IsServer) return;
        modificacionRango.Value += cantidad;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Modifica la vida del ingrediente en este nodo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ModificarVida(float cantidad)
    {
        if (!IsServer) return;
        modificacionVida.Value += cantidad;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Establece si el ingrediente en este nodo puede moverse
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void SetMovible(bool movible)
    {
        if (!IsServer) return;
        esMovible.Value = movible;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene el rango actual incluyendo modificadores
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public int GetRangoActual()
    {
        if (currentIngredient == null) return 0;

        ResourcesSO recurso = currentIngredient.GetComponent<ResourcesSO>();
        if (recurso == null) return 0;

        return recurso.range + modificacionRango.Value;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene la vida actual incluyendo modificadores
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public float GetVidaActual()
    {
        if (currentIngredient == null) return 0;

        ResourcesSO recurso = currentIngredient.GetComponent<ResourcesSO>();
        if (recurso == null) return 0;

        return recurso.vida + modificacionVida.Value;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Comprueba si el ingrediente en este nodo puede moverse
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public bool PuedeMoverse()
    {
        if (currentIngredient == null) return false;

        ResourcesSO recurso = currentIngredient.GetComponent<ResourcesSO>();
        if (recurso == null) return false;

        return recurso.esmovible && esMovible.Value;
    }
}