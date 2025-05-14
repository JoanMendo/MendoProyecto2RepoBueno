using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtensiliosProgramados : MonoBehaviour
{
    public static UtensiliosProgramados Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private float tiempoEntreEjecuciones = 1.0f;
    [SerializeField] private bool ejecutarAutomaticamente = false;
    [SerializeField] private bool mostrarDebug = true;

    // Lista de utensilios y nodos programados
    private List<Utensilio> utensiliosProgramados = new List<Utensilio>();
    private List<List<GameObject>> nodosPorUtensilio = new List<List<GameObject>>();

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

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Añade un utensilio a la cola de programación
    /// ‡‡</summary>_PLACEHOLDER‡‡
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

        // Añadir a la cola
        utensiliosProgramados.Add(utensilio);
        nodosPorUtensilio.Add(new List<GameObject>(nodos));

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
            Debug.Log($"Utensilio {utensilio.Name} programado con {nodos.Count} nodos");
        }

        // Iniciar ejecución automática si está configurado
        if (ejecutarAutomaticamente && utensiliosProgramados.Count == 1)
        {
            EjecutarUtensiliosProgramados();
        }

        return true;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Ejecuta todos los utensilios programados en secuencia
    /// ‡‡</summary>_PLACEHOLDER‡‡
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

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Coroutine para ejecutar los utensilios programados en secuencia
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private IEnumerator EjecutarSecuencia()
    {
        ejecutandoActualmente = true;

        if (mostrarDebug)
        {
            Debug.Log($"Iniciando ejecución de {utensiliosProgramados.Count} utensilios programados");
        }

        for (int i = 0; i < utensiliosProgramados.Count; i++)
        {
            Utensilio utensilio = utensiliosProgramados[i];
            List<GameObject> nodos = nodosPorUtensilio[i];

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
                Debug.Log($"Ejecutando utensilio {i + 1}/{utensiliosProgramados.Count}: {utensilio.Name}");
            }

            // Ejecutar acción con UtensiliosManager
            bool exito = false;
            if (UtensiliosManager.Instance != null)
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
        nodosPorUtensilio.Clear();
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
        for (int i = 0; i < utensiliosProgramados.Count; i++)
        {
            List<GameObject> nodos = nodosPorUtensilio[i];
            foreach (var nodo in nodos)
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
        nodosPorUtensilio.Clear();

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

