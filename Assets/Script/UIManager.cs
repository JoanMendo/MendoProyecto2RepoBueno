using UnityEngine;


public class UIManager : MonoBehaviour
{
    public PortalManager portalManager;

    public void MostrarItems ()
    {
        portalManager.AparecerDesdePortal ();     // hace caer los nuevos objetos
        portalManager.DesaparecerEnPortal ();     // hace desaparecer los viejos (si los hay)
    }

    public void MostrarComida ()
    {
        portalManager.AparecerDesdePortal ();
        portalManager.DesaparecerEnPortal ();
    }
}
