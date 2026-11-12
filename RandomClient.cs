using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RandomClient : NetworkBehaviour
{
    [SerializeField]
    private GameObject[] clients;

    // 同期用のネットワークリスト (クライアント側にも自動で同期される)
    private NetworkList<int> activeClients;

    private void Awake()
    {
        activeClients = new NetworkList<int>();
        activeClients.OnListChanged += OnActiveClientsChanged;
    }

    public override void OnNetworkSpawn()
    {
        HideAllClients();

        if (IsServer)
        {
            SelectInitialClients(3);
        }
        else
        {
            // クライアント側は既に同期されたリストに基づいて表示する
            foreach (int index in activeClients)
            {
                SetClientVisible(clients[index], true);
            }
        }
    }

    private void SelectInitialClients(int count)
    {
        List<int> availableClients = new List<int>();
        for (int i = 0; i < clients.Length; i++)
            availableClients.Add(i);

        activeClients.Clear();

        for (int i = 0; i < Mathf.Min(count, clients.Length); i++)
        {
            int randomIndex = availableClients[Random.Range(0, availableClients.Count)];
            activeClients.Add(randomIndex);
            availableClients.Remove(randomIndex);
        }
    }

    public void ChangeClient(GameObject deliveredClient)
    {
        int clientToReplace = System.Array.IndexOf(clients, deliveredClient);
        if (clientToReplace == -1 || activeClients.Count == 0) return;

        List<int> availableClients = new List<int>();
        for (int i = 0; i < clients.Length; i++)
        {
            if (!activeClients.Contains(i))
                availableClients.Add(i);
        }

        if (availableClients.Count == 0) return;

        int newClientIndex = availableClients[Random.Range(0, availableClients.Count)];

        int replaceIndexInActiveList = activeClients.IndexOf(clientToReplace);
        activeClients[replaceIndexInActiveList] = newClientIndex;
    }

    private void OnActiveClientsChanged(NetworkListEvent<int> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<int>.EventType.Add:
                SetClientVisible(clients[changeEvent.Value], true);
                break;

            case NetworkListEvent<int>.EventType.Remove:
                SetClientVisible(clients[changeEvent.Value], false);
                break;

            case NetworkListEvent<int>.EventType.Value:
                SetClientVisible(clients[changeEvent.PreviousValue], false);
                SetClientVisible(clients[changeEvent.Value], true);
                break;

            case NetworkListEvent<int>.EventType.Clear:
                HideAllClients();
                break;
        }
    }

    private void HideAllClients()
    {
        foreach (var client in clients)
            SetClientVisible(client, false);
    }

    private void SetClientVisible(GameObject clientObj, bool isVisible)
    {
        Renderer[] renderers = clientObj.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.enabled = isVisible;

        Collider[] colliders = clientObj.GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
            c.enabled = isVisible;
    }
}
