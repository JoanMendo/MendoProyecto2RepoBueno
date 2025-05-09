using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// ��<summary>_PLACEHOLDER��
/// Efecto que crea una conexi�n entre dos nodos por un n�mero de turnos
/// ��</summary>_PLACEHOLDER��
[CreateAssetMenu(fileName = "S_Blanca", menuName = "CookingGame/Resources/Efectos/S_Blanca")]
public class S_Blanca : Efectos
{
    [Tooltip("Prefab visual para la conexi�n entre nodos")]
    public GameObject prefabConexion;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Este efecto no se activa autom�ticamente, solo a trav�s de CrearConexion
        Debug.Log("S_Blanca requiere selecci�n de nodos espec�ficos para activarse");
    }

    /// ��<summary>_PLACEHOLDER��
    /// Crea una conexi�n de salsa entre dos nodos
    /// ��</summary>_PLACEHOLDER��
    public void CrearConexion(GameObject nodoOrigen, GameObject nodoDestino)
    {
        if (nodoOrigen == null || nodoDestino == null) return;

        // Crear gestor para la conexi�n
        GameObject gestorObj = new GameObject("EfectoBlancoManager");
        EfectoBlancoManager gestor = gestorObj.AddComponent<EfectoBlancoManager>();

        // Iniciar el efecto
        gestor.IniciarEfectoConexion(nodoOrigen, nodoDestino, this, 3);
    }
}

/// ��<summary>_PLACEHOLDER��
/// Componente que gestiona la conexi�n visual y l�gica entre dos nodos
/// ��</summary>_PLACEHOLDER��
public class EfectoBlancoManager : NetworkBehaviour
{
    // Referencias a los nodos conectados
    private GameObject nodoOrigen;
    private GameObject nodoDestino;
    private S_Blanca efectoSalsa;

    // Objeto visual de la conexi�n
    private GameObject conexionVisual;

    // Estado de la conexi�n
    public NetworkVariable<int> turnosRestantes = new NetworkVariable<int>(0);
    public NetworkVariable<bool> activa = new NetworkVariable<bool>(false);

    /// ��<summary>_PLACEHOLDER��
    /// Inicia el efecto de conexi�n entre dos nodos
    /// ��</summary>_PLACEHOLDER��
    public void IniciarEfectoConexion(GameObject origen, GameObject destino, S_Blanca efecto, int duracion)
    {
        nodoOrigen = origen;
        nodoDestino = destino;
        efectoSalsa = efecto;

        // Spawnear objeto de red para permitir RPC
        GetComponent<NetworkObject>().Spawn();

        // Solicitar inicio de efecto al servidor
        CrearConexionServerRpc(
            origen.GetComponent<NetworkObject>().NetworkObjectId,
            destino.GetComponent<NetworkObject>().NetworkObjectId,
            duracion
        );
    }

    [ServerRpc(RequireOwnership = false)]
    private void CrearConexionServerRpc(ulong origenId, ulong destinoId, int duracion)
    {
        // Obtener referencias por NetworkObjectId
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        NetworkObject destinoNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[destinoId];

        if (origenNetObj == null || destinoNetObj == null) return;

        // Inicializar estado
        turnosRestantes.Value = duracion;
        activa.Value = true;

        // Notificar a clientes para crear visuales
        CrearConexionVisualClientRpc(origenId, destinoId);
    }

    [ClientRpc]
    private void CrearConexionVisualClientRpc(ulong origenId, ulong destinoId)
    {
        // Obtener referencias por NetworkObjectId
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        NetworkObject destinoNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[destinoId];

        if (origenNetObj == null || destinoNetObj == null) return;

        nodoOrigen = origenNetObj.gameObject;
        nodoDestino = destinoNetObj.gameObject;

        // Crear visual de conexi�n
        if (efectoSalsa.prefabConexion != null)
        {
            conexionVisual = Instantiate(efectoSalsa.prefabConexion);

            // Posicionar entre los dos nodos
            conexionVisual.transform.position = Vector3.Lerp(
                nodoOrigen.transform.position,
                nodoDestino.transform.position,
                0.5f
            );

            // Rotar para apuntar del origen al destino
            Vector3 direccion = nodoDestino.transform.position - nodoOrigen.transform.position;
            conexionVisual.transform.rotation = Quaternion.LookRotation(direccion);

            // Escalar seg�n distancia
            float distancia = Vector3.Distance(nodoOrigen.transform.position, nodoDestino.transform.position);
            conexionVisual.transform.localScale = new Vector3(1, 1, distancia);
        }
    }

    /// ��<summary>_PLACEHOLDER��
    /// Procesa el efecto en cada turno
    /// ��</summary>_PLACEHOLDER��
    public void ProcesarTurno()
    {
        if (!IsServer) return;

        // Reducir duraci�n
        turnosRestantes.Value--;

        // Aplicar efecto espec�fico (por ejemplo, transferir ingredientes)
        if (turnosRestantes.Value >= 0)
        {
            AplicarEfectoConexion();
        }

        // Si termin�, destruir
        if (turnosRestantes.Value <= 0)
        {
            FinalizarConexion();
        }
    }

    /// ��<summary>_PLACEHOLDER��
    /// Aplica el efecto espec�fico de la conexi�n
    /// ��</summary>_PLACEHOLDER��
    private void AplicarEfectoConexion()
    {
        if (!IsServer) return;

        // Implementar efecto espec�fico seg�n el tipo de conexi�n
        // Por ejemplo, compartir propiedades entre ingredientes
    }

    /// ��<summary>_PLACEHOLDER��
    /// Finaliza la conexi�n y limpia recursos
    /// ��</summary>_PLACEHOLDER��
    private void FinalizarConexion()
    {
        if (!IsServer) return;

        activa.Value = false;

        // Notificar a clientes para limpiar visuales
        LimpiarConexionClientRpc();

        // Destruir este objeto despu�s de un breve retraso
        StartCoroutine(DestruirDespuesDeRetraso());
    }

    [ClientRpc]
    private void LimpiarConexionClientRpc()
    {
        // Destruir visuales
        if (conexionVisual != null)
        {
            Destroy(conexionVisual);
        }
    }

    private System.Collections.IEnumerator DestruirDespuesDeRetraso()
    {
        yield return new WaitForSeconds(0.2f);

        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
        Destroy(gameObject);
    }
}
