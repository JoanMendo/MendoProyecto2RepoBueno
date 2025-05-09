using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Enumera las fases posibles de un turno de juego
/// ‡‡</summary>_PLACEHOLDER‡‡
public enum FaseTurno
{
    ColocacionIngredientes,
    DespliegueUtensiliosEfectos,
    EjecucionAcciones,
    FinTurno
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Gestiona el flujo de turnos y fases del juego, sincronizado en red.
/// ‡‡</summary>_PLACEHOLDER‡‡
public class TurnManager : NetworkBehaviour
{
    [Header("Configuración de Turnos")]
    [Tooltip("Número máximo de turnos antes de finalizar la partida")]
    [SerializeField] private int maxTurnos = 10;

    [Header("Configuración de Fases")]
    [Tooltip("Duración máxima de cada fase en segundos")]
    [SerializeField] private float duracionFaseSegundos = 30f;

    [Header("Referencias")]
    [Tooltip("Referencia al sistema de victoria")]
    [SerializeField] private ComprobadorVictoria victoria;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Turno actual del juego, sincronizado en red
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public NetworkVariable<int> turnoActual = new NetworkVariable<int>(1);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Fase actual del turno, sincronizada en red
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public NetworkVariable<FaseTurno> faseActual = new NetworkVariable<FaseTurno>(FaseTurno.ColocacionIngredientes);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Indica si la partida ha terminado
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private NetworkVariable<bool> partidaTerminada = new NetworkVariable<bool>(false);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Tiempo restante en la fase actual
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private float tiempoRestanteFase;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Botones de listo encontrados en la escena
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private ReadyButton[] readyButtons;

    // Eventos para comunicación con otros sistemas
    public static event Action<FaseTurno> OnFaseCambiada;
    public static event Action<int> OnNuevoTurno;
    public static event Action OnPartidaTerminada;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Solo el servidor inicia el juego
            StartCoroutine(InitializeGameDelayed());
        }

        // Suscribirse a cambios en variables de red
        faseActual.OnValueChanged += OnFaseActualCambiada;
        turnoActual.OnValueChanged += OnTurnoActualCambiado;
        partidaTerminada.OnValueChanged += OnPartidaTerminadaCambiada;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicializa el juego con un pequeño retraso para asegurar
    /// que todos los objetos de red estén listos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private IEnumerator InitializeGameDelayed()
    {
        yield return new WaitForSeconds(0.5f);

        // Encontrar todos los botones de "listo"
        readyButtons = FindObjectsByType<ReadyButton>(FindObjectsSortMode.None);
        Debug.Log($"TurnManager: Encontrados {readyButtons.Length} ReadyButtons en la escena");

        // Iniciar la primera fase
        IniciarFase(FaseTurno.ColocacionIngredientes);
    }

    private void OnDisable()
    {
        // Desuscribirse de eventos
        if (faseActual != null)
            faseActual.OnValueChanged -= OnFaseActualCambiada;

        if (turnoActual != null)
            turnoActual.OnValueChanged -= OnTurnoActualCambiado;

        if (partidaTerminada != null)
            partidaTerminada.OnValueChanged -= OnPartidaTerminadaCambiada;
    }

    private void Update()
    {
        // Solo el servidor maneja la lógica de tiempo
        if (!IsServer || partidaTerminada.Value) return;

        // Actualizar temporizador de fase
        tiempoRestanteFase -= Time.deltaTime;

        // Pasar a siguiente fase si se agota el tiempo
        if (tiempoRestanteFase <= 0f)
        {
            Debug.Log($"Tiempo agotado en fase {faseActual.Value}. Pasando a siguiente fase.");
            PasarFaseSiguiente();
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia una nueva fase del juego (solo llamar desde el servidor)
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarFase(FaseTurno nuevaFase)
    {
        if (!IsServer) return;
        if (partidaTerminada.Value) return;

        Debug.Log($"Iniciando fase: {nuevaFase}");

        // Reiniciar temporizador
        tiempoRestanteFase = duracionFaseSegundos;

        // Reiniciar estado de botones Ready
        ReiniciarBotonesReady();

        // Cambiar fase actual
        faseActual.Value = nuevaFase;

        // Acciones específicas por fase
        switch (nuevaFase)
        {
            case FaseTurno.ColocacionIngredientes:
                // Configuración específica para fase de colocación
                break;

            case FaseTurno.DespliegueUtensiliosEfectos:
                // Configuración específica para fase de utensilios
                break;

            case FaseTurno.EjecucionAcciones:
                // Ejecución automática, pasa inmediatamente
                PasarFaseSiguiente();
                break;

            case FaseTurno.FinTurno:
                // Incrementar turno y verificar fin de juego
                turnoActual.Value++;

                if (turnoActual.Value > maxTurnos)
                {
                    FinalizarPartida();
                }
                else
                {
                    // Iniciar nuevo turno con retraso para permitir animaciones
                    StartCoroutine(IniciarNuevoTurnoDelayed());
                }
                break;
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia un nuevo turno con un pequeño retraso
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private IEnumerator IniciarNuevoTurnoDelayed()
    {
        yield return new WaitForSeconds(1.5f);
        IniciarFase(FaseTurno.ColocacionIngredientes);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Finaliza la partida y determina el ganador
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void FinalizarPartida()
    {
        if (!IsServer) return;

        Debug.Log("Partida finalizada. Calculando resultado...");
        partidaTerminada.Value = true;

        // Comprobar victoria (implementación actual)
        if (victoria != null)
        {
            victoria.ComprobarVictoria();
        }

        // Notificar a sistemas interesados
        OnPartidaTerminada?.Invoke();
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Avanza a la siguiente fase del turno
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void PasarFaseSiguiente()
    {
        if (!IsServer) return;
        if (partidaTerminada.Value) return;

        switch (faseActual.Value)
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
                // Controlado en IniciarFase
                break;
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Reinicia todos los botones de "listo" al estado no listo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void ReiniciarBotonesReady()
    {
        if (!IsServer) return;

        foreach (var button in readyButtons)
        {
            if (button != null && button.isActiveAndEnabled)
            {
                button.ResetButtonState();
            }
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Verifica si todos los jugadores están listos para avanzar
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void CheckPlayersReady()
    {
        if (!IsServer) return;
        if (partidaTerminada.Value) return;

        bool todosListos = true;

        // Verificar cada botón de listo
        foreach (var button in readyButtons)
        {
            if (button == null || !button.isActiveAndEnabled)
                continue;

            if (!button.isReady.Value)
            {
                todosListos = false;
                break;
            }
        }

        // Si todos están listos, avanzar a la siguiente fase
        if (todosListos)
        {
            Debug.Log("Todos los jugadores están listos. Avanzando fase...");
            PasarFaseSiguiente();
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Devuelve el tiempo restante en la fase actual (para UI)
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public float GetTiempoRestante()
    {
        return Mathf.Max(tiempoRestanteFase, 0f);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Manejador para cambios en fase actual
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void OnFaseActualCambiada(FaseTurno oldValue, FaseTurno newValue)
    {
        // Notificar a sistemas interesados del cambio de fase
        OnFaseCambiada?.Invoke(newValue);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Manejador para cambios en turno actual
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void OnTurnoActualCambiado(int oldValue, int newValue)
    {
        Debug.Log($"Iniciando turno {newValue}");
        OnNuevoTurno?.Invoke(newValue);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Manejador para cambio en estado de partida terminada
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void OnPartidaTerminadaCambiada(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("La partida ha terminado");
        }
    }
}