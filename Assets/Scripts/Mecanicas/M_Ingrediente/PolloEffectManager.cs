using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System;


public class PolloEffectManager : NetworkBehaviour, IEffectManager
{
    [SerializeField] private float velocidadRotacion = 90f;
    [SerializeField] private float duracionEfecto = 3f;
    [SerializeField] private float radioOrbita = 0.5f;

    private GameObject nodoOrigen;
    private List<GameObject> nodosAfectados = new List<GameObject>();
    private Dictionary<int, Node> mapaIndexNodo = new Dictionary<int, Node>();
    private Dictionary<int, GameObject> mapaIndexIngrediente = new Dictionary<int, GameObject>();
    private float tiempoAnimacion = 0f;
    private bool efectoActivo = false;
    private bool rotacionCompletada = false;
    private IngredientesSO _ingredienteConfigurado;

    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
    }

    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        this.nodoOrigen = nodoOrigen;
        this.nodosAfectados = new List<GameObject>(nodosAfectados);

        // Construir una lista simple con los ingredientes afectados
        mapaIndexNodo.Clear();
        mapaIndexIngrediente.Clear();

        // Filtrar solo nodos con ingredientes y asignarles un índice
        List<Node> nodosConIngredientes = new List<Node>();

        foreach (var nodoObj in nodosAfectados)
        {
            Node nodo = nodoObj.GetComponent<Node>();
            if (nodo != null && nodo.hasIngredient.Value && nodo.currentIngredient != null)
            {
                nodosConIngredientes.Add(nodo);
            }
        }

        // Si no hay suficientes ingredientes, cancelar
        if (nodosConIngredientes.Count < 2)
        {
            Debug.Log("No hay suficientes ingredientes para rotar");
            FinalizarEfectoLogica();
            return;
        }

        // Generar índices secuenciales para los nodos e ingredientes
        for (int i = 0; i < nodosConIngredientes.Count; i++)
        {
            mapaIndexNodo[i] = nodosConIngredientes[i];
            mapaIndexIngrediente[i] = nodosConIngredientes[i].currentIngredient;
            Debug.Log($"Índice {i}: Nodo en {nodosConIngredientes[i].position} con ingrediente {nodosConIngredientes[i].currentIngredient.name}");
        }

        tiempoAnimacion = 0f;
        rotacionCompletada = false;
        efectoActivo = true;

        IniciarEfectoServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void IniciarEfectoServerRpc()
    {
        IniciarEfectoClientRpc();
    }

    [ClientRpc]
    private void IniciarEfectoClientRpc() { }

    private void Update()
    {
        if (!efectoActivo) return;

        tiempoAnimacion += Time.deltaTime;

        if (tiempoAnimacion <= duracionEfecto)
        {
            // Animación visual
            AnimarRotacion();
        }
        else if (!rotacionCompletada)
        {
            rotacionCompletada = true;

            if (IsServer)
            {
                RealizarIntercambioDeIngredientes();
            }

            FinalizarEfectoLogica();
        }
    }

    private void AnimarRotacion()
    {
        float porcentajeCompletado = tiempoAnimacion / duracionEfecto;
        float anguloActual = porcentajeCompletado * 360f;

        Vector3 posicionCentral = nodoOrigen.transform.position;

        // Animar cada ingrediente según su índice
        for (int i = 0; i < mapaIndexIngrediente.Count; i++)
        {
            GameObject ingrediente = mapaIndexIngrediente[i];
            if (ingrediente == null) continue;

            // Distribuir los ingredientes en un círculo y rotarlos
            float anguloBase = (360f / mapaIndexIngrediente.Count) * i;
            float anguloFinal = anguloBase + anguloActual;
            float radianes = anguloFinal * Mathf.Deg2Rad;

            Vector3 desplazamiento = new Vector3(
                Mathf.Sin(radianes) * radioOrbita,
                0,
                Mathf.Cos(radianes) * radioOrbita
            );

            Vector3 nuevaPosicion = posicionCentral + desplazamiento;
            nuevaPosicion.y = ingrediente.transform.position.y; // Mantener altura

            ingrediente.transform.position = nuevaPosicion;
            ingrediente.transform.LookAt(new Vector3(posicionCentral.x, ingrediente.transform.position.y, posicionCentral.z));
        }
    }

    private void RealizarIntercambioDeIngredientes()
    {
        if (!IsServer) return;

        Debug.Log("Realizando rotación en sentido horario");

        // Obtener los nombres de prefabs de cada ingrediente
        Dictionary<int, string> nombresPrefabs = new Dictionary<int, string>();

        for (int i = 0; i < mapaIndexIngrediente.Count; i++)
        {
            GameObject ingrediente = mapaIndexIngrediente[i];
            if (ingrediente == null) continue;

            // Obtener nombre del prefab a través del componente
            componente comp = ingrediente.GetComponent<componente>();
            if (comp != null && comp.data != null)
            {
                nombresPrefabs[i] = comp.data.name;
            }
            else
            {
                nombresPrefabs[i] = ingrediente.name.Replace("(Clone)", "");
            }
        }

        // ROTACIÓN SIMPLE:
        // Para una rotación en sentido horario, cada ingrediente va al siguiente índice
        // (el último va al primero)
        Dictionary<Node, string> planDeColocacion = new Dictionary<Node, string>();

        for (int i = 0; i < mapaIndexNodo.Count; i++)
        {
            Node nodoDestino = mapaIndexNodo[i];

            // El ingrediente que irá a este nodo es el del índice anterior (rotación horaria)
            int indiceOrigen = (i == 0) ? mapaIndexNodo.Count - 1 : i - 1;

            if (nombresPrefabs.ContainsKey(indiceOrigen))
            {
                string nombrePrefab = nombresPrefabs[indiceOrigen];
                planDeColocacion[nodoDestino] = nombrePrefab;
                Debug.Log($"Plan: Mover {nombrePrefab} al nodo {nodoDestino.position}");
            }
        }

        // Limpiar todos los nodos primero
        foreach (var nodo in mapaIndexNodo.Values)
        {
            nodo.ClearNodeIngredient();
        }

        // Realizar la colocación
        StartCoroutine(EjecutarPlanDeColocacion(planDeColocacion));
    }

    private IEnumerator EjecutarPlanDeColocacion(Dictionary<Node, string> planDeColocacion)
    {
        yield return null; // Esperar un frame

        foreach (var entry in planDeColocacion)
        {
            Node nodo = entry.Key;
            string nombrePrefab = entry.Value;

            Debug.Log($"Colocando {nombrePrefab} en nodo {nodo.position}");

            // Intentar cargar el prefab de varias formas
            GameObject prefab = null;

            // Opción 1: Directamente de Resources
            string nombrePrefabFormateado = char.ToUpper(nombrePrefab[0]) + nombrePrefab.Substring(1);
            prefab = Resources.Load<GameObject>($"Prefabs/Ingredients(Network)/{nombrePrefabFormateado}");

            // Registro para depuración
            Debug.Log($"Intentando cargar: Prefabs/Ingredients(Network)/{nombrePrefabFormateado}");

            // Opción 2: A través de IngredientManager si está disponible
            if (prefab == null && IngredientManager.Instance != null)
            {
                IngredientesSO datosSO = IngredientManager.Instance.GetIngredienteByName(nombrePrefab);
                if (datosSO != null && datosSO.prefab3D != null)
                {
                    prefab = datosSO.prefab3D;
                }
            }

            // Si hemos encontrado el prefab, colocarlo
            if (prefab != null)
            {
                nodo.SetNodeIngredient(prefab);
                Debug.Log($"Colocado {nombrePrefab} en nodo {nodo.position}");
            }
            else
            {
                Debug.LogError($"No se pudo encontrar el prefab: {nombrePrefab}");
            }
        }

        IntercambioCompletadoClientRpc();
    }

    [ClientRpc]
    private void IntercambioCompletadoClientRpc()
    {
        Debug.Log("Rotación de ingredientes completada");
    }

    public void LimpiarEfecto()
    {
        FinalizarEfectoLogica();
    }

    private void FinalizarEfectoLogica()
    {
        if (!efectoActivo) return;

        efectoActivo = false;

        if (!rotacionCompletada)
        {
            // Restaurar posiciones originales si no completamos la rotación
            foreach (var entry in mapaIndexIngrediente)
            {
                GameObject ingrediente = entry.Value;
                int indice = entry.Key;

                if (ingrediente != null && mapaIndexNodo.ContainsKey(indice))
                {
                    Node nodoOriginal = mapaIndexNodo[indice];
                    ingrediente.transform.position = nodoOriginal.transform.position;
                    ingrediente.transform.rotation = Quaternion.identity;
                }
            }
        }

        if (IsSpawned)
        {
            FinalizarEfectoServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void FinalizarEfectoServerRpc()
    {
        FinalizarEfectoClientRpc();
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    [ClientRpc]
    private void FinalizarEfectoClientRpc()
    {
        Debug.Log("Efecto Pollo finalizado");
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
        else if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}