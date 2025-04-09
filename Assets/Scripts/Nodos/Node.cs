using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Node : NetworkBehaviour
{
    public Vector2 position; // Posición dentro de la cuadricula
    public bool hasIngredient = false;
    private BoxCollider boxCollider;
    private ResourcesSO currentResource;
    private GameObject ingrediente;

    // NetworkVariable para sincronizar el color
    private NetworkVariable<Color> cubeColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse a cambios en el color
        cubeColor.OnValueChanged += OnColorChanged;

        // Aplicar el color actual si ya tiene valor
        if (ingrediente != null)
        {
            ingrediente.GetComponent<Renderer>().material.color = cubeColor.Value;
        }
    }

    private void OnColorChanged(Color previous, Color current)
    {
        if (ingrediente != null)
        {
            ingrediente.GetComponent<Renderer>().material.color = current;
        }
    }

    public void SetIngredient(GameObject recurso)
    {
        ingrediente = recurso;
        SetIngredientServerRpc(); // Cambiado el nombre aquí también para mantener consistencia
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetIngredientServerRpc() // Nombre corregido
    {
        if (ingrediente != null)
        {
            // Instancia el modelo del ingrediente
            Vector3 center = new Vector3(boxCollider.bounds.center.x, boxCollider.bounds.max.y, boxCollider.bounds.center.z);
            ingrediente = Instantiate(ingrediente, center, gameObject.transform.rotation);

            Random.InitState((int)NetworkManager.Singleton.LocalClientId);
            Color randomColor = Random.ColorHSV();

            // Asignar el color inicial
            cubeColor.Value = randomColor;
            ingrediente.GetComponent<Renderer>().material.color = randomColor;

            ingrediente.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);

            currentResource = ingrediente.GetComponent<ResourcesSO>();
            ingrediente.transform.SetParent(gameObject.transform, true);
        }
    }

    public void SetColorForAllPlayers(Color newColor)
    {
        if (IsOwner)
        {
            // Solo el dueño puede cambiar el valor, lo que activará la sincronización
            cubeColor.Value = newColor;

            // Actualización local inmediata
            if (ingrediente != null)
            {
                ingrediente.GetComponent<Renderer>().material.color = newColor;
            }
        }
        else
        {
            // Si no es el dueño, solicitar el cambio al dueño
            RequestColorChangeServerRpc(newColor);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestColorChangeServerRpc(Color newColor, ServerRpcParams rpcParams = default)
    {
        // En Distributed Authority, el servidor puede forzar el cambio de dueño o aplicar el cambio directamente
        cubeColor.Value = newColor;
    }
}