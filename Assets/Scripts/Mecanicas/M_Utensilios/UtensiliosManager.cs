using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Unity.Netcode;
/// ‡‡<summary>_PLACEHOLDER‡‡
/// Interfaz para estandarizar los managers de utensilios
/// ‡‡</summary>_PLACEHOLDER‡‡
public interface IUtensilioManager
{
    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Configura el manager con los datos del utensilio específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    void ConfigurarConUtensilio(Utensilio utensilio);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Valida si los nodos seleccionados son adecuados para la acción del utensilio
    /// ‡‡</summary>_PLACEHOLDER‡‡
    bool ValidarNodos(List<GameObject> nodosSeleccionados);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Ejecuta la acción del utensilio sobre los nodos seleccionados
    /// ‡‡</summary>_PLACEHOLDER‡‡
    void EjecutarAccion(List<GameObject> nodosSeleccionados);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Limpia los efectos visuales y recursos utilizados
    /// ‡‡</summary>_PLACEHOLDER‡‡
    void LimpiarEfecto();
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Gestor central para todos los utensilios del juego
/// ‡‡</summary>_PLACEHOLDER‡‡
/// ‡‡<summary>_PLACEHOLDER‡‡
/// Gestor central para todos los utensilios del juego
/// ‡‡</summary>_PLACEHOLDER‡‡
public class UtensiliosManager : MonoBehaviour
{
    // Singleton
    public static UtensiliosManager Instance { get; private set; }

    [Header("Configuración")]
    [Tooltip("Ruta de la carpeta donde se almacenan los ScriptableObjects de utensilios")]
    [SerializeField] private string rutaUtensilios = "ScriptableObjects/Utensilios";

    [Tooltip("Ruta de la carpeta donde se almacenan los prefabs de managers de utensilios")]
    [SerializeField] private string rutaManagersPrefabs = "Prefabs/UtensilioManagers";

    [Tooltip("Mostrar mensajes de depuración")]
    [SerializeField] private bool mostrarDebug = false;

    // Caché de datos
    private Dictionary<string, Utensilio> utensiliosCache = new Dictionary<string, Utensilio>();
    private Dictionary<string, GameObject> managersPrefabsCache = new Dictionary<string, GameObject>();

    // Lista de todos los utensilios disponibles en el juego
    private List<Utensilio> todosLosUtensilios = new List<Utensilio>();

    // Propiedad pública para acceder a todos los utensilios
    public IReadOnlyList<Utensilio> TodosLosUtensilios => todosLosUtensilios.AsReadOnly();

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
        CargarUtensilios();
        CargarManagersPrefabs();

        if (mostrarDebug)
        {
            Debug.Log($"UtensiliosManager: Inicializado con {utensiliosCache.Count} utensilios y {managersPrefabsCache.Count} managers");
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Carga todos los ScriptableObjects de utensilios desde Resources
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void CargarUtensilios()
    {
        // Cargar todos los utensilios
        Utensilio[] utensilios = Resources.LoadAll<Utensilio>(rutaUtensilios);

        // Limpiar y actualizar colecciones
        utensiliosCache.Clear();
        todosLosUtensilios.Clear();

        foreach (Utensilio utensilio in utensilios)
        {
            if (utensilio != null)
            {
                string clave = utensilio.Name.ToLower();
                utensiliosCache[clave] = utensilio;
                todosLosUtensilios.Add(utensilio);

                if (mostrarDebug)
                {
                    Debug.Log($"UtensiliosManager: Cargado {utensilio.Name} ({utensilio.GetType().Name})");
                }
            }
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Carga todos los prefabs de managers de utensilios desde Resources
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void CargarManagersPrefabs()
    {
        // Cargar todos los prefabs de managers
        GameObject[] prefabs = Resources.LoadAll<GameObject>(rutaManagersPrefabs);

        managersPrefabsCache.Clear();

        foreach (GameObject prefab in prefabs)
        {
            if (prefab.GetComponent<IUtensilioManager>() != null)
            {
                string nombre = prefab.name.ToLower().Replace("manager", "");
                managersPrefabsCache[nombre] = prefab;

                if (mostrarDebug)
                {
                    Debug.Log($"UtensiliosManager: Cargado prefab de manager para {nombre}");
                }
            }
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene un utensilio por su nombre
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public Utensilio GetUtensilioPorNombre(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;

        string clave = nombre.ToLower();

        // Buscar en caché
        if (utensiliosCache.TryGetValue(clave, out Utensilio utensilio))
        {
            return utensilio;
        }

        // Intentar cargar bajo demanda
        Utensilio utensilioObj = Resources.Load<Utensilio>($"{rutaUtensilios}/{nombre}");
        if (utensilioObj != null)
        {
            utensiliosCache[clave] = utensilioObj;
            if (!todosLosUtensilios.Contains(utensilioObj))
            {
                todosLosUtensilios.Add(utensilioObj);
            }
            return utensilioObj;
        }

        if (mostrarDebug)
        {
            Debug.LogWarning($"No se encontró utensilio con nombre: {nombre}");
        }

        return null;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Crea un nuevo manager para el tipo de utensilio especificado
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public GameObject CrearUtensilioManager(string tipoUtensilio)
    {
        if (string.IsNullOrEmpty(tipoUtensilio)) return null;

        string clave = tipoUtensilio.ToLower();

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
        return CrearManagerGenericoPorTipo(tipoUtensilio);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Intenta crear un manager genérico basado en el tipo de utensilio
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private GameObject CrearManagerGenericoPorTipo(string tipoUtensilio)
    {
        // Crear un GameObject nuevo
        GameObject managerObj = new GameObject($"{tipoUtensilio}Manager");

        // Añadir el componente específico según el tipo, o uno genérico
        switch (tipoUtensilio.ToLower())
        {
            case "rodillo":
                managerObj.AddComponent<RodilloManager>();
                break;
            case "pinzas":
                managerObj.AddComponent<PinzasManager>();
                break;
            case "espatula":
                managerObj.AddComponent<EspatulaManager>();
                break;
            case "cuchillo":
                managerObj.AddComponent<CuchilloManager>();
                break;
            default:
                Debug.LogError($"No se pudo crear manager para: {tipoUtensilio}");
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
    /// Crea y configura un manager para un utensilio específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public GameObject CrearYConfigurarManager(Utensilio utensilio)
    {
        if (utensilio == null) return null;

        // Obtener el tipo específico de utensilio
        string tipoUtensilio = utensilio.GetType().Name;

        // Crear el manager
        GameObject managerObj = CrearUtensilioManager(tipoUtensilio);
        if (managerObj == null) return null;

        // Configurar el manager
        IUtensilioManager manager = managerObj.GetComponent<IUtensilioManager>();
        if (manager != null)
        {
            manager.ConfigurarConUtensilio(utensilio);
        }
        else
        {
            Debug.LogError($"El manager creado para {tipoUtensilio} no implementa IUtensilioManager");
            Destroy(managerObj);
            return null;
        }

        return managerObj;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Ejecuta la acción de un utensilio sobre los nodos seleccionados
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public bool EjecutarAccionUtensilio(Utensilio utensilio, List<GameObject> nodosSeleccionados)
    {
        if (utensilio == null || nodosSeleccionados == null || nodosSeleccionados.Count == 0)
        {
            return false;
        }

        // Verificar economía
        Economia economia = FindObjectOfType<Economia>();
        if (economia != null && economia.money.Value < utensilio.Price)
        {
            if (mostrarDebug)
            {
                Debug.Log($"No hay suficiente dinero para usar {utensilio.Name}. Necesitas: {utensilio.Price}, Tienes: {economia.money.Value}");
            }
            return false;
        }

        // Crear y configurar manager
        GameObject managerObj = CrearYConfigurarManager(utensilio);
        if (managerObj == null) return false;

        IUtensilioManager manager = managerObj.GetComponent<IUtensilioManager>();

        // Validar nodos
        if (!manager.ValidarNodos(nodosSeleccionados))
        {
            if (mostrarDebug)
            {
                Debug.Log($"Los nodos seleccionados no son válidos para {utensilio.Name}");
            }
            Destroy(managerObj);
            return false;
        }

        // Ejecutar acción
        manager.EjecutarAccion(nodosSeleccionados);

        // Cobrar dinero por usar el utensilio
        if (economia != null)
        {
            economia.less_money(utensilio.Price);
        }

        return true;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Verifica si un conjunto de nodos es válido para un utensilio específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public bool ValidarNodosParaUtensilio(Utensilio utensilio, List<GameObject> nodos)
    {
        if (utensilio == null || nodos == null) return false;

        // Verificación básica de cantidad
        if (nodos.Count != utensilio.nodosRequeridos)
        {
            return false;
        }

        // Verificación específica según tipo
        return utensilio.ValidarColocacion(nodos);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene todos los utensilios disponibles de un tipo específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public List<Utensilio> GetUtensiliosPorTipo(string tipo)
    {
        List<Utensilio> resultado = new List<Utensilio>();

        foreach (Utensilio utensilio in todosLosUtensilios)
        {
            if (utensilio.tipoUtensilio.ToLower() == tipo.ToLower())
            {
                resultado.Add(utensilio);
            }
        }

        return resultado;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene la lista de utensilios disponibles para el nivel de jugador actual
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public List<Utensilio> GetUtensiliosDisponibles(int nivelJugador = 1)
    {
        List<Utensilio> disponibles = new List<Utensilio>();

        foreach (Utensilio utensilio in todosLosUtensilios)
        {
            if (utensilio.nivel <= nivelJugador)
            {
                disponibles.Add(utensilio);
            }
        }

        return disponibles;
    }
}
