using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerInventory : NetworkBehaviour
{
    public NetworkVariable<int> pizzaCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> point = new NetworkVariable<int>(0);

    [SerializeField] private Text pizzaCountText;
    [SerializeField] private Text pointText;

    // ピザ獲得時の効果音用フィールド
    [SerializeField] private AudioSource pizzaAudioSource;
    [SerializeField] private AudioClip pizzaPickupSound;

    // 衝突時（PlayerTag との衝突）に再生する新たな効果音
    [SerializeField] private AudioClip collisionSound;

    private void Start()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        if (pizzaCountText == null || pointText == null)
        {
            Debug.LogError("UIテキストが設定されていません。Inspectorで設定してください。");
            return;
        }

        // ピザ獲得時の効果音と UI 更新
        pizzaCount.OnValueChanged += (oldValue, newValue) =>
        {
            UpdatePizzaUI(newValue);
            if (newValue > oldValue && pizzaAudioSource != null && pizzaPickupSound != null)
            {
                pizzaAudioSource.PlayOneShot(pizzaPickupSound);
            }
        };

        point.OnValueChanged += (oldValue, newValue) => UpdatePointUI(newValue);

        UpdatePizzaUI(pizzaCount.Value);
        UpdatePointUI(point.Value);
    }

    // OnCollisionEnter を利用した衝突処理
    private void OnCollisionEnter(Collision collision)
    {
        // 衝突したオブジェクトが "PlayerTag" を持っているかチェック
        if (collision.gameObject.CompareTag("Player"))
        {
            if (pizzaCount.Value > 0)
            {
                RemovePizza(1);
                // 衝突専用の効果音を再生
                if (pizzaAudioSource != null && collisionSound != null)
                {
                    pizzaAudioSource.PlayOneShot(collisionSound);
                }
            }
        }
    }

    // ポイント加算メソッド
    public void AddPoint(int amount)
    {
        if (IsOwner)
        {
            AddPointServerRpc(amount);
        }
    }

    [ServerRpc]
    private void AddPointServerRpc(int amount)
    {
        point.Value += amount;
        Debug.Log($"ポイント加算: 現在のポイント = {point.Value}");
    }

    public void AddPizza(int amount)
    {
        if (IsOwner)
        {
            AddPizzaServerRpc(amount);
        }
    }

    public void RemovePizza(int amount)
    {
        if (IsOwner)
        {
            RemovePizzaServerRpc(amount);
        }
    }

    [ServerRpc]
    private void AddPizzaServerRpc(int amount)
    {
        pizzaCount.Value += amount;
        Debug.Log($"ピザ獲得: 現在のピザ数 = {pizzaCount.Value}");
    }

    [ServerRpc]
    private void RemovePizzaServerRpc(int amount)
    {
        if (pizzaCount.Value >= amount)
        {
            pizzaCount.Value -= amount;
            Debug.Log($"ピザ削除: 現在のピザ数 = {pizzaCount.Value}");
        }
    }

    [ServerRpc]
    public void DeliverPizzaServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            Debug.Log("[DEBUG] DeliverPizzaServerRpc がクライアントで呼ばれてしまった");
            return;
        }
        pizzaCount.Value -= 1;
        Debug.Log($"ピザ配達成功！ 残りピザ: {pizzaCount.Value}");
    }

    private void UpdatePizzaUI(int newPizzaCount)
    {
        if (pizzaCountText != null)
        {
            pizzaCountText.text = $"×{newPizzaCount}";
        }
    }

    private void UpdatePointUI(int newPoint)
    {
        if (pointText != null)
        {
            pointText.text = $"{newPoint}pt";
        }
    }
}
