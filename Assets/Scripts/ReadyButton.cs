using UnityEngine;
using Unity.Netcode;
public class ReadyButton : NetworkBehaviour, IInteractuable
{

    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner
);



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
       isReady.Value = !isReady.Value;
       

    }

    public void OnReadyChanged(bool oldvalue, bool newValue)
    {
        if (isReady.Value)
        {

            gameObject.GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            gameObject.gameObject.GetComponent<Renderer>().material.color = Color.green;
        }
        Object.FindFirstObjectByType<TurnManager>().CheckPlayersReady();
    }
}
