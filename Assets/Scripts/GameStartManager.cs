using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameStartManager : NetworkBehaviour
{
    public GameObject tableroPrefab;

    private bool hasSpawnedTableros = false;

    public override void OnNetworkSpawn()
    {
        // Solo el servidor hace esto
        if (IsServer && !hasSpawnedTableros)
        {
            SpawnAllTableros();
            hasSpawnedTableros = true;
        }

    }

    private void SpawnAllTableros()
    {

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong clientId = client.ClientId;
            SpawnTableroForClient(clientId); // todos los tableros, uno por cliente
        }
    }




    private void SpawnTableroForClient(ulong clientId)
    {
        Vector3 spawnPos = GetSpawnPosition(clientId);

        // Instanciar el tablero
        GameObject tablero = Instantiate(tableroPrefab, spawnPos, Quaternion.identity);

        // Generar nodos del mapa
        var nodeMap = tablero.GetComponent<NodeMap>();
        nodeMap.Generate3DTilemap();

        tablero.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
        // Spawnear nodos con ownership
        foreach (GameObject casilla in nodeMap.nodesList)
        {

            casilla.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

        }

        // Spawnear el tablero y darle ownership

    }

    private Vector3 GetSpawnPosition(ulong clientId)
    {
        return new Vector3((int)clientId * 35f, 0f, 0f);
    }
}
