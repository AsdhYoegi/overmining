using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance { get; private set; }

    // グリッド座標をキーにして、そのマスを占有しているオブジェクトを保存
    private Dictionary<Vector2Int, GameObject> gridOccupancy = new Dictionary<Vector2Int, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // サーバー上でのみ管理を行う
        gridOccupancy.Clear();
    }

    // ワールド座標をグリッド座標に変換 (1マス=1m)
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int z = Mathf.RoundToInt(worldPos.z);
        return new Vector2Int(x, z);
    }

    // グリッド座標をワールド座標に変換 (Yは0で返すため、必要に応じて上書きする)
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x, 0, gridPos.y);
    }

    // 特定のマスにオブジェクトを登録（サーバー専用）
    public bool RegisterObject(Vector2Int gridPos, GameObject obj)
    {
        if (!IsServer) return false;

        if (gridOccupancy.ContainsKey(gridPos))
        {
            if (gridOccupancy[gridPos] == null)
            {
                // すでに破棄されているオブジェクトの残骸なら上書き
                gridOccupancy[gridPos] = obj;
                return true;
            }
            // 既に何かが存在する場合は登録失敗
            return false;
        }
        else
        {
            gridOccupancy.Add(gridPos, obj);
            return true;
        }
    }

    // ワールド座標ベースでの登録
    public bool RegisterObjectAtWorld(Vector3 worldPos, GameObject obj)
    {
        return RegisterObject(WorldToGrid(worldPos), obj);
    }

    // 特定のマスの登録を解除（サーバー専用）
    public void UnregisterObject(Vector2Int gridPos, GameObject requestingObj = null)
    {
        if (!IsServer) return;

        if (gridOccupancy.ContainsKey(gridPos))
        {
            // requestingObjが指定されている場合、自分が登録したものだけを解除できる（安全対策）
            if (requestingObj == null || gridOccupancy[gridPos] == requestingObj || gridOccupancy[gridPos] == null)
            {
                gridOccupancy.Remove(gridPos);
            }
        }
    }

    // ワールド座標ベースでの解除
    public void UnregisterObjectAtWorld(Vector3 worldPos, GameObject requestingObj = null)
    {
        UnregisterObject(WorldToGrid(worldPos), requestingObj);
    }

    // ColliderのBoundsに基づいて、被っているマスを自動的に全て登録する
    public void RegisterBounds(Bounds bounds, GameObject obj)
    {
        // 境界ギリギリの微小なはみ出しで隣のマスを占有しないよう、少し内側に縮小して判定する
        Vector3 min = bounds.min + Vector3.one * 0.2f;
        Vector3 max = bounds.max - Vector3.one * 0.2f;

        int minX = Mathf.RoundToInt(min.x);
        int maxX = Mathf.RoundToInt(max.x);
        int minZ = Mathf.RoundToInt(min.z);
        int maxZ = Mathf.RoundToInt(max.z);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                RegisterObject(new Vector2Int(x, z), obj);
            }
        }
    }

    // 特定のマスが空いているか確認
    public bool IsGridEmpty(Vector2Int gridPos)
    {
        if (!IsServer) return false; // クライアントからは判定させない（常にサーバーが行う）

        if (gridOccupancy.ContainsKey(gridPos))
        {
            if (gridOccupancy[gridPos] == null)
            {
                // オブジェクトがDestoryされていれば空きとみなす
                gridOccupancy.Remove(gridPos);
                return true;
            }
            return false; // 塞がっている
        }
        return true; // 空いている
    }

    // ワールド座標ベースでの空き確認
    public bool IsGridEmptyAtWorld(Vector3 worldPos)
    {
        return IsGridEmpty(WorldToGrid(worldPos));
    }
    
    // エディタ上でのデバッグ表示（使用中のマスを赤く塗る）
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && IsServer)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            foreach (var kvp in gridOccupancy)
            {
                if (kvp.Value != null)
                {
                    Vector3 pos = GridToWorld(kvp.Key);
                    Gizmos.DrawCube(pos + Vector3.up * 0.1f, new Vector3(0.9f, 0.2f, 0.9f));
                }
            }
        }
    }
}
