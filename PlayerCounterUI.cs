using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerCountUI : NetworkBehaviour
{
    public Text playerCountText;

    private NetworkVariable<int> connectedPlayers = new NetworkVariable<int>(0);

    void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;

            UpdatePlayerCountUI(); // 最初の更新
        }

        connectedPlayers.OnValueChanged += (oldVal, newVal) => UpdatePlayerCountUI();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
        }
    }

    private void OnPlayerConnected(ulong clientId)
    {
        if (IsServer)
            connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;
    }

    private void OnPlayerDisconnected(ulong clientId)
    {
        if (IsServer)
            connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;
    }

    private void UpdatePlayerCountUI()
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"({connectedPlayers.Value}人が待機中)";
        }
    }
}

