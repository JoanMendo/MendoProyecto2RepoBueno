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

    // Implementaci�n de ConfigurarConIngrediente de IEffectManager
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

    // Implementaci�n de IniciarEfecto de IEffectManager
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

        // Para el ma�z, nodosAfectados no se usa, pero lo recibimos por la interfaz
        // Solo necesitamos la cantidad de dinero, que ya deber�a estar configurada en ConfigurarConIngrediente

        // Llamamos al m�todo espec�fico pasando los valores que necesita
        IniciarEfectoMaiz(nodoOrigen, clientId, cantidadDinero.Value);
    }

    /// ��<summary>_PLACEHOLDER��
    /// Inicia el efecto econ�mico del ma�z
    /// ��</summary>_PLACEHOLDER��
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
            // Aqu� deber�as registrar este gestor para ser notificado en cada inicio de turno
            // Depende de c�mo tienes implementado tu TurnManager
        }

        Debug.Log($"Efecto Ma�z iniciado para cliente {clientId} generando {cantidad} monedas");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AplicarEfectoServerRpc(ulong clientId, float cantidad)
    {
        efectoAplicado = true;
        efectoActivo.Value = true;

        // Localizar la econom�a del jugador
        Economia economiaJugador = null;

        // Buscar la econom�a asociada al cliente espec�fico
        // Esto se podr�a mejorar con un sistema central que registre las econom�as por clienteId
        foreach (var economia in FindObjectsOfType<Economia>())
        {
            // Aqu� deber�as tener alguna forma de identificar a qu� cliente pertenece cada econom�a
            // Por ejemplo, econom�a.ClientId == clientId
            economiaJugador = economia;
            break;
        }

        if (economiaJugador != null)
        {
            // Aplicar efecto econ�mico
            economiaJugador.more_money(cantidad);

            // Notificar a todos los clientes
            AplicarEfectoClientRpc(clientId, cantidad);
        }
        else
        {
            Debug.LogError($"No se encontr� componente Econom�a para el cliente {clientId}");
        }
    }

    [ClientRpc]
    private void AplicarEfectoClientRpc(ulong clientId, float cantidad)
    {
        Debug.Log($"Efecto Ma�z aplicado: Cliente {clientId} recibi� {cantidad} monedas");

        // Efectos visuales como part�culas de monedas o texto flotante
        if (nodoOrigen != null)
        {
            // Aqu� podr�as instanciar efectos visuales
            // Ejemplo: Instantiate(efectoDinero, nodoOrigen.transform.position, Quaternion.identity);
        }
    }

    // Este m�todo ser�a llamado por el TurnManager al inicio de cada turno
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
        // Verificar si el nodo origen a�n tiene el ma�z
        Node nodo = nodoOrigen?.GetComponent<Node>();
        if (nodo == null || !nodo.hasIngredient.Value)
        {
            // El ma�z ya no est�, desactivar efecto
            efectoActivo.Value = false;
            LimpiarEfecto();
            return;
        }

        // Mismo c�digo que en AplicarEfectoServerRpc para encontrar la econom�a y dar dinero
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
        Debug.Log($"Ma�z gener� {cantidad} monedas adicionales en nuevo turno para cliente {clientId}");

        // Efectos visuales de nuevo turno
    }

    // Implementaci�n de LimpiarEfecto de IEffectManager
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

        // Auto-destrucci�n despu�s de un peque�o retraso
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        Debug.Log("Limpiando efecto Ma�z");

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