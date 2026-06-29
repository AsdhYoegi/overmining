using UnityEngine;
using Unity.Netcode;
using System;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GamePhase
    {
        Mining,
        Collection,
        Shop,
        Placement
    }

    public NetworkVariable<GamePhase> CurrentPhase = new NetworkVariable<GamePhase>(GamePhase.Mining);
    public NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(60f);
    public NetworkVariable<int> TeamMoney = new NetworkVariable<int>(0);
    public NetworkVariable<int> CurrentLevel = new NetworkVariable<int>(1);
    public NetworkVariable<int> DailyQuota = new NetworkVariable<int>(50);
    
    // 購入した在庫
    public NetworkVariable<int> PurchasedRocks = new NetworkVariable<int>(0);
    public NetworkVariable<int> PurchasedShelves = new NetworkVariable<int>(0);

    public float defaultMiningTime = 60f;

    public event Action<GamePhase> OnPhaseChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentPhase.Value = GamePhase.Mining;
            TimeRemaining.Value = defaultMiningTime;
            if (CurrentLevel.Value == 1)
            {
                DailyQuota.Value = 50;
            }
        }

        CurrentPhase.OnValueChanged += (oldPhase, newPhase) =>
        {
            OnPhaseChanged?.Invoke(newPhase);
            if (IsServer && newPhase == GamePhase.Mining)
            {
                // 朝になったら岩をリセット
                RockNode[] rocks = FindObjectsByType<RockNode>(FindObjectsSortMode.None);
                foreach(var r in rocks) r.ResetRock();
            }
        };
    }

    private void Update()
    {
        if (!IsServer) return;

        if (CurrentPhase.Value == GamePhase.Mining)
        {
            TimeRemaining.Value -= Time.deltaTime;
            if (TimeRemaining.Value <= 0)
            {
                TimeRemaining.Value = 0;
                AdvancePhaseServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AdvancePhaseServerRpc()
    {
        switch (CurrentPhase.Value)
        {
            case GamePhase.Mining:
                CurrentPhase.Value = GamePhase.Collection;
                break;
            case GamePhase.Collection:
                TeamMoney.Value -= DailyQuota.Value;
                if (TeamMoney.Value < 0) TeamMoney.Value = 0;
                CurrentPhase.Value = GamePhase.Shop;
                break;
            case GamePhase.Shop:
                CurrentPhase.Value = GamePhase.Placement;
                break;
            case GamePhase.Placement:
                CurrentPhase.Value = GamePhase.Mining;
                CurrentLevel.Value++;
                DailyQuota.Value = 100 + (CurrentLevel.Value * 50);
                TimeRemaining.Value = defaultMiningTime;
                break;
        }
    }

    public void AddMoney(int amount)
    {
        if (IsServer)
        {
            TeamMoney.Value += amount;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuyRockServerRpc()
    {
        if (TeamMoney.Value >= 10)
        {
            TeamMoney.Value -= 10;
            PurchasedRocks.Value++;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuyShelfServerRpc()
    {
        if (TeamMoney.Value >= 10)
        {
            TeamMoney.Value -= 10;
            PurchasedShelves.Value++;
        }
    }
}
