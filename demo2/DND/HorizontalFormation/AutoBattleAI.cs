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

        // 在类内定义最小的战斗行动类型（仅包含目标）
        [System.Serializable]
        private class BattleAction
        {
            public CharacterStats target;
        }

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
                // 包含生命值>0的活跃角色，以及处于昏迷（Unconscious）的倒地角色
                if (character.currentHitPoints > 0 || character.HasStatusEffect(StatusEffectType.Unconscious))
                {
                    allCombatants.Add(character);
                    Debug.Log($"🎯 添加参战角色: {character.GetDisplayName()} - 阵营: {character.battleSide} - 血量: {character.currentHitPoints}");
                }
                else
                {
                    Debug.Log($"🎯 跳过不可参战角色: {character.GetDisplayName()} - 血量: {character.currentHitPoints}");
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
            if (currentEntry == null)
            {
                AdvanceToNextTurn();
                return;
            }

            // 如果当前条目无法行动且不是处于倒地（Unconscious）状态，则跳过
            if (!currentEntry.CanAct() && !(currentEntry.character != null && currentEntry.character.HasStatusEffect(StatusEffectType.Unconscious)))
            {
                AdvanceToNextTurn();
                return;
            }

            isProcessingTurn = true;
            CharacterStats character = currentEntry.character;

            // 如果当前角色处于昏迷（倒地），则本回合不做AI决策，改为执行一次死豁（按回合触发），然后结束其回合
            if (character != null && character.HasStatusEffect(StatusEffectType.Unconscious))
            {
                if (showAIThoughts) Debug.Log($"=== {character.GetDisplayName()} 倒地状态 - 执行死豁 (按回合) ===");
                character.PerformDeathSaveTick();
                currentEntry.MarkAsActed();
                AdvanceToNextTurn();
                isProcessingTurn = false;
                return;
            }

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
                        ProcessAttackHit(attacker, action.target, true);
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
                        ProcessAttackHit(attacker, action.target, false);
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
        private void ProcessAttackHit(CharacterStats attacker, CharacterStats target, bool isMeleeAttack)
        {
            if (attacker == null || target == null) return;

            // 如果目标处于昏迷（倒地），近战攻击获得优势，远程攻击则为劣势
            int advantageFlag = 0;
            if (target.HasStatusEffect(StatusEffectType.Unconscious))
            {
                advantageFlag = isMeleeAttack ? 1 : -1;
                Debug.Log($"[DEBUG] 目标处于昏迷：设置攻击掷骰优势标志 = {advantageFlag} (1=优势, -1=劣势)");
            }

            // 执行攻击检定和伤害计算（传入优势/劣势标志）
            var attackResult = HorizontalCombatRules.ResolveAttack(attacker, target, advantageFlag);

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

                // 如果目标处于昏迷，则按规则处理死豁失败计数（伤害不再让角色掉到负HP）
                if (target.HasStatusEffect(StatusEffectType.Unconscious))
                {
                    // 普通伤害计一次失败，暴击计两次
                    target.RegisterUnconsciousHit(isCritical);

                    // 仍然触发显示与事件（UI 需要显示伤害或 MISS）
                    var damageChannel = EventChannelManager.Instance?.GetChannel<DamageEventChannel_SO>("DamageEventChannel");
                    damageChannel?.RaiseEvent(target, attacker, damage, isCritical);

                    // 重要：倒地状态不再播放受击动画，避免覆盖昏迷循环
                    //DND_CharacterAdapter targetAdapter = target.GetComponent<DND_CharacterAdapter>();
                    //targetAdapter?.PlayHitAnimation();
                }
                else
                {
                    // 正常应用伤害
                    target.TakeDamage(damage, DamageType.Bludgeoning, isCritical);

                    // 触发伤害事件用于UI更新 - 第一个参数是受害者，第二个是攻击者
                    var damageChannel = EventChannelManager.Instance?.GetChannel<DamageEventChannel_SO>("DamageEventChannel");
                    damageChannel?.RaiseEvent(target, attacker, damage, isCritical);

                    // 仅当目标仍存活且未进入倒地，才播放受击动画，避免覆盖死亡/昏迷动画
                    if (target.currentHitPoints > 0 && !target.HasStatusEffect(StatusEffectType.Unconscious))
                    {
                        DND_CharacterAdapter targetAdapter = target.GetComponent<DND_CharacterAdapter>();
                        targetAdapter?.PlayHitAnimation();
                    }
                }
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
            // 修复：只有当一方在先攻列表中完全不存在时，战斗才结束
            bool playerSideExists = initiativeOrder.Any(e => e.initialSide == BattleSide.Player);
            bool enemySideExists = initiativeOrder.Any(e => e.initialSide == BattleSide.Enemy);

            Debug.Log($"[IsBattleOver] 阵营存在检查 - 玩家: {playerSideExists}, 敌人: {enemySideExists}");

            // 如果一方已经不存在于列表中，则战斗结束
            return !playerSideExists || !enemySideExists;
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
            if (idleManager != null)
            {
                idleManager.OnBattleCompleted(playerVictory);
            }
            else
            {
                Debug.LogWarning("未找到 IdleGameManager，无法通知战斗结束。");
            }
        }

        /// <summary>
        /// 新增：从先攻列表中移除一个角色
        /// </summary>
        public void RemoveCharacterFromInitiative(CharacterStats characterToRemove)
        {
            if (characterToRemove == null)
            {
                Debug.LogWarning("RemoveCharacterFromInitiative 调用时传入了空角色");
                return;
            }

            // 移除所有与该角色相关的条目
            int removedCount = initiativeOrder.RemoveAll(e => e == null || e.character == null || e.character == characterToRemove);
            Debug.Log($"[Initiative] 已从先攻列表移除 {removedCount} 条与 {characterToRemove.GetDisplayName()} 相关的条目");

            // 调整 currentTurnIndex，避免越界
            if (currentTurnIndex >= initiativeOrder.Count)
            {
                currentTurnIndex = Mathf.Clamp(currentTurnIndex, 0, Mathf.Max(initiativeOrder.Count - 1, 0));
            }

            // 如果列表为空或战斗双方之一已不存在，则结束战斗
            if (initiativeOrder.Count == 0 || IsBattleOver())
            {
                EndBattle();
                return;
            }
        }

        /// <summary>
        /// 为当前角色决定一个最优行动（简化版：选择最近的敌方目标进行攻击）。
        /// </summary>
        private BattleAction DecideBestAction(CharacterStats actor)
        {
            if (actor == null) return null;
            var target = FindBestTarget(actor);
            if (target == null) return null;
            return new BattleAction { target = target };
        }

        /// <summary>
        /// 选择一个最佳攻击目标：
        /// - 优先选择敌方阵营且存活的目标（HP>0）；
        /// - 若没有存活目标，选择处于昏迷的敌方目标；
        /// - 在候选中选择距离最近的一个。
        /// </summary>
        private CharacterStats FindBestTarget(CharacterStats actor)
        {
            var all = FindObjectsOfType<CharacterStats>();
            if (all == null || all.Length == 0) return null;

            // 敌方候选（活着的）
            var livingOpponents = all
                .Where(c => c != null && c.battleSide != actor.battleSide && c.currentHitPoints > 0)
                .ToList();

            // 敌方候选（昏迷的）
            var downedOpponents = all
                .Where(c => c != null && c.battleSide != actor.battleSide && c.currentHitPoints <= 0 && c.HasStatusEffect(StatusEffectType.Unconscious))
                .ToList();

            List<CharacterStats> pool = livingOpponents.Count > 0 ? livingOpponents : downedOpponents;
            if (pool == null || pool.Count == 0) return null;

            CharacterStats best = null;
            float bestDist = float.MaxValue;
            foreach (var c in pool)
            {
                float d = Vector3.Distance(actor.transform.position, c.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
            return best;
        }
    }
}
