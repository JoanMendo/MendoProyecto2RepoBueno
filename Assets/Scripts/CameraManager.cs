using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Configuraci�n de Transici�n")]
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Camera targetCamera;
    private Vector3 initialPosition;
    private float initialOrthoSize;
    private Vector3 targetPosition;
    private float targetOrthoSize;
    private float transitionProgress;
    private bool isTransitioning;

    private void Awake()
    {
        targetCamera = Camera.main;
    }

    // M�todo p�blico para iniciar la transici�n
    public void MoveCamera(Vector3 newPosition, float newOrthoSize)
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("No se encontr� la c�mara principal");
            return;
        }

        initialPosition = targetCamera.transform.position;
        initialOrthoSize = targetCamera.orthographicSize;
        targetPosition = newPosition - (targetCamera.transform.forward * 35);
        targetOrthoSize = newOrthoSize;
        transitionProgress = 0f;
        isTransitioning = true;
    }

    private void Update()
    {
        if (!isTransitioning) return;

        transitionProgress += Time.deltaTime / transitionDuration;
        transitionProgress = Mathf.Clamp01(transitionProgress);

        float curveProgress = movementCurve.Evaluate(transitionProgress);

        // Aplicar la interpolaci�n
        targetCamera.transform.position = Vector3.Lerp(
            initialPosition,
            targetPosition,
            curveProgress
        );

        targetCamera.orthographicSize = Mathf.Lerp(
            initialOrthoSize,
            targetOrthoSize,
            curveProgress
        );

        // Finalizar la transici�n cuando se complete
        if (transitionProgress >= 1f)
        {
            isTransitioning = false;
        }
    }
}