using System.Collections;
using System.Collections.Generic;
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
    public float tiempoPorFase = 10f; // 10 segundos por fase
    private float temporizadorFase;

    public FaseTurno faseActual = FaseTurno.ColocacionIngredientes;
    public ComprobadorVictoria victoria;

    public void Start()
    {

        IniciarFase(FaseTurno.ColocacionIngredientes);
        temporizadorFase = tiempoPorFase; // Inicializar el temporizador
    }

    private void Update()
    {


        // Actualizar temporizador solo en el servidor
        temporizadorFase -= Time.deltaTime;

        if (temporizadorFase <= 0f)
        {
            // Cambiar a la siguiente fase cuando el temporizador llega a cero
            PasarFaseSiguiente();
            temporizadorFase = tiempoPorFase; // Reiniciar temporizador
        }
    }

    public void IniciarFase(FaseTurno nuevaFase)
    {
        faseActual = nuevaFase;
        temporizadorFase = tiempoPorFase; // Reiniciar temporizador al cambiar de fase
        GlobalGameManager globalGameManager = FindFirstObjectByType<GlobalGameManager>();
        CameraManager cameraManager = FindFirstObjectByType<CameraManager>();

        switch (faseActual)
        {
            case FaseTurno.ColocacionIngredientes:
                LocalGameManager.Instance.ingredientCount = 0;
                
               // cameraManager.MoveCamera(globalGameManager.tablerosEnEscena[(int)NetworkManager.Singleton.LocalClientId].transform.position, 25f); // Cambiar la posición de la cámara a la posición inicial del tablero
                                                                        

                break;
            case FaseTurno.DespliegueUtensiliosEfectos:
                LocalGameManager.Instance.utensilEffectsCount = 0;

                //cameraManager.MoveCamera(globalGameManager.tablerosEnEscena[(int)NetworkManager.Singleton.LocalClientId].transform.position, 30f);
                break;
            case FaseTurno.EjecucionAcciones:
                //cameraManager.MoveCamera(globalGameManager.tablerosEnEscena[(int)NetworkManager.Singleton.LocalClientId].transform.position, 35f);
                
                break;
            case FaseTurno.FinTurno:
                
                turnoActual++;
                if (turnoActual > maxTurnos)
                {
                    // Lógica para final del juego
                    Debug.Log("¡Juego terminado!");
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
                if (turnoActual <= maxTurnos)
                {
                    IniciarFase(FaseTurno.ColocacionIngredientes);
                }
                break;
        }
    }

  
}                                       