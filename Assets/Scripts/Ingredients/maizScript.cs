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
        actualValue = 1;
        Vector2 position = node.position;
        NodeMap nodeMap = FindFirstObjectByType<NodeMap>();
        HashSet<System.Type> tiposUnicos = new HashSet<System.Type>(); // Para evitar tipos repetidos

        for (float i = node.position.x - 1; i <= node.position.x + 1; i++)
        {
            for (float j = node.position.y - 1; j <= node.position.y + 1; j++)
            {
                if (i == node.position.x && j == node.position.y) continue;

                Node targetNode = nodeMap.GetNodeAtPosition(new Vector2(i, j));
                if (targetNode != null && targetNode.hasIngredient)
                {
                    AbstractIngredient ingrediente = targetNode.currentIngredient.GetComponent<AbstractIngredient>();
                    if (ingrediente != null)
                    {
                        System.Type tipo = ingrediente.GetType();
                        if (!tiposUnicos.Contains(tipo))
                        {
                            tiposUnicos.Add(tipo);
                            // Aquí puedes aplicar lógica específica si quieres
                            Debug.Log($"Ingrediente único encontrado: {tipo.Name}");
                        }
                    }
                }
            }
        }

        foreach (System.Type tipo in tiposUnicos)
        {
                actualValue = initialValue ++ ; // Si hay un maíz, el valor se duplica
                break; // Salimos del bucle si encontramos un maíz
            
        }

        textObj.GetComponent<TextMeshPro>().text = actualValue.ToString();
    }
}
