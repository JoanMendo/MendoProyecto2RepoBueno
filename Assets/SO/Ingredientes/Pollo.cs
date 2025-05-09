using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que rota los ingredientes vecinos en sentido horario.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Pollo", menuName = "CookingGame/Resources/Ingredients/Pollo")]
public class Pollo : IngredientesSO
{
    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        if (nodosAfectados.Count <= 0) return;

        Debug.Log("Ejecutando la pasiva de pollo");

        // 1. Recopilar información de estado actual
        List<(GameObject nodo, GameObject ingrediente)> estadoActual = new List<(GameObject, GameObject)>();

        foreach (var nodo in nodosAfectados)
        {
            Node componenteNodo = nodo.GetComponent<Node>();
            if (componenteNodo != null)
            {
                estadoActual.Add((nodo, componenteNodo.hasIngredient.Value ? componenteNodo.currentIngredient : null));
            }
        }

        // Si sólo hay un nodo, no hay nada que rotar
        if (estadoActual.Count <= 1) return;

        // 2. Crear el ServerRpc para solicitar la rotación (debe implementarse en un componente de red)
        GameObject polloManager = new GameObject("PolloManager_Temp");
        PolloEffectManager effectManager = polloManager.AddComponent<PolloEffectManager>();
        effectManager.IniciarRotacion(estadoActual);
    }
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente de red para manejar la rotación de ingredientes (efecto Pollo)
/// ‡‡</summary>_PLACEHOLDER‡‡
public class PolloEffectManager : NetworkBehaviour
{
    private List<(GameObject nodo, GameObject ingrediente)> estadoInicial;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el proceso de rotación
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarRotacion(List<(GameObject nodo, GameObject ingrediente)> estado)
    {
        this.estadoInicial = estado;

        // Spawnear en red para poder ejecutar ServerRpc
        GetComponent<NetworkObject>().Spawn();

        // Solicitar rotación al servidor
        RotarIngredientesServerRpc();
    }

    [ServerRpc]
    private void RotarIngredientesServerRpc()
    {
        // Crear un nuevo estado con los ingredientes rotados un espacio
        List<GameObject> ingredientesRotados = new List<GameObject>();

        // Primero, extraer solo los ingredientes en orden
        foreach (var (_, ingrediente) in estadoInicial)
        {
            ingredientesRotados.Add(ingrediente);
        }

        // Rotar la lista un espacio (mover último al primero)
        if (ingredientesRotados.Count > 0)
        {
            GameObject ultimo = ingredientesRotados[ingredientesRotados.Count - 1];
            ingredientesRotados.RemoveAt(ingredientesRotados.Count - 1);
            ingredientesRotados.Insert(0, ultimo);
        }

        // Aplicar el estado rotado
        for (int i = 0; i < estadoInicial.Count; i++)
        {
            GameObject nodo = estadoInicial[i].nodo;
            GameObject nuevoIngrediente = i < ingredientesRotados.Count ? ingredientesRotados[i] : null;

            Node componenteNodo = nodo.GetComponent<Node>();
            if (componenteNodo != null)
            {
                // Limpiar ingrediente actual si existe
                if (componenteNodo.hasIngredient.Value && componenteNodo.currentIngredient != null)
                {
                    // Despawnear el ingrediente actual
                    NetworkObject netObj = componenteNodo.currentIngredient.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned)
                    {
                        netObj.Despawn();
                    }
                    componenteNodo.hasIngredient.Value = false;
                    componenteNodo.currentIngredient = null;
                }

                // Asignar nuevo ingrediente si existe
                if (nuevoIngrediente != null)
                {
                    GameObject ingredientePrefab = nuevoIngrediente.GetComponent<ResourcesSO>().prefab3D;
                    componenteNodo.SetNodeIngredient(ingredientePrefab);
                }
            }
        }

        // Destruir este objeto después de completar el efecto
        Destroy(gameObject, 0.5f);
    }
}
