using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target; // 追従する対象（ローカルプレイヤー）
    
    // 斜め視点の設定
    private Vector3 normalOffset = new Vector3(0, 8f, -8f);
    private Vector3 normalRotation = new Vector3(45f, 0f, 0f);

    // 真上視点（トップダウン）の設定
    private Vector3 topDownOffset = new Vector3(0, 12f, 0f);
    private Vector3 topDownRotation = new Vector3(90f, 0f, 0f);

    private bool isTopDownView = false;

    private void Update()
    {
        // Cキー または ゲームパッドのセレクトボタンで視点切り替え
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            isTopDownView = !isTopDownView;
        }
        else if (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame)
        {
            isTopDownView = !isTopDownView;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 現在の視点モードに応じたオフセットと角度を決定
        Vector3 currentOffset = isTopDownView ? topDownOffset : normalOffset;
        Vector3 currentRotation = isTopDownView ? topDownRotation : normalRotation;

        // ぼよんと動かないように、Lerpを外して位置を完全に固定で追従させる
        transform.position = target.position + currentOffset;

        // 角度を適用
        transform.rotation = Quaternion.Euler(currentRotation);
    }
}
