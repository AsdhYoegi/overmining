using UnityEngine;
using Unity.Netcode;

public class DeliveryBox : NetworkBehaviour
{
    private void Awake()
    {
        // 2x2マスの中央に綺麗に収まるよう、座標を .5 にスナップさせる
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
        if (!IsServer) return; // 納品の判定はサーバーでのみ行う

        ItemDrop item = other.GetComponent<ItemDrop>();
        if (item != null)
        {
            NetworkObject netObj = item.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                Debug.Log($"【納品】アイテムが納品箱に入れられました！ (ID: {netObj.NetworkObjectId})");
                
                // アイテムを消滅させる（納品完了）
                netObj.Despawn();
                
                // TODO: ここで共有資金の加算やオーダーのクリア処理を呼ぶ
            }
        }
    }
}
