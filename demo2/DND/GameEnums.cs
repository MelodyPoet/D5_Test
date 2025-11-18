using System.Collections.Generic;
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

    /// <summary>
    /// 骰子公式：X d Y（本项目的法术伤害统一使用骰子表达，不使用固定数值）
    /// </summary>
    [System.Serializable]
    public struct DiceFormula {
        [Tooltip("骰子个数 X，如 1d6 则为 1")]
        public int diceCount;
        [Tooltip("骰子面数 Y，如 1d6 则为 6")]
        public int diceSize;
    }

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

    /// <summary>
    /// 法术数据 - 使用ScriptableObject存储
    /// 伤害：使用骰子表达，并可随施法者等级增加额外骰子。
    /// 消耗：无 mana 概念；Level 0（戏法）无限施法；Level 1+ 受法术位限制（不在本数据内统计）。
    /// </summary>
    [CreateAssetMenu(fileName = "New Spell", menuName = "DND/Spell")]
    public class SpellData : ScriptableObject {
        [Header("法术基本信息")]
        public string spellName = "法术名称";
        [Tooltip("法术环位：0=戏法（无限施法），>=1 为法术，受法术位限制")]
        [Min(0)]
        public int level = 0;
        [TextArea]
        public string description = "法术描述";

        [Header("法术效果")]
        [Tooltip("该法术是否造成伤害")]
        public bool dealsDamage = true;
        [Tooltip("该法术是否治疗（若为治疗类可在其他系统中读取），与伤害互斥由上层控制")]
        public bool heals = false;

        [Tooltip("伤害骰（基础）：例如 1d8、2d6 等")]
        public DiceFormula damageDice = new DiceFormula { diceCount = 1, diceSize = 8 };
        [Tooltip("伤害类型")]
        public DamageType damageType = DamageType.Fire;
        [Tooltip("范围：0=单体，>0=半径或自定义单位，由上层解释")]
        public int areaOfEffect = 0;

        [Header("等级成长（随施法者等级）")]
        [Tooltip("当施法者等级达到阈值时，额外增加若干骰子（同面数）\n示例：5级+1d，11级+1d，17级+1d（戏法标准成长）")]
        public List<DiceScalingStep> scalingByCasterLevel = new List<DiceScalingStep>();

        /// <summary>
        /// 计算在给定施法者等级下的总伤害骰（不含任何属性加值与易伤/抗性等外部修正）。
        /// </summary>
        public DiceFormula GetDamageDiceAtCasterLevel(int casterLevel) {
            int extra = 0;
            if (scalingByCasterLevel != null) {
                for (int i = 0; i < scalingByCasterLevel.Count; i++) {
                    var step = scalingByCasterLevel[i];
                    if (casterLevel >= step.casterLevelThreshold) extra += Mathf.Max(0, step.addDice);
                }
            }
            return new DiceFormula {
                diceCount = Mathf.Max(1, damageDice.diceCount + extra),
                diceSize = Mathf.Max(2, damageDice.diceSize)
            };
        }
    }
}
