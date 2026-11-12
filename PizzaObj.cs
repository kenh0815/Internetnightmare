using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PizzaObj : NetworkBehaviour
{
    // すでにピックアップ済みかどうかを示すフラグ（重複処理防止用）
    private bool hasBeenPickedUp = false;

    // プレイヤーがピックアップ対象に衝突したときの処理
    private void OnTriggerEnter(Collider other)
    {
        // プレイヤータグでない場合は処理しない
        if (!other.CompareTag("Player"))
            return;

        // 当たったオブジェクトの親から PlayerInventory を取得
        PlayerInventory playerInventory = other.GetComponentInParent<PlayerInventory>();
        if (playerInventory == null)
            return;

        // 既にこのピザが処理済みの場合は何もしない
        if (hasBeenPickedUp)
            return;

        hasBeenPickedUp = true;

        // クライアント側かサーバー側かで分岐
        if (!IsServer)
        {
            // クライアント側は ServerRpc で処理をリクエスト
            RequestPizzaPickupServerRpc(playerInventory.OwnerClientId);
        }
        else
        {
            // サーバー側なら直接処理
            ProcessPizzaPickup(playerInventory);
        }
    }

    // クライアント側からサーバーに対してピックアップ処理をリクエストする ServerRpc
    [ServerRpc(RequireOwnership = false)]
    private void RequestPizzaPickupServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        PlayerInventory playerInventory = GetPlayerInventoryFromClientId(clientId);
        if (playerInventory != null)
        {
            ProcessPizzaPickup(playerInventory);
        }
        else
        {
            Debug.LogError($"[PizzaPickup] Client {clientId} の PlayerInventory が取得できませんでした。");
        }
    }

    // 実際にピックアップ処理を行う（ピザカウントを 1 加算し、オブジェクトを削除）
    private void ProcessPizzaPickup(PlayerInventory playerInventory)
    {
        // プレイヤーのピザ数を 1 加算
        playerInventory.pizzaCount.Value += 1;
        Debug.Log($"[PizzaPickup] Client {playerInventory.OwnerClientId} がピザを取得。現在のピザ数：{playerInventory.pizzaCount.Value}");

        // ネットワークオブジェクトであれば Despawn、なければ Destroy でオブジェクト削除
        if (NetworkObject != null)
        {
            NetworkObject.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ClientID から PlayerInventory を取得するためのヘルパーメソッド
    private PlayerInventory GetPlayerInventoryFromClientId(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            return networkClient.PlayerObject.GetComponent<PlayerInventory>();
        }
        return null;
    }

    // 毎フレーム呼ばれ、ピザオブジェクトを常に回転させる
    private void Update()
    {
        // Y軸を中心に、1秒間に約90度回転させる（Time.deltaTime を掛けて滑らかに）
        transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
    }
}
