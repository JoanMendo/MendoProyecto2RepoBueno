using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que detecta patrones de ingredientes en el tablero y aplica efectos
/// ‡‡</summary>_PLACEHOLDER‡‡
public class PatternDetector : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool detectarAutomaticamente = true;
    [SerializeField] private float intervaloDeteccion = 0.5f;
    [SerializeField] private bool mostrarDebug = false;

    // Referencias y variables internas
    private NodeMap nodeMap;
    private List<PatternDefinitions.Patron> patrones = new List<PatternDefinitions.Patron>();
    private List<PatternDefinitions.PatronDetectado> patronesActivos = new List<PatternDefinitions.PatronDetectado>();
    private float ultimaDeteccion = 0f;

    // Evento para notificar cuando se detecta un patrón
    public delegate void PatronDetectadoHandler(PatternDefinitions.PatronDetectado patron);
    public static event PatronDetectadoHandler OnPatronDetectado;

    private void Start()
    {
        // Obtener referencia al mapa de nodos
        nodeMap = GetComponent<NodeMap>();
        if (nodeMap == null)
        {
            Debug.LogError("PatternDetector requiere un NodeMap en el mismo GameObject");
            enabled = false;
            return;
        }

        // Cargar patrones predefinidos
        patrones = PatternDefinitions.ObtenerPatronesDisponibles();

        if (mostrarDebug)
        {
            Debug.Log($"PatternDetector inicializado con {patrones.Count} patrones");
        }
    }

    private void Update()
    {
        // Solo el servidor realiza la detección
        if (!IsServer) return;

        // Detección automática según intervalo
        if (detectarAutomaticamente && Time.time - ultimaDeteccion > intervaloDeteccion)
        {
            DetectarPatrones();
            ultimaDeteccion = Time.time;
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Detecta todos los patrones activos en el tablero
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void DetectarPatrones()
    {
        if (!IsServer || nodeMap == null) return;

        // Lista para almacenar los nuevos patrones detectados
        List<PatternDefinitions.PatronDetectado> nuevosPatrones = new List<PatternDefinitions.PatronDetectado>();

        // Obtener todos los nodos del tablero
        List<GameObject> nodos = nodeMap.nodesList;
        if (nodos == null || nodos.Count == 0) return;

        // Comprobar cada nodo como posible origen de un patrón
        foreach (GameObject nodoOriginal in nodos)
        {
            Node nodoComp = nodoOriginal.GetComponent<Node>();
            if (nodoComp == null || nodoComp.currentIngredient == null) continue;

            // Comprobar cada patrón desde este nodo
            foreach (PatternDefinitions.Patron patron in patrones)
            {
                // Probar todas las transformaciones del patrón
                foreach (List<Vector2Int> transformacion in patron.GenerarTransformaciones())
                {
                    PatternDefinitions.PatronDetectado detectado = ComprobarPatron(nodoOriginal, patron, transformacion);
                    if (detectado != null)
                    {
                        nuevosPatrones.Add(detectado);
                        if (mostrarDebug)
                        {
                            Debug.Log($"Patrón '{patron.nombre}' detectado en {nodoComp.position}");
                        }
                    }
                }
            }
        }

        // Aplicar nuevos patrones y notificar a los clientes
        foreach (PatternDefinitions.PatronDetectado nuevo in nuevosPatrones)
        {
            // Verificar si ya existe este patrón
            bool existente = false;
            foreach (PatternDefinitions.PatronDetectado activo in patronesActivos)
            {
                if (SonPatronesIguales(nuevo, activo))
                {
                    existente = true;
                    break;
                }
            }

            if (!existente)
            {
                // Aplicar efecto
                nuevo.AplicarEfecto();

                // Notificar a los clientes
                foreach (GameObject nodoObjetivo in nuevo.nodosObjetivo)
                {
                    NetworkObject netObj = nodoObjetivo.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        NotificarEfectoClientRpc(
                            nuevo.nombre,
                            netObj.NetworkObjectId,
                            nuevo.efecto.nombre,
                            nuevo.efecto.colorEfecto
                        );
                    }
                }

                // Disparar evento
                OnPatronDetectado?.Invoke(nuevo);
            }
        }

        // Actualizar lista de patrones activos
        patronesActivos = nuevosPatrones;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Comprueba si un patrón coincide desde un nodo específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private PatternDefinitions.PatronDetectado ComprobarPatron(
        GameObject nodoOrigen,
        PatternDefinitions.Patron patron,
        List<Vector2Int> transformacion)
    {
        Node nodoOrigenComp = nodoOrigen.GetComponent<Node>();
        if (nodoOrigenComp == null) return null;

        // Posición del nodo origen en la cuadrícula
        Vector2Int posOrigen = new Vector2Int(
            Mathf.RoundToInt(nodoOrigenComp.position.x),
            Mathf.RoundToInt(nodoOrigenComp.position.y)
        );

        // Lista de nodos que coinciden con el patrón
        List<GameObject> nodosCoincidentes = new List<GameObject>();
        Dictionary<Vector2Int, GameObject> mapaPositionToNodo = new Dictionary<Vector2Int, GameObject>();

        // Verificar cada posición relativa del patrón
        foreach (Vector2Int posRelativa in transformacion)
        {
            // Calcular posición absoluta en el tablero
            Vector2Int posAbsoluta = posOrigen + posRelativa;

            // Buscar nodo en esta posición
            GameObject nodo = null;
            foreach (GameObject n in nodeMap.nodesList)
            {
                Node comp = n.GetComponent<Node>();
                if (comp != null &&
                    Mathf.RoundToInt(comp.position.x) == posAbsoluta.x &&
                    Mathf.RoundToInt(comp.position.y) == posAbsoluta.y)
                {
                    nodo = n;
                    break;
                }
            }

            // Si no hay nodo en esta posición o no tiene ingrediente, no coincide
            if (nodo == null)
            {
                return null;
            }

            Node nodoComp = nodo.GetComponent<Node>();
            if (nodoComp == null || nodoComp.currentIngredient == null)
            {
                return null;
            }

            // Agregar a la lista de coincidencias
            nodosCoincidentes.Add(nodo);
            mapaPositionToNodo[posRelativa] = nodo;
        }

        // Verificar ingredientes requeridos
        if (patron.ingredientesRequeridos.Count > 0)
        {
            HashSet<string> ingredientesEncontrados = new HashSet<string>();

            foreach (GameObject nodo in nodosCoincidentes)
            {
                Node comp = nodo.GetComponent<Node>();
                if (comp != null && comp.currentIngredient != null)
                {
                    // Aquí deberías obtener el nombre del ingrediente
                    // Ejemplo: ingredientesEncontrados.Add(comp.currentIngredient.GetComponent<IngredienteSO>().nombre);
                    ingredientesEncontrados.Add("NombreIngrediente"); // Reemplazar con el código real
                }
            }

            // Verificar que todos los ingredientes requeridos estén presentes
            foreach (string ingrediente in patron.ingredientesRequeridos)
            {
                if (!ingredientesEncontrados.Contains(ingrediente))
                {
                    return null;
                }
            }
        }

        // Si llegamos aquí, el patrón coincide
        PatternDefinitions.PatronDetectado resultado = new PatternDefinitions.PatronDetectado
        {
            nombre = patron.nombre,
            nodos = nodosCoincidentes,
            efecto = patron.efecto
        };

        // Determinar nodos objetivo según el modo configurado
        switch (patron.modoObjetivo)
        {
            case PatternDefinitions.Patron.ModoObjetivo.TodosLosNodos:
                resultado.nodosObjetivo = new List<GameObject>(nodosCoincidentes);
                break;

            case PatternDefinitions.Patron.ModoObjetivo.NodoCentral:
                if (mapaPositionToNodo.ContainsKey(Vector2Int.zero))
                {
                    resultado.nodosObjetivo.Add(mapaPositionToNodo[Vector2Int.zero]);
                }
                else if (nodosCoincidentes.Count > 0)
                {
                    resultado.nodosObjetivo.Add(nodosCoincidentes[0]);
                }
                break;

            case PatternDefinitions.Patron.ModoObjetivo.PosicionesEspecificas:
                foreach (Vector2Int pos in patron.posicionesObjetivo)
                {
                    // Aquí debería transformar las posiciones objetivo según la transformación aplicada
                    // Por simplicidad, usaremos las posiciones sin transformar
                    if (mapaPositionToNodo.ContainsKey(pos))
                    {
                        resultado.nodosObjetivo.Add(mapaPositionToNodo[pos]);
                    }
                }
                break;
        }

        return resultado;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Compara dos patrones detectados para ver si son iguales
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private bool SonPatronesIguales(PatternDefinitions.PatronDetectado patron1, PatternDefinitions.PatronDetectado patron2)
    {
        if (patron1.nombre != patron2.nombre) return false;
        if (patron1.nodos.Count != patron2.nodos.Count) return false;

        // Comparar contenido sin importar orden
        HashSet<GameObject> set1 = new HashSet<GameObject>(patron1.nodos);
        HashSet<GameObject> set2 = new HashSet<GameObject>(patron2.nodos);

        return set1.SetEquals(set2);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Notifica a los clientes sobre un efecto aplicado
    /// ‡‡</summary>_PLACEHOLDER‡‡
    [ClientRpc]
    private void NotificarEfectoClientRpc(
        string nombrePatron,
        ulong nodoId,
        string nombreEfecto,
        Color colorEfecto)
    {
        if (IsServer) return; // El servidor ya lo procesó

        // Encontrar nodo por su ID
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodoId, out NetworkObject netObj))
        {
            return;
        }

        GameObject nodo = netObj.gameObject;

        // Aplicar efectos visuales en el cliente
        Debug.Log($"Efecto '{nombreEfecto}' aplicado por patrón '{nombrePatron}'");

        // Aquí añadirías efectos visuales, como:
        // - Destacar el nodo
        // - Mostrar partículas
        // - Mostrar texto flotante
        // - Reproducir efectos de sonido
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Método público para forzar la detección de patrones
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ForzarDeteccion()
    {
        DetectarPatrones();
    }
}