using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 横版战斗攻击规则系统
/// 处理基于位置的攻击范围和目标选择
/// </summary>
public static class HorizontalCombatRules {    /// <summary>
                                               /// 获取近战攻击的有效目标 - 线性布局版本
                                               /// 近战攻击只能攻击距离 <= 1 的目标
                                               /// </summary>
    public static List<CharacterStats> GetMeleeTargets(CharacterStats attacker) {
        BattlePositionComponent positionComponent = attacker.GetComponent<BattlePositionComponent>();
        if (positionComponent == null) {
            Debug.LogWarning($"{attacker.name} 没有 BattlePositionComponent");
            return new List<CharacterStats>();
        }

        HorizontalPosition attackerPos = positionComponent.currentPosition;
        BattleSide attackerSide = HorizontalFormationAI.GetPositionSide(attackerPos);

        List<CharacterStats> validTargets = new List<CharacterStats>();

        // 检查所有位置，找到距离 <= 1 的敌方目标
        for (int i = 0; i < 12; i++) {
            HorizontalPosition targetPos = (HorizontalPosition)i;

            // 跳过同阵营位置
            if (HorizontalFormationAI.GetPositionSide(targetPos) == attackerSide)
                continue;

            // 检查距离
            if (HorizontalFormationAI.CanMeleeAttack(attackerPos, targetPos)) {
                CharacterStats target = HorizontalBattleFormationManager.Instance.GetCharacterAtPosition(targetPos);
                if (target != null && target.currentHitPoints > 0) {
                    validTargets.Add(target);
                }
            }
        }

        return validTargets;
    }
    /// <summary>
    /// 获取远程攻击的有效目标 - 线性布局版本
    /// 远程攻击可以攻击任何距离的敌方目标，但要考虑掩护
    /// </summary>
    public static List<CharacterStats> GetRangedTargets(CharacterStats attacker) {
        BattlePositionComponent positionComponent = attacker.GetComponent<BattlePositionComponent>();
        if (positionComponent == null) {
            Debug.LogWarning($"{attacker.name} 没有 BattlePositionComponent");
            return new List<CharacterStats>();
        }

        HorizontalPosition attackerPos = positionComponent.currentPosition;
        BattleSide attackerSide = HorizontalFormationAI.GetPositionSide(attackerPos);

        List<CharacterStats> validTargets = new List<CharacterStats>();

        // 远程攻击可以攻击对面所有位置的敌人
        for (int i = 0; i < 12; i++) {
            HorizontalPosition targetPos = (HorizontalPosition)i;

            // 跳过同阵营位置
            if (HorizontalFormationAI.GetPositionSide(targetPos) == attackerSide)
                continue;

            CharacterStats target = HorizontalBattleFormationManager.Instance.GetCharacterAtPosition(targetPos);
            if (target != null && target.currentHitPoints > 0) {
                // 检查掩护效果
                CoverType cover = HorizontalCoverSystem.CheckCover(attackerPos, targetPos);

                // 即使有掩护也可以攻击，只是命中率降低
                validTargets.Add(target);
            }
        }

        return validTargets;
    }

    /// <summary>
    /// 获取法术攻击的有效目标
    /// </summary>
    public static List<CharacterStats> GetSpellTargets(CharacterStats caster, DND5E.Spell spell) {
        // 大部分法术遵循远程攻击规则
        List<CharacterStats> targets = GetRangedTargets(caster);

        // 根据法术类型进行特殊处理
        if (spell.heals) {
            // 治疗法术只能对友方使用
            return GetHealingTargets(caster);
        }

        if (spell.areaOfEffect > 0) {
            // 范围法术需要特殊处理
            return GetAreaSpellTargets(caster, spell);
        }

        return targets;
    }    /// <summary>
         /// 获取治疗法术的有效目标 - 线性布局版本
         /// </summary>
    public static List<CharacterStats> GetHealingTargets(CharacterStats caster) {
        BattlePositionComponent positionComponent = caster.GetComponent<BattlePositionComponent>();
        if (positionComponent == null) return new List<CharacterStats>();

        BattleSide casterSide = HorizontalFormationAI.GetPositionSide(positionComponent.currentPosition);
        List<CharacterStats> friendlyTargets = new List<CharacterStats>();

        // 检查所有位置，找到同阵营的受伤盟友
        for (int i = 0; i < 12; i++) {
            HorizontalPosition pos = (HorizontalPosition)i;

            // 只检查同阵营位置
            if (HorizontalFormationAI.GetPositionSide(pos) != casterSide)
                continue;

            CharacterStats ally = HorizontalBattleFormationManager.Instance.GetCharacterAtPosition(pos);
            if (ally != null && ally.currentHitPoints > 0 && ally.currentHitPoints < ally.maxHitPoints) {
                friendlyTargets.Add(ally);
            }
        }

        return friendlyTargets;
    }

    /// <summary>
    /// 获取范围法术的有效目标
    /// </summary>
    public static List<CharacterStats> GetAreaSpellTargets(CharacterStats caster, DND5E.Spell spell) {
        // 简化处理：范围法术可以同时攻击对面前排或后排的多个目标
        BattlePositionComponent positionComponent = caster.GetComponent<BattlePositionComponent>();
        if (positionComponent == null) return new List<CharacterStats>();

        BattleSide casterSide = HorizontalFormationAI.GetPositionSide(positionComponent.currentPosition);
        List<CharacterStats> areaTargets = new List<CharacterStats>();        // 获取对面前排目标
        HorizontalPosition[] frontRowPositions = HorizontalFormationAI.GetNearestEnemyFrontRow(casterSide); foreach (HorizontalPosition pos in frontRowPositions) {
            CharacterStats target = HorizontalBattleFormationManager.Instance.GetCharacterAtPosition(pos);
            if (target != null && target.currentHitPoints > 0) {
                areaTargets.Add(target);
            }
        }

        // 如果前排目标少于2个，也包含后排
        if (areaTargets.Count < 2) {
            HorizontalPosition[] allEnemyPositions = HorizontalFormationAI.GetOppositeAllPositions(casterSide); foreach (HorizontalPosition pos in allEnemyPositions) {
                CharacterStats target = HorizontalBattleFormationManager.Instance.GetCharacterAtPosition(pos);
                if (target != null && target.currentHitPoints > 0 && !areaTargets.Contains(target)) {
                    areaTargets.Add(target);
                }
            }
        }

        return areaTargets;
    }

    /// <summary>
    /// 检查是否可以进行机会攻击
    /// </summary>
    public static bool CanMakeOpportunityAttack(CharacterStats attacker, CharacterStats movingTarget) {
        BattlePositionComponent attackerPos = attacker.GetComponent<BattlePositionComponent>();
        BattlePositionComponent targetPos = movingTarget.GetComponent<BattlePositionComponent>();

        if (attackerPos == null || targetPos == null) return false;

        // 检查攻击者是否在前排
        if (!attackerPos.IsInFrontRow()) return false;

        // 检查目标是否从相邻位置移动
        bool wasAdjacent = HorizontalFormationAI.ArePositionsAdjacent(
            attackerPos.currentPosition,
            targetPos.previousPosition
        );

        bool isStillAdjacent = HorizontalFormationAI.ArePositionsAdjacent(
            attackerPos.currentPosition,
            targetPos.currentPosition
        );

        // 如果目标从相邻位置移开，则可以进行机会攻击
        return wasAdjacent && !isStillAdjacent;
    }

    /// <summary>
    /// 计算攻击的命中修正
    /// </summary>
    public static int CalculateAttackModifier(CharacterStats attacker, CharacterStats target) {
        int baseModifier = 0;
        BattlePositionComponent attackerPos = attacker.GetComponent<BattlePositionComponent>();
        BattlePositionComponent targetPos = target.GetComponent<BattlePositionComponent>();

        if (attackerPos == null || targetPos == null) return baseModifier;

        // 检查掩护效果
        CoverType cover = HorizontalCoverSystem.CheckCover(attackerPos.currentPosition, targetPos.currentPosition);
        switch (cover) {
            case CoverType.Half:
                baseModifier -= 2; // 半掩护 -2 命中
                break;
            case CoverType.ThreeQuarter:
                baseModifier -= 5; // 四分之三掩护 -5 命中
                break;
            case CoverType.Full:
                return -999; // 完全掩护无法攻击
        }

        // 检查背刺加成
        if (IsFlankingAttack(attacker, target)) {
            baseModifier += 2; // 背刺攻击 +2 命中
        }

        return baseModifier;
    }    /// <summary>
         /// 检查是否为背刺攻击
         /// </summary>
    private static bool IsFlankingAttack(CharacterStats attacker, CharacterStats target) {
        // 检查攻击者是否是盗贼且处于背刺状态
        if (attacker.characterClass == DND5E.CharacterClass.Rogue) {
            HorizontalStealthComponent stealthComponent = attacker.GetComponent<HorizontalStealthComponent>();
            if (stealthComponent != null && stealthComponent.stealthState == StealthState.Flanking) {
                return true;
            }
        }

        return false;
    }
}
