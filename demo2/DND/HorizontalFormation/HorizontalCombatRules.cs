using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 横版战斗规则系统
    /// 处理先攻检定、攻击计算等战斗逻辑
    /// </summary>
    public static class HorizontalCombatRules
    {
        /// <summary>
        /// 执行先攻检定并排序
        /// </summary>
        public static List<InitiativeEntry> RollAndSortInitiative(List<CharacterStats> combatants)
        {
            List<InitiativeEntry> initiativeList = new List<InitiativeEntry>();

            foreach (CharacterStats character in combatants)
            {
                if (character == null) continue;

                // 先攻检定：1d20 + 敏捷调整值
                int dexModifier = GetAbilityModifier(character.dexterity);
                int roll = Random.Range(1, 21); // 1d20
                int totalInitiative = roll + dexModifier;

                InitiativeEntry entry = new InitiativeEntry(character, totalInitiative);
                initiativeList.Add(entry);

                Debug.Log($"{character.GetDisplayName()} 先攻检定: {roll} + {dexModifier} = {totalInitiative}");
            }

            // 按先攻值从高到低排序
            initiativeList = initiativeList.OrderByDescending(entry => entry.initiativeValue).ToList();

            return initiativeList;
        }

        /// <summary>
        /// 攻击结果数据结构
        /// </summary>
        public struct AttackResult
        {
            public bool isHit;
            public int damage;
            public bool isCritical;
            public string description;
        }

        /// <summary>
        /// 解决攻击检定
        /// </summary>
        public static AttackResult ResolveAttack(CharacterStats attacker, CharacterStats target)
        {
            AttackResult result = new AttackResult();

            if (attacker == null || target == null)
            {
                result.isHit = false;
                result.description = "无效攻击";
                return result;
            }

            // 攻击检定：1d20 + 攻击加值
            int attackRoll = Random.Range(1, 21);
            int attackBonus = GetAttackBonus(attacker);
            int totalAttack = attackRoll + attackBonus;

            // 检查暴击（天然20）
            result.isCritical = (attackRoll == 20);

            // 命中检定
            result.isHit = totalAttack >= target.armorClass || result.isCritical;

            if (result.isHit)
            {
                // 计算伤害
                result.damage = CalculateDamage(attacker, result.isCritical);
                result.description = result.isCritical ?
                    $"暴击命中！伤害: {result.damage}" :
                    $"命中！伤害: {result.damage}";
            }
            else
            {
                result.damage = 0;
                result.description = "攻击未命中";
            }

            return result;
        }

        /// <summary>
        /// 计算攻击加值
        /// </summary>
        private static int GetAttackBonus(CharacterStats character)
        {
            // 简化计算：等级/2 + 力量调整值
            int levelBonus = character.level / 2;
            int strModifier = GetAbilityModifier(character.strength);
            return levelBonus + strModifier;
        }

        /// <summary>
        /// 计算伤害
        /// </summary>
        private static int CalculateDamage(CharacterStats character, bool isCritical)
        {
            // 基础伤害：1d6 + 力量调整值
            int baseDamage = Random.Range(1, 7) + GetAbilityModifier(character.strength);

            // 暴击双倍伤害
            if (isCritical)
            {
                baseDamage *= 2;
            }

            // 确保最小伤害为1
            return Mathf.Max(1, baseDamage);
        }

        /// <summary>
        /// 计算属性调整值
        /// </summary>
        private static int GetAbilityModifier(int abilityScore)
        {
            return (abilityScore - 10) / 2;
        }

        /// <summary>
        /// 检查角色是否可以攻击目标
        /// </summary>
        public static bool CanAttackTarget(CharacterStats attacker, CharacterStats target)
        {
            if (attacker == null || target == null) return false;
            if (attacker.currentHitPoints <= 0 || target.currentHitPoints <= 0) return false;
            if (attacker.battleSide == target.battleSide) return false; // 不能攻击同伙

            return true;
        }

        /// <summary>
        /// 获取攻击距离
        /// </summary>
        public static float GetAttackRange(CharacterStats character)
        {
            // 简化逻辑：前排=近战，后排=远程
            BattlePositionComponent positionComponent = character.GetComponent<BattlePositionComponent>();
            if (positionComponent != null && positionComponent.rowPosition == RowPosition.Back)
            {
                return 10f; // 远程攻击距离
            }
            return 1.5f; // 近战攻击距离
        }
    }
}
