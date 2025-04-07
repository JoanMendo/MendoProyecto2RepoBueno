
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Vector2 position; // Posición dentro de la cuadricula
    public bool hasIngredient = false;
    BoxCollider boxCollider;

    public void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

    }


    public void SetIngredient(GameObject recurso)
    {
        if (recurso != null)
        {
            Vector3 center = new Vector3(boxCollider.bounds.center.x, boxCollider.bounds.max.y, boxCollider.bounds.center.z);

            GameObject cilindro = Instantiate(recurso, center, gameObject.transform.rotation);
            cilindro.transform.SetParent(gameObject.transform, true);


            Application.Quit();


        }
    }

}
