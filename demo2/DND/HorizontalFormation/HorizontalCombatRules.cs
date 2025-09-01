using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 横版战斗攻击规则系统
/// 处理基于位置的攻击范围和目标选择
/// </summary>
public static class HorizontalCombatRules {
    /// <summary>
    /// DND5E先攻检定 - 1d20 + 敏捷调整值
    /// </summary>
    public static int RollInitiative(CharacterStats character) {
        int d20Roll = Random.Range(1, 21); // 1d20
        int dexterityModifier = character.DexMod; // 直接使用DexMod属性
        int initiative = d20Roll + dexterityModifier;

        Debug.Log($"🎲 {character.GetDisplayName()} 先攻检定: {d20Roll} + {dexterityModifier} = {initiative}");
        return initiative;
    }

    /// <summary>
    /// 比较两个角色的先攻顺序
    /// 规则: 先攻值高者优先 > 敏捷属性高者优先 > 随机决定
    /// </summary>
    public static int CompareInitiative(CharacterStats a, CharacterStats b, int initiativeA, int initiativeB) {
        // 先攻值不同，值高者优先
        if (initiativeA != initiativeB) {
            return initiativeB.CompareTo(initiativeA); // 降序排列
        }

        // 先攻值相同，敏捷属性高者优先
        int dexA = a.dexterity;
        int dexB = b.dexterity;
        if (dexA != dexB) {
            return dexB.CompareTo(dexA); // 敏捷高者优先
        }

        // 敏捷也相同，随机决定
        return Random.Range(0, 2) == 0 ? -1 : 1;
    }

    /// <summary>
    /// 为所有参战角色进行先攻检定并排序
    /// </summary>
    public static List<InitiativeEntry> RollAndSortInitiative(List<CharacterStats> combatants) {
        List<InitiativeEntry> initiativeList = new List<InitiativeEntry>();

        // 为每个角色进行先攻检定
        foreach (CharacterStats character in combatants) {
            if (character != null && character.currentHitPoints > 0) {
                int initiative = RollInitiative(character);
                initiativeList.Add(new InitiativeEntry {
                    character = character,
                    initiative = initiative
                });
            }
        }

        // 按先攻顺序排序
        initiativeList.Sort((a, b) => CompareInitiative(a.character, b.character, a.initiative, b.initiative));

        Debug.Log("📋 先攻顺序排序完成:");
        for (int i = 0; i < initiativeList.Count; i++) {
            var entry = initiativeList[i];
            Debug.Log($"  {i + 1}. {entry.character.GetDisplayName()} - 先攻值: {entry.initiative} (敏捷: {entry.character.dexterity})");
        }

        return initiativeList;
    }

    /// <summary>
    /// DND5E攻击检定 - 1d20 + 攻击加值 vs 目标AC
    /// </summary>
    public static bool RollAttackCheck(CharacterStats attacker, CharacterStats target) {
        if (attacker == null || target == null) return false;

        // 1d20攻击骰
        int d20Roll = Random.Range(1, 21);

        // 攻击加值 = 熟练加值 + 属性调整值
        int proficiencyBonus = CalculateProficiencyBonus(attacker.characterLevel);
        int attackModifier = GetAttackModifier(attacker);
        int attackBonus = proficiencyBonus + attackModifier;

        // 总攻击检定值
        int totalAttack = d20Roll + attackBonus;

        // 目标AC
        int targetAC = target.armorClass; // 直接使用armorClass属性

        // 判定结果
        bool isHit = totalAttack >= targetAC;
        bool isCriticalHit = d20Roll == 20;
        bool isCriticalMiss = d20Roll == 1;

        // 自然1自动失败，自然20自动命中
        if (isCriticalMiss) isHit = false;
        if (isCriticalHit) isHit = true;

        Debug.Log($"🎲 攻击检定: {attacker.GetDisplayName()} → {target.GetDisplayName()}");
        Debug.Log($"   骰子:{d20Roll} + 攻击加值:{attackBonus} = {totalAttack} vs AC:{targetAC}");
        Debug.Log($"   结果: {(isHit ? "命中" : "失败")}{(isCriticalHit ? " (暴击!)" : "")}{(isCriticalMiss ? " (大失败)" : "")}");

        return isHit;
    }

    /// <summary>
    /// DND5E伤害计算 - 武器伤害骰 + 属性调整值
    /// </summary>
    public static int RollDamage(CharacterStats attacker, CharacterStats target) {
        if (attacker == null || target == null) return 0;

        // 基础伤害骰（简化版本，实际应该从武器数据获取）
        int baseDamage = GetBaseDamage(attacker);

        // 属性调整值
        int damageModifier = GetDamageModifier(attacker);

        // 总伤害
        int totalDamage = baseDamage + damageModifier;

        // 最小伤害为1
        totalDamage = Mathf.Max(1, totalDamage);

        Debug.Log($"💥 伤害计算: {attacker.GetDisplayName()} → {target.GetDisplayName()}");
        Debug.Log($"   基础伤害:{baseDamage} + 调整值:{damageModifier} = {totalDamage}");

        return totalDamage;
    }

    /// <summary>
    /// 计算熟练加值
    /// </summary>
    private static int CalculateProficiencyBonus(int characterLevel) {
        return 2 + (characterLevel - 1) / 4; // DND5E标准公式
    }

    /// <summary>
    /// 获取攻击属性调整值
    /// </summary>
    private static int GetAttackModifier(CharacterStats character) {
        // 根据职业和攻击类型决定使用哪个属性
        switch (character.characterClass) {
            case CharacterClass.Fighter:
            case CharacterClass.Paladin:
            case CharacterClass.Barbarian:
                return character.StrMod; // 力量攻击

            case CharacterClass.Rogue:
            case CharacterClass.Ranger:
                return character.DexMod; // 敏捷攻击

            case CharacterClass.Wizard:
            case CharacterClass.Sorcerer:
                return character.IntMod; // 智力施法

            case CharacterClass.Cleric:
            case CharacterClass.Druid:
                return character.WisMod; // 感知施法

            case CharacterClass.Warlock:
            case CharacterClass.Bard:
                return character.ChaMod; // 魅力施法

            default:
                return character.StrMod; // 默认力量
        }
    }

    /// <summary>
    /// 获取伤害属性调整值
    /// </summary>
    private static int GetDamageModifier(CharacterStats character) {
        // 通常与攻击调整值相同
        return GetAttackModifier(character);
    }

    /// <summary>
    /// 获取基础伤害骰（简化版本）
    /// </summary>
    private static int GetBaseDamage(CharacterStats character) {
        // 简化的武器伤害，实际应该从装备系统获取
        switch (character.characterClass) {
            case CharacterClass.Fighter:
            case CharacterClass.Paladin:
                return Random.Range(1, 9); // 1d8 长剑

            case CharacterClass.Barbarian:
                return Random.Range(1, 13); // 1d12 巨斧

            case CharacterClass.Rogue:
                return Random.Range(1, 7); // 1d6 短剑

            case CharacterClass.Ranger:
                return Random.Range(1, 9); // 1d8 长弓

            case CharacterClass.Wizard:
            case CharacterClass.Sorcerer:
            case CharacterClass.Warlock:
                return Random.Range(1, 5); // 1d4 法术焦点

            case CharacterClass.Cleric:
                return Random.Range(1, 7); // 1d6 战锤

            case CharacterClass.Druid:
                return Random.Range(1, 7); // 1d6 木棒

            case CharacterClass.Bard:
                return Random.Range(1, 7); // 1d6 细剑

            default:
                return Random.Range(1, 5); // 1d4 默认
        }
    }

    /// <summary>
    /// 判断攻击者是否为远程攻击角色
    /// </summary>
    public static bool IsRangedAttacker(CharacterStats character) {
        switch (character.characterClass) {
            case CharacterClass.Wizard:
            case CharacterClass.Sorcerer:
            case CharacterClass.Warlock:
            case CharacterClass.Ranger:
                return true;
            case CharacterClass.Cleric:
            case CharacterClass.Druid:
            case CharacterClass.Bard:
                return true; // 施法者默认远程
            default:
                return false; // 近战职业
        }
    }
}
