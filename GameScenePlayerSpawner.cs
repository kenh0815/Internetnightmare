using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement; // ←こちらを使います（Unity標準のSceneManagement）

public class GameScenePlayerSpawner : NetworkBehaviour
{
    public GameObject playerPrefab; // プレイヤープレハブ
    public Transform[] spawnPoints; // スポーンポイント（4つ）

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoadComplete;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SceneLoadComplete;
        }
    }

    private void SceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        int spawnIndex = 0;
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Vector3 spawnPosition = spawnPoints[spawnIndex % spawnPoints.Length].position;
            Quaternion spawnRotation = spawnPoints[spawnIndex % spawnPoints.Length].rotation;

            GameObject player = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

            spawnIndex++;
        }

        Debug.Log("All players spawned successfully after scene load completed.");
    }
}

