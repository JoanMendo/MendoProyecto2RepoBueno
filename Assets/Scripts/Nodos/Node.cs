
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Node
{

    public Vector3 position; // Posición en la cuadrícula
    private Dictionary<int, Node> zonas = new Dictionary<int, Node>();
    public List<Node> neighbors = new List<Node>(); // Conexiones con otros nodos
    public bool hasIngredient;
    public BoxCollider collider;

    private ResourcesSO recurso;
    private bool esmovible;

    public Node(Vector2Int pos, Vector3 cellSize, bool walkable, int nodeId)
    { 

        GameObject nodeObject = new GameObject(nodeId.ToString()); // Nombre del GameObject = Número de creación
        nodeObject.transform.position = new Vector3(position.x + 0.5f, position.y + 0.5f, -1); // Centrar el nodo

        nodeObject.tag = "Nodo"; // Asignar la etiqueta "Nodo"
        nodeObject.layer = 7;

        collider = nodeObject.AddComponent<BoxCollider>();


        // Ajustar escala del sprite al tamaño de la celda del Tilemap
        nodeObject.transform.localScale = new Vector3(cellSize.x, cellSize.y, 1);
    }

    

    

    public void SetIngrediente(ResourcesSO ingrediente)
    {
        recurso = ingrediente;


    }
    public void setterZonas(Dictionary<int, Node> posiciones)
    {
        zonas = posiciones;
    }


    public void radio_efecto(int rango,int nivel,string forma)
    {
        for (int i = 0; i < nivel; i++)
        {

            List<int> vecinillos = new List<int>();

            switch (forma)
            {

            }


            for (int j = 0; j < vecinillos.Count; j++)
            {
                if (zonas[vecinillos[i]] != null)
                {
                    neighbors.Add(zonas[vecinillos[i]]);
                }
                else
                {
                    continue;
                }
            }
        }
        
    }

}
