using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Camera mainCamera; // Asigna la cámara principal en el Inspector
    public LayerMask raycastLayer; // Define en qué capas puede hacer colisión el Raycast
    public LayerMask TilesLayer; // Define en qué capas puede hacer colisión el Raycast
    private InputManager inputManager;
    private Vector3 mousePosition;
    public GameObject currentResource; // Referencia al objeto de recurso actual

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
      
    }


    public void OnEnable()
    {
        InputManager.OnClicked += TrySetIngredient;
        InputManager.OnMouseMoved += UpdateMousePosition;
    }
    public void OnDisable()
    {
        InputManager.OnClicked -= TrySetIngredient;
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

    private void TrySetIngredient()
    {
        if (currentResource != null)
        {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(mousePosition); // Crear un rayo desde la cámara a la posición del mouse
            // Realizar el raycast
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, TilesLayer))
            {
                // Verificar si el rayo colisiona con un objeto en la capa especificada

                GameObject hitNode = hit.collider.gameObject;

                if (hitNode.TryGetComponent<Node>(out Node node))
                {
                    if (node.hasIngredient)
                    {
                        Debug.Log("El nodo ya tiene un ingrediente.");
                        return; // Si el nodo ya tiene un ingrediente, no hacer nada
                    }
                    node.SetIngredient(currentResource);
                }
            }


        }
    }


}
