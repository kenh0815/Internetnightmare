using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PizzaPickup : NetworkBehaviour
{
    public string playerTag = "Player";
    private float stayTime = 0f;
    public float requiredTime = 3f;

    private PlayerInventory playerInventory;
    private PizzaProgressUI progressUI;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag(playerTag))
        {
            Debug.Log("ピザエリアに侵入");

            playerInventory = other.GetComponent<PlayerInventory>();
            progressUI = other.GetComponent<PizzaProgressUI>();

            if (progressUI != null)
            {
                progressUI.ShowGauge(true);
                progressUI.UpdateGauge(0f);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag(playerTag))
        {
            stayTime += Time.deltaTime;

            if (progressUI != null)
            {
                progressUI.UpdateGauge(stayTime / requiredTime);
            }

            if (stayTime >= requiredTime)
            {
                if (playerInventory != null)
                {
                    AddPizzaServerRpc(1);
                }

                stayTime -= requiredTime;

                if (progressUI != null)
                {
                    progressUI.UpdateGauge(stayTime / requiredTime);
                }
            }
        }
    }

    [ServerRpc]
    private void AddPizzaServerRpc(int amount)
    {
        playerInventory.pizzaCount.Value += amount;
        Debug.Log($"ピザを獲得: {playerInventory.pizzaCount.Value}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            stayTime = 0f;
            if (progressUI != null)
            {
                progressUI.ResetGauge();
            }
        }
    }
}