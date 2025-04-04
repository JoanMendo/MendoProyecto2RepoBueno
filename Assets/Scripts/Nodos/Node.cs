
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Vector2 position; // Posición dentro de la cuadricula
    public bool hasIngredient = false;
    public GameObject recurso;



    public void SetIngredient(GameObject recurso)
    {
        if (recurso != null)
        {
            hasIngredient = true;
            GameObject ingredient = Instantiate(recurso, transform.position, Quaternion.identity);
            ingredient.transform.SetParent(transform);
            ingredient.transform.localPosition = Vector3.zero;
        }
    }

}
