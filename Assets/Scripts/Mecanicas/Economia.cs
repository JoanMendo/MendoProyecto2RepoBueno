using UnityEngine;
using Unity.Netcode;

public class Economia : NetworkBehaviour
{
    public NetworkVariable<float> money = new NetworkVariable<float>(1000f);
    private NetworkVariable<int> multiplicador = new NetworkVariable<int>(1);

    public NetworkVariable<ulong> clientId = new NetworkVariable<ulong>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            clientId.Value = OwnerClientId;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddMoneyServerRpc(float incremento)
    {
        // Aplicar multiplicador al incremento
        money.Value += incremento * multiplicador.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubtractMoneyServerRpc(float cantidad)
    {
        money.Value -= cantidad;
        if (money.Value < 0f) money.Value = 0f;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMultiplicadorServerRpc(int nuevoValor)
    {
        multiplicador.Value = nuevoValor;
    }

    // Métodos helper para llamar a los ServerRpc
    public void more_money(float incremento)
    {
        AddMoneyServerRpc(incremento);
    }

    public void less_money(float cantidad)
    {
        Debug.Log($"[ECONOMIA] Restando {cantidad} dinero. Antes: {money.Value}, Después: {money.Value - cantidad}");
        SubtractMoneyServerRpc(cantidad);
    }

    public void SetMultiplicador(int valor)
    {
        SetMultiplicadorServerRpc(valor);
    }

    // Para verificar si hay suficiente dinero
    public bool TieneSuficienteDinero(float cantidad)
    {
        return money.Value >= cantidad;
    }
}