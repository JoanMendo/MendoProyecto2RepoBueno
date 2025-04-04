
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Vector2 position; // Posición dentro de la cuadricula
    public bool hasIngredient = false;



    public void SetIngredient(GameObject recurso)
    {
        if (recurso != null)
        {
            GameObject cilindro = Instantiate(recurso, gameObject.transform.position, gameObject.transform.rotation);
            cilindro.transform.SetParent(gameObject.transform, true);
            //cilindro.transform.localScale = prefabCilindro.transform.localScale;

        }
    }

}
