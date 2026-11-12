using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class NetworkCarController : NetworkBehaviour
{
    // 速度、回転などのデータをサーバーとクライアント間で同期
    private NetworkVariable<float> speed = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<float> steering = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] private float maxSpeed = 90f;
    [SerializeField] private float maxSteeringAngle = 27f;
    [SerializeField] private float accelerationMultiplier = 2f;
    [SerializeField] private float steeringSpeed = 0.5f;
    [SerializeField] private float brakeForce = 350f;

    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    private Rigidbody carRigidbody;

    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (IsOwner) // クライアントで操作する
        {
            float vertical = Input.GetAxis("Vertical"); // W/Sキー or ↑↓キー
            float horizontal = Input.GetAxis("Horizontal"); // A/Dキー or ←→キー

            // サーバーに入力を送信
            SubmitInputServerRpc(vertical, horizontal);
        }

        if (IsServer) // サーバー側で物理演算を適用
        {
            ApplyMovement();
        }
    }

    // クライアントが入力をサーバーに送信
    [ServerRpc]
    private void SubmitInputServerRpc(float vertical, float horizontal)
    {
        speed.Value = vertical * maxSpeed;
        steering.Value = horizontal * maxSteeringAngle;
    }

    // サーバーが車両の移動を制御
    private void ApplyMovement()
    {
        float currentSteering = Mathf.Lerp(frontLeftCollider.steerAngle, steering.Value, steeringSpeed);
        frontLeftCollider.steerAngle = currentSteering;
        frontRightCollider.steerAngle = currentSteering;

        if (speed.Value > 0)
        {
            ApplyTorque(speed.Value);
        }
        else if (speed.Value < 0)
        {
            ApplyBrake();
        }
        else
        {
            StopCar();
        }
    }

    private void ApplyTorque(float speed)
    {
        rearLeftCollider.brakeTorque = 0;
        rearRightCollider.brakeTorque = 0;
        rearLeftCollider.motorTorque = accelerationMultiplier * speed;
        rearRightCollider.motorTorque = accelerationMultiplier * speed;
    }

    private void ApplyBrake()
    {
        rearLeftCollider.brakeTorque = brakeForce;
        rearRightCollider.brakeTorque = brakeForce;
    }

    private void StopCar()
    {
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;
        rearLeftCollider.brakeTorque = 0;
        rearRightCollider.brakeTorque = 0;
    }

}
