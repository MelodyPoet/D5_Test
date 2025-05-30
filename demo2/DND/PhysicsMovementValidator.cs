using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 基于物理系统的移动验证器
/// 使用碰撞检测来验证移动是否有效
/// </summary>
public class PhysicsMovementValidator : MonoBehaviour
{
    [Header("检测设置")]
    [Tooltip("角色碰撞半径")]
    public float characterRadius = 0.5f;
    
    [Tooltip("检测层级（阻挡物所在的层）")]
    public LayerMask blockingLayers = -1;
    
    [Tooltip("是否使用2D物理")]
    public bool use2DPhysics = true;
    
    [Tooltip("路径检测精度（检测点数量）")]
    [Range(3, 20)]
    public int pathCheckPoints = 5;
    
    [Tooltip("是否显示调试信息")]
    public bool showDebugRays = false;

    [Header("标签检测")]
    [Tooltip("阻挡物标签")]
    public string[] blockingTags = { "MovementBlocker", "Wall", "Obstacle" };

    // 单例
    public static PhysicsMovementValidator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 检查位置是否有效（没有被阻挡）
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <param name="characterCollider">角色的碰撞体（可选）</param>
    /// <returns>如果位置有效返回true</returns>
    public bool IsPositionValid(Vector3 position, Collider2D characterCollider = null)
    {
        if (use2DPhysics)
        {
            return IsPositionValid2D(position, characterCollider);
        }
        else
        {
            return IsPositionValid3D(position);
        }
    }

    /// <summary>
    /// 2D物理检测
    /// </summary>
    private bool IsPositionValid2D(Vector3 position, Collider2D characterCollider = null)
    {
        // 方法1: 使用OverlapCircle检测
        Collider2D hit = Physics2D.OverlapCircle(position, characterRadius, blockingLayers);
        if (hit != null && IsBlockingObject(hit.gameObject))
        {
            if (showDebugRays)
            {
                Debug.DrawLine(position, hit.transform.position, Color.red, 0.1f);
            }
            return false;
        }

        // 方法2: 如果有角色碰撞体，使用更精确的检测
        if (characterCollider != null)
        {
            // 临时移动碰撞体到目标位置进行检测
            Vector3 originalPos = characterCollider.transform.position;
            characterCollider.transform.position = position;
            
            // 检测重叠
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(blockingLayers);
            filter.useTriggers = false;
            
            List<Collider2D> results = new List<Collider2D>();
            int hitCount = characterCollider.OverlapCollider(filter, results);
            
            // 恢复原始位置
            characterCollider.transform.position = originalPos;
            
            // 检查是否有阻挡物
            foreach (var result in results)
            {
                if (IsBlockingObject(result.gameObject))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 3D物理检测
    /// </summary>
    private bool IsPositionValid3D(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, characterRadius, blockingLayers);
        
        foreach (var hit in hits)
        {
            if (IsBlockingObject(hit.gameObject))
            {
                if (showDebugRays)
                {
                    Debug.DrawLine(position, hit.transform.position, Color.red, 0.1f);
                }
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// 检查从起点到终点的路径是否有效
    /// </summary>
    /// <param name="startPos">起始位置</param>
    /// <param name="endPos">目标位置</param>
    /// <param name="characterCollider">角色碰撞体</param>
    /// <returns>如果路径有效返回true</returns>
    public bool IsPathValid(Vector3 startPos, Vector3 endPos, Collider2D characterCollider = null)
    {
        // 检查终点是否有效
        if (!IsPositionValid(endPos, characterCollider))
        {
            return false;
        }

        // 沿路径检查多个点
        for (int i = 1; i <= pathCheckPoints; i++)
        {
            float t = (float)i / pathCheckPoints;
            Vector3 checkPos = Vector3.Lerp(startPos, endPos, t);
            
            if (!IsPositionValid(checkPos, characterCollider))
            {
                if (showDebugRays)
                {
                    Debug.DrawLine(startPos, checkPos, Color.red, 1f);
                }
                return false;
            }
        }

        if (showDebugRays)
        {
            Debug.DrawLine(startPos, endPos, Color.green, 1f);
        }

        return true;
    }

    /// <summary>
    /// 获取最近的有效位置
    /// </summary>
    /// <param name="targetPos">目标位置</param>
    /// <param name="startPos">起始位置</param>
    /// <param name="characterCollider">角色碰撞体</param>
    /// <returns>最近的有效位置</returns>
    public Vector3 GetNearestValidPosition(Vector3 targetPos, Vector3 startPos, Collider2D characterCollider = null)
    {
        // 如果目标位置本身就有效，直接返回
        if (IsPositionValid(targetPos, characterCollider))
        {
            return targetPos;
        }

        // 尝试在目标位置周围找到有效位置
        float searchRadius = 1f;
        int searchSteps = 8;
        
        for (int radius = 1; radius <= 5; radius++)
        {
            searchRadius = radius * 0.5f;
            
            for (int i = 0; i < searchSteps; i++)
            {
                float angle = (float)i / searchSteps * 360f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * searchRadius;
                Vector3 testPos = targetPos + offset;
                
                if (IsPositionValid(testPos, characterCollider))
                {
                    return testPos;
                }
            }
        }

        // 如果找不到有效位置，返回起始位置
        return startPos;
    }

    /// <summary>
    /// 获取沿路径的最远有效位置
    /// </summary>
    /// <param name="startPos">起始位置</param>
    /// <param name="targetPos">目标位置</param>
    /// <param name="characterCollider">角色碰撞体</param>
    /// <returns>最远的有效位置</returns>
    public Vector3 GetFarthestValidPosition(Vector3 startPos, Vector3 targetPos, Collider2D characterCollider = null)
    {
        Vector3 direction = (targetPos - startPos).normalized;
        float maxDistance = Vector3.Distance(startPos, targetPos);
        
        // 二分查找最远有效距离
        float minDistance = 0f;
        float testDistance = maxDistance;
        Vector3 lastValidPos = startPos;
        
        for (int i = 0; i < 10; i++) // 最多10次迭代
        {
            Vector3 testPos = startPos + direction * testDistance;
            
            if (IsPositionValid(testPos, characterCollider))
            {
                lastValidPos = testPos;
                minDistance = testDistance;
                testDistance = (testDistance + maxDistance) / 2f;
            }
            else
            {
                maxDistance = testDistance;
                testDistance = (minDistance + testDistance) / 2f;
            }
            
            if (maxDistance - minDistance < 0.1f)
            {
                break;
            }
        }
        
        return lastValidPos;
    }

    /// <summary>
    /// 检查游戏对象是否是阻挡物
    /// </summary>
    /// <param name="obj">要检查的游戏对象</param>
    /// <returns>如果是阻挡物返回true</returns>
    private bool IsBlockingObject(GameObject obj)
    {
        // 检查标签
        foreach (string tag in blockingTags)
        {
            if (obj.CompareTag(tag))
            {
                return true;
            }
        }
        
        // 检查是否有MovementBlocker组件
        return obj.GetComponent<MovementBlocker>() != null;
    }

    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showDebugRays) return;
        
        // 绘制检测半径
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, characterRadius);
    }

    /// <summary>
    /// 设置角色碰撞半径
    /// </summary>
    /// <param name="radius">新的半径</param>
    public void SetCharacterRadius(float radius)
    {
        characterRadius = Mathf.Max(0.1f, radius);
    }

    /// <summary>
    /// 添加阻挡标签
    /// </summary>
    /// <param name="tag">要添加的标签</param>
    public void AddBlockingTag(string tag)
    {
        if (System.Array.IndexOf(blockingTags, tag) == -1)
        {
            System.Array.Resize(ref blockingTags, blockingTags.Length + 1);
            blockingTags[blockingTags.Length - 1] = tag;
        }
    }
}
