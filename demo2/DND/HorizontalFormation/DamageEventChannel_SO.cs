using UnityEngine;
using UnityEngine.Events;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 伤害事件通道 - 用于解耦伤害系统
    /// 发布者：HorizontalCombatRules
    /// 订阅者：CharacterStats（受击动画）、UI系统（血条更新）等
    /// 支持暴击信息
    /// </summary>
    [CreateAssetMenu(fileName = "DamageEventChannel", menuName = "DND/Events/Damage Event Channel")]
    public class DamageEventChannel_SO : ScriptableObject
    {
        /// <summary>
        /// 伤害事件：受伤角色、攻击者、伤害数值、是否暴击
        /// </summary>
        public UnityAction<CharacterStats, CharacterStats, int, bool> OnEventRaised;

        /// <summary>
        /// 触发伤害事件
        /// </summary>
        /// <param name="recipient">受伤角色</param>
        /// <param name="dealer">攻击者</param>
        /// <param name="damageAmount">伤害数值</param>
        /// <param name="isCritical">是否暴击</param>
        public void RaiseEvent(CharacterStats recipient, CharacterStats dealer, int damageAmount, bool isCritical = false)
        {
            OnEventRaised?.Invoke(recipient, dealer, damageAmount, isCritical);

            // 输出调试信息
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

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe(UnityAction<CharacterStats, CharacterStats, int, bool> callback)
        {
            OnEventRaised += callback;
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe(UnityAction<CharacterStats, CharacterStats, int, bool> callback)
        {
            OnEventRaised -= callback;
        }

        /// <summary>
        /// 清理所有订阅
        /// </summary>
        private void OnDisable()
        {
            OnEventRaised = null;
        }
    }
}
