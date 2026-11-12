using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HidePlayersOnResultScene : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "ResultScene")
        {
            // 「Player」タグが付いているオブジェクトを全て非表示にする
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players)
            {
                // 視覚要素だけを無効にする場合は、必要なコンポーネントを無効化
                // 例: MeshRenderer や Canvas コンポーネントを無効化
                MeshRenderer meshRenderer = player.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer != null)
                    meshRenderer.enabled = false;

                Canvas canvas = player.GetComponentInChildren<Canvas>();
                if (canvas != null)
                    canvas.enabled = false;

                // もしくは、プレイヤーオブジェクト自体を非表示にする
                player.SetActive(false);
            }
        }
    }
}

