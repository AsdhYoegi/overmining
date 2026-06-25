using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerMining : NetworkBehaviour
{
    [Header("Mining Settings")]
    [Tooltip("岩を検知する距離")]
    [SerializeField] private float mineDistance = 2.0f;
    
    private void Update()
    {
        if (!IsOwner) return;

        // スペースキーが押された瞬間
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // アイテムを持っている場合は採掘しない（投げるアクションが優先）
            PlayerArm arm = GetComponent<PlayerArm>();
            if (arm != null && arm.HasItem())
            {
                return;
            }

            PerformMining();
        }
    }

    private void PerformMining()
    {
        // 目の前にRayを飛ばして岩を判定する
        RaycastHit hit;
        // ロボットの少し上から前方にRayを飛ばす
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, transform.forward, out hit, mineDistance))
        {
            RockNode rock = hit.collider.GetComponent<RockNode>();
            if (rock != null)
            {
                // 岩山コンポーネントが見つかったら、サーバー側に採掘をリクエストする
                rock.MineServerRpc();
            }
        }
    }
}
