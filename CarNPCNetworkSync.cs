using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class CarNPCNetworkSync : NetworkBehaviour
{
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();

    private Transform carTransform;

    void Start()
    {
        carTransform = transform;
    }

    void Update()
    {
        if (IsServer)
        {
            // サーバー（ホスト）がNPC位置・回転を送信
            networkPosition.Value = carTransform.position;
            networkRotation.Value = carTransform.rotation;
        }
        else
        {
            // クライアントが位置を補間して滑らかに動かす
            carTransform.position = Vector3.Lerp(carTransform.position, networkPosition.Value, Time.deltaTime * 10);
            carTransform.rotation = Quaternion.Slerp(carTransform.rotation, networkRotation.Value, Time.deltaTime * 10);
        }
    }
}

