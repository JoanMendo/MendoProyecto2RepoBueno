using TMPro;
using UnityEngine;


public class cebollaScript : AbstractIngredient
{
    private void Start()
    {
        initialValue = 2;
        actualValue = 2;
        SetFloatingText(); 
    }
    public override void Efecto()
    {
        actualValue = 2;
        Vector2 position = node.position; // Obtiene la posición del nodo
        NodeMap nodeMap = FindFirstObjectByType<NodeMap>();
        bool foundIngredient = false; // Bandera para verificar si se encontró un ingrediente en el área de efecto

        for (float i = node.position.x-1; i <= node.position.x+1; i++)
        {
            for (float j = node.position.y - 1; j <= node.position.y+1; j++)
            {
                if (i == node.position.x && j == node.position.y) continue; // Evita aplicar el efecto en el nodo actual

                Node targetNode = nodeMap.GetNodeAtPosition(new Vector2(i, j)); // Obtiene el nodo objetivo
                if (targetNode != null && targetNode.hasIngredient) // Verifica si el nodo objetivo tiene un ingrediente
                {
                    actualValue = initialValue/2; // Reinicia el valor actual al valor inicial dividido por 2
                    foundIngredient = true; // Se encontró un ingrediente en el área de efecto
                    break;

                }
            }
        }
        // Si no se encontró un nodo objetivo con un ingrediente, el valor inicial se triplica
        if (!foundIngredient)
            actualValue = initialValue * 3;

        textObj.GetComponent<TextMeshPro>().text = actualValue.ToString();
    }
}
  

