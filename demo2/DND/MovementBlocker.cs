using UnityEngine;

/// <summary>
/// 移动阻挡器组件
/// 用于标记阻挡角色移动的区域或物体
/// </summary>
public class MovementBlocker : MonoBehaviour
{
    [Header("阻挡设置")]
    [Tooltip("阻挡类型")]
    public BlockerType blockerType = BlockerType.Wall;

    [Tooltip("是否显示阻挡区域的可视化")]
    public bool showVisualization = true;

    [Tooltip("可视化颜色")]
    public Color visualizationColor = new Color(1f, 0f, 0f, 0.3f);

    [Tooltip("阻挡描述")]
    public string description = "移动阻挡区域";

    public enum BlockerType
    {
        Wall,           // 墙壁
        Boundary,       // 边界
        Obstacle,       // 障碍物
        DeathZone,      // 危险区域
        CustomArea      // 自定义区域
    }

    private void Awake()
    {
        // 确保有碰撞体
        if (GetComponent<Collider2D>() == null && GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"MovementBlocker '{gameObject.name}' 没有碰撞体组件！请添加Collider2D或Collider组件。");
        }

        // 设置为触发器（如果需要）
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            col2D.isTrigger = false; // 阻挡器不应该是触发器
        }

        Collider col3D = GetComponent<Collider>();
        if (col3D != null)
        {
            col3D.isTrigger = false; // 阻挡器不应该是触发器
        }

        // 设置Tag（标签）- 用于识别阻挡物
        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "MovementBlocker";
            Debug.Log($"为 {gameObject.name} 设置Tag为 'MovementBlocker'");
        }

        // 可选：设置Layer（层级）- 用于物理检测优化
        // 如果你创建了专用的MovementBlocker层，可以取消下面的注释
        /*
        int movementBlockerLayer = LayerMask.NameToLayer("MovementBlocker");
        if (movementBlockerLayer != -1 && gameObject.layer == 0)
        {
            gameObject.layer = movementBlockerLayer;
            Debug.Log($"为 {gameObject.name} 设置Layer为 'MovementBlocker'");
        }
        */
    }

    private void OnDrawGizmos()
    {
        if (!showVisualization) return;

        DrawVisualization();
    }

    private void OnDrawGizmosSelected()
    {
        // 选中时总是显示
        DrawVisualization();

        // 显示额外信息
        Gizmos.color = Color.yellow;
        Vector3 labelPos = transform.position + Vector3.up * 0.5f;

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(labelPos, $"{blockerType}\n{description}");
#endif
    }

    private void DrawVisualization()
    {
        Gizmos.color = visualizationColor;

        // 根据碰撞体类型绘制不同的可视化
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            DrawCollider2D(col2D);
            return;
        }

        Collider col3D = GetComponent<Collider>();
        if (col3D != null)
        {
            DrawCollider3D(col3D);
            return;
        }

        // 如果没有碰撞体，绘制默认立方体
        Gizmos.DrawCube(transform.position, Vector3.one);
    }

    private void DrawCollider2D(Collider2D collider)
    {
        if (collider is BoxCollider2D box)
        {
            Vector3 center = transform.TransformPoint(box.offset);
            Vector3 size = new Vector3(box.size.x * transform.localScale.x, box.size.y * transform.localScale.y, 0.1f);
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (collider is CircleCollider2D circle)
        {
            Vector3 center = transform.TransformPoint(circle.offset);
            float radius = circle.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
            Gizmos.DrawSphere(center, radius);
        }
        else if (collider is CapsuleCollider2D capsule)
        {
            Vector3 center = transform.TransformPoint(capsule.offset);
            Vector3 size = new Vector3(capsule.size.x * transform.localScale.x, capsule.size.y * transform.localScale.y, 0.1f);
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    private void DrawCollider3D(Collider collider)
    {
        if (collider is BoxCollider box)
        {
            Vector3 center = transform.TransformPoint(box.center);
            Vector3 size = Vector3.Scale(box.size, transform.localScale);
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (collider is SphereCollider sphere)
        {
            Vector3 center = transform.TransformPoint(sphere.center);
            float radius = sphere.radius * Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            Gizmos.DrawSphere(center, radius);
        }
        else if (collider is CapsuleCollider capsule)
        {
            Vector3 center = transform.TransformPoint(capsule.center);
            Vector3 size = new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2);
            size = Vector3.Scale(size, transform.localScale);
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    /// <summary>
    /// 检查指定位置是否被此阻挡器阻挡
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <param name="characterRadius">角色的碰撞半径</param>
    /// <returns>如果被阻挡返回true</returns>
    public bool IsPositionBlocked(Vector3 position, float characterRadius = 0.5f)
    {
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            return col2D.OverlapPoint(position);
        }

        Collider col3D = GetComponent<Collider>();
        if (col3D != null)
        {
            return col3D.bounds.Contains(position);
        }

        return false;
    }

    /// <summary>
    /// 获取阻挡器的边界
    /// </summary>
    /// <returns>阻挡器的边界</returns>
    public Bounds GetBounds()
    {
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            return col2D.bounds;
        }

        Collider col3D = GetComponent<Collider>();
        if (col3D != null)
        {
            return col3D.bounds;
        }

        // 默认边界
        return new Bounds(transform.position, Vector3.one);
    }

    /// <summary>
    /// 创建快速设置方法
    /// </summary>
    [ContextMenu("设置为墙壁阻挡")]
    public void SetAsWall()
    {
        blockerType = BlockerType.Wall;
        description = "墙壁";
        visualizationColor = new Color(0.8f, 0.4f, 0.2f, 0.5f); // 棕色
    }

    [ContextMenu("设置为边界阻挡")]
    public void SetAsBoundary()
    {
        blockerType = BlockerType.Boundary;
        description = "场景边界";
        visualizationColor = new Color(1f, 0f, 0f, 0.3f); // 红色
    }

    [ContextMenu("设置为障碍物")]
    public void SetAsObstacle()
    {
        blockerType = BlockerType.Obstacle;
        description = "障碍物";
        visualizationColor = new Color(0.5f, 0.5f, 0.5f, 0.4f); // 灰色
    }

    [ContextMenu("添加BoxCollider2D")]
    public void AddBoxCollider2D()
    {
        if (GetComponent<BoxCollider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
            Debug.Log($"已为 {gameObject.name} 添加 BoxCollider2D");
        }
    }

    [ContextMenu("添加BoxCollider")]
    public void AddBoxCollider()
    {
        if (GetComponent<BoxCollider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
            Debug.Log($"已为 {gameObject.name} 添加 BoxCollider");
        }
    }
}
