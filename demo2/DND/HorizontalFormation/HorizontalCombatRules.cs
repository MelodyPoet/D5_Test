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
                // 日志：先攻
                try { GameLog.LogInitiative(character.GetDisplayName(), roll, dexModifier, totalInitiative); } catch { }
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
            public DamageType damageType;
        }

        /// <summary>
        /// 解决攻击检定
        /// </summary>
        /// <param name="attacker">发起攻击的角色</param>
        /// <param name="target">被攻击的目标角色</param>
        /// <param name="advantageFlag">1 = advantage, 0 = normal, -1 = disadvantage</param>
        /// <param name="isMeleeAttack">是否为近战（用于物理攻击时选择 STR/DEX）</param>
        public static AttackResult ResolveAttack(CharacterStats attacker, CharacterStats target, int advantageFlag = 0, bool isMeleeAttack = true)
        {
            AttackResult result = new AttackResult();

            if (attacker == null || target == null)
            {
                result.isHit = false;
                result.description = "无效攻击";
                return result;
            }

            bool isSpellAttack = attacker.template != null && attacker.template.defaultAttackType == DefaultAttackType.Spell;

            // 选择攻击类型与能力（不再根据前/后排决定能力，按模板与兜底规则）
            string abilityNameForHit;
            int abilityModForHit;
            string attackTypeName;
            int attackBonus = GetAttackBonus(attacker, isMeleeAttack, out abilityNameForHit, out abilityModForHit, out attackTypeName);

            // 攻击检定：1d20 (+ advantage/disadvantage) + 攻击加值
            int roll1 = Random.Range(1, 21);
            int roll2 = Random.Range(1, 21);
            int attackRoll = roll1;
            if (advantageFlag > 0)
            {
                attackRoll = Mathf.Max(roll1, roll2);
            }
            else if (advantageFlag < 0)
            {
                attackRoll = Mathf.Min(roll1, roll2);
            }
            int totalAttack = attackRoll + attackBonus;

            // 检查暴击（天然20）
            if (advantageFlag > 0)
            {
                result.isCritical = (roll1 == 20 || roll2 == 20);
            }
            else if (advantageFlag < 0)
            {
                result.isCritical = (roll1 == 20 && roll2 == 20);
            }
            else
            {
                result.isCritical = (attackRoll == 20);
            }

            // 命中检定
            result.isHit = totalAttack >= target.armorClass || result.isCritical;

            // 实时日志：命中检定（显示选择的能力与总修正）
            try { GameLog.LogHit(attacker.GetDisplayName(), target.GetDisplayName(), attackTypeName, abilityNameForHit, attackRoll, attackBonus, totalAttack, target.armorClass, result.isHit); } catch { }

            if (result.isHit)
            {
                // 计算伤害（Spell 使用法术骰且不叠加属性；Physical 使用 1d6+STR）
                int diceSize, rolledTotal, strModForDmg;
                int damage = CalculateDamageUnified(attacker, result.isCritical, isSpellAttack, out diceSize, out rolledTotal, out strModForDmg);
                result.damage = damage;
                result.description = result.isCritical ?
                    $"暴击命中！伤害: {result.damage}" :
                    $"命中！伤害: {result.damage}";

                // 伤害类型
                result.damageType = isSpellAttack
                    ? ((attacker.template != null && attacker.template.defaultCantrip != null) ? attacker.template.defaultCantrip.damageType : DamageType.Force)
                    : DamageType.Bludgeoning;

                // 构造骰子表达（暴击：仅翻倍骰子个数）
                int baseDiceCount = isSpellAttack
                    ? (attacker.template != null && attacker.template.defaultCantrip != null
                        ? attacker.template.defaultCantrip.GetDamageDiceAtCasterLevel(attacker.Level).diceCount
                        : 1)
                    : 1;
                string diceExpr = (result.isCritical ? (baseDiceCount * 2) : baseDiceCount) + "d" + diceSize;
                if (result.isCritical)
                {
                    diceExpr += "（暴击）";
                }

                if (isSpellAttack)
                {
                    // Spell 伤害日志：不显示能力修正
                    try { GameLog.LogDamage(attacker.GetDisplayName(), target.GetDisplayName(), result.damageType.ToString(), diceExpr, rolledTotal, "未应用抗性/易伤", result.damage); } catch { }
                }
                else
                {
                    // Physical 伤害日志：显示 STR 修正
                    try { GameLog.LogDamage(attacker.GetDisplayName(), target.GetDisplayName(), result.damageType.ToString(), diceExpr, rolledTotal, "strength", strModForDmg, "未应用抗性/易伤", result.damage); } catch { }
                }
            }
            else
            {
                result.damage = 0;
                result.description = "攻击未命中";
            }

            return result;
        }

        /// <summary>
        /// 计算攻击加值，并返回用于命中的能力与类型名
        /// Physical：命中使用 max(STR, DEX) 的调整值（兜底规则），不依赖前/后排；
        /// Spell：使用模板主施法属性。
        /// </summary>
        private static int GetAttackBonus(CharacterStats character, bool isMeleeAttack, out string abilityNameForHit, out int abilityModForHit, out string attackTypeName)
        {
            // 默认值
            abilityNameForHit = "strength";
            attackTypeName = "物理普通攻击";

            bool isSpell = character.template != null && character.template.defaultAttackType == DefaultAttackType.Spell;
            if (isSpell)
            {
                // 使用默认法术名字作为攻击类型名
                attackTypeName = (character.template != null && character.template.defaultCantrip != null && !string.IsNullOrEmpty(character.template.defaultCantrip.spellName))
                    ? character.template.defaultCantrip.spellName
                    : "法术普通攻击";
                abilityNameForHit = NormalizeAbilityName(character.template != null ? character.template.primarySpellAbility : "intelligence");
                abilityModForHit = GetAbilityModifierFromStats(character, abilityNameForHit);
            }
            else
            {
                // 物理兜底：命中取 STR/DEX 中较大者
                int str = GetAbilityModifierFromStats(character, "strength");
                int dex = GetAbilityModifierFromStats(character, "dexterity");
                if (dex > str)
                {
                    abilityNameForHit = "dexterity";
                    abilityModForHit = dex;
                }
                else
                {
                    abilityNameForHit = "strength";
                    abilityModForHit = str;
                }
            }

            // 熟练加值：仅当模板判定本次攻击为熟练时才叠加
            int prof = 0;
            if (character != null && character.template != null)
            {
                bool proficient = character.template.IsProficientForAttack(isSpell, isMeleeAttack);
                if (proficient)
                {
                    prof = character.template.GetProficiencyBonusByLevel(character.Level);
                }
            }
            return abilityModForHit + prof;
        }

        /// <summary>
        /// 统一伤害计算：
        /// - Spell：使用 defaultCantrip 的伤害骰（随等级成长），不叠加属性；暴击仅翻倍骰子数。
        /// - Physical：使用 1d6 + STR 调整值；暴击仅翻倍骰。
        /// 输出：diceSize（面数）、rolledTotal（掷骰合计）、strModForDmg（物理用的STR修正；Spell为0）。
        /// </summary>
        private static int CalculateDamageUnified(CharacterStats character, bool isCritical, bool isSpellAttack, out int diceSize, out int rolledTotal, out int strModForDmg)
        {
            rolledTotal = 0;
            strModForDmg = 0;

            if (isSpellAttack)
            {
                // 法术：取模板 defaultCantrip
                DiceFormula dice;
                DamageType dt = DamageType.Force;
                if (character.template != null && character.template.defaultCantrip != null)
                {
                    dice = character.template.defaultCantrip.GetDamageDiceAtCasterLevel(character.Level);
                    dt = character.template.defaultCantrip.damageType;
                }
                else
                {
                    dice = new DiceFormula { diceCount = 1, diceSize = 8 };
                }
                int count = dice.diceCount;
                diceSize = dice.diceSize;
                if (isCritical) count *= 2;
                for (int i = 0; i < count; i++)
                {
                    rolledTotal += Random.Range(1, diceSize + 1);
                }
                // 不叠加属性
                return Mathf.Max(1, rolledTotal);
            }
            else
            {
                // 物理兜底：1d6 + STR
                diceSize = 6;
                int count = isCritical ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    rolledTotal += Random.Range(1, diceSize + 1);
                }
                strModForDmg = GetAbilityModifierFromStats(character, "strength");
                return Mathf.Max(1, rolledTotal + strModForDmg);
            }
        }

        /// <summary>
        /// 通过角色当前属性获取指定能力的调整值
        /// </summary>
        private static int GetAbilityModifierFromStats(CharacterStats c, string ability)
        {
            switch ((ability ?? string.Empty).ToLowerInvariant())
            {
                case "strength":
                case "str": return GetAbilityModifier(c.strength);
                case "dexterity":
                case "dex": return GetAbilityModifier(c.dexterity);
                case "constitution":
                case "con": return GetAbilityModifier(c.constitution);
                case "intelligence":
                case "int": return GetAbilityModifier(c.intelligence);
                case "wisdom":
                case "wis": return GetAbilityModifier(c.wisdom);
                case "charisma":
                case "cha": return GetAbilityModifier(c.charisma);
                default: return 0;
            }
        }

        /// <summary>
        /// 归一化能力名
        /// </summary>
        private static string NormalizeAbilityName(string ability)
        {
            if (string.IsNullOrWhiteSpace(ability)) return "intelligence";
            string val = ability.Trim().ToLowerInvariant();
            switch (val)
            {
                case "str": return "strength";
                case "dex": return "dexterity";
                case "con": return "constitution";
                case "int": return "intelligence";
                case "wis": return "wisdom";
                case "cha": return "charisma";
                default: return val;
            }
        }

        /// <summary>
        /// 计算属性调整值（通用）
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
            // 允许对处于昏迷（Unconscious）的角色进行攻击，即使其 currentHitPoints <= 0
            if (attacker.currentHitPoints <= 0) return false;
            if (target.currentHitPoints <= 0 && !target.HasStatusEffect(StatusEffectType.Unconscious)) return false;
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
