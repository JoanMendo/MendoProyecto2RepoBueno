using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using Unity.Netcode;

public enum FaseTurno
{
    ColocacionIngredientes,
    DespliegueUtensiliosEfectos,
    EjecucionAcciones,
    FinTurno
}
public class TurnManager : NetworkBehaviour
{
    public int turnoActual = 1;
    public int maxTurnos = 10;

    public FaseTurno faseActual = FaseTurno.ColocacionIngredientes;

    public ComprobadorVictoria victoria;

    private ReadyButton[] readyButtons;
    void Start()
    {
        readyButtons = Object.FindObjectsByType<ReadyButton>(FindObjectsSortMode.None);
        IniciarFase(FaseTurno.ColocacionIngredientes);
    }


    public void IniciarFase(FaseTurno nuevaFase)
    {

        faseActual = nuevaFase;

        switch (faseActual)
        {
            case FaseTurno.ColocacionIngredientes:
                LocalGameManager.Instance.ingredientCount = 0;
                break;
            case FaseTurno.DespliegueUtensiliosEfectos:
                LocalGameManager.Instance.utensilEffectsCount = 0;
                break;
            case FaseTurno.EjecucionAcciones:
                

                PasarFaseSiguiente();
                break;
            case FaseTurno.FinTurno:
                turnoActual++;
                if (turnoActual > maxTurnos)
                {
;

                }
                else
                {
                    IniciarFase(FaseTurno.ColocacionIngredientes);
                }
                break;
        }
    }

    public void PasarFaseSiguiente()
    {
       
        switch (faseActual)
        {
            case FaseTurno.ColocacionIngredientes:
                IniciarFase(FaseTurno.DespliegueUtensiliosEfectos);
                break;
            case FaseTurno.DespliegueUtensiliosEfectos:
                IniciarFase(FaseTurno.EjecucionAcciones);
                break;
            case FaseTurno.EjecucionAcciones:
                IniciarFase(FaseTurno.FinTurno);
                break;
            case FaseTurno.FinTurno:
                // Ya manejado en IniciarFase
                break;
        }
    }

    // M�todo para que los jugadores indiquen que est�n listos (bot�n listo)
    public bool CheckPlayersReady()
    {
       foreach (ReadyButton readyButton in readyButtons)
        {
            if (!readyButton.isReady.Value)
            {
                return false;
            }
            
        }
        foreach (ReadyButton readyButton in readyButtons)
        {
            readyButton.TryChangeReady();
        }


        PasarFaseSiguiente();
        return true;

    }
}