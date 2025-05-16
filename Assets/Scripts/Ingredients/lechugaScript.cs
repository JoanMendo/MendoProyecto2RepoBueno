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
        Debug.Log("Efecto de lechuga activado, valor actial: " +  actualValue);
        actualValue = 1;
        Vector2 position = node.position;
        NodeMap nodeMap = FindFirstObjectByType<NodeMap>();

        for (int i = Mathf.FloorToInt(position.x - 1); i <= Mathf.FloorToInt(position.x + 1); i++)
        {
            for (int j = Mathf.FloorToInt(position.y - 1); j <= Mathf.FloorToInt(position.y + 1); j++)
            {
                if (i == position.x && j == position.y) continue;

                Node targetNode = nodeMap.GetNodeAtPosition(new Vector2(i, j));
                if (targetNode != null && targetNode.hasIngredient)
                {
                    if (targetNode.currentIngredient.TryGetComponent<lechugaScript>(out lechugaScript lechuga))
                    {
                        actualValue++;
                    }
                }
            }
        }

        Debug.Log("Efecto de lechuga activado, valor actial: " + actualValue);
        if (textMeshPro != null)
            textMeshPro.text = actualValue.ToString();
    }
}



