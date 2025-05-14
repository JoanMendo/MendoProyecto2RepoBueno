using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ModificadorRecurso : NetworkBehaviour
{
    private ResourcesSO recursoBase;

    // Cola de operaciones pendientes para cuando el objeto esté listo
    private List<Action> operacionesPendientes = new List<Action>();
    private bool esNetworkObjectListo = false;

    // Variables de red para sincronizar modificaciones
    public NetworkVariable<int> modificacionRango = new NetworkVariable<int>(0);
    public NetworkVariable<float> modificacionVida = new NetworkVariable<float>(0f);
    public NetworkVariable<bool> esMovible = new NetworkVariable<bool>(true);

    public void SetRecursoBase(ResourcesSO recurso)
    {
        recursoBase = recurso;
        Debug.Log($"[RANGO] Asignado recurso {(recurso != null ? recurso.Name : "null")} a {gameObject.name}");

        if (recursoBase != null)
        {
            Debug.Log($"[RANGO] Verificación: Rango base = {recursoBase.range}, total = {GetRangoActual()}");
        }
    }

    private void Awake()
    {
        // Suscribirse a cambios en las variables de red
        modificacionRango.OnValueChanged += OnRangoModificado;
        modificacionVida.OnValueChanged += OnVidaModificada;
        esMovible.OnValueChanged += OnMovilidadModificada;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Ahora el NetworkObject está listo para usar NetworkVariables
        esNetworkObjectListo = true;
        Debug.Log($"[NETWORK] NetworkObject inicializado para {gameObject.name}");

        // Ejecutar operaciones pendientes
        if (operacionesPendientes.Count > 0)
        {
            Debug.Log($"[NETWORK] Ejecutando {operacionesPendientes.Count} operaciones pendientes");
            foreach (var operacion in operacionesPendientes)
            {
                operacion.Invoke();
            }
            operacionesPendientes.Clear();
        }
    }

    // Método para encolar operaciones si el objeto no está listo
    private void EjecutarCuandoListo(Action operacion)
    {
        if (esNetworkObjectListo)
        {
            operacion.Invoke();
        }
        else
        {
            Debug.Log($"[NETWORK] Encolando operación para {gameObject.name}");
            operacionesPendientes.Add(operacion);
        }
    }

    // Método para modificar directamente (sin RPC)
    public void AumentarRango(int cantidad)
    {
        if (!IsServer) return;

        modificacionRango.Value += cantidad;
        Debug.Log($"[RANGO] Aumento aplicado a {gameObject.name}: {modificacionRango.Value} (+{cantidad})");
    }

    public void DisminuirRango(int cantidad)
    {
        if (!IsServer) return;

        modificacionRango.Value -= cantidad;
        Debug.Log($"[RANGO] Disminución aplicada a {gameObject.name}: {modificacionRango.Value} (-{cantidad})");
    }

    // Métodos para modificar valores con seguridad (conservamos estos para uso normal)
    [ServerRpc(RequireOwnership = false)]
    public void AumentarRangoServerRpc(int cantidad)
    {
        EjecutarCuandoListo(() => {
            modificacionRango.Value += cantidad;
            Debug.Log($"[RANGO] Aumento aplicado a {gameObject.name}: {modificacionRango.Value} (+{cantidad})");
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void DisminuirRangoServerRpc(int cantidad)
    {
        EjecutarCuandoListo(() => {
            modificacionRango.Value -= cantidad;
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void AumentarVidaServerRpc(float cantidad)
    {
        EjecutarCuandoListo(() => {
            modificacionVida.Value += cantidad;
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void DisminuirVidaServerRpc(float cantidad)
    {
        EjecutarCuandoListo(() => {
            modificacionVida.Value -= cantidad;
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void HacerInmovilServerRpc()
    {
        EjecutarCuandoListo(() => {
            esMovible.Value = false;
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void HacerMovilServerRpc()
    {
        EjecutarCuandoListo(() => {
            esMovible.Value = true;
        });
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

    // Métodos para consultar valores actuales (sin cambios)
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