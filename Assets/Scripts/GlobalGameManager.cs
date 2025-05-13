using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;


public class GlobalGameManager : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject tableroPrefab;
    // Nueva referencia para el botón Ready
    [Header("UI Elements")]
    [SerializeField] private GameObject readyButtonPrefab;
    [SerializeField] private Vector3 buttonOffset = new Vector3(-50, 0.5f, -25f); // Ajusta según necesites

    // Diccionario para rastrear la asociación entre tableros y botones
    private Dictionary<ulong, GameObject> clientReadyButtons = new Dictionary<ulong, GameObject>();

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
            //  Instanciar ReadyButton por separado
            SpawnReadyButtonForTablero(clientId, tablero);
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


    private void SpawnReadyButtonForTablero(ulong clientId, GameObject tablero)
    {
        if (readyButtonPrefab == null)
        {
            Debug.LogError("No se ha asignado el prefab del ReadyButton en GlobalGameManager");
            return;
        }

        // Calcular posición relativa al tablero
        Vector3 buttonPosition = tablero.transform.position + tablero.transform.TransformDirection(buttonOffset);

        // Instanciar el botón
        GameObject readyButton = Instantiate(readyButtonPrefab, buttonPosition, tablero.transform.rotation);
        readyButton.name = $"ReadyButton_Client_{clientId}";

        // Opcionalmente, guardar referencia al tablero asociado
        ReadyButton readyComponent = readyButton.GetComponent<ReadyButton>();
        if (readyComponent != null)
        {
            readyComponent.AssociatedTableroId = clientId;
        }

        // Spawnear con el mismo ownership que el tablero
        NetworkObject buttonNetObj = readyButton.GetComponent<NetworkObject>();
        if (buttonNetObj != null)
        {
            buttonNetObj.SpawnWithOwnership(clientId);

            // Guardar en el diccionario para seguimiento
            clientReadyButtons[clientId] = readyButton;

            Debug.Log($"ReadyButton spawneado para cliente {clientId}");
        }
        else
        {
            Debug.LogError("¡El ReadyButton no tiene NetworkObject! No puede ser spawneado.");
            Destroy(readyButton);
        }
    }


    public void ResetInitialization()
    {
        // Método para forzar reinicialización (útil para cambios de escena)
        initialized = false;

        // Limpiar lista de tableros, pero no destruirlos
        // (La destrucción debería manejarse por el sistema de cambio de escena)
        spawnedBoards.Clear();
    }

    // Modificar método existente para limpiar recursos
    public override void OnDestroy()
    {
        base.OnDestroy();

        // Desuscribirse de eventos al destruir
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        // Limpiar diccionario de botones
        clientReadyButtons.Clear();
    }
}