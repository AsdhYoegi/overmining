using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerArm : NetworkBehaviour
{
    [Header("Arm Settings")]
    [Tooltip("アイテムを掴める距離")]
    public float reachDistance = 1.5f;
    [Tooltip("アイテムを保持する位置（ローカル座標）")]
    public Vector3 holdPosition = new Vector3(0, 1.5f, 0);

    private NetworkVariable<NetworkObjectReference> heldItemRef = new NetworkVariable<NetworkObjectReference>();
    
    // 現在自分が持っているアイテムの参照（クライアント/サーバー共通）
    private NetworkObject currentHeldItem;

    private void Update()
    {
        if (!IsOwner) return;

        // スペースキー または ゲームパッドの下ボタン（B/A等）
        bool grabInput = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                         (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (grabInput)
        {
            if (currentHeldItem == null)
            {
                TryGrabItem();
            }
            else
            {
                DropItemServerRpc();
            }
        }

        // 自分が持っているアイテムの位置を更新（見た目の同期はNetworkTransformでも可能だが、手動で追従させるのが手軽）
        if (currentHeldItem != null)
        {
            currentHeldItem.transform.position = transform.position + transform.rotation * holdPosition;
            currentHeldItem.transform.rotation = transform.rotation;
        }
    }

    private void TryGrabItem()
    {
        // 前方にあるアイテムを探す
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;
        
        RaycastHit[] hits = Physics.SphereCastAll(origin, 0.5f, direction, reachDistance);
        
        foreach (var hit in hits)
        {
            ItemDrop item = hit.collider.GetComponent<ItemDrop>();
            if (item != null)
            {
                NetworkObject netObj = item.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    GrabItemServerRpc(netObj);
                    break; // 最初に見つかった1つだけを掴む
                }
            }
        }
    }

    [ServerRpc]
    private void GrabItemServerRpc(NetworkObjectReference itemRef)
    {
        if (itemRef.TryGet(out NetworkObject netObj))
        {
            // 他の人が既に持っていないかなどのチェックは後で追加可能
            
            // サーバー側で NetworkObject の所有権をこのプレイヤーに変更する（必須ではないが、クライアントが操作しやすくするため）
            if (netObj.OwnerClientId != OwnerClientId)
            {
                netObj.ChangeOwnership(OwnerClientId);
            }

            // 掴んだアイテムを登録
            heldItemRef.Value = itemRef;
            SetHeldItemClientRpc(itemRef);
        }
    }

    public bool HasItem()
    {
        return currentHeldItem != null;
    }

    [ServerRpc]
    private void DropItemServerRpc()
    {
        if (heldItemRef.Value.TryGet(out NetworkObject netObj))
        {
            // プレイヤーの少し前（1マス先）のグリッド位置を計算
            Vector3 targetPos = transform.position + transform.forward * 1.5f;
            
            // 1マス（1.0m）単位でグリッドスナップ
            float snapX = Mathf.Round(targetPos.x);
            float snapZ = Mathf.Round(targetPos.z);
            Vector3 dropPos = new Vector3(snapX, targetPos.y, snapZ);

            // 投げようとしているマスに既に別のアイテムがないかチェック
            Collider[] colliders = Physics.OverlapSphere(dropPos, 0.45f);
            foreach (var col in colliders)
            {
                if (col.GetComponent<ItemDrop>() != null && col.gameObject != netObj.gameObject)
                {
                    // 既にアイテムがある場合は投げられない（手に持ったままにする）
                    return;
                }
            }

            netObj.transform.position = dropPos;
            
            // 所有権をサーバーに戻す
            if (netObj.OwnerClientId != NetworkManager.ServerClientId)
            {
                netObj.ChangeOwnership(NetworkManager.ServerClientId);
            }

            heldItemRef.Value = default;
            ClearHeldItemClientRpc();
        }
    }

    [ClientRpc]
    private void SetHeldItemClientRpc(NetworkObjectReference itemRef)
    {
        if (itemRef.TryGet(out NetworkObject netObj))
        {
            currentHeldItem = netObj;
            // 当たり判定を無効化して自分に引っかからないようにする
            var col = currentHeldItem.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    [ClientRpc]
    private void ClearHeldItemClientRpc()
    {
        if (currentHeldItem != null)
        {
            // 当たり判定を戻す
            var col = currentHeldItem.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            currentHeldItem = null;
        }
    }
}
