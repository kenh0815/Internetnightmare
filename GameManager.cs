using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    // UI要素（TimerCanvas 内）
    private Text countdownText;
    private Text gameTimerText;
    private Image rankImage;
    // TimerCanvas の参照
    private GameObject timerCanvas;

    // ランク表示用のスプライト
    public Sprite rank1Sprite;
    public Sprite rank2Sprite;
    public Sprite rank3Sprite;
    public Sprite rank4Sprite;

    // タイマーの設定
    public float countdownDuration = 3f;
    public float gameDuration = 180f;

    // シーン名の設定
    public string lobbySceneName = "LobbyScene";
    public string gameSceneName = "GameScene";
    public string resultSceneName = "ResultScene";

    // プレイヤーリスト
    public List<PlayerInventory> players;

    // ★★★ 以下、効果音・BGM用の設定 ★★★
    // 効果音とBGM再生用の AudioSource（GameManager にアタッチされた AudioSource を利用）
    public AudioSource audioSource;
    // カウントダウン（「3,2,1」）用効果音
    public AudioClip countdownClip;
    // 「GO！」用効果音
    public AudioClip goClip;
    // ゲーム開始時のBGM
    public AudioClip gameBGM;
    // ★★★ ここまで ★★★

    // Awake で重複生成対策＆シーン遷移時に破棄されないように設定
    void Awake()
    {
        if (FindObjectsOfType<GameManager>().Length > 1)
        {
            Debug.Log("[GameManager] Duplicate detected, destroying this instance.");
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    // シーンロード時のイベント登録
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Start では、最初（ロビーシーン）では TimerCanvas を非表示にする
    void Start()
    {
        // TimerCanvas を GameManager の子オブジェクトから取得
        Transform tc = transform.Find("TimerCanvas");
        if (tc != null)
        {
            timerCanvas = tc.gameObject;
            // ロビーシーンでは TimerCanvas は非表示
            if (SceneManager.GetActiveScene().name != gameSceneName)
            {
                timerCanvas.SetActive(false);
            }
            else
            {
                timerCanvas.SetActive(true);
                FindUIComponents();
            }
        }
        else
        {
            Debug.LogError("[GameManager] TimerCanvas not found as child of GameManager.");
        }
    }

    // ネットワーク上で Spawn されたときの処理（ここではログのみ）
    public override void OnNetworkSpawn()
    {
        Debug.Log($"[GameManager] OnNetworkSpawn Called. IsServer: {IsServer}, IsClient: {IsClient}");
    }

    // シーンがロードされたときに呼ばれる
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            // ゲームシーンなら TimerCanvas を表示＆UI要素を再取得
            if (timerCanvas != null)
            {
                timerCanvas.SetActive(true);
                FindUIComponents();
            }
            else
            {
                Debug.LogError("[GameManager] TimerCanvas is missing in game scene.");
            }

            // サーバー側なら、プレイヤー待機後にゲーム開始処理を実行
            if (IsServer)
            {
                StartCoroutine(WaitForPlayersAndInitialize());
            }
        }
        else if (scene.name == resultSceneName)
        {
            // 結果シーンに遷移したときにBGMを停止
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            // 結果シーンでは TimerCanvas を非表示にする
            if (timerCanvas != null)
            {
                timerCanvas.SetActive(false);
            }
        }
        else
        {
            // ゲームシーン以外（かつ結果シーン以外）では TimerCanvas を非表示
            if (timerCanvas != null)
            {
                timerCanvas.SetActive(false);
            }
        }
    }

    // TimerCanvas 内の UI 要素（countdownText, gameTimerText, rankImage）を取得する
    private void FindUIComponents()
    {
        if (timerCanvas == null)
        {
            Debug.LogError("[GameManager] TimerCanvas is null. Cannot find UI components.");
            return;
        }
        Transform countdownT = timerCanvas.transform.Find("countdownText");
        Transform gameTimerT = timerCanvas.transform.Find("gameTimerText");
        Transform rankImageT = timerCanvas.transform.Find("RankImage");

        if (countdownT != null)
            countdownText = countdownT.GetComponent<Text>();
        else
            Debug.LogError("[GameManager] countdownText not found in TimerCanvas.");

        if (gameTimerT != null)
            gameTimerText = gameTimerT.GetComponent<Text>();
        else
            Debug.LogError("[GameManager] gameTimerText not found in TimerCanvas.");

        if (rankImageT != null)
            rankImage = rankImageT.GetComponent<Image>();
        else
            Debug.LogError("[GameManager] RankImage not found in TimerCanvas.");
    }

    // プレイヤーのスポーンを待って初期化し、ゲーム開始のコルーチンを起動
    private IEnumerator WaitForPlayersAndInitialize()
    {
        yield return new WaitForSeconds(2f); // プレイヤーのスポーン待ち

        players = new List<PlayerInventory>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                Debug.LogWarning($"[GameManager] PlayerObject is NULL for ClientID: {client.ClientId}");
                continue;
            }
            var inventory = client.PlayerObject.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                players.Add(inventory);
                Debug.Log($"[GameManager] PlayerInventory found for ClientID: {client.ClientId}");
            }
            else
            {
                Debug.LogError($"[GameManager] PlayerInventory NOT found for ClientID: {client.ClientId}");
            }
        }

        if (IsServer)
        {
            StartCoroutine(StartGameSequence());
        }
    }

    // ゲーム開始のカウントダウンと制限時間タイマーの処理
    private IEnumerator StartGameSequence()
    {
        // カウントダウン開始前は、カウントダウン用テキストは表示せず、RankImageも非表示にする
        SetCountdownTextActiveClientRpc(false);
        SetRankImageActiveClientRpc(false);

        // ゲーム開始前にプレイヤー操作を無効化
        SetPlayersControl(false);

        // カウントダウン開始時に countdownText を表示する
        SetCountdownTextActiveClientRpc(true);

        // --- カウントダウン開始 ---
        float timer = countdownDuration;
        while (timer > 0)
        {
            // 数字を更新
            string countText = Mathf.CeilToInt(timer).ToString();
            UpdateCountdownClientRpc(countText);

            // 各カウント（3,2,1）の効果音を再生
            if (audioSource != null && countdownClip != null)
            {
                audioSource.PlayOneShot(countdownClip);
            }

            yield return new WaitForSeconds(1f);
            timer -= 1f;
        }

        // "GO!" 表示と効果音の再生
        UpdateCountdownClientRpc("GO！");
        if (audioSource != null && goClip != null)
        {
            audioSource.PlayOneShot(goClip);
        }
        yield return new WaitForSeconds(1f);
        // カウントダウン終了後、カウントダウンテキストを非表示にし、ランク画像を表示する
        SetCountdownTextActiveClientRpc(false);
        SetRankImageActiveClientRpc(true);
        UpdateCountdownClientRpc("");

        // GO後、ゲーム開始と同時にBGMを再生（ループ再生）
        if (audioSource != null && gameBGM != null)
        {
            audioSource.clip = gameBGM;
            audioSource.loop = true;
            audioSource.Play();
        }
        // --- カウントダウン終了 ---

        // カウントダウン終了後、プレイヤー操作を有効化
        SetPlayersControl(true);

        // 制限時間タイマー開始
        float gameTimer = gameDuration;
        while (gameTimer > 0)
        {
            UpdateGameTimerClientRpc($"{Mathf.FloorToInt(gameTimer / 60):00}:{Mathf.FloorToInt(gameTimer % 60):00}");
            UpdatePlayerRanks();
            yield return new WaitForSeconds(1f);
            gameTimer -= 1f;
        }
        UpdateGameTimerClientRpc("00:00");

        // ゲーム終了後、プレイヤー操作を無効化し結果シーンへ移行
        SetPlayersControl(false);
        SaveResults();
        LoadResultScene();
    }

    // プレイヤー操作の有効／無効を設定（PrometeoCarController を使用している前提）
    void SetPlayersControl(bool enable)
    {
        foreach (var player in FindObjectsOfType<PrometeoCarController>())
        {
            player.SetCanControl(enable);
        }
    }

    // ClientRpc でカウントダウンテキストを更新
    [ClientRpc]
    void UpdateCountdownClientRpc(string message)
    {
        if (countdownText != null)
            countdownText.text = message;
    }

    // ClientRpc でゲームタイマーのテキストを更新
    [ClientRpc]
    void UpdateGameTimerClientRpc(string message)
    {
        if (gameTimerText != null)
            gameTimerText.text = message;
    }

    // ClientRpc で countdownText の表示／非表示を切り替える
    [ClientRpc]
    void SetCountdownTextActiveClientRpc(bool active)
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(active);
    }

    // ClientRpc で rankImage の表示／非表示を切り替える
    [ClientRpc]
    void SetRankImageActiveClientRpc(bool active)
    {
        if (rankImage != null)
            rankImage.gameObject.SetActive(active);
    }

    // プレイヤーのスコア順にランク付けして更新
    void UpdatePlayerRanks()
    {
        players = players.OrderByDescending(p => p.point.Value).ToList();
        for (int i = 0; i < players.Count; i++)
        {
            UpdateRankClientRpc(players[i].OwnerClientId, i + 1);
        }
    }

    // ClientRpc で各クライアントのランク画像を更新
    [ClientRpc]
    void UpdateRankClientRpc(ulong clientId, int rank)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            if (rankImage == null)
            {
                Debug.LogError("[GameManager] rankImage is NULL. Check UI setup!");
                return;
            }
            switch (rank)
            {
                case 1:
                    if (rank1Sprite != null)
                        rankImage.sprite = rank1Sprite;
                    else
                        Debug.LogError("[GameManager] rank1Sprite is NULL!");
                    break;
                case 2:
                    if (rank2Sprite != null)
                        rankImage.sprite = rank2Sprite;
                    else
                        Debug.LogError("[GameManager] rank2Sprite is NULL!");
                    break;
                case 3:
                    if (rank3Sprite != null)
                        rankImage.sprite = rank3Sprite;
                    else
                        Debug.LogError("[GameManager] rank3Sprite is NULL!");
                    break;
                default:
                    if (rank4Sprite != null)
                        rankImage.sprite = rank4Sprite;
                    else
                        Debug.LogError("[GameManager] rank4Sprite is NULL!");
                    break;
            }
        }
    }

    // 結果シーンへ移行（サーバー側で実行）
    void LoadResultScene()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(resultSceneName, LoadSceneMode.Single);
        }
    }

    // プレイヤーのスコアを保存（PlayerPrefs を使用）
    void SaveResults()
    {
        var sortedPlayers = players.OrderByDescending(p => p.point.Value).ToList();
        PlayerPrefs.SetInt("PlayerCount", sortedPlayers.Count);
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            PlayerPrefs.SetInt($"Player_{i}_Score", sortedPlayers[i].point.Value);
        }
        PlayerPrefs.Save();
    }
}
