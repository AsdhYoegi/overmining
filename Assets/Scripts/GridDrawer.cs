using UnityEngine;

public class GridDrawer : MonoBehaviour
{
    [SerializeField] private float gridSize = 1.5f;
    [SerializeField] private int gridCount = 3; // 3x3マス
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private Material lineMaterial;

    private void Start()
    {
        // 良い感じのデフォルトマテリアルが見つからなければ専用のSprites-Defaultなどを使う
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            lineMaterial = new Material(shader);
        }

        DrawGrid();
    }

    private void DrawGrid()
    {
        float halfSize = (gridCount * gridSize) / 2f;
        // 地面にめり込まないように少し浮かす
        Vector3 startOffset = new Vector3(-halfSize, 0.02f, -halfSize);

        // X軸方向の線
        for (int i = 0; i <= gridCount; i++)
        {
            Vector3 start = startOffset + new Vector3(0, 0, i * gridSize);
            Vector3 end = start + new Vector3(gridCount * gridSize, 0, 0);
            CreateLine(start, end);
        }

        // Z軸方向の線
        for (int i = 0; i <= gridCount; i++)
        {
            Vector3 start = startOffset + new Vector3(i * gridSize, 0, 0);
            Vector3 end = start + new Vector3(0, 0, gridCount * gridSize);
            CreateLine(start, end);
        }
    }

    private void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(this.transform, false);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = lineMaterial;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
    }
}
