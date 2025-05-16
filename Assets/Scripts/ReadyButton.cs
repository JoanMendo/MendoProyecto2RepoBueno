using UnityEngine;
using Unity.Netcode;

public class ReadyButton : NetworkBehaviour, IInteractuable
{
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    [ServerRpc(RequireOwnership = false)]
    public void RequestSetReadyServerRpc(bool value)
    {
        // Solo el servidor puede ejecutar esto
        isReady.Value = value;
    }

    public void Awake()
    {
        isReady.OnValueChanged += OnReadyChanged;
    }

    public void OnDisable()
    {
        isReady.OnValueChanged -= OnReadyChanged;
    }

    public void Interactuar()
    {
        if (IsOwner)
        {
            // Si somos el dueño, cambiamos directamente
            isReady.Value = !isReady.Value;
        }
        else
        {
            // Si no somos el dueño, solicitamos el cambio al servidor
            RequestSetReadyServerRpc(!isReady.Value);
        }

        // Notificar al TurnManager (esto debería moverse al OnValueChanged)
        Object.FindFirstObjectByType<TurnManager>()?.CheckPlayersReady();
    }

    public void TryChangeReady()
    {
        if (IsOwner)
        {
            // Si somos el dueño, cambiamos directamente
            isReady.Value = false;
        }
        else
        {
            // Si no somos el dueño, solicitamos el cambio al servidor
            RequestSetReadyServerRpc(false);
        }
    }

    public void OnReadyChanged(bool oldvalue, bool newValue)
    {
        // Actualizar la apariencia visual
        var renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = newValue ? Color.red : Color.green;
        }

        // Notificar al TurnManager cuando cambia el estado
        Object.FindFirstObjectByType<TurnManager>()?.CheckPlayersReady();
    }
}