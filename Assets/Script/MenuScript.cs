using UnityEngine;

public class MenuScript : MonoBehaviour
{
    public AudioClip sonido;

    public void ReproducirSonido()
    {
        AudioSource.PlayClipAtPoint(sonido, Camera.main.transform.position);
    }
}
