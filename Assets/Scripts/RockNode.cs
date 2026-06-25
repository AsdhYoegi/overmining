using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class RockNode : NetworkBehaviour
{
    [Header("Rock Settings")]
    [Tooltip("1日に採掘できる上限")]
    [SerializeField] private int maxHealth = 8;
    [Tooltip("ドロップするアイテムのプレハブ")]
    public GameObject dropItemPrefab;
    
    // 現在の耐久度（サーバーとクライアントで自動同期される）
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    
    [Header("Mining Settings")]
    public float mineCooldown = 0.5f;
    private float lastMineTime = 0f;
    
    private Renderer rockRenderer;
    private Color originalColor;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        if (dropItemPrefab == null)
        {
            dropItemPrefab = Resources.Load<GameObject>("ItemDrop");
        }

        rockRenderer = GetComponent<Renderer>();
        if (rockRenderer != null)
        {
            originalColor = rockRenderer.material.color;
        }
        
        // 耐久度が変わった時（減った時・復活した時）に見た目を更新する
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        if (rockRenderer != null)
        {
            if (newValue <= 0)
            {
                rockRenderer.material.color = Color.gray; // 枯渇したらグレーにする
            }
            else
            {
                rockRenderer.material.color = originalColor; // 復活したら元の色に
            }
        }
    }

    // プレイヤー側から呼ばれ、サーバー側で実行される採掘処理
    [ServerRpc(RequireOwnership = false)]
    public void MineServerRpc()
    {
        if (Time.time - lastMineTime < mineCooldown) return;
        lastMineTime = Time.time;

        // まだ掘れる場合のみ
        if (currentHealth.Value > 0)
        {
            currentHealth.Value--;
            
            SpawnDropItem();
        }
    }

    // 次の日の朝に呼ばれる関数
    public void ResetRock()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
    }

    private void SpawnDropItem()
    {
        if (dropItemPrefab == null)
        {
            Debug.LogError("Drop item prefab is not assigned.");
            return;
        }

        // 2x2マス（2.0x2.0）の岩の周囲12マスの相対座標（岩のコライダーを完全に避けるため少し外側の2.0を使用）
        Vector3[] directions = {
            // 上の辺
            new Vector3(-2.0f, 0, 2.0f), new Vector3(-1.0f, 0, 2.0f), new Vector3(1.0f, 0, 2.0f), new Vector3(2.0f, 0, 2.0f),
            // 下の辺
            new Vector3(-2.0f, 0, -2.0f), new Vector3(-1.0f, 0, -2.0f), new Vector3(1.0f, 0, -2.0f), new Vector3(2.0f, 0, -2.0f),
            // 左の辺
            new Vector3(-2.0f, 0, 1.0f), new Vector3(-2.0f, 0, -1.0f),
            // 右の辺
            new Vector3(2.0f, 0, 1.0f), new Vector3(2.0f, 0, -1.0f)
        };
        
        // 配列をシャッフルしてランダムな空きマスに落とす
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 temp = directions[i];
            int randomIndex = Random.Range(i, directions.Length);
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        Vector3 dropTarget = transform.position;
        bool foundSpot = false;
        
        foreach (var offset in directions)
        {
            Vector3 rawTarget = transform.position + offset;
            // グリッド（1マス単位）にスナップさせる
            float snapX = Mathf.Round(rawTarget.x);
            float snapZ = Mathf.Round(rawTarget.z);
            Vector3 targetSpot = new Vector3(snapX, rawTarget.y, snapZ);

            // そのマスに既にアイテムがないかチェック
            Collider[] colliders = Physics.OverlapSphere(targetSpot, 0.45f);
            bool isOccupied = false;
            foreach (var col in colliders)
            {
                if (col.GetComponent<ItemDrop>() != null)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
            {
                dropTarget = targetSpot;
                foundSpot = true;
                break;
            }
        }

        if (foundSpot)
        {
            // サーバー上でプレハブを生成（岩山の上空1mの位置からスタート）
            Vector3 startPos = transform.position + Vector3.up * 1.0f;
            GameObject item = Instantiate(dropItemPrefab, startPos, Quaternion.identity);
            NetworkObject netObj = item.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(); // 全クライアントに同期
            }

            ItemDrop itemDrop = item.GetComponent<ItemDrop>();
            if (itemDrop != null && IsServer)
            {
                itemDrop.startPos.Value = startPos;
                itemDrop.targetPos.Value = dropTarget;
                itemDrop.startTime.Value = (float)NetworkManager.Singleton.ServerTime.Time;
            }
        }
    }
}
