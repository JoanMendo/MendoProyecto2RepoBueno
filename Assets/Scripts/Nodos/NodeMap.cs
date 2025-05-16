
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Unity.Netcode;

public class NodeMap : NetworkBehaviour
{

    public GameObject map;
    public GameObject nodePrefab;
    public List<GameObject> nodesList;
    public int width;
    public int height;

    void Start()
    {
        if (map == null)
        {
            map = gameObject;
        }

    }
    public void Generate3DTilemap()
    {
        BoxCollider boxCollider = map.GetComponent<BoxCollider>();

        // Obtener el tamaño real del BoxCollider del mapa en coordenadas globales
        Vector3 colliderSize = boxCollider.bounds.size;

        // Calcular el tamaño de cada celda dentro del BoxCollider
        Vector3 cellSize = new Vector3(colliderSize.x / width, colliderSize.y, colliderSize.z / height);


        // Obtener la esquina inferior izquierda del mapa en X y Z
        Vector3 startPosition = new Vector3(boxCollider.bounds.min.x + cellSize.x / 2,
                                              boxCollider.bounds.max.y,
                                              boxCollider.bounds.min.z + cellSize.z / 2);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Crear la casilla
                GameObject casilla = Instantiate(nodePrefab, map.transform);

                // Asegurar que la rotación de la casilla coincida con la del mapa
                casilla.transform.rotation = map.transform.rotation;

                // Obtener el BoxCollider de la casilla
                BoxCollider casillaCollider = casilla.GetComponent<BoxCollider>();
                if (casillaCollider == null)
                {
                    continue;   
                }

                // Obtener el tamaño real del BoxCollider de la casilla
                Vector3 casillaColliderSize = casillaCollider.bounds.size;

                // Ajustar la escala de la casilla para que encaje en el tamaño de celda
                Vector3 adjustedScale = new Vector3(
                    cellSize.x / casillaColliderSize.x,
                    cellSize.y / casillaColliderSize.y,
                    cellSize.z / casillaColliderSize.z
                );

                // Aplicar la escala ajustada
                casilla.transform.localScale = adjustedScale * 1.1f;

                Vector3 position = startPosition + new Vector3(x * cellSize.x,
                                                               0f, // Mantener la altura fija, ya que se está ajustando la escala
                                                               y * cellSize.z);

                // Asignar la posición en coordenadas globales
                casilla.transform.position = position;

                casilla.GetComponent<Node>().position = new Vector2(x, y);
                nodesList.Add(casilla);
            }
        }
    }

    public Node GetNodeAtPosition(Vector2 position)
    {
        foreach (GameObject node in nodesList)
        {
            Node nodeComponent = node.GetComponent<Node>();
            if (nodeComponent.position == position)
            {
                return nodeComponent;
            }
        }
        return null; // Si no se encuentra el nodo, devuelve null
    }

    public void ExecuteAllNodeIngredientEffects()
    {
        foreach (GameObject node in nodesList)
        {
            Node nodeComponent = node.GetComponent<Node>();
            if (nodeComponent.hasIngredient)
            {
                nodeComponent.currentIngredient.GetComponent<AbstractIngredient>().Efecto();
            }
        }
    }

}

