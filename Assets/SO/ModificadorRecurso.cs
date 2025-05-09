using UnityEngine;
using Unity.Netcode;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona modificaciones dinámicas a recursos (rango, movilidad, etc).
/// ‡‡</summary>_PLACEHOLDER‡‡
public class ModificadorRecurso : NetworkBehaviour
{
    private ResourcesSO recursoBase;

    // Variables de red para sincronizar modificaciones
    public NetworkVariable<int> modificacionRango = new NetworkVariable<int>(0);
    public NetworkVariable<float> modificacionVida = new NetworkVariable<float>(0f);
    public NetworkVariable<bool> esMovible = new NetworkVariable<bool>(true);

    private void Awake()
    {
        recursoBase = GetComponent<ResourcesSO>();

        // Suscribirse a cambios en las variables de red
        modificacionRango.OnValueChanged += OnRangoModificado;
        modificacionVida.OnValueChanged += OnVidaModificada;
        esMovible.OnValueChanged += OnMovilidadModificada;
    }

    public void AumentarRango(int cantidad)
    {
        if (!IsServer) return;
        modificacionRango.Value += cantidad;
    }

    public void DisminuirRango(int cantidad)
    {
        if (!IsServer) return;
        modificacionRango.Value -= cantidad;
    }

    public void AumentarVida(float cantidad)
    {
        if (!IsServer) return;
        modificacionVida.Value += cantidad;
    }

    public void DisminuirVida(float cantidad)
    {
        if (!IsServer) return;
        modificacionVida.Value -= cantidad;
    }

    public void HacerInmovil()
    {
        if (!IsServer) return;
        esMovible.Value = false;
    }

    public void HacerMovil()
    {
        if (!IsServer) return;
        esMovible.Value = true;
    }

    // Eventos de respuesta a cambios
    private void OnRangoModificado(int valorAnterior, int nuevoValor)
    {
        if (recursoBase != null)
        {
            Debug.Log($"Rango de {recursoBase.Name} modificado de {recursoBase.range + valorAnterior} a {recursoBase.range + nuevoValor}");
        }
    }

    private void OnVidaModificada(float valorAnterior, float nuevoValor)
    {
        if (recursoBase != null)
        {
            Debug.Log($"Vida de {recursoBase.Name} modificada de {recursoBase.vida + valorAnterior} a {recursoBase.vida + nuevoValor}");
        }
    }

    private void OnMovilidadModificada(bool valorAnterior, bool nuevoValor)
    {
        if (recursoBase != null)
        {
            Debug.Log($"Movilidad de {recursoBase.Name} cambiada a {nuevoValor}");
        }
    }

    // Métodos para consultar valores actuales
    public int GetRangoActual()
    {
        if (recursoBase == null) return 0;
        return recursoBase.range + modificacionRango.Value;
    }

    public float GetVidaActual()
    {
        if (recursoBase == null) return 0f;
        return recursoBase.vida + modificacionVida.Value;
    }

    public bool EsMovible()
    {
        return esMovible.Value;
    }
}