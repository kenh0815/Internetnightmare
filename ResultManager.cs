using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Linq;

public class ResultManager : NetworkBehaviour
{
    [Header("ランキングエントリープレハブと表示位置")]
    [SerializeField] private GameObject rankingEntryPrefab;
    [SerializeField] private Transform firstPosition;
    [SerializeField] private Transform secondPosition;
    [SerializeField] private Transform thirdPosition;
    [SerializeField] private Transform fourthPosition;

    [Header("ランクアイコン用Sprite")]
    [SerializeField] private Sprite rank1Sprite;
    [SerializeField] private Sprite rank2Sprite;
    [SerializeField] private Sprite rank3Sprite;
    [SerializeField] private Sprite rank4Sprite;

    public override void OnNetworkSpawn()
    {
        Debug.Log("ResultManager OnNetworkSpawn called. IsServer: " + IsServer + ", IsClient: " + IsClient);

        // ホスト側でのみランキングデータを送信
        if (IsServer)
        {
            SendRankingData();
        }
    }

    /// <summary>
    /// ホスト側でスコア降順にプレイヤーを並べ、クライアントIDとスコアをClientRpcで送信。
    /// </summary>
    private void SendRankingData()
    {
        // GameManagerにあるplayersリストを参照
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("GameManagerが見つかりません。");
            return;
        }

        // スコア降順でソート
        var sortedPlayers = gm.players.OrderByDescending(p => p.point.Value).ToList();
        int validCount = Mathf.Min(sortedPlayers.Count, 4);

        // 4件分の配列（クライアントID と スコア）
        string[] clientIds = new string[4];
        int[] scores = new int[4];

        for (int i = 0; i < validCount; i++)
        {
            // ★クライアントIDを文字列化して格納
            clientIds[i] = sortedPlayers[i].OwnerClientId.ToString();
            scores[i] = sortedPlayers[i].point.Value;
        }
        // 残りを空欄/0で埋める
        for (int i = validCount; i < 4; i++)
        {
            clientIds[i] = "";
            scores[i] = 0;
        }

        Debug.Log("【スコア順】クライアントIDとスコアをClientRpcに送信します。validCount: " + validCount);

        // ClientRpc で全クライアントに送信
        UpdateRankingClientRpc(
            clientIds[0], scores[0],
            clientIds[1], scores[1],
            clientIds[2], scores[2],
            clientIds[3], scores[3],
            validCount
        );
    }

    /// <summary>
    /// クライアント側でランキングデータ(クライアントID + スコア)を受信
    /// </summary>
    [ClientRpc]
    private void UpdateRankingClientRpc(
        string clientId0, int score0,
        string clientId1, int score1,
        string clientId2, int score2,
        string clientId3, int score3,
        int validCount)
    {
        Debug.Log($"UpdateRankingClientRpc. validCount: {validCount}");

        // validCountぶんだけ配列を準備
        string[] clientIds = new string[validCount];
        int[] scores = new int[validCount];

        if (validCount > 0)
        {
            clientIds[0] = clientId0;
            scores[0] = score0;
        }
        if (validCount > 1)
        {
            clientIds[1] = clientId1;
            scores[1] = score1;
        }
        if (validCount > 2)
        {
            clientIds[2] = clientId2;
            scores[2] = score2;
        }
        if (validCount > 3)
        {
            clientIds[3] = clientId3;
            scores[3] = score3;
        }

        ShowRanking(clientIds, scores);
    }

    /// <summary>
    /// クライアントIDとスコアを、順位ごとのUI（Prefab）に反映
    /// </summary>
    private void ShowRanking(string[] clientIds, int[] playerScores)
    {
        Debug.Log("ShowRankingを開始。エントリー数: " + clientIds.Length);

        Transform[] positions = { firstPosition, secondPosition, thirdPosition, fourthPosition };
        Sprite[] rankSprites = { rank1Sprite, rank2Sprite, rank3Sprite, rank4Sprite };

        int count = clientIds.Length;
        for (int i = 0; i < count && i < positions.Length; i++)
        {
            Debug.Log($"ランク {i + 1} のエントリを生成: ClientID={clientIds[i]}, {playerScores[i]}pt");

            GameObject entryObj = Instantiate(rankingEntryPrefab, positions[i]);
            RectTransform rect = entryObj.GetComponent<RectTransform>();
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            TextMeshProUGUI playerNameText = entryObj.transform.Find("PlayerNameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI scoreText = entryObj.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
            Image rankIcon = entryObj.transform.Find("RankIcon")?.GetComponent<Image>();

            if (playerNameText == null || scoreText == null || rankIcon == null)
            {
                Debug.LogError("ランキングエントリープレハブに必要なUIコンポーネントが見つかりません。");
                continue;
            }

            // 表示名を「ClientID: <ID>」の形式にする例
            playerNameText.text = $"ClientID: {clientIds[i]}";
            scoreText.text = $"{playerScores[i]}pt";
            rankIcon.sprite = rankSprites[i];
        }
    }

    // もし結果シーンでプレイヤーオブジェクトを非表示にしたい場合の処理
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
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players)
            {
                var meshRenderer = player.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer != null) meshRenderer.enabled = false;

                var canvas = player.GetComponentInChildren<Canvas>();
                if (canvas != null) canvas.enabled = false;
            }
        }
    }
}
