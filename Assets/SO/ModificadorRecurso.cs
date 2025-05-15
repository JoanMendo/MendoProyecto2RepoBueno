using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Component that manages network synchronized modifications to resources like ingredients.
/// Handles range, life, and movement capability modifications.
/// </summary>
public class ModificadorRecurso : NetworkBehaviour
{
    // Reference to the base resource being modified
    private ResourcesSO recursoBase;

    // Queue of operations pending for when the NetworkObject is ready
    private List<Action> operacionesPendientes = new List<Action>();

    // Flag to track if the NetworkObject is ready for variable modifications
    private bool esNetworkObjectListo = false;

    // Network synchronized variables for modifications
    public NetworkVariable<int> modificacionRango = new NetworkVariable<int>(0);
    public NetworkVariable<float> modificacionVida = new NetworkVariable<float>(0f);
    public NetworkVariable<bool> esMovible = new NetworkVariable<bool>(true);

    // Debug flag for troubleshooting
    [SerializeField] private bool mostrarDebug = false;

    /// <summary>
    /// Sets the base resource this component will modify
    /// </summary>
    public void SetRecursoBase(ResourcesSO recurso)
    {
        recursoBase = recurso;
        if (mostrarDebug)
        {
            Debug.Log($"[MODIFICADOR] Asignado recurso {(recurso != null ? recurso.Name : "null")} a {gameObject.name}");

            if (recursoBase != null)
            {
                Debug.Log($"[MODIFICADOR] Verificación: Rango base = {recursoBase.range}, total = {GetRangoActual()}");
            }
        }
    }

    private void Awake()
    {
        // Subscribe to network variable changes
        modificacionRango.OnValueChanged += OnRangoModificado;
        modificacionVida.OnValueChanged += OnVidaModificada;
        esMovible.OnValueChanged += OnMovilidadModificada;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Now the NetworkObject is ready to use NetworkVariables
        esNetworkObjectListo = true;

        if (mostrarDebug)
        {
            Debug.Log($"[NETWORK] NetworkObject inicializado para {gameObject.name}");
        }

        // Execute any pending operations
        if (operacionesPendientes.Count > 0)
        {
            if (mostrarDebug)
            {
                Debug.Log($"[NETWORK] Ejecutando {operacionesPendientes.Count} operaciones pendientes");
            }

            foreach (var operacion in operacionesPendientes)
            {
                operacion.Invoke();
            }
            operacionesPendientes.Clear();
        }
    }

    /// <summary>
    /// Helper method to execute operations only when the NetworkObject is ready
    /// </summary>
    private void EjecutarCuandoListo(Action operacion)
    {
        // Check if the object is spawned and has network authority
        if (IsSpawned && (IsServer || IsOwner))
        {
            if (esNetworkObjectListo)
            {
                operacion.Invoke();
            }
            else
            {
                if (mostrarDebug)
                {
                    Debug.Log($"[NETWORK] Encolando operación para {gameObject.name} - NetworkObject aún no listo");
                }
                operacionesPendientes.Add(operacion);
            }
        }
        else
        {
            // If we're a client without authority, we need to request the server to make changes
            if (mostrarDebug)
            {
                Debug.Log($"[NETWORK] {gameObject.name} no tiene autoridad para modificar directamente. Usando RPC");
            }

            // Enqueue for local consistency until we get a server update
            operacionesPendientes.Add(operacion);
        }
    }

    // Methods for direct modification (server-only)
    #region Server Direct Modifications

    /// <summary>
    /// Increases the range of the resource directly (server only)
    /// </summary>
    public void AumentarRango(int cantidad)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[MODIFICADOR] AumentarRango directo llamado desde cliente, ignorando");
            return;
        }

        if (mostrarDebug)
        {
            Debug.Log($"[MODIFICADOR] Aplicando aumento de rango directo: +{cantidad}");
        }

        modificacionRango.Value += cantidad;
    }

    /// <summary>
    /// Decreases the range of the resource directly (server only)
    /// </summary>
    public void DisminuirRango(int cantidad)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[MODIFICADOR] DisminuirRango directo llamado desde cliente, ignorando");
            return;
        }

        if (mostrarDebug)
        {
            Debug.Log($"[MODIFICADOR] Aplicando disminución de rango directo: -{cantidad}");
        }

        modificacionRango.Value -= cantidad;
    }

    /// <summary>
    /// Increases the life of the resource directly (server only)
    /// </summary>
    public void AumentarVida(float cantidad)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[MODIFICADOR] AumentarVida directo llamado desde cliente, ignorando");
            return;
        }

        modificacionVida.Value += cantidad;
    }

    /// <summary>
    /// Decreases the life of the resource directly (server only)
    /// </summary>
    public void DisminuirVida(float cantidad)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[MODIFICADOR] DisminuirVida directo llamado desde cliente, ignorando");
            return;
        }

        modificacionVida.Value -= cantidad;
    }

    /// <summary>
    /// Sets the resource as immobile directly (server only)
    /// </summary>
    public void HacerInmovil()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[MODIFICADOR] HacerInmovil directo llamado desde cliente, ignorando");
            return;
        }

        esMovible.Value = false;
    }

    /// <summary>
    /// Sets the resource as mobile directly (server only)
    /// </summary>
    public void HacerMovil()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[MODIFICADOR] HacerMovil directo llamado desde cliente, ignorando");
            return;
        }

        esMovible.Value = true;
    }

    #endregion

    // RPCs for remote modification (can be called from anywhere)
    #region Server RPCs for Remote Modification

    /// <summary>
    /// Server RPC to increase range safely
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AumentarRangoServerRpc(int cantidad)
    {
        EjecutarCuandoListo(() => {
            int valorAntes = modificacionRango.Value;
            modificacionRango.Value += cantidad;

            if (mostrarDebug)
            {
                Debug.Log($"[RPC] Aumento de rango aplicado a {gameObject.name}: {valorAntes} -> {modificacionRango.Value} (+{cantidad})");
            }
        });
    }

    /// <summary>
    /// Server RPC to decrease range safely
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void DisminuirRangoServerRpc(int cantidad)
    {
        EjecutarCuandoListo(() => {
            int valorAntes = modificacionRango.Value;
            modificacionRango.Value -= cantidad;

            if (mostrarDebug)
            {
                Debug.Log($"[RPC] Disminución de rango aplicada a {gameObject.name}: {valorAntes} -> {modificacionRango.Value} (-{cantidad})");
            }
        });
    }

    /// <summary>
    /// Server RPC to increase life safely
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AumentarVidaServerRpc(float cantidad)
    {
        EjecutarCuandoListo(() => {
            float valorAntes = modificacionVida.Value;
            modificacionVida.Value += cantidad;

            if (mostrarDebug)
            {
                Debug.Log($"[RPC] Aumento de vida aplicado a {gameObject.name}: {valorAntes} -> {modificacionVida.Value} (+{cantidad})");
            }
        });
    }

    /// <summary>
    /// Server RPC to decrease life safely
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void DisminuirVidaServerRpc(float cantidad)
    {
        EjecutarCuandoListo(() => {
            float valorAntes = modificacionVida.Value;
            modificacionVida.Value -= cantidad;

            if (mostrarDebug)
            {
                Debug.Log($"[RPC] Disminución de vida aplicada a {gameObject.name}: {valorAntes} -> {modificacionVida.Value} (-{cantidad})");
            }
        });
    }

    /// <summary>
    /// Server RPC to make the resource immobile safely
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void HacerInmovilServerRpc()
    {
        EjecutarCuandoListo(() => {
            bool valorAntes = esMovible.Value;
            esMovible.Value = false;

            if (mostrarDebug && valorAntes != false)
            {
                Debug.Log($"[RPC] {gameObject.name} ahora es inmóvil");
            }
        });
    }

    /// <summary>
    /// Server RPC to make the resource mobile safely
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void HacerMovilServerRpc()
    {
        EjecutarCuandoListo(() => {
            bool valorAntes = esMovible.Value;
            esMovible.Value = true;

            if (mostrarDebug && valorAntes != true)
            {
                Debug.Log($"[RPC] {gameObject.name} ahora es móvil");
            }
        });
    }

    #endregion

    // Event handlers for network variable changes
    #region Network Variable Change Handlers

    private void OnRangoModificado(int valorAnterior, int nuevoValor)
    {
        if (recursoBase != null && mostrarDebug)
        {
            Debug.Log($"[EVENTO] Rango de {recursoBase.Name} modificado de {recursoBase.range + valorAnterior} a {recursoBase.range + nuevoValor}");
        }
    }

    private void OnVidaModificada(float valorAnterior, float nuevoValor)
    {
        if (recursoBase != null && mostrarDebug)
        {
            Debug.Log($"[EVENTO] Vida de {recursoBase.Name} modificada de {recursoBase.vida + valorAnterior} a {recursoBase.vida + nuevoValor}");
        }
    }

    private void OnMovilidadModificada(bool valorAnterior, bool nuevoValor)
    {
        if (recursoBase != null && mostrarDebug)
        {
            Debug.Log($"[EVENTO] Movilidad de {recursoBase.Name} cambiada a {nuevoValor}");
        }
    }

    #endregion

    // Methods to query current values
    #region Value Getters

    /// <summary>
    /// Gets the current range including modifications
    /// </summary>
    public int GetRangoActual()
    {
        if (recursoBase == null) return 0;
        return recursoBase.range + modificacionRango.Value;
    }

    /// <summary>
    /// Gets the current life including modifications
    /// </summary>
    public float GetVidaActual()
    {
        if (recursoBase == null) return 0f;
        return recursoBase.vida + modificacionVida.Value;
    }

    /// <summary>
    /// Checks if the resource is currently movable
    /// </summary>
    public bool EsMovible()
    {
        if (recursoBase == null) return false;
        return recursoBase.esmovible && esMovible.Value;
    }

    #endregion
}