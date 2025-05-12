using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CoinCollector : MonoBehaviour
{
    public GameObject coinUIPrefab;         // Prefab de la moneda UI
    public Transform coinTargetUI;          // Donde debe volar (posición del contador)
    public Canvas canvas;                   // El canvas donde están los elementos UI
    public Vector3 mundoCoinPosition;       // Posición desde donde empieza (en mundo)
    public TextMeshProUGUI countText;       // Texto del contador con TMP

    private int coinCount = 0;

    void Update ()
    {
        if (Input.GetKeyDown (KeyCode.Space))
        {
            StartCoroutine (FlyCoin ());
        }
    }

    IEnumerator FlyCoin ()
    {
        Vector3 start = Camera.main.WorldToScreenPoint (mundoCoinPosition);
        Vector3 end = coinTargetUI.position;

        GameObject coin = Instantiate (coinUIPrefab, start, Quaternion.identity, canvas.transform);

        float height = 100f;
        float duration = 0.5f;
        float t = 0;

        while (t < duration)
        {
            float progress = t / duration;
            Vector3 current = Vector3.Lerp (start, end, progress);
            float arc = height * 4 * (progress - progress * progress);
            current.y += arc;

            coin.transform.position = current;
            t += Time.deltaTime;
            yield return null;
        }

        coin.transform.position = end;
        Destroy (coin);

        coinCount++;
        StartCoroutine (AnimateCount (coinCount - 1, coinCount));
    }

    IEnumerator AnimateCount (int from, int to)
    {
        float duration = 0.3f;
        float t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            int current = Mathf.RoundToInt (Mathf.Lerp (from, to, t / duration));
            countText.text = current.ToString ();
            yield return null;
        }

        countText.text = to.ToString ();
    }
}
