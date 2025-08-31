using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DND5E;

/// <summary>
/// 自动战斗AI系统 - 线性阵型版本
/// 实现挂机模式的智能战斗决策和攻击动画
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

        // 1. 考虑攻击行动（优先级最高）
        BattleAction attackAction = ConsiderAttack(character);
        if (attackAction != null) {
            possibleActions.Add(attackAction);
        }

        // 2. 考虑法术行动
        BattleAction spellAction = ConsiderSpellcasting(character);
        if (spellAction != null) {
            possibleActions.Add(spellAction);
        }

        // 3. 检查是否需要治疗
        BattleAction healAction = ConsiderHealing(character);
        if (healAction != null) {
            possibleActions.Add(healAction);
        }

        // 根据优先级排序并选择最佳行动
        return ChooseBestAction(possibleActions);
    }

    /// <summary>
    /// 考虑攻击行动
    /// </summary>
    private BattleAction ConsiderAttack(CharacterStats character) {
        // 查找可攻击的目标
        List<CharacterStats> availableTargets = FindAvailableTargets(character);

        if (availableTargets.Count > 0) {
            CharacterStats bestTarget = ChooseBestAttackTarget(availableTargets);

            // 根据角色职业决定攻击类型
            bool isMeleeCharacter = IsMeleeCharacter(character);

            return new BattleAction {
                type = isMeleeCharacter ? BattleActionType.MeleeAttack : BattleActionType.RangedAttack,
                priority = offensivePriority + (isMeleeCharacter ? 0.1f : 0f),
                target = bestTarget,
                description = $"{(isMeleeCharacter ? "近战" : "远程")}攻击 {bestTarget.GetDisplayName()}"
            };
        }

        return null;
    }

    /// <summary>
    /// 考虑法术施放 - 简化版本
    /// </summary>
    private BattleAction ConsiderSpellcasting(CharacterStats character) {
        SpellSystem spellSystem = character.GetComponent<SpellSystem>();
        if (spellSystem == null || spellSystem.spellList == null) return null;

        // 检查可用法术
        List<DND5E.Spell> availableSpells = GetAvailableSpells(spellSystem);
        if (availableSpells.Count == 0) return null;

        // 找到可攻击的目标
        List<CharacterStats> availableTargets = FindAvailableTargets(character);
        if (availableTargets.Count == 0) return null;

        // 选择最佳法术
        foreach (DND5E.Spell spell in availableSpells) {
            if (spell.dealsDamage) // 伤害法术
            {
                CharacterStats bestTarget = ChooseBestSpellTarget(availableTargets, spell);
                return new BattleAction {
                    type = BattleActionType.Spell,
                    priority = offensivePriority + (spell.level * 0.1f),
                    target = bestTarget,
                    spell = spell,
                    description = $"施放 {spell.name} 攻击 {bestTarget.GetDisplayName()}"
                };
            }
        }

        return null;
    }

    /// <summary>
    /// 考虑治疗行动 - 简化版本
    /// </summary>
    private BattleAction ConsiderHealing(CharacterStats character) {
        // 简化版本：检查同阵营受伤的盟友
        BattleSide side = character.battleSide;

        // 找到最需要治疗的盟友
        CharacterStats mostWounded = null;
        float lowestHealthPercent = 1.0f;

        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();
        foreach (CharacterStats ally in allCharacters) {
            if (ally.battleSide == side && ally.currentHitPoints > 0 && ally != character) {
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
                priority = healingPriority + (1.0f - lowestHealthPercent),
                target = mostWounded,
                description = $"治疗 {mostWounded.GetDisplayName()}"
            };
        }

        return null;
    }

    /// <summary>
    /// 查找可攻击的目标
    /// </summary>
    private List<CharacterStats> FindAvailableTargets(CharacterStats attacker) {
        List<CharacterStats> targets = new List<CharacterStats>();

        // 获取攻击者的阵营
        BattleSide attackerSide = attacker.battleSide;
        BattleSide enemySide = attackerSide == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;

        // 查找所有敌方角色
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();
        foreach (CharacterStats character in allCharacters) {
            if (character.battleSide == enemySide && character.currentHitPoints > 0) {
                targets.Add(character);
            }
        }

        return targets;
    }

    /// <summary>
    /// 判断角色是否为近战职业
    /// </summary>
    private bool IsMeleeCharacter(CharacterStats character) {
        switch (character.characterClass) {
            case CharacterClass.Fighter:
            case CharacterClass.Paladin:
            case CharacterClass.Barbarian:
            case CharacterClass.Rogue:
                return true;
            case CharacterClass.Wizard:
            case CharacterClass.Sorcerer:
            case CharacterClass.Warlock:
            case CharacterClass.Ranger:
                return false;
            case CharacterClass.Cleric:
            case CharacterClass.Druid:
            case CharacterClass.Bard:
                return false; // 默认远程，可根据需要调整
            default:
                return true; // 默认近战
        }
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
        // 根据DND5E战术规则选择目标：前排优先，同排选择血量最少
        return ChooseTargetByTacticalPriority(targets);
    }

    private CharacterStats ChooseBestSpellTarget(List<CharacterStats> targets, DND5E.Spell spell) {
        if (spell.areaOfEffect > 0) {
            // 范围法术选择能影响最多敌人的位置（简化版本：选择第一个目标）
            return targets.First();
        }
        else {
            // 单体法术选择最佳目标
            return ChooseBestAttackTarget(targets);
        }
    }

    /// <summary>
    /// 根据战术优先级选择攻击目标
    /// 规则：前排优先 > 血量最少
    /// </summary>
    private CharacterStats ChooseTargetByTacticalPriority(List<CharacterStats> targets) {
        if (targets.Count == 0) return null;
        if (targets.Count == 1) return targets[0];

        // 按战术优先级分组：前排 > 后排
        var frontLineTargets = new List<CharacterStats>();
        var backLineTargets = new List<CharacterStats>();

        foreach (CharacterStats target in targets) {
            bool isFrontLine = IsInFrontLine(target);
            Debug.Log($"🎯 目标分析: {target.GetDisplayName()} - 位置X:{target.transform.position.x:F1} - 阵营:{target.battleSide} - 判定:{(isFrontLine ? "前排" : "后排")}");

            if (isFrontLine) {
                frontLineTargets.Add(target);
            } else {
                backLineTargets.Add(target);
            }
        }

        Debug.Log($"🎯 战术分析: 前排目标{frontLineTargets.Count}个, 后排目标{backLineTargets.Count}个");

        // 优先攻击前排，如果前排没有存活角色则攻击后排
        if (frontLineTargets.Count > 0) {
            // 前排中选择血量最少的
            CharacterStats selectedTarget = frontLineTargets.OrderBy(t => t.currentHitPoints).First();
            Debug.Log($"✅ 选择前排目标: {selectedTarget.GetDisplayName()} (血量:{selectedTarget.currentHitPoints})");
            return selectedTarget;
        } else {
            // 后排中选择血量最少的
            CharacterStats selectedTarget = backLineTargets.OrderBy(t => t.currentHitPoints).First();
            Debug.Log($"⚠️ 前排无目标，选择后排: {selectedTarget.GetDisplayName()} (血量:{selectedTarget.currentHitPoints})");
            return selectedTarget;
        }
    }

    /// <summary>
    /// 判断角色是否在前排位置
    /// 根据BattlePositionComponent或实际spawn点坐标判断
    /// </summary>
    private bool IsInFrontLine(CharacterStats character) {
        // 方法1：通过BattlePositionComponent判断（优先使用）
        BattlePositionComponent positionComponent = character.GetComponent<BattlePositionComponent>();
        if (positionComponent != null) {
            bool result = IsPositionInFrontLine(positionComponent.currentPosition);
            Debug.Log($"🔍 位置组件判断: {character.GetDisplayName()} - 枚举位置:{positionComponent.currentPosition} - 判定:{(result ? "前排" : "后排")}");
            return result;
        }

        // 方法2：通过实际spawn点坐标判断（从HorizontalBattleFormationManager获取）
        if (HorizontalBattleFormationManager.Instance != null) {
            BattleSide side = character.battleSide;
            float characterX = character.transform.position.x;

            // 获取该阵营前排和后排的X坐标范围
            float frontLineX = GetFrontLineXCoordinate(side);
            float backLineX = GetBackLineXCoordinate(side);

            // 判断角色更接近前排还是后排
            float distanceToFront = Mathf.Abs(characterX - frontLineX);
            float distanceToBack = Mathf.Abs(characterX - backLineX);

            bool result = distanceToFront < distanceToBack;
            Debug.Log($"🔍 坐标判断: {character.GetDisplayName()} - X:{characterX:F1} - 前排X:{frontLineX:F1} - 后排X:{backLineX:F1} - 判定:{(result ? "前排" : "后排")}");
            return result;
        }

        // 方法3：备用简单判断（不应该到这里）
        Debug.LogWarning($"⚠️ 无法准确判断 {character.GetDisplayName()} 的前后排位置，使用备用判断");
        return true; // 默认当作前排
    }

    /// <summary>
    /// 获取指定阵营前排的X坐标
    /// </summary>
    private float GetFrontLineXCoordinate(BattleSide side) {
        if (HorizontalBattleFormationManager.Instance == null) return 0f;

        Transform frontSpawn;
        if (side == BattleSide.Player) {
            frontSpawn = HorizontalBattleFormationManager.Instance.playerFrontCenterSpawn;
        } else {
            frontSpawn = HorizontalBattleFormationManager.Instance.enemyFrontCenterSpawn;
        }

        return frontSpawn != null ? frontSpawn.position.x : 0f;
    }

    /// <summary>
    /// 获取指定阵营后排的X坐标
    /// </summary>
    private float GetBackLineXCoordinate(BattleSide side) {
        if (HorizontalBattleFormationManager.Instance == null) return 0f;

        Transform backSpawn;
        if (side == BattleSide.Player) {
            backSpawn = HorizontalBattleFormationManager.Instance.playerBackCenterSpawn;
        } else {
            backSpawn = HorizontalBattleFormationManager.Instance.enemyBackCenterSpawn;
        }

        return backSpawn != null ? backSpawn.position.x : 0f;
    }

    /// <summary>
    /// 根据阵型位置判断是否为前排
    /// </summary>
    private bool IsPositionInFrontLine(HorizontalPosition position) {
        switch (position) {
            // 玩家前排
            case HorizontalPosition.PlayerFrontLeft:
            case HorizontalPosition.PlayerFrontCenter:
            case HorizontalPosition.PlayerFrontRight:
            // 敌人前排
            case HorizontalPosition.EnemyFrontLeft:
            case HorizontalPosition.EnemyFrontCenter:
            case HorizontalPosition.EnemyFrontRight:
                return true;
            // 玩家后排
            case HorizontalPosition.PlayerBackLeft:
            case HorizontalPosition.PlayerBackCenter:
            case HorizontalPosition.PlayerBackRight:
            // 敌人后排
            case HorizontalPosition.EnemyBackLeft:
            case HorizontalPosition.EnemyBackCenter:
            case HorizontalPosition.EnemyBackRight:
                return false;
            default:
                return true; // 默认当作前排
        }
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

    /// <summary>
    /// 执行战斗行动 - 实现真正的攻击动画和行为模式
    /// </summary>
    private void ExecuteBattleAction(CharacterStats character, BattleAction action) {
        switch (action.type) {
            case BattleActionType.MeleeAttack:
                ExecuteMeleeAttack(character, action.target);
                break;

            case BattleActionType.RangedAttack:
                ExecuteRangedAttack(character, action.target);
                break;

            case BattleActionType.Spell:
                ExecuteSpellCast(character, action.target);
                break;

            case BattleActionType.Move:
                // 执行移动逻辑（暂时简化）
                Debug.Log($"{character.GetDisplayName()} 移动");
                break;

            case BattleActionType.Special:
                // 执行特殊技能
                action.specialAction?.Invoke();
                break;
        }
    }

    /// <summary>
    /// 执行近战攻击 - 移动到敌人面前攻击后返回原位
    /// </summary>
    private void ExecuteMeleeAttack(CharacterStats attacker, CharacterStats target) {
        if (attacker == null || target == null) return;

        Debug.Log($"🗡️ {attacker.GetDisplayName()} 执行近战攻击 → {target.GetDisplayName()}");

        // 获取攻击者的动画适配器
        DND_CharacterAdapter attackerAdapter = attacker.GetComponent<DND_CharacterAdapter>();
        if (attackerAdapter == null) {
            Debug.LogError($"ExecuteMeleeAttack: {attacker.GetDisplayName()} 缺少DND_CharacterAdapter组件");
            return;
        }

        // 启动近战攻击协程（移动→攻击→返回）
        StartCoroutine(MeleeAttackSequence(attacker, target, attackerAdapter));
    }

    /// <summary>
    /// 近战攻击序列：移动到敌人面前→攻击→返回原位
    /// </summary>
    private System.Collections.IEnumerator MeleeAttackSequence(
        CharacterStats attacker, CharacterStats target, DND_CharacterAdapter attackerAdapter) {

        // 记录原始位置
        Vector3 originalPosition = attacker.transform.position;

        // 计算目标旁边的攻击位置（目标前方1.5单位）
        Vector3 targetPosition = target.transform.position;
        Vector3 attackPosition = targetPosition + (targetPosition.x > originalPosition.x ? Vector3.left : Vector3.right) * 1.5f;

        Debug.Log($"🏃 {attacker.GetDisplayName()} 冲向 {target.GetDisplayName()}");

        // 第一阶段：移动到攻击位置，播放走路动画
        attackerAdapter.PlayAnimation(attackerAdapter.animationMapping.walkAnimation, true);

        float moveSpeed = 5f;
        float moveTime = Vector3.Distance(originalPosition, attackPosition) / moveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < moveTime) {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveTime;
            attacker.transform.position = Vector3.Lerp(originalPosition, attackPosition, t);
            yield return null;
        }

        attacker.transform.position = attackPosition;

        // 第二阶段：执行攻击动画
        Debug.Log($"⚔️ {attacker.GetDisplayName()} 攻击 {target.GetDisplayName()}");
        attackerAdapter.PlayAttackAnimation();

        // 等待攻击动画完成
        float attackDuration = 1.0f; // 默认攻击动画时长
        if (attackerAdapter.skeletonAnimation?.AnimationState?.GetCurrent(0) != null) {
            attackDuration = attackerAdapter.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Duration;
        }
        yield return new WaitForSeconds(attackDuration);

        // 应用伤害
        ApplyMeleeDamage(attacker, target);

        // 第三阶段：返回原位
        Debug.Log($"🔙 {attacker.GetDisplayName()} 返回原位");
        attackerAdapter.PlayAnimation(attackerAdapter.animationMapping.walkAnimation, true);

        elapsedTime = 0f;
        while (elapsedTime < moveTime) {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveTime;
            attacker.transform.position = Vector3.Lerp(attackPosition, originalPosition, t);
            yield return null;
        }

        attacker.transform.position = originalPosition;

        // 返回待机动画
        attackerAdapter.PlayAnimation(attackerAdapter.animationMapping.idleAnimation, true);
        Debug.Log($"✅ {attacker.GetDisplayName()} 近战攻击完成");
    }

    /// <summary>
    /// 执行远程攻击 - 原地攻击，支持跨位攻击后排
    /// </summary>
    private void ExecuteRangedAttack(CharacterStats attacker, CharacterStats target) {
        if (attacker == null || target == null) return;

        Debug.Log($"🏹 {attacker.GetDisplayName()} 执行远程攻击 → {target.GetDisplayName()}");

        // 获取攻击者的动画适配器
        DND_CharacterAdapter attackerAdapter = attacker.GetComponent<DND_CharacterAdapter>();
        if (attackerAdapter == null) {
            Debug.LogError($"ExecuteRangedAttack: {attacker.GetDisplayName()} 缺少DND_CharacterAdapter组件");
            return;
        }

        // 启动远程攻击协程
        StartCoroutine(RangedAttackSequence(attacker, target, attackerAdapter));
    }

    /// <summary>
    /// 远程攻击序列：原地攻击动画
    /// </summary>
    private System.Collections.IEnumerator RangedAttackSequence(
        CharacterStats attacker, CharacterStats target, DND_CharacterAdapter attackerAdapter) {

        Debug.Log($"🎯 {attacker.GetDisplayName()} 瞄准 {target.GetDisplayName()}");

        // 播放攻击动画（原地不动）
        attackerAdapter.PlayAttackAnimation();

        // 等待攻击动画完成
        float attackDuration = 1.0f; // 默认攻击动画时长
        if (attackerAdapter.skeletonAnimation?.AnimationState?.GetCurrent(0) != null) {
            attackDuration = attackerAdapter.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Duration;
        }

        // 在动画中途应用伤害（模拟箭矢/法术飞行时间）
        yield return new WaitForSeconds(attackDuration * 0.6f);

        // 应用伤害
        ApplyRangedDamage(attacker, target);

        // 等待动画完全结束
        yield return new WaitForSeconds(attackDuration * 0.4f);

        Debug.Log($"✅ {attacker.GetDisplayName()} 远程攻击完成");
    }

    /// <summary>
    /// 执行法术施放
    /// </summary>
    private void ExecuteSpellCast(CharacterStats caster, CharacterStats target) {
        if (caster == null || target == null) return;

        Debug.Log($"✨ {caster.GetDisplayName()} 施放法术 → {target.GetDisplayName()}");

        // 获取施法者的动画适配器
        DND_CharacterAdapter casterAdapter = caster.GetComponent<DND_CharacterAdapter>();
        if (casterAdapter == null) {
            Debug.LogError($"ExecuteSpellCast: {caster.GetDisplayName()} 缺少DND_CharacterAdapter组件");
            return;
        }

        // 播放施法动画
        casterAdapter.PlayAnimation(casterAdapter.animationMapping.castAnimation, false);

        // 等待施法完成后返回待机
        float castDuration = 1.5f; // 默认施法时长
        if (casterAdapter.skeletonAnimation?.AnimationState?.GetCurrent(0) != null) {
            castDuration = casterAdapter.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Duration;
        }

        StartCoroutine(DelayedReturnToIdle(casterAdapter, castDuration));
    }

    /// <summary>
    /// 延迟返回待机状态
    /// </summary>
    private System.Collections.IEnumerator DelayedReturnToIdle(DND_CharacterAdapter adapter, float delay) {
        yield return new WaitForSeconds(delay);
        adapter.PlayAnimation(adapter.animationMapping.idleAnimation, true);
    }

    /// <summary>
    /// 应用近战伤害
    /// </summary>
    private void ApplyMeleeDamage(CharacterStats attacker, CharacterStats target) {
        // 计算基础伤害（可以后续扩展为DND5E规则）
        int damage = Random.Range(attacker.level * 2, attacker.level * 4 + 5);

        Debug.Log($"💥 {attacker.GetDisplayName()} 对 {target.GetDisplayName()} 造成 {damage} 点近战伤害");

        // 应用伤害
        target.currentHitPoints = Mathf.Max(0, target.currentHitPoints - damage);

        // 播放目标受击动画
        DND_CharacterAdapter targetAdapter = target.GetComponent<DND_CharacterAdapter>();
        if (targetAdapter != null) {
            targetAdapter.PlayHitAnimation();
        }

        // 检查死亡
        if (target.currentHitPoints <= 0) {
            Debug.Log($"💀 {target.GetDisplayName()} 被击败");
            if (targetAdapter != null) {
                targetAdapter.PlayDeathAnimation();
            }
        }
    }

    /// <summary>
    /// 应用远程伤害
    /// </summary>
    private void ApplyRangedDamage(CharacterStats attacker, CharacterStats target) {
        // 远程攻击伤害稍低但更稳定
        int damage = Random.Range(attacker.level * 1, attacker.level * 3 + 3);

        Debug.Log($"🎯 {attacker.GetDisplayName()} 对 {target.GetDisplayName()} 造成 {damage} 点远程伤害");

        // 应用伤害
        target.currentHitPoints = Mathf.Max(0, target.currentHitPoints - damage);

        // 播放目标受击动画
        DND_CharacterAdapter targetAdapter = target.GetComponent<DND_CharacterAdapter>();
        if (targetAdapter != null) {
            targetAdapter.PlayHitAnimation();
        }

        // 检查死亡
        if (target.currentHitPoints <= 0) {
            Debug.Log($"💀 {target.GetDisplayName()} 被击败");
            if (targetAdapter != null) {
                targetAdapter.PlayDeathAnimation();
            }
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
