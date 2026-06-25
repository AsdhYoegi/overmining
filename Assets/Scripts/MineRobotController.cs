using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MineRobotController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("ロボットの移動速度")]
    [SerializeField] private float moveSpeed = 5.0f;
    [Tooltip("ロボットの旋回（向き直り）速度：1秒間に回転する角度")]
    [SerializeField] private float turnSpeed = 2880.0f;

    private Rigidbody rb;
    private float forwardInput;
    private float rightInput;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 自分が操作権限を持つプレイヤーなら、メインカメラを自分に追従させる
        if (IsOwner)
        {
            if (Camera.main != null)
            {
                CameraController camController = Camera.main.gameObject.GetComponent<CameraController>();
                if (camController == null)
                {
                    camController = Camera.main.gameObject.AddComponent<CameraController>();
                }
                camController.target = this.transform;
            }
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        if (!IsOwner) return;

        forwardInput = 0f;
        rightInput = 0f;

        // キーボードからの入力（4方向＋斜めの8方向）
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) forwardInput += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) forwardInput -= 1f;
            
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) rightInput += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) rightInput -= 1f;
        }

        // コントローラー（ゲームパッド）からの入力（アナログ360度を許容）
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.magnitude > 0.1f)
            {
                rightInput += stick.x;
                forwardInput += stick.y;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        MoveRobot();
    }

    private void MoveRobot()
    {
        // ワールド座標ベースの入力ベクトル
        // コントローラーのアナログ入力やキーボードの斜め入力を考慮して normalized を掛けるかクランプする
        Vector3 moveDirection = new Vector3(rightInput, 0f, forwardInput);
        if (moveDirection.magnitude > 1f) moveDirection.Normalize();
        
        // 入力がある場合のみ移動と回転を行う
        if (moveDirection.magnitude >= 0.1f)
        {
            // 滑らかに向きを変える（速すぎると斜め入力が難しいため）
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, 1000.0f * Time.fixedDeltaTime));

            // 移動
            Vector3 moveMovement = moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + moveMovement);
        }
    }
}
