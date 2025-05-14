using UnityEngine;
using Unity.Netcode;

public class UtensilioButton3D : MonoBehaviour, IInteractuable
{
    [Header("Configuración")]
    [SerializeField] private string nombreUtensilio;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material materialNormal;
    [SerializeField] private Material materialSeleccionado;
    [SerializeField] private Material materialNoDisponible;

    // Referencia al utensilio
    private Utensilio utensilioData;

    // Visual para indicador de precio
    [SerializeField] private TextMesh textoPrecio;

    private void Awake()
    {
        // Establecer el layer correcto para la interacción
        gameObject.layer = LayerMask.NameToLayer("Interact");

        // Asegurar que tenemos las referencias
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        // Guardar el material normal si no está asignado
        if (materialNormal == null && meshRenderer != null)
            materialNormal = meshRenderer.material;
    }

    private void Start()
    {
        // Cargar datos del utensilio
        CargarDatosUtensilio();

        // Actualizar visuales
        ActualizarEstadoVisual();
    }

    private void CargarDatosUtensilio()
    {
        if (string.IsNullOrEmpty(nombreUtensilio)) return;

        if (UtensiliosManager.Instance != null)
        {
            utensilioData = UtensiliosManager.Instance.GetUtensilioPorNombre(nombreUtensilio);

            if (utensilioData != null)
            {
                // Actualizar el mesh si es posible
                if (utensilioData.prefab3D != null)
                {
                    MeshFilter meshSource = utensilioData.prefab3D.GetComponent<MeshFilter>();
                    if (meshSource != null)
                    {
                        MeshFilter myMesh = GetComponent<MeshFilter>();
                        if (myMesh != null)
                        {
                            myMesh.mesh = meshSource.sharedMesh;
                        }
                    }
                }

                // Actualizar texto de precio
                if (textoPrecio != null)
                {
                    textoPrecio.text = utensilioData.Price.ToString();
                }
            }
        }
    }

    // MÉTODO AUXILIAR: Obtener la economía del jugador local
    private Economia ObtenerEconomiaJugadorLocal()
    {
        // Obtener el ID de cliente local
        ulong clienteLocalId = NetworkManager.Singleton.LocalClientId;

        // Buscar el NodeMap que pertenece a este cliente
        NodeMap[] todosLosTableros = FindObjectsOfType<NodeMap>();
        foreach (NodeMap tablero in todosLosTableros)
        {
            if (tablero.ownerClientId == clienteLocalId)
            {
                // Devolver la economía asociada a este tablero
                return tablero.economia;
            }
        }

        Debug.LogWarning("No se encontró un tablero para el jugador local");
        return null;
    }

    public void Interactuar()
    {
        Debug.Log($"Botón de utensilio {nombreUtensilio} interactuado");

        if (utensilioData == null)
        {
            Debug.LogWarning($"No se encontraron datos para el utensilio: {nombreUtensilio}");
            return;
        }

        // Obtener la economía del jugador local
        Economia economiaLocal = ObtenerEconomiaJugadorLocal();
        if (economiaLocal == null)
        {
            Debug.LogError("No se pudo obtener la economía del jugador local");
            return;
        }

        // Verificar si tenemos suficiente dinero
        bool tieneSuficienteDinero = economiaLocal.money.Value >= utensilioData.Price;

        if (!tieneSuficienteDinero)
        {
            Debug.Log($"Dinero insuficiente para {nombreUtensilio}: " +
                     $"necesitas {utensilioData.Price}, tienes {economiaLocal.money.Value}");
            return;
        }

        // Seleccionar el utensilio
        if (LocalGameManager.Instance != null)
        {
            LocalGameManager.Instance.SeleccionarUtensilio(utensilioData);

            // Actualizar todos los botones
            UtensilioButton3D[] todosBotones = FindObjectsOfType<UtensilioButton3D>();
            foreach (var boton in todosBotones)
            {
                boton.ActualizarEstadoVisual();
            }
        }
    }

    public void ActualizarEstadoVisual()
    {
        if (meshRenderer == null || utensilioData == null) return;

        // Verificar si está seleccionado
        bool seleccionado = false;
        if (LocalGameManager.Instance != null &&
            LocalGameManager.Instance.currentUtensilio == utensilioData)
        {
            seleccionado = true;
        }

        // Obtener la economía del jugador local para verificar dinero disponible
        Economia economiaLocal = ObtenerEconomiaJugadorLocal();
        bool tieneSuficienteDinero = true;

        if (economiaLocal != null)
        {
            tieneSuficienteDinero = economiaLocal.money.Value >= utensilioData.Price;
        }

        // Asignar material según estado
        if (seleccionado && materialSeleccionado != null)
        {
            meshRenderer.material = materialSeleccionado;
        }
        else if (!tieneSuficienteDinero && materialNoDisponible != null)
        {
            meshRenderer.material = materialNoDisponible;
        }
        else
        {
            meshRenderer.material = materialNormal;
        }
    }

    private void OnEnable()
    {
        // Actualizar al activar
        ActualizarEstadoVisual();
    }
}