using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarDetector : MonoBehaviour
{
    private CarNPCAI carNPCAI;
    private AudioSource audioSource; // クラクション用のAudioSource
    public AudioClip honkSound; // クラクション音

    void Start()
    {
        carNPCAI = GetComponentInParent<CarNPCAI>();
        audioSource = GetComponent<AudioSource>(); // AudioSourceを取得

        if (audioSource == null)
        {
            Debug.LogError("AudioSource が detector に見つかりません！AudioSource を追加してください。");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player")||other.gameObject.CompareTag("Car")) // "Player" タグのオブジェクトと接触したら
        {
            Debug.Log("前方に車を感知");
            if (other.gameObject.CompareTag("Player"))
            {
                PlayHonkSound();

            }
            carNPCAI.SetStopped(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Car")) // 接触が解除されたら再開
        {
            carNPCAI.SetStopped(false);
        }
    }

    private void PlayHonkSound()
    {
        if (honkSound != null && audioSource != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(honkSound);
        }
    }
}
