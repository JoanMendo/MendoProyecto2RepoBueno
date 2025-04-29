using UnityEngine;
using Unity.Netcode;
public class ReadyButton : NetworkBehaviour, IInteractuable
{

    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false);
    public NetworkVariable<Color> color = new NetworkVariable<Color>();

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
            color.Value = Color.red;
        }
        else
        {
            color.Value = Color.green;
        }
        Object.FindFirstObjectByType<TurnManager>().CheckPlayersReady();
    }
}
