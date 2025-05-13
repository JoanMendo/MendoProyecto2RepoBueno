using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class IngredientManager : MonoBehaviour
{
    public static IngredientManager Instance { get; private set; }

    // Diccionario para almacenar prefabs de gestores
    private Dictionary<string, GameObject> gestoresPrefabs = new Dictionary<string, GameObject>();

    // Diccionario para almacenar ScriptableObjects de ingredientes
    private Dictionary<string, IngredientesSO> ingredientesSOCache = new Dictionary<string, IngredientesSO>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Cargar recursos
        CargarPrefabsGestores();
        CargarIngredientesSO();
    }

    private void CargarPrefabsGestores()
    {
        // Cargar todos los prefabs de gestores desde Resources
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/EffectManagers");
        foreach (GameObject prefab in prefabs)
        {
            if (prefab.GetComponent<IEffectManager>() != null)
            {
                string nombre = prefab.name.ToLower().Replace("effectmanager", "");
                gestoresPrefabs[nombre] = prefab;
                Debug.Log($"IngredientManager: Cargado gestor para {nombre}");
            }
        }
    }

    private void CargarIngredientesSO()
    {
        // Cargar todos los ScriptableObjects de ingredientes
        IngredientesSO[] ingredientes = Resources.LoadAll<IngredientesSO>("ScriptableObjects");
        foreach (IngredientesSO ingrediente in ingredientes)
        {
            ingredientesSOCache[ingrediente.name.ToLower()] = ingrediente;
            Debug.Log($"IngredientManager: Cargado IngredientesSO {ingrediente.name}");
        }
    }

    public GameObject CrearGestorEfecto(string tipoIngrediente)
    {
        tipoIngrediente = tipoIngrediente.ToLower();

        GameObject prefabGestor = null;

        if (gestoresPrefabs.ContainsKey(tipoIngrediente))
        {
            prefabGestor = gestoresPrefabs[tipoIngrediente];
        }
        else
        {
            prefabGestor = Resources.Load<GameObject>($"Prefabs/EffectManagers/{tipoIngrediente}Manager");
            if (prefabGestor != null)
            {
                gestoresPrefabs[tipoIngrediente] = prefabGestor;
            }
        }

        if (prefabGestor == null)
        {
            Debug.LogError($"No se encontró prefab de gestor para: {tipoIngrediente}");
            return null;
        }

        return Instantiate(prefabGestor);
    }

    public IngredientesSO GetIngredienteByName(string nombre)
    {
        nombre = nombre.ToLower();

        if (ingredientesSOCache.ContainsKey(nombre))
        {
            return ingredientesSOCache[nombre];
        }

        // Intentar cargar bajo demanda si no está en caché
        IngredientesSO ingrediente = Resources.Load<IngredientesSO>($"ScriptableObjects/Ingredientes/{nombre}");
        if (ingrediente != null)
        {
            ingredientesSOCache[nombre] = ingrediente;
            return ingrediente;
        }

        Debug.LogWarning($"No se encontró IngredientesSO para {nombre}");
        return null;
    }
}


public interface IEffectManager
{
    void ConfigurarConIngrediente(IngredientesSO ingrediente);
    void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados);
    void LimpiarEfecto();
}