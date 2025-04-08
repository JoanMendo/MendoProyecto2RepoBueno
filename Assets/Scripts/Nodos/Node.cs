
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
    private GameObject ingrediente;

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
            ingrediente = Instantiate(recurso, center, gameObject.transform.rotation);
            
            if (NetworkManager.Singleton.IsServer)
            {

                ingrediente.GetComponent<Renderer>().material.color = Color.white;

                ingrediente.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                ingrediente.GetComponent<Renderer>().material.color = Color.red;
                SetIngredientServerRpc();
            }

            //Añade el ResourceSO del ingrediente
            currentResource = recurso.GetComponent<ResourcesSO>();

            ingrediente.transform.SetParent(gameObject.transform, true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetIngredientServerRpc()
    {

        if (currentResource != null)
        {
            
        }
    }

}
