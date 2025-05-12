using UnityEngine;

public class SpiralMovement : MonoBehaviour
{
    public float a = 0f;               // Radio inicial
    public float b = 0.5f;             // Velocidad de expansión radial
    public float angleSpeed = 2f;      // Velocidad angular
    public float speedMultiplier = 1f; // Multiplicador general
    public float maxTurns = 5f;        // Máximo número de vueltas

    private float theta = 0f;          // Ángulo actual
    private bool stopped = false;
    private Vector3 initialPosition;   // Posición original del objeto

    void Start ()
    {
        initialPosition = transform.position;
    }

    void Update ()
    {
        if (stopped) return;

        theta += angleSpeed * speedMultiplier * Time.deltaTime;

        if (theta >= maxTurns * 2 * Mathf.PI)
        {
            stopped = true;
            return;
        }

        float r = a + b * theta;
        float x = r * Mathf.Cos (theta);
        float y = r * Mathf.Sin (theta);

        transform.position = initialPosition + new Vector3 (x, y, 0);
    }
}
