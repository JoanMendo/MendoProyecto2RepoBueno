using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Efecto que mueve todos los ingredientes de una fila hacia arriba
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "S_Especial", menuName = "CookingGame/Resources/Efectos/S_Especial")]
public class S_Especial : Efectos
{
    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Crear gestor temporal para este efecto
        GameObject gestorObj = new GameObject("EfectoEspecialManager");
        EfectoEspecialManager gestor = gestorObj.AddComponent<EfectoEspecialManager>();

        // Iniciar efecto con duración de 1 turno
        gestor.IniciarEfecto(nodoOrigen, 1);
    }
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona el efecto de movimiento hacia arriba
/// ‡‡</summary>_PLACEHOLDER‡‡
public class EfectoEspecialManager : NetworkBehaviour
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

        // Ejecutar el efecto de mover hacia arriba
        MoverIngredientesHaciaArriba();

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
    /// Mueve todos los ingredientes de la fila hacia arriba
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void MoverIngredientesHaciaArriba()
    {
        if (!IsServer) return;

        // Obtener la fila del nodo origen
        Node nodoComponent = nodoOrigen.GetComponent<Node>();
        if (nodoComponent == null) return;

        int fila = Mathf.RoundToInt(nodoComponent.position.y);

        // Obtener NodeMap para buscar nodos
        NodeMap nodeMap = FindObjectOfType<NodeMap>();
        if (nodeMap == null) return;

        // Obtener todos los nodos de la fila (Y constante)
        List<GameObject> nodosEnFila = new List<GameObject>();
        foreach (var nodo in nodeMap.nodesList)
        {
            Node componente = nodo.GetComponent<Node>();
            if (componente != null && Mathf.RoundToInt(componente.position.y) == fila)
            {
                nodosEnFila.Add(nodo);
            }
        }

        // Para cada nodo en la fila, mover su ingrediente arriba si es posible
        foreach (var nodo in nodosEnFila)
        {
            Node nodoActual = nodo.GetComponent<Node>();
            if (nodoActual == null || !nodoActual.hasIngredient.Value) continue;

            // Buscar nodo arriba
            Vector2 posArriba = new Vector2(nodoActual.position.x, nodoActual.position.y + 1);

            // Buscar nodo destino
            GameObject nodoArriba = null;
            foreach (var candidato in nodeMap.nodesList)
            {
                Node comp = candidato.GetComponent<Node>();
                if (comp != null && comp.position == posArriba)
                {
                    nodoArriba = candidato;
                    break;
                }
            }

            // Si hay nodo arriba y está vacío, mover ingrediente
            if (nodoArriba != null)
            {
                Node nodoArribaComp = nodoArriba.GetComponent<Node>();
                if (nodoArribaComp != null && !nodoArribaComp.hasIngredient.Value)
                {
                    // Obtener información del ingrediente actual
                    GameObject ingredienteActual = nodoActual.currentIngredient;
                    ResourcesSO recursoActual = ingredienteActual.GetComponent<ResourcesSO>();

                    // Si hay recurso válido, moverlo
                    if (recursoActual != null && recursoActual.prefab3D != null)
                    {
                        // Desactivar ingrediente actual
                        nodoActual.ClearNodeIngredient();

                        // Colocar en nodo superior
                        nodoArribaComp.SetNodeIngredient(recursoActual.prefab3D);
                    }
                }
            }
        }
    }
}