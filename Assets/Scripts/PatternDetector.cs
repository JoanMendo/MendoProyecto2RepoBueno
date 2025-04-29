using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[System.Serializable]
public class FormationPattern
{
    public string Name;                       // Nombre del patr�n
    public string[] RequiredElements;         // Ingredientes necesarios
    public Vector2[] Positions;            // Posiciones relativas
     // Efecto a aplicar cuando se detecta

    // Constructor para patrones basados en posiciones relativas e ingredientes
    public FormationPattern(string name, string[] requiredElements, Vector2[] positions)
    {
        Name = name;
        RequiredElements = requiredElements;
        Positions = positions;

    }
}

public class PatternDetector : MonoBehaviour
{
    [SerializeField] private List<FormationPattern> patterns = new List<FormationPattern>();

    private void Awake()
    {
        // Inicializar los patrones predefinidos
        InitializePatterns();
    }

    private void InitializePatterns()
    {
        // Define tus patrones aqu� usando vectores de posiciones relativas
        patterns.Add(new FormationPattern(
            name: "L�nea Horizontal",
            requiredElements: new string[] { "tomate", "tomate", "tomate" },
            positions: new Vector2[] {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 0)
            }

        ));

        patterns.Add(new FormationPattern(
            name: "Cuadrado",
            requiredElements: new string[] { "lechuga", "lechuga", "lechuga", "lechuga" },
            positions: new Vector2[] {
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 1), new Vector2(1, 1)
            }

        ));

        patterns.Add(new FormationPattern(
            name: "L Shape",
            requiredElements: new string[] { "cebolla", "cebolla", "cebolla", "cebolla" },
            positions: new Vector2[] {
                new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(0, 2), new Vector2(1, 0)
            }
        ));

        // A�ade m�s patrones seg�n necesites
    }

    // M�todo principal que se llamar� desde NodeMap
    public void DetectarFormaciones(List<GameObject> nodes)
    {

        foreach (var pattern in patterns)
        {
            DetectarPatron(pattern, nodes);
        }
    }

    // M�todo para detectar un patr�n espec�fico
    private void DetectarPatron(FormationPattern pattern, List<GameObject> nodes)
    {
        // Recorremos cada nodo como posible origen del patr�n
        foreach (var startNodeEntry in nodes)
        {
            Node startNode = startNodeEntry.GetComponent<Node>();
            if (!startNode.hasIngredient)
                continue;



            // Solo comprobamos si este nodo puede ser parte del patr�n buscado
            if (!IsValidIngredientForPattern(startNode.currentIngredient.GetComponent<ResourcesSO>().Name, pattern.RequiredElements))
                continue;

            // Intentamos encontrar el patr�n comenzando desde este nodo
            TryMatchPatternFromNode(startNode, pattern, nodes);
        }
    }

    // Intenta hacer coincidir un patr�n desde un nodo espec�fico
    private void TryMatchPatternFromNode(Node startNode, FormationPattern pattern, List<GameObject> nodes)
    {
        // Para cada orientaci�n posible (0�, 90�, 180�, 270�)
        for (int rotation = 0; rotation < 4; rotation++)
        {
            // Tambi�n comprobamos reflejos (para cada rotaci�n)
            for (int reflection = 0; reflection < 2; reflection++)
            {
                List<Node> matchingNodes = new List<Node>();
                matchingNodes.Add(startNode); // El nodo inicial siempre forma parte
                bool patternFound = true;

                // Verificamos cada posici�n relativa en este patr�n (excepto la primera que es 0,0)
                for (int i = 1; i < pattern.Positions.Length; i++)
                {
                    Vector2 relPos = pattern.Positions[i];

                    // Aplicar rotaci�n y reflexi�n si es necesario
                    Vector2 transformedPos = TransformPosition(relPos, rotation, reflection == 1);

                    // Calcular la posici�n real en el tablero
                    Vector2 targetPos = startNode.position + transformedPos;

                    // Buscar el nodo en esa posici�n
                    Node targetNode = FindNodeAtPosition(targetPos, nodes);

                    // Verificar si existe un nodo ah� y si tiene el ingrediente correcto
                    if (targetNode == null || !targetNode.currentIngredient)
                    {
                        patternFound = false;
                        break;
                    }

                    ResourcesSO targetResource = targetNode.GetComponent<ResourcesSO>(); 
                    if (targetResource == null || !IsValidIngredientForPattern(targetResource.Name, pattern.RequiredElements))
                    {
                        patternFound = false;
                        break;
                    }

                    matchingNodes.Add(targetNode);
                }

            }
        }
    }

    // Transforma una posici�n relativa aplicando rotaci�n y/o reflexi�n
    private Vector2 TransformPosition(Vector2 position, int rotation, bool reflection)
    {
        Vector2 transformed = position;

        // Aplicar reflexi�n (sobre el eje Y) si se requiere
        if (reflection)
        {
            transformed.x = -transformed.x;
        }

        // Aplicar rotaci�n (en incrementos de 90 grados)
        for (int i = 0; i < rotation; i++)
        {
            // Rotaci�n de 90 grados en sentido horario: (x,y) -> (y,-x)
            float temp = transformed.x;
            transformed.x = transformed.y;
            transformed.y = -temp;
        }

        return transformed;
    }

    private Node FindNodeAtPosition(Vector2 position, List<GameObject> nodes)
    {
        foreach (var node in nodes)
        {
            Node nodeComponent = node.GetComponent<Node>();
            if (nodeComponent.position == position)
            {
                return nodeComponent;
            }
        }
        return null;
    }


    // Verifica si un ingrediente es v�lido para el patr�n
    private bool IsValidIngredientForPattern(string ingredientName, string[] requiredElements)
    {
        // Si el patr�n requiere ingredientes espec�ficos, verificar que coincida
        if (requiredElements != null && requiredElements.Length > 0)
        {
            return Array.Exists(requiredElements, element => element == ingredientName);
        }

        // Si no hay requisitos espec�ficos, cualquier ingrediente es v�lido
        return true;
    }

 
}