using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 先攻条目数据结构
    /// 用于存储角色的先攻检定结果
    /// </summary>
    [System.Serializable]
    public class InitiativeEntry {
        [Header("先攻数据")]
        public CharacterStats character;     // 角色数据
        public int initiativeValue;          // 先攻值 (1d20 + 敏捷调整值)
        public int initiativeRoll;           // 先攻检定骰子结果 (兼容旧代码)

        [Header("回合状态")]
        public bool hasActedThisRound;      // 本轮是否已行动
        public bool isDelayingAction;       // 是否延迟行动
        public int delayedInitiative;       // 延迟后的先攻值

        /// <summary>
        /// 默认构造函数（Unity序列化需要）
        /// </summary>
        public InitiativeEntry() {
            character = null;
            initiativeValue = 0;
            initiativeRoll = 0;
            ResetTurnState();
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public InitiativeEntry(CharacterStats character, int initiative) {
            this.character = character;
            this.initiativeValue = initiative;
            this.initiativeRoll = initiative; // 兼容性
            ResetTurnState();
        }

        /// <summary>
        /// 重置回合状态
        /// </summary>
        public void ResetTurnState() {
            hasActedThisRound = false;
            isDelayingAction = false;
            delayedInitiative = initiativeValue;
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
        /// 延迟行动到指定先攻值
        /// </summary>
        public void DelayAction(int newInitiative) {
            isDelayingAction = true;
            delayedInitiative = newInitiative;
        }

        /// <summary>
        /// 获取当前有效的先攻值
        /// </summary>
        public int GetEffectiveInitiative() {
            return isDelayingAction ? delayedInitiative : initiativeValue;
        }

        /// <summary>
        /// 获取显示信息
        /// </summary>
        public string GetDisplayInfo() {
            string status = hasActedThisRound ? "已行动" : "待行动";
            if (isDelayingAction) {
                status += $" (延迟至{delayedInitiative})";
            }
            return $"{character?.GetDisplayName() ?? "未知"} - 先攻:{GetEffectiveInitiative()} - {status}";
        }
    }
}
