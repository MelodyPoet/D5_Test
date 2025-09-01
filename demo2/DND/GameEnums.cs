using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色职业枚举
/// </summary>
public enum CharacterClass {
    Fighter,    // 战士
    Rogue,      // 盗贼
    Wizard,     // 法师
    Cleric,     // 牧师
    Ranger,     // 游侠
    Paladin,    // 圣武士
    Barbarian,  // 野蛮人
    Bard,       // 吟游诗人
    Druid,      // 德鲁伊
    Monk,       // 武僧
    Sorcerer,   // 术士
    Warlock     // 契术师
}

/// <summary>
/// 伤害类型枚举
/// </summary>
public enum DamageType {
    Slashing,   // 挥砍
    Piercing,   // 穿刺
    Bludgeoning,// 钝击
    Fire,       // 火焰
    Cold,       // 寒冷
    Lightning,  // 闪电
    Poison,     // 毒素
    Acid,       // 酸性
    Necrotic,   // 暗蚀
    Radiant,    // 光耀
    Psychic,    // 心灵
    Force,      // 力场
    Thunder     // 雷鸣
}

/// <summary>
/// 状态效果类型枚举
/// </summary>
public enum StatusEffectType {
    Blinded,        // 目盲
    Charmed,        // 魅惑
    Deafened,       // 耳聋
    Frightened,     // 恐慌
    Grappled,       // 擒抱
    Incapacitated,  // 失能
    Invisible,      // 隐形
    Paralyzed,      // 麻痹
    Petrified,      // 石化
    Poisoned,       // 中毒
    Prone,          // 倒地
    Restrained,     // 束缚
    Stunned,        // 震慑
    Unconscious,    // 昏迷
    Dodging         // 闪避中
}

/// <summary>
/// 战斗阵营枚举
/// </summary>
public enum BattleSide {
    Player,    // 玩家方
    Enemy      // 敌人方
}

/// <summary>
/// 技能枚举
/// </summary>
public enum Skill {
    Athletics,      // 运动 (力量)
    Acrobatics,     // 杂技 (敏捷)
    SleightOfHand,  // 手上功夫 (敏捷)
    Stealth,        // 隐匿 (敏捷)
    Arcana,         // 奥秘 (智力)
    History,        // 历史 (智力)
    Investigation,  // 调查 (智力)
    Nature,         // 自然 (智力)
    Religion,       // 宗教 (智力)
    AnimalHandling, // 驯兽 (感知)
    Insight,        // 洞悉 (感知)
    Medicine,       // 医疗 (感知)
    Perception,     // 察觉 (感知)
    Survival,       // 生存 (感知)
    Deception,      // 欺瞒 (魅力)
    Intimidation,   // 威吓 (魅力)
    Performance,    // 表演 (魅力)
    Persuasion      // 说服 (魅力)
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
