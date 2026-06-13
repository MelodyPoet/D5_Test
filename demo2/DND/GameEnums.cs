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
    ///
    /// 分层结构：
    ///   内层（基础装饰层，固定身体部位，仅服务于外观）：
    ///     SkinBase - 默认基础身体（躯干+四肢基础服装+五官皮肤）
    ///     Hair - 头发（仅外观）
    ///     Eyes - 眼睛（仅外观）
    ///     Mouth - 嘴（仅外观）
    ///
    ///   外层（装备外观层，与游戏逻辑关联 — 物品共生/拼接专长）：
    ///     Helmet - 头盔（覆盖头部+头发+眼睛+嘴）
    ///     Armor - 躯干护甲
    ///     Gloves - 护腕/手套
    ///     Boots - 靴子
    ///     Belt - 腰带
    ///     Cloak - 披风
    ///     MainHandWeapon - 主手武器
    ///     OffHandShield - 副手盾牌
    ///     OffHandWeapon - 副手武器
    ///
    ///   特殊：
    ///     FullSkin - 全身套装（整套替换，无需组合）
    ///
    /// 注意：以下旧枚举值保留用于向后兼容，新代码不应使用：
    ///   Clothes, Legs, Eyelids, Nose, Accessory
    /// </summary>
    public enum SkinBodyPartType {
        // === 内层：基础装饰层（仅外观） ===
        SkinBase,       // 基础身体（躯干+四肢基础服装+五官皮肤，Layer 1 基准）
        Hair,           // 头发（仅外观）
        Eyes,           // 眼睛（仅外观）
        Mouth,          // 嘴（仅外观）

        // === 外层：装备外观层（与游戏逻辑关联） ===
        Helmet,         // 头盔（覆盖头部区域，包含脸/眼睛/鼻子）
        Armor,          // 躯干护甲
        Gloves,         // 护腕/手套
        Boots,          // 靴子
        Belt,           // 腰带
        Cloak,          // 披风
        MainHandWeapon, // 主手武器
        OffHandShield,  // 副手盾牌
        OffHandWeapon,  // 副手武器

        // === 特殊 ===
        FullSkin,       // 全身套装（整套替换，无需组合）

        // === 向后兼容（已废弃，新代码不应使用） ===
        [System.Obsolete("请使用独立的装备部位枚举值")]
        Clothes,        // 衣服（已废弃）
        [System.Obsolete("请使用独立的装备部位枚举值")]
        Legs,           // 腿部（已废弃）
        [System.Obsolete("请使用独立的装备部位枚举值")]
        Eyelids,        // 眼皮（已废弃，合并到五官皮肤中）
        [System.Obsolete("请使用独立的装备部位枚举值")]
        Nose,           // 鼻子（已废弃，合并到五官皮肤中）
        [System.Obsolete("请使用独立的装备部位枚举值")]
        Accessory,      // 配饰（已废弃）
    }

    /// <summary>
    /// 装备外观行为枚举
    /// 定义装备穿戴后对角色外观的影响方式：
    /// - None：无外观影响（戒指、项链等）
    /// - Cover：覆盖型（头盔/铠甲等，完全替换对应身体部位）
    /// - Overlay：叠加型（头环/王冠等，在现有外观上层叠加显示）
    /// </summary>
    public enum EquipmentAppearanceBehavior {
        None,    // 无外观影响
        Cover,   // 覆盖型
        Overlay  // 叠加型
    }
}
