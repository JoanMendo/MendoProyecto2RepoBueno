using UnityEngine;
using System.Collections;

public class PortalManager : MonoBehaviour
{
    public GameObject vegetable;
    public GameObject items;
    public float stopY = -30f; // Altura a la que se detiene la caída

    public ParticleSystem EnterPortal;
    public ParticleSystem ExitPortal;

    private bool puedeDesaparecer = true;
    private bool puedoAparecer = true; 

    public void Start ()
    {
        EnterPortal.Stop ();
        ExitPortal.Stop ();
    }

    public void AparecerDesdePortal ()
    {
        if (!puedoAparecer || items.activeSelf) return;

        puedoAparecer = false;
        StartCoroutine (ReactivarAparicionItems (6f));  
        ExitPortal.Play ();
        StartCoroutine (DetenerPortalDespuesDeTiempo3 (ExitPortal, 2f)); // Cambiado a 2f para que aparezca más rápido
        
        EnterPortal.Play ();
        StartCoroutine (AparecerConRetrasoItems ()); // Cambiado a 2f para que aparezca más rápido
        StartCoroutine (DetenerPortalDespuesDeTiempo (EnterPortal, 4f)); 
    }

    private IEnumerator DetenerPortalDespuesDeTiempo3 (ParticleSystem portal, float segundos)
    {
        yield return new WaitForSeconds (segundos);
        portal.Stop ();
        vegetable.SetActive (false);
    }

    private IEnumerator AparecerConRetrasoItems ()
    {
        yield return new WaitForSeconds (2f);

        vegetable.SetActive (false);
        items.SetActive (true);
        items.transform.position = new Vector3 (-16.46345f, -22.1f, -6.2f);

        Rigidbody rb = items.GetComponent<Rigidbody> ();
        if (rb != null)
        {
            rb.useGravity = true;
            StartCoroutine (DesactivarGravedadAlLlegar (rb));
        }
    }

    private IEnumerator ReactivarAparicionItems (float delay)
    {
        yield return new WaitForSeconds (delay);
        puedoAparecer = true;
    }

 

    public void DesaparecerEnPortal ()
    {
        if (!puedeDesaparecer || vegetable.activeSelf) return;

        puedeDesaparecer = false;
        StartCoroutine (ReactivarDesaparicion (6f));
        ExitPortal.Play ();
        StartCoroutine (DetenerPortalDespuesDeTiempo2 (ExitPortal, 2f));
        
        EnterPortal.Play ();
        StartCoroutine (AparecerConRetraso (2f));
        StartCoroutine (DetenerPortalDespuesDeTiempo (EnterPortal, 4f));
    }

    private IEnumerator DetenerPortalDespuesDeTiempo2 (ParticleSystem portal, float segundos)
    {
        yield return new WaitForSeconds (segundos);
        portal.Stop ();
        items.SetActive (false);
    }




    private IEnumerator AparecerConRetraso (float segundos)
    {
        yield return new WaitForSeconds (segundos); 

        vegetable.SetActive (true);
        vegetable.transform.position = new Vector3 (-16.46345f, -22.1f, -23.1f);
        Rigidbody rb = vegetable.GetComponent<Rigidbody> ();
        rb.useGravity = true;
        StartCoroutine (DesactivarGravedadAlLlegar (rb));
    }

    private IEnumerator DesactivarGravedadAlLlegar (Rigidbody rb)
    {
        while (rb.transform.position.y > stopY)
        {
            yield return null;
        }

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero; // Corrección: era "linearVelocity" que no existe
    }

    private IEnumerator DetenerPortalDespuesDeTiempo (ParticleSystem portal, float segundos)
    {
        yield return new WaitForSeconds (segundos);
        portal.Stop ();
    }

    private IEnumerator ReactivarDesaparicion (float delay)
    {
        yield return new WaitForSeconds (delay);
        puedeDesaparecer = true;
    }
}
