using UnityEngine;
using Unity.Netcode;

public class DeliveryBox : NetworkBehaviour
{
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
