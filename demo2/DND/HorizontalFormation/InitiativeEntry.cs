using UnityEngine;

/// <summary>
/// 先攻条目数据结构
/// 用于存储角色的先攻检定结果
/// </summary>
[System.Serializable]
public class InitiativeEntry {
    [Header("先攻数据")]
    public CharacterStats character;     // 角色数据
    public int initiative;              // 先攻值 (1d20 + 敏捷调整值)

    [Header("回合状态")]
    public bool hasActedThisRound;      // 本轮是否已行动
    public bool isDelayingAction;       // 是否延迟行动
    public int delayedInitiative;       // 延迟后的先攻值

    /// <summary>
    /// 重置回合状态
    /// </summary>
    public void ResetTurnState() {
        hasActedThisRound = false;
        isDelayingAction = false;
        delayedInitiative = initiative;
    }

    /// <summary>
    /// 标记已行动
    /// </summary>
    public void MarkAsActed() {
        hasActedThisRound = true;
    }

    /// <summary>
    /// 检查是否可以行动
    /// </summary>
    public bool CanAct() {
        return character != null &&
               character.currentHitPoints > 0 &&
               !hasActedThisRound;
    }

    /// <summary>
    /// 获取显示信息
    /// </summary>
    public string GetDisplayInfo() {
        if (character == null) return "无效角色";

        string status = hasActedThisRound ? "已行动" : "待行动";
        return $"{character.GetDisplayName()} (先攻:{initiative}, {status})";
    }
}
