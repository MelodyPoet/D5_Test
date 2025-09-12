using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 横版战斗攻击规则系统
    /// 处理基于位置的攻击范围和目标选择
    /// </summary>
    public static class HorizontalCombatRules {
        /// <summary>
        /// 伤害事件通道 - 用于解耦伤害处理
        /// </summary>
        public static DamageEventChannel_SO DamageEventChannel; // 修正命名规范

        /// <summary>
        /// DND5E先攻检定 - 1d20 + 敏捷调整值
        /// </summary>
        public static int RollInitiative(CharacterStats character) {
            int d20Roll = Random.Range(1, 21); // 1d20
            int dexterityModifier = character.DexMod; // 直接使用DexMod属性
            int initiative = d20Roll + dexterityModifier;

            Debug.Log($"{character.GetDisplayName()} 先攻检定: {d20Roll} + {dexterityModifier} = {initiative}");
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
                    initiativeList.Add(new InitiativeEntry(character, initiative));
                }
            }

            // 按先攻值排序
            initiativeList.Sort((a, b) => CompareInitiative(a.character, b.character, a.initiativeValue, b.initiativeValue));

            Debug.Log("=== 先攻顺序确定 ===");
            for (int i = 0; i < initiativeList.Count; i++) {
                Debug.Log($"{i + 1}. {initiativeList[i].character.GetDisplayName()} - 先攻值: {initiativeList[i].initiativeValue}");
            }

            return initiativeList;
        }

        /// <summary>
        /// DND5E攻击检定
        /// 计算 1d20 + 属性调整值 + 熟练加值 vs 目标AC
        /// </summary>
        public static bool MakeAttackRoll(CharacterStats attacker, CharacterStats target, out bool isCriticalHit, out int attackRoll) {
            // 攻击骰投掷
            int d20 = Random.Range(1, 21);

            // 力量或敏捷调整值（近战用力量，远程用敏捷）
            int attributeModifier = attacker.StrMod; // 默认使用力量

            // 熟练加值
            int proficiencyBonus = GetProficiencyBonus(attacker.Level); // 修正为 Level（大写）

            // 最终攻击检定
            attackRoll = d20 + attributeModifier + proficiencyBonus;

            // 暴击检测（天然20）
            isCriticalHit = (d20 == 20);

            // 攻击命中检测
            bool hits = attackRoll >= target.armorClass || isCriticalHit;

            Debug.Log($"{attacker.GetDisplayName()} 攻击 {target.GetDisplayName()}: " +
                     $"投骰{d20} + 属性{attributeModifier} + 熟练{proficiencyBonus} = {attackRoll} " +
                     $"vs AC{target.armorClass} - {(hits ? "命中" : "未命中")}" +
                     $"{(isCriticalHit ? " [暴击!]" : "")}");

            return hits;
        }

        /// <summary>
        /// 计算伤害
        /// </summary>
        public static int CalculateDamage(CharacterStats attacker, bool isCriticalHit) {
            // 基础武器伤害 (1d6 默认)
            int baseDamage = Random.Range(1, 7);

            // 暴击时骰子翻倍
            if (isCriticalHit) {
                baseDamage += Random.Range(1, 7);
            }

            // 属性调整值加成
            int attributeModifier = attacker.StrMod;

            int totalDamage = Mathf.Max(1, baseDamage + attributeModifier); // 最少1点伤害

            Debug.Log($"{attacker.GetDisplayName()} 造成伤害: 武器{baseDamage} + 属性{attributeModifier} = {totalDamage}");

            return totalDamage;
        }

        /// <summary>
        /// 应用伤害到目标 - 通过事件通道发布伤害事件，不再直接修改目标血量
        /// </summary>
        public static void ApplyDamage(CharacterStats target, CharacterStats attacker, int damage) {
            Debug.Log($"{attacker.GetDisplayName()} 对 {target.GetDisplayName()} 造成 {damage} 点伤害");

            // 通过事件通道发布伤害事件，而不是直接修改目标血量
            if (DamageEventChannel != null) { // 使用修正后的字段名
                DamageEventChannel.RaiseEvent(target, attacker, damage);
            } else {
                Debug.LogError("伤害事件通道未设置！请在Inspector中拖入DamageEventChannel_SO资产");
            }
        }

        /// <summary>
        /// 处理攻击失败 - 显示MISS
        /// </summary>
        public static void HandleAttackMiss(CharacterStats target, CharacterStats attacker) {
            Debug.Log($"{attacker.GetDisplayName()} 攻击 {target.GetDisplayName()} 失败 - MISS!");

            // 使用统一的伤害显示管理器
            if (DamageDisplayManager.Instance != null) {
                DamageDisplayManager.Instance.ShowMiss(target.transform);
            } else {
                Debug.LogWarning("没有找到伤害显示管理器，无法显示MISS");
            }
        }

        /// <summary>
        /// 执行完整的攻击序列（包含命中判定和伤害计算）
        /// </summary>
        public static void PerformAttack(CharacterStats attacker, CharacterStats target) {
            if (attacker == null || target == null) {
                Debug.LogError("攻击者或目标为空！");
                return;
            }

            // 进行攻击检定
            bool hits = MakeAttackRoll(attacker, target, out bool isCriticalHit, out int attackRoll);

            if (hits) {
                // 攻击命中，计算并应用伤害
                int damage = CalculateDamage(attacker, isCriticalHit);
                ApplyDamage(target, attacker, damage);
            } else {
                // 攻击失败，显示MISS
                HandleAttackMiss(target, attacker);
            }
        }

        /// <summary>
        /// 获取基于等级的熟练加值
        /// </summary>
        public static int GetProficiencyBonus(int level) {
            return 2 + (level - 1) / 4; // DND5E标准熟练加值计算
        }

        /// <summary>
        /// 检查是否可以攻击目标（基于位置规则）
        /// </summary>
        public static bool CanAttackTarget(CharacterStats attacker, CharacterStats target, HorizontalBattleFormationManager formationManager) {
            if (attacker == null || target == null || formationManager == null) return false;

            // 同阵营无法攻击
            if (attacker.battleSide == target.battleSide) return false;

            // 目标已死亡无法攻击
            if (target.currentHitPoints <= 0) return false;

            // 近战角色只能攻击前排，前排全灭后可攻击后排
            if (formationManager.IsMeleeClass(attacker)) {
                var enemyFrontline = formationManager.GetFrontlineCharacters(target.battleSide);

                // 如果前排还有存活角色，近战只能攻击前排
                if (enemyFrontline.Count > 0) {
                    return enemyFrontline.Contains(target);
                }
                // 前排全灭，可以攻击后排
            }

            // 远程角色可以攻击任意目标
            return true;
        }

        /// <summary>
        /// 获取角色的可攻击目标列表
        /// </summary>
        public static List<CharacterStats> GetValidTargets(CharacterStats attacker, HorizontalBattleFormationManager formationManager) {
            List<CharacterStats> validTargets = new List<CharacterStats>();

            if (attacker == null || formationManager == null) return validTargets;

            // 确定敌方阵营
            BattleSide enemySide = (attacker.battleSide == BattleSide.Player) ? BattleSide.Enemy : BattleSide.Player;

            // 获取所有敌方存活角色
            List<CharacterStats> enemies = formationManager.GetAllAliveCharacters(enemySide);

            // 筛选可攻击的目标
            foreach (CharacterStats enemy in enemies) {
                if (CanAttackTarget(attacker, enemy, formationManager)) {
                    validTargets.Add(enemy);
                }
            }

            return validTargets;
        }

        /// <summary>
        /// AI目标选择 - 优先攻击前排，前排全灭后攻击后排
        /// </summary>
        public static CharacterStats SelectBestTarget(CharacterStats attacker, HorizontalBattleFormationManager formationManager) {
            List<CharacterStats> validTargets = GetValidTargets(attacker, formationManager);

            if (validTargets.Count == 0) return null;

            // 确定敌方阵营
            BattleSide enemySide = (attacker.battleSide == BattleSide.Player) ? BattleSide.Enemy : BattleSide.Player;

            // 优先攻击前排
            var frontlineTargets = formationManager.GetFrontlineCharacters(enemySide);
            var availableFrontline = validTargets.Where(t => frontlineTargets.Contains(t)).ToList();

            if (availableFrontline.Count > 0) {
                // 从前排中选择血量最少的目标
                return availableFrontline.OrderBy(t => t.currentHitPoints).First();
            }

            // 前排无目标，攻击后排血量最少的
            return validTargets.OrderBy(t => t.currentHitPoints).First();
        }
    }
}
