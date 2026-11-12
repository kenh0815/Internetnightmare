using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PizzaGet : NetworkBehaviour
{
    // 客選出システムへの参照（シーン内に1つ存在する前提）
    private RandomClient randomClient;

    // 同じプレイヤーからの重複処理を防ぐための管理（プレイヤーID毎）
    private HashSet<ulong> processedPlayers = new HashSet<ulong>();

    // ★★ 効果音用の設定 ★★
    // 効果音の再生に使用する AudioSource（Inspector で設定）
    public AudioSource audioSource;
    // 配達成功時に再生する効果音
    public AudioClip deliverySound;
    // ★★ ここまで ★★

    void Start()
    {
        // シーン内から RandomClient を取得
        randomClient = FindObjectOfType<RandomClient>();
        if (randomClient == null)
        {
            Debug.LogError("[PizzaGet] RandomClient がシーン内に見つかりません。");
        }
    }

    // 客の家（このオブジェクト）の Collider（Is Trigger）にプレイヤーが侵入したとき
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerInventory playerInventory = other.GetComponentInParent<PlayerInventory>();
        if (playerInventory == null) return;

        ulong clientId = playerInventory.OwnerClientId;

        if (processedPlayers.Contains(clientId)) return;
        processedPlayers.Add(clientId);

        if (!IsServer)
        {
            // クライアント側では必ず自分のclientIdをRPCで渡す
            RequestDeliverPizzaServerRpc(clientId);
        }
        else
        {
            // サーバー側は直接取得したPlayerInventoryを使う
            DeliverPizza(playerInventory);
        }
    }

    // プレイヤーがエリアから退出したとき、重複処理フラグを解除
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerInventory playerInventory = other.GetComponentInParent<PlayerInventory>();
        if (playerInventory == null) return;

        ulong clientId = playerInventory.OwnerClientId;
        processedPlayers.Remove(clientId);
    }

    // クライアントからサーバーへ、ピザ配達をリクエストする ServerRpc
    [ServerRpc(RequireOwnership = false)]
    private void RequestDeliverPizzaServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        PlayerInventory playerInventory = GetPlayerInventoryFromClientId(clientId);
        if (playerInventory != null)
        {
            DeliverPizza(playerInventory);
        }
        else
        {
            Debug.LogError($"[PizzaGet] クライアント {clientId} の PlayerInventory が取得できませんでした。");
        }
    }

    // 配達処理：ピザがあれば1枚減らし、ポイントを加算する。成功したら効果音を再生し、ランダムに新たな客を選出する。
    private void DeliverPizza(PlayerInventory playerInventory)
    {
        if (playerInventory.pizzaCount.Value <= 0)
        {
            Debug.Log($"[PizzaGet] {playerInventory.OwnerClientId} はピザを持っていないため処理終了。");
            return;
        }

        playerInventory.pizzaCount.Value--;
        playerInventory.point.Value++;

        // 効果音を再生する（全クライアントで再生）
        PlayDeliverySoundClientRpc();

        if (IsServer && randomClient != null)
        {
            randomClient.ChangeClient(gameObject);
        }
    }

    // ClientRpc を利用して、各クライアント側で配達成功の効果音を再生する
    [ClientRpc]
    private void PlayDeliverySoundClientRpc()
    {
        if (audioSource != null && deliverySound != null)
        {
            audioSource.PlayOneShot(deliverySound);
        }
    }

    private PlayerInventory GetPlayerInventoryFromClientId(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            return networkClient.PlayerObject.GetComponent<PlayerInventory>();
        }
        return null;
    }
}
