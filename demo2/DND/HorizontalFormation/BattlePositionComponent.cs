using UnityEngine;

/// <summary>
/// 角色战斗位置组件
/// 跟踪角色在横版战场中的位置信息
/// </summary>
public class BattlePositionComponent : MonoBehaviour {
    [Header("位置信息")]
    public HorizontalPosition currentPosition;
    public HorizontalPosition previousPosition;

    [Header("移动状态")]
    public bool isMoving = false;
    public float moveSpeed = 2.0f;

    private Vector3 targetWorldPosition;
    private bool hasTargetPosition = false;

    void Update() {
        // 平滑移动到目标位置
        if (hasTargetPosition && isMoving) {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetWorldPosition,
                moveSpeed * Time.deltaTime
            );

            // 检查是否到达目标位置
            if (Vector3.Distance(transform.position, targetWorldPosition) < 0.1f) {
                transform.position = targetWorldPosition;
                isMoving = false;
                hasTargetPosition = false;

                Debug.Log($"{name} 到达位置 {currentPosition}");
            }
        }
    }

    /// <summary>
    /// 设置目标世界位置
    /// </summary>
    public void SetTargetWorldPosition(Vector3 worldPos) {
        targetWorldPosition = worldPos;
        hasTargetPosition = true;
        isMoving = true;
    }

    /// <summary>
    /// 更新位置信息
    /// </summary>
    public void UpdatePosition(HorizontalPosition newPosition) {
        previousPosition = currentPosition;
        currentPosition = newPosition;

        Debug.Log($"{name} 从 {previousPosition} 移动到 {currentPosition}");
    }

    /// <summary>
    /// 获取角色所在的排
    /// </summary>
    public BattleRow GetCurrentRow() {
        return HorizontalFormationAI.GetPositionRow(currentPosition);
    }

    /// <summary>
    /// 获取角色所属阵营
    /// </summary>
    public BattleSide GetCurrentSide() {
        return HorizontalFormationAI.GetPositionSide(currentPosition);
    }    /// <summary>
         /// 检查是否在前排
         /// </summary>
    public bool IsInFrontRow() {
        BattleRow row = GetCurrentRow();
        return row == BattleRow.PlayerFront || row == BattleRow.EnemyFront;
    }

    /// <summary>
    /// 检查是否在后排
    /// </summary>
    public bool IsInBackRow() {
        BattleRow row = GetCurrentRow();
        return row == BattleRow.PlayerBack || row == BattleRow.EnemyBack;
    }
}
