using Unity.Netcode;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Camera mainCamera; // Asigna la cámara principal en el Inspector
    public LayerMask InteractLayer; // Define en qué capas puede hacer colisión el Raycast

    public GameObject currentResource; // Referencia al objeto de recurso actual
    public float initialScale;
    public GameObject cursorPrefab;

    private InputManager inputManager;
    private Vector3 mousePosition;

    public AudioClip sonido;


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
        initialScale = transform.localScale.x; // Guarda la escala inicial del cursor

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
        mousePosition = new Vector3(newMousePosition.x, newMousePosition.y, 0);// Obtener la posición del mouse en la pantalla

        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z); // Asegúrate de que el Z es positivo para la proyección
        Vector3 cameraForward = mainCamera.transform.forward;

        // Convertir la posición del mouse a coordenadas del mundo
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition) + cameraForward*2;

        // Ajustar la posición del cursor en el mundo
        transform.position = worldMousePosition;
    }

    public void ResetPointerMesh()
    {

        if (cursorPrefab != null)
        {
            LocalGameManager.Instance.currentIngredient = null;

            gameObject.transform.localScale = new Vector3(initialScale, initialScale, initialScale); // Ajustar la escala del cursor
            gameObject.transform.rotation = Quaternion.Euler(27.7831211f, 79.9371033f, -4.82511587e-06f);
            gameObject.GetComponent<MeshFilter>().sharedMesh = cursorPrefab.GetComponent<MeshFilter>().sharedMesh; // Cambia la malla del cursor
            gameObject.GetComponent<MeshRenderer>().materials = cursorPrefab.GetComponent<MeshRenderer>().sharedMaterials;
        }

    }

    private void Interact()
    {
        if (currentResource != null)
        {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(mousePosition); // Crear un rayo desde la cámara a la posición del mouse
                                                                  // Realizar el raycast
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, InteractLayer))
            {
                GameObject hitObject = hit.collider.gameObject;

                NetworkObject netObj = hitObject.GetComponentInParent<NetworkObject>();

                AudioSource.PlayClipAtPoint(sonido, Camera.main.transform.position);


                if (netObj != null)
                {
                    // Aquí usamos HasAuthority
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
