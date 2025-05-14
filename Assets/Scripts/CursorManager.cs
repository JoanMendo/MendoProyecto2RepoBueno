using Unity.Netcode;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Camera mainCamera; // Asigna la cámara principal en el Inspector
    public LayerMask InteractLayer; // Define en qué capas puede hacer colisión el Raycast
    private InputManager inputManager;
    private Vector3 mousePosition;
    public GameObject currentResource; // Referencia al objeto de recurso actual

    [Header("Apariencia del Cursor")]
    private MeshRenderer meshRenderer;
    private Material originalMaterial;
    public Material defaultCursorMaterial; // Material por defecto del cursor

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Asigna la cámara principal si no se ha asignado
        }

        inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager == null)
        {
            Debug.LogError("No se encontró el InputManager en la escena.");
        }

        // Obtener el MeshRenderer
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogWarning("CursorManager no tiene componente MeshRenderer");
        }
        else
        {
            // Guardar el material original
            originalMaterial = meshRenderer.material;
        }
    }

    public void OnEnable()
    {
        Debug.Log($"[CursorManager] OnEnable - Suscribiendo a eventos. InputManager: {inputManager != null}");

        if (inputManager == null)
        {
            inputManager = FindFirstObjectByType<InputManager>();
            Debug.Log($"[CursorManager] Buscando InputManager: {inputManager != null}");
        }

        if (inputManager != null)
        {
            InputManager.OnClicked += Interact;
            InputManager.OnMouseMoved += UpdateMousePosition;
            Debug.Log("[CursorManager] Suscripción a eventos completada");
        }
        else
        {
            Debug.LogError("[CursorManager] ¡ERROR! No se pudo encontrar InputManager");
        }
    }

    public void OnDisable()
    {
        Debug.Log("[CursorManager] OnDisable - Desuscribiendo de eventos");
        InputManager.OnClicked -= Interact;
        InputManager.OnMouseMoved -= UpdateMousePosition;
    }

    public void UpdateMousePosition(Vector2 newMousePosition)
    {
        mousePosition = new Vector3(newMousePosition.x, newMousePosition.y, 0);// Obtener la posición del mouse en la pantalla

        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z); // Asegúrate de que el Z es positivo para la proyección

        // Convertir la posición del mouse a coordenadas del mundo
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition);

        // Ajustar la posición del cursor en el mundo
        transform.position = worldMousePosition;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Restaura el material original del cursor
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void RestoreOriginalMaterial()
    {
        if (meshRenderer == null) return;

        if (originalMaterial != null)
        {
            meshRenderer.material = originalMaterial;
        }
        else if (defaultCursorMaterial != null)
        {
            meshRenderer.material = defaultCursorMaterial;
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Cambia el material del cursor
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void SetCursorMaterial(Material newMaterial)
    {
        if (meshRenderer == null || newMaterial == null) return;

        meshRenderer.material = newMaterial;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Guarda el material actual como original
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void SaveCurrentAsOriginal()
    {
        if (meshRenderer == null) return;

        originalMaterial = meshRenderer.material;
    }

    private void Interact()
    {
        Debug.Log("[CursorManager] Método Interact llamado");
        // NUEVA VERIFICACIÓN: Comprobar si estamos en fase de ejecución de acciones
        if (TurnManager.Instance != null && TurnManager.Instance.GetFaseActual() == FaseTurno.EjecucionAcciones)
        {
            Debug.Log("[CursorManager] Ignorando interacción durante fase de ejecución de acciones");
            return; // No procesamos interacciones durante esta fase
        }

        if (currentResource == null)
        {
            Debug.LogWarning("No hay recurso seleccionado. El raycast no se ejecutará.");
            return;
        }

        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        // DIAGNÓSTICO 1: Visualizar el rayo
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 5f);
        Debug.Log($"Lanzando raycast desde {ray.origin} en dirección {ray.direction}");

        // DIAGNÓSTICO 2: Verificar la capa (layer mask)
        Debug.Log($"Usando layer mask: {InteractLayer.value} ({LayerMaskToString(InteractLayer)})");

        // DIAGNÓSTICO 3: Raycast contra todas las capas para comparar
        bool hitAnything = Physics.Raycast(ray, out RaycastHit generalHit);
        if (hitAnything)
        {
            Debug.Log($"Raycast sin filtro golpeó: {generalHit.collider.gameObject.name} en capa {LayerMaskToString(1 << generalHit.collider.gameObject.layer)}");
        }
        else
        {
            Debug.LogWarning("Raycast sin filtro no golpeó nada. Esto es muy extraño.");
        }

        // Raycast original con la capa específica
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, InteractLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log($"¡ÉXITO! Raycast golpeó: {hitObject.name} en posición {hit.point}");

            NetworkObject netObj = hitObject.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                Debug.Log($"NetworkObject encontrado: HasAuthority={netObj.HasAuthority}, OwnerClientId={netObj.OwnerClientId}");

                if (netObj.HasAuthority)
                {
                    if (hitObject.TryGetComponent<IInteractuable>(out IInteractuable script))
                    {
                        Debug.Log($"Ejecutando Interactuar() en {hitObject.name}");
                        script.Interactuar();
                    }
                    else
                    {
                        Debug.LogWarning($"El objeto {hitObject.name} no implementa IInteractuable");
                    }
                }
                else
                {
                    Debug.Log($"No tienes autoridad sobre este objeto. Client={NetworkManager.Singleton.LocalClientId}, Owner={netObj.OwnerClientId}");
                }
            }
            else
            {
                Debug.LogWarning($"El objeto {hitObject.name} no tiene NetworkObject");
            }
        }
        else
        {
            Debug.Log("Raycast con capa específica no golpeó nada. Es probable que los nodos no estén en la capa correcta.");
        }
    }

    // Método auxiliar para mostrar nombres de capas
    private string LayerMaskToString(LayerMask mask)
    {
        var layers = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & mask) != 0)
            {
                layers.Add(LayerMask.LayerToName(i));
            }
        }
        return string.Join(", ", layers);
    }
}