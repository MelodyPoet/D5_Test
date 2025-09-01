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

    /// <summary>
    /// 计算生命值
    /// </summary>
    public int CalculateHitPoints() {
        int constitutionMod = (constitution - 10) / 2;
        return hitDie + constitutionMod + (level - 1) * (hitDie / 2 + 1 + constitutionMod);
    }

    /// <summary>
    /// 获取技能加值
    /// </summary>
    public int GetSkillBonus(Skill skill) {
        int abilityMod = GetAbilityModifierForSkill(skill);
        int bonus = abilityMod;

        // 如果精通该技能，添加熟练加值
        if (proficientSkills.Contains(skill)) {
            bonus += proficiencyBonus;
        }

        return bonus;
    }

    /// <summary>
    /// 获取豁免检定加值
    /// </summary>
    public int GetSavingThrowBonus(string ability) {
        int abilityMod = GetAbilityModifier(ability);
        int bonus = abilityMod;

        // 如果精通该豁免检定，添加熟练加值
        if (proficientSaves.Contains(ability.ToLower())) {
            bonus += proficiencyBonus;
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
}
