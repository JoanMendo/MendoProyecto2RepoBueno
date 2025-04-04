using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Camera mainCamera; // Asigna la c�mara principal en el Inspector
    public LayerMask raycastLayer; // Define en qu� capas puede hacer colisi�n el Raycast
    private InputManager inputManager;

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

    void Update()
    {
        Set3DMousePosition();
    }

    public void Set3DMousePosition()
    {
        // Obtener la posici�n del mouse en la pantalla
        Vector3 mousePosition = inputManager.MousePosition;
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z); // Aseg�rate de que el Z es positivo para la proyecci�n

        // Convertir la posici�n del mouse a coordenadas del mundo
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition);

        // Ajustar la posici�n del cursor en el mundo
        transform.position = worldMousePosition;
    }


}
