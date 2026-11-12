using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public void OnReceivePizza()
    {
        // 自分のプレイヤーならローカルUIを更新
        if (IsOwner)
        {
            LocalPizzaProgressUI.Instance?.StartGauge();
        }
    }
}

