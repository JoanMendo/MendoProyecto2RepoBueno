using UnityEngine;
using UnityEngine.UI;

public class LoadingBar : MonoBehaviour
{
    // Asigna la UI Image configurada con Fill Method = Horizontal en el Inspector
    public Image loadingBar;
    // Duración total en segundos para llenar la barra
    public float duration = 20f;
    // Velocidad de llenado calculada a partir de la duración
    private float loadSpeed;

    void Start ()
    {
        // Calculamos la velocidad para que en "duration" segundos, fillAmount pase de 0 a 1
        loadSpeed = 1f / duration;
    }

    void Update ()
    {
        if (loadingBar != null)
        {
            // Incrementa el fillAmount de la imagen hasta llegar a 1 (100% cargado)
            if (loadingBar.fillAmount < 1f)
            {
                loadingBar.fillAmount += loadSpeed * Time.deltaTime;
            }
        }
        else
        {
            Debug.LogError ("No se ha asignado la UI Image de la barra de carga.");
        }
    }
}
