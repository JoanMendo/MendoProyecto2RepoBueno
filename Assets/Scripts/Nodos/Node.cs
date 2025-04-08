
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Vector2 position; // Posición dentro de la cuadricula
    public bool hasIngredient = false;
    private BoxCollider boxCollider;
    private ResourcesSO currentResource;

    public void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

    }


    public void SetIngredient(GameObject recurso)
    {
        if (recurso != null)
        {
            //Instancia el modelo del ingrediente
            Vector3 center = new Vector3(boxCollider.bounds.center.x, boxCollider.bounds.max.y, boxCollider.bounds.center.z);
            GameObject cilindro = Instantiate(recurso, center, gameObject.transform.rotation);
            cilindro.transform.SetParent(gameObject.transform, true);
            if (NetworkManager.Singleton.IsServer)
            {
                cilindro.GetComponent<Renderer>().material.color = Color.white;
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                cilindro.GetComponent<Renderer>().material.color = Color.red;
            }

            //Añade el ResourceSO del ingrediente
            currentResource = recurso.GetComponent<ResourcesSO>();
        }
    }

}
