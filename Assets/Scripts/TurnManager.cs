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
    DespliegueUtensilios,
    EjecucionAcciones,
    FinTurno
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Gestiona el flujo de turnos y fases del juego, sincronizado en red.
/// ‡‡</summary>_PLACEHOLDER‡‡
public class TurnManager : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float tiempoMaximoPorFase = 60f; // Tiempo máximo por fase en segundos
    [SerializeField] private bool usarTiempoMaximo = true; // ¿Usar límite de tiempo?

    // Variables de red
    public NetworkVariable<FaseTurno> faseTurnoActual = new NetworkVariable<FaseTurno>(FaseTurno.ColocacionIngredientes);
    public NetworkVariable<float> tiempoRestanteFase = new NetworkVariable<float>(60f);
    public NetworkVariable<int> numeroTurnoActual = new NetworkVariable<int>(1);

    // Control de jugadores listos
    private List<ReadyButton> readyButtons = new List<ReadyButton>();
    private bool todosPreparedos = false;

    // Singleton para acceso fácil (opcional)
    public static TurnManager Instance { get; private set; }

    private void Awake()
    {
        Debug.Log("[TurnManager] Awake iniciado");

        // Configurar singleton (opcional)
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TurnManager] Ya existe una instancia. Destruyendo duplicado.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Debug.Log("[TurnManager] Awake completado correctamente");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[TurnManager] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}");

        // Inicializar variables según si es servidor o cliente
        if (IsServer)
        {
            // Configuración inicial solo en el servidor
            faseTurnoActual.Value = FaseTurno.ColocacionIngredientes;
            tiempoRestanteFase.Value = tiempoMaximoPorFase;
            numeroTurnoActual.Value = 1;

            Debug.Log("[TurnManager] Valores iniciales configurados en el servidor");
        }

        // Suscribirse a cambios de fase para todos (servidor y clientes)
        faseTurnoActual.OnValueChanged += OnFaseTurnoChanged;

        Debug.Log("[TurnManager] OnNetworkSpawn completado");
    }

    private void Start()
    {
        Debug.Log("[TurnManager] Start iniciado");

        // Esperar un frame para que se inicialicen otros objetos
        Invoke("BuscarReadyButtons", 1f);

        // Configurar comprobación periódica de estado (solo en servidor)
        if (IsServer)
        {
            InvokeRepeating("ServerCheckAllPlayersReady", 2f, 0.5f);
        }

        Debug.Log("[TurnManager] Start completado");
    }

    // Buscar todos los ReadyButtons en la escena
    private void BuscarReadyButtons()
    {
        Debug.Log("[TurnManager] Buscando ReadyButtons en la escena...");

        readyButtons.Clear();
        ReadyButton[] allButtons = FindObjectsOfType<ReadyButton>();

        foreach (ReadyButton btn in allButtons)
        {
            readyButtons.Add(btn);
            Debug.Log($"[TurnManager] ReadyButton encontrado: ID={btn.GetInstanceID()}, OwnerClientId={btn.OwnerClientId}");
        }

        Debug.Log($"[TurnManager] Total de ReadyButtons encontrados: {readyButtons.Count}");
    }

    private void Update()
    {
        // Solo el servidor gestiona el tiempo y avance automático
        if (IsServer && usarTiempoMaximo)
        {
            // Actualizar tiempo restante
            tiempoRestanteFase.Value -= Time.deltaTime;

            // Avanzar fase automáticamente si se acaba el tiempo
            if (tiempoRestanteFase.Value <= 0)
            {
                Debug.Log("[TurnManager] Tiempo agotado para la fase actual. Avanzando automáticamente.");
                AvanzarFase();
            }
        }
    }

    // Nuevo método: solo el servidor verifica periódicamente el estado
    private void ServerCheckAllPlayersReady()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[TurnManager] ServerCheckAllPlayersReady ejecutado desde un cliente. Esto no debería suceder.");
            return;
        }

        Debug.Log("[TurnManager] Ejecutando verificación periódica del servidor..."); // Nuevo log para confirmar ejecución

        // Refrescar lista por si ha cambiado
        if (readyButtons.Count == 0)
        {
            Debug.Log("[TurnManager] No hay ReadyButtons en la lista. Buscando nuevos botones."); // Nuevo log
            BuscarReadyButtons();
        }

        // Verificar que todos los botones estén en estado "listo"
        bool todosListos = true;
        int readyCount = 0;
        int totalValid = 0;

        Debug.Log($"[TurnManager] Iniciando verificación de {readyButtons.Count} botones"); // Nuevo log

        foreach (ReadyButton btn in readyButtons)
        {
            if (btn == null)
            {
                Debug.LogWarning("[TurnManager] Se encontró un ReadyButton nulo en la lista");
                continue;
            }

            totalValid++;
            Debug.Log($"[TurnManager] Botón {btn.GetInstanceID()} para cliente {btn.OwnerClientId} - Estado: {btn.isReady.Value}"); // Nuevo log detallado

            if (btn.isReady.Value)
            {
                readyCount++;
            }
            else
            {
                todosListos = false;
            }
        }

        // Siempre mostrar el estado actual para depuración
        Debug.Log($"[TurnManager] ESTADO ACTUAL: {readyCount}/{totalValid} jugadores listos (de {readyButtons.Count} totales)");

        // Actualizar estado y posiblemente avanzar fase
        todosPreparedos = todosListos && totalValid > 0;

        Debug.Log($"[TurnManager] ¿Todos preparados? {todosPreparedos} (todosListos={todosListos}, totalValid={totalValid})"); // Nuevo log

        // Si todos están listos, avanzar fase automáticamente
        if (todosPreparedos)
        {
            Debug.Log("[TurnManager] ¡Todos los jugadores están listos! Avanzando fase...");
            AvanzarFase();
        }
    }


    // Modificar para que sea completamente privado
    private void CheckPlayersReady()
    {
        Debug.LogError("[TurnManager] ¡Método CheckPlayersReady obsoleto! Usa ServerCheckAllPlayersReady() en su lugar");
    }

    // Avanzar a la siguiente fase del turno
    public void AvanzarFase()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[TurnManager] AvanzarFase llamado desde un cliente. Solo el servidor puede avanzar fases.");
            return;
        }

        FaseTurno nuevaFase;

        switch (faseTurnoActual.Value)
        {
            case FaseTurno.ColocacionIngredientes:
                nuevaFase = FaseTurno.DespliegueUtensilios;
                Debug.Log("[TurnManager] Avanzando de ColocacionIngredientes a DespliegueUtensilios");
                break;

            case FaseTurno.DespliegueUtensilios:
                nuevaFase = FaseTurno.EjecucionAcciones;
                Debug.Log("[TurnManager] Avanzando de DespliegueUtensilios a EjecucionAcciones");
                break;

            case FaseTurno.EjecucionAcciones:
                nuevaFase = FaseTurno.FinTurno;
                Debug.Log("[TurnManager] Avanzando de EjecucionAcciones a FinTurno");
                break;

            case FaseTurno.FinTurno:
                // Iniciar nuevo turno
                nuevaFase = FaseTurno.ColocacionIngredientes;
                numeroTurnoActual.Value++;
                Debug.Log($"[TurnManager] Fin del turno. Iniciando nuevo turno #{numeroTurnoActual.Value}");
                break;

            default:
                nuevaFase = FaseTurno.ColocacionIngredientes;
                Debug.LogWarning("[TurnManager] Fase desconocida. Reiniciando a ColocacionIngredientes");
                break;
        }

        // Actualizar fase y reiniciar temporizador
        faseTurnoActual.Value = nuevaFase;
        tiempoRestanteFase.Value = tiempoMaximoPorFase;

        // Resetear botones de Ready al iniciar nueva fase
        ResetReadyButtons();

        Debug.Log($"[TurnManager] Fase actualizada a: {nuevaFase}, tiempo reiniciado a {tiempoMaximoPorFase}s");
    }

    // Evento cuando cambia la fase
    private void OnFaseTurnoChanged(FaseTurno previousValue, FaseTurno newValue)
    {
        Debug.Log($"[TurnManager] Fase cambiada: {previousValue} -> {newValue}");

        if (newValue == FaseTurno.EjecucionAcciones && IsServer)
        {
            ExecutarAccionesFase();
        }

        // Si implementas un sistema de eventos podrías hacer algo como:
        // EventManager.TriggerEvent("CambioFaseTurno", newValue);
    }

    // Reiniciar los botones de Ready
    private void ResetReadyButtons()
    {
        if (!IsServer) return;

        Debug.Log("[TurnManager] Reiniciando estado de todos los ReadyButtons");

        foreach (ReadyButton btn in readyButtons)
        {
            if (btn != null)
            {
                btn.ResetButtonState();
                Debug.Log($"[TurnManager] Reset ReadyButton para jugador {btn.OwnerClientId}");
            }
        }

        todosPreparedos = false;
    }

    // Obtener la fase actual (método de conveniencia)
    public FaseTurno GetFaseActual()
    {
        return faseTurnoActual.Value;
    }

    // Obtener el tiempo restante formateado como string mm:ss
    public string GetTiempoRestanteFormateado()
    {
        int minutos = Mathf.FloorToInt(tiempoRestanteFase.Value / 60);
        int segundos = Mathf.FloorToInt(tiempoRestanteFase.Value % 60);
        return string.Format("{0:00}:{1:00}", minutos, segundos);
    }

    // Para depuración: forzar cambio de fase desde inspector (solo en servidor)
    [ContextMenu("Forzar Avance de Fase")]
    public void DebugAvanzarFase()
    {
        if (IsServer)
        {
            Debug.Log("[TurnManager] Avance de fase forzado desde inspector");
            AvanzarFase();
        }
        else
        {
            Debug.LogWarning("[TurnManager] No se puede forzar avance de fase - No es el servidor");
        }
    }
    
    private void ExecutarAccionesFase()
    {
        Debug.Log("[TurnManager] Ejecutando acciones de la fase actual");

        if (faseTurnoActual.Value == FaseTurno.EjecucionAcciones)
        {
            // Iniciar Coroutine para ejecutar todas las acciones en secuencia
            StartCoroutine(EjecutarAccionesSecuencialmente());
        }
    }
    private IEnumerator EjecutarAccionesSecuencialmente()
    {
        // 1. Primero activar todos los efectos de ingredientes en todos los tableros
        NodeMap[] tableros = FindObjectsOfType<NodeMap>();
        foreach (NodeMap tablero in tableros)
        {
            Debug.Log($"[TurnManager] Activando efectos en tablero de cliente {tablero.ownerClientId}");
            tablero.ActivarTodosLosEfectos();
        }

        // Esperar un tiempo breve para que los efectos de ingredientes se procesen
        yield return new WaitForSeconds(0.5f);

        // 2. Ejecutar todos los efectos programados (NUEVO)
        if (EfectosProgramados.Instance != null)
        {
            Debug.Log("[TurnManager] Ejecutando efectos programados");
            EfectosProgramados.Instance.EjecutarEfectosProgramados();

            // Esperar un tiempo para que los efectos se visualicen
            yield return new WaitForSeconds(1.0f);
        }
        else
        {
            Debug.LogWarning("[TurnManager] No se encontró instancia de EfectosProgramados");
        }

        // 3. Luego ejecutar todos los utensilios programados
        if (UtensiliosProgramados.Instance != null)
        {
            Debug.Log("[TurnManager] Ejecutando utensilios programados");

            // Suscribirse al evento de finalización para avanzar automáticamente
            UtensiliosProgramados.Instance.OnEjecucionCompletada += OnEjecucionUtensiliosCompletada;

            // Iniciar ejecución de utensilios programados
            UtensiliosProgramados.Instance.EjecutarUtensiliosProgramados();

            Debug.Log("[TurnManager] Ejecución de utensilios iniciada, esperando finalización...");
        }
        else
        {
            Debug.LogWarning("[TurnManager] No se encontró instancia de UtensiliosProgramados");

            // Si no hay instancia, avanzar después de un breve retraso
            yield return new WaitForSeconds(1.0f);
            AvanzarAutomaticamente();
        }
    }

    // Este método será llamado cuando se complete la ejecución de los utensilios
    private void OnEjecucionUtensiliosCompletada()
    {
        Debug.Log("[TurnManager] Ejecución de utensilios completada");

        // Desuscribirse para evitar múltiples llamadas
        if (UtensiliosProgramados.Instance != null)
        {
            UtensiliosProgramados.Instance.OnEjecucionCompletada -= OnEjecucionUtensiliosCompletada;
        }

        // Avanzar a la siguiente fase después de un breve retraso
        StartCoroutine(AvanzarDespuesDeRetraso(1.0f));
    }

    private IEnumerator AvanzarDespuesDeRetraso(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        AvanzarAutomaticamente();
    }

    private void AvanzarAutomaticamente()
    {
        if (!IsServer) return;

        Debug.Log("[TurnManager] Avanzando automáticamente después de completar acciones");
        AvanzarFase();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        Debug.Log("[TurnManager] OnDestroy");

        // Limpiar eventos
        if (faseTurnoActual != null)
        {
            faseTurnoActual.OnValueChanged -= OnFaseTurnoChanged;
        }

        // Limpiar singleton si somos la instancia
        if (Instance == this)
        {
            Instance = null;
        }
    }
}