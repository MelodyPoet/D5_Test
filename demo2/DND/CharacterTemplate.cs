using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色模板 - 定义角色的基础属性和配置
/// </summary>
[CreateAssetMenu(fileName = "New Character Template", menuName = "DND/Character Template")]
public class CharacterTemplate : ScriptableObject {
    [Header("基本信息")]
    public string characterName = "角色";
    public CharacterClass characterClass = CharacterClass.Fighter;
    public int level = 1;
    public BattleSide defaultSide = BattleSide.Player;

    [Header("默认普通攻击设置")]
    [Tooltip("默认普通攻击方式：Physical=物理普通攻击；Spell=法术普通攻击（命中按职业主属性）")]
    public DefaultAttackType defaultAttackType = DefaultAttackType.Physical;

    [Tooltip("职业主法术属性（用于法术普通攻击的命中）。可填：intelligence/wisdom/charisma（忽略大小写）")]
    public string primarySpellAbility = "intelligence";

    [Tooltip("施法职业默认自带的攻击型戏法（作为普通攻击使用）")]
    public SpellData defaultCantrip;

    [Header("基础属性")]
    public int strength = 10;
    public int dexterity = 10;
    public int constitution = 10;
    public int intelligence = 10;
    public int wisdom = 10;
    public int charisma = 10;

    [Header("战斗属性")]
    public int baseArmorClass = 10;
    public int hitDie = 8; // 生命骰

    [Header("抗性与免疫")]
    public List<DamageType> resistances = new List<DamageType>();
    public List<DamageType> immunities = new List<DamageType>();
    public List<DamageType> vulnerabilities = new List<DamageType>();

    [Header("技能加值")]
    public int proficiencyBonus = 2;
    public List<Skill> proficientSkills = new List<Skill>();

    [Header("豁免检定加值")]
    public List<string> proficientSaves = new List<string>();

    [Header("战斗熟练（SO 可配置）")]
    [Tooltip("是否熟练法术攻击检定（用于普通法术攻击/戏法的命中加值）")]
    public bool proficientSpellAttacks = false;
    [Tooltip("是否熟练近战武器（在未接入装备系统前，按阵位行为或调用方的 isMeleeAttack 判定使用）")]
    public bool proficientMelee = true;
    [Tooltip("是否熟练远程武器（在未接入装备系统前，按阵位行为或调用方的 isMeleeAttack 判定使用）")]
    public bool proficientRanged = false;
    [Tooltip("武器类别熟练（预留）：如 simple/martial 等；当前版本未在命中中细分使用")]
    public List<string> proficientWeaponClasses = new List<string>();
    [Tooltip("武器类型熟练（预留）：如 longsword/shortbow/finesse 等；当前版本未在命中中细分使用")]
    public List<string> proficientWeaponTypes = new List<string>();

    /// <summary>
    /// 获取熟练加值（便于外部调用保持一致）。注意：此方法基于模板上的 level，
    /// 若需要运行时等级，请调用 GetProficiencyBonusByLevel(int level)。
    /// </summary>
    public int GetProficiencyBonus() {
        return GetProficiencyBonusByLevel(level);
    }

    /// <summary>
    /// 按 DND5e 规则根据等级计算熟练加值。
    /// 1-4:+2, 5-8:+3, 9-12:+4, 13-16:+5, 17-20:+6；超出范围向下/上限钳制。
    /// </summary>
    public int GetProficiencyBonusByLevel(int lvl) {
        int l = Mathf.Max(1, lvl);
        if (l <= 4) return 2;
        if (l <= 8) return 3;
        if (l <= 12) return 4;
        if (l <= 16) return 5;
        return 6; // 17-20 及以上按 +6 处理
    }

    /// <summary>
    /// 判定本次攻击是否熟练（未接入装备系统前的最小实现）。
    /// isSpell=true 走法术普通攻击；否则按 isMelee 判定近战/远程。
    /// </summary>
    public bool IsProficientForAttack(bool isSpell, bool isMelee) {
        if (isSpell) return proficientSpellAttacks;
        return isMelee ? proficientMelee : proficientRanged;
    }

    /// <summary>
    /// 计算在指定等级下的生命值（DND5e：1级=满生命骰+体质；2级及以上每级=平均骰+体质）。
    /// </summary>
    public int CalculateHitPointsAtLevel(int lvl) {
        int conMod = (constitution - 10) / 2;
        int l = Mathf.Max(1, lvl);
        // 第1级：满生命骰 + 体质（至少1点）
        int firstLevelHp = Mathf.Max(1, hitDie + conMod);
        // 每级增加：平均骰（向上取整 = hitDie/2+1） + 体质（每级至少+1）
        int perLevelGain = Mathf.Max(1, (hitDie / 2 + 1) + conMod);
        if (l == 1) return firstLevelHp;
        int hp = firstLevelHp + (l - 1) * perLevelGain;
        return hp;
    }

    /// <summary>
    /// 计算生命值（基于模板自身 level 字段）。
    /// </summary>
    public int CalculateHitPoints() {
        return CalculateHitPointsAtLevel(level);
    }

    /// <summary>
    /// 获取技能加值（基于模板等级）。如需运行时等级，请调用重载 GetSkillBonus(skill, level)。
    /// </summary>
    public int GetSkillBonus(Skill skill) {
        return GetSkillBonus(skill, level);
    }

    /// <summary>
    /// 获取技能加值（传入用于熟练加值计算的等级）。
    /// </summary>
    public int GetSkillBonus(Skill skill, int lvl) {
        int abilityMod = GetAbilityModifierForSkill(skill);
        int bonus = abilityMod;

        // 如果精通该技能，添加熟练加值
        if (proficientSkills.Contains(skill)) {
            bonus += GetProficiencyBonusByLevel(lvl);
        }

        return bonus;
    }

    /// <summary>
    /// 获取豁免检定加值（基于模板等级）。如需运行时等级，请调用重载 GetSavingThrowBonus(ability, level)。
    /// </summary>
    public int GetSavingThrowBonus(string ability) {
        return GetSavingThrowBonus(ability, level);
    }

    /// <summary>
    /// 获取豁免检定加值（传入用于熟练加值计算的等级）。
    /// </summary>
    public int GetSavingThrowBonus(string ability, int lvl) {
        int abilityMod = GetAbilityModifier(ability);
        int bonus = abilityMod;

        // 如果精通该豁免检定，添加熟练加值
        if (proficientSaves.Contains(ability.ToLower())) {
            bonus += GetProficiencyBonusByLevel(lvl);
        }

        return bonus;
    }

    /// <summary>
    /// 获取技能对应的属性调整值
    /// </summary>
    private int GetAbilityModifierForSkill(Skill skill) {
        switch (skill) {
            case Skill.Athletics:
                return (strength - 10) / 2;
            case Skill.Acrobatics:
            case Skill.SleightOfHand:
            case Skill.Stealth:
                return (dexterity - 10) / 2;
            case Skill.Arcana:
            case Skill.History:
            case Skill.Investigation:
            case Skill.Nature:
            case Skill.Religion:
                return (intelligence - 10) / 2;
            case Skill.AnimalHandling:
            case Skill.Insight:
            case Skill.Medicine:
            case Skill.Perception:
            case Skill.Survival:
                return (wisdom - 10) / 2;
            case Skill.Deception:
            case Skill.Intimidation:
            case Skill.Performance:
            case Skill.Persuasion:
                return (charisma - 10) / 2;
            default:
                return 0;
        }
    }

    /// <summary>
    /// 获取属性调整值
    /// </summary>
    private int GetAbilityModifier(string ability) {
        switch (ability.ToLower()) {
            case "strength":
            case "str":
                return (strength - 10) / 2;
            case "dexterity":
            case "dex":
                return (dexterity - 10) / 2;
            case "constitution":
            case "con":
                return (constitution - 10) / 2;
            case "intelligence":
            case "int":
                return (intelligence - 10) / 2;
            case "wisdom":
            case "wis":
                return (wisdom - 10) / 2;
            case "charisma":
            case "cha":
                return (charisma - 10) / 2;
            default:
                return 0;
        }
    }

    /// <summary>
    /// 在编辑器中校验并规范字段，降低配置出错概率。
    /// </summary>
    private void OnValidate() {
        // 规范化 primarySpellAbility，仅允许 intelligence / wisdom / charisma
        if (string.IsNullOrWhiteSpace(primarySpellAbility)) {
            primarySpellAbility = "intelligence";
        } else {
            string val = primarySpellAbility.Trim().ToLowerInvariant();
            switch (val) {
                case "int": val = "intelligence"; break;
                case "wis": val = "wisdom"; break;
                case "cha": val = "charisma"; break;
            }
            if (val == "intelligence" || val == "wisdom" || val == "charisma") {
                primarySpellAbility = val;
            } else {
                Debug.LogWarning($"[CharacterTemplate] 不支持的 primarySpellAbility: '{primarySpellAbility}', 已回退为 intelligence（只允许 intelligence / wisdom / charisma）");
                primarySpellAbility = "intelligence";
            }
        }
    }
}
