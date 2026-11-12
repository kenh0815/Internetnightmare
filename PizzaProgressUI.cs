using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// ピザチャージ中のUI（ゲージ）を管理するクラス。
/// ・NetcodeのNetworkBehaviourを継承し、オーナーシップ(IsOwner)をチェック。
/// ・オーナーではない場合はUIを無効化し、オーナーであればゲージ操作が可能になる。
/// </summary>
public class PizzaProgressUI : NetworkBehaviour
{
    [Header("Gauge UI References")]
    [Tooltip("ゲージ本体のCanvasやPanelなど、ゲージ全体を含むオブジェクト")]
    public GameObject gaugeUI;

    [Tooltip("ゲージ用のSliderコンポーネント")]
    public Slider gaugeSlider;

    private Coroutine gaugeCoroutine;

    private void Start()
    {
        // ★ オーナーシップのデバッグログ
        Debug.Log($"[PizzaProgressUI] OwnerClientId={OwnerClientId}, " +
                  $"LocalClientId={NetworkManager.Singleton.LocalClientId}, " +
                  $"IsOwner={IsOwner}, IsHost={IsHost}");

        // 自分がオーナーでなければUIを使わない
        if (!IsOwner)
        {
            if (gaugeUI != null)
            {
                gaugeUI.SetActive(false);
            }
            this.enabled = false;
            return;
        }

        // ローカルプレイヤー(オーナー)だけ UI を初期非表示
        ShowGauge(false);
    }

    /// <summary>
    /// ゲージをスタートさせる
    /// </summary>
    public void StartGauge()
    {
        // 念のためチェック
        if (!IsOwner) return;

        if (gaugeCoroutine == null)
        {
            gaugeCoroutine = StartCoroutine(FillGauge());
        }
    }

    /// <summary>
    /// ゲージUIの表示/非表示
    /// </summary>
    public void ShowGauge(bool show)
    {
        if (gaugeUI != null)
        {
            gaugeUI.SetActive(show);
        }
    }

    /// <summary>
    /// スライダーの値を更新 (0~1)
    /// </summary>
    public void UpdateGauge(float progress)
    {
        if (gaugeSlider != null)
        {
            gaugeSlider.value = Mathf.Clamp01(progress);
        }
    }

    /// <summary>
    /// 3秒かけてゲージを満タンまで進める例
    /// </summary>
    private IEnumerator FillGauge()
    {
        ShowGauge(true);
        float progress = 0f;
        float duration = 3f; // 3秒で満タン

        while (progress < 1f)
        {
            progress += Time.deltaTime / duration;
            UpdateGauge(progress);
            yield return null;
        }

        Debug.Log("[PizzaProgressUI] ゲージ満タンになりました。");
        ShowGauge(false);
        gaugeCoroutine = null;
    }

    /// <summary>
    /// ゲージをリセットし非表示にする
    /// </summary>
    public void ResetGauge()
    {
        UpdateGauge(0f);
        ShowGauge(false);
    }

    /* -------------------------------------------------------------------------
     * 以下は参考：カスタムでプレイヤーをSpawnする場合の例（通常は他のGameManager系スクリプトで実装する）
     * -------------------------------------------------------------------------
    [ServerRpc]
    public void SpawnPlayerForClientRpc(ulong clientId)
    {
        // ★サーバーだけがSpawn可能。Prefabなどの参照がある前提
        // 例: "playerPrefab"という変数にPlayerオブジェクト(PizzaProgressUI付き)がアサインされている場合
        // var playerObj = Instantiate(playerPrefab);
        // var netObj = playerObj.GetComponent<NetworkObject>();
        // netObj.SpawnAsPlayerObject(clientId);
    }
    */
}
