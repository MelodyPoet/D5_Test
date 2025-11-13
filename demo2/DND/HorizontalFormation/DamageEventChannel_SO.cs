using UnityEngine;
using demo2.DND.Core.Events;
using demo2.DND.Core.Events.Data;
using System;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 伤害事件通道 - 用于解耦伤害系统
    /// 发布者：HorizontalCombatRules
    /// 订阅者：CharacterStats（受击动画）、UI系统（血条更新）等
    /// 支持暴击信息
    /// </summary>
    [CreateAssetMenu(fileName = "DamageEventChannel", menuName = "DND/Events/Damage Event Channel")]
    public class DamageEventChannel_SO : EventChannelSO<DamageInfo>
    {
        /// <summary>
        /// [已过时] 触发伤害事件。请改用 RaiseEvent(DamageInfo info)。
        /// 此方法为保持向后兼容而保留，将在未来移除。
        /// </summary>
        /// <param name="recipient">受伤角色</param>
        /// <param name="dealer">攻击者</param>
        /// <param name="damageAmount">伤害数值</param>
        /// <param name="isCritical">是否暴击</param>
        [Obsolete("请改用 RaiseEvent(DamageInfo info)")]
        public void RaiseEvent(CharacterStats recipient, CharacterStats dealer, int damageAmount, bool isCritical = false)
        {
            var damageInfo = new DamageInfo(recipient, dealer, damageAmount, isCritical);
            RaiseEvent(damageInfo); // 调用基类的新方法

            // 无法从子类访问基类事件的订阅列表，移除相关日志。

            if (damageAmount > 0)
            {
                string critText = isCritical ? " (暴击!)" : "";
                Debug.Log($"[伤害事件] {dealer.GetDisplayName()} 对 {recipient.GetDisplayName()} 造成 {damageAmount} 点伤害{critText}");
            }
            else
            {
                Debug.Log($"[伤害事件] {dealer.GetDisplayName()} 攻击 {recipient.GetDisplayName()} 未命中");
            }
        }
    }
}
