using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;


public class PolloEffectManager : NetworkBehaviour, IEffectManager
{
    [SerializeField] private float velocidadRotacion = 90f;
    [SerializeField] private float duracionEfecto = 3f;
    [SerializeField] private float radioOrbita = 0.5f;

    // Referencias
    private GameObject nodoOrigen;
    private List<GameObject> nodosAfectados = new List<GameObject>();
    private Dictionary<GameObject, Vector3> posicionesOriginales = new Dictionary<GameObject, Vector3>();
    private List<GameObject> ingredientesOrdenados = new List<GameObject>();
    private List<Node> nodosOrdenados = new List<Node>();

    // Estado del efecto
    private float tiempoAnimacion = 0f;
    private bool efectoActivo = false;
    private bool rotacionCompletada = false;
    private IngredientesSO _ingredienteConfigurado;

    // Método de configuración requerido por la interfaz IEffectManager
    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        _ingredienteConfigurado = ingrediente;
        Debug.Log($"PolloEffectManager configurado con: {ingrediente.name}");
    }

    // Método principal para iniciar el efecto
    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Asegurar que estamos en red
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        this.nodoOrigen = nodoOrigen;
        this.nodosAfectados = new List<GameObject>(nodosAfectados);

        // Limpiar colecciones
        posicionesOriginales.Clear();
        ingredientesOrdenados.Clear();
        nodosOrdenados.Clear();

        // Recopilar información de los nodos e ingredientes afectados
        foreach (var nodoObj in nodosAfectados)
        {
            Node nodo = nodoObj.GetComponent<Node>();
            if (nodo != null && nodo.hasIngredient.Value && nodo.currentIngredient != null)
            {
                // Guardar el ingrediente y su nodo
                GameObject ingrediente = nodo.currentIngredient;
                ingredientesOrdenados.Add(ingrediente);
                nodosOrdenados.Add(nodo);

                // Guardar posición original para la animación
                posicionesOriginales[ingrediente] = ingrediente.transform.position;
            }
        }

        // Verificar si hay ingredientes para rotar
        if (ingredientesOrdenados.Count < 2)
        {
            Debug.Log("No hay suficientes ingredientes para rotar");
            FinalizarEfectoLogica();
            return;
        }

        // Iniciar efecto
        tiempoAnimacion = 0f;
        rotacionCompletada = false;
        efectoActivo = true;

        // Notificar al servidor
        IniciarEfectoServerRpc();

        Debug.Log($"Efecto Pollo iniciado con {ingredientesOrdenados.Count} ingredientes");
    }

    [ServerRpc(RequireOwnership = false)]
    private void IniciarEfectoServerRpc()
    {
        IniciarEfectoClientRpc();
    }

    [ClientRpc]
    private void IniciarEfectoClientRpc()
    {
        // Efectos visuales adicionales de inicio si los hubiera
    }

    private void Update()
    {
        if (!efectoActivo) return;

        // Actualizar tiempo de animación
        tiempoAnimacion += Time.deltaTime;

        if (tiempoAnimacion <= duracionEfecto)
        {
            // FASE 1: Animación visual de rotación
            AnimarRotacion();
        }
        else if (!rotacionCompletada)
        {
            // FASE 2: Realizar el intercambio permanente
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
        // Calcular porcentaje de progreso
        float porcentajeCompletado = tiempoAnimacion / duracionEfecto;
        float anguloActual = porcentajeCompletado * 360f; // Una rotación completa

        // Centro de rotación (posición del pollo)
        Vector3 posicionCentral = nodoOrigen.transform.position;

        // Animar cada ingrediente
        for (int i = 0; i < ingredientesOrdenados.Count; i++)
        {
            GameObject ingrediente = ingredientesOrdenados[i];

            if (ingrediente != null)
            {
                // Calcular posición orbital específica para este ingrediente
                float anguloBase = (360f / ingredientesOrdenados.Count) * i;
                float anguloFinal = anguloBase + anguloActual;
                float radianes = anguloFinal * Mathf.Deg2Rad;

                // Calcular posición en órbita
                Vector3 desplazamiento = new Vector3(
                    Mathf.Sin(radianes) * radioOrbita,
                    0,
                    Mathf.Cos(radianes) * radioOrbita
                );

                Vector3 nuevaPosicion = posicionCentral + desplazamiento;
                nuevaPosicion.y = ingrediente.transform.position.y; // Mantener altura

                // Aplicar posición visual durante la animación
                ingrediente.transform.position = nuevaPosicion;

                // Hacer que el ingrediente mire hacia el centro
                ingrediente.transform.LookAt(new Vector3(posicionCentral.x, ingrediente.transform.position.y, posicionCentral.z));
            }
        }
    }

    private void RealizarIntercambioDeIngredientes()
    {
        if (!IsServer) return;

        Debug.Log("Realizando intercambio permanente de ingredientes");

        // Obtener datos de los prefabs actuales
        Dictionary<Node, string> nombresPrefabs = new Dictionary<Node, string>();

        for (int i = 0; i < nodosOrdenados.Count; i++)
        {
            Node nodo = nodosOrdenados[i];
            GameObject ingrediente = ingredientesOrdenados[i];

            if (nodo != null && ingrediente != null)
            {
                // Obtener nombre del prefab a través del componente
                componente comp = ingrediente.GetComponent<componente>();
                if (comp != null && comp.data != null)
                {
                    // Usar el nombre del dato del ScriptableObject en lugar del GameObject
                    // Esto evita el problema del sufijo "(Clone)"
                    string nombrePrefab = comp.data.name;
                    nombresPrefabs[nodo] = nombrePrefab;
                    Debug.Log($"Ingrediente en nodo {nodo.position}: {nombrePrefab}");
                }
                else
                {
                    // Si no podemos obtener el nombre desde comp.data, usar el nombre del GameObject
                    string nombrePrefab = ingrediente.name.Replace("(Clone)", "");
                    nombresPrefabs[nodo] = nombrePrefab;
                    Debug.Log($"Ingrediente (nombre alternativo) en nodo {nodo.position}: {nombrePrefab}");
                }
            }
        }

        // Rotar la lista de nodos para crear el efecto de intercambio
        if (nodosOrdenados.Count > 0)
        {
            Node primerNodo = nodosOrdenados[0];
            nodosOrdenados.RemoveAt(0);
            nodosOrdenados.Add(primerNodo);
        }

        // Limpiar todos los nodos primero
        foreach (var nodo in nodosOrdenados)
        {
            nodo.ClearNodeIngredient();
        }

        // Esperar un frame para asegurar que los nodos estén limpios
        StartCoroutine(ColocarIngredientesDespuesDeUnFrame(nombresPrefabs, nodosOrdenados));
    }


    private IEnumerator ColocarIngredientesDespuesDeUnFrame(Dictionary<Node, string> nombresPrefabs, List<Node> nodosRotados)
    {
        // Esperar un frame
        yield return null;

        // Colocar ingredientes en los nuevos nodos
        foreach (var nodo in nodosOrdenados)
        {
            if (nombresPrefabs.ContainsKey(nodo))
            {
                string nombrePrefabConClone = nombresPrefabs[nodo];
                string nombrePrefab = nombrePrefabConClone.Replace("(Clone)", "");

                Debug.Log($"Intentando colocar ingrediente: {nombrePrefab} en nodo {nodo.position}");

                // Estrategia 1: Cargar directamente de Resources usando la ruta conocida
                GameObject prefab = Resources.Load<GameObject>($"Prefabs/Ingredients(Network)/{nombrePrefab}");

                // Estrategia 2: Si falla la primera estrategia, intentar obtener el IngredientesSO
                // para ver si tiene una referencia al prefab
                if (prefab == null && IngredientManager.Instance != null)
                {
                    IngredientesSO datosSO = IngredientManager.Instance.GetIngredienteByName(nombrePrefab);
                    if (datosSO != null && datosSO.prefab3D != null)
                    {
                        prefab = datosSO.prefab3D;
                        Debug.Log($"Prefab obtenido a través de IngredientesSO: {nombrePrefab}");
                    }
                }

                // Estrategia 3: Intentar carga directa sin subdirectorio
                if (prefab == null)
                {
                    prefab = Resources.Load<GameObject>(nombrePrefab);
                    if (prefab != null)
                    {
                        Debug.Log($"Prefab encontrado en raíz de Resources: {nombrePrefab}");
                    }
                }

                // Estrategia 4: Última oportunidad - intentar acceder a LocalGameManager
                if (prefab == null && LocalGameManager.Instance != null)
                {
                    if (LocalGameManager.Instance.currentIngredient != null &&
                        LocalGameManager.Instance.currentIngredient.name.Replace("(Clone)", "") == nombrePrefab)
                    {
                        prefab = LocalGameManager.Instance.currentIngredient;
                        Debug.Log($"Prefab obtenido de LocalGameManager: {nombrePrefab}");
                    }
                }

                // Si hemos encontrado el prefab, colocarlo
                if (prefab != null)
                {
                    nodo.SetNodeIngredient(prefab);
                    Debug.Log($"Ingrediente {nombrePrefab} colocado con éxito en nodo {nodo.position}");
                }
                else
                {
                    Debug.LogError($"No se pudo encontrar el prefab: {nombrePrefab} después de intentar múltiples estrategias");
                }
            }
        }

        // Notificar que se ha completado el intercambio
        IntercambioCompletadoClientRpc();
    }

    [ClientRpc]
    private void IntercambioCompletadoClientRpc()
    {
        Debug.Log("Intercambio de ingredientes completado con éxito");
    }

    public void LimpiarEfecto()
    {
        FinalizarEfectoLogica();
    }

    private void FinalizarEfectoLogica()
    {
        if (!efectoActivo) return;

        efectoActivo = false;

        // Si no completamos la rotación, restaurar posiciones originales
        if (!rotacionCompletada)
        {
            foreach (var ingrediente in ingredientesOrdenados)
            {
                if (ingrediente != null && posicionesOriginales.ContainsKey(ingrediente))
                {
                    ingrediente.transform.position = posicionesOriginales[ingrediente];
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