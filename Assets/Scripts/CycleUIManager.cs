using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class CycleUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI quotaText;
    public TextMeshProUGUI levelText;
    
    public Button nextPhaseButton;
    public GameObject shopPanel;
    
    [Header("Shop UI")]
    public Button buyRockButton;
    public Button buyShelfButton;
    public TextMeshProUGUI inventoryText;

    private void Start()
    {
        if (nextPhaseButton != null)
        {
            nextPhaseButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null && NetworkManager.Singleton.IsServer)
                {
                    GameManager.Instance.AdvancePhaseServerRpc();
                }
            });
        }
        
        if (buyRockButton != null)
        {
            buyRockButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.BuyRockServerRpc();
            });
        }
        
        if (buyShelfButton != null)
        {
            buyShelfButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.BuyShelfServerRpc();
            });
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // フェーズ表示
        GameManager.GamePhase phase = GameManager.Instance.CurrentPhase.Value;
        if (phaseText != null)
        {
            phaseText.text = $"Phase: {phase.ToString()}";
        }

        // 時間表示
        if (timeText != null)
        {
            if (phase == GameManager.GamePhase.Mining)
            {
                timeText.text = $"Time: {Mathf.CeilToInt(GameManager.Instance.TimeRemaining.Value)}s";
            }
            else
            {
                timeText.text = "Time: --";
            }
        }

        // お金・ノルマ表示
        if (moneyText != null) moneyText.text = $"Money: {GameManager.Instance.TeamMoney.Value}G";
        if (quotaText != null) quotaText.text = $"Quota: {GameManager.Instance.DailyQuota.Value}G";
        if (levelText != null) levelText.text = $"Level: {GameManager.Instance.CurrentLevel.Value}";

        // 次へボタンの表示とテキスト更新（ホストのみ操作可能）
        if (nextPhaseButton != null)
        {
            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            nextPhaseButton.gameObject.SetActive(isHost && phase != GameManager.GamePhase.Mining);
            
            TextMeshProUGUI btnText = nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                if (phase == GameManager.GamePhase.Collection) btnText.text = "Open Shop";
                else if (phase == GameManager.GamePhase.Shop) btnText.text = "Start Placement";
                else if (phase == GameManager.GamePhase.Placement) btnText.text = "Start Next Day";
            }
        }
        
        // ショップパネルの表示
        if (shopPanel != null)
        {
            shopPanel.SetActive(phase == GameManager.GamePhase.Shop);
        }
        
        // インベントリの表示更新
        if (inventoryText != null)
        {
            inventoryText.text = $"Inventory\nRocks: {GameManager.Instance.PurchasedRocks.Value}\nShelves: {GameManager.Instance.PurchasedShelves.Value}";
            inventoryText.gameObject.SetActive(phase == GameManager.GamePhase.Shop || phase == GameManager.GamePhase.Placement);
        }
    }
}
