using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que permite moverse a un nodo adyacente vacío.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Tomate", menuName = "CookingGame/Resources/Ingredients/Tomate")]
public class Tomate : IngredientesSO
{
    [Tooltip("Material para resaltar nodos disponibles para movimiento")]
    public Material materialResaltado;

    [Tooltip("Color para resaltar nodos disponibles")]
    public Color colorResaltado = Color.red;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        Debug.Log("Ejecutando efecto de Tomate");

        // Crear un gestor de movimiento para el tomate
        GameObject gestorObj = new GameObject("TomateMovementManager");
        TomateMovementManager gestor = gestorObj.AddComponent<TomateMovementManager>();
        gestor.IniciarMovimientoTomate(nodoOrigen, nodosAfectados, materialResaltado, colorResaltado);
    }
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona el movimiento de Tomate a nodos adyacentes.
/// ‡‡</summary>_PLACEHOLDER‡‡
public class TomateMovementManager : NetworkBehaviour
{
    private GameObject nodoOrigen;
    private List<GameObject> nodosDisponibles = new List<GameObject>();
    private List<Material> materialesOriginales = new List<Material>();
    private Material materialResaltado;
    private Color colorResaltado;
    private bool movimientoEnProgreso = false;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el proceso de selección para mover el tomate
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarMovimientoTomate(GameObject origen, List<GameObject> vecinos, Material material, Color color)
    {
        nodoOrigen = origen;
        materialResaltado = material;
        colorResaltado = color;

        // Convertirse en objeto de red para poder usar RPC
        GetComponent<NetworkObject>().Spawn();

        // Filtrar solo los nodos disponibles (vacíos)
        foreach (var nodo in vecinos)
        {
            Node componenteNodo = nodo.GetComponent<Node>();
            if (componenteNodo != null && !componenteNodo.hasIngredient.Value)
            {
                nodosDisponibles.Add(nodo);

                // Guardar material original y aplicar resaltado
                MeshRenderer renderer = nodo.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    materialesOriginales.Add(renderer.material);

                    if (materialResaltado != null)
                    {
                        renderer.material = materialResaltado;
                    }
                    else
                    {
                        // Si no hay material específico, usar color
                        Material matTemp = new Material(renderer.material);
                        matTemp.color = colorResaltado;
                        renderer.material = matTemp;
                    }
                }
                else
                {
                    materialesOriginales.Add(null);
                }

                // Agregar detector de clic al nodo
                NodeClickDetector clickDetector = nodo.AddComponent<NodeClickDetector>();
                clickDetector.Initialize(this);
            }
        }

        // Si no hay nodos disponibles, terminar
        if (nodosDisponibles.Count == 0)
        {
            Debug.Log("No hay nodos disponibles para mover el tomate");
            FinalizarMovimiento();
            return;
        }

        movimientoEnProgreso = true;

        // Temporizador de seguridad para limpiar después de un tiempo
        Invoke("FinalizarMovimiento", 10f);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Procesa un clic en un nodo específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ProcesarClic(GameObject nodoSeleccionado)
    {
        if (!movimientoEnProgreso) return;

        // Verificar si el nodo es válido
        if (!nodosDisponibles.Contains(nodoSeleccionado)) return;

        // Solicitar el movimiento al servidor
        MoverTomateServerRpc(nodoOrigen.GetComponent<NetworkObject>().NetworkObjectId,
                             nodoSeleccionado.GetComponent<NetworkObject>().NetworkObjectId);

        // Finalizar el proceso
        FinalizarMovimiento();
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Limpia todos los resaltados y detectores
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void FinalizarMovimiento()
    {
        if (!movimientoEnProgreso) return;

        movimientoEnProgreso = false;

        // Restaurar materiales originales
        for (int i = 0; i < nodosDisponibles.Count; i++)
        {
            GameObject nodo = nodosDisponibles[i];

            // Quitar detector de clic
            NodeClickDetector clickDetector = nodo.GetComponent<NodeClickDetector>();
            if (clickDetector != null)
            {
                Destroy(clickDetector);
            }

            // Restaurar material original
            MeshRenderer renderer = nodo.GetComponent<MeshRenderer>();
            if (renderer != null && i < materialesOriginales.Count && materialesOriginales[i] != null)
            {
                renderer.material = materialesOriginales[i];
            }
        }

        // Destruir este gestor
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
        Destroy(gameObject, 0.1f);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoverTomateServerRpc(ulong origenId, ulong destinoId)
    {
        // Obtener referencias por NetworkObjectId
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        NetworkObject destinoNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[destinoId];

        if (origenNetObj == null || destinoNetObj == null) return;

        GameObject origen = origenNetObj.gameObject;
        GameObject destino = destinoNetObj.gameObject;

        // Verificar componentes
        Node nodoOrigen = origen.GetComponent<Node>();
        Node nodoDestino = destino.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null) return;
        if (!nodoOrigen.hasIngredient.Value || nodoDestino.hasIngredient.Value) return;

        // Obtener referencia al tomate
        GameObject tomateObj = nodoOrigen.currentIngredient;
        if (tomateObj == null) return;

        // Obtener el prefab del tomate para recrearlo en el destino
        ResourcesSO tomateSO = tomateObj.GetComponent<ResourcesSO>();
        if (tomateSO == null || tomateSO.prefab3D == null) return;

        // Quitar el tomate del origen
        NetworkObject tomateNetObj = tomateObj.GetComponent<NetworkObject>();
        if (tomateNetObj != null && tomateNetObj.IsSpawned)
        {
            tomateNetObj.Despawn();
        }
        nodoOrigen.hasIngredient.Value = false;
        nodoOrigen.currentIngredient = null;

        // Colocar el tomate en el destino
        nodoDestino.SetNodeIngredient(tomateSO.prefab3D);

        // Notificar a todos los clientes
        MoverTomateClientRpc(origenId, destinoId);
    }

    [ClientRpc]
    private void MoverTomateClientRpc(ulong origenId, ulong destinoId)
    {
        Debug.Log($"Tomate movido de nodo {origenId} a nodo {destinoId}");
    }
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que detecta clics en nodos para el efecto Tomate
/// ‡‡</summary>_PLACEHOLDER‡‡
public class NodeClickDetector : MonoBehaviour
{
    private TomateMovementManager manager;

    public void Initialize(TomateMovementManager manager)
    {
        this.manager = manager;
    }

    private void OnMouseDown()
    {
        if (manager != null)
        {
            manager.ProcesarClic(gameObject);
        }
    }
}