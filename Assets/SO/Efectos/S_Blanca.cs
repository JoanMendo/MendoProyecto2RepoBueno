using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Efecto que crea una conexión entre dos nodos por un número de turnos
/// </summary>
[CreateAssetMenu(fileName = "S_Blanca", menuName = "CookingGame/Resources/Efectos/S_Blanca")]
public class S_Blanca : Efectos
{
    [Tooltip("Prefab visual para la conexión entre nodos")]
    public GameObject prefabConexion;

    [Tooltip("Color de la conexión")]
    public Color colorConexion = Color.white;

    [Tooltip("Grosor de la línea de conexión")]
    [Range(0.05f, 0.3f)]
    public float grosorLinea = 0.1f;

    [Tooltip("Frecuencia de intercambio automático (segundos)")]
    [Range(0.5f, 5f)]
    public float frecuenciaIntercambio = 1.0f;

    /// <summary>
    /// Activa el efecto en el nodo especificado
    /// </summary>
    public override void Activar(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Este efecto se activa de forma especial a través de CrearConexion
        Debug.Log("S_Blanca requiere selección de nodos específicos para activarse");
    }

    /// <summary>
    /// Crea una conexión entre dos nodos
    /// </summary>
    public void CrearConexion(GameObject nodoOrigen, GameObject nodoDestino)
    {
        // Validación para prevenir null reference
        if (nodoOrigen == null || nodoDestino == null)
        {
            Debug.LogError($"S_Blanca.CrearConexion: Nodo origen o destino es null");
            return;
        }

        // Obtener el EfectosManager
        EfectosManager manager = EfectosManager.Instance;
        if (manager == null)
        {
            Debug.LogError("No se encontró el EfectosManager en la escena");
            return;
        }

        // Crear el gestor de efecto
        GameObject gestorObj = manager.CrearEfectoManager("Blanco");

        // Si no se pudo crear, implementar lógica de respaldo
        if (gestorObj == null)
        {
            Debug.LogWarning("No se pudo crear EfectoBlancoManager desde EfectosManager, creando uno nuevo");

            // Crear uno nuevo como fallback
            gestorObj = new GameObject("EfectoBlancoManager");
            gestorObj.AddComponent<EfectoBlancoManager>();

            // Añadir NetworkObject si es necesario
            if (!gestorObj.TryGetComponent<NetworkObject>(out _))
            {
                gestorObj.AddComponent<NetworkObject>();
            }
        }

        // Configurar el gestor
        EfectoBlancoManager gestorBlanco = gestorObj.GetComponent<EfectoBlancoManager>();

        // Validar que no sea null
        if (gestorBlanco == null)
        {
            Debug.LogError("No se pudo obtener el componente EfectoBlancoManager");
            Destroy(gestorObj);
            return;
        }

        // Configurar el NetworkObject si existe
        NetworkObject netObj = gestorObj.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsSpawned)
        {
            netObj.Spawn();
        }

        // Transferir configuración al gestor
        gestorBlanco.ConfigurarConEfecto(this);

        // Iniciar el efecto
        gestorBlanco.IniciarEfectoConexion(nodoOrigen, nodoDestino, this, duracion);

        // Registrar el efecto con su gestor propio
        manager.RegistrarEfectoConGestorPropio(this, nodoOrigen, nodoDestino, gestorObj);

        Debug.Log($"Efecto S_Blanca: Conexión creada entre {nodoOrigen.name} y {nodoDestino.name} por {duracion} turnos");
    }

    /// <summary>
    /// Calcula los nodos afectados por este efecto (necesario para la interfaz de Efectos)
    /// </summary>
    public override List<GameObject> CalcularNodosAfectados(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Para S_Blanca, necesitamos un segundo nodo, pero eso se maneja en CrearConexion
        // Este método se usa solo para la interfaz
        return new List<GameObject>() { nodoObjetivo };
    }
}