using System.Collections.Generic;
using UnityEngine;

public class EfectosProgramados : MonoBehaviour
{
    [System.Serializable]
    public class EfectoProgramado
    {
        public Efectos efecto;
        public List<GameObject> nodos = new List<GameObject>();
        public bool ejecutado = false;
    }

    public static EfectosProgramados Instance { get; private set; }
    public List<EfectoProgramado> efectosProgramados = new List<EfectoProgramado>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Programa un efecto para ejecutarse durante la fase de ejecución
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public bool ProgramarEfecto(Efectos efecto, List<GameObject> nodos)
    {
        if (efecto == null || nodos == null || nodos.Count == 0)
        {
            Debug.LogWarning("No se puede programar un efecto sin todos los datos necesarios");
            return false;
        }

        // Verificar si el efecto requiere cierta cantidad de nodos
        bool esEfectoMultiNodo = efecto is S_Blanca;
        if (esEfectoMultiNodo && nodos.Count < 2)
        {
            Debug.LogWarning("Este efecto requiere al menos 2 nodos");
            return false;
        }

        // Crear un nuevo efecto programado
        EfectoProgramado nuevoProgramado = new EfectoProgramado
        {
            efecto = efecto,
            nodos = new List<GameObject>(nodos),
            ejecutado = false
        };

        // Agregar a la lista
        efectosProgramados.Add(nuevoProgramado);
        Debug.Log($"Efecto {efecto.Name} programado con éxito para {nodos.Count} nodos");

        // Marcar los nodos como programados
        foreach (GameObject nodo in nodos)
        {
            Node nodoComp = nodo.GetComponent<Node>();
            if (nodoComp != null)
            {
                nodoComp.MarcarProgramado(efecto);
            }
        }

        return true;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Ejecuta todos los efectos programados
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void EjecutarEfectosProgramados()
    {
        Debug.Log($"Ejecutando {efectosProgramados.Count} efectos programados");

        foreach (EfectoProgramado efectoProgramado in efectosProgramados)
        {
            if (efectoProgramado.ejecutado) continue;

            // Usar el nuevo EfectosManager
            bool exito = EfectosManager.Instance.EjecutarAccionEfecto(
                efectoProgramado.efecto,
                efectoProgramado.nodos
            );

            if (!exito)
            {
                Debug.LogWarning($"Error al ejecutar efecto: {efectoProgramado.efecto.Name}");

                // Desmarcar nodos
                foreach (var nodo in efectoProgramado.nodos)
                {
                    Node nodoComp = nodo.GetComponent<Node>();
                    if (nodoComp != null)
                    {
                        nodoComp.DesmarcarProgramado();
                    }
                }
            }

            efectoProgramado.ejecutado = true;
        }

        // Limpiar lista después de ejecutar
        LimpiarEfectosEjecutados();
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Elimina los efectos que ya han sido ejecutados
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void LimpiarEfectosEjecutados()
    {
        efectosProgramados.RemoveAll(e => e.ejecutado);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Limpia todos los efectos programados (cancelarlos)
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void LimpiarTodosLosEfectos()
    {
        // Desmarcar todos los nodos
        foreach (EfectoProgramado efectoProgramado in efectosProgramados)
        {
            foreach (GameObject nodo in efectoProgramado.nodos)
            {
                if (nodo != null)
                {
                    Node nodoComp = nodo.GetComponent<Node>();
                    if (nodoComp != null)
                    {
                        nodoComp.DesmarcarProgramado();
                    }
                }
            }
        }

        // Limpiar lista
        efectosProgramados.Clear();
    }
}