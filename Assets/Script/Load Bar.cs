using UnityEngine;
using UnityEngine.UI;

public class LoadingBar : MonoBehaviour
{
    public Image loadingBar;
    public float duration = 20f;
    private float loadSpeed;

    void Start ()
    {
        loadSpeed = 1f / duration;
    }

    void Update ()
    {
        if (loadingBar != null)
        {
            if (loadingBar.fillAmount < 1f)
            {
                loadingBar.fillAmount += loadSpeed * Time.deltaTime;

                // Cambiar de verde a rojo
                float t = loadingBar.fillAmount; // Va de 0 a 1
                Color color = Color.Lerp (Color.green, Color.red, t);
                loadingBar.color = color;
            }
        }
        else
        {
            Debug.LogError ("No se ha asignado la UI Image de la barra de carga.");
        }
    }
}
