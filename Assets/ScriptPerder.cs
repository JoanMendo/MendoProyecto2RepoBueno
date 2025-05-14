using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class GameOverHandler : MonoBehaviour
{
    // Jugador 1
    public GameObject objeto1_J1; // Guante
    public GameObject objeto2_J1; // Tortita
    public float velocidadGuante = 5f;
    public float velocidadTortita = 6.2f;

    // Jugador 2
    public GameObject objeto1_J2; // Otro objeto
    public GameObject objeto2_J2; // Otro objeto
    public float velocidadObjeto1_J2 = 5f;
    public float velocidadObjeto2_J2 = 6.2f;

    public void Start ()
    {
        Jugador1Pierde (); 

        //Jugador2Pierde ();
    }
    public void Jugador1Pierde ()
    {
        StartCoroutine (MoverObjetosJugador1Pierde ());
    }

    public void Jugador2Pierde ()
    {
        StartCoroutine (MoverObjetosJugador2Pierde ());
    }

    private IEnumerator MoverObjetosJugador1Pierde ()
    {
        Vector3 posicion1 = new Vector3 (-6.34f, 1.94f, -17.38218f);
        yield return StartCoroutine (MoverSuavemente (objeto1_J1, posicion1, velocidadGuante));

        Vector3 posicionFinal = new Vector3 (13.4f, -8.4f, -17.38218f);
        StartCoroutine (MoverSuavemente (objeto1_J1, posicionFinal, velocidadGuante));
        yield return StartCoroutine (MoverSuavementeConDesaparicion (objeto2_J1, posicionFinal, velocidadTortita));
    }

    private IEnumerator MoverObjetosJugador2Pierde ()
    {
        Vector3 posicion1 = new Vector3 (5.14f, 1.65f, -17.38218f);
        yield return StartCoroutine (MoverSuavemente (objeto1_J2, posicion1, velocidadObjeto1_J2));

        Vector3 posicionFinal = new Vector3 (13.4f, -8.4f, -17.38218f);
        StartCoroutine (MoverSuavemente (objeto1_J2, posicionFinal, velocidadObjeto1_J2));
        yield return StartCoroutine (MoverSuavementeConDesaparicion (objeto2_J2, posicionFinal, velocidadObjeto2_J2));
    }

    private IEnumerator MoverSuavemente (GameObject objeto, Vector3 destino, float velocidad)
    {
        while (Vector3.Distance (objeto.transform.position, destino) > 0.01f)
        {
            objeto.transform.position = Vector3.MoveTowards (
                objeto.transform.position,
                destino,
                velocidad * Time.deltaTime
            );
            yield return null;
        }

        objeto.transform.position = destino;
    }

    private IEnumerator MoverSuavementeConDesaparicion (GameObject objeto, Vector3 destino, float velocidad)
    {
        yield return StartCoroutine (MoverSuavemente (objeto, destino, velocidad));
        objeto.SetActive (false); // Desaparece al llegar
    }
}
