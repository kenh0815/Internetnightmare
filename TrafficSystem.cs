using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSystem : MonoBehaviour
{
    
    public GameObject carStopper; // 直角方向の交通を止めるための BoxCollider (Trigger)
    private HashSet<GameObject> carsInIntersection = new HashSet<GameObject>(); // 交差点にいる車を管理

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car")||other.CompareTag("Player") )// "Car"タグを持つオブジェクトが侵入したら
        {
            if (carsInIntersection.Count == 0) // 最初の車が侵入したら
            {
                carStopper.SetActive(true); // 直角方向の交通を止める
            }
            carsInIntersection.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            carsInIntersection.Remove(other.gameObject);

            if (carsInIntersection.Count == 0) // 交差点の車がすべて通過したら
            {
                carStopper.SetActive(false); // 直角方向の交通を再開
            }
        }
    }
}
