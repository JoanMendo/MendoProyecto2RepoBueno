using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class maizScript : AbstractIngredient
{
    private void Start()
    {
        initialValue = 1;
        actualValue = 1;
        SetFloatingText(); // 
    }

    public override void Efecto()
    {
        initialValue = 1;
        actualValue = initialValue;

        NodeMap nodeMap = FindFirstObjectByType<NodeMap>();
        HashSet<System.Type> tiposUnicos = new HashSet<System.Type>();

        Vector2 position = node.position;

        for (int i = Mathf.FloorToInt(position.x - 1); i <= Mathf.FloorToInt(position.x + 1); i++)
        {
            for (int j = Mathf.FloorToInt(position.y - 1); j <= Mathf.FloorToInt(position.y + 1); j++)
            {
                if (i == (int)position.x && j == (int)position.y) continue;

                Node targetNode = nodeMap.GetNodeAtPosition(new Vector2(i, j));
                if (targetNode != null && targetNode.hasIngredient)
                {
                    AbstractIngredient ingrediente = targetNode.currentIngredient.GetComponent<AbstractIngredient>();
                    if (ingrediente != null)
                    {
                        tiposUnicos.Add(ingrediente.GetType()); // Solo se guarda un tipo único
                    }
                }
            }
        }

        // Suma 1 por cada tipo único (ya incluye initialValue = 1)
        actualValue += tiposUnicos.Count;

        // Actualiza el texto flotante
        if (textMeshPro != null)
        {
            textMeshPro.text = actualValue.ToString();
        }
    }

}
