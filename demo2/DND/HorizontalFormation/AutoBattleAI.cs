using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using demo2.DND;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 自动战斗AI系统 - 线性阵型版本
    /// 使用DOTween+SpineEvent事件驱动，摒弃协程方式
    /// </summary>
    public class AutoBattleAI : MonoBehaviour
    {
        [Header("AI设置")]
        public bool enableAutoBattle = true;
        public float decisionDelay = 1.0f;
        public bool showAIThoughts = true;

        [Header("战术优先级")]
        [Range(0, 1)] public float healingPriority = 0.8f;
        [Range(0, 1)] public float positioningPriority = 0.6f;
        [Range(0, 1)] public float offensivePriority = 0.7f;
        [Range(0, 1)] public float defensivePriority = 0.5f;

        [Header("先攻系统")]
        public List<InitiativeEntry> initiativeOrder = new List<InitiativeEntry>();
        public int currentTurnIndex;
        public bool isBattleActive;

        private bool isProcessingTurn;
        private float turnTimer;

        /// <summary>
        /// 开始战斗序列 - 执行先攻检定并开始回合制战斗
        /// </summary>
        public void StartBattleSequence()
        {
            Debug.Log("🎯 ========== AutoBattleAI.StartBattleSequence 被调用 ==========");

            if (isBattleActive)
            {
                Debug.LogWarning("战斗已经在进行中！");
                return;
            }

            // 收集所有参战角色
            List<CharacterStats> allCombatants = new List<CharacterStats>();
            CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();

            Debug.Log($"🎯 找到角色总数: {allCharacters.Length}");

            foreach (CharacterStats character in allCharacters)
            {
                if (character.currentHitPoints > 0)
                {
                    allCombatants.Add(character);
                    Debug.Log($"🎯 添加参战角色: {character.GetDisplayName()} - 阵营: {character.battleSide} - 血量: {character.currentHitPoints}");
                }
                else
                {
                    Debug.Log($"🎯 跳过死亡角色: {character.GetDisplayName()} - 血量: {character.currentHitPoints}");
                }
            }

            if (allCombatants.Count < 2)
            {
                Debug.LogWarning($"🎯 参战角色不足，无法开始战斗。当前参战角色数: {allCombatants.Count}");
                return;
            }

            // 执行先攻检定并排序
            Debug.Log("🎯 开始战斗！执行先攻检定...");
            initiativeOrder = HorizontalCombatRules.RollAndSortInitiative(allCombatants);
            currentTurnIndex = 0;
            isBattleActive = true;
            isProcessingTurn = false;
            turnTimer = 0f;

            Debug.Log("🎯 先攻顺序：");
            for (int i = 0; i < initiativeOrder.Count; i++)
            {
                Debug.Log($"🎯 {i + 1}. {initiativeOrder[i].character.GetDisplayName()} (先攻值: {initiativeOrder[i].initiativeRoll})");
            }

            // 开始第一个回合
            Debug.Log("🎯 准备开始第一个回合...");
            StartNextTurn();
        }

        void Update()
        {
            if (!isBattleActive || !enableAutoBattle) return;

            // 处理回合计时器
            if (!isProcessingTurn)
            {
                turnTimer += Time.deltaTime;
                if (turnTimer >= decisionDelay)
                {
                    ProcessCurrentTurn();
                }
            }
        }

        /// <summary>
        /// 开始下一个回合
        /// </summary>
        private void StartNextTurn()
        {
            if (!isBattleActive) return;

            // 检查战斗是否结束
            if (IsBattleOver())
            {
                EndBattle();
                return;
            }

            // 重置回合计时器
            turnTimer = 0f;
            isProcessingTurn = false;

            // 获取当前行动角色
            InitiativeEntry currentEntry = GetCurrentInitiativeEntry();
            if (currentEntry != null)
            {
                Debug.Log($"轮到 {currentEntry.character.GetDisplayName()} 行动 (先攻顺序 {currentTurnIndex + 1})");
            }
        }

        /// <summary>
        /// 处理当前回合
        /// </summary>
        private void ProcessCurrentTurn()
        {
            if (isProcessingTurn) return;

            InitiativeEntry currentEntry = GetCurrentInitiativeEntry();
            if (currentEntry == null || !currentEntry.CanAct())
            {
                AdvanceToNextTurn();
                return;
            }

            isProcessingTurn = true;
            CharacterStats character = currentEntry.character;

            if (showAIThoughts)
            {
                Debug.Log($"=== {character.GetDisplayName()} 的回合开始 ===");
            }

            // AI决策流程
            BattleAction chosenAction = DecideBestAction(character);

            if (chosenAction != null)
            {
                // 使用事件驱动方式执行战斗行动
                ExecuteBattleActionEvent(character, chosenAction, () => {
                    // 行动完成回调
                    currentEntry.MarkAsActed();
                    AdvanceToNextTurn();
                });
            }
            else
            {
                // 无有效行动，直接结束回合
                currentEntry.MarkAsActed();
                AdvanceToNextTurn();
            }
        }

        /// <summary>
        /// 事件驱动的战斗行动执行
        /// </summary>
        private void ExecuteBattleActionEvent(CharacterStats attacker, BattleAction action, System.Action onComplete)
        {
            if (attacker == null || action == null || action.target == null)
            {
                onComplete?.Invoke();
                return;
            }

            Debug.Log($"[DEBUG] ========== ExecuteBattleActionEvent 开始 ==========");
            Debug.Log($"[DEBUG] 攻击者: {attacker.GetDisplayName()}, 目标: {action.target.GetDisplayName()}");

            // 获取攻击者的动画适配器
            DND_CharacterAdapter attackerAdapter = attacker.GetComponent<DND_CharacterAdapter>();
            if (attackerAdapter == null)
            {
                Debug.LogError($"角色 {attacker.GetDisplayName()} 缺少 DND_CharacterAdapter 组件！");
                onComplete?.Invoke();
                return;
            }

            // 判断攻击类型：前排=近战，后排=远程
            bool isMeleeAttack = IsCharacterInFrontRow(attacker);

            Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 攻击类型判断结果: {(isMeleeAttack ? "近战攻击" : "远程攻击")}");

            if (isMeleeAttack)
            {
                Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 开始执行近战攻击序列");
                // 近战攻击：移动+攻击+返回
                attackerAdapter.ExecuteMeleeAttack(
                    action.target.transform,
                    onAttackHit: () => {
                        // SpineEvent触发攻击命中
                        Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 近战攻击命中回调触发");
                        ProcessAttackHit(attacker, action.target);
                    },
                    onComplete: () => {
                        // 攻击动画完成
                        Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 近战攻击完成回调触发");
                        onComplete?.Invoke();
                    }
                );
            }
            else
            {

                // 远程攻击：原地攻击
                attackerAdapter.ExecuteRangedAttack(
                    action.target.transform,
                    onAttackHit: () => {
                        // SpineEvent触发攻击命中
                        Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 远程攻击命中回调触发");
                        ProcessAttackHit(attacker, action.target);
                    },
                    onComplete: () => {
                        // 攻击动画完成
                        Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 远程攻击完成回调触发");
                        onComplete?.Invoke();
                    }
                );
            }

            Debug.Log($"[DEBUG] ========== ExecuteBattleActionEvent 结束 ==========");
        }

        /// <summary>
        /// 处理攻击命中 - 由SpineEvent触发
        /// </summary>
        private void ProcessAttackHit(CharacterStats attacker, CharacterStats target)
        {
            if (attacker == null || target == null) return;

            // 执行攻击检定和伤害计算
            var attackResult = HorizontalCombatRules.ResolveAttack(attacker, target);

            if (attackResult.isHit)
            {
                // 命中：计算伤害
                int damage = attackResult.damage;
                bool isCritical = attackResult.isCritical;

                if (showAIThoughts)
                {
                    string critText = isCritical ? " (暴击!)" : "";
                    Debug.Log($"{attacker.GetDisplayName()} 攻击 {target.GetDisplayName()}: 命中! 造成 {damage} 点伤害{critText}");
                }

                // 应用伤害
                target.TakeDamage(damage);

                // 触发伤害事件用于UI更新 - 第一个参数是受害者，第二个是攻击者
                var damageChannel = EventChannelManager.Instance?.GetChannel<DamageEventChannel_SO>("DamageEventChannel");
                damageChannel?.RaiseEvent(target, attacker, damage, isCritical);

                // 播放受击动画
                DND_CharacterAdapter targetAdapter = target.GetComponent<DND_CharacterAdapter>();
                targetAdapter?.PlayHitAnimation();
            }
            else
            {
                // 未命中
                if (showAIThoughts)
                {
                    Debug.Log($"{attacker.GetDisplayName()} 攻击 {target.GetDisplayName()}: 未命中!");
                }

                // 播放闪避动画
                DND_CharacterAdapter targetAdapter = target.GetComponent<DND_CharacterAdapter>();
                targetAdapter?.PlayDodgeAnimation();

                // 显示 MISS 提示（调用 CharacterStats 的接口）
                try
                {
                    target.ShowMiss();
                    Debug.Log($"AutoBattleAI.ProcessAttackHit: 已调用 ShowMiss() for {target.GetDisplayName()}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"AutoBattleAI.ProcessAttackHit: 调用 ShowMiss 时异常 - {ex}");
                }
            }
        }

        /// <summary>
        /// 判断角色是否在前排
        /// </summary>
        private bool IsCharacterInFrontRow(CharacterStats character)
        {
            Debug.Log($"[DEBUG] 判断角色 {character.GetDisplayName()} 的位置");

            // 通过BattlePositionComponent组件判断位置
            BattlePositionComponent positionComponent = character.GetComponent<BattlePositionComponent>();
            if (positionComponent != null)
            {
                Debug.Log($"[DEBUG] {character.GetDisplayName()} 找到BattlePositionComponent，rowPosition: {positionComponent.rowPosition}");
                return positionComponent.rowPosition == RowPosition.Front;
            }
            else
            {
                Debug.LogWarning($"[DEBUG] {character.GetDisplayName()} 没有BattlePositionComponent组件！");
            }

            // 备用方案：通过世界坐标判断（前排X坐标更靠前）
            HorizontalBattleFormationManager formationManager = FindObjectOfType<HorizontalBattleFormationManager>();
            if (formationManager != null)
            {
                bool isFrontRow = formationManager.IsCharacterInFrontRow(character);
                Debug.Log($"[DEBUG] {character.GetDisplayName()} 通过FormationManager判断，isFrontRow: {isFrontRow}");
                return isFrontRow;
            }
            else
            {
                Debug.LogWarning($"[DEBUG] 找不到HorizontalBattleFormationManager！");
            }

            // 默认为近战
            Debug.Log($"[DEBUG] {character.GetDisplayName()} 使用默认判断：前排（近战）");
            return true;
        }

        /// <summary>
        /// 获取当前行动的先攻条目
        /// </summary>
        private InitiativeEntry GetCurrentInitiativeEntry()
        {
            if (currentTurnIndex >= 0 && currentTurnIndex < initiativeOrder.Count)
            {
                return initiativeOrder[currentTurnIndex];
            }
            return null;
        }

        /// <summary>
        /// 前进到下一个回合
        /// </summary>
        private void AdvanceToNextTurn()
        {
            currentTurnIndex++;
            if (currentTurnIndex >= initiativeOrder.Count)
            {
                currentTurnIndex = 0;
                ResetRoundState();
                Debug.Log("新的战斗轮次开始");
            }

            // 开始下一个回合
            StartNextTurn();
        }

        /// <summary>
        /// 重置轮次状态
        /// </summary>
        private void ResetRoundState()
        {
            foreach (InitiativeEntry entry in initiativeOrder)
            {
                entry.ResetTurnState();
            }
        }

        /// <summary>
        /// 检查战斗是否结束
        /// </summary>
        private bool IsBattleOver()
        {
            bool hasPlayerAlive = false;
            bool hasEnemyAlive = false;

            foreach (InitiativeEntry entry in initiativeOrder)
            {
                if (entry.character != null && entry.character.currentHitPoints > 0)
                {
                    if (entry.character.battleSide == BattleSide.Player)
                    {
                        hasPlayerAlive = true;
                    }
                    else if (entry.character.battleSide == BattleSide.Enemy)
                    {
                        hasEnemyAlive = true;
                    }
                }
            }

            return !hasPlayerAlive || !hasEnemyAlive;
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        private void EndBattle()
        {
            isBattleActive = false;
            isProcessingTurn = false;

            bool playerVictory = initiativeOrder.Any(entry =>
                entry.character.battleSide == BattleSide.Player &&
                entry.character.currentHitPoints > 0);

            if (playerVictory)
            {
                Debug.Log("玩家胜利！");
            }
            else
            {
                Debug.Log("玩家失败！");
            }

            // 通知IdleGameManager战斗结束
            IdleGameManager idleManager = FindObjectOfType<IdleGameManager>();
            idleManager?.OnBattleCompleted(playerVictory);
        }

        /// <summary>
        /// AI决策逻辑
        /// </summary>
        private BattleAction DecideBestAction(CharacterStats character)
        {
            if (character == null) return null;

            // 获取可攻击的目标列表
            List<CharacterStats> availableTargets = GetAvailableTargets(character);

            if (availableTargets.Count == 0)
            {
                if (showAIThoughts)
                {
                    Debug.Log($"{character.GetDisplayName()} 没有可攻击的目标");
                }
                return null;
            }

            // 选择最优目标
            CharacterStats bestTarget = SelectBestTarget(character, availableTargets);

            if (bestTarget != null)
            {
                return new BattleAction
                {
                    actionType = BattleActionType.Attack,
                    target = bestTarget,
                    description = $"攻击 {bestTarget.GetDisplayName()}"
                };
            }

            return null;
        }

        /// <summary>
        /// 获取可攻击目标列表
        /// </summary>
        private List<CharacterStats> GetAvailableTargets(CharacterStats attacker)
        {
            List<CharacterStats> targets = new List<CharacterStats>();
            BattleSide enemySide = attacker.battleSide == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;

            foreach (InitiativeEntry entry in initiativeOrder)
            {
                CharacterStats character = entry.character;
                if (character != null &&
                    character.currentHitPoints > 0 &&
                    character.battleSide == enemySide)
                {
                    // 检查攻击距离限制
                    if (CanAttackTarget(attacker, character))
                    {
                        targets.Add(character);
                    }
                }
            }

            return targets;
        }

        /// <summary>
        /// 检查是否可以攻击目标
        /// </summary>
        private bool CanAttackTarget(CharacterStats attacker, CharacterStats target)
        {
            bool attackerInFront = IsCharacterInFrontRow(attacker);
            bool targetInFront = IsCharacterInFrontRow(target);

            // 近战角色只能攻击敌方前排，除非敌方前排全灭
            if (attackerInFront)
            {
                if (targetInFront) return true;

                // 检查敌方前排是否全灭
                bool enemyFrontRowExists = HasEnemyInFrontRow(target.battleSide);
                return !enemyFrontRowExists;
            }

            // 远程角色可以攻击任何目标
            return true;
        }

        /// <summary>
        /// 检查指定阵营是否还有前排角色存活
        /// </summary>
        private bool HasEnemyInFrontRow(BattleSide side)
        {
            foreach (InitiativeEntry entry in initiativeOrder)
            {
                if (entry.character != null &&
                    entry.character.currentHitPoints > 0 &&
                    entry.character.battleSide == side &&
                    IsCharacterInFrontRow(entry.character))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 选择最优攻击目标
        /// </summary>
        private CharacterStats SelectBestTarget(CharacterStats attacker, List<CharacterStats> targets)
        {
            if (targets.Count == 0) return null;
            if (targets.Count == 1) return targets[0];

            // 优先攻击血量最少的敌人
            CharacterStats bestTarget = targets[0];
            foreach (CharacterStats target in targets)
            {
                if (target.currentHitPoints < bestTarget.currentHitPoints)
                {
                    bestTarget = target;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// 战斗行动数据结构
        /// </summary>
        public class BattleAction
        {
            public BattleActionType actionType;
            public CharacterStats target;
            public string description;
        }

        public enum BattleActionType
        {
            Attack,
            Defend,
            Cast,
            Move,
            Wait
        }
    }
}
