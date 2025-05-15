using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Node : NetworkBehaviour, IInteractuable
{
    [Header("Posición")]
    public Vector2Int position;

    [Header("Estado")]
    public NetworkVariable<bool> hasIngredient = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hasUtensilio = new NetworkVariable<bool>(false);

    // En la clase Node, añadir estos campos
    [Header("Indicadores Visuales")]
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private GameObject programmedIndicator;
    [SerializeField] private GameObject executingIndicator;

    [Header("Referencias")]
    public GameObject currentIngredient;
    public GameObject currentUtensilio;
    public List<GameObject> vecinos = new List<GameObject>();

    [Header("Componentes")]
    public MeshRenderer meshRenderer;
    public BoxCollider nodeCollider;

    // Mantener la referencia a NodeMap para compatibilidad
    [HideInInspector] public NodeMap nodeMap;
    // Materiales para estados visuales
    private Material materialNormal;
    private Material materialResaltado;

    // Lista de efectos asociados con este nodo
    private List<Efectos> efectosAsociados = new List<Efectos>();

    // Propiedades para modificaciones
    private NetworkVariable<int> modificacionRango = new NetworkVariable<int>(0);
    private NetworkVariable<float> modificacionVida = new NetworkVariable<float>(0);
    private NetworkVariable<bool> esMovible = new NetworkVariable<bool>(true);

    // Debug
    [SerializeField] private bool mostrarDebug = false;

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Inicializa el nodo con referencias necesarias
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void Initialize(NodeMap map)
    {
        nodeMap = map;

        // Obtener componentes si no están asignados
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (nodeCollider == null)
            nodeCollider = GetComponent<BoxCollider>();

        if (meshRenderer != null)
            materialNormal = meshRenderer.material;
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Configura el material cuando el nodo está resaltado
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void SetHighlightMaterial(Material material)
    {
        materialResaltado = material;
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Resalta visualmente el nodo
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void Highlight(bool isHighlighted)
    {
        if (meshRenderer == null) return;

        if (isHighlighted && materialResaltado != null)
        {
            meshRenderer.material = materialResaltado;
        }
        else
        {
            meshRenderer.material = materialNormal;
        }
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Método para interacción (implementa IInteractuable)
    /// Permite colocar un ingrediente en el nodo
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void Interactuar()
    {
        Debug.Log($"[Node {position}] Inicia Interactuar()");

        bool esNodoPropio = (nodeMap.ownerClientId == NetworkManager.Singleton.LocalClientId);

        // Si estamos en modo selección de enemigo, derivar a esa lógica
        if (!esNodoPropio && LocalGameManager.Instance.modoSeleccionEnemigo)
        {
            // Procesar como interacción con tablero enemigo
            if (LocalGameManager.Instance.currentEfecto != null)
            {
                LocalGameManager.Instance.ProcesarSeleccionNodoParaEfectoOponente(gameObject);
                return;
            }

            if (LocalGameManager.Instance.currentUtensilio != null)
            {
                LocalGameManager.Instance.ProcesarSeleccionNodoParaUtensilioOponente(gameObject);
                return;
            }

            // Si no hay efecto o utensilio seleccionado, desactivar modo enemigo
            LocalGameManager.Instance.DesactivarModoEnemigo();
            return;
        }

        // Si no es nodo propio y no estamos en modo enemigo, ignorar interacción
        if (!esNodoPropio && !LocalGameManager.Instance.modoSeleccionEnemigo)
        {
            Debug.Log($"[Node {position}] Este nodo pertenece a otro jugador. Active el modo enemigo para interactuar.");
            return;
        }

        // Check if this is an opponent's node
        bool isOpponentNode = nodeMap.ownerClientId != NetworkManager.Singleton.LocalClientId;

        if (isOpponentNode)
        {
            // Only allow specific interactions on opponent's board
            if (LocalGameManager.Instance.currentEfecto != null)
            {
               
                LocalGameManager.Instance.ProcesarSeleccionNodoParaEfectoOponente(gameObject);
                return;
            }

            if (LocalGameManager.Instance.currentUtensilio != null)
            {
                // Process cross-board utensil
                LocalGameManager.Instance.ProcesarSeleccionNodoParaUtensilioOponente(gameObject);
                return;
            }

            // Not allowed to place ingredients on opponent's board
            Debug.Log("No puedes colocar ingredientes en el tablero del oponente");
            return;
        }

        // Verificar autoridad
        Debug.Log($"[Node {position}] Verificando autoridad: IsOwner={IsOwner}, IsServer={IsServer}");
        if (!IsOwner && !IsServer)
        {
            Debug.Log($"[Node {position}] No tiene autoridad para interactuar");
            return;
        }
        // NUEVO: Verificar si estamos en modo efecto
        if (LocalGameManager.Instance.currentEfecto != null)
        {
            // Modo efecto - procesar selección
            LocalGameManager.Instance.ProcesarSeleccionNodoParaEfecto(gameObject);
            return;
        }

        // Verificar si estamos en modo utensilio
        if (LocalGameManager.Instance.currentUtensilio != null)
        {
            // Modo utensilio - procesar selección
            LocalGameManager.Instance.ProcesarSeleccionNodoParaUtensilio(gameObject);
            return;
        }

        // Verificar si ya hay ingrediente
        Debug.Log($"[Node {position}] Verificando estado: hasIngredient={hasIngredient.Value}");
        if (hasIngredient.Value)
        {
            Debug.Log($"[Node {position}] Ya hay un ingrediente en este nodo");
            return;
        }

        // Verificar LocalGameManager
        Debug.Log($"[Node {position}] Verificando LocalGameManager: {(LocalGameManager.Instance != null ? "Encontrado" : "NO ENCONTRADO")}");
        if (LocalGameManager.Instance == null)
        {
            Debug.LogError($"[Node {position}] Error crítico: LocalGameManager.Instance es null");
            return;
        }

        // Verificar ingrediente seleccionado
        GameObject ingredienteSeleccionado = LocalGameManager.Instance.currentIngredient;
        ResourcesSO datosPrefab = LocalGameManager.Instance.currentIngredientData;
        Debug.Log($"[Node {position}] Ingrediente actual: {(ingredienteSeleccionado != null ? ingredienteSeleccionado.name : "NINGUNO")}");
        Debug.Log($"[Node {position}] Datos ingrediente: {(datosPrefab != null ? datosPrefab.Name : "NINGUNO")}");

        if (ingredienteSeleccionado == null || datosPrefab == null)
        {
            Debug.Log($"[Node {position}] No hay ingrediente seleccionado o datos válidos");
            return;
        }

        // Verificar dinero
        float precio = datosPrefab.Price;
        float dineroActual = nodeMap.economia.money.Value;
        Debug.Log($"[Node {position}] Verificando dinero: Precio={precio}, Dinero actual={dineroActual}");

        if (dineroActual < precio)
        {
            Debug.Log($"[Node {position}] Dinero insuficiente: necesitas {precio}, tienes {dineroActual}");
            return;
        }

        // Llamar al ServerRpc
        string nombrePrefab = ingredienteSeleccionado.name;
        Debug.Log($"[Node {position}] Llamando PlaceIngredientServerRpc con {nombrePrefab}");
        PlaceIngredientServerRpc(nombrePrefab);
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// RPC para solicitar al servidor que coloque un ingrediente
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    [ServerRpc(RequireOwnership = false)]
    private void PlaceIngredientServerRpc(string nombrePrefab)
    {
        // Verificar que no haya ingrediente ya
        if (hasIngredient.Value)
        {
            if (mostrarDebug) Debug.Log("Ya hay un ingrediente en este nodo (verificación servidor)");
            return;
        }

        // Buscar el prefab correspondiente
        GameObject prefabIngrediente = BuscarPrefabIngrediente(nombrePrefab);
        if (prefabIngrediente == null)
        {
            Debug.LogError($"No se encontró el prefab {nombrePrefab}");
            return;
        }

        // Obtener el precio del ingrediente
        componente datos = prefabIngrediente.GetComponent<componente>();
        float precio = 0;
        if (datos != null)
        {
            precio = datos.data.Price;
        }

        // Colocar el ingrediente usando el método existente
        SetNodeIngredient(prefabIngrediente);

        // Notificar al cliente (para efectos visuales, sonido, etc.)
        IngredienteColocadoClientRpc();

        // Reducir el dinero del jugador
        Economia economia = FindFirstObjectByType<Economia>();
        if (economia != null)
        {
            economia.less_money(precio);
        }
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Método auxiliar para buscar el prefab de ingrediente por nombre
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    private GameObject BuscarPrefabIngrediente(string nombrePrefab)
    {
        Debug.Log($"[SERVER] Buscando prefab: {nombrePrefab}");

        // OPCIÓN 1: Resources.Load
        GameObject prefab = Resources.Load<GameObject>($"Prefabs/Ingredients(Network)/{nombrePrefab}");
        if (prefab != null)
        {
            Debug.Log($"[SERVER] Prefab encontrado en Resources: {prefab.name}");
            return prefab;
        }



        // OPCIÓN 3: Usar LocalGameManager como último recurso
        if (LocalGameManager.Instance != null &&
            LocalGameManager.Instance.currentIngredient != null &&
            LocalGameManager.Instance.currentIngredient.name == nombrePrefab)
        {
            Debug.Log($"[SERVER] Prefab encontrado en LocalGameManager: {LocalGameManager.Instance.currentIngredient.name}");
            return LocalGameManager.Instance.currentIngredient;
        }

        Debug.LogError($"[SERVER] No se pudo encontrar ningún prefab con nombre: {nombrePrefab}");
        return null;
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Notifica a los clientes que se ha colocado un ingrediente
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    [ClientRpc]
    private void IngredienteColocadoClientRpc()
    {
        // Efectos visuales o sonidos cuando se coloca un ingrediente
        if (mostrarDebug) Debug.Log($"Ingrediente colocado con éxito en posición {position}");

        // Actualizar LocalGameManager si es necesario


        // Aquí podrías reproducir un sonido o efecto visual
        // AudioSource.PlayClipAtPoint(sonidoColocacion, transform.position);
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Limpia cualquier ingrediente en este nodo
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void ClearNodeIngredient()
    {
        if (!IsServer) return;

        hasIngredient.Value = false;

        if (currentIngredient != null)
        {
            // Despawnear ingrediente actual si es objeto de red
            NetworkObject netObj = currentIngredient.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            currentIngredient = null;
        }

        // Notificar a clientes
        LimpiarIngredienteClientRpc();
    }

    [ClientRpc]
    private void LimpiarIngredienteClientRpc()
    {
        if (IsServer) return; // El servidor ya lo hizo

        // Destruir localmente si existe
        if (currentIngredient != null)
        {
            Destroy(currentIngredient);
            currentIngredient = null;
        }
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Coloca un ingrediente en este nodo
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void SetNodeIngredient(GameObject prefabIngrediente)
    {
        if (!IsServer) return;

        // Si ya hay ingrediente, limpiarlo primero
        if (hasIngredient.Value)
        {
            ClearNodeIngredient();
        }

        // Instanciar nuevo ingrediente
        GameObject nuevoIngrediente = Instantiate(prefabIngrediente, transform.position, Quaternion.identity);
        NetworkObject netObj = nuevoIngrediente.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();
            currentIngredient = nuevoIngrediente;
            hasIngredient.Value = true;
        }
        else
        {
            Debug.LogError("Error: El prefab de ingrediente debe tener componente NetworkObject");
            Destroy(nuevoIngrediente);
        }
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Activa el efecto del ingrediente colocado
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    private void ActivarEfectoIngrediente()
    {
        if (!IsServer || currentIngredient == null) return;

        ResourcesSO recurso = currentIngredient.GetComponent<ResourcesSO>();
        if (recurso != null && nodeMap != null)
        {
            // Activar efecto pasando el NodeMap
            recurso.ActivarEfecto(gameObject, nodeMap);
        }
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Aplica un efecto visual de utensilio sobre este nodo
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void SetUtensilioVisual(ResourcesSO utensilio)
    {
        if (!IsServer) return;

        // Limpiar utensilio actual si existe
        if (currentUtensilio != null)
        {
            NetworkObject netObj = currentUtensilio.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            currentUtensilio = null;
            hasUtensilio.Value = false;
        }

        // Si tenemos un nuevo utensilio, mostrarlo
        if (utensilio != null && utensilio.prefab3D != null)
        {
            GameObject visual = Instantiate(utensilio.prefab3D, transform.position, Quaternion.identity);
            NetworkObject netObj = visual.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                netObj.Spawn();
                currentUtensilio = visual;
                hasUtensilio.Value = true;
            }
            else
            {
                Debug.LogError("Error: El prefab de utensilio debe tener componente NetworkObject");
                Destroy(visual);
            }
        }

        // Notificar a clientes
        ActualizarUtensilioClientRpc();
    }

    [ClientRpc]
    private void ActualizarUtensilioClientRpc()
    {
        // Actualizar estado visual si es necesario
    }




    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Modifica el rango de efectos en este nodo
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void ModificarRango(int cantidad)
    {
        if (!IsServer) return;
        modificacionRango.Value += cantidad;
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Modifica la vida del ingrediente en este nodo
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void ModificarVida(float cantidad)
    {
        if (!IsServer) return;
        modificacionVida.Value += cantidad;
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Establece si el ingrediente en este nodo puede moverse
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public void SetMovible(bool movible)
    {
        if (!IsServer) return;
        esMovible.Value = movible;
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Obtiene el rango actual incluyendo modificadores
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public int GetRangoActual()
    {
        if (currentIngredient == null) return 0;

        ResourcesSO recurso = currentIngredient.GetComponent<ResourcesSO>();
        if (recurso == null) return 0;

        return recurso.range + modificacionRango.Value;
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Obtiene la vida actual incluyendo modificadores
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public float GetVidaActual()
    {
        if (currentIngredient == null) return 0;

        ResourcesSO recurso = currentIngredient.GetComponent<ResourcesSO>();
        if (recurso == null) return 0;

        return recurso.vida + modificacionVida.Value;
    }

    /// ‡‡‡‡<summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    /// Comprueba si el ingrediente en este nodo puede moverse
    /// ‡‡‡‡</summary>_PLACEHOLDER‡‡_PLACEHOLDER‡‡
    public bool PuedeMoverse()
    {
        if (currentIngredient == null) return false;

        ResourcesSO recurso = currentIngredient.GetComponent<componente>().data;
        if (recurso == null) return false;

        return recurso.esmovible && esMovible.Value;
    }

    // Añadir estos métodos
    public void MarcarProgramado(Utensilio utensilio)
    {
        if (programmedIndicator != null)
        {
            programmedIndicator.SetActive(true);

            // Opcionalmente, puedes usar un icono que represente el utensilio
            // Image iconoUtensilio = programmedIndicator.GetComponentInChildren<Image>();
            // if (iconoUtensilio != null && utensilio.Sprite != null)
            // {
            //    iconoUtensilio.sprite = utensilio.Sprite;
            // }
        }

        // Desactivar otros indicadores
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
        if (executingIndicator != null)
        {
            executingIndicator.SetActive(false);
        }
    }

    public void MarcarEjecutando()
    {
        if (executingIndicator != null)
        {
            executingIndicator.SetActive(true);
        }
        if (programmedIndicator != null)
        {
            programmedIndicator.SetActive(false);
        }
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }

    public void DesmarcarProgramado()
    {
        if (programmedIndicator != null)
        {
            programmedIndicator.SetActive(false);
        }
        if (executingIndicator != null)
        {
            executingIndicator.SetActive(false);
        }
    }
    // Métodos para marcado visual de selección
    public void MarcarSeleccion()
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(true);
        }
    }

    public void DesmarcarSeleccion()
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }

    // Añadir estos métodos para manejar efectos
    public void AplicarEfecto(Efectos efecto)
    {
        if (!IsServer || efecto == null) return;

        // Crear efectos visuales temporales
        if (efecto.prefab3D != null)
        {
            GameObject efectoVisual = Instantiate(efecto.prefab3D, transform.position, Quaternion.identity);
            // Configurar escala, rotación, etc. según necesidad

            // Destruir después de tiempo
            Destroy(efectoVisual, 2.0f);
        }

        // Si tiene nodeMap, pasar a activar el efecto
        if (nodeMap != null)
        {
            efecto.ActivarEfecto(gameObject, nodeMap);
        }

        // Notificar a clientes
        MostrarEfectoVisualClientRpc(efecto.Name);
    }

    public void MarcarProgramado(Efectos efecto)
    {
        if (programmedIndicator != null)
        {
            programmedIndicator.SetActive(true);

            // Opcionalmente cambiar color según el tipo de efecto
            Renderer indicatorRenderer = programmedIndicator.GetComponent<Renderer>();
            if (indicatorRenderer != null)
            {
                if (efecto is S_Blanca)
                    indicatorRenderer.material.color = Color.white;
                else if (efecto is S_Especial)
                    indicatorRenderer.material.color = Color.green;
                else if (efecto is S_Picante)
                    indicatorRenderer.material.color = Color.red;
            }
        }

        // Desactivar otros indicadores
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
        if (executingIndicator != null)
        {
            executingIndicator.SetActive(false);
        }
    }

    [ClientRpc]
    private void MostrarEfectoVisualClientRpc(string nombreEfecto)
    {
        // Esta función se llama en todos los clientes
        // Podemos mostrar efectos visuales adicionales

        // Buscar el efecto por nombre
        Efectos efecto = null;
        if (EfectosManager.Instance != null)
        {
            efecto = EfectosManager.Instance.GetEfectoPorNombre(nombreEfecto);
        }

        // Solo mostrar en clientes (el servidor ya lo hizo)
        if (!IsServer && efecto != null && efecto.prefab3D != null)
        {
            GameObject efectoVisual = Instantiate(efecto.prefab3D, transform.position, Quaternion.identity);
            // Configurar según necesidad

            // Destruir después de tiempo
            Destroy(efectoVisual, 2.0f);
        }
    }
    /// <summary>
    /// Marca este nodo con un efecto visual para indicar que tiene un efecto asociado
    /// </summary>
    public void MarcarConEfecto(Efectos efecto)
    {
        if (efecto == null) return;

        // Guardar referencia al efecto
        if (!efectosAsociados.Contains(efecto))
        {
            efectosAsociados.Add(efecto);
        }

        // Aplicar algún efecto visual
        // Podría ser un cambio de color, una partícula, etc.
        // Por ejemplo:
        if (efecto is S_Picante)
        {
            // Efecto picante: color rojo
            if (meshRenderer != null)
            {
                Material mat = meshRenderer.material;
                mat.SetColor("_EmissionColor", Color.red * 0.3f);
                mat.EnableKeyword("_EMISSION");
            }
        }
    }

    /// <summary>
    /// Elimina un efecto asociado a este nodo
    /// </summary>
    public void EliminarEfecto(Efectos efecto)
    {
        if (efecto == null) return;

        efectosAsociados.Remove(efecto);

        // Si no quedan efectos, restaurar apariencia normal
        if (efectosAsociados.Count == 0 && meshRenderer != null)
        {
            meshRenderer.material.DisableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// Verifica si este nodo tiene un efecto específico
    /// </summary>
    public bool TieneEfecto(System.Type tipoEfecto)
    {
        return efectosAsociados.Any(e => e.GetType() == tipoEfecto);
    }
}