using TMPro;
using UnityEngine;


public class lechugaScript : AbstractIngredient
{
    private void Start()
    {
        initialValue = 1;
        actualValue = 1;
        SetFloatingText(); // Inicializa el texto flotante
    }
    public override void Efecto()
    {
        actualValue = 1;
        Vector2 position = node.position; // Obtiene la posición del nodo
        NodeMap nodeMap = FindFirstObjectByType<NodeMap>();

        for (float i = node.position.x-1; i < node.position.x+1; i++)
        {
            for (float j = node.position.y - 1; j < node.position.y+1; j++)
            {
                if (i == node.position.x && j == node.position.y) continue; // Evita aplicar el efecto en el nodo actual

                Node targetNode = nodeMap.GetNodeAtPosition(new Vector2(i, j)); // Obtiene el nodo objetivo
                if (targetNode != null && targetNode.hasIngredient) // Verifica si el nodo objetivo tiene un ingrediente
                {
                    if (targetNode.currentIngredient.TryGetComponent<lechugaScript>(out lechugaScript lechuga)) // Verifica si el ingrediente es una lechuga
                    {
                        actualValue++;
                    }

                }
            }
        }
        textObj.GetComponent<TextMeshPro>().text = actualValue.ToString();
    }
}
  

