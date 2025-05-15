using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class UtensiliosProgramados : MonoBehaviour
{
    [System.Serializable]
    public class UtensilioProgramado
    {
        public Utensilio utensilio;
        public List<GameObject> nodos = new List<GameObject>();
        public bool ejecutado = false;

        // Nuevo campo para rastrear si el utensilio es en tablero enemigo
        public bool esUtensilioEnemigo = false;
    }

    public static UtensiliosProgramados Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private float tiempoEntreEjecuciones = 1.0f;
    [SerializeField] private bool ejecutarAutomaticamente = false;
    [SerializeField] private bool mostrarDebug = true;


    // Lista de utensilios y nodos programados
    private List<UtensilioProgramado> utensiliosProgramados = new List<UtensilioProgramado>();

    // Estado de ejecución
    private bool ejecutandoActualmente = false;

    // Evento que se dispara al completar todas las ejecuciones
    public System.Action OnEjecucionCompletada;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Añade un utensilio a la cola de programación
    /// </summary>
    public bool ProgramarUtensilio(Utensilio utensilio, List<GameObject> nodos)
    {
        // Verificar que no estamos en medio de una ejecución
        if (ejecutandoActualmente)
        {
            Debug.LogWarning("No se puede programar mientras se están ejecutando acciones");
            return false;
        }

        // Verificación básica de validez
        if (utensilio == null || nodos == null || nodos.Count == 0)
        {
            Debug.LogWarning("Datos de utensilio no válidos para programación");
            return false;
        }

        // Verificar número de nodos necesarios
        if (nodos.Count != utensilio.nodosRequeridos)
        {
            Debug.LogWarning($"El utensilio {utensilio.Name} requiere {utensilio.nodosRequeridos} nodos, pero se proporcionaron {nodos.Count}");
            return false;
        }

        // Determinar si los nodos pertenecen a un tablero enemigo
        bool esTableroEnemigo = false;
        ulong clienteLocal = NetworkManager.Singleton.LocalClientId;
        NodeMap mapaEnemigo = null;

        foreach (GameObject nodo in nodos)
        {
            Node nodoComp = nodo.GetComponent<Node>();
            if (nodoComp != null && nodoComp.nodeMap != null)
            {
                if (nodoComp.nodeMap.ownerClientId != clienteLocal)
                {
                    esTableroEnemigo = true;
                    mapaEnemigo = nodoComp.nodeMap;
                }
            }
        }

        // Verificación adicional para utensilios en tablero enemigo
        if (esTableroEnemigo)
        {
            Debug.Log($"Programando utensilio {utensilio.Name} en tablero enemigo");
        }

        // Validar con UtensiliosManager si los nodos son adecuados
        if (UtensiliosManager.Instance != null)
        {
            GameObject managerTemp = UtensiliosManager.Instance.CrearYConfigurarManager(utensilio);
            if (managerTemp != null)
            {
                IUtensilioManager manager = managerTemp.GetComponent<IUtensilioManager>();
                if (manager != null)
                {
                    if (!manager.ValidarNodos(nodos))
                    {
                        Debug.LogWarning($"Los nodos seleccionados no son válidos para {utensilio.Name}");
                        Destroy(managerTemp);
                        return false;
                    }
                }
                Destroy(managerTemp); // Ya no necesitamos el manager temporal
            }
        }

        // Añadir a la cola con la nueva propiedad
        UtensilioProgramado nuevoProgramado = new UtensilioProgramado
        {
            utensilio = utensilio,
            nodos = new List<GameObject>(nodos),
            ejecutado = false,
            esUtensilioEnemigo = esTableroEnemigo
        };

        utensiliosProgramados.Add(nuevoProgramado);

        // Marcar nodos visualmente como programados
        foreach (var nodo in nodos)
        {
            Node nodoComp = nodo.GetComponent<Node>();
            if (nodoComp != null)
            {
                nodoComp.MarcarProgramado(utensilio);
            }
        }

        if (mostrarDebug)
        {
            Debug.Log($"Utensilio {utensilio.Name} programado con {nodos.Count} nodos. Es en tablero enemigo: {esTableroEnemigo}");
        }

        // Iniciar ejecución automática si está configurado
        if (ejecutarAutomaticamente && utensiliosProgramados.Count == 1)
        {
            EjecutarUtensiliosProgramados();
        }

        return true;
    }

    /// <summary>
    /// Ejecuta todos los utensilios programados en secuencia
    /// </summary>
    public void EjecutarUtensiliosProgramados()
    {
        if (ejecutandoActualmente)
        {
            Debug.LogWarning("Ya hay una ejecución en progreso");
            return;
        }

        if (utensiliosProgramados.Count == 0)
        {
            Debug.Log("No hay utensilios programados para ejecutar");
            OnEjecucionCompletada?.Invoke();
            return;
        }

        StartCoroutine(EjecutarSecuencia());
    }

    /// <summary>
    /// Coroutine para ejecutar los utensilios programados en secuencia
    /// </summary>
    private IEnumerator EjecutarSecuencia()
    {
        ejecutandoActualmente = true;

        if (mostrarDebug)
        {
            Debug.Log($"Iniciando ejecución de {utensiliosProgramados.Count} utensilios programados");
        }

        foreach (var utensilioProgramado in utensiliosProgramados)
        {
            Utensilio utensilio = utensilioProgramado.utensilio;
            List<GameObject> nodos = utensilioProgramado.nodos;
            bool esEnemigoUtensilio = utensilioProgramado.esUtensilioEnemigo;

            // Desmarcar nodos de programados a ejecutando
            foreach (var nodo in nodos)
            {
                Node nodoComp = nodo.GetComponent<Node>();
                if (nodoComp != null)
                {
                    nodoComp.MarcarEjecutando();
                }
            }

            if (mostrarDebug)
            {
                string mensajeTarget = esEnemigoUtensilio ? "en tablero enemigo" : "en tablero propio";
                Debug.Log($"Ejecutando utensilio {utensilio.Name} {mensajeTarget}");
            }

            // Verificar permisos según el propietario del tablero
            bool tienePermiso = true;

            // Si es utensilio enemigo, verificar permisos especiales
            if (esEnemigoUtensilio)
            {
                // Aquí podrías implementar verificaciones adicionales o restricciones
                // para utensilios enemigos si fuera necesario

                // Por ahora, permitimos todos los utensilios en tablero enemigo
                tienePermiso = true;
            }

            // Ejecutar acción con UtensiliosManager
            bool exito = false;
            if (tienePermiso && UtensiliosManager.Instance != null)
            {
                exito = UtensiliosManager.Instance.EjecutarAccionUtensilio(utensilio, nodos);
            }

            if (!exito)
            {
                Debug.LogWarning($"Error al ejecutar utensilio: {utensilio.Name}");

                // Desmarcar nodos
                foreach (var nodo in nodos)
                {
                    Node nodoComp = nodo.GetComponent<Node>();
                    if (nodoComp != null)
                    {
                        nodoComp.DesmarcarProgramado();
                    }
                }
            }

            // Esperar entre ejecuciones
            yield return new WaitForSeconds(tiempoEntreEjecuciones);
        }

        // Limpiar después de ejecutar
        utensiliosProgramados.Clear();
        ejecutandoActualmente = false;

        if (mostrarDebug)
        {
            Debug.Log("Ejecución de utensilios programados completada");
        }

        // Notificar que hemos terminado
        OnEjecucionCompletada?.Invoke();
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Cancela todos los utensilios programados
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void CancelarProgramacion()
    {
        if (ejecutandoActualmente)
        {
            Debug.LogWarning("No se puede cancelar durante la ejecución");
            return;
        }

        // Desmarcar todos los nodos
        foreach (var utensilioItem in utensiliosProgramados)
        {
            foreach (var nodo in utensilioItem.nodos)
            {
                Node nodoComp = nodo.GetComponent<Node>();
                if (nodoComp != null)
                {
                    nodoComp.DesmarcarProgramado();
                }
            }
        }

        // Limpiar listas
        utensiliosProgramados.Clear();

        if (mostrarDebug)
        {
            Debug.Log("Programación de utensilios cancelada");
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene la cantidad de utensilios programados actualmente
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public int ObtenerCantidadProgramados()
    {
        return utensiliosProgramados.Count;
    }
}

