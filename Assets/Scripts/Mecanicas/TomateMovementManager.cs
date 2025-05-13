using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class TomateMovementManager : NetworkBehaviour, IEffectManager
{
    private GameObject nodoOrigen;
    private List<GameObject> nodosDisponibles = new List<GameObject>();
    private List<Material> materialesOriginales = new List<Material>();
    private Material materialResaltado;
    private Color colorResaltado = Color.red;
    private bool movimientoEnProgreso = false;
    private IngredientesSO _ingredienteConfigurado;

    // Implementación de ConfigurarConIngrediente de IEffectManager
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
        Debug.Log($"TomateMovementManager configurado con: {ingrediente.name}");

        // Si el ingrediente es Tomate, podemos obtener sus parámetros visuales
        if (ingrediente is Tomate tomateIngrediente)
        {
            materialResaltado = tomateIngrediente.materialResaltado;
            colorResaltado = tomateIngrediente.colorResaltado;
        }
    }

    // Implementación de IniciarEfecto de IEffectManager
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Obtener material y color del ingrediente configurado, o usar valores por defecto
        Material material = materialResaltado;
        Color color = colorResaltado;

        // Si tenemos la referencia al ingrediente específico, usar sus valores
        if (_ingredienteConfigurado is Tomate tomateIngrediente)
        {
            material = tomateIngrediente.materialResaltado;
            color = tomateIngrediente.colorResaltado;
        }

        // Delegar al método específico
        IniciarMovimientoTomate(nodoOrigen, nodosAfectados, material, color);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el proceso de selección para mover el tomate
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarMovimientoTomate(GameObject origen, List<GameObject> vecinos, Material material, Color color)
    {
        nodoOrigen = origen;
        materialResaltado = material;
        colorResaltado = color;

        // Convertirse en objeto de red para poder usar RPC
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Limpiar listas previas (por si se reutiliza el manager)
        nodosDisponibles.Clear();
        materialesOriginales.Clear();

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

        // Obtener los NetworkObject para los nodos origen y destino
        NetworkObject origenNetObj = nodoOrigen?.GetComponent<NetworkObject>();
        NetworkObject destinoNetObj = nodoSeleccionado?.GetComponent<NetworkObject>();

        if (origenNetObj == null || !origenNetObj.IsSpawned ||
            destinoNetObj == null || !destinoNetObj.IsSpawned)
        {
            Debug.LogError("TomateMovementManager: Los nodos no tienen NetworkObject válidos");
            FinalizarMovimiento();
            return;
        }

        // Solicitar el movimiento al servidor
        MoverTomateServerRpc(origenNetObj.NetworkObjectId, destinoNetObj.NetworkObjectId);

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
            if (nodo == null) continue;

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

        // Cancelar el temporizador de seguridad si está activo
        CancelInvoke("FinalizarMovimiento");

        // Destruir este gestor
        if (IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
        else if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    // Implementación de LimpiarEfecto de IEffectManager
    public void LimpiarEfecto()
    {
        Debug.Log("TomateMovementManager: LimpiarEfecto llamado");
        FinalizarMovimiento();
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoverTomateServerRpc(ulong origenId, ulong destinoId)
    {
        // Verificar que el NetworkManager existe y tiene el SpawnManager
        if (NetworkManager.Singleton == null ||
            NetworkManager.Singleton.SpawnManager == null ||
            NetworkManager.Singleton.SpawnManager.SpawnedObjects == null)
        {
            Debug.LogError("TomateMovementManager: NetworkManager o SpawnManager no disponibles");
            return;
        }

        // Verificar que los objetos existen en el SpawnManager
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out NetworkObject origenNetObj) ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(destinoId, out NetworkObject destinoNetObj))
        {
            Debug.LogError($"TomateMovementManager: No se encontraron objetos con IDs {origenId} o {destinoId}");
            return;
        }

        GameObject origen = origenNetObj.gameObject;
        GameObject destino = destinoNetObj.gameObject;

        // Verificar componentes
        Node nodoOrigen = origen.GetComponent<Node>();
        Node nodoDestino = destino.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null)
        {
            Debug.LogError("TomateMovementManager: Uno o ambos nodos no tienen componente Node");
            return;
        }

        if (!nodoOrigen.hasIngredient.Value || nodoDestino.hasIngredient.Value)
        {
            Debug.LogError("TomateMovementManager: Estado inválido de nodos (origen sin ingrediente o destino ocupado)");
            return;
        }

        // Obtener referencia al tomate
        GameObject tomateObj = nodoOrigen.currentIngredient;
        if (tomateObj == null)
        {
            Debug.LogError("TomateMovementManager: Nodo origen no tiene ingrediente");
            return;
        }

        // Obtener el prefab del tomate para recrearlo en el destino
        ResourcesSO tomateSO = tomateObj.GetComponent<ResourcesSO>();
        if (tomateSO == null || tomateSO.prefab3D == null)
        {
            Debug.LogError("TomateMovementManager: No se pudo obtener ResourcesSO o prefab3D del tomate");
            return;
        }

        // Quitar el tomate del origen
        NetworkObject tomateNetObj = tomateObj.GetComponent<NetworkObject>();
        if (tomateNetObj != null && tomateNetObj.IsSpawned)
        {
            tomateNetObj.Despawn(true);
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