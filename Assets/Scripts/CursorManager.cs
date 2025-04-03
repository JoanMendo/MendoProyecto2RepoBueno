using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Camera mainCamera; // Asigna la c�mara principal en el Inspector
    public LayerMask raycastLayer; // Define en qu� capas puede hacer colisi�n el Raycast

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Asigna la c�mara principal si no se ha asignado
        }
    }

    void Update()
    {
        // Obtener la posici�n del mouse en la pantalla
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z); // Aseg�rate de que el Z es positivo para la proyecci�n

        // Convertir la posici�n del mouse a coordenadas del mundo
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition);

        // Ajustar la posici�n del cursor en el mundo
        transform.position = worldMousePosition;
    }
}
