using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    // 最大4人分のスポーンポイントを登録
    public Transform[] spawnPoints = new Transform[4];

    private int currentSpawnIndex = 0;

    void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        }
    }


    private void OnPlayerConnected(ulong clientId)
    {
        if (!IsServer) return;

        var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObject != null && spawnPoints.Length > 0)
        {
            // スポーンポイントを順番に割り当てる
            int spawnIndex = currentSpawnIndex % spawnPoints.Length;
            playerObject.transform.position = spawnPoints[spawnIndex].position;
            playerObject.transform.rotation = spawnPoints[spawnIndex].rotation;

            currentSpawnIndex++;

            Debug.Log($"Player {clientId} spawned at position {spawnIndex}.");
        }
        else
        {
            Debug.LogError("Spawn points are not correctly set or PlayerObject is missing.");
        }
    }
}

