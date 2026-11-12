using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class NetworkUI : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource buttonSound;
    public AudioSource bgm;

    [Header("UI Elements")]
    // Relay で接続する際、初期 UI（ボタンなど）を非表示にするためのキャンバス
    public GameObject canvasToHide;
    public Button startHostButton;
    public Button startClientButton;
    public Button startGameButton;
    // クライアント用：Join Code 入力用の TMP_InputField（ホストの場合はここにJoin Codeを表示します）
    public TMP_InputField joinCodeInput;
    // ※ 必要であれば、ホスト側専用の表示用テキストを用意してもよいですが、今回は入力フィールドに表示する方式とします

    [Header("Prefabs")]
    // GameManager のプレハブ（NetworkObject コンポーネント付き）を Inspector でセット
    public GameObject gameManagerPrefab;

    async void Start()
    {
        // BGM 再生（あれば）
        if (bgm != null)
            bgm.Play();

        if (startGameButton != null)
            startGameButton.gameObject.SetActive(false);

        // 各ボタンのイベント割り当て
        startHostButton.onClick.AddListener(() => { HandleInitialButtonClick(); CreateRelayHost(); });
        startClientButton.onClick.AddListener(() => { HandleInitialButtonClick(); JoinRelay(); });
        startGameButton.onClick.AddListener(StartGame);

        // Unity Services の初期化と匿名認証を先に行う
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
    }

    /// <summary>
    /// ホストとして Relay Allocation を作成し、Join Code を取得後にホストを起動する
    /// </summary>
    public async void CreateRelayHost()
    {
        try
        {
            // ここでは最大 3 接続として Allocation を作成する（必要に応じて数字を変更してください）
            Allocation allocation = await Relay.Instance.CreateAllocationAsync(3);
            string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // UnityTransport に Relay 情報を設定
            UnityTransport utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData);

            Debug.Log("Join Code: " + joinCode);

            // ホスト側の TMP_InputField に Join Code を表示する
            if (joinCodeInput != null)
            {
                joinCodeInput.text = joinCode;
            }

            // ホストとして起動
            NetworkManager.Singleton.StartHost();

            if (startGameButton != null)
                startGameButton.gameObject.SetActive(true);

            // ホストの場合、GameManager のスポーン処理
            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log("[NetworkUI] Spawning GameManager...");
                GameObject gameManager = Instantiate(gameManagerPrefab);
                gameManager.GetComponent<NetworkObject>().Spawn();
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay Host 作成エラー: " + e);
        }
    }

    /// <summary>
    /// Join Code 入力値から Relay に参加し、クライアントを起動する
    /// </summary>
    public async void JoinRelay()
    {
        string joinCode = joinCodeInput.text;
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Join Code を入力してください。");
            return;
        }

        try
        {
            Debug.Log("Join Relay with code: " + joinCode);
            JoinAllocation joinAllocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            UnityTransport utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay Join エラー: " + e);
        }
    }

    /// <summary>
    /// 初期接続ボタン押下時の共通処理（ボタン音再生、UI 非表示など）
    /// </summary>
    private void HandleInitialButtonClick()
    {
        if (buttonSound != null)
            buttonSound.Play();
        if (canvasToHide != null)
            canvasToHide.SetActive(false);
    }

    /// <summary>
    /// ゲーム開始時に BGM を停止し、シーン遷移する
    /// ※ ホストの場合のみシーンを遷移する
    /// </summary>
    public void StartGame()
    {
        if (bgm != null)
            bgm.Stop();

        if (!NetworkManager.Singleton.IsHost)
            return;

        NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
