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
                // 228：当前攻击方式威胁范围内无目标 => 本轮无法执行攻击动作，跳过并提示玩家。
                try { GameLog.LogAction(character.GetDisplayName(), "当前攻击方式无法执行，跳过本轮行动"); }
                catch (System.Exception ex) { Debug.LogWarning($"[AutoBattleAI] 记录无法攻击日志失败: {ex.Message}"); }
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
                // 攻击倒地/昏迷目标：近战获优势、远程/法术获劣势（既有规则，优先级最高）
                advantageFlag = isMeleeAttack ? 1 : -1;
                Debug.Log($"[DEBUG] 目标处于昏迷：设置攻击掷骰优势标志 = {advantageFlag} (1=优势, -1=劣势)");
            }
            else
            {
                // D 项：按攻击方式 + 纵深距离计算优势/劣势（文档 222/224/270/272/273）
                AttackStyle style = GetAttackStyle(attacker);
                int dist = GetDepthTier(attacker) + GetDepthTier(target) + 1; // 与 ComputeAttackableTargets 同口径
                advantageFlag = ComputeAttackModifiers(attacker, target, style, dist);
                if (advantageFlag != 0)
                {
                    Debug.Log($"[DEBUG] {attacker.GetDisplayName()} 攻击 {target.GetDisplayName()}: 距离={dist}, 攻击方式={GetAttackStylePreview(attacker)}, 目标方式={GetAttackStylePreview(target)} => {(advantageFlag > 0 ? "优势(2d20取高)" : "劣势(2d20取低)")}");
                }
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
            // A 项规则：无武器 = 徒手近战，攻击模式等同近战武器（不可像长触及/远程跨位攻击）。
            // 命中/伤害一律走 template 的 unarmedDamage* 参数（见 HorizontalCombatRules）。
            // 不引入 intrinsicAttackType：徒手恒为 Melee，由 GetDepthTier 威胁范围限制为邻接1纵深。
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
        /// 纵深距离 = 攻击者纵深 + 目标纵深 + 1（敌我前排贴面邻接，间隔 1 个纵深单位），
        /// 纵深取值：前排=0 / 中场=1 / 后排=2（见 GetDepthTier 对文档三层模型的还原）。
        /// 威胁上限（纵深距离）：近战 <=2；长触及 <=3；远程/法术 = 全图。
        /// 例：前排近战(0)→敌前排(0)=1、敌中场(1)=2 命中，敌后排(2)=3 超出；
        ///     中场近战(1)→敌前排(0)=2 命中，其余超出；后排近战(2)→无目标（按 228 跳过攻击）。
        /// </summary>
        private List<CharacterStats> ComputeAttackableTargets(CharacterStats attacker)
        {
            var result = new List<CharacterStats>();
            if (attacker == null) return result;
            AttackStyle style = GetAttackStyle(attacker);
            int atkDepth = GetDepthTier(attacker);
            // 威胁范围（纵深距离上限）：近战<=2、长触及<=3、远程/法术全图。
            int maxDist = style == AttackStyle.Melee ? 2 : (style == AttackStyle.Reach ? 3 : 99);

            // 从阵型管理器读取当前波次实际生成且存活的敌方单位（即真实存在的敌人 prefab 实例），
            // 红圈只标在这些敌人 prefab 脚下，而不是固定在阵型槽位上。
            BattleSide enemySide = (attacker.battleSide == BattleSide.Player) ? BattleSide.Enemy : BattleSide.Player;
            var manager = FindObjectOfType<HorizontalBattleFormationManager>();
            var enemies = manager != null ? manager.GetAliveUnits(enemySide) : new List<CharacterStats>();
            foreach (var c in enemies)
            {
                if (c == null || c == attacker) continue;
                int dist = atkDepth + GetDepthTier(c) + 1; // 敌我前排贴面，间隔 1
                if (dist <= maxDist) result.Add(c);
            }
            return result;
        }

        /// <summary>
        /// D 项：优势/劣势判定（文档 222/224/270/272/273）。
        /// 返回 +1=优势 / -1=劣势 / 0=正常；优势与劣势同时成立时按 273 抵消为 0。
        ///  - 远程/法术攻击者：dist<=1（抵近邻接,272）或 dist>4（超射程,270/222） => 劣势(-1)
        ///  - 近战/长触及攻击者：目标为远程/法术且 dist<=武器威胁上限(近战2/长触及3,224/222镜像) => 优势(+1)
        /// 说明：两规则按“攻击者自身方式”分别判定，不会并存；与 228 的“无法攻击跳过”解耦
        ///       （无法攻击指威胁范围内无目标，而非劣势——劣势仍可攻击但掷骰取低）。
        /// </summary>
        private int ComputeAttackModifiers(CharacterStats attacker, CharacterStats target, AttackStyle style, int dist)
        {
            bool isRangedOrSpell = style == AttackStyle.Ranged || style == AttackStyle.Spell;
            bool isMeleeOrReach  = style == AttackStyle.Melee || style == AttackStyle.Reach;

            bool hasAdvantage = false;
            bool hasDisadvantage = false;

            if (isRangedOrSpell)
            {
                // 远程/法术：抵近邻接(<=1) 或 超射程(>4) => 劣势
                if (dist <= 1 || dist > 4) hasDisadvantage = true;
            }
            else if (isMeleeOrReach)
            {
                // 近战/长触及：目标为远程/法术且在其威胁范围内 => 优势
                AttackStyle targetStyle = GetAttackStyle(target);
                bool targetRangedOrSpell = targetStyle == AttackStyle.Ranged || targetStyle == AttackStyle.Spell;
                int maxDist = style == AttackStyle.Melee ? 2 : 3; // 长触及=3
                if (targetRangedOrSpell && dist <= maxDist) hasAdvantage = true;
            }

            if (hasAdvantage && hasDisadvantage) return 0; // 273 抵消
            if (hasAdvantage) return 1;
            if (hasDisadvantage) return -1;
            return 0;
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
            // 只从“当前攻击方式威胁范围内”的敌方目标中挑选（D/228：超出威胁范围视为无法攻击，本轮跳过）。
            // 与 ShowAttackMarkers / ComputeAttackableTargets 同口径，保证红圈、可攻击判定、实际攻击三者一致。
            var inRange = ComputeAttackableTargets(actor);
            if (inRange == null || inRange.Count == 0) return null;

            CharacterStats best = null;
            float bestDist = float.MaxValue;
            foreach (var c in inRange)
            {
                if (c == null) continue;
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
