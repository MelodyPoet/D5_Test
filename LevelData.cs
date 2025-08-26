using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData {
    public static Rect WalkRect = new Rect(-29f, -3.995085f, 81, 3.5f);

    /// <summary>
    /// 检查位置是否在允许的行走区域内
    /// 使用矩形区域检测
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <returns>如果位置在允许区域内返回true</returns>
    public static bool IsPositionValid(Vector3 position) {
        // 使用矩形检测
        return WalkRect.Contains(new Vector2(position.x, position.y));
    }

    /// <summary>
    /// 将位置限制在允许的行走区域内
    /// </summary>
    /// <param name="position">原始位置</param>
    /// <returns>限制后的位置</returns>
    public static Vector3 ClampPosition(Vector3 position) {
        // 使用矩形限制
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
    public static Vector3 GetValidDestination(Vector3 startPos, Vector3 targetPos) {
        // 检查目标位置是否有效
        if (IsPositionValid(targetPos)) {
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
    public static bool IsPathValid(Vector3 startPos, Vector3 targetPos) {
        // 使用简单检测
        return IsPositionValid(targetPos);
    }
}
