using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using demo2.DND.InventoryTetris; // for ItemBaseSO / CharacterEquipment / EquipmentSlot

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

        [Header("攻击范围标记")]
        [Tooltip("可攻击目标脚下的红圈 prefab（由策划制作并拖入此字段）")]
        public GameObject attackMarkerPrefab;

        [Header("先攻系统")]
        public List<InitiativeEntry> initiativeOrder = new List<InitiativeEntry>();
        public int currentTurnIndex;
        public bool isBattleActive;

        private bool isProcessingTurn;
        private float turnTimer;

        // 当前行动者显示的攻击范围红圈实例（行动结束清除）
        private List<GameObject> activeMarkers = new List<GameObject>();

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

            // 显示当前攻击者按其攻击方式（武器/法术）可命中的敌方目标红圈；本次行动结束清除
            var wrappedOnComplete = onComplete;
            onComplete = () => { ClearAttackMarkers(); wrappedOnComplete?.Invoke(); };
            ShowAttackMarkers(attacker);

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
                    string atkTypePreview = GetAttackStylePreview(attacker);
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

                bool assumeMelee = IsMeleeStyle(GetAttackStyle(attacker));
                ProcessAttackHit(attacker, action.target, assumeMelee);
                onComplete?.Invoke();
                return;
            }

            AttackStyle style = GetAttackStyle(attacker);
            bool isMeleeAttack = IsMeleeStyle(style);
            Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 攻击类型判断结果(按武器/法术): {GetAttackStylePreview(attacker)}");

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

        #region 攻击方式解析与可攻击范围（B/C：基于武器/法术，与排位无关）
        /// <summary>
        /// 统一攻击方式解析：法术优先；否则读主手武器的 WeaponType；无武器视为徒手近战。
        /// 玩家与敌人通用（都按各自持有的武器或模板法术能力走，不再按前/后排判定）。
        /// </summary>
        private AttackStyle GetAttackStyle(CharacterStats attacker)
        {
            if (attacker == null) return AttackStyle.Melee;

            // 法术优先（需模板声明法术攻击）；其余完全按"主手武器 WeaponType"判定，
            // 不得因 template 为空而把持远程武器的单位误判为近战（见文档 268：按持有武器/模板区分）。
            if (attacker.template != null && attacker.template.defaultAttackType == DefaultAttackType.Spell) return AttackStyle.Spell;

            var weapon = GetMainHandWeapon(attacker);
            if (weapon != null)
            {
                switch (weapon.weaponType)
                {
                    case WeaponType.Ranged: return AttackStyle.Ranged;
                    case WeaponType.Reach:  return AttackStyle.Reach;
                    default:                return AttackStyle.Melee;
                }
            }
            return AttackStyle.Melee; // 徒手
        }

        private bool IsMeleeStyle(AttackStyle style)
        {
            return style == AttackStyle.Melee || style == AttackStyle.Reach;
        }

        private string GetAttackStylePreview(CharacterStats attacker)
        {
            switch (GetAttackStyle(attacker))
            {
                case AttackStyle.Spell:  return "施放法术";
                case AttackStyle.Ranged: return "远程攻击";
                case AttackStyle.Reach:  return "长触及攻击";
                default:                return "近战攻击";
            }
        }

        private ItemBaseSO GetMainHandWeapon(CharacterStats c)
        {
            if (c == null) return null;
            var eq = c.GetComponent<CharacterEquipment>()
                      ?? c.GetComponentInParent<CharacterEquipment>()
                      ?? c.GetComponentInChildren<CharacterEquipment>(true);
            var inst = eq != null ? eq.GetEquipped(EquipmentSlot.MainHand) : null;
            return inst != null ? inst.data : null;
        }

        /// <summary>
        /// 由 BattlePositionComponent.currentPosition 还原文档的三层纵深（Front=0 / Middle=1 / Back=2）。
        /// 代码中 Middle 对应 *BackCenter（索引4/10），其余 *BackLeft/Right 为 Back。
        /// </summary>
        private int GetDepthTier(CharacterStats c)
        {
            var pc = c != null ? c.GetComponent<BattlePositionComponent>() : null;
            if (pc == null) return 0;
            switch (pc.currentPosition)
            {
                case HorizontalPosition.PlayerFrontLeft:
                case HorizontalPosition.PlayerFrontCenter:
                case HorizontalPosition.PlayerFrontRight:
                case HorizontalPosition.EnemyFrontLeft:
                case HorizontalPosition.EnemyFrontCenter:
                case HorizontalPosition.EnemyFrontRight:
                    return 0;
                case HorizontalPosition.PlayerBackCenter:
                case HorizontalPosition.EnemyBackCenter:
                    return 1;
                default:
                    return 2;
            }
        }

        /// <summary>
        /// 按攻击方式与纵深层级计算当前攻击者可以攻击到的敌方目标列表。
        /// 近战：纵深差<=1；长触及：<=2；远程/法术：任意（全图）。
        /// </summary>
        private List<CharacterStats> ComputeAttackableTargets(CharacterStats attacker)
        {
            var result = new List<CharacterStats>();
            if (attacker == null) return result;
            AttackStyle style = GetAttackStyle(attacker);
            int atkTier = GetDepthTier(attacker);
            int maxDiff = style == AttackStyle.Melee ? 1 : (style == AttackStyle.Reach ? 2 : 99);

            // 从阵型管理器读取当前波次实际生成且存活的敌方单位（即真实存在的敌人 prefab 实例），
            // 红圈只标在这些敌人 prefab 脚下，而不是固定在阵型槽位上。
            BattleSide enemySide = (attacker.battleSide == BattleSide.Player) ? BattleSide.Enemy : BattleSide.Player;
            var manager = FindObjectOfType<HorizontalBattleFormationManager>();
            var enemies = manager != null ? manager.GetAliveUnits(enemySide) : new List<CharacterStats>();
            foreach (var c in enemies)
            {
                if (c == null || c == attacker) continue;
                if (Mathf.Abs(GetDepthTier(c) - atkTier) <= maxDiff) result.Add(c);
            }
            return result;
        }

        private void ShowAttackMarkers(CharacterStats attacker)
        {
            ClearAttackMarkers();
            if (attackMarkerPrefab == null)
            {
                Debug.LogWarning("[AutoBattleAI] attackMarkerPrefab 未配置，跳过红圈显示");
                return;
            }
            var targets = ComputeAttackableTargets(attacker);
            var manager = FindObjectOfType<HorizontalBattleFormationManager>();
            foreach (var t in targets)
            {
                // 首选：目标 prefab 下统一的 "Target" 空挂点（已统一添加，正好是 Spine 网格脚底坐标）。
                // 兜底：场景 Spawn 位置点 -> 目标视觉中心地面投影。
                Vector3 pos;
                var targetAnchor = FindChildByName(t.transform, "Target");
                if (targetAnchor != null)
                {
                    pos = targetAnchor.position;
                }
                else if (manager != null)
                {
                    var spawn = manager.GetSpawnPointForUnit(t);
                    pos = spawn != null ? spawn.position : GetUnitGroundPosition(t.transform);
                    pos.y = 0.05f; // 贴地，避免与地面 z-fighting
                }
                else
                {
                    pos = GetUnitGroundPosition(t.transform);
                }
                var m = Instantiate(attackMarkerPrefab, pos, Quaternion.identity);
                activeMarkers.Add(m);
            }
            Debug.Log($"[AutoBattleAI] {attacker.GetDisplayName()} 攻击方式={GetAttackStylePreview(attacker)}，红圈标记可攻击目标 {targets.Count} 个");
        }

        /// <summary>
        /// 递归查找名为 name 的子节点（含自身），找不到返回 null。
        /// 用于在目标 prefab 下定位统一的 "Target" 空挂点（Spine 网格脚底坐标）。
        /// </summary>
        private static Transform FindChildByName(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// 计算一个单位 prefab 的"脚下地面位置"：用所有渲染器包围盒求视觉中心，
        /// 取其中 x/z，y 固定贴地（0.05）。这样红圈始终落在真实模型脚下，
        /// 不受根 transform / 网格局部偏移影响。
        /// </summary>
        private Vector3 GetUnitGroundPosition(Transform t)
        {
            if (t == null) return Vector3.zero;
            var renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                if (b.size.sqrMagnitude > 0f)
                    return new Vector3(b.center.x, 0.05f, b.center.z);
            }
            // 回退：无渲染器时直接用根坐标贴地
            Vector3 p = t.position;
            p.y = 0.05f;
            return p;
        }

        private void ClearAttackMarkers()
        {
            foreach (var m in activeMarkers) if (m != null) Destroy(m);
            activeMarkers.Clear();
        }
        #endregion

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
