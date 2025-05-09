
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Unity.Netcode;

public class NodeMap : NetworkBehaviour
{
    [Header("Configuración del Mapa")]
    public GameObject map;
    public GameObject nodePrefab;
    public int width = 8;
    public int height = 8;

    [Header("Referencias")]
    public List<GameObject> nodesList = new List<GameObject>();

    [Header("Depuración")]
    [SerializeField] private bool autoGenerateOnStart = true;
    [SerializeField] private bool showDebugInfo = true;

    private Dictionary<Vector2Int, GameObject> nodesByPosition;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicialización cuando se activa el objeto
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void Awake()
    {
        if (map == null)
        {
            map = gameObject;
        }

        nodesByPosition = new Dictionary<Vector2Int, GameObject>();
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Al iniciar, generar mapa si está configurado para ello
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Solo el servidor genera el mapa
        if (IsServer && autoGenerateOnStart)
        {
            Generate3DTilemap();
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Genera un mapa 3D de nodos basado en la configuración
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void Generate3DTilemap()
    {
        // Solo el servidor puede generar el mapa
        if (!IsServer) return;

        // Limpiar nodos existentes si los hay
        foreach (var nodo in nodesList)
        {
            if (nodo != null)
            {
                NetworkObject netObj = nodo.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn();
                }
                Destroy(nodo);
            }
        }
        nodesList.Clear();
        nodesByPosition.Clear();

        // Verificar colisionador
        BoxCollider boxCollider = map.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogError("El mapa necesita un BoxCollider para generar el grid");
            return;
        }

        // Obtener dimensiones del colisionador
        Vector3 colliderSize = boxCollider.bounds.size;
        Vector3 cellSize = new Vector3(colliderSize.x / width, colliderSize.y, colliderSize.z / height);

        // Calcular posición inicial
        Vector3 startPosition = new Vector3(
            boxCollider.bounds.min.x + cellSize.x / 2,
            boxCollider.bounds.max.y,
            boxCollider.bounds.min.z + cellSize.z / 2
        );

        // Generar nodos
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Crear e inicializar nodo
                GameObject casilla = Instantiate(nodePrefab, map.transform);
                NetworkObject netObj = casilla.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                }

                // Configurar rotación para que coincida con el mapa
                casilla.transform.rotation = map.transform.rotation;

                // Ajustar escala para que encaje en la celda
                BoxCollider casillaCollider = casilla.GetComponent<BoxCollider>();
                if (casillaCollider != null)
                {
                    Vector3 casillaColliderSize = casillaCollider.bounds.size;
                    Vector3 adjustedScale = new Vector3(
                        cellSize.x / casillaColliderSize.x,
                        cellSize.y / casillaColliderSize.y,
                        cellSize.z / casillaColliderSize.z
                    );
                    casilla.transform.localScale = adjustedScale * 0.95f; // Ligero espaciado
                }

                // Posicionar nodo
                Vector3 position = startPosition + new Vector3(
                    x * cellSize.x,
                    0f,
                    y * cellSize.z
                );
                casilla.transform.position = position;

                // Configurar componente Node
                Node nodoComp = casilla.GetComponent<Node>();
                if (nodoComp != null)
                {
                    Vector2Int coords = new Vector2Int(x, y);
                    nodoComp.position = coords;
                    nodoComp.Initialize(this);

                    // Nombrar para depuración
                    casilla.name = $"Node_{x}_{y}";
                }

                // Guardar referencia
                nodesList.Add(casilla);
                nodesByPosition[new Vector2Int(x, y)] = casilla;

                if (showDebugInfo)
                {
                    Debug.Log($"Nodo creado en ({x}, {y}) - Posición: {position}");
                }
            }
        }

        // Construir vecinos para cada nodo
        foreach (var nodo in nodesList)
        {
            Node nodeComp = nodo.GetComponent<Node>();
            if (nodeComp != null)
            {
                CalcularVecinosNodo(nodeComp);
            }
        }

        Debug.Log($"Mapa generado: {width}x{height} = {nodesList.Count} nodos");
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Calcula y asigna los nodos vecinos para un nodo específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void CalcularVecinosNodo(Node nodo)
    {
        Vector2Int pos = Vector2Int.RoundToInt(nodo.position);

        // Definir direcciones adyacentes
        Vector2Int[] direcciones = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // Derecha
            new Vector2Int(-1, 0),  // Izquierda
            new Vector2Int(0, 1),   // Arriba
            new Vector2Int(0, -1),  // Abajo
            new Vector2Int(1, 1),   // Diagonal superior derecha
            new Vector2Int(-1, 1),  // Diagonal superior izquierda
            new Vector2Int(1, -1),  // Diagonal inferior derecha
            new Vector2Int(-1, -1)  // Diagonal inferior izquierda
        };

        // Buscar vecinos en cada dirección
        foreach (var dir in direcciones)
        {
            Vector2Int vecPos = pos + dir;
            if (nodesByPosition.TryGetValue(vecPos, out GameObject vecino))
            {
                nodo.vecinos.Add(vecino);
            }
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene un nodo en la posición especificada
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public GameObject GetNodeAtPosition(Vector2 position)
    {
        Vector2Int posInt = Vector2Int.RoundToInt(position);
        if (nodesByPosition.TryGetValue(posInt, out GameObject nodo))
        {
            return nodo;
        }

        // Búsqueda alternativa si no está en el diccionario
        foreach (var candidato in nodesList)
        {
            Node componente = candidato.GetComponent<Node>();
            if (componente != null && Vector2Int.RoundToInt(componente.position) == posInt)
            {
                return candidato;
            }
        }

        return null;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene todos los nodos en una columna específica
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public List<GameObject> GetNodesInColumn(int x)
    {
        List<GameObject> resultado = new List<GameObject>();
        foreach (var nodo in nodesList)
        {
            Node componente = nodo.GetComponent<Node>();
            if (componente != null && Mathf.RoundToInt(componente.position.x) == x)
            {
                resultado.Add(nodo);
            }
        }
        return resultado;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Obtiene todos los nodos en una fila específica
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public List<GameObject> GetNodesInRow(int y)
    {
        List<GameObject> resultado = new List<GameObject>();
        foreach (var nodo in nodesList)
        {
            Node componente = nodo.GetComponent<Node>();
            if (componente != null && Mathf.RoundToInt(componente.position.y) == y)
            {
                resultado.Add(nodo);
            }
        }
        return resultado;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Procesa todos los efectos activos al final del turno
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ProcesarEfectosTurno()
    {
        if (!IsServer) return;

        // Encontrar todos los gestores de efectos
        EfectoPicanteManager[] efectosPicantes = FindObjectsOfType<EfectoPicanteManager>();
        EfectoEspecialManager[] efectosEspeciales = FindObjectsOfType<EfectoEspecialManager>();
        EfectoBlancoManager[] efectosBlancos = FindObjectsOfType<EfectoBlancoManager>();

        // Procesar cada tipo de efecto
        foreach (var efecto in efectosPicantes) efecto.ProcesarTurno();
        foreach (var efecto in efectosEspeciales) efecto.ProcesarTurno();
        foreach (var efecto in efectosBlancos) efecto.ProcesarTurno();

        Debug.Log($"Procesados {efectosPicantes.Length + efectosEspeciales.Length + efectosBlancos.Length} efectos activos");
    }

}

