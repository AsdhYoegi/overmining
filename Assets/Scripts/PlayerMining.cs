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
            PlayerArm arm = GetComponent<PlayerArm>();
            if (arm != null)
            {
                // アイテムを持っている場合は「投げる」が優先されるため掘らない
                if (arm.HasItem()) return;

                // 目の前に拾えるアイテムがある場合は「拾う」が優先されるため掘らない
                if (arm.CanGrabItem()) return;
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
