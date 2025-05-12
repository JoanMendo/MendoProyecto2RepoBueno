using UnityEngine;
using TMPro;

public class PingPongTextFade : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    // Duración de un ciclo completo (de gris a negro y de negro a gris)
    public float cycleDuration = 2f;

    void Update ()
    {
        if (textMesh != null)
        {
            // Mathf.PingPong devuelve un valor que oscila entre 0 y 1
            float t = Mathf.PingPong (Time.time / cycleDuration, 1f);
            textMesh.color = Color.Lerp (Color.gray, Color.black, t);
        }
        else
        {
            Debug.LogError ("No se ha asignado el TextMeshProUGUI.");
        }
    }
}
