using Unity.Netcode;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Camera mainCamera; // Asigna la c�mara principal en el Inspector
    public LayerMask InteractLayer; // Define en qu� capas puede hacer colisi�n el Raycast
    private InputManager inputManager;
    private Vector3 mousePosition;
    public GameObject currentResource; // Referencia al objeto de recurso actual

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Asigna la c�mara principal si no se ha asignado
        }
        inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager == null)
        {
            Debug.LogError("No se encontr� el InputManager en la escena.");
        }
      
    }


    public void OnEnable()
    {
        InputManager.OnClicked += Interact;
        InputManager.OnMouseMoved += UpdateMousePosition;
    }
    public void OnDisable()
    {
        InputManager.OnClicked -= Interact;
        InputManager.OnMouseMoved -= UpdateMousePosition;
    }

    public void UpdateMousePosition(Vector2 newMousePosition)
    {
        mousePosition = new Vector3(newMousePosition.x, newMousePosition.y, 0);// Obtener la posici�n del mouse en la pantalla

        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z); // Aseg�rate de que el Z es positivo para la proyecci�n

        // Convertir la posici�n del mouse a coordenadas del mundo
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition);

        // Ajustar la posici�n del cursor en el mundo
        transform.position = worldMousePosition;
    }

    private void Interact()
    {
        if (currentResource != null)
        {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(mousePosition); // Crear un rayo desde la c�mara a la posici�n del mouse
                                                                  // Realizar el raycast
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, InteractLayer))
            {
                GameObject hitObject = hit.collider.gameObject;

                NetworkObject netObj = hitObject.GetComponentInParent<NetworkObject>();

                if (netObj != null)
                {
                    // Aqu� usamos HasAuthority
                    if (netObj.HasAuthority)
                    {
                        if (hitObject.TryGetComponent<IInteractuable>(out IInteractuable script))
                        {
                            script.Interactuar();
                        }
                    }
                    else
                    {
                        Debug.Log("No tienes autoridad sobre este objeto.");
                    }
                }
            }
        }
    }
}
