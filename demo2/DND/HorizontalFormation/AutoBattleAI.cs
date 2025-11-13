using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 自动战斗AI系统 - 线性阵型版本（事件驱动，无协程）
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

        [System.Serializable]
        private class BattleAction
        {
            public CharacterStats target;
        }

        public void StartBattleSequence()
        {
            Debug.Log("🎯 ========== AutoBattleAI.StartBattleSequence 被调用 ==========");

            if (isBattleActive)
            {
                Debug.LogWarning("战斗已经在进行中！");
                return;
            }

            // 收集所有参战角色
            var allCombatants = new List<CharacterStats>();
            CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();
            Debug.Log($"🎯 找到角色总数: {allCharacters.Length}");

            foreach (var character in allCharacters)
            {
                if (character.CurrentHitPoints > 0 || character.HasStatusEffect(StatusEffectType.Unconscious))
                {
                    allCombatants.Add(character);
                    Debug.Log($"🎯 添加参战角色: {character.GetDisplayName()} - 阵营: {character.battleSide} - 血量: {character.CurrentHitPoints}");
                }
                else
                {
                    Debug.Log($"🎯 跳过不可参战角色: {character.GetDisplayName()} - 血量: {character.CurrentHitPoints}");
                }
            }

            if (allCombatants.Count < 2)
            {
                Debug.LogWarning($"🎯 参战角色不足，无法开始战斗。目前参战角色数: {allCombatants.Count}");
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

            Debug.Log("🎯 准备开始第一个回合...");
            StartNextTurn();
        }

        private void Update()
        {
            if (!isBattleActive || !enableAutoBattle) return;

            if (!isProcessingTurn)
            {
                turnTimer += Time.deltaTime;
                if (turnTimer >= decisionDelay)
                {
                    ProcessCurrentTurn();
                }
            }
        }

        private void StartNextTurn()
        {
            if (!isBattleActive) return;

            if (IsBattleOver())
            {
                EndBattle();
                return;
            }

            turnTimer = 0f;
            isProcessingTurn = false;

            var currentEntry = GetCurrentInitiativeEntry();
            if (currentEntry != null)
            {
                Debug.Log($"轮到 {currentEntry.character.GetDisplayName()} 行动 (先攻顺序 {currentTurnIndex + 1})");
                try { GameLog.LogAction(currentEntry.character.GetDisplayName(), "的回合开始"); }
                catch (System.Exception ex) { Debug.LogWarning($"[AutoBattleAI] 记录回合开始日志失败: {ex.Message}"); }
            }
        }

        private void ProcessCurrentTurn()
        {
            if (isProcessingTurn) return;

            var currentEntry = GetCurrentInitiativeEntry();
            if (currentEntry == null)
            {
                AdvanceToNextTurn();
                return;
            }

            if (!currentEntry.CanAct() && !(currentEntry.character != null && currentEntry.character.HasStatusEffect(StatusEffectType.Unconscious)))
            {
                AdvanceToNextTurn();
                return;
            }

            isProcessingTurn = true;
            var character = currentEntry.character;

            if (character != null && character.HasStatusEffect(StatusEffectType.Unconscious))
            {
                if (showAIThoughts) Debug.Log($"=== {character.GetDisplayName()} 倒地状态 - 执行死豁免 (按回合) ===");
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

            var chosenAction = DecideBestAction(character);
            if (chosenAction != null)
            {
                ExecuteBattleActionEvent(character, chosenAction, () =>
                {
                    currentEntry.MarkAsActed();
                    AdvanceToNextTurn();
                });
            }
            else
            {
                currentEntry.MarkAsActed();
                AdvanceToNextTurn();
            }
        }

        private void ExecuteBattleActionEvent(CharacterStats attacker, BattleAction action, System.Action onComplete)
        {
            if (attacker == null || action == null || action.target == null)
            {
                onComplete?.Invoke();
                return;
            }

            Debug.Log("[DEBUG] ========== ExecuteBattleActionEvent 开始 ==========");
            Debug.Log($"[DEBUG] 攻击者: {attacker.GetDisplayName()}, 目标: {action.target.GetDisplayName()}");

            try
            {
                bool isSpell = attacker.template != null && attacker.template.defaultAttackType == DefaultAttackType.Spell;
                if (isSpell)
                {
                    string spellName = (attacker.template.defaultCantrip != null && !string.IsNullOrEmpty(attacker.template.defaultCantrip.spellName))
                        ? attacker.template.defaultCantrip.spellName
                        : "法术";
                    GameLog.LogAction(attacker.GetDisplayName(), $"施放 {spellName} 对 {action.target.GetDisplayName()}");
                }
                else
                {
                    string atkTypePreview = IsCharacterInFrontRow(attacker) ? "近战攻击" : "远程攻击";
                    GameLog.LogAction(attacker.GetDisplayName(), $"对 {action.target.GetDisplayName()} 发动{atkTypePreview}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AutoBattleAI] 宣言行动日志失败: {ex.Message}");
            }

            var attackerAdapter = attacker.GetComponent<DND_CharacterAdapter>();
            if (attackerAdapter == null)
            {
                Debug.LogError($"角色 {attacker.GetDisplayName()} 缺少 DND_CharacterAdapter 组件！将跳过动画，直接进行结算。");
                try { GameLog.LogAction("系统", $"{attacker.GetDisplayName()} 缺少动画适配器，直接进行命中与伤害结算"); }
                catch (System.Exception ex) { Debug.LogWarning($"[AutoBattleAI] 记录缺少动画适配器日志失败: {ex.Message}"); }

                bool assumeMelee = IsCharacterInFrontRow(attacker);
                ProcessAttackHit(attacker, action.target, assumeMelee);
                onComplete?.Invoke();
                return;
            }

            bool isMeleeAttack = IsCharacterInFrontRow(attacker);
            Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 攻击类型判断结果: {(isMeleeAttack ? "近战攻击" : "远程攻击")}");

            if (isMeleeAttack)
            {
                Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 开始执行近战攻击序列");
                bool hitInvoked = false;
                attackerAdapter.ExecuteMeleeAttack(
                    action.target.transform,
                    onAttackHit: () =>
                    {
                        Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 近战攻击命中回调触发");
                        if (!hitInvoked)
                        {
                            hitInvoked = true;
                            ProcessAttackHit(attacker, action.target, true);
                        }
                    },
                    onComplete: () =>
                    {
                        Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 近战攻击完成回调触发");
                        if (!hitInvoked)
                        {
                            ProcessAttackHit(attacker, action.target, true);
                        }
                        onComplete?.Invoke();
                    }
                );
            }
            else
            {
                bool hitInvoked = false;
                attackerAdapter.ExecuteRangedAttack(
                    action.target.transform,
                    onAttackHit: () =>
                    {
                        Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 远程攻击命中回调触发");
                        if (!hitInvoked)
                        {
                            hitInvoked = true;
                            ProcessAttackHit(attacker, action.target, false);
                        }
                    },
                    onComplete: () =>
                    {
                        Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 远程攻击完成回调触发");
                        if (!hitInvoked)
                        {
                            ProcessAttackHit(attacker, action.target, false);
                        }
                        onComplete?.Invoke();
                    }
                );
            }

            Debug.Log("[DEBUG] ========== ExecuteBattleActionEvent 结束 ==========");
        }

        private void ProcessAttackHit(CharacterStats attacker, CharacterStats target, bool isMeleeAttack)
        {
            if (attacker == null || target == null) return;

            int advantageFlag = 0;
            if (target.HasStatusEffect(StatusEffectType.Unconscious))
            {
                advantageFlag = isMeleeAttack ? 1 : -1;
                Debug.Log($"[DEBUG] 目标处于昏迷：设置攻击掷骰优势标志 = {advantageFlag} (1=优势, -1=劣势)");
            }

            var attackResult = HorizontalCombatRules.ResolveAttack(attacker, target, advantageFlag, isMeleeAttack);

            if (attackResult.isHit)
            {
                int damage = attackResult.damage;
                bool isCritical = attackResult.isCritical;

                if (showAIThoughts)
                {
                    string critText = isCritical ? " (暴击!)" : "";
                    Debug.Log($"{attacker.GetDisplayName()} 攻击 {target.GetDisplayName()}: 命中! 造成 {damage} 点伤害{critText}");
                }

                if (target.HasStatusEffect(StatusEffectType.Unconscious))
                {
                    target.RegisterUnconsciousHit(isCritical);
                    // 伤害事件已由 HorizontalCombatRules 统一发布
                }
                else
                {
                    target.TakeDamage(damage, DamageType.Bludgeoning, isCritical);
                    // 伤害事件已由 HorizontalCombatRules 统一发布

                    if (target.CurrentHitPoints > 0 && !target.HasStatusEffect(StatusEffectType.Unconscious))
                    {
                        var targetAdapter = target.GetComponent<DND_CharacterAdapter>();
                        targetAdapter?.PlayHitAnimation();
                    }
                }
            }
            else
            {
                if (showAIThoughts)
                {
                    Debug.Log($"{attacker.GetDisplayName()} 攻击 {target.GetDisplayName()}: 未命中!");
                }

                var targetAdapter = target.GetComponent<DND_CharacterAdapter>();
                targetAdapter?.PlayDodgeAnimation();

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

        private bool IsCharacterInFrontRow(CharacterStats character)
        {
            Debug.Log($"[DEBUG] 判断角色 {character.GetDisplayName()} 的位置");

            var positionComponent = character.GetComponent<BattlePositionComponent>();
            if (positionComponent != null)
            {
                Debug.Log($"[DEBUG] {character.GetDisplayName()} 找到BattlePositionComponent，rowPosition: {positionComponent.rowPosition}");
                return positionComponent.rowPosition == RowPosition.Front;
            }
            else
            {
                Debug.LogWarning($"[DEBUG] {character.GetDisplayName()} 没有BattlePositionComponent组件！");
            }

            var formationManager = FindObjectOfType<HorizontalBattleFormationManager>();
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

            Debug.Log($"[DEBUG] {character.GetDisplayName()} 使用默认判断：前排（近战）");
            return true;
        }

        private InitiativeEntry GetCurrentInitiativeEntry()
        {
            if (currentTurnIndex >= 0 && currentTurnIndex < initiativeOrder.Count)
            {
                return initiativeOrder[currentTurnIndex];
            }
            return null;
        }

        private void AdvanceToNextTurn()
        {
            currentTurnIndex++;
            if (currentTurnIndex >= initiativeOrder.Count)
            {
                currentTurnIndex = 0;
                ResetRoundState();
                Debug.Log("新的战斗轮次开始");
            }

            StartNextTurn();
        }

        private void ResetRoundState()
        {
            foreach (var entry in initiativeOrder)
            {
                entry.ResetTurnState();
            }
        }

        private bool IsBattleOver()
        {
            bool playerSideExists = initiativeOrder.Any(e => e.initialSide == BattleSide.Player);
            bool enemySideExists = initiativeOrder.Any(e => e.initialSide == BattleSide.Enemy);

            Debug.Log($"[IsBattleOver] 阵营存在检查 - 玩家: {playerSideExists}, 敌人: {enemySideExists}");
            return !playerSideExists || !enemySideExists;
        }

        private void EndBattle()
        {
            isBattleActive = false;
            isProcessingTurn = false;

            bool playerVictory = initiativeOrder.Any(entry =>
                entry.character.battleSide == BattleSide.Player &&
                entry.character.CurrentHitPoints > 0);

            if (playerVictory) Debug.Log("玩家胜利！"); else Debug.Log("玩家失败！");

            try { GameLog.LogAction("系统", playerVictory ? "战斗结束：玩家胜利" : "战斗结束：玩家失败"); }
            catch (System.Exception ex) { Debug.LogWarning($"[AutoBattleAI] 记录战斗结束日志失败: {ex.Message}"); }

            var idleManager = FindObjectOfType<IdleGameManager>();
            if (idleManager != null)
            {
                idleManager.OnBattleCompleted(playerVictory);
            }
            else
            {
                Debug.LogWarning("未找到 IdleGameManager，无法通知战斗结束。");
            }
        }

        public void RemoveCharacterFromInitiative(CharacterStats characterToRemove)
        {
            if (characterToRemove == null)
            {
                Debug.LogWarning("RemoveCharacterFromInitiative 调用时传入了空角色");
                return;
            }

            int removedCount = initiativeOrder.RemoveAll(e => e == null || e.character == null || e.character == characterToRemove);
            Debug.Log($"[Initiative] 已从先攻列表移除 {removedCount} 条与 {characterToRemove.GetDisplayName()} 相关的条目");

            if (currentTurnIndex >= initiativeOrder.Count)
            {
                currentTurnIndex = Mathf.Clamp(currentTurnIndex, 0, Mathf.Max(initiativeOrder.Count - 1, 0));
            }

            if (initiativeOrder.Count == 0 || IsBattleOver())
            {
                EndBattle();
            }
        }

        private BattleAction DecideBestAction(CharacterStats actor)
        {
            if (actor == null) return null;
            var target = FindBestTarget(actor);
            if (target == null) return null;
            return new BattleAction { target = target };
        }

        private CharacterStats FindBestTarget(CharacterStats actor)
        {
            var all = FindObjectsOfType<CharacterStats>();
            if (all == null || all.Length == 0) return null;

            var livingOpponents = all
                .Where(c => c != null && c.battleSide != actor.battleSide && c.CurrentHitPoints > 0)
                .ToList();

            var downedOpponents = all
                .Where(c => c != null && c.battleSide != actor.battleSide && c.CurrentHitPoints <= 0 && c.HasStatusEffect(StatusEffectType.Unconscious))
                .ToList();

            List<CharacterStats> pool = livingOpponents.Count > 0 ? livingOpponents : downedOpponents;
            if (pool.Count == 0) return null;

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
