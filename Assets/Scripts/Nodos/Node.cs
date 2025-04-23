using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Node : NetworkBehaviour
{
    public Vector2 position; // Posición dentro de la cuadricula
    public bool hasIngredient = false;

    public void SetNodeIngredient (GameObject prefabingredient)
    {
        hasIngredient = true; // Cambia el estado del nodo a "tiene ingrediente"
        NetworkObject nodeNetworkObject = Instantiate(prefabingredient).GetComponent<NetworkObject>();
        nodeNetworkObject.Spawn(); // Asigna la propiedad del objeto al cliente que posee el nodo
    }
}