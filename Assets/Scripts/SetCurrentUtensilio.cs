using UnityEngine;

public class SetCurrentUtensilio : MonoBehaviour, IInteractuable
{
    public string nombreUtensilio;
    private Utensilio datosUtensilio;

    private void Awake()
    {
        // Intentar cargar los datos del utensilio
        if (UtensiliosManager.Instance != null)
        {
            datosUtensilio = UtensiliosManager.Instance.GetUtensilioPorNombre(nombreUtensilio);

            if (datosUtensilio == null)
            {
                Debug.LogWarning($"No se encontraron datos para el utensilio: {nombreUtensilio}");
            }
        }
    }

    public void Interactuar()
    {
        // Asegurar que UtensiliosManager existe
        if (UtensiliosManager.Instance == null)
        {
            Debug.LogError("UtensiliosManager no encontrado");
            return;
        }

        // Si los datos no se cargaron en Awake, intentar nuevamente
        if (datosUtensilio == null)
        {
            datosUtensilio = UtensiliosManager.Instance.GetUtensilioPorNombre(nombreUtensilio);
        }

        if (datosUtensilio != null)
        {
            LocalGameManager.Instance.SeleccionarUtensilio(datosUtensilio);
        }
        else
        {
            Debug.LogError($"No se pudieron cargar datos del utensilio: {nombreUtensilio}");
        }
    }
}