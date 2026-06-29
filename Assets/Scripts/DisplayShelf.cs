using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class DisplayShelf : NetworkBehaviour
{
    public static DisplayShelf Instance { get; private set; }
    
    // 保持しているアイテムのリスト
    public List<ItemDrop> displayedItems = new List<ItemDrop>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        // 1x1マスのため、セルの中心（0.5）にスナップさせる
        Vector3 pos = transform.position;
        float newX = Mathf.Floor(pos.x) + 0.5f;
        float newZ = Mathf.Floor(pos.z) + 0.5f;
        transform.position = new Vector3(newX, pos.y, newZ);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                GridManager.Instance.RegisterBounds(col.bounds, gameObject);
            }
            else
            {
                Vector2Int baseGrid = GridManager.Instance.WorldToGrid(transform.position);
                GridManager.Instance.RegisterObject(baseGrid, gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // 判定はサーバーでのみ行う

        // 既に1つアイテムが置かれている場合は受け付けない
        if (displayedItems.Count >= 1) return;

        ItemDrop item = other.GetComponent<ItemDrop>();
        if (item != null && !displayedItems.Contains(item))
        {
            // 商品棚にアイテムを登録
            displayedItems.Add(item);
            
            // 見た目上、商品棚の上（少し浮かせる）に小さくして飾る
            item.startPos.Value = item.transform.position;
            item.targetPos.Value = transform.position + Vector3.up * 1.0f; // 棚の上の高さ
            item.targetScale.Value = new Vector3(0.5f, 0.5f, 0.5f); // 半分のサイズに縮小
            item.startTime.Value = (float)NetworkManager.Singleton.ServerTime.Time;
            item.TriggerDropClientRpc(); // アニメーション開始
            
            Debug.Log($"【商品棚】アイテムが並べられました！ (現在: {displayedItems.Count}個)");
        }
    }
    
    // お客さんがアイテムを買い取る処理
    public bool TrySellItem(string requestedItemName, out int price)
    {
        price = 0;
        for (int i = 0; i < displayedItems.Count; i++)
        {
            ItemDrop item = displayedItems[i];
            if (item != null && item.itemData != null && item.itemData.itemName == requestedItemName)
            {
                // 見つけた！売却処理
                price = item.itemData.sellPrice;
                displayedItems.RemoveAt(i);
                
                NetworkObject netObj = item.GetComponent<NetworkObject>();
                if (netObj != null) netObj.Despawn();
                
                return true;
            }
        }
        return false;
    }
}
