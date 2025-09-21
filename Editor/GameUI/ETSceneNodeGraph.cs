using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ETSceneNodeGraph : EditorWindow
{
    // -------------------- Fields --------------------
    private Vector2 viewOffset = Vector2.zero;
    private bool isDragging = false;
    private float zoomLevel = 1.0f;

    private const float MinZoom = 0.2f;
    private const float MaxZoom = 3.0f;

    private static Dictionary<Color, Texture2D> textureCache;
    private GUIStyle nodeStyle;

    private bool needsRepaint = false;

    // -------------------- Unity Lifecycle --------------------
    [MenuItem("GameUI/ET Scene Node Graph")]
    static void OpenWindow() => GetWindow<ETSceneNodeGraph>("Scene Node Graph");

    void OnGUI()
    {
        EnsureInitialized();
        HandleMouseEvents();

        Matrix4x4 oldMatrix = GUI.matrix;

        // 1) 绘制网格（屏幕空间）
        GUI.matrix = Matrix4x4.identity;
        DrawGrid(20f, 0.2f, Color.gray);
        DrawGrid(100f, 0.4f, Color.gray);

        // 2) 绘制节点 & 连线（world 空间）
        GUI.matrix = Matrix4x4.TRS(viewOffset, Quaternion.identity, Vector3.one) *
                     Matrix4x4.Scale(new Vector3(zoomLevel, zoomLevel, 1f));

        DrawGraphContent();

        // 3) 恢复矩阵 & UI 信息
        GUI.matrix = oldMatrix;
        GUI.Label(new Rect(10, 10, 400, 20), $"偏移: {viewOffset} | 缩放: {zoomLevel:F2}x");

        if (needsRepaint)
        {
            needsRepaint = false;
            Repaint();
        }
    }

    // -------------------- Input Handling --------------------
    void HandleMouseEvents()
    {
        Event e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0:
                isDragging = true;
                e.Use();
                break;

            case EventType.MouseDrag when isDragging && e.button == 0:
                viewOffset += e.delta;
                GUI.changed = true;
                needsRepaint = true;
                e.Use();
                break;

            case EventType.MouseUp when e.button == 0:
                isDragging = false;
                e.Use();
                break;

            case EventType.ScrollWheel:
                float zoomChange = -e.delta.y / 100f;
                float oldZoom = zoomLevel;
                zoomLevel = Mathf.Clamp(zoomLevel + zoomChange, MinZoom, MaxZoom);

                Vector2 mousePos = e.mousePosition;
                viewOffset = (viewOffset - mousePos) * (zoomLevel / oldZoom) + mousePos;

                needsRepaint = true;
                e.Use();
                break;
        }
    }

    // -------------------- Drawing --------------------
    void DrawGrid(float spacing, float opacity, Color color)
    {
        Handles.BeginGUI();
        Color prevColor = Handles.color;
        Handles.color = new Color(color.r, color.g, color.b, opacity);

        float worldW = position.width / zoomLevel;
        float worldH = position.height / zoomLevel;
        float worldX = -viewOffset.x / zoomLevel;
        float worldY = -viewOffset.y / zoomLevel;

        float startX = Mathf.Floor(worldX / spacing) * spacing;
        float startY = Mathf.Floor(worldY / spacing) * spacing;

        int cols = Mathf.Min(Mathf.CeilToInt(worldW / spacing) + 2, 2048);
        int rows = Mathf.Min(Mathf.CeilToInt(worldH / spacing) + 2, 2048);

        // 垂直线
        for (int i = 0; i <= cols; i++)
        {
            float sx = viewOffset.x + (startX + i * spacing) * zoomLevel;
            sx = Mathf.Round(sx) + 0.5f;
            Handles.DrawLine(new Vector3(sx, -10f, 0f), new Vector3(sx, position.height + 10f, 0f));
        }

        // 水平线
        for (int j = 0; j <= rows; j++)
        {
            float sy = viewOffset.y + (startY + j * spacing) * zoomLevel;
            sy = Mathf.Round(sy) + 0.5f;
            Handles.DrawLine(new Vector3(-10f, sy, 0f), new Vector3(position.width + 10f, sy, 0f));
        }

        Handles.color = prevColor;
        Handles.EndGUI();
    }

    void DrawGraphContent()
    {
        Rect startNode = new Rect(50, 50, 100, 40);
        Rect stateA = new Rect(250, 50, 100, 40);
        Rect stateB = new Rect(250, 150, 100, 40);
        Rect endNode = new Rect(450, 100, 100, 40);

        DrawNode(startNode, "开始", Color.green);
        DrawNode(stateA, "状态A", Color.blue);
        DrawNode(stateB, "状态B", Color.red);
        DrawNode(endNode, "结束", Color.gray);

        DrawConnection(GetRightCenter(startNode), GetLeftCenter(stateA));
        DrawConnection(GetRightCenter(startNode), GetLeftCenter(stateB));
        DrawConnection(GetRightCenter(stateA), GetLeftCenter(endNode));
        DrawConnection(GetRightCenter(stateB), GetLeftCenter(endNode));
    }

    void DrawNode(Rect rect, string title, Color color)
    {
        nodeStyle.normal.background = GetTextureForColor(color);
        GUI.Box(rect, title, nodeStyle);
    }

    void DrawConnection(Vector2 from, Vector2 to)
    {
        float distance = Mathf.Abs(to.x - from.x) * 0.5f;
        Vector3 startTangent = from + Vector2.right * distance;
        Vector3 endTangent = to + Vector2.left * distance;

        Handles.DrawBezier(from, to, startTangent, endTangent, Color.white, null, 2f);
        DrawArrow(to, endTangent);
    }

    void DrawArrow(Vector3 pos, Vector3 tangent)
    {
        float size = 6f;
        Vector3 dir = (pos - tangent).normalized;

        Vector3[] arrow = new Vector3[3];
        arrow[0] = pos;
        arrow[1] = pos + (Quaternion.Euler(0, 0, 135) * dir) * size;
        arrow[2] = pos + (Quaternion.Euler(0, 0, -135) * dir) * size;

        Handles.DrawAAConvexPolygon(arrow);
    }

    // -------------------- Utilities --------------------
    void EnsureInitialized()
    {
        if (nodeStyle == null)
        {
            GUIStyle baseStyle = GUI.skin != null ? GUI.skin.box : new GUIStyle();
            nodeStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }

        textureCache ??= new Dictionary<Color, Texture2D>();
    }

    Texture2D GetTextureForColor(Color color)
    {
        if (!textureCache.TryGetValue(color, out var tex) || tex == null)
        {
            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            textureCache[color] = tex;
        }
        return tex;
    }

    Vector2 GetRightCenter(Rect rect) => new(rect.x + rect.width, rect.y + rect.height / 2f);
    Vector2 GetLeftCenter(Rect rect) => new(rect.x, rect.y + rect.height / 2f);
}
