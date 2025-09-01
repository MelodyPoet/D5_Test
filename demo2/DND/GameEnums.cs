using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏核心枚举定义
/// </summary>

public enum CharacterClass {
    Fighter,    // 战士
    Wizard,     // 法师
    Rogue,      // 盗贼
    Cleric,     // 牧师
    Ranger,     // 游侠
    Barbarian,  // 野蛮人
    Paladin,    // 圣骑士
    Warlock,    // 术士
    Sorcerer,   // 法师
    Bard,       // 诗人
    Druid,      // 德鲁伊
    Monk        // 武僧
}

/// <summary>
/// 状态效果类型枚举
/// </summary>
public enum StatusEffectType {
    None,           // 无状态
    Poisoned,       // 中毒
    Paralyzed,      // 麻痹
    Stunned,        // 昏迷
    Charmed,        // 魅惑
    Frightened,     // 恐惧
    Unconscious,    // 失去意识
    Dodging,        // 闪避状态
    Blessed,        // 祝福
    Cursed,         // 诅咒
    Burning,        // 燃烧
    Frozen,         // 冰冻
    Bleeding        // 流血
}

/// <summary>
/// 伤害类型枚举
/// </summary>
public enum DamageType {
    Bludgeoning,    // 钝击伤害
    Piercing,       // 穿刺伤害
    Slashing,       // 挥砍伤害
    Fire,           // 火焰伤害
    Cold,           // 寒冷伤害
    Lightning,      // 闪电伤害
    Thunder,        // 雷鸣伤害
    Acid,           // 强酸伤害
    Poison,         // 毒素伤害
    Psychic,        // 心灵伤害
    Necrotic,       // 坏死伤害
    Radiant,        // 光辉伤害
    Force           // 力场伤害
}

/// <summary>
/// 技能枚举
/// </summary>
public enum Skill {
    Acrobatics,     // 杂技
    AnimalHandling, // 驯兽
    Arcana,         // 奥术学识
    Athletics,      // 运动
    Deception,      // 欺骗
    History,        // 历史
    Insight,        // 洞察
    Intimidation,   // 威吓
    Investigation,  // 调查
    Medicine,       // 医疗
    Nature,         // 自然
    Perception,     // 察觉
    Performance,    // 表演
    Persuasion,     // 说服
    Religion,       // 宗教
    SleightOfHand,  // 巧手
    Stealth,        // 潜行
    Survival        // 生存
}

/// <summary>
/// 战斗阵营枚举
/// </summary>
public enum BattleSide {
    Player,  // 玩家阵营
    Enemy,   // 敌人阵营
    Neutral  // 中立阵营
}

/// <summary>
/// 法术数据 - 使用ScriptableObject存储
/// </summary>
[CreateAssetMenu(fileName = "New Spell", menuName = "DND/Spell")]
public class SpellData : ScriptableObject {
    [Header("法术基本信息")]
    public string spellName = "法术名称";
    public int level = 1;
    public string description = "法术描述";

    [Header("法术效果")]
    public bool dealsDamage = true;
    public bool heals = false;
    public int baseDamage = 8;
    public DamageType damageType = DamageType.Fire;
    public int areaOfEffect = 0; // 0表示单体，>0表示范围

    [Header("法术消耗")]
    public int manaCost = 1;
}
