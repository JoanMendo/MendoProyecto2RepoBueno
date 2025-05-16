using TMPro;
using UnityEngine;


public class olivaVerdeScript : AbstractIngredient
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
        int olivasNegras = 0; // Bandera para verificar si se encontró un ingrediente en el área de efecto

        for (float i = node.position.x-1; i <= node.position.x+1; i++)
        {
            for (float j = node.position.y - 1; j <= node.position.y+1; j++)
            {
                if (i == node.position.x && j == node.position.y) continue; // Evita aplicar el efecto en el nodo actual

                Node targetNode = nodeMap.GetNodeAtPosition(new Vector2(i, j)); // Obtiene el nodo objetivo
                if (targetNode != null && targetNode.hasIngredient) // Verifica si el nodo objetivo tiene un ingrediente
                {
                    if (targetNode.currentIngredient.TryGetComponent<olivaNegraScript>(out olivaNegraScript olivaNegra))
                    {
                        olivasNegras++; // Se encontró un ingrediente en el área de efecto
                    }
                    


                }
            }
        }
        if (olivasNegras == 1)
        {
            actualValue = initialValue * 4; // Si hay una oliva negra, el valor se duplica  
        }

        textObj.GetComponent<TextMeshPro>().text = actualValue.ToString();
    }
}
  

