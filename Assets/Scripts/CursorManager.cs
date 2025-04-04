using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Camera mainCamera; // Asigna la cámara principal en el Inspector
    public LayerMask raycastLayer; // Define en qué capas puede hacer colisión el Raycast
    private InputManager inputManager;

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

    void Update()
    {
        Set3DMousePosition();
    }

    public void Set3DMousePosition()
    {
        // Obtener la posición del mouse en la pantalla
        Vector3 mousePosition = inputManager.MousePosition;
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z); // Asegúrate de que el Z es positivo para la proyección

        // Convertir la posición del mouse a coordenadas del mundo
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition);

        // Ajustar la posición del cursor en el mundo
        transform.position = worldMousePosition;
    }


}
