using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Camera mainCamera; // Asigna la cámara principal en el Inspector
    public LayerMask raycastLayer; // Define en qué capas puede hacer colisión el Raycast

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Asigna la cámara principal si no se ha asignado
        }
    }

    void Update()
    {
        // Obtener la posición del mouse en la pantalla
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z); // Asegúrate de que el Z es positivo para la proyección

        // Convertir la posición del mouse a coordenadas del mundo
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition);

        // Ajustar la posición del cursor en el mundo
        transform.position = worldMousePosition;
    }
}
