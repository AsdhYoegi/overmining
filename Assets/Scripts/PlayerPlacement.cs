using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerPlacement : NetworkBehaviour
{
    public GameObject rockPrefab;
    public GameObject shelfPrefab;
    public Material previewValidMat;
    public Material previewInvalidMat;
    
    private GameObject previewObject;
    private int selectedItem = 0; // 0=None, 1=Rock, 2=Shelf

    private void Awake()
    {
    }

    private void Update()
    {
        if (!IsOwner) return;

        bool isPlacementPhase = GameManager.Instance != null && GameManager.Instance.CurrentPhase.Value == GameManager.GamePhase.Placement;

        if (!isPlacementPhase)
        {
            if (previewObject != null) Destroy(previewObject);
            selectedItem = 0;
            return;
        }

        // アイテム選択
        if (Keyboard.current.digit1Key.wasPressedThisFrame) ChangeSelection(1);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) ChangeSelection(2);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) ChangeSelection(0); // 選択解除（移動モード）

        // プレビュー表示位置の計算
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(transform.position);
        Vector2Int targetGrid = gridPos;
        
        Vector3 forward = transform.forward;
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
        {
            if (forward.x > 0) targetGrid += Vector2Int.right;
            else targetGrid += Vector2Int.left;
        }
        else
        {
            if (forward.z > 0) targetGrid += Vector2Int.up;
            else targetGrid += Vector2Int.down;
        }
        
        Vector3 placePos = GridManager.Instance.GridToWorld(targetGrid);
        bool canPlace = GridManager.Instance.IsGridEmpty(targetGrid);

        // プレビューの更新
        if (selectedItem != 0 && previewObject != null)
        {
            previewObject.transform.position = placePos;
            bool hasStock = (selectedItem == 1 && GameManager.Instance.PurchasedRocks.Value > 0) ||
                            (selectedItem == 2 && GameManager.Instance.PurchasedShelves.Value > 0);
                            
            UpdatePreviewMaterial(canPlace && hasStock);
        }

        // Spaceキーでアクション
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (selectedItem != 0)
            {
                // 設置アクション
                bool hasStock = (selectedItem == 1 && GameManager.Instance.PurchasedRocks.Value > 0) ||
                                (selectedItem == 2 && GameManager.Instance.PurchasedShelves.Value > 0);
                                
                if (canPlace && hasStock)
                {
                    PlaceObjectServerRpc(selectedItem, placePos, targetGrid);
                }
            }
            else
            {
                // 何も選択していない場合は、既存設備を回収（移動）するアクション
                if (!canPlace) // 何かがある
                {
                    GameObject obj = GridManager.Instance.GetObjectAtGridWorld(placePos); // 修正: WorldPosで取得
                    if (obj != null)
                    {
                        // 岩山以外（今回は商品棚）なら回収できる
                        DisplayShelf shelf = obj.GetComponent<DisplayShelf>();
                        if (shelf != null && shelf.displayedItems.Count == 0) // アイテムが乗ってない場合のみ
                        {
                            PickupShelfServerRpc(placePos);
                        }
                    }
                }
            }
        }
    }

    private void ChangeSelection(int id)
    {
        selectedItem = id;
        if (previewObject != null) Destroy(previewObject);
        
        if (id == 1 && rockPrefab != null)
        {
            previewObject = Instantiate(rockPrefab);
            SetupPreview(previewObject);
        }
        else if (id == 2 && shelfPrefab != null)
        {
            previewObject = Instantiate(shelfPrefab);
            SetupPreview(previewObject);
        }
    }

    private void SetupPreview(GameObject obj)
    {
        // NetworkObjectや不要なコンポーネントを削除して見た目だけにする
        if (obj.GetComponent<NetworkObject>() != null) Destroy(obj.GetComponent<NetworkObject>());
        if (obj.GetComponent<RockNode>() != null) Destroy(obj.GetComponent<RockNode>());
        if (obj.GetComponent<DisplayShelf>() != null) Destroy(obj.GetComponent<DisplayShelf>());
        if (obj.GetComponent<Collider>() != null) Destroy(obj.GetComponent<Collider>());
        
        UpdatePreviewMaterial(true);
    }

    private void UpdatePreviewMaterial(bool isValid)
    {
        if (previewObject == null) return;
        Material mat = isValid ? previewValidMat : previewInvalidMat;
        if (mat == null) return;
        
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.sharedMaterial = mat;
        }
    }

    [ServerRpc]
    private void PlaceObjectServerRpc(int itemId, Vector3 pos, Vector2Int gridPos)
    {
        if (!GridManager.Instance.IsGridEmpty(gridPos)) return;

        if (itemId == 1 && GameManager.Instance.PurchasedRocks.Value > 0)
        {
            GameManager.Instance.PurchasedRocks.Value--;
            GameObject go = Instantiate(rockPrefab, pos, Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn();
        }
        else if (itemId == 2 && GameManager.Instance.PurchasedShelves.Value > 0)
        {
            GameManager.Instance.PurchasedShelves.Value--;
            GameObject go = Instantiate(shelfPrefab, pos, Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn();
        }
    }

    [ServerRpc]
    private void PickupShelfServerRpc(Vector3 worldPos)
    {
        GameObject obj = GridManager.Instance.GetObjectAtGridWorld(worldPos);
        if (obj != null)
        {
            DisplayShelf shelf = obj.GetComponent<DisplayShelf>();
            if (shelf != null && shelf.displayedItems.Count == 0)
            {
                // 回収して在庫に戻す
                GameManager.Instance.PurchasedShelves.Value++;
                Vector2Int grid = GridManager.Instance.WorldToGrid(worldPos);
                GridManager.Instance.UnregisterObject(grid);
                obj.GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}
