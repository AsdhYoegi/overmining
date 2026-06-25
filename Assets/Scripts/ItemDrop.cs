using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class ItemDrop : NetworkBehaviour
{
    public NetworkVariable<Vector3> startPos = new NetworkVariable<Vector3>();
    public NetworkVariable<Vector3> targetPos = new NetworkVariable<Vector3>();
    public NetworkVariable<float> startTime = new NetworkVariable<float>();
    
    private float moveTime = 0.5f;
    private float jumpHeight = 2.0f;
    private bool hasLanded = false;

    private void Update()
    {
        // 目標がセットされていない、または到着済みなら何もしない
        if (hasLanded || targetPos.Value == Vector3.zero || startTime.Value == 0f) return;

        // サーバー側の時間を基準にして経過時間を計算
        float elapsedTime = (float)NetworkManager.Singleton.ServerTime.Time - startTime.Value;
        
        if (elapsedTime >= moveTime)
        {
            // 到着
            transform.position = targetPos.Value;
            transform.rotation = Quaternion.identity;
            hasLanded = true;
        }
        else if (elapsedTime > 0)
        {
            // 移動中
            float t = elapsedTime / moveTime;
            Vector3 currentPos = Vector3.Lerp(startPos.Value, targetPos.Value, t);
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            currentPos.y += height;

            transform.position = currentPos;
            transform.Rotate(Vector3.up * 720f * Time.deltaTime);
        }
    }
}
