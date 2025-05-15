using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;


public class GlobalGameManager : NetworkBehaviour
{
    [Header("Configuraci�n")]
    [SerializeField] private GameObject tableroPrefab;
    // Nueva referencia para el bot�n Ready
    [Header("UI Elements")]
    [SerializeField] private GameObject readyButtonPrefab;
    [SerializeField] private Vector3 buttonOffset = new Vector3(-50, 0.5f, -25f); // Ajusta seg�n necesites

    // Diccionario para rastrear la asociaci�n entre tableros y botones
    private Dictionary<ulong, GameObject> clientReadyButtons = new Dictionary<ulong, GameObject>();

    // Lista para rastrear los tableros generados
    private List<GameObject> spawnedBoards = new List<GameObject>();

    // Flag para evitar inicializaci�n m�ltiple
    private bool initialized = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Verificar inicializaci�n previa para evitar duplicados
        if (initialized) return;

        if (IsServer)
        {
            Debug.Log($"GlobalGameManager: Inicializando en servidor. Clientes conectados: {NetworkManager.Singleton.ConnectedClientsIds.Count}");
            SpawnAllTableros();

            // Suscribirse a eventos de conexi�n de nuevos clientes
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
                Debug.Log($"El cliente {clientId} ya tiene un tablero asignado. Omitiendo creaci�n duplicada.");
                return; // Evitar duplicados
            }
        }

        // Instanciar el tablero primero sin spawnearlo
        GameObject tablero = Instantiate(tableroPrefab);

        // Configurar todas las propiedades ANTES de spawning
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

        // Asegurar que la econom�a est� configurada
        Economia economiaJugador = tablero.GetComponent<Economia>();
        if (economiaJugador == null)
        {
            economiaJugador = tablero.AddComponent<Economia>();
        }
        economiaJugador.clientId.Value = clientId;

        // IMPORTANTE: Primero hacer spawn del tablero
        NetworkObject tableNetObj = tablero.GetComponent<NetworkObject>();
        if (tableNetObj != null)
        {
            tableNetObj.SpawnWithOwnership(clientId);
            Debug.Log($"Tablero spawneado para cliente {clientId}");

            // Despu�s registrar el tablero
            if (TableroRegistry.Instance != null)
            {
                TableroRegistry.Instance.RegistrarTablero(clientId, nodeMap);
                Debug.Log($"Tablero registrado en TableroRegistry para cliente {clientId}");
            }

            // Finalmente instanciar el bot�n Ready
            SpawnReadyButtonForTablero(clientId, tablero);

            // Registrar el tablero generado
            spawnedBoards.Add(tablero);
        }
        else
        {
            Debug.LogError($"�El tablero no tiene NetworkObject! No puede ser spawneado para cliente {clientId}");
            Destroy(tablero);
        }
    }


    private void SpawnReadyButtonForTablero(ulong clientId, GameObject tablero)
    {
        if (readyButtonPrefab == null)
        {
            Debug.LogError("No se ha asignado el prefab del ReadyButton en GlobalGameManager");
            return;
        }

        // Calcular posici�n relativa al tablero
        Vector3 buttonPosition = tablero.transform.position + tablero.transform.TransformDirection(buttonOffset);

        // Instanciar el bot�n
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
            Debug.LogError("�El ReadyButton no tiene NetworkObject! No puede ser spawneado.");
            Destroy(readyButton);
        }
    }


    public void ResetInitialization()
    {
        // M�todo para forzar reinicializaci�n (�til para cambios de escena)
        initialized = false;

        // Limpiar lista de tableros, pero no destruirlos
        // (La destrucci�n deber�a manejarse por el sistema de cambio de escena)
        spawnedBoards.Clear();
    }

    // Modificar m�todo existente para limpiar recursos
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