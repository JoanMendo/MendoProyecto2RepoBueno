using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Efecto que crea una conexión entre dos nodos por un número de turnos
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "S_Blanca", menuName = "CookingGame/Resources/Efectos/S_Blanca")]
public class S_Blanca : Efectos
{
    [Tooltip("Prefab visual para la conexión entre nodos")]
    public GameObject prefabConexion;

    public override void Activar(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Este efecto se activa de forma especial a través de CrearConexion
        Debug.Log("S_Blanca requiere selección de nodos específicos para activarse");
    }

    public void CrearConexion(GameObject nodoOrigen, GameObject nodoDestino)
    {
        // ⚠️ VALIDACIÓN PARA PREVENIR NULL REFERENCE
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

        // Verificar si hay un prefab para el manager
        GameObject gestorObj = null;

        // Intentar obtener el prefab del diccionario
        // Usar el método público de EfectosManager para crear un manager
        gestorObj = manager.CrearEfectoManager("Blanco");

        // Si no se pudo crear, implementar lógica de respaldo
        if (gestorObj == null)
        {
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

        // ⚠️ VALIDAR QUE NO SEA NULL
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

        // Iniciar el efecto
        gestorBlanco.IniciarEfectoConexion(nodoOrigen, nodoDestino, this, duracion);

        // Registrar el efecto con su gestor propio
        manager.RegistrarEfectoConGestorPropio(this, nodoOrigen, nodoDestino, gestorObj);
    }

    public override List<GameObject> CalcularNodosAfectados(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Para S_Blanca, necesitamos un segundo nodo, pero eso se maneja en CrearConexion
        // Este método se usa solo para la interfaz
        return new List<GameObject>() { nodoObjetivo };
    }
}


