using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DND5E;

/// <summary>
/// 自动战斗AI系统 - 线性阵型版本
/// 实现挂机模式的智能战斗决策
/// </summary>
public class AutoBattleAI : MonoBehaviour {
    [Header("AI设置")]
    public bool enableAutoBattle = false;
    public float decisionDelay = 1.0f; // AI决策延迟
    public bool showAIThoughts = true; // 显示AI思考过程

    [Header("战术优先级")]
    [Range(0, 1)] public float healingPriority = 0.8f;
    [Range(0, 1)] public float positioningPriority = 0.6f;
    [Range(0, 1)] public float offensivePriority = 0.7f;
    [Range(0, 1)] public float defensivePriority = 0.5f;

    private bool isProcessingTurn = false;

    /// <summary>
    /// 为角色执行自动战斗回合
    /// </summary>
    public void ExecuteAutoBattleTurn(CharacterStats character) {
        if (isProcessingTurn || !enableAutoBattle) return;

        StartCoroutine(ProcessAutoBattleTurn(character));
    }

    /// <summary>
    /// 处理自动战斗回合
    /// </summary>
    private System.Collections.IEnumerator ProcessAutoBattleTurn(CharacterStats character) {
        isProcessingTurn = true;

        if (showAIThoughts)
            Debug.Log($"=== {character.GetDisplayName()} 的AI回合开始 ===");

        yield return new WaitForSeconds(decisionDelay);

        // AI决策流程
        BattleAction chosenAction = DecideBestAction(character);

        if (chosenAction != null) {
            ExecuteBattleAction(character, chosenAction);
        }

        yield return new WaitForSeconds(decisionDelay);

        isProcessingTurn = false;
    }

    /// <summary>
    /// 决定最佳行动
    /// </summary>
    private BattleAction DecideBestAction(CharacterStats character) {
        List<BattleAction> possibleActions = new List<BattleAction>();

        // 1. 检查是否需要治疗
        BattleAction healAction = ConsiderHealing(character);
        if (healAction != null) {
            possibleActions.Add(healAction);
        }

        // 2. 检查是否需要重新定位
        BattleAction moveAction = ConsiderRepositioning(character);
        if (moveAction != null) {
            possibleActions.Add(moveAction);
        }

        // 3. 考虑攻击行动
        BattleAction attackAction = ConsiderAttack(character);
        if (attackAction != null) {
            possibleActions.Add(attackAction);
        }

        // 4. 考虑法术行动
        BattleAction spellAction = ConsiderSpellcasting(character);
        if (spellAction != null) {
            possibleActions.Add(spellAction);
        }

        // 5. 考虑特殊职业技能
        BattleAction specialAction = ConsiderSpecialAbilities(character);
        if (specialAction != null) {
            possibleActions.Add(specialAction);
        }

        // 根据优先级排序并选择最佳行动
        return ChooseBestAction(possibleActions);
    }

    /// <summary>
    /// 考虑治疗行动
    /// </summary>
    private BattleAction ConsiderHealing(CharacterStats character) {        // 检查自己和盟友的血量
        BattlePositionComponent positionComponent = character.GetComponent<BattlePositionComponent>();
        if (positionComponent == null) return null;

        BattleSide side = HorizontalFormationAI.GetPositionSide(positionComponent.currentPosition);

        // 找到最需要治疗的盟友
        CharacterStats mostWounded = null;
        float lowestHealthPercent = 1.0f;

        for (int i = 0; i < 12; i++) {
            HorizontalPosition pos = (HorizontalPosition)i;
            if (HorizontalFormationAI.GetPositionSide(pos) != side) continue;

            CharacterStats ally = HorizontalBattleFormationManager.Instance?.GetCharacterAtPosition(pos);
            if (ally != null && ally.currentHitPoints > 0) {
                float healthPercent = (float)ally.currentHitPoints / ally.maxHitPoints;
                if (healthPercent < lowestHealthPercent && healthPercent < 0.5f) {
                    lowestHealthPercent = healthPercent;
                    mostWounded = ally;
                }
            }
        }

        if (mostWounded != null && CanCastHealingSpell(character)) {
            return new BattleAction {
                type = BattleActionType.Spell,
                priority = healingPriority + (1.0f - lowestHealthPercent), // 越受伤优先级越高
                target = mostWounded,
                description = $"治疗 {mostWounded.GetDisplayName()}"
            };
        }

        return null;
    }
    /// <summary>
    /// 考虑重新定位
    /// </summary>
    private BattleAction ConsiderRepositioning(CharacterStats character) {
        BattlePositionComponent positionComponent = character.GetComponent<BattlePositionComponent>();
        if (positionComponent == null) return null;

        HorizontalPosition currentPos = positionComponent.currentPosition;
        BattleSide side = HorizontalFormationAI.GetPositionSide(currentPos);

        // 检查当前位置是否合适
        HorizontalPosition optimalPos = HorizontalFormationAI.GetOptimalPosition(character, side);

        if (currentPos != optimalPos && !HorizontalBattleFormationManager.Instance.IsPositionOccupied(optimalPos)) {
            return new BattleAction {
                type = BattleActionType.Move,
                priority = positioningPriority,
                targetPosition = optimalPos,
                description = $"移动到更好的位置: {optimalPos}"
            };
        }

        return null;
    }

    /// <summary>
    /// 考虑攻击行动
    /// </summary>
    private BattleAction ConsiderAttack(CharacterStats character) {
        List<CharacterStats> meleeTargets = HorizontalCombatRules.GetMeleeTargets(character);
        List<CharacterStats> rangedTargets = HorizontalCombatRules.GetRangedTargets(character);

        // 优先近战攻击（更高伤害）
        if (meleeTargets.Count > 0) {
            CharacterStats bestTarget = ChooseBestAttackTarget(meleeTargets);
            return new BattleAction {
                type = BattleActionType.MeleeAttack,
                priority = offensivePriority + 0.1f, // 近战优先级稍高
                target = bestTarget,
                description = $"近战攻击 {bestTarget.GetDisplayName()}"
            };
        }

        // 远程攻击
        if (rangedTargets.Count > 0) {
            CharacterStats bestTarget = ChooseBestAttackTarget(rangedTargets);
            return new BattleAction {
                type = BattleActionType.RangedAttack,
                priority = offensivePriority,
                target = bestTarget,
                description = $"远程攻击 {bestTarget.GetDisplayName()}"
            };
        }

        return null;
    }

    /// <summary>
    /// 考虑法术施放
    /// </summary>
    private BattleAction ConsiderSpellcasting(CharacterStats character) {
        SpellSystem spellSystem = character.GetComponent<SpellSystem>();
        if (spellSystem == null || spellSystem.spellList == null) return null;
        // 检查可用法术
        List<DND5E.Spell> availableSpells = GetAvailableSpells(spellSystem);
        if (availableSpells.Count == 0) return null;
        // 选择最佳法术
        foreach (DND5E.Spell spell in availableSpells) {
            if (spell.dealsDamage) // 伤害法术
            {
                List<CharacterStats> spellTargets = HorizontalCombatRules.GetSpellTargets(character, spell);
                if (spellTargets.Count > 0) {
                    CharacterStats bestTarget = ChooseBestSpellTarget(spellTargets, spell);
                    return new BattleAction {
                        type = BattleActionType.Spell,
                        priority = offensivePriority + (spell.level * 0.1f),
                        target = bestTarget,
                        spell = spell,
                        description = $"施放 {spell.name} 攻击 {bestTarget.GetDisplayName()}"
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 考虑特殊职业技能
    /// </summary>
    private BattleAction ConsiderSpecialAbilities(CharacterStats character) {
        switch (character.characterClass) {
            case CharacterClass.Rogue:
                return ConsiderRogueAbilities(character);
            case CharacterClass.Fighter:
                return ConsiderFighterAbilities(character);
            case CharacterClass.Paladin:
                return ConsiderPaladinAbilities(character);
            default:
                return null;
        }
    }

    /// <summary>
    /// 考虑盗贼技能
    /// </summary>
    private BattleAction ConsiderRogueAbilities(CharacterStats character) {
        HorizontalStealthComponent stealthComponent = character.GetComponent<HorizontalStealthComponent>();
        if (stealthComponent == null) return null;

        if (stealthComponent.stealthState == StealthState.Visible && Random.Range(0f, 1f) < 0.3f) {
            return new BattleAction {
                type = BattleActionType.Special,
                priority = 0.6f,
                description = "尝试潜行",
                specialAction = () => stealthComponent.AttemptStealth()
            };
        }
        if (stealthComponent.stealthState == StealthState.Hidden && Random.Range(0f, 1f) < 0.7f) {
            return new BattleAction {
                type = BattleActionType.Special,
                priority = 0.8f,
                description = "尝试背刺",
                specialAction = () => stealthComponent.AttemptFlankingManeuver()
            };
        }

        return null;
    }

    /// <summary>
    /// 考虑战士技能
    /// </summary>
    private BattleAction ConsiderFighterAbilities(CharacterStats character) {
        // 战士的第二次攻击等特殊能力
        // 这里可以扩展更多战士技能
        return null;
    }

    /// <summary>
    /// 考虑圣武士技能
    /// </summary>
    private BattleAction ConsiderPaladinAbilities(CharacterStats character) {
        // 圣武士的治疗能力、神圣打击等
        // 这里可以扩展更多圣武士技能
        return null;
    }
    // 辅助方法
    private bool CanCastHealingSpell(CharacterStats character) {
        SpellSystem spellSystem = character.GetComponent<SpellSystem>();
        if (spellSystem?.spellList == null) return false;

        return spellSystem.spellList.knownSpells.Any(s => s.name.Contains("Heal") &&
                                                    spellSystem.CanCastSpell(s));
    }
    private List<DND5E.Spell> GetAvailableSpells(SpellSystem spellSystem) {
        List<DND5E.Spell> availableSpells = new List<DND5E.Spell>();

        foreach (DND5E.Spell spell in spellSystem.spellList.knownSpells) {
            if (spellSystem.CanCastSpell(spell)) {
                availableSpells.Add(spell);
            }
        }

        return availableSpells;
    }

    private CharacterStats ChooseBestAttackTarget(List<CharacterStats> targets) {
        // 优先攻击血量最少的敌人
        return targets.OrderBy(t => t.currentHitPoints).First();
    }
    private CharacterStats ChooseBestSpellTarget(List<CharacterStats> targets, DND5E.Spell spell) {
        if (spell.areaOfEffect > 0) {
            // 范围法术选择能影响最多敌人的位置
            return targets.OrderByDescending(t => CountNearbyEnemies(t)).First();
        }
        else {
            // 单体法术选择最佳目标
            return ChooseBestAttackTarget(targets);
        }
    }

    private int CountNearbyEnemies(CharacterStats center) {        // 计算目标周围的敌人数量（用于范围法术）
        BattlePositionComponent posComponent = center.GetComponent<BattlePositionComponent>();
        if (posComponent == null) return 0;

        int count = 0;
        HorizontalPosition centerPos = posComponent.currentPosition;

        for (int i = 0; i < 12; i++) {
            HorizontalPosition pos = (HorizontalPosition)i;
            if (HorizontalFormationAI.GetLinearDistance(centerPos, pos) <= 2) {
                CharacterStats enemy = HorizontalBattleFormationManager.Instance?.GetCharacterAtPosition(pos);
                if (enemy != null && enemy.currentHitPoints > 0) {
                    count++;
                }
            }
        }

        return count;
    }

    private BattleAction ChooseBestAction(List<BattleAction> actions) {
        if (actions.Count == 0) return null;

        // 按优先级排序
        actions.Sort((a, b) => b.priority.CompareTo(a.priority));

        BattleAction chosen = actions[0];

        if (showAIThoughts) {
            Debug.Log($"AI选择行动: {chosen.description} (优先级: {chosen.priority:F2})");
        }

        return chosen;
    }

    private void ExecuteBattleAction(CharacterStats character, BattleAction action) {
        switch (action.type) {
            case BattleActionType.MeleeAttack:
                // 执行近战攻击逻辑
                Debug.Log($"{character.GetDisplayName()} 执行近战攻击");
                break;

            case BattleActionType.RangedAttack:
                // 执行远程攻击逻辑
                Debug.Log($"{character.GetDisplayName()} 执行远程攻击");
                break;

            case BattleActionType.Spell:
                // 执行法术施放逻辑
                Debug.Log($"{character.GetDisplayName()} 施放法术");
                break;

            case BattleActionType.Move:
                // 执行移动逻辑
                if (action.targetPosition.HasValue) {
                    HorizontalBattleFormationManager.Instance.PlaceCharacterAtPosition(character, action.targetPosition.Value);
                    Debug.Log($"{character.GetDisplayName()} 移动到 {action.targetPosition.Value}");
                }
                break;

            case BattleActionType.Special:
                // 执行特殊技能
                action.specialAction?.Invoke();
                break;
        }
    }
}

/// <summary>
/// 战斗行动类型
/// </summary>
public enum BattleActionType {
    MeleeAttack,
    RangedAttack,
    Spell,
    Move,
    Special
}

/// <summary>
/// 战斗行动数据结构
/// </summary>
public class BattleAction {
    public BattleActionType type;
    public float priority;
    public CharacterStats target;
    public HorizontalPosition? targetPosition;
    public DND5E.Spell spell;
    public string description;
    public System.Action specialAction;
}
