

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NodeMap : NetworkBehaviour
{
    [Header("Configuración de Cuadrícula")]
    [SerializeField] public GameObject nodePrefab;
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private BoxCollider boundingBox;

    [Header("Depuración")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showVisualDebugHelpers = false;

   
    // Lista de nodos generados
    public List<GameObject> nodesList = new List<GameObject>();

    // Lista de objetos de depuración para poder limpiarlos
    private List<GameObject> debugObjects = new List<GameObject>();

    // ID del cliente propietario (asignado por GlobalGameManager)
    [HideInInspector] public ulong ownerClientId;

    private Economia _economia;
    public Economia economia
    {
        get
        {
            if (_economia == null)
            {
                _economia = GetComponent<Economia>();
            }
            return _economia;
        }
    }

    // Flag para evitar generar nodos múltiples veces
    private bool nodesGenerated = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (showDebugInfo)
            {
                Debug.Log($"NodeMap: OnNetworkSpawn en servidor para tablero de cliente {ownerClientId}");
            }

            // Solo el servidor genera los nodos y solo una vez
            if (!nodesGenerated)
            {
                GenerateGrid();
            }
        }
    }

    // Método para limpiar objetos de depuración si es necesario
    private void ClearDebugObjects()
    {
        foreach (var obj in debugObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        debugObjects.Clear();
    }

    public void GenerateGrid()
    {
        // Verificación crítica para evitar generación múltiple
        if (nodesGenerated)
        {
            Debug.LogWarning($"¡Se intentó generar la cuadrícula dos veces para el cliente {ownerClientId}!");
            return;
        }

        // Limpiar objetos de depuración anteriores
        ClearDebugObjects();

        if (showDebugInfo)
        {
            Debug.Log($"Generando cuadrícula {width}x{height} para cliente {ownerClientId}");
        }

        // NUEVO: Buscar BoxCollider si no está asignado
        if (boundingBox == null)
        {
            // Intentar obtenerlo del mismo objeto
            boundingBox = GetComponent<BoxCollider>();

            // Si aún no lo encuentra, buscarlo en hijos
            if (boundingBox == null)
            {
                boundingBox = GetComponentInChildren<BoxCollider>();
            }

            // Si aún no lo encuentra, intentar en el padre
            if (boundingBox == null && transform.parent != null)
            {
                boundingBox = transform.parent.GetComponent<BoxCollider>();
            }
        }

        // DIAGNÓSTICO DETALLADO para entender el problema
        if (showDebugInfo && boundingBox != null)
        {
            Debug.Log("------ DIAGNÓSTICO DETALLADO DEL TABLERO ------");
            Debug.Log($"NodeMap Transform: Pos={transform.position}, Rot={transform.rotation.eulerAngles}, Scale={transform.localScale}");
            Debug.Log($"BoxCollider Transform: Pos={boundingBox.transform.position}, Rot={boundingBox.transform.rotation.eulerAngles}, Scale={boundingBox.transform.localScale}");
            Debug.Log($"BoxCollider Local: Center={boundingBox.center}, Size={boundingBox.size}");
            Debug.Log($"BoxCollider World: Min={boundingBox.bounds.min}, Max={boundingBox.bounds.max}, Center={boundingBox.bounds.center}");

            // Visualizar punto central y esquinas
            CreateDebugSphere(boundingBox.bounds.center, Color.yellow, 0.3f);
            CreateDebugSphere(boundingBox.bounds.min, Color.red);
            CreateDebugSphere(boundingBox.bounds.max, Color.blue);
            CreateDebugSphere(new Vector3(boundingBox.bounds.min.x, boundingBox.bounds.min.y, boundingBox.bounds.max.z), Color.green);
            CreateDebugSphere(new Vector3(boundingBox.bounds.max.x, boundingBox.bounds.min.y, boundingBox.bounds.min.z), Color.magenta);

            // Dibujar líneas del boundingBox
            Debug.DrawLine(boundingBox.bounds.min,
                          new Vector3(boundingBox.bounds.max.x, boundingBox.bounds.min.y, boundingBox.bounds.min.z),
                          Color.red, 30f);
            Debug.DrawLine(boundingBox.bounds.min,
                          new Vector3(boundingBox.bounds.min.x, boundingBox.bounds.min.y, boundingBox.bounds.max.z),
                          Color.green, 30f);
            Debug.DrawLine(boundingBox.bounds.max,
                          new Vector3(boundingBox.bounds.min.x, boundingBox.bounds.max.y, boundingBox.bounds.max.z),
                          Color.blue, 30f);
            Debug.DrawLine(boundingBox.bounds.max,
                          new Vector3(boundingBox.bounds.max.x, boundingBox.bounds.max.y, boundingBox.bounds.min.z),
                          Color.yellow, 30f);
        }

        // Crear nodos usando INTERPOLACIÓN CORRECTA
        if (boundingBox != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    // Usar interpolación para distribuir uniformemente dentro del collider
                    float percentX = width > 1 ? (float)x / (width - 1) : 0.5f;
                    float percentZ = height > 1 ? (float)z / (height - 1) : 0.5f;

                    // SOLUCIÓN 1: Calcular posición usando interpolación directa
                    Vector3 minPos = boundingBox.bounds.min;
                    Vector3 maxPos = boundingBox.bounds.max;

                    // Calcular posición interpolada en el área del collider
                    Vector3 nodePos = new Vector3(
                        Mathf.Lerp(minPos.x, maxPos.x, percentX),
                        minPos.y, // Mantener altura constante en el mínimo
                        Mathf.Lerp(minPos.z, maxPos.z, percentZ)
                    );

                    string nodeName = $"Node_{x}_{z}";

                    // IMPORTANTE: Usar la rotación del collider
                    GameObject node = Instantiate(nodePrefab, nodePos, boundingBox.transform.rotation);
                    node.name = nodeName;

                    // Configurar el nodo con referencias a su tablero "padre"
                    Node nodeComponent = node.GetComponent<Node>();
                    if (nodeComponent != null)
                    {
                        nodeComponent.position = new Vector2Int(x, z);
                        nodeComponent.Initialize(this);
                    }

                    // Spawn independiente
                    NetworkObject nodeNetObj = node.GetComponent<NetworkObject>();
                    if (nodeNetObj != null)
                    {
                        nodeNetObj.SpawnWithOwnership(ownerClientId);
                    }

                    // Añadir a la lista de nodos
                    nodesList.Add(node);

                    // Añadir elementos visuales de depuración
                    if (showVisualDebugHelpers)
                    {
                        // Cubo para marcar la posición
                        GameObject debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        debugCube.transform.position = nodePos;
                        debugCube.transform.rotation = boundingBox.transform.rotation; // Usar misma rotación
                        debugCube.transform.localScale = Vector3.one * 0.2f;
                        debugCube.GetComponent<Renderer>().material.color = Color.red;
                        debugObjects.Add(debugCube);

                        // Texto para mostrar coordenadas
                        GameObject textObj = new GameObject($"Text_{x}_{z}");
                        TextMesh text = textObj.AddComponent<TextMesh>();
                        text.transform.position = nodePos + Vector3.up * 0.2f;
                        text.text = $"({x},{z})";
                        text.fontSize = 10;
                        text.alignment = TextAlignment.Center;
                        text.anchor = TextAnchor.MiddleCenter;
                        textObj.transform.rotation = Quaternion.LookRotation(textObj.transform.position - Camera.main.transform.position);
                        debugObjects.Add(textObj);

                        if (showDebugInfo)
                            Debug.Log($"Nodo {nodeName} creado en posición mundial {nodePos}, índice ({x},{z})");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("No se pudo encontrar BoxCollider para definir el área del tablero");
        }

        // Marcar como generado
        nodesGenerated = true;

        if (showDebugInfo)
        {
            Debug.Log($"Cuadrícula generada con éxito: {nodesList.Count} nodos para cliente {ownerClientId}");
        }
    }

    // Método auxiliar para crear esferas de depuración
    private void CreateDebugSphere(Vector3 position, Color color, float scale = 0.2f)
    {
        if (!showVisualDebugHelpers) return;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * scale;
        sphere.GetComponent<Renderer>().material.color = color;
        debugObjects.Add(sphere);
    }

    //  Método para obtener un nodo en una posición específica
    public GameObject GetNodeAtPosition(Vector2Int position)
    {
        foreach (GameObject node in nodesList)
        {
            Node nodeComp = node.GetComponent<Node>();
            if (nodeComp != null &&
                nodeComp.position.x == position.x &&
                nodeComp.position.y == position.y)
            {
                return node;
            }
        }

        // Si no se encuentra, registrar para depuración
        if (showDebugInfo)
        {
            Debug.LogWarning($"No se encontró nodo en posición ({position.x}, {position.y})");
        }
        return null;
    }

    //  Método para obtener nodos adyacentes con manejo adecuado de tipos
    public List<GameObject> GetAdjacentNodes(GameObject centerNode, int radius = 1)
    {
        List<GameObject> adjacentNodes = new List<GameObject>();
        Node centerNodeComp = centerNode.GetComponent<Node>();

        if (centerNodeComp == null)
        {
            if (showDebugInfo)
                Debug.LogError("GetAdjacentNodes: El nodo central no tiene componente Node");
            return adjacentNodes;
        }

        if (showDebugInfo)
            Debug.Log($"Buscando nodos adyacentes a ({centerNodeComp.position.x}, {centerNodeComp.position.y}) con radio {radius}");

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                // Saltar el nodo central
                if (dx == 0 && dy == 0) continue;

                // Asegurarnos que trabajamos con enteros
                int posX = centerNodeComp.position.x + dx;
                int posY = centerNodeComp.position.y + dy;

                Vector2Int adjacentPos = new Vector2Int(posX, posY);

                GameObject adjacentNode = GetNodeAtPosition(adjacentPos);
                if (adjacentNode != null)
                {
                    adjacentNodes.Add(adjacentNode);

                    if (showDebugInfo)
                        Debug.Log($"Nodo adyacente encontrado en ({adjacentPos.x}, {adjacentPos.y})");
                }
            }
        }

        if (showDebugInfo)
            Debug.Log($"Total de nodos adyacentes encontrados: {adjacentNodes.Count}");

        return adjacentNodes;
    }

    // Método para DEBUG: Visualizar toda la cuadrícula
    public void VisualizeGrid()
    {
        if (!showVisualDebugHelpers) return;

        ClearDebugObjects();

        foreach (GameObject node in nodesList)
        {
            Node nodeComp = node.GetComponent<Node>();
            if (nodeComp != null)
            {
                GameObject debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debugCube.transform.position = node.transform.position;
                debugCube.transform.rotation = node.transform.rotation;
                debugCube.transform.localScale = Vector3.one * 0.2f;
                debugCube.GetComponent<Renderer>().material.color = Color.green;
                debugObjects.Add(debugCube);
            }
        }
    }


    // Método para activar todos los efectos en este tablero
    public void ActivarTodosLosEfectos()
    {
        if (!IsServer) return;

        // Recorrer todos los nodos y activar los efectos de sus ingredientes
        foreach (var nodoObj in nodesList)
        {
            // Obtener el componente Node (en lugar de asumir que nodesList contiene Nodes)
            Node nodo = nodoObj.GetComponent<Node>();

            if (nodo != null && nodo.hasIngredient.Value)
            {
                ActivarEfectoIngrediente(nodo);
            }
        }
    }

    /// En la clase NodeMap
    private void ActivarEfectoIngrediente(Node nodo)
    {
        if (nodo == null || !nodo.hasIngredient.Value || nodo.currentIngredient == null)
            return;

        // Obtener el tipo de ingrediente directamente del componente ResourcesSO
        componente recursoIngrediente = nodo.currentIngredient.GetComponent<componente>();
        if (recursoIngrediente == null)
        {
            Debug.LogWarning("El ingrediente no tiene componente ResourcesSO");
            return;
        }

        string tipoIngrediente = recursoIngrediente.data.name;

        // Obtener el ScriptableObject del ingrediente usando IngredientManager
        IngredientesSO ingredienteSO = IngredientManager.Instance.GetIngredienteByName(tipoIngrediente);
        if (ingredienteSO == null)
        {
            Debug.LogWarning($"No se encontró IngredientesSO para {tipoIngrediente}");
            return;
        }

        // Calcular nodos afectados
        List<GameObject> nodosAfectados = ingredienteSO.CalcularNodosAfectados(nodo.gameObject, this);

        // Crear gestor de efecto
        GameObject gestorObj = IngredientManager.Instance.CrearGestorEfecto(tipoIngrediente.ToLower());
        if (gestorObj != null)
        {
            IEffectManager gestor = gestorObj.GetComponent<IEffectManager>();
            if (gestor != null)
            {
                gestor.ConfigurarConIngrediente(ingredienteSO);
                gestor.IniciarEfecto(nodo.gameObject, nodosAfectados);
            }
            else
            {
                Debug.LogError($"El gestor para {tipoIngrediente} no implementa IEffectManager");
                Destroy(gestorObj);
            }
        }
    }
}