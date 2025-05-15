using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class EfectosProgramados : MonoBehaviour
    {
        [System.Serializable]
        public class EfectoProgramado
        {
            public Efectos efecto;
            public List<GameObject> nodos = new List<GameObject>();
            public bool ejecutado = false;

            // Nuevo campo para rastrear si el efecto es en tablero enemigo
            public bool esEfectoEnemigo = false;
        }
    public System.Action OnEjecucionCompletada;
    public static EfectosProgramados Instance { get; private set; }
        public List<EfectoProgramado> efectosProgramados = new List<EfectoProgramado>();
        private bool ejecutandoActualmente = false;
        [SerializeField] private bool mostrarDebug = true;

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

        /// <summary>
        /// Programa un efecto para ejecutarse durante la fase de ejecución
        /// </summary>
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

            // Verificación adicional para efectos en tablero enemigo
            if (esTableroEnemigo)
            {
                Debug.Log($"Programando efecto {efecto.Name} en tablero enemigo");
            }

            // Crear un nuevo efecto programado con la nueva propiedad
            EfectoProgramado nuevoProgramado = new EfectoProgramado
            {
                efecto = efecto,
                nodos = new List<GameObject>(nodos),
                ejecutado = false,
                esEfectoEnemigo = esTableroEnemigo
            };

            // Agregar a la lista
            efectosProgramados.Add(nuevoProgramado);
            Debug.Log($"Efecto {efecto.Name} programado con éxito para {nodos.Count} nodos. Es en tablero enemigo: {esTableroEnemigo}");

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

        /// <summary>
        /// Ejecuta todos los efectos programados
        /// </summary>
        public void EjecutarEfectosProgramados()
        {
            Debug.Log($"Ejecutando {efectosProgramados.Count} efectos programados");

            foreach (EfectoProgramado efectoProgramado in efectosProgramados)
            {
                if (efectoProgramado.ejecutado) continue;

                // Log especial para efectos en tablero enemigo
                if (efectoProgramado.esEfectoEnemigo)
                {
                    Debug.Log($"Ejecutando efecto {efectoProgramado.efecto.Name} en tablero enemigo");
                }

                // Verificar permisos según el propietario del tablero
                bool tienePermiso = true;

                // Si es efecto enemigo, verificar permisos especiales
                if (efectoProgramado.esEfectoEnemigo)
                {
                    // Aquí podrías implementar verificaciones adicionales o restricciones
                    // para efectos enemigos si fuera necesario

                    // Por ahora, permitimos todos los efectos en tablero enemigo
                    tienePermiso = true;
                }

                if (tienePermiso)
                {
                    // Usar el EfectosManager
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
                }
                else
                {
                    Debug.LogWarning($"Sin permiso para ejecutar efecto {efectoProgramado.efecto.Name} en tablero enemigo");

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
            OnEjecucionCompletada?.Invoke();
        }


        /// <summary>
        /// Elimina los efectos que ya han sido ejecutados
        /// </summary>
        public void LimpiarEfectosEjecutados()
        {
            efectosProgramados.RemoveAll(e => e.ejecutado);
        }

        /// <summary>
        /// Limpia todos los efectos programados (cancelarlos)
        /// </summary>
        public void LimpiarTodosLosEfectos()
        {
            // Si está en ejecución, no permitir la limpieza
            if (ejecutandoActualmente)
            {
                Debug.LogWarning("No se pueden limpiar los efectos durante la ejecución");
                return;
            }

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

            if (mostrarDebug)
            {
                Debug.Log("Todos los efectos programados han sido limpiados");
            }
        }

        /// <summary>
        /// Obtiene la cantidad de efectos programados pendientes
        /// </summary>
        public int ObtenerCantidadPendientes()
        {
            return efectosProgramados.Count;
        }
    }
