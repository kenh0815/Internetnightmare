using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCameraController : NetworkBehaviour
{
    public GameObject mainCamera;  // TCamをセット
    public GameObject miniMapCamera; // MiniMapCamをセット

    void Start()
    {
        if (IsOwner)
        {
            // 自分のカメラを有効化
            mainCamera.SetActive(true);
            miniMapCamera.SetActive(true);
        }
        else
        {
            // 他プレイヤーのカメラは無効化
            mainCamera.SetActive(false);
            miniMapCamera.SetActive(false);
        }
    }
}
