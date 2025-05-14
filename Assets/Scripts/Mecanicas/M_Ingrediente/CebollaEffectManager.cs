using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class CebollaEffectManager : NetworkBehaviour, IEffectManager
{
    private GameObject nodoOrigen;
    private List<GameObject> nodosVecinos = new List<GameObject>();
    private NetworkVariable<ulong> clienteId = new NetworkVariable<ulong>();
    private NetworkVariable<bool> efectoActivo = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> multiplicadorActivo = new NetworkVariable<bool>(false);
    private IngredientesSO _ingredienteConfigurado;

    // Valor del multiplicador (cambiado a int para que coincida con el método SetMultiplicador)
    private const int MULTIPLICADOR_CEBOLLA = 2;

    // Implementación de ConfigurarConIngrediente de IEffectManager
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
        Debug.Log($"CebollaEffectManager configurado con: {ingrediente.name}");
    }

    // Implementación de IniciarEfecto de IEffectManager
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Obtener la referencia al NodeMap desde el nodo
        Node nodoComponente = nodoOrigen.GetComponent<Node>();
        if (nodoComponente == null || nodoComponente.nodeMap == null)
        {
            Debug.LogError("No se pudo obtener NodeMap desde el nodo origen");
            return;
        }

        // Obtener ID del propietario del tablero
        ulong clientId = nodoComponente.nodeMap.ownerClientId;

        // Delegar al método específico
        IniciarEfectoCebolla(nodoOrigen, nodosAfectados, clientId);
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Inicia el efecto de la cebolla, verificando si los vecinos están vacíos
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void IniciarEfectoCebolla(GameObject nodoOrigen, List<GameObject> nodosVecinos, ulong clientId)
    {
        // Convertirse en objeto de red para poder usar RPC
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Guardar referencias
        this.nodoOrigen = nodoOrigen;
        this.nodosVecinos = new List<GameObject>(nodosVecinos); // Crear copia de la lista
        clienteId.Value = clientId;

        // Iniciar comprobación
        ComprobarVecinosVaciosServerRpc();

        // Suscribirse a eventos relevantes para volver a comprobar cuando el tablero cambia
        SuscribirseACambiosTablero();

        Debug.Log($"Efecto Cebolla iniciado para cliente {clientId}");
    }

    // Suscribirse a eventos de cambio en el tablero
    private void SuscribirseACambiosTablero()
    {
        // Buscar el NodeMap correspondiente
        Node nodo = nodoOrigen?.GetComponent<Node>();
        if (nodo != null && nodo.nodeMap != null)
        {
            // Si el NodeMap tiene un evento de cambio, suscribirse
            // Por ejemplo: nodo.nodeMap.OnNodeChanged += ComprobarVecinosVacios;
        }

        // Suscribirse al cambio de fase/turno para comprobar al inicio de cada turno
        TurnManager turnManager = FindObjectOfType<TurnManager>();
        if (turnManager != null)
        {
            // Aquí deberías registrar este gestor para ser notificado cuando cambia la fase
            // Depende de la implementación actual de tu TurnManager
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void ComprobarVecinosVaciosServerRpc()
    {
        efectoActivo.Value = true;

        // Verificar si todos los nodos vecinos están vacíos
        bool todosVacios = true;
        foreach (var vecino in nodosVecinos)
        {
            Node nodoVecino = vecino.GetComponent<Node>();
            if (nodoVecino != null && nodoVecino.hasIngredient.Value)
            {
                todosVacios = false;
                break;
            }
        }

        // Actualizar estado del multiplicador
        bool cambioEstado = todosVacios != multiplicadorActivo.Value;
        multiplicadorActivo.Value = todosVacios;

        // MODIFICAR: Acceder directamente a la economía a través del nodo origen
        if (cambioEstado)
        {
            // Obtener la economía del tablero directamente desde el nodo
            Node nodoComponente = nodoOrigen.GetComponent<Node>();
            if (nodoComponente != null && nodoComponente.nodeMap != null && nodoComponente.nodeMap.economia != null)
            {
                Economia economiaJugador = nodoComponente.nodeMap.economia;

                if (multiplicadorActivo.Value)
                {
                    economiaJugador.SetMultiplicador(MULTIPLICADOR_CEBOLLA);
                }
                else
                {
                    economiaJugador.SetMultiplicador(2);
                }

                // Notificar a todos los clientes
                ActualizarEstadoClientRpc(multiplicadorActivo.Value);
            }
            else
            {
                Debug.LogError($"No se encontró componente Economía para el cliente {clienteId.Value}");
            }
        }
    }

    [ClientRpc]
    private void ActualizarEstadoClientRpc(bool multiplicadorActivo)
    {
        Debug.Log($"Efecto Cebolla: Multiplicador {(multiplicadorActivo ? "ACTIVADO" : "DESACTIVADO")}");

        // Efectos visuales para mostrar el estado de la cebolla
        // Por ejemplo, cambiar color o mostrar un indicador sobre la cebolla
        if (nodoOrigen != null)
        {
            // Aquí podrías activar/desactivar un efecto visual
            // Ejemplo: nodoOrigen.transform.Find("IndicadorMultiplicador").gameObject.SetActive(multiplicadorActivo);
        }
    }

    // Este método debe ser llamado cuando cambia el estado del tablero
    public void ComprobarVecinosVacios()
    {
        if (IsSpawned && efectoActivo.Value)
        {
            ComprobarVecinosVaciosServerRpc();
        }
    }

    // Implementación de LimpiarEfecto de IEffectManager
    public void LimpiarEfecto()
    {
        Debug.Log("CebollaEffectManager: LimpiarEfecto llamado");
        if (IsSpawned)
        {
            LimpiarEfectoServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LimpiarEfectoServerRpc()
    {
        // Si el multiplicador estaba activo, desactivarlo
        if (multiplicadorActivo.Value)
        {
            // MODIFICAR: Acceder directamente a la economía
            Node nodoComponente = nodoOrigen.GetComponent<Node>();
            if (nodoComponente != null && nodoComponente.nodeMap != null && nodoComponente.nodeMap.economia != null)
            {
                Economia economiaJugador = nodoComponente.nodeMap.economia;
                economiaJugador.SetMultiplicador(1);
            }
        }

        efectoActivo.Value = false;
        multiplicadorActivo.Value = false;

        // Notificar a los clientes y eliminar el objeto
        LimpiarEfectoClientRpc();

        // Auto-destrucción después de un pequeño retraso
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        Debug.Log("Limpiando efecto Cebolla");

        // Desuscribirse de eventos
        // Por ejemplo: UnityAction<Node> handler = ComprobarVecinosVacios;
        // nodoOrigen.GetComponent<Node>().nodeMap.OnNodeChanged -= handler;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Despawn(true); // true para destruir el objeto
        }
        else if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}