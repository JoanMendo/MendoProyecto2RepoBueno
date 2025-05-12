using UnityEngine;
using UnityEngine.UI;

public class BotonSonido : MonoBehaviour
{
    public AudioClip sonido;
    private AudioSource audioSource;

    void Start ()
    {
        // Añade un componente AudioSource si no existe
        audioSource = gameObject.AddComponent<AudioSource> ();
        audioSource.clip = sonido;
    }

    public void ReproducirSonido ()
    {
        Debug.Log ("Sonido OK");
        audioSource.Play ();
    }
}
