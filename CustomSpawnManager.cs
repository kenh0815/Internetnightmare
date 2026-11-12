using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CustomSpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab; // NetworkPrefabsListに登録済みのPrefab

    private void Start()
    {
        // ネットワーク開始時のイベント登録
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
    }

    private void OnServerStarted()
    {
        // ホストがStartHost()した時にも自分用のプレイヤーをSpawnする
        if (NetworkManager.Singleton.IsHost)
        {
            SpawnPlayerForClient(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        // サーバー（ホスト）が新しいクライアントに対してプレイヤーをSpawn
        if (NetworkManager.Singleton.IsServer)
        {
            // すでにホスト用は作っている場合、クライアントのIDだけ作るなど条件分岐OK
            SpawnPlayerForClient(clientId);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        // PrefabをInstantiateしてNetworkObjectを取得
        GameObject playerObj = Instantiate(playerPrefab);
        var netObj = playerObj.GetComponent<NetworkObject>();
        // ここで「clientId」のプレイヤーがオーナーとなるよう Spawn
        netObj.SpawnAsPlayerObject(clientId);
    }
}