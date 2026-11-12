using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LocalPizzaProgressUI : MonoBehaviour
{
    public static LocalPizzaProgressUI Instance { get; private set; }

    [SerializeField] private GameObject gaugeUI;  // ゲージの親オブジェクト
    [SerializeField] private Slider gaugeSlider;  // スライダー

    private Coroutine gaugeCoroutine;

    private void Awake()
    {
        // シングルトン的に扱う場合の例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // ゲージを初期的に非表示
        ShowGauge(false);
    }

    public void StartGauge()
    {
        if (gaugeCoroutine == null)
        {
            gaugeCoroutine = StartCoroutine(FillGauge());
        }
    }

    private IEnumerator FillGauge()
    {
        ShowGauge(true);
        float progress = 0f;
        float duration = 3f; // 3秒で満タンにする例

        while (progress < 1f)
        {
            progress += Time.deltaTime / duration;
            UpdateGauge(progress);
            yield return null;
        }

        Debug.Log("ゲージが満タンになりました");
        ShowGauge(false);
        gaugeCoroutine = null;
    }

    public void ShowGauge(bool show)
    {
        if (gaugeUI != null)
        {
            gaugeUI.SetActive(show);
        }
    }

    public void UpdateGauge(float progress)
    {
        if (gaugeSlider != null)
        {
            gaugeSlider.value = Mathf.Clamp01(progress);
        }
    }

    public void ResetGauge()
    {
        UpdateGauge(0f);
        ShowGauge(false);
    }
}

