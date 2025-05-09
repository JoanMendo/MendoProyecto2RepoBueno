using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Clase base para todos los recursos del juego (ingredientes, utensilios y efectos).
/// Implementada como ScriptableObject para fácil configuración y reutilización.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "resource", menuName = "CookingGame/Resources/Base")]
public abstract class ResourcesSO : ScriptableObject
{
    [Header("Información Básica")]
    [Tooltip("Identificador único del recurso")]
    public string resourceID;

    [Tooltip("Nombre visible del recurso")]
    public string Name;

    [Tooltip("Descripción del recurso")]
    [TextArea(3, 5)]
    public string Description;

    [Header("Economía y Límites")]
    [Tooltip("Costo del recurso en monedas")]
    public float Price;

    [Tooltip("Límite máximo de este recurso por jugador")]
    public int Limit = -1; // -1 significa sin límite

    [Header("Propiedades de Juego")]
    [Tooltip("Rango de efecto del recurso")]
    public int range = 1;

    [Tooltip("Nivel o tier del recurso")]
    public int nivel = 1;

    [Tooltip("Puntos de resistencia del recurso")]
    public float vida = 1f;

    [Tooltip("Forma del área de efecto: 'cruz', 'x', 'cuadrado'")]
    public string forma = "cuadrado";

    [Tooltip("Si el recurso puede ser movido después de colocado")]
    public bool esmovible = false;

    [Tooltip("Niveles de rango que serán ignorados por el efecto")]
    public List<int> niveles_ignorar = new List<int>();

    [Header("Representación Visual")]
    [Tooltip("Sprite 2D del recurso para UI")]
    public Sprite Sprite;

    [Tooltip("Prefab 3D del recurso para el tablero")]
    public GameObject prefab3D;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Método para activar el efecto específico del recurso.
    /// Implementado por cada tipo de recurso concreto.
    /// ‡‡</summary>_PLACEHOLDER‡‡
    /// <param name="nodoOrigen">Nodo donde se origina el efecto</param>
    /// <param name="nodeMap">Mapa de nodos para localizar vecinos</param>
    public abstract void ActivarEfecto(GameObject nodoOrigen, NodeMap nodeMap);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Calcula los nodos afectados por este recurso según su forma y rango.
    /// ‡‡</summary>_PLACEHOLDER‡‡
    /// <param name="nodoOrigen">Nodo origen del efecto</param>
    /// <param name="nodeMap">Mapa de nodos para búsqueda</param>
    /// <returns>Lista de nodos afectados</returns>
    public virtual List<GameObject> CalcularNodosAfectados(GameObject nodoOrigen, NodeMap nodeMap)
    {
        List<GameObject> nodosAfectados = new List<GameObject>();

        // Si no hay nodo origen o mapa, devolver lista vacía
        if (nodoOrigen == null || nodeMap == null)
            return nodosAfectados;

        Node origen = nodoOrigen.GetComponent<Node>();
        if (origen == null)
            return nodosAfectados;

        // Obtener posición del nodo origen
        Vector2 posicionOrigen = origen.position;

        // Recorrer todos los nodos del mapa
        foreach (var nodo in nodeMap.nodesList)
        {
            Node componenteNodo = nodo.GetComponent<Node>();
            if (componenteNodo == null) continue;

            // No incluir el nodo origen
            if (componenteNodo.position == posicionOrigen) continue;

            // Calcular distancia en coordenadas de rejilla
            float dx = Mathf.Abs(componenteNodo.position.x - posicionOrigen.x);
            float dy = Mathf.Abs(componenteNodo.position.y - posicionOrigen.y);

            // Calcular distancia según tipo de medición
            float distancia = Mathf.Max(dx, dy); // Distancia "chebyshev" para grids

            // Verificar si está en rango
            if (distancia > range) continue;

            // Verificar si el nivel debe ser ignorado
            if (niveles_ignorar.Contains(Mathf.RoundToInt(distancia))) continue;

            // Filtrar según la forma requerida
            bool incluir = false;

            switch (forma.ToLower())
            {
                case "cruz":
                    // Solo arriba, abajo, izquierda, derecha
                    incluir = (dx == 0 || dy == 0) && !(dx == 0 && dy == 0);
                    break;

                case "x":
                    // Solo diagonales
                    incluir = (dx == dy) && dx > 0;
                    break;

                case "cuadrado":
                    // Todos los nodos en rango
                    incluir = true;
                    break;

                case "utensilio":
                    // Solo arriba y abajo (caso específico)
                    incluir = (dx == 0 && dy > 0);
                    break;

                default:
                    Debug.LogWarning($"Forma '{forma}' no reconocida para {Name}");
                    break;
            }

            if (incluir)
            {
                nodosAfectados.Add(nodo);
            }
        }

        return nodosAfectados;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Método para ordenar los nodos afectados según el tipo de efecto
    /// ‡‡</summary>_PLACEHOLDER‡‡
    protected virtual List<GameObject> OrdenarNodosAfectados(List<GameObject> nodos, Vector2 origen)
    {
        if (forma == "cuadrado")
        {
            // Ordenar por ángulo alrededor del origen
            nodos.Sort((a, b) => {
                Vector2 posA = a.GetComponent<Node>().position;
                Vector2 posB = b.GetComponent<Node>().position;

                Vector2 dirA = posA - origen;
                Vector2 dirB = posB - origen;

                float angleA = Mathf.Atan2(dirA.y, dirA.x);
                float angleB = Mathf.Atan2(dirB.y, dirB.x);

                return angleA.CompareTo(angleB);
            });
        }
        else if (forma == "cruz")
        {
            // Ordenar por dirección específica: abajo, izquierda, derecha, arriba
            nodos.Sort((a, b) => {
                Vector2 posA = a.GetComponent<Node>().position;
                Vector2 posB = b.GetComponent<Node>().position;

                int prioridadA = GetCruzPrioridad(posA, origen);
                int prioridadB = GetCruzPrioridad(posB, origen);

                return prioridadA.CompareTo(prioridadB);
            });
        }

        return nodos;
    }

    // Prioridad para ordenamiento "cruz"
    private int GetCruzPrioridad(Vector2 pos, Vector2 origen)
    {
        if (pos.y > origen.y) return 0; // Abajo (mayor prioridad)
        if (pos.x < origen.x) return 1; // Izquierda
        if (pos.x > origen.x) return 2; // Derecha
        return 3; // Arriba (menor prioridad)
    }
}