using System.Collections;
using UnityEngine;

public class EspiralSpawner : MonoBehaviour
{
    public float spawnTime = 0.8f;
    public GameObject espiralprefab;
    private void OnEnable ()
    {
        StartCoroutine (SpawnSpirals ());
    }

    private void OnDisable ()
    {
        StopCoroutine (SpawnSpirals ());
    }
    public IEnumerator SpawnSpirals()
    {
        while(true)
        {
            GameObject espiral = Instantiate (espiralprefab, transform.position, Quaternion.identity, transform); // Instancia la espiral en la posición del spawner);


            yield return new WaitForSeconds (spawnTime); // Espera 0.5 segundos entre cada espiral
        }
    }

    public void Update ()
    {
        //Rotate the object in the Z axis
        transform.Rotate (0, 0, 1);
    }
}
