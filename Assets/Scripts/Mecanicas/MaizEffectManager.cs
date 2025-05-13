using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic; // Para la interfaz que usa List<GameObject>

public class MaizEffectManager : NetworkBehaviour, IEffectManager
{
    private GameObject nodoOrigen;
    private NetworkVariable<float> cantidadDinero = new NetworkVariable<float>(10f);
    private NetworkVariable<ulong> clienteId = new NetworkVariable<ulong>();
    private bool efectoAplicado = false;
    private IngredientesSO _ingredienteConfigurado;

    // Para efectos persistentes entre turnos
    private NetworkVariable<bool> efectoActivo = new NetworkVariable<bool>(false);

    // Implementación de ConfigurarConIngrediente de IEffectManager
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
        Debug.Log($"MaizEffectManager configurado con: {ingrediente.name}");

        // Si el ingrediente es Maiz, podemos obtener la cantidad de dinero directamente
        if (ingrediente is Maiz maizIngrediente)
        {
            cantidadDinero.Value = maizIngrediente.cantidadDinero;
        }
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

        // Para el maíz, nodosAfectados no se usa, pero lo recibimos por la interfaz
        // Solo necesitamos la cantidad de dinero, que ya debería estar configurada en ConfigurarConIngrediente

        // Llamamos al método específico pasando los valores que necesita
        IniciarEfectoMaiz(nodoOrigen, clientId, cantidadDinero.Value);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el efecto económico del maíz
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarEfectoMaiz(GameObject nodoOrigen, ulong clientId, float cantidad)
    {
        // Convertirse en objeto de red para poder usar RPC
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Guardar referencias
        this.nodoOrigen = nodoOrigen;
        cantidadDinero.Value = cantidad;
        clienteId.Value = clientId;

        // Aplicar efecto inmediato y registrarse para efectos recurrentes
        AplicarEfectoServerRpc(clienteId.Value, cantidadDinero.Value);

        // Suscribirse al evento de cambio de turno para generar dinero recurrente
        TurnManager turnManager = FindObjectOfType<TurnManager>();
        if (turnManager != null)
        {
            // Aquí deberías registrar este gestor para ser notificado en cada inicio de turno
            // Depende de cómo tienes implementado tu TurnManager
        }

        Debug.Log($"Efecto Maíz iniciado para cliente {clientId} generando {cantidad} monedas");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong clientId, float cantidad)
    {
        efectoAplicado = true;
        efectoActivo.Value = true;

        // Localizar la economía del jugador
        Economia economiaJugador = null;

        // Buscar la economía asociada al cliente específico
        // Esto se podría mejorar con un sistema central que registre las economías por clienteId
        foreach (var economia in FindObjectsOfType<Economia>())
        {
            // Aquí deberías tener alguna forma de identificar a qué cliente pertenece cada economía
            // Por ejemplo, economía.ClientId == clientId
            economiaJugador = economia;
            break;
        }

        if (economiaJugador != null)
        {
            // Aplicar efecto económico
            economiaJugador.more_money(cantidad);

            // Notificar a todos los clientes
            AplicarEfectoClientRpc(clientId, cantidad);
        }
        else
        {
            Debug.LogError($"No se encontró componente Economía para el cliente {clientId}");
        }
    }

    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong clientId, float cantidad)
    {
        Debug.Log($"Efecto Maíz aplicado: Cliente {clientId} recibió {cantidad} monedas");

        // Efectos visuales como partículas de monedas o texto flotante
        if (nodoOrigen != null)
        {
            // Aquí podrías instanciar efectos visuales
            // Ejemplo: Instantiate(efectoDinero, nodoOrigen.transform.position, Quaternion.identity);
        }
    }

    // Este método sería llamado por el TurnManager al inicio de cada turno
    public void GenerarDineroTurno()
    {
        if (IsSpawned && efectoActivo.Value)
        {
            GenerarDineroTurnoServerRpc(clienteId.Value, cantidadDinero.Value);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GenerarDineroTurnoServerRpc(ulong clientId, float cantidad)
    {
        // Verificar si el nodo origen aún tiene el maíz
        Node nodo = nodoOrigen?.GetComponent<Node>();
        if (nodo == null || !nodo.hasIngredient.Value)
        {
            // El maíz ya no está, desactivar efecto
            efectoActivo.Value = false;
            LimpiarEfecto();
            return;
        }

        // Mismo código que en AplicarEfectoServerRpc para encontrar la economía y dar dinero
        Economia economiaJugador = null;
        foreach (var economia in FindObjectsOfType<Economia>())
        {
            economiaJugador = economia;
            break;
        }

        if (economiaJugador != null)
        {
            economiaJugador.more_money(cantidad);
            GenerarDineroTurnoClientRpc(clientId, cantidad);
        }
    }

    [ClientRpc]
    private void GenerarDineroTurnoClientRpc(ulong clientId, float cantidad)
    {
        Debug.Log($"Maíz generó {cantidad} monedas adicionales en nuevo turno para cliente {clientId}");

        // Efectos visuales de nuevo turno
    }

    // Implementación de LimpiarEfecto de IEffectManager
    public void LimpiarEfecto()
    {
        Debug.Log("MaizEffectManager: LimpiarEfecto llamado");
        if (IsSpawned)
        {
            LimpiarEfectoServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LimpiarEfectoServerRpc()
    {
        efectoActivo.Value = false;

        // Notificar a los clientes y eliminar el objeto
        LimpiarEfectoClientRpc();

        // Auto-destrucción después de un pequeño retraso
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        Debug.Log("Limpiando efecto Maíz");

        // Desregistrarse de eventos del TurnManager si es necesario
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