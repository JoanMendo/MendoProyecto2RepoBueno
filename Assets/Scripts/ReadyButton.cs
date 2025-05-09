using UnityEngine;
using Unity.Netcode;

/// ��<summary>_PLACEHOLDER��
/// Representa un bot�n de "Listo" que puede ser activado por el jugador que tiene autoridad.
/// Comunica su estado al TurnManager y cambia de color visualmente.
/// ��</summary>_PLACEHOLDER��
public class ReadyButton : NetworkBehaviour, IInteractuable
{
    [Header("Referencias")]
    [Tooltip("Referencia al renderer que cambiar� de color")]
    [SerializeField] private Renderer buttonRenderer;

    [Header("Colores de Estado")]
    [Tooltip("Color cuando el jugador est� listo")]
    [SerializeField] private Color readyColor = Color.green;
    [Tooltip("Color cuando el jugador no est� listo")]
    [SerializeField] private Color notReadyColor = Color.red;

    /// ��<summary>_PLACEHOLDER��
    /// Estado de listo/no listo del jugador, sincronizado en red
    /// ��</summary>_PLACEHOLDER��
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false);

    /// ��<summary>_PLACEHOLDER��
    /// Color actual del bot�n, sincronizado en red
    /// ��</summary>_PLACEHOLDER��
    private NetworkVariable<Color> buttonColor = new NetworkVariable<Color>();

    /// ��<summary>_PLACEHOLDER��
    /// Referencia al TurnManager para comunicar cambios de estado
    /// ��</summary>_PLACEHOLDER��
    private TurnManager turnManager;

    private void Awake()
    {
        // Encontrar el TurnManager al inicio
        turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager == null)
        {
            Debug.LogError("No se encontr� TurnManager en la escena");
        }

        // Suscribirse a los cambios de estado
        isReady.OnValueChanged += OnReadyChanged;
        buttonColor.OnValueChanged += OnColorChanged;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Inicializar color seg�n estado inicial
        UpdateButtonColor(isReady.Value);
    }

    private void OnDisable()
    {
        // Desuscribirse de eventos cuando se desactiva
        if (isReady != null)
            isReady.OnValueChanged -= OnReadyChanged;

        if (buttonColor != null)
            buttonColor.OnValueChanged -= OnColorChanged;
    }

    /// ��<summary>_PLACEHOLDER��
    /// M�todo llamado cuando el jugador interact�a con el bot�n
    /// ��</summary>_PLACEHOLDER��
    public void Interactuar()
    {
        // Solo el due�o del objeto puede cambiar su estado
        if (!IsOwner) return;

        // Cambiar estado (funciona en el servidor y se sincroniza)
        UpdateReadyStateServerRpc(!isReady.Value);
    }

    /// ��<summary>_PLACEHOLDER��
    /// RPC para cambiar el estado de listo en el servidor
    /// ��</summary>_PLACEHOLDER��
    [ServerRpc(RequireOwnership = true)]
    private void UpdateReadyStateServerRpc(bool newState)
    {
        isReady.Value = newState;
        UpdateButtonColor(newState);

        // Notificar al TurnManager
        if (turnManager != null)
        {
            turnManager.CheckPlayersReady();
        }
    }

    /// ��<summary>_PLACEHOLDER��
    /// Actualiza el color del bot�n seg�n su estado
    /// ��</summary>_PLACEHOLDER��
    private void UpdateButtonColor(bool ready)
    {
        buttonColor.Value = ready ? readyColor : notReadyColor;
    }

    /// ��<summary>_PLACEHOLDER��
    /// Responde a cambios en el estado de listo
    /// ��</summary>_PLACEHOLDER��
    private void OnReadyChanged(bool oldValue, bool newValue)
    {
        // Si estamos en el servidor, aseguramos que el color sea actualizado
        if (IsServer)
        {
            UpdateButtonColor(newValue);
        }
    }

    /// ��<summary>_PLACEHOLDER��
    /// Responde a cambios en el color del bot�n, actualizando el renderer
    /// ��</summary>_PLACEHOLDER��
    private void OnColorChanged(Color oldValue, Color newValue)
    {
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = newValue;
        }
    }

    /// ��<summary>_PLACEHOLDER��
    /// M�todo p�blico para reiniciar el estado del bot�n (llamado desde TurnManager)
    /// ��</summary>_PLACEHOLDER��
    public void ResetButtonState()
    {
        // Solo el servidor puede reiniciar estados
        if (!IsServer) return;

        isReady.Value = false;
        UpdateButtonColor(false);
    }
}