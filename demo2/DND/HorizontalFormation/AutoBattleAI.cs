using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 自动战斗AI系统 - 线性阵型版本
/// 实现挂机模式的智能战斗决策和攻击动画
/// </summary>
public class AutoBattleAI : MonoBehaviour {
    [Header("AI设置")]
    public bool enableAutoBattle = true;
    public float decisionDelay = 1.0f; // AI决策延迟
    public bool showAIThoughts = true; // 显示AI思考过程

    [Header("战术优先级")]
    [Range(0, 1)] public float healingPriority = 0.8f;
    [Range(0, 1)] public float positioningPriority = 0.6f;
    [Range(0, 1)] public float offensivePriority = 0.7f;
    [Range(0, 1)] public float defensivePriority = 0.5f;

    [Header("先攻系统")]
    public List<InitiativeEntry> initiativeOrder = new List<InitiativeEntry>(); // 先攻顺序列表
    public int currentTurnIndex; // 当前回合索引
    public bool isBattleActive; // 战斗是否激活

    private bool isProcessingTurn;
    private Coroutine battleSequenceCoroutine;

    /// <summary>
    /// 开始战斗序列 - 执行先攻检定并开始回合制战斗
    /// </summary>
    public void StartBattleSequence() {
        if (isBattleActive) {
            Debug.LogWarning("战斗已经在进行中！");
            return;
        }

        // 收集所有参战角色
        List<CharacterStats> allCombatants = new List<CharacterStats>();
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();

        foreach (CharacterStats character in allCharacters) {
            if (character.currentHitPoints > 0) {
                allCombatants.Add(character);
            }
        }

        if (allCombatants.Count < 2) {
            Debug.LogWarning("参战角色不足，无法开始战斗");
            return;
        }

        // 执行先攻检定并排序
        Debug.Log("🚀 开始战斗！执行先攻检定...");
        initiativeOrder = HorizontalCombatRules.RollAndSortInitiative(allCombatants);
        currentTurnIndex = 0;
        isBattleActive = true;

        // 开始回合制战斗循环
        if (battleSequenceCoroutine != null) {
            StopCoroutine(battleSequenceCoroutine);
        }
        battleSequenceCoroutine = StartCoroutine(BattleSequenceLoop());
    }

    /// <summary>
    /// 战斗序列循环 - 按先攻顺序执行每个角色的回合
    /// </summary>
    private IEnumerator BattleSequenceLoop() {
        while (isBattleActive && enableAutoBattle) {
            // 检查战斗是否结束
            if (IsBattleOver()) {
                EndBattle();
                yield break;
            }

            // 获取当前行动角色
            InitiativeEntry currentEntry = GetCurrentInitiativeEntry();

            if (currentEntry != null && currentEntry.CanAct()) {
                Debug.Log($"⚡ 轮到 {currentEntry.character.GetDisplayName()} 行动 (先攻顺序 {currentTurnIndex + 1})");

                // 执行角色回合
                yield return StartCoroutine(ProcessCharacterTurn(currentEntry));

                // 标记已行动
                currentEntry.MarkAsActed();
            }

            // 移动到下一个角色
            AdvanceToNextTurn();

            // 检查是否完成一轮，重置行动状态
            if (currentTurnIndex == 0) {
                ResetRoundState();
                Debug.Log("📝 新的战斗轮次开始");
            }

            yield return new WaitForSeconds(0.1f); // 短暂延迟避免卡死
        }
    }

    /// <summary>
    /// 处理角色回合
    /// </summary>
    private IEnumerator ProcessCharacterTurn(InitiativeEntry initiativeEntry) {
        CharacterStats character = initiativeEntry.character;

        if (character == null || character.currentHitPoints <= 0) {
            yield break;
        }

        isProcessingTurn = true;

        if (showAIThoughts) {
            Debug.Log($"=== {character.GetDisplayName()} 的回合开始 ===");
        }

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
    /// 获取当前行动的先攻条目
    /// </summary>
    private InitiativeEntry GetCurrentInitiativeEntry() {
        if (currentTurnIndex >= 0 && currentTurnIndex < initiativeOrder.Count) {
            return initiativeOrder[currentTurnIndex];
        }
        return null;
    }

    /// <summary>
    /// 前进到下一个回合
    /// </summary>
    private void AdvanceToNextTurn() {
        currentTurnIndex++;
        if (currentTurnIndex >= initiativeOrder.Count) {
            currentTurnIndex = 0; // 回到第一个角色，开始新轮次
        }
    }

    /// <summary>
    /// 重置轮次状态
    /// </summary>
    private void ResetRoundState() {
        foreach (InitiativeEntry entry in initiativeOrder) {
            entry.ResetTurnState();
        }
    }

    /// <summary>
    /// 检查战斗是否结束
    /// </summary>
    private bool IsBattleOver() {
        bool hasPlayerAlive = false;
        bool hasEnemyAlive = false;

        foreach (InitiativeEntry entry in initiativeOrder) {
            if (entry.character != null && entry.character.currentHitPoints > 0) {
                if (entry.character.battleSide == BattleSide.Player) {
                    hasPlayerAlive = true;
                } else {
                    hasEnemyAlive = true;
                }
            }
        }

        return !hasPlayerAlive || !hasEnemyAlive;
    }

    /// <summary>
    /// 结束战斗
    /// </summary>
    private void EndBattle() {
        isBattleActive = false;
        isProcessingTurn = false;

        if (battleSequenceCoroutine != null) {
            StopCoroutine(battleSequenceCoroutine);
            battleSequenceCoroutine = null;
        }

        // 确定胜利方
        bool playerVictory = initiativeOrder.Any(e => e.character != null &&
                                                      e.character.currentHitPoints > 0 &&
                                                      e.character.battleSide == BattleSide.Player);

        Debug.Log(playerVictory ? "🎉 玩家胜利！" : "💀 敌人胜利！");

        // 清理先攻列表
        initiativeOrder.Clear();
        currentTurnIndex = 0;
    }

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
    private IEnumerator ProcessAutoBattleTurn(CharacterStats character) {
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
                return false; // 默认为远程，可根据需要调整
            default:
                return true; // 默认近战
        }
    }

    /// <summary>
    /// 根据战术优先级选择攻击目标
    /// 规则：前排优先 > 血量最少
    /// </summary>
    private CharacterStats ChooseBestAttackTarget(List<CharacterStats> targets) {
        if (targets.Count == 0) return null;
        if (targets.Count == 1) return targets[0];

        // 按战术优先级分组：前排 > 后排
        var frontLineTargets = new List<CharacterStats>();
        var backLineTargets = new List<CharacterStats>();

        foreach (CharacterStats target in targets) {
            bool isFrontLine = IsInFrontLine(target);

            if (isFrontLine) {
                frontLineTargets.Add(target);
            } else {
                backLineTargets.Add(target);
            }
        }

        Debug.Log($"🎯 战术分析: 前排目标{frontLineTargets.Count}个, 后排目标{backLineTargets.Count}个");

        // 优先攻击前排，如果前排没有存活角色则攻击后排
        if (frontLineTargets.Count > 0) {
            CharacterStats selectedTarget = frontLineTargets.OrderBy(t => t.currentHitPoints).First();
            Debug.Log($"✅ 选择前排目标: {selectedTarget.GetDisplayName()} (血量:{selectedTarget.currentHitPoints})");
            return selectedTarget;
        } else {
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

        // 默认判断为前排
        Debug.LogWarning($"⚠️ 无法判断 {character.GetDisplayName()} 的位置，默认为前排");
        return true;
    }

    /// <summary>
    /// 通过位置枚举判断是否为前排
    /// </summary>
    private bool IsPositionInFrontLine(HorizontalPosition position) {
        switch (position) {
            case HorizontalPosition.PlayerFrontLeft:
            case HorizontalPosition.PlayerFrontCenter:
            case HorizontalPosition.PlayerFrontRight:
            case HorizontalPosition.EnemyFrontLeft:
            case HorizontalPosition.EnemyFrontCenter:
            case HorizontalPosition.EnemyFrontRight:
                return true;
            case HorizontalPosition.PlayerBackLeft:
            case HorizontalPosition.PlayerBackCenter:
            case HorizontalPosition.PlayerBackRight:
            case HorizontalPosition.EnemyBackLeft:
            case HorizontalPosition.EnemyBackCenter:
            case HorizontalPosition.EnemyBackRight:
                return false;
            default:
                return true; // 默认前排
        }
    }

    /// <summary>
    /// 选择最佳行动
    /// </summary>
    private BattleAction ChooseBestAction(List<BattleAction> actions) {
        if (actions.Count == 0) return null;
        if (actions.Count == 1) return actions[0];

        // 按优先级排序，选择最高优先级的行动
        return actions.OrderByDescending(a => a.priority).First();
    }

    /// <summary>
    /// 执行战斗行动
    /// </summary>
    private void ExecuteBattleAction(CharacterStats actor, BattleAction action) {
        if (action == null || actor == null) return;

        Debug.Log($"🎬 执行行动: {actor.GetDisplayName()} -> {action.description}");

        switch (action.type) {
            case BattleActionType.MeleeAttack:
                ExecuteMeleeAttack(actor, action.target);
                break;

            case BattleActionType.RangedAttack:
                ExecuteRangedAttack(actor, action.target);
                break;

            case BattleActionType.Defend:
                ExecuteDefend(actor);
                break;

            default:
                Debug.LogWarning($"未知的战斗行动类型: {action.type}");
                break;
        }
    }

    /// <summary>
    /// 执行近战攻击
    /// </summary>
    private void ExecuteMeleeAttack(CharacterStats attacker, CharacterStats target) {
        if (attacker == null || target == null) return;

        Debug.Log($"⚔️ {attacker.GetDisplayName()} 对 {target.GetDisplayName()} 发起近战攻击");

        // 播放攻击动画
        DND_CharacterAdapter adapter = attacker.GetComponent<DND_CharacterAdapter>();
        if (adapter != null) {
            adapter.PlayAttackAnimation();
        }

        // 执行攻击检定和伤害计算
        bool hitSuccess = HorizontalCombatRules.RollAttackCheck(attacker, target);
        if (hitSuccess) {
            int damage = HorizontalCombatRules.RollDamage(attacker, target);
            target.TakeDamage(damage);
            Debug.Log($"💥 攻击命中！造成 {damage} 点伤害，{target.GetDisplayName()} 剩余血量: {target.currentHitPoints}");
        } else {
            Debug.Log($"🛡️ 攻击未命中！");
        }
    }

    /// <summary>
    /// 执行远程攻击
    /// </summary>
    private void ExecuteRangedAttack(CharacterStats attacker, CharacterStats target) {
        if (attacker == null || target == null) return;

        Debug.Log($"🏹 {attacker.GetDisplayName()} 对 {target.GetDisplayName()} 发起远程攻击");

        // 播放攻击动画
        DND_CharacterAdapter adapter = attacker.GetComponent<DND_CharacterAdapter>();
        if (adapter != null) {
            adapter.PlayAttackAnimation();
        }

        // 执行攻击检定和伤害计算
        bool hitSuccess = HorizontalCombatRules.RollAttackCheck(attacker, target);
        if (hitSuccess) {
            int damage = HorizontalCombatRules.RollDamage(attacker, target);
            target.TakeDamage(damage);
            Debug.Log($"💥 攻击命中！造成 {damage} 点伤害，{target.GetDisplayName()} 剩余血量: {target.currentHitPoints}");
        } else {
            Debug.Log($"🛡️ 远程攻击未命中！");
        }
    }

    /// <summary>
    /// 执行防御动作
    /// </summary>
    private void ExecuteDefend(CharacterStats character) {
        Debug.Log($"🛡️ {character.GetDisplayName()} 采取防御姿态");

        // 播放防御动画或保持待机
        DND_CharacterAdapter adapter = character.GetComponent<DND_CharacterAdapter>();
        if (adapter != null) {
            adapter.PlayIdleAnimation();
        }
    }

    /// <summary>
    /// 战斗行动数据结构
    /// </summary>
    [System.Serializable]
    public class BattleAction {
        public BattleActionType type;
        public float priority;
        public CharacterStats target;
        public Vector3 targetPosition;
        public string description;
    }

    /// <summary>
    /// 战斗行动类型枚举
    /// </summary>
    public enum BattleActionType {
        MeleeAttack,    // 近战攻击
        RangedAttack,   // 远程攻击
        Spell,          // 法术
        Move,           // 移动
        Defend,         // 防御
        Item            // 使用物品
    }
}
