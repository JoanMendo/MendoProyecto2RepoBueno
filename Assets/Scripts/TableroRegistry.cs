using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
/// <summary>
/// Gestor central que permite localizar tableros de todos los jugadores
/// </summary>
public class TableroRegistry : MonoBehaviour
{
    // Singleton para acceso fácil
    public static TableroRegistry Instance { get; private set; }

    // Diccionario que mapea ID de cliente a su tablero
    private Dictionary<ulong, NodeMap> tablerosPorCliente = new Dictionary<ulong, NodeMap>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Registra un tablero para un cliente específico
    /// </summary>
    public void RegistrarTablero(ulong clientId, NodeMap tablero)
    {
        if (tablerosPorCliente.ContainsKey(clientId))
        {
            tablerosPorCliente[clientId] = tablero;
        }
        else
        {
            tablerosPorCliente.Add(clientId, tablero);
        }

        Debug.Log($"Tablero registrado para cliente {clientId}");
    }

    /// <summary>
    /// Obtiene el tablero de un cliente específico
    /// </summary>
    public NodeMap ObtenerTablero(ulong clientId)
    {
        if (tablerosPorCliente.TryGetValue(clientId, out NodeMap tablero))
        {
            return tablero;
        }
        return null;
    }

    /// <summary>
    /// Obtiene todos los tableros de oponentes para un cliente
    /// </summary>
    public List<NodeMap> ObtenerTablerosOponentes(ulong clientId)
    {
        List<NodeMap> oponentes = new List<NodeMap>();

        foreach (var kvp in tablerosPorCliente)
        {
            if (kvp.Key != clientId)
            {
                oponentes.Add(kvp.Value);
            }
        }

        return oponentes;
    }

    /// <summary>
    /// Obtiene el tablero del primer oponente encontrado
    /// </summary>
    public NodeMap ObtenerTableroOponente(ulong clientId)
    {
        foreach (var kvp in tablerosPorCliente)
        {
            if (kvp.Key != clientId)
            {
                return kvp.Value;
            }
        }
        return null;
    }
}