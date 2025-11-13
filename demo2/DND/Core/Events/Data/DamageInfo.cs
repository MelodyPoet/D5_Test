using demo2.DND.HorizontalFormation;

namespace demo2.DND.Core.Events.Data
{
    /// <summary>
    /// 封装一次伤害事件所需的所有信息。
    /// </summary>
    public struct DamageInfo
    {
        public readonly CharacterStats Recipient; // 受伤角色
        public readonly CharacterStats Dealer;    // 攻击者
        public readonly int DamageAmount;                                     // 伤害数值
        public readonly bool IsCritical;                                      // 是否暴击

        public DamageInfo(CharacterStats recipient, CharacterStats dealer, int damageAmount, bool isCritical)
        {
            Recipient = recipient;
            Dealer = dealer;
            DamageAmount = damageAmount;
            IsCritical = isCritical;
        }
    }
}

