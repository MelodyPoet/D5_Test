using UnityEngine;

/// <summary>
/// 可视化行走区域边界的组件
/// 在Scene视图中显示LevelData.WalkRect定义的区域
/// </summary>
public class WalkAreaVisualizer : MonoBehaviour
{
    [Header("可视化设置")]
    [Tooltip("是否在Scene视图中显示行走区域")]
    public bool showInSceneView = true;
    
    [Tooltip("是否在Game视图中显示行走区域")]
    public bool showInGameView = false;
    
    [Tooltip("边界线颜色")]
    public Color boundaryColor = Color.red;
    
    [Tooltip("填充区域颜色（透明）")]
    public Color fillColor = new Color(0f, 1f, 0f, 0.1f);
    
    [Tooltip("边界线宽度")]
    [Range(1f, 5f)]
    public float lineWidth = 2f;

    private void OnDrawGizmos()
    {
        if (!showInSceneView) return;
        
        DrawWalkArea();
    }

    private void OnDrawGizmosSelected()
    {
        // 当选中此对象时总是显示区域
        DrawWalkArea();
    }

    private void DrawWalkArea()
    {
        Rect walkRect = LevelData.WalkRect;
        
        // 绘制填充区域
        Gizmos.color = fillColor;
        Vector3 center = new Vector3(walkRect.center.x, walkRect.center.y, 0);
        Vector3 size = new Vector3(walkRect.width, walkRect.height, 0.1f);
        Gizmos.DrawCube(center, size);
        
        // 绘制边界线
        Gizmos.color = boundaryColor;
        
        // 四个角的坐标
        Vector3 bottomLeft = new Vector3(walkRect.xMin, walkRect.yMin, 0);
        Vector3 bottomRight = new Vector3(walkRect.xMax, walkRect.yMin, 0);
        Vector3 topLeft = new Vector3(walkRect.xMin, walkRect.yMax, 0);
        Vector3 topRight = new Vector3(walkRect.xMax, walkRect.yMax, 0);
        
        // 绘制四条边
        Gizmos.DrawLine(bottomLeft, bottomRight);  // 底边
        Gizmos.DrawLine(bottomRight, topRight);    // 右边
        Gizmos.DrawLine(topRight, topLeft);        // 顶边
        Gizmos.DrawLine(topLeft, bottomLeft);      // 左边
        
        // 在中心显示区域信息
        Vector3 labelPos = center + Vector3.up * 0.5f;
        
#if UNITY_EDITOR
        UnityEditor.Handles.color = boundaryColor;
        UnityEditor.Handles.Label(labelPos, $"行走区域\n{walkRect.width:F1} x {walkRect.height:F1}");
#endif
    }

    private void OnGUI()
    {
        if (!showInGameView) return;
        
        // 在Game视图中显示区域边界
        DrawWalkAreaInGameView();
    }

    private void DrawWalkAreaInGameView()
    {
        Rect walkRect = LevelData.WalkRect;
        
        // 将世界坐标转换为屏幕坐标
        Vector3 bottomLeft = Camera.main.WorldToScreenPoint(new Vector3(walkRect.xMin, walkRect.yMin, 0));
        Vector3 bottomRight = Camera.main.WorldToScreenPoint(new Vector3(walkRect.xMax, walkRect.yMin, 0));
        Vector3 topLeft = Camera.main.WorldToScreenPoint(new Vector3(walkRect.xMin, walkRect.yMax, 0));
        Vector3 topRight = Camera.main.WorldToScreenPoint(new Vector3(walkRect.xMax, walkRect.yMax, 0));
        
        // Unity的屏幕坐标Y轴是反的，需要转换
        bottomLeft.y = Screen.height - bottomLeft.y;
        bottomRight.y = Screen.height - bottomRight.y;
        topLeft.y = Screen.height - topLeft.y;
        topRight.y = Screen.height - topRight.y;
        
        // 创建材质用于绘制线条
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        }
        
        // 绘制边界线
        GL.PushMatrix();
        lineMaterial.SetPass(0);
        GL.LoadPixelMatrix();
        GL.Begin(GL.LINES);
        GL.Color(boundaryColor);
        
        // 底边
        GL.Vertex3(bottomLeft.x, bottomLeft.y, 0);
        GL.Vertex3(bottomRight.x, bottomRight.y, 0);
        
        // 右边
        GL.Vertex3(bottomRight.x, bottomRight.y, 0);
        GL.Vertex3(topRight.x, topRight.y, 0);
        
        // 顶边
        GL.Vertex3(topRight.x, topRight.y, 0);
        GL.Vertex3(topLeft.x, topLeft.y, 0);
        
        // 左边
        GL.Vertex3(topLeft.x, topLeft.y, 0);
        GL.Vertex3(bottomLeft.x, bottomLeft.y, 0);
        
        GL.End();
        GL.PopMatrix();
    }

    private Material lineMaterial;

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            DestroyImmediate(lineMaterial);
        }
    }

    /// <summary>
    /// 在Inspector中提供的便捷方法，用于调整行走区域
    /// </summary>
    [ContextMenu("调整行走区域到当前位置")]
    public void AdjustWalkAreaToCurrentPosition()
    {
        // 这个方法可以在Inspector中右键点击组件标题时调用
        // 将当前对象的位置作为行走区域的中心
        Vector3 currentPos = transform.position;
        
        Debug.Log($"当前位置: {currentPos}");
        Debug.Log($"当前行走区域: {LevelData.WalkRect}");
        Debug.Log("请在LevelData.cs中手动调整WalkRect的值");
    }

    /// <summary>
    /// 检查指定位置是否在行走区域内
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <returns>是否在区域内</returns>
    public bool IsPositionInWalkArea(Vector3 position)
    {
        return LevelData.IsPositionValid(position);
    }

    /// <summary>
    /// 获取最近的有效位置
    /// </summary>
    /// <param name="position">原始位置</param>
    /// <returns>限制后的有效位置</returns>
    public Vector3 GetNearestValidPosition(Vector3 position)
    {
        return LevelData.ClampPosition(position);
    }
}
