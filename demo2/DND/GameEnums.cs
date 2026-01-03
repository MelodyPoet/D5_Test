﻿using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND
{
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
    /// 装备槽位枚举（集中定义，供 CharacterEquipment 等使用）
    /// 按项目文档要求统一在 GameEnums 中定义。
    /// </summary>
    public enum EquipmentSlot {
        MainHand,   // 主手（武器/双手/单手）
        OffHand,    // 副手（单手武器/盾牌）
        Armor,      // 身体护甲
        Helmet,     // 头盔
        Gauntlets,  // 护手
        Boots,      // 靴子
        Necklace,   // 项链
        Ring1,      // 戒指槽1
        Ring2,      // 戒指槽2
        Belt,       // 腰带
        Cloak       // 披风
    }

    /// <summary>
    /// 护甲类别（用于 5e AC 计算）
    /// </summary>
    public enum ArmorType {
        Light,
        Medium,
        Heavy
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
    /// 默认普通攻击方式（角色模板用）
    /// </summary>
    public enum DefaultAttackType {
        Physical,   // 物理普通攻击（后续由装备系统决定命中属性）
        Spell       // 法术普通攻击（施法职业：命中按职业主属性）
    }

    // 物理普通攻击命中能力的选择模式（供武器/模板配置使用）
    public enum PhysicalHitAbilityMode {
        Strength,       // 固定使用 力量
        Dexterity,      // 固定使用 敏捷
        BestOfStrDex    // 取 力量/敏捷 中较大者
    }

    // 注：DiceFormula 已作为独立的 class 定义在 DiceFormula.cs 中

    /// <summary>
    /// 随施法者等级成长的额外加骰配置（当施法者等级 >= 指定等级时，额外+N个骰子）
    /// 例如：{5级 +1d, 11级 +1d, 17级 +1d} 用于戏法的标准成长。
    /// </summary>
    [System.Serializable]
    public class DiceScalingStep {
        [Min(1)]
        [Tooltip("施法者等级阈值（达到或超过时生效）")]
        public int casterLevelThreshold = 5;
        [Min(1)]
        [Tooltip("额外增加的骰子个数（与基础骰子同面数）")]
        public int addDice = 1;
    }

    // 注：SpellData 已作为独立的 ScriptableObject 定义在 SpellData.cs 中

    /// <summary>
    /// 角色换装系统 - 身体部件类型枚举
    /// </summary>
    public enum SkinBodyPartType {
        SkinBase,  // 基础皮肤（只有人脸和裸手臂，作为其他部件的基础层）
        Hair,      // 头发
        Clothes,   // 衣服
        Legs,      // 腿部
        Eyes,      // 眼睛
        Eyelids,   // 眼皮
        Nose,      // 鼻子
        Accessory, // 配饰
        FullSkin   // 全身套装（整套替换，无需组合）
    }
}
