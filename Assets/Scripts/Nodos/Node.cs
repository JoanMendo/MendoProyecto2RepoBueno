using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Node : NetworkBehaviour, IInteractuable
{
    public Vector2 position; // Posición dentro de la cuadricula
    public bool hasIngredient = false;
    public GameObject currentIngredient; // Prefab del ingrediente que se puede colocar en el nodo


    public void SetNodeIngredient (GameObject prefabingredient)
    {
        hasIngredient = true; // Cambia el estado del nodo a "tiene ingrediente"
        currentIngredient = Instantiate(prefabingredient, gameObject.transform.position, Quaternion.identity);
        NetworkObject nodeNetworkObject = currentIngredient.GetComponent<NetworkObject>(); // Obtiene el componente NetworkObject del ingrediente
        nodeNetworkObject.Spawn(); // Asigna la propiedad del objeto al cliente que posee el nodo
    }

    public void Interactuar()
    {
        if (hasIngredient) return; // Si el nodo ya tiene un ingrediente, no hacer nada
       SetNodeIngredient(GameManager.Instance.currentIngredient); // Asigna el objeto de recurso actual al GameManager
    }
}