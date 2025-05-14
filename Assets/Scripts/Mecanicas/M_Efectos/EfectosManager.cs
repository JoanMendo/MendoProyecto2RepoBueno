using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Interfaz para estandarizar los managers de efectos
/// ‡‡</summary>_PLACEHOLDER‡‡
public interface IEfectoManager
{
    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Configura el manager con los datos del efecto específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    void ConfigurarConEfecto(Efectos efecto);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Valida si los nodos seleccionados son adecuados para la acción del efecto
    /// ‡‡</summary>_PLACEHOLDER‡‡
    bool ValidarNodos(List<GameObject> nodosSeleccionados);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Ejecuta la acción del efecto sobre los nodos seleccionados
    /// ‡‡</summary>_PLACEHOLDER‡‡
    void EjecutarAccion(List<GameObject> nodosSeleccionados);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Limpia los efectos visuales y recursos utilizados
    /// ‡‡</summary>_PLACEHOLDER‡‡
    void LimpiarEfecto();
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Gestor central para todos los efectos del juego
/// ‡‡</summary>_PLACEHOLDER‡‡
public class EfectosManager : NetworkBehaviour
{
    // Singleton
    public static EfectosManager Instance { get; private set; }

    [Header("Configuración")]
    [Tooltip("Ruta de la carpeta donde se almacenan los ScriptableObjects de efectos")]
    [SerializeField] private string rutaEfectos = "ScriptableObjects/Efectos";

    [Tooltip("Ruta de la carpeta donde se almacenan los prefabs de managers de efectos")]
    [SerializeField] private string rutaManagersPrefabs = "Prefabs/EfectosManager";

    [Tooltip("Mostrar mensajes de depuración")]
    [SerializeField] private bool mostrarDebug = false;

    [Header("Efectos precargados")]
    [SerializeField] private Efectos[] efectosPrecargados;

    // Caché de datos
    private Dictionary<string, Efectos> efectosCache = new Dictionary<string, Efectos>();
    private Dictionary<string, GameObject> managersPrefabsCache = new Dictionary<string, GameObject>();

    // Lista de todos los efectos disponibles en el juego
    private List<Efectos> todosLosEfectos = new List<Efectos>();

    // Seguimiento de efectos activos
    private List<EfectoActivo> efectosActivos = new List<EfectoActivo>();

    // Clase para rastrear efectos activos
    private class EfectoActivo
    {
        public Efectos efecto;
        public GameObject nodoObjetivo;
        public List<GameObject> nodosAfectados;
        public int turnosRestantes;
        public GameObject gestorEfecto; // Referencia al gestor
    }

    // Propiedad pública para acceder a todos los efectos
    public IReadOnlyList<Efectos> TodosLosEfectos => todosLosEfectos.AsReadOnly();

    private void Awake()
    {
        // Configuración del singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Inicializar caches
        CargarEfectos();
        CargarManagersPrefabs();

        if (mostrarDebug)
        {
            Debug.Log($"EfectosManager: Inicializado con {efectosCache.Count} efectos y {managersPrefabsCache.Count} managers");
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Carga todos los ScriptableObjects de efectos desde Resources
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void CargarEfectos()
    {
        // Agregar efectos precargados
        foreach (var efecto in efectosPrecargados)
        {
            if (efecto != null)
            {
                string clave = efecto.Name.ToLower();
                efectosCache[clave] = efecto;
                if (!todosLosEfectos.Contains(efecto))
                {
                    todosLosEfectos.Add(efecto);
                }
            }
        }

        // Cargar todos los efectos
        Efectos[] efectos = Resources.LoadAll<Efectos>(rutaEfectos);

        foreach (Efectos efecto in efectos)
        {
            if (efecto != null)
            {
                string clave = efecto.Name.ToLower();
                efectosCache[clave] = efecto;
                if (!todosLosEfectos.Contains(efecto))
                {
                    todosLosEfectos.Add(efecto);
                }

                if (mostrarDebug)
                {
                    Debug.Log($"EfectosManager: Cargado {efecto.Name} ({efecto.GetType().Name})");
                }
            }
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Carga todos los prefabs de managers de efectos desde Resources
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void CargarManagersPrefabs()
    {
        // Cargar todos los prefabs de managers
        GameObject[] prefabs = Resources.LoadAll<GameObject>(rutaManagersPrefabs);

        managersPrefabsCache.Clear();

        foreach (GameObject prefab in prefabs)
        {
            if (prefab.GetComponent<IEfectoManager>() != null)
            {
                // Extraer el nombre del tipo de efecto del nombre del prefab
                string nombre = prefab.name.ToLower();
                nombre = nombre.Replace("manager", "").Replace("efecto", "");
                managersPrefabsCache[nombre] = prefab;

                if (mostrarDebug)
                {
                    Debug.Log($"EfectosManager: Cargado prefab de manager para {nombre}");
                }
            }
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene un efecto por su nombre
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public Efectos GetEfectoPorNombre(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;

        string clave = nombre.ToLower();

        // Buscar en caché
        if (efectosCache.TryGetValue(clave, out Efectos efecto))
        {
            return efecto;
        }

        // Intentar cargar bajo demanda
        Efectos efectoObj = Resources.Load<Efectos>($"{rutaEfectos}/{nombre}");
        if (efectoObj != null)
        {
            efectosCache[clave] = efectoObj;
            if (!todosLosEfectos.Contains(efectoObj))
            {
                todosLosEfectos.Add(efectoObj);
            }
            return efectoObj;
        }

        if (mostrarDebug)
        {
            Debug.LogWarning($"No se encontró efecto con nombre: {nombre}");
        }

        return null;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Crea un nuevo manager para el tipo de efecto especificado
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public GameObject CrearEfectoManager(string tipoEfecto)
    {
        if (string.IsNullOrEmpty(tipoEfecto)) return null;

        string clave = tipoEfecto.ToLower().Replace("s_", "");

        // Buscar prefab en caché
        GameObject prefab = null;
        if (managersPrefabsCache.TryGetValue(clave, out prefab) && prefab != null)
        {
            GameObject instancia = Instantiate(prefab);
            return instancia;
        }

        // Intentar cargar bajo demanda
        GameObject managerPrefab = Resources.Load<GameObject>($"{rutaManagersPrefabs}/{clave}Manager");
        if (managerPrefab != null)
        {
            managersPrefabsCache[clave] = managerPrefab;
            GameObject instancia = Instantiate(managerPrefab);
            return instancia;
        }

        // Crear manager genérico según el tipo
        return CrearManagerGenericoPorTipo(tipoEfecto);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Intenta crear un manager genérico basado en el tipo de efecto
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private GameObject CrearManagerGenericoPorTipo(string tipoEfecto)
    {
        // Crear un GameObject nuevo
        GameObject managerObj = new GameObject($"{tipoEfecto}Manager");

        // Añadir el componente específico según el tipo, o uno genérico
        switch (tipoEfecto.ToLower().Replace("s_", ""))
        {
            case "picante":
                managerObj.AddComponent<EfectoPicanteManager>();
                break;
            case "especial":
                managerObj.AddComponent<EfectoEspecialManager>();
                break;
            case "blanca":
                managerObj.AddComponent<EfectoBlancoManager>();
                break;
            default:
                Debug.LogError($"No se pudo crear manager para: {tipoEfecto}");
                Destroy(managerObj);
                return null;
        }

        // Añadir NetworkObject si no lo tiene
        if (managerObj.GetComponent<NetworkObject>() == null)
        {
            managerObj.AddComponent<NetworkObject>();
        }

        return managerObj;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Crea y configura un manager para un efecto específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public GameObject CrearYConfigurarManager(Efectos efecto)
    {
        if (efecto == null) return null;

        // Obtener el tipo específico de efecto
        string tipoEfecto = efecto.GetType().Name;

        // Crear el manager
        GameObject managerObj = CrearEfectoManager(tipoEfecto);
        if (managerObj == null) return null;

        // Configurar el manager
        IEfectoManager manager = managerObj.GetComponent<IEfectoManager>();
        if (manager != null)
        {
            manager.ConfigurarConEfecto(efecto);
        }
        else
        {
            Debug.LogError($"El manager creado para {tipoEfecto} no implementa IEfectoManager");
            Destroy(managerObj);
            return null;
        }

        return managerObj;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Ejecuta la acción de un efecto sobre los nodos seleccionados
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public bool EjecutarAccionEfecto(Efectos efecto, List<GameObject> nodosSeleccionados)
    {
        if (efecto == null || nodosSeleccionados == null || nodosSeleccionados.Count == 0)
        {
            return false;
        }

        // Verificar economía
        Economia economia = FindObjectOfType<Economia>();
        if (economia != null && economia.money.Value < efecto.Price)
        {
            if (mostrarDebug)
            {
                Debug.Log($"No hay suficiente dinero para usar {efecto.Name}. Necesitas: {efecto.Price}, Tienes: {economia.money.Value}");
            }
            return false;
        }

        // Crear y configurar manager
        GameObject managerObj = CrearYConfigurarManager(efecto);
        if (managerObj == null) return false;

        IEfectoManager manager = managerObj.GetComponent<IEfectoManager>();

        // Validar nodos
        if (!manager.ValidarNodos(nodosSeleccionados))
        {
            if (mostrarDebug)
            {
                Debug.Log($"Los nodos seleccionados no son válidos para {efecto.Name}");
            }
            Destroy(managerObj);
            return false;
        }

        // Ejecutar acción
        manager.EjecutarAccion(nodosSeleccionados);

        // Cobrar dinero por usar el efecto
        if (economia != null)
        {
            economia.less_money(efecto.Price);
        }

        // Si el efecto tiene duración, registrarlo para seguimiento
        if (efecto.duracion > 0)
        {
            RegisterActiveEffect(efecto, nodosSeleccionados[0], managerObj);
        }

        return true;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Registra un efecto activo para su seguimiento
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void RegisterActiveEffect(Efectos efecto, GameObject nodoPrincipal, GameObject manager)
    {
        // Solo el servidor registra efectos
        if (!IsServer) return;

        Node nodoComp = nodoPrincipal.GetComponent<Node>();
        if (nodoComp == null || nodoComp.nodeMap == null) return;

        List<GameObject> nodosAfectados = efecto.CalcularNodosAfectados(nodoPrincipal, nodoComp.nodeMap);

        EfectoActivo nuevoEfecto = new EfectoActivo
        {
            efecto = efecto,
            nodoObjetivo = nodoPrincipal,
            nodosAfectados = nodosAfectados,
            turnosRestantes = efecto.duracion,
            gestorEfecto = manager
        };

        efectosActivos.Add(nuevoEfecto);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Registra un efecto que conecta dos nodos (caso especial como S_Blanca)
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void RegistrarEfectoConGestorPropio(Efectos efecto, GameObject nodoOrigen, GameObject nodoDestino, GameObject gestorObj)
    {
        if (!IsServer) return;

        List<GameObject> nodosAfectados = new List<GameObject> { nodoOrigen, nodoDestino };

        EfectoActivo nuevoEfecto = new EfectoActivo
        {
            efecto = efecto,
            nodoObjetivo = nodoOrigen,
            nodosAfectados = nodosAfectados,
            turnosRestantes = efecto.duracion,
            gestorEfecto = gestorObj
        };

        efectosActivos.Add(nuevoEfecto);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Procesa todos los efectos activos por turno
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ProcesarEfectosPorTurno()
    {
        if (!IsServer) return;

        // Copiar lista para evitar problemas al modificarla durante la iteración
        List<EfectoActivo> efectosAProcesar = new List<EfectoActivo>(efectosActivos);

        foreach (var efectoActivo in efectosAProcesar)
        {
            // Procesar usando el manager apropiado
            if (efectoActivo.gestorEfecto != null)
            {
                IEfectoManager manager = efectoActivo.gestorEfecto.GetComponent<IEfectoManager>();
                if (manager != null)
                {
                    // El manager se encarga del procesamiento
                    List<GameObject> nodos = new List<GameObject> { efectoActivo.nodoObjetivo };
                    nodos.AddRange(efectoActivo.nodosAfectados.Where(n => n != efectoActivo.nodoObjetivo));

                    // No necesitamos ejecutar la acción completa de nuevo, solo procesar el turno
                    // Esto debería manejarse internamente en cada manager
                }
            }

            // Reducir turno restante
            efectoActivo.turnosRestantes--;

            // Si llegó a cero, finalizar y remover
            if (efectoActivo.turnosRestantes <= 0)
            {
                // Limpiar el efecto
                if (efectoActivo.gestorEfecto != null)
                {
                    IEfectoManager manager = efectoActivo.gestorEfecto.GetComponent<IEfectoManager>();
                    if (manager != null)
                    {
                        manager.LimpiarEfecto();
                    }
                }

                // Remover de la lista
                efectosActivos.Remove(efectoActivo);
            }
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Verifica si un conjunto de nodos es válido para un efecto específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public bool ValidarNodosParaEfecto(Efectos efecto, List<GameObject> nodos)
    {
        if (efecto == null || nodos == null) return false;

        // Verificación básica según tipo
        if (efecto is S_Blanca)
        {
            // Necesita exactamente 2 nodos para crear una conexión
            return nodos.Count == 2;
        }
        else if (efecto is S_Picante || efecto is S_Especial)
        {
            // Estos efectos requieren exactamente 1 nodo
            return nodos.Count == 1;
        }

        return false;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene todos los efectos disponibles para el nivel de jugador actual
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public List<Efectos> GetEfectosDisponibles(int nivelJugador = 1)
    {
        List<Efectos> disponibles = new List<Efectos>();

        foreach (Efectos efecto in todosLosEfectos)
        {
            if (efecto.nivel <= nivelJugador)
            {
                disponibles.Add(efecto);
            }
        }

        return disponibles;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Muestra un efecto visual en una posición
    /// ‡‡</summary>_PLACEHOLDER‡‡
    [ClientRpc]
    public void MostrarEfectoVisualClientRpc(string nombreEfecto, Vector3 posicion)
    {
        // Buscar efecto por nombre
        Efectos efecto = GetEfectoPorNombre(nombreEfecto);

        // Mostrar efecto visual si existe
        if (efecto != null && efecto.prefab3D != null)
        {
            GameObject efectoVisual = Instantiate(efecto.prefab3D, posicion, Quaternion.identity);

            // Destruir después de un tiempo
            Destroy(efectoVisual, 2.0f);
        }
    }
}