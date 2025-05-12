using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;


public class GlobalGameManager : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject tableroPrefab;

    // Lista para rastrear los tableros generados
    private List<GameObject> spawnedBoards = new List<GameObject>();

    // Flag para evitar inicialización múltiple
    private bool initialized = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Verificar inicialización previa para evitar duplicados
        if (initialized) return;

        if (IsServer)
        {
            Debug.Log($"GlobalGameManager: Inicializando en servidor. Clientes conectados: {NetworkManager.Singleton.ConnectedClientsIds.Count}");
            SpawnAllTableros();

            // Suscribirse a eventos de conexión de nuevos clientes
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            initialized = true;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            Debug.Log($"Nuevo cliente conectado: {clientId}. Generando tablero...");
            SpawnTableroForClient(clientId);
        }
    }

    public void SpawnAllTableros()
    {
        Debug.Log($"Generando tableros para {NetworkManager.Singleton.ConnectedClientsIds.Count} clientes conectados");

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnTableroForClient(clientId);
        }
    }

    public void SpawnTableroForClient(ulong clientId)
    {
        // VERIFICAR si ya existe un tablero para este cliente
        foreach (GameObject existingBoard in spawnedBoards)
        {
            if (existingBoard == null) continue; // Skip if destroyed

            NetworkObject existingNetObj = existingBoard.GetComponent<NetworkObject>();
            if (existingNetObj != null && existingNetObj.OwnerClientId == clientId)
            {
                Debug.Log($"El cliente {clientId} ya tiene un tablero asignado. Omitiendo creación duplicada.");
                return; // Evitar duplicados
            }
        }

        // Instanciar el tablero primero sin spawnearlo
        GameObject tablero = Instantiate(tableroPrefab);

        // Obtener NodeMap y configurarlo antes del spawn
        NodeMap nodeMap = tablero.GetComponent<NodeMap>();
        if (nodeMap != null)
        {
            // Configurar propiedades antes de crear nodos
            nodeMap.ownerClientId = clientId;
        }
        else
        {
            Debug.LogWarning("El tablero no tiene componente NodeMap!");
        }

        // Spawnear el tablero
        NetworkObject tableNetObj = tablero.GetComponent<NetworkObject>();
        if (tableNetObj != null)
        {
            Debug.Log($"Spawneando tablero para cliente {clientId}");
            tableNetObj.SpawnWithOwnership(clientId);
        }
        else
        {
            Debug.LogError("¡El tablero no tiene NetworkObject! No puede ser spawneado.");
            Destroy(tablero);
            return;
        }

        // Registrar el tablero generado
        spawnedBoards.Add(tablero);
    }


    public void ResetInitialization()
    {
        // Método para forzar reinicialización (útil para cambios de escena)
        initialized = false;

        // Limpiar lista de tableros, pero no destruirlos
        // (La destrucción debería manejarse por el sistema de cambio de escena)
        spawnedBoards.Clear();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        // Desuscribirse de eventos al destruir
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}