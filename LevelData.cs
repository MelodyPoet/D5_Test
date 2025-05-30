using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData
{
    public static Rect WalkRect = new Rect(-29f, -3.995085f, 81, 3.5f);

    /// <summary>
    /// 检查位置是否在允许的行走区域内
    /// 优先使用物理检测，如果没有物理验证器则回退到矩形检测
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <returns>如果位置在允许区域内返回true</returns>
    public static bool IsPositionValid(Vector3 position)
    {
        // 优先使用物理检测
        if (PhysicsMovementValidator.Instance != null)
        {
            return PhysicsMovementValidator.Instance.IsPositionValid(position);
        }

        // 回退到矩形检测
        return WalkRect.Contains(new Vector2(position.x, position.y));
    }

    /// <summary>
    /// 将位置限制在允许的行走区域内
    /// </summary>
    /// <param name="position">原始位置</param>
    /// <returns>限制后的位置</returns>
    public static Vector3 ClampPosition(Vector3 position)
    {
        // 优先使用物理检测
        if (PhysicsMovementValidator.Instance != null)
        {
            return PhysicsMovementValidator.Instance.GetNearestValidPosition(position, position);
        }

        // 回退到矩形限制
        Vector3 clampedPos = position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, WalkRect.xMin, WalkRect.xMax);
        clampedPos.y = Mathf.Clamp(clampedPos.y, WalkRect.yMin, WalkRect.yMax);
        return clampedPos;
    }

    /// <summary>
    /// 获取从起点到终点的有效路径，如果终点超出边界则调整到边界内
    /// </summary>
    /// <param name="startPos">起始位置</param>
    /// <param name="targetPos">目标位置</param>
    /// <returns>调整后的有效目标位置</returns>
    public static Vector3 GetValidDestination(Vector3 startPos, Vector3 targetPos)
    {
        // 优先使用物理检测
        if (PhysicsMovementValidator.Instance != null)
        {
            // 检查路径是否有效
            if (PhysicsMovementValidator.Instance.IsPathValid(startPos, targetPos))
            {
                return targetPos;
            }

            // 如果路径无效，获取最远的有效位置
            return PhysicsMovementValidator.Instance.GetFarthestValidPosition(startPos, targetPos);
        }

        // 回退到矩形检测
        if (IsPositionValid(targetPos))
        {
            return targetPos;
        }

        // 如果目标位置超出边界，将其限制在边界内
        Vector3 clampedTarget = ClampPosition(targetPos);
        return clampedTarget;
    }

    /// <summary>
    /// 检查从起点到终点的路径是否有效（无阻挡）
    /// </summary>
    /// <param name="startPos">起始位置</param>
    /// <param name="targetPos">目标位置</param>
    /// <returns>如果路径有效返回true</returns>
    public static bool IsPathValid(Vector3 startPos, Vector3 targetPos)
    {
        // 优先使用物理检测
        if (PhysicsMovementValidator.Instance != null)
        {
            return PhysicsMovementValidator.Instance.IsPathValid(startPos, targetPos);
        }

        // 回退到简单检测
        return IsPositionValid(targetPos);
    }
}
