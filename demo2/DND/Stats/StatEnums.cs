// Minimal enums for the modifier system
namespace demo2.DND.Stats
{
    // 目标属性类型（可扩展）
    public enum StatType
    {
        Strength,
        Dexterity,
        Constitution,
        Intelligence,
        Wisdom,
        Charisma,
        ArmorClass,
        MaxHitPoints,
        ProficiencyBonus,
        // 后续：SavingThrow_Str/Dex/...、AttackBonus、DamageBonus、Speed 等
    }

    // 修正运算
    public enum ModifierOp
    {
        Add,
        Multiply,
        Override,
        Flag
    }

    // 修正生效层
    public enum ModifierLayer
    {
        Permanent,    // 等级/专长/种族/职业特性等长期项
        Equipment,    // 装备项（WhileEquipped）
        Effect,       // 临时效果（BUFF/DEBUFF）
        Situational   // 情境（本轮/本动作）
    }

    // 持续类型
    public enum DurationType
    {
        Instant,
        TimedSeconds,
        TimedRounds,
        WhileEquipped,
        WhileConcentrating,
        UntilRest
    }

    // 同栈策略（同一 stackKey 冲突时处理方式）
    public enum StackPolicy
    {
        Sum,
        Max,
        Min,
        Replace
    }
}
