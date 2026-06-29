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

        // 配置フェーズ中はツルハシ・アイテム持ち上げ操作を無効化
        if (GameManager.Instance != null && GameManager.Instance.CurrentPhase.Value == GameManager.GamePhase.Placement) return;

        // Eキーが押された瞬間：持っていれば足元にそっと置く（Drop）
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldItemRef.Value.TryGet(out NetworkObject _))
            {
                PlaceItemServerRpc();
            }
        }

        // スペースキーが押された瞬間：拾う か 遠くに投げる（Throw）
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (heldItemRef.Value.TryGet(out NetworkObject _))
            {
                ThrowItemServerRpc();
            }
            else
            {
                TryGrabItem();
            }
        }

        // 自分が持っているアイテムの位置を更新（見た目の同期はNetworkTransformでも可能だが、手動で追従させるのが手軽）
        if (currentHeldItem != null)
        {
            currentHeldItem.transform.position = transform.position + transform.rotation * holdPosition;
            currentHeldItem.transform.rotation = transform.rotation;
        }
    }

    public bool CanGrabItem()
    {
        Vector3 center = transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;
        Collider[] colliders = Physics.OverlapSphere(center, 1.5f);
        foreach (var col in colliders)
        {
            if (col.GetComponent<ItemDrop>() != null) return true;
        }
        return false;
    }

    private void TryGrabItem()
    {
        // プレイヤーの足元〜少し前方の範囲でアイテムを探す（半径1.5m）
        Vector3 center = transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;
        Collider[] colliders = Physics.OverlapSphere(center, 1.5f);
        
        foreach (var col in colliders)
        {
            ItemDrop item = col.GetComponent<ItemDrop>();
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
            if (netObj.OwnerClientId != OwnerClientId)
            {
                netObj.ChangeOwnership(OwnerClientId);
            }

            heldItemRef.Value = itemRef;
            
            // 拾い上げたマスの使用権を解除する
            GridManager.Instance.UnregisterObjectAtWorld(netObj.transform.position, netObj.gameObject);

            SetHeldItemClientRpc(itemRef);
        }
    }

    public bool HasItem()
    {
        return currentHeldItem != null;
    }

    private bool IsValidDropTarget(Vector3 dropPos)
    {
        if (GridManager.Instance.IsGridEmptyAtWorld(dropPos)) return true;
        
        GameObject occupant = GridManager.Instance.GetObjectAtGridWorld(dropPos);
        if (occupant != null && occupant.GetComponent<DisplayShelf>() != null)
        {
            return true; // 商品棚の上には置ける/投げられる！
        }
        return false;
    }

    [ServerRpc]
    private void PlaceItemServerRpc()
    {
        if (heldItemRef.Value.TryGet(out NetworkObject netObj))
        {
            // プレイヤーの足元のグリッド位置を計算
            Vector3 targetPos = transform.position;
            
            // 1マス（1.0m）単位でグリッドスナップし、Y座標は床(0)に固定（セルの中心は0.5）
            float snapX = Mathf.Floor(targetPos.x) + 0.5f;
            float snapZ = Mathf.Floor(targetPos.z) + 0.5f;
            Vector3 dropPos = new Vector3(snapX, 0f, snapZ);

            // GridManager で判定（置こうとしているマスが空いているか、納品箱か）
            if (!IsValidDropTarget(dropPos))
            {
                return; // 塞がっている場合は置けない
            }

            netObj.transform.position = dropPos;
            
            // 落ちた先のマスを登録する
            GridManager.Instance.RegisterObjectAtWorld(dropPos, netObj.gameObject);
            
            // 所有権をサーバーに戻す
            if (netObj.OwnerClientId != NetworkManager.ServerClientId)
            {
                netObj.ChangeOwnership(NetworkManager.ServerClientId);
            }

            heldItemRef.Value = default;
            ClearHeldItemClientRpc();
        }
    }

    [ServerRpc]
    private void ThrowItemServerRpc()
    {
        if (heldItemRef.Value.TryGet(out NetworkObject netObj))
        {
            // 投げる距離（最大）
            float throwDistance = 3.5f;

            // プレイヤーの高さ1.0m（納品箱などの背の低いオブジェクトより上）からレイキャスト
            Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;
            // プレイヤー自身に当たらないようにするため、少し前から飛ばす
            rayOrigin += transform.forward * 0.5f;

            // 障害物チェック
            if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, throwDistance))
            {
                // ItemDrop自身やプレイヤー以外に当たった場合（壁や岩山など）
                ItemDrop hitDrop = hit.collider.GetComponent<ItemDrop>();
                if (hitDrop == null)
                {
                    // ぶつかった地点の少し手前（0.5m）を落下距離にする（最低でも足元(0)にする）
                    throwDistance = Mathf.Max(0f, hit.distance - 0.5f);
                }
            }

            // プレイヤーから計算した距離先のグリッド位置を計算
            Vector3 targetPos = transform.position + transform.forward * throwDistance;
            
            // 1マス単位でグリッドスナップし、Y座標は床(0)に固定（セルの中心は0.5）
            float snapX = Mathf.Floor(targetPos.x) + 0.5f;
            float snapZ = Mathf.Floor(targetPos.z) + 0.5f;
            Vector3 originalDropPos = new Vector3(snapX, 0f, snapZ);
            Vector3 dropPos = originalDropPos;
            bool foundSpot = false;

            // まず本来の投下地点をチェック
            if (IsValidDropTarget(originalDropPos))
            {
                foundSpot = true;
            }
            else
            {
                // 塞がっている場合は周囲8マスをチェック
                Vector3[] offsets = {
                    new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1),
                    new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(-1, 0, -1)
                };
                foreach (var offset in offsets)
                {
                    Vector3 checkPos = originalDropPos + offset;
                    if (IsValidDropTarget(checkPos))
                    {
                        dropPos = checkPos;
                        foundSpot = true;
                        break; // 最初の空きマスを見つけたらそこに決定
                    }
                }
            }

            if (!foundSpot)
            {
                return; // 周囲も含めて全て塞がっている場合は投げられない
            }

            // アニメーション設定 (手元から投げる)
            ItemDrop itemDrop = netObj.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                Vector3 startPos = transform.position + Vector3.up * 1.5f;
                itemDrop.startPos.Value = startPos;
                itemDrop.targetPos.Value = dropPos;
                itemDrop.startTime.Value = (float)NetworkManager.Singleton.ServerTime.Time;
                
                // 確実にアニメーションを再開させる
                itemDrop.TriggerDropClientRpc();
            }

            GridManager.Instance.RegisterObjectAtWorld(dropPos, netObj.gameObject);

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
        }
    }

    [ClientRpc]
    private void ClearHeldItemClientRpc()
    {
        if (currentHeldItem != null)
        {
            currentHeldItem = null;
        }
    }
}
