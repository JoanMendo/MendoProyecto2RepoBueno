using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Efecto que mueve ingredientes a posiciones aleatorias cada turno
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "S_Picante", menuName = "CookingGame/Resources/Efectos/S_Picante")]
public class S_Picante : Efectos
{
    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Crear gestor temporal para este efecto
        GameObject gestorObj = new GameObject("EfectoPicanteManager");
        EfectoPicanteManager gestor = gestorObj.AddComponent<EfectoPicanteManager>();

        // Iniciar efecto con duración de 3 turnos
        gestor.IniciarEfecto(nodoOrigen, 3);
    }
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona el efecto picante de movimiento aleatorio
/// ‡‡</summary>_PLACEHOLDER‡‡
public class EfectoPicanteManager : NetworkBehaviour
{
    private GameObject nodoOrigen;
    public NetworkVariable<int> turnosRestantes = new NetworkVariable<int>(0);

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el efecto con una duración específica
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarEfecto(GameObject origen, int duracion)
    {
        nodoOrigen = origen;

        // Spawnear objeto de red para permitir RPC
        GetComponent<NetworkObject>().Spawn();

        // Iniciar efecto en el servidor
        IniciarEfectoServerRpc(origen.GetComponent<NetworkObject>().NetworkObjectId, duracion);
    }

    [ServerRpc(RequireOwnership = false)]
    private void IniciarEfectoServerRpc(ulong origenId, int duracion)
    {
        // Obtener referencias por NetworkObjectId
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        if (origenNetObj == null) return;

        nodoOrigen = origenNetObj.gameObject;
        turnosRestantes.Value = duracion;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Ejecuta el efecto al final de cada turno
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ProcesarTurno()
    {
        if (!IsServer) return;

        // Ejecutar el efecto de mover aleatoriamente
        MoverIngredienteAleatorio();

        // Reducir duración
        turnosRestantes.Value--;

        // Si terminó, destruir
        if (turnosRestantes.Value <= 0)
        {
            if (IsSpawned)
            {
                GetComponent<NetworkObject>().Despawn();
            }
            Destroy(gameObject);
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Mueve el ingrediente a una posición aleatoria vacía
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void MoverIngredienteAleatorio()
    {
        if (!IsServer) return;

        // Verificar si el nodo origen tiene ingrediente
        Node nodoComponent = nodoOrigen.GetComponent<Node>();
        if (nodoComponent == null || !nodoComponent.hasIngredient.Value) return;

        // Obtener NodeMap para buscar nodos
        NodeMap nodeMap = FindObjectOfType<NodeMap>();
        if (nodeMap == null) return;

        // Buscar nodos vacíos
        List<GameObject> nodosVacios = new List<GameObject>();
        foreach (var nodo in nodeMap.nodesList)
        {
            Node componente = nodo.GetComponent<Node>();
            if (componente != null && !componente.hasIngredient.Value)
            {
                nodosVacios.Add(nodo);
            }
        }

        // Si no hay nodos vacíos, terminar
        if (nodosVacios.Count == 0) return;

        // Seleccionar un nodo vacío aleatorio
        int indiceAleatorio = Random.Range(0, nodosVacios.Count);
        GameObject nodoDestino = nodosVacios[indiceAleatorio];
        Node nodoDestinoComp = nodoDestino.GetComponent<Node>();

        // Mover ingrediente
        GameObject ingredienteActual = nodoComponent.currentIngredient;
        ResourcesSO recursoActual = ingredienteActual.GetComponent<ResourcesSO>();

        if (recursoActual != null && recursoActual.prefab3D != null)
        {
            // Desactivar ingrediente actual
            nodoComponent.ClearNodeIngredient();

            // Colocar en nodo destino
            nodoDestinoComp.SetNodeIngredient(recursoActual.prefab3D);
        }
    }
}