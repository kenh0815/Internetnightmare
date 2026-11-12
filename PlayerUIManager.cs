using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerUIManager : NetworkBehaviour
{
    public GameObject playerUI; // 各プレイヤー専用のUI

    void Start()
    {
        if (!IsOwner)
        {
            // 自分以外のプレイヤーのUIは無効化
            playerUI.SetActive(false);
            this.enabled = false;
        }
    }
}
