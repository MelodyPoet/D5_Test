using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Spine.Unity; // 引用Spine命名空间
using DG.Tweening; // 引用DOTween命名空间

namespace DND5E {
    // 攻击类型枚举
    public enum AttackType {
        Melee,      // 近战
        Ranged,     // 远程
        Spell       // 法术
    }
}

public class CombatManager : MonoBehaviour {
    // 单例实例
    public static CombatManager Instance { get; private set; }    // 战斗状态枚举
    public enum CombatState {
        Inactive,       // 未激活
        PreBattle,      // 探索阶段 - 战斗前准备
        Approaching,    // 接战阶段 - 玩家队伍向战斗位置移动
        Formation,      // 阵型就位 - 双方到达战斗位置
        RollingInitiative, // 掷先攻
        TurnStart,      // 回合开始
        ActionSelection,// 行动选择
        ExecutingAction,// 执行行动
        TurnEnd,        // 回合结束
        CombatEnd       // 战斗结束
    }

    // 当前战斗状态
    public CombatState currentState = CombatState.Inactive;

    // 先攻顺序
    public List<CharacterStats> initiativeOrder = new List<CharacterStats>();

    // 当前行动角色索引
    public int currentTurnIndex = 0;

    // 当前回合数
    public int currentRound = 0;

    // 当前行动的角色
    public CharacterStats currentCharacter => initiativeOrder.Count > 0 ? initiativeOrder[currentTurnIndex] : null;

    // 战斗是否激活
    public bool isCombatActive => currentState != CombatState.Inactive;

    // 事件委托
    public delegate void CombatEventHandler();
    public delegate void CharacterTurnEventHandler(CharacterStats character);

    // 战斗事件
    public event CombatEventHandler OnCombatStart;
    public event CombatEventHandler OnCombatEnd;
    public event CombatEventHandler OnRoundStart;
    public event CombatEventHandler OnRoundEnd;
    public event CharacterTurnEventHandler OnTurnStart;
    public event CharacterTurnEventHandler OnTurnEnd;

    // 接战动画相关配置
    [Header("接战动画配置")]
    [Tooltip("玩家队伍从探索区移动到战斗区的速度")]
    public float approachSpeed = 3f;
    [Tooltip("接战动画过程中的缓动曲线类型")]
    public Ease approachEaseType = Ease.OutQuart;
    [Tooltip("角色之间的移动延迟，创造更自然的群体移动效果")]
    public float characterMoveDelay = 0.2f;

    // 接战阶段使用的队伍引用
    private List<CharacterStats> playerTeamForApproach = new List<CharacterStats>();
    private List<CharacterStats> enemyTeamForApproach = new List<CharacterStats>();

    // 接战动画完成计数器
    private int approachCompletedCount = 0;
    private int totalApproachingCharacters = 0; void Start() {
        HorizontalBattleFormationManager.Instance.InitializeBattle();
    }


    private void Awake() {
        // 单例模式
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    // 开始战斗
    public void StartCombat(List<CharacterStats> participants) {
        if (isCombatActive) {
            Debug.LogWarning("战斗已经在进行中!");
            return;
        }

        // 清空先攻顺序
        initiativeOrder.Clear();

        // 添加参与者
        initiativeOrder.AddRange(participants);

        foreach (CharacterStats character in participants) {
            if (character != null) {
                string displayName = character.GetDisplayName();
                Debug.Log($"战斗开始前获取角色显示名称: {displayName}");
            }
        }

        // 掷先攻
        RollInitiative();

        // 设置战斗状态
        currentState = CombatState.RollingInitiative;
        currentRound = 1;

        // 触发战斗开始事件
        OnCombatStart?.Invoke();        // 在战斗日志中添加战斗开始信息
        if (DND_BattleUI.Instance != null) {
            DND_BattleUI.Instance.AddCombatLog("=== 战斗开始 ===");
        }

        // 设置所有角色朝向对方阵营
        InitializeCharactersFacing();

        // 开始第一个回合
        StartNextTurn();
    }

    // 掷先攻
    private void RollInitiative() {        // 为每个角色掷先攻
        Dictionary<CharacterStats, int> initiativeRolls = new Dictionary<CharacterStats, int>();

        foreach (CharacterStats character in initiativeOrder) {
            // 使用DexMod + 随机值计算先攻
            int dexMod = character.stats.DexMod;
            int initiativeRoll = Random.Range(1, 21) + dexMod;
            initiativeRolls[character] = initiativeRoll;
            Debug.Log($"{character.characterName} 掷出先攻 {initiativeRoll} (1d20 + {dexMod})");
        }

        // 按先攻值排序（降序）
        initiativeOrder = initiativeOrder.OrderByDescending(c => initiativeRolls[c]).ToList();

        // 输出先攻顺序
        Debug.Log("先攻顺序:");
        for (int i = 0; i < initiativeOrder.Count; i++) {
            Debug.Log($"{i + 1}. {initiativeOrder[i].characterName} - {initiativeRolls[initiativeOrder[i]]}");
        }
    }

    // 开始下一个回合
    public void StartNextTurn() {
        Debug.Log("【重要】开始下一个回合");

        // 检查战斗是否结束
        if (CheckCombatEnd()) {
            Debug.Log("战斗已结束，调用EndCombat");
            EndCombat();
            return;
        }

        // 记录当前角色索引
        int previousTurnIndex = currentTurnIndex;

        // 更新当前回合索引
        currentTurnIndex = (currentTurnIndex + 1) % initiativeOrder.Count;
        Debug.Log($"回合索引从 {previousTurnIndex} 更新为 {currentTurnIndex}");

        // 如果回到第一个角色，增加回合数
        if (currentTurnIndex == 0) {
            currentRound++;
            OnRoundStart?.Invoke();
            Debug.Log($"【重要】第 {currentRound} 回合开始!");

            // 在战斗日志中添加回合开始信息
            if (DND_BattleUI.Instance != null) {
                DND_BattleUI.Instance.AddCombatLog($"=== 第 {currentRound} 回合开始 ===");
            }
        }

        // 获取当前角色
        CharacterStats character = currentCharacter;
        if (character == null) {
            Debug.LogError("当前角色为null，无法开始回合");
            return;
        }

        Debug.Log($"当前角色: {character.characterName}, 标签: {character.gameObject.tag}");

        // 检查角色是否失去意识
        if (character.HasStatusEffect(DND5E.StatusEffectType.Unconscious)) {
            // 失去意识的角色自动跳过回合
            Debug.Log($"{character.characterName} 失去意识，跳过回合!");
            StartNextTurn();
            return;
        }

        // 设置战斗状态
        currentState = CombatState.TurnStart;

        // 重置角色行动状态（在触发事件前重置）
        ResetCharacterActions(character);

        // 处理回合开始时的状态效果清理
        ProcessStartOfTurnStatusEffects(character);

        // 触发回合开始事件
        Debug.Log($"【ActionPanel调试】准备触发OnTurnStart事件，角色: {character.characterName}, 标签: {character.gameObject.tag}");
        Debug.Log($"【ActionPanel调试】OnTurnStart事件订阅者数量: {OnTurnStart?.GetInvocationList()?.Length ?? 0}");
        OnTurnStart?.Invoke(character);

        Debug.Log($"【重要】{character.characterName} 的回合开始!");

        // 在战斗日志中添加角色回合开始信息
        if (DND_BattleUI.Instance != null) {
            DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 的回合开始");
        }

        // 处理回合开始时的状态效果
        ProcessStatusEffects(character);

        // 回合开始时让角色面朝最近的敌人
        FaceNearestEnemy(character);

        // 进入行动选择阶段
        currentState = CombatState.ActionSelection;
    }

    // 结束当前回合
    public void EndCurrentTurn() {
        Debug.Log($"【重要】尝试结束当前回合");

        // 获取当前角色
        CharacterStats character = currentCharacter;

        if (character == null) {
            Debug.LogError("无法结束回合：当前角色为null");
            return;
        }

        // 记录当前角色信息，用于调试
        string characterName = character.characterName;
        string characterTag = character.gameObject.tag;
        int characterIndex = currentTurnIndex;

        Debug.Log($"结束回合 - 角色: {characterName}, 标签: {characterTag}, 索引: {characterIndex}");

        // 设置战斗状态
        currentState = CombatState.TurnEnd;
        // 处理回合结束时的状态效果
        ProcessEndOfTurnStatusEffects(character);

        // 触发回合结束事件
        OnTurnEnd?.Invoke(character);

        Debug.Log($"【重要】{characterName} 的回合结束!");

        // 在战斗日志中添加角色回合结束信息
        if (DND_BattleUI.Instance != null) {
            DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 的回合结束");
        }

        // 如果是最后一个角色，触发回合结束事件
        if (currentTurnIndex == initiativeOrder.Count - 1) {
            OnRoundEnd?.Invoke();
            Debug.Log($"第 {currentRound} 回合结束!");

            // 在战斗日志中添加回合结束信息
            if (DND_BattleUI.Instance != null) {
                DND_BattleUI.Instance.AddCombatLog($"=== 第 {currentRound} 回合结束 ===");
            }
        }

        // 开始下一个回合
        StartNextTurn();
    }

    // 结束战斗
    public void EndCombat() {
        // 设置战斗状态
        currentState = CombatState.CombatEnd;

        // 触发战斗结束事件
        OnCombatEnd?.Invoke();

        Debug.Log("战斗结束!");

        // 在战斗日志中添加战斗结束信息
        if (DND_BattleUI.Instance != null) {
            DND_BattleUI.Instance.AddCombatLog("=== 战斗结束 ===");
        }

        // 重置战斗状态
        currentState = CombatState.Inactive;
        initiativeOrder.Clear();
        currentTurnIndex = 0;
        currentRound = 0;
    }

    // 检查战斗是否结束
    private bool CheckCombatEnd() {        // 检查是否所有敌人都已经失去意识
        bool allEnemiesDefeated = true;
        bool allPlayersDefeated = true;

        foreach (CharacterStats character in initiativeOrder) {
            // 假设玩家角色的标签是"Player"，敌人的标签是"Enemy"
            if (character.gameObject.CompareTag("Player")) {
                if (!character.HasStatusEffect(DND5E.StatusEffectType.Unconscious)) {
                    allPlayersDefeated = false;
                }
            }
            else if (character.gameObject.CompareTag("Enemy")) {
                if (!character.HasStatusEffect(DND5E.StatusEffectType.Unconscious)) {
                    allEnemiesDefeated = false;
                }
            }
        }

        return allEnemiesDefeated || allPlayersDefeated;
    }

    // 重置角色行动状态
    private void ResetCharacterActions(CharacterStats character) {
        // 获取角色的ActionSystem组件
        DND5E.ActionSystem actionSystem = character.GetComponent<DND5E.ActionSystem>();
        if (actionSystem != null) {
            // 重置行动状态
            actionSystem.ResetActions();
        }
        else {
            Debug.LogWarning($"{character.characterName} 没有ActionSystem组件，无法重置行动状态");
        }
    }

    // 处理状态效果
    private void ProcessStatusEffects(CharacterStats character) {        // 处理各种状态效果
        foreach (DND5E.StatusEffectType effect in character.statusEffects.ToList()) {
            switch (effect) {
                case DND5E.StatusEffectType.Poisoned:
                    // 中毒状态每回合造成伤害
                    int poisonDamage = Random.Range(1, 5); // 1d4伤害
                    character.TakeDamage(poisonDamage, DND5E.DamageType.Poison);

                    // 确保UI更新
                    if (DND_BattleUI.Instance != null) {
                        DND_BattleUI.Instance.UpdateCharacterStatusUI(character);
                    }

                    Debug.Log($"{character.characterName} 受到中毒效果影响，损失{poisonDamage}点生命值");

                    // 在战斗日志中添加中毒效果信息
                    if (DND_BattleUI.Instance != null) {
                        DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 受到中毒效果影响，损失 {poisonDamage} 点生命值");
                    }
                    break;

                case DND5E.StatusEffectType.Stunned:
                    // 震慑状态无法行动
                    Debug.Log($"{character.characterName} 处于震慑状态，本回合无法行动");

                    // 在战斗日志中添加震慑状态信息
                    if (DND_BattleUI.Instance != null) {
                        DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 处于震慑状态，本回合无法行动");
                    }
                    break;

                case DND5E.StatusEffectType.Paralyzed:
                    // 麻痹状态无法行动
                    Debug.Log($"{character.characterName} 处于麻痹状态，本回合无法行动");

                    // 在战斗日志中添加麻痹状态信息
                    if (DND_BattleUI.Instance != null) {
                        DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 处于麻痹状态，本回合无法行动");
                    }
                    break;

                case DND5E.StatusEffectType.Dodging:
                    // 防御姿态在下回合开始时移除
                    Debug.Log($"{character.characterName} 处于防御姿态，AC+2，将在下回合开始时移除");

                    // 在战斗日志中添加防御姿态状态信息
                    if (DND_BattleUI.Instance != null) {
                        DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 处于防御姿态，AC+2");
                    }
                    break;

                    // 其他状态效果...
            }
        }

        // 输出当前状态效果
        if (character.statusEffects.Count > 0) {
            Debug.Log($"{character.characterName} 当前状态效果: {string.Join(", ", character.statusEffects)}");
        }
    }

    // 处理回合开始时的状态效果清理
    private void ProcessStartOfTurnStatusEffects(CharacterStats character) {
        // 移除防御姿态（在新回合开始时移除）
        if (character.HasStatusEffect(DND5E.StatusEffectType.Dodging)) {
            Debug.Log($"{character.characterName} 的防御姿态在新回合开始时移除");
            character.RemoveStatusEffect(DND5E.StatusEffectType.Dodging);

            // 确保UI更新
            if (DND_BattleUI.Instance != null) {
                DND_BattleUI.Instance.UpdateCharacterStatusUI(character);
            }
        }

        // 可以在这里处理其他在回合开始时需要清理的状态效果
    }

    // 处理回合结束时的状态效果
    private void ProcessEndOfTurnStatusEffects(CharacterStats character) {
        // 防御姿态不在回合结束时移除，而是在下回合开始时移除
        // 这样可以确保防御姿态持续整个回合周期

        // 可以在这里处理其他在回合结束时需要处理的状态效果
        // 例如：中毒、燃烧等持续伤害效果
    }

    // 执行攻击
    public IEnumerator ExecuteAttack(CharacterStats attacker, CharacterStats target, DND5E.AttackType attackType, string ability, string damageFormula, DND5E.DamageType damageType) {
        // 检查是否有主要动作
        DND5E.ActionSystem actionSystem = attacker.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasAction) {
            Debug.LogWarning($"{attacker.characterName} 没有主要动作可用!");
            yield break;
        }

        // 检查攻击距离
        float rangeInFeet = 0;
        switch (attackType) {
            case DND5E.AttackType.Melee:
                rangeInFeet = RangeManager.Instance.meleeRange;
                break;
            case DND5E.AttackType.Ranged:
                rangeInFeet = RangeManager.Instance.defaultRangedRange;
                break;
            case DND5E.AttackType.Spell:
                rangeInFeet = RangeManager.Instance.defaultSpellRange;
                break;
        }

        // 获取实际距离
        float distanceInFeet = RangeManager.Instance.GetDistanceInFeet(attacker, target);

        // 检查是否在范围内
        if (distanceInFeet > rangeInFeet) {
            Debug.LogWarning($"{attacker.characterName} 与目标距离 {distanceInFeet:F1} 尺，超出 {attackType} 攻击范围 {rangeInFeet} 尺!");
            yield break;
        }

        // 设置战斗状态
        currentState = CombatState.ExecutingAction;

        // 使用动作
        actionSystem.UseAction(DND5E.ActionType.Action);

        // 确定朝向
        Vector3 direction = (target.transform.position - attacker.transform.position).normalized;
        if (direction.x != 0) {
            // 如果角色有SpriteRenderer，设置flipX
            SpriteRenderer spriteRenderer = attacker.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) {
                spriteRenderer.flipX = direction.x < 0;
            }

            // 如果角色有SkeletonAnimation，设置scale.x
            Spine.Unity.SkeletonAnimation skeletonAnimation = attacker.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (skeletonAnimation != null) {
                Vector3 scale = skeletonAnimation.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (direction.x < 0 ? -1 : 1);
                skeletonAnimation.transform.localScale = scale;
            }
        }

        // 首先尝试使用DND_CharacterAdapter播放攻击动画
        DND_CharacterAdapter characterAdapter = attacker.GetComponent<DND_CharacterAdapter>();
        AnimationController animController = null;
        float animationDelay = 0.5f;

        if (characterAdapter != null) {
            // 根据攻击类型播放不同动画
            switch (attackType) {
                case DND5E.AttackType.Melee:
                    characterAdapter.PlayAttackAnimation();
                    Debug.Log($"使用DND_CharacterAdapter播放攻击动画: {characterAdapter.animationMapping.attackAnimation}");
                    break;
                case DND5E.AttackType.Ranged:
                    characterAdapter.PlayAttackAnimation(); // 可以根据需要添加远程攻击动画
                    Debug.Log($"使用DND_CharacterAdapter播放远程攻击动画: {characterAdapter.animationMapping.attackAnimation}");
                    break;
                case DND5E.AttackType.Spell:
                    characterAdapter.PlayCastAnimation();
                    Debug.Log($"使用DND_CharacterAdapter播放施法动画: {characterAdapter.animationMapping.castAnimation}");
                    break;
            }

            // 获取动画持续时间
            if (characterAdapter.skeletonAnimation != null && characterAdapter.skeletonAnimation.AnimationState.GetCurrent(0) != null) {
                float duration = characterAdapter.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Duration;
                animationDelay = duration * 0.5f; // 在动画中间点计算伤害
                Debug.Log($"攻击动画持续时间: {duration}秒, 延迟: {animationDelay}秒");
            }
        }
        else {
            // 如果没有DND_CharacterAdapter，尝试使用AnimationController
            animController = attacker.GetComponent<AnimationController>();
            if (animController != null) {
                // 根据攻击类型播放不同动画
                switch (attackType) {
                    case DND5E.AttackType.Melee:
                        animController.PlayMeleeAttack();
                        Debug.Log($"使用AnimationController播放近战攻击动画: {animController.attackMeleeAnimation}");
                        break;
                    case DND5E.AttackType.Ranged:
                        animController.PlayRangedAttack();
                        Debug.Log($"使用AnimationController播放远程攻击动画: {animController.attackRangedAnimation}");
                        break;
                    case DND5E.AttackType.Spell:
                        animController.PlayCastSpell();
                        Debug.Log($"使用AnimationController播放施法动画: {animController.castSpellAnimation}");
                        break;
                }

                // 获取动画持续时间
                string animName = "";
                switch (attackType) {
                    case DND5E.AttackType.Melee:
                        animName = animController.attackMeleeAnimation;
                        break;
                    case DND5E.AttackType.Ranged:
                        animName = animController.attackRangedAnimation;
                        break;
                    case DND5E.AttackType.Spell:
                        animName = animController.castSpellAnimation;
                        break;
                }

                float duration = animController.GetAnimationDuration(animName);
                if (duration > 0) {
                    animationDelay = duration * 0.5f; // 在动画中间点计算伤害
                    Debug.Log($"攻击动画持续时间: {duration}秒, 延迟: {animationDelay}秒");
                }
            }
            else {
                Debug.LogWarning("角色既没有DND_CharacterAdapter也没有AnimationController组件，无法播放攻击动画");
            }
        }

        yield return new WaitForSeconds(animationDelay);

        // 确定是否有优势或劣势
        bool hasAdvantage = false;
        bool hasDisadvantage = false;

        // 检查目标是否有影响攻击的状态效果
        if (target.HasStatusEffect(DND5E.StatusEffectType.Prone) && attackType == DND5E.AttackType.Melee) {
            hasAdvantage = true;
        }

        if (target.HasStatusEffect(DND5E.StatusEffectType.Invisible)) {
            hasDisadvantage = true;
        }

        // 进行攻击掷骰
        int attackRoll = attacker.AttackRoll(ability, hasAdvantage, hasDisadvantage);

        // 记录攻击掷骰结果
        string attackTypeStr = "";
        switch (attackType) {
            case DND5E.AttackType.Melee:
                attackTypeStr = "近战";
                break;
            case DND5E.AttackType.Ranged:
                attackTypeStr = "远程";
                break;
            case DND5E.AttackType.Spell:
                attackTypeStr = "法术";
                break;
        }

        string advantageStr = "";
        if (hasAdvantage && !hasDisadvantage)
            advantageStr = "（优势）";
        else if (!hasAdvantage && hasDisadvantage)
            advantageStr = "（劣势）";

        // 获取攻击骰值和加值的详细信息
        int d20Roll;
        int abilityMod = 0;
        int profBonus = attacker.proficiencyBonus;
        int weaponBonus = 0; // 未来可能添加的武器加值
        int otherBonus = 0;  // 其他可能的加值

        // 根据能力值确定属性加值
        switch (ability.ToLower()) {
            case "str":
            case "strength":
                abilityMod = attacker.stats.StrMod;
                break;
            case "dex":
            case "dexterity":
                abilityMod = attacker.stats.DexMod;
                break;
            case "int":
            case "intelligence":
                abilityMod = attacker.stats.IntMod;
                break;
        }

        // 计算d20的原始骰值
        d20Roll = attackRoll - abilityMod - profBonus - weaponBonus - otherBonus;

        // 构建攻击公式字符串
        string attackFormula = $"d20({d20Roll})";
        if (abilityMod != 0) attackFormula += $" + {ability}({abilityMod})";
        if (profBonus != 0) attackFormula += $" + 熟练({profBonus})";
        if (weaponBonus != 0) attackFormula += $" + 武器({weaponBonus})";
        if (otherBonus != 0) attackFormula += $" + 其他({otherBonus})";

        // 添加到战斗日志
        if (DND_BattleUI.Instance != null) {
            DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {attacker.GetDisplayName()} 进行{attackTypeStr}攻击{advantageStr}");
            DND_BattleUI.Instance.AddCombatLog($"攻击检定: {attackFormula} = {attackRoll} vs AC {target.armorClass}");
        }

        // 检查是否命中
        if (attackRoll >= target.armorClass) {
            // 计算伤害
            int damage = attacker.CalculateDamage(ability, damageFormula);

            // 重用之前计算的属性加值，不需要重新声明变量
            // 根据能力值重新确定属性加值
            switch (ability.ToLower()) {
                case "str":
                case "strength":
                    abilityMod = attacker.stats.StrMod;
                    break;
                case "dex":
                case "dexterity":
                    abilityMod = attacker.stats.DexMod;
                    break;
                case "int":
                case "intelligence":
                    abilityMod = attacker.stats.IntMod;
                    break;
            }

            // 解析伤害公式，例如 "1d8"
            string[] parts = damageFormula.Split('d');
            int diceCount = 1;
            int diceType = 8;

            if (parts.Length == 2) {
                diceCount = int.Parse(parts[0]);
                diceType = int.Parse(parts[1]);
            }

            // 计算骰子伤害值
            int diceRoll = damage - abilityMod - weaponBonus - otherBonus;

            // 构建伤害公式字符串
            string damageFormulaPretty = $"{diceCount}d{diceType}({diceRoll})";
            if (abilityMod != 0) damageFormulaPretty += $" + {ability}({abilityMod})";
            if (weaponBonus != 0) damageFormulaPretty += $" + 武器({weaponBonus})";
            if (otherBonus != 0) damageFormulaPretty += $" + 其他({otherBonus})";

            // 记录伤害结果
            if (DND_BattleUI.Instance != null) {
                DND_BattleUI.Instance.AddCombatLog($"{attacker.GetDisplayName()} 命中!");
                DND_BattleUI.Instance.AddCombatLog($"伤害: {damageFormulaPretty} = {damage} 点{damageType}伤害");
            }

            // 播放目标受击动画
            DND_CharacterAdapter targetAdapter = target.GetComponent<DND_CharacterAdapter>();
            AnimationController targetAnimController = null;

            // 等待一小段时间，确保有时间准备播放受击动画
            yield return new WaitForSeconds(0.1f);

            // 如果目标有DND_CharacterAdapter组件，调用其TakeDamage方法
            // 这个方法会播放受击动画，并在生命值为0时播放死亡动画
            if (targetAdapter != null) {
                Debug.Log($"调用 {target.characterName} 的TakeDamage方法播放受击动画");
                targetAdapter.TakeDamage();
            }
            else {
                // 如果没有DND_CharacterAdapter，尝试使用AnimationController
                targetAnimController = target.GetComponent<AnimationController>();
                if (targetAnimController != null) {
                    targetAnimController.PlayHit();
                    Debug.Log($"使用AnimationController播放目标受击动画: {targetAnimController.hitAnimation}");
                }
                else {
                    Debug.LogWarning($"目标 {target.characterName} 既没有DND_CharacterAdapter也没有AnimationController组件，无法播放受击动画");
                }
            }

            // 等待一小段时间，确保受击动画有时间播放
            yield return new WaitForSeconds(0.2f);

            // 造成伤害
            target.TakeDamage(damage, damageType);

            // 确保UI更新
            if (DND_BattleUI.Instance != null) {
                DND_BattleUI.Instance.UpdateCharacterStatusUI(target);
            }

            Debug.Log($"{attacker.characterName} 对 {target.characterName} 进行 {attackType} 攻击，命中! 造成 {damage} 点 {damageType} 伤害!");

            // 检查目标是否死亡
            if (target.currentHitPoints <= 0) {
                yield return new WaitForSeconds(0.5f); // 给受击动画更多时间

                if (targetAdapter != null) {
                    // 使用DND_CharacterAdapter播放死亡动画
                    targetAdapter.PlayDeathAnimation();
                    Debug.Log($"使用DND_CharacterAdapter播放目标死亡动画: {targetAdapter.animationMapping.deathAnimation}");
                }
                else if (targetAnimController != null) {
                    // 如果没有DND_CharacterAdapter，使用AnimationController
                    targetAnimController.PlayDeath();
                    Debug.Log($"使用AnimationController播放目标死亡动画: {targetAnimController.deathAnimation}");
                }
            }
        }
        else {
            // 播放闪避动画
            // 注意：DND_CharacterAdapter没有专门的闪避动画方法，可以考虑添加
            // 这里我们暂时只使用AnimationController
            AnimationController targetAnimController = target.GetComponent<AnimationController>();
            if (targetAnimController != null) {
                targetAnimController.PlayDodge();
                Debug.Log($"使用AnimationController播放目标闪避动画: {targetAnimController.dodgeAnimation}");
            }

            Debug.Log($"{attacker.characterName} 对 {target.characterName} 进行 {attackType} 攻击，未命中!");

            // 显示Miss文本
            if (DamageNumberManager.Instance != null) {
                DamageNumberManager.Instance.ShowMissText(target.transform);
            }

            // 记录未命中结果
            if (DND_BattleUI.Instance != null) {
                DND_BattleUI.Instance.AddCombatLog($"{attacker.GetDisplayName()} 未命中 {target.GetDisplayName()}!");
            }
        }

        // 等待动画完成
        yield return new WaitForSeconds(0.5f);

        // 只有在攻击者没有死亡的情况下才恢复待机动画
        if (attacker.currentHitPoints > 0) {
            if (characterAdapter != null) {
                // 使用DND_CharacterAdapter播放待机动画
                characterAdapter.PlayAnimation(characterAdapter.animationMapping.idleAnimation, true);
                Debug.Log($"攻击后使用DND_CharacterAdapter播放待机动画: {characterAdapter.animationMapping.idleAnimation}");
            }
            else if (animController != null) {
                // 如果没有DND_CharacterAdapter，使用AnimationController
                animController.PlayIdle();
                Debug.Log($"攻击后使用AnimationController播放待机动画: {animController.idleAnimation}");
            }
        }
        else {
            Debug.Log($"{attacker.characterName} 已死亡，不恢复待机动画");
        }

        // 恢复战斗状态
        currentState = CombatState.ActionSelection;

        // 清除范围指示器
        RangeManager.Instance.ClearAllIndicators();
    }

    // 执行移动
    public IEnumerator ExecuteMovement(CharacterStats character, Vector3 destination) {
        Debug.Log("开始执行移动");

        if (character == null) {
            Debug.LogError("ExecuteMovement: character为null");
            yield break;
        }

        // 获取ActionSystem组件
        DND5E.ActionSystem actionSystem = character.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasMovement) {
            Debug.LogWarning("没有移动动作可用!");
            yield break;
        }

        // 记录起始位置
        Vector3 startPos = character.transform.position;

        // 检查目标点附近是否有敌人，如果有，则调整目标位置到攻击范围边缘
        // 现在支持Player和Ally标签
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();
        CharacterStats nearestEnemy = null;
        float nearestDistance = float.MaxValue;        // 查找最近的敌人
        foreach (CharacterStats potentialTarget in allCharacters) {
            bool isValidTarget = false;

            // 如果移动的是玩家控制的角色，寻找敌人
            if (character.gameObject.CompareTag("Player") || character.gameObject.CompareTag("Ally")) {
                isValidTarget = potentialTarget.gameObject.CompareTag("Enemy");
            }
            // 如果移动的是敌人，寻找玩家控制的角色（主角或队友）
            else if (character.gameObject.CompareTag("Enemy")) {
                isValidTarget = potentialTarget.gameObject.CompareTag("Player") || potentialTarget.gameObject.CompareTag("Ally");
            }

            if (isValidTarget && potentialTarget.currentHitPoints > 0) {
                float distToTarget = Vector3.Distance(potentialTarget.transform.position, destination);
                if (distToTarget < nearestDistance) {
                    nearestDistance = distToTarget;
                    nearestEnemy = potentialTarget;
                }
            }
        }

        // 如果目标点附近有敌人，且距离小于某个阈值，则调整目标位置
        float proximityThreshold = 2.0f; // 如果目标点距离敌人小于2个单位，认为玩家想要接近这个敌人
        if (nearestEnemy != null && nearestDistance < proximityThreshold) {
            Debug.Log($"检测到目标点附近有敌人: {nearestEnemy.characterName}，距离: {nearestDistance}，调整目标位置到攻击范围边缘");

            // 计算从敌人到角色的方向
            Vector3 dirFromEnemyToChar = (startPos - nearestEnemy.transform.position).normalized;

            // 计算攻击范围边缘的位置（敌人位置 + 方向 * 攻击范围）
            float attackRangeInUnits = RangeManager.Instance.meleeRange * RangeManager.Instance.unitToFeetRatio;

            // 稍微减小一点距离，确保在攻击范围内
            float safeDistance = attackRangeInUnits * 0.9f;

            // 计算新的目标位置
            destination = nearestEnemy.transform.position + dirFromEnemyToChar * safeDistance;

            Debug.Log($"调整后的目标位置: {destination}，攻击范围: {RangeManager.Instance.meleeRange} 尺，单位: {attackRangeInUnits}");
        }

        // 检查目标位置是否在允许的行走区域内，如果不在则调整
        Vector3 validDestination = LevelData.GetValidDestination(startPos, destination);
        if (validDestination != destination) {
            Debug.Log($"目标位置超出行走区域，已调整: {destination} -> {validDestination}");
            destination = validDestination;
        }

        // 确定移动方向（保留Y轴方向）
        Vector3 direction = (destination - startPos).normalized;

        // 计算移动距离（Unity单位）
        float totalDistance = Vector3.Distance(startPos, destination);

        // 转换为尺（假设1单位 = 5尺）
        float totalDistanceInFeet = totalDistance / RangeManager.Instance.unitToFeetRatio;

        // 检查是否超出移动范围
        if (totalDistanceInFeet > actionSystem.movementRemaining) {
            Debug.LogWarning($"移动距离 {totalDistanceInFeet:F1} 尺超出剩余移动范围 {actionSystem.movementRemaining} 尺!");

            // 计算可移动的最大距离（按照DND规则限制）
            float maxMoveDistance = actionSystem.movementRemaining * RangeManager.Instance.unitToFeetRatio;

            // 计算实际目的地（在最大移动距离内）
            destination = startPos + direction * maxMoveDistance;

            // 再次检查调整后的目标位置是否在行走区域内
            destination = LevelData.GetValidDestination(startPos, destination);

            // 更新实际移动距离
            totalDistance = Vector3.Distance(startPos, destination);
            totalDistanceInFeet = totalDistance / RangeManager.Instance.unitToFeetRatio;

            Debug.Log($"已调整移动目标点，最大可移动距离: {actionSystem.movementRemaining} 尺，实际移动: {totalDistanceInFeet:F1} 尺");
        }

        // 设置战斗状态
        currentState = CombatState.ExecutingAction;

        // 首先尝试使用DND_CharacterAdapter播放动画
        DND_CharacterAdapter characterAdapter = character.GetComponent<DND_CharacterAdapter>();
        if (characterAdapter != null) {
            // 使用DND_CharacterAdapter播放walk_shield动画
            characterAdapter.PlayWalkAnimation();
            Debug.Log($"使用DND_CharacterAdapter播放行走动画: {characterAdapter.animationMapping.walkAnimation}");
        }
        else {
            // 如果没有DND_CharacterAdapter，尝试使用AnimationController
            AnimationController animController = character.GetComponent<AnimationController>();
            if (animController != null) {
                animController.PlayWalk();
                Debug.Log($"使用AnimationController播放行走动画: {animController.walkAnimation}");
            }
            else {
                Debug.LogWarning("角色既没有DND_CharacterAdapter也没有AnimationController组件，无法播放动画");
            }
        }

        // 确定朝向
        if (direction.x != 0) {
            // 如果角色有SpriteRenderer，设置flipX
            SpriteRenderer spriteRenderer = character.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) {
                spriteRenderer.flipX = direction.x < 0;
            }

            // 如果角色有SkeletonAnimation，设置scale.x
            Spine.Unity.SkeletonAnimation skeletonAnimation = character.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (skeletonAnimation != null) {
                Vector3 scale = skeletonAnimation.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (direction.x < 0 ? -1 : 1);
                skeletonAnimation.transform.localScale = scale;
            }
        }

        // 使用DOTween进行平滑移动（如果可用）
        if (DOTween.instance != null) {
            // 计算移动时间（基于距离）
            float moveTime = totalDistance / 5f; // 移动速度调整
            moveTime = Mathf.Clamp(moveTime, 0.3f, 2f); // 限制移动时间在合理范围内

            Debug.Log($"使用DOTween移动，距离: {totalDistance:F2}，时间: {moveTime:F2}秒");

            // 使用DOTween移动
            character.transform.DOMove(destination, moveTime).SetEase(Ease.OutQuad);

            // 等待移动完成
            yield return new WaitForSeconds(moveTime);
        }
        else {
            // 备用方案：简单移动
            float moveTime = totalDistance / 5f; // 移动速度调整
            moveTime = Mathf.Clamp(moveTime, 0.3f, 2f); // 限制移动时间在合理范围内
            float elapsedTime = 0;

            Debug.Log($"使用简单移动，距离: {totalDistance:F2}，时间: {moveTime:F2}秒");

            while (elapsedTime < moveTime) {
                character.transform.position = Vector3.Lerp(startPos, destination, elapsedTime / moveTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        // 确保最终位置准确
        character.transform.position = destination;

        // 更新剩余移动距离
        actionSystem.movementRemaining -= (int)totalDistanceInFeet;
        actionSystem.hasMoved = true; // 标记角色已经移动
        Debug.Log($"{character.characterName} 移动了 {totalDistanceInFeet:F1} 尺，剩余 {actionSystem.movementRemaining} 尺，已标记为已移动");

        // 记录移动结果
        if (DND_BattleUI.Instance != null) {
            DND_BattleUI.Instance.AddCombatLog($"{character.GetDisplayName()} 移动了 {totalDistanceInFeet:F1} 尺，剩余 {actionSystem.movementRemaining} 尺");
        }

        // 移动完成后，让角色面朝最近的敌人
        FaceNearestEnemy(character);

        // 只有在角色没有死亡的情况下才恢复待机动画
        if (character.currentHitPoints > 0) {
            if (characterAdapter != null) {
                // 使用DND_CharacterAdapter播放过渡动画
                characterAdapter.StopWalkWithTransition();
                Debug.Log($"移动后使用DND_CharacterAdapter播放过渡动画: {characterAdapter.animationMapping.moveToIdleAnimation}");
            }
            else {
                // 如果没有DND_CharacterAdapter，使用AnimationController的过渡动画
                AnimationController animController = character.GetComponent<AnimationController>();
                if (animController != null) {
                    animController.StopWalkWithTransition();
                    Debug.Log($"移动后使用AnimationController播放过渡动画: {animController.moveToIdleAnimation}");
                }
            }
        }
        else {
            Debug.Log($"{character.characterName} 已死亡，不恢复待机动画");
        }

        // 恢复战斗状态
        currentState = CombatState.ActionSelection;

        // 清除范围指示器
        RangeManager.Instance.ClearAllIndicators();
    }

    // 接战动画
    public void StartApproachAnimation(List<CharacterStats> playerTeam, List<CharacterStats> enemyTeam) {
        if (currentState != CombatState.PreBattle) {
            Debug.LogWarning("当前状态无法开始接战动画: " + currentState);
            return;
        }

        playerTeamForApproach = playerTeam;
        enemyTeamForApproach = enemyTeam;
        approachCompletedCount = 0;
        totalApproachingCharacters = playerTeam.Count + enemyTeam.Count;

        // 禁用所有参与接战的角色的碰撞体，防止在接战过程中发生碰撞
        foreach (CharacterStats character in playerTeam) {
            character.GetComponent<Collider2D>().enabled = false;
        }
        foreach (CharacterStats character in enemyTeam) {
            character.GetComponent<Collider2D>().enabled = false;
        }

        // 开始接战动画协程
        StartCoroutine(ApproachAnimationCoroutine());
    }

    private IEnumerator ApproachAnimationCoroutine() {
        Debug.Log("开始接战动画");

        // 计算所有角色的目标位置
        Vector3[] playerTargets = new Vector3[playerTeamForApproach.Count];
        Vector3[] enemyTargets = new Vector3[enemyTeamForApproach.Count];

        for (int i = 0; i < playerTeamForApproach.Count; i++) {
            CharacterStats character = playerTeamForApproach[i];
            // 计算目标位置为当前角色位置的右侧（朝向敌人方向）
            playerTargets[i] = character.transform.position + Vector3.right * 2f;
        }

        for (int i = 0; i < enemyTeamForApproach.Count; i++) {
            CharacterStats character = enemyTeamForApproach[i];
            // 计算目标位置为当前角色位置的左侧（朝向玩家方向）
            enemyTargets[i] = character.transform.position + Vector3.left * 2f;
        }

        // 动态队伍移动到战斗位置
        yield return StartCoroutine(MoveTeamToPositions(playerTeamForApproach, playerTargets, approachSpeed, approachEaseType));
        yield return StartCoroutine(MoveTeamToPositions(enemyTeamForApproach, enemyTargets, approachSpeed, approachEaseType));

        Debug.Log("接战动画完成");

        // 启用所有角色的碰撞体
        foreach (CharacterStats character in playerTeamForApproach) {
            character.GetComponent<Collider2D>().enabled = true;
        }
        foreach (CharacterStats character in enemyTeamForApproach) {
            character.GetComponent<Collider2D>().enabled = true;
        }

        // 设置战斗状态为阵型就位
        currentState = CombatState.Formation;
    }

    // 移动队伍到指定位置
    private IEnumerator MoveTeamToPositions(List<CharacterStats> team, Vector3[] targets, float speed, Ease easeType) {
        totalApproachingCharacters = team.Count;
        approachCompletedCount = 0;

        for (int i = 0; i < team.Count; i++) {
            CharacterStats character = team[i];
            Vector3 target = targets[i];

            // 计算延迟时间，创造更自然的群体移动效果
            float delay = i * characterMoveDelay;

            // 使用DOTween进行平滑移动
            character.transform.DOMove(target, speed).SetEase(easeType).SetDelay(delay).OnComplete(() => {
                approachCompletedCount++;
                Debug.Log($"{character.characterName} 到达目标位置，当前完成计数: {approachCompletedCount}");

                // 到达目标后，角色面朝最近的敌人
                FaceNearestEnemy(character);
            });

            // 等待一小段时间，确保动画开始
            yield return new WaitForSeconds(0.1f);
        }

        // 等待所有角色都到达目标位置
        while (approachCompletedCount < totalApproachingCharacters) {
            yield return null;
        }

        Debug.Log("所有角色已到达目标位置");
    }

    void Update() {
        // 调试输入 - 强制结束战斗
        if (Input.GetKeyDown(KeyCode.E)) {
            EndCombat();
        }
    }

    /// <summary>
    /// 让所有角色自动朝向对方阵营 - 基于横版线性阵型布局
    /// 玩家方角色朝右，敌人方角色朝左
    /// </summary>
    public void SetAllCharactersFacingDirection() {
        Debug.Log("开始设置所有角色朝向对方阵营");

        // 获取场景中所有角色
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();

        foreach (CharacterStats character in allCharacters) {
            if (character == null || character.currentHitPoints <= 0) {
                continue; // 跳过null或死亡的角色
            }

            SetCharacterFacingDirection(character);
        }

        Debug.Log("所有角色朝向设置完成");
    }

    /// <summary>
    /// 设置单个角色的朝向 - 基于横版线性阵型
    /// 玩家方角色朝右（面向敌人），敌人方角色朝左（面向玩家）
    /// </summary>
    /// <param name="character">要设置朝向的角色</param>
    public void SetCharacterFacingDirection(CharacterStats character) {
        if (character == null) {
            Debug.LogWarning("SetCharacterFacingDirection: 角色为null");
            return;
        }

        bool shouldFaceRight = false;

        // 根据角色标签确定朝向
        if (character.gameObject.CompareTag("Player") || character.gameObject.CompareTag("Ally")) {
            // 玩家方角色朝右（朝向敌人）
            shouldFaceRight = true;
            Debug.Log($"设置玩家方角色 {character.GetDisplayName()} 朝右");
        }
        else if (character.gameObject.CompareTag("Enemy")) {
            // 敌人方角色朝左（朝向玩家）
            shouldFaceRight = false;
            Debug.Log($"设置敌人方角色 {character.GetDisplayName()} 朝左");
        }
        else {
            Debug.LogWarning($"角色 {character.GetDisplayName()} 标签未识别: {character.gameObject.tag}，跳过朝向设置");
            return;
        }

        // 应用朝向设置
        ApplyFacingDirection(character, shouldFaceRight);
    }

    /// <summary>
    /// 应用角色朝向设置
    /// </summary>
    /// <param name="character">角色</param>
    /// <param name="faceRight">是否朝右</param>
    private void ApplyFacingDirection(CharacterStats character, bool faceRight) {
        if (character == null) return;

        // 如果角色有SpriteRenderer，设置flipX
        SpriteRenderer spriteRenderer = character.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
            spriteRenderer.flipX = !faceRight; // flipX=true表示朝左，flipX=false表示朝右
            Debug.Log($"{character.GetDisplayName()} SpriteRenderer朝向设置: flipX={spriteRenderer.flipX} (朝{(faceRight ? "右" : "左")})");
        }

        // 如果角色有SkeletonAnimation，设置scale.x
        Spine.Unity.SkeletonAnimation skeletonAnimation = character.GetComponent<Spine.Unity.SkeletonAnimation>();
        if (skeletonAnimation != null) {
            Vector3 scale = skeletonAnimation.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (faceRight ? 1 : -1); // 正数朝右，负数朝左
            skeletonAnimation.transform.localScale = scale;
            Debug.Log($"{character.GetDisplayName()} SkeletonAnimation朝向设置: scale.x={scale.x} (朝{(faceRight ? "右" : "左")})");
        }

        // 如果既没有SpriteRenderer也没有SkeletonAnimation，记录警告
        if (spriteRenderer == null && skeletonAnimation == null) {
            Debug.LogWarning($"角色 {character.GetDisplayName()} 既没有SpriteRenderer也没有SkeletonAnimation组件，无法设置朝向");
        }
    }    /// <summary>
         /// 在战斗开始时设置所有角色朝向
         /// </summary>
    public void InitializeCharactersFacing() {
        // 延迟一帧执行，确保所有角色都已生成
        StartCoroutine(InitializeCharactersFacingCoroutine());
    }

    /// <summary>
    /// 协程版本的角色朝向初始化，确保在角色生成后执行
    /// </summary>
    private IEnumerator InitializeCharactersFacingCoroutine() {
        // 等待一帧，确保角色生成完成
        yield return null;

        Debug.Log("战斗开始后设置所有角色朝向");
        SetAllCharactersFacingDirection();
    }

    /// <summary>
    /// 让角色面朝最近的敌人
    /// </summary>
    /// <param name="character">需要调整朝向的角色</param>
    private void FaceNearestEnemy(CharacterStats character) {
        if (character == null) return;

        CharacterStats nearestEnemy = GetNearestEnemy(character);
        if (nearestEnemy != null) {
            Vector3 direction = nearestEnemy.transform.position - character.transform.position;
            if (direction.x > 0) {
                // 敌人在右侧，面朝右
                character.transform.localScale = new Vector3(1, 1, 1);
            }
            else if (direction.x < 0) {
                // 敌人在左侧，面朝左
                character.transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }

    /// <summary>
    /// 获取距离指定角色最近的敌人
    /// </summary>
    /// <param name="character">参考角色</param>
    /// <returns>最近的敌人，如果没有则返回null</returns>
    private CharacterStats GetNearestEnemy(CharacterStats character) {
        if (character == null) return null;

        CharacterStats nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        foreach (CharacterStats otherCharacter in initiativeOrder) {
            if (otherCharacter != null && otherCharacter != character) {
                // 使用标签来区分阵营
                bool isEnemy = false;
                if (character.gameObject.CompareTag("Player") || character.gameObject.CompareTag("Ally")) {
                    isEnemy = otherCharacter.gameObject.CompareTag("Enemy");
                }
                else if (character.gameObject.CompareTag("Enemy")) {
                    isEnemy = otherCharacter.gameObject.CompareTag("Player") || otherCharacter.gameObject.CompareTag("Ally");
                }
                if (isEnemy) {
                    float distance = Vector3.Distance(character.transform.position, otherCharacter.transform.position);
                    if (distance < nearestDistance) {
                        nearestDistance = distance;
                        nearestEnemy = otherCharacter;
                    }
                }
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// 开始"探索→遇敌→接战→战斗"的完整流程
    /// 这是系统的主入口点，从探索阶段过渡到战斗阶段
    /// </summary>
    /// <param name="playerTeam">玩家队伍，来自探索阶段</param>
    /// <param name="enemyTeam">敌人队伍，由遇敌事件触发生成</param>
    /// <param name="explorePositions">玩家队伍在探索区的当前位置（可选）</param>
    public void StartExplorationToCombatSequence(List<CharacterStats> playerTeam, List<CharacterStats> enemyTeam, Vector3[] explorePositions = null) {
        Debug.Log("🎬 开始探索到遇敌到接战到战斗的完整流程");

        // 防护性检查
        if (playerTeam == null || playerTeam.Count == 0) {
            Debug.LogError("❌ 玩家队伍为空，无法开始战斗流程！");
            return;
        }

        if (enemyTeam == null || enemyTeam.Count == 0) {
            Debug.LogError("❌ 敌人队伍为空，无法开始战斗流程！");
            return;
        }

        // 保存队伍引用
        playerTeamForApproach = new List<CharacterStats>(playerTeam);
        enemyTeamForApproach = new List<CharacterStats>(enemyTeam);

        // 设置战斗状态为准备阶段
        currentState = CombatState.PreBattle;

        // 如果没有提供探索位置，使用默认探索区位置
        if (explorePositions == null || explorePositions.Length != playerTeam.Count) {
            explorePositions = GenerateDefaultExplorePositions(playerTeam.Count);
        }

        // 将玩家队伍移动到探索区起始位置（如果他们不在那里）
        PositionTeamInExploreArea(playerTeam, explorePositions);

        // 让敌人队伍立即就位到战斗位置（他们本来就在战斗区域等待）
        PositionEnemyTeamInBattleArea(enemyTeam);

        // 延迟一段时间后开始接战动画
        StartCoroutine(DelayedApproachSequence(1f));
    }

    /// <summary>
    /// 生成默认的探索区位置
    /// 探索区位置应该在战斗区域左侧更远的地方
    /// </summary>
    /// <param name="teamSize">队伍大小</param>
    /// <returns>探索区位置数组</returns>
    private Vector3[] GenerateDefaultExplorePositions(int teamSize) {
        Vector3[] positions = new Vector3[teamSize];

        // 探索区在战斗区域左侧8个单位的地方
        float exploreX = -8f;
        float startY = (teamSize - 1) * 1f * 0.5f; // 队伍居中分布

        for (int i = 0; i < teamSize; i++) {
            positions[i] = new Vector3(exploreX, startY - i * 1f, 0);
        }

        return positions;
    }

    /// <summary>
    /// 将玩家队伍定位到探索区
    /// </summary>
    /// <param name="playerTeam">玩家队伍</param>
    /// <param name="explorePositions">探索区位置</param>
    private void PositionTeamInExploreArea(List<CharacterStats> playerTeam, Vector3[] explorePositions) {
        for (int i = 0; i < playerTeam.Count && i < explorePositions.Length; i++) {
            if (playerTeam[i] != null && playerTeam[i].transform != null) {
                playerTeam[i].transform.position = explorePositions[i];
                // 让角色面朝右（战斗方向）
                playerTeam[i].transform.localScale = new Vector3(1, 1, 1);
            }
        }

        Debug.Log($"🏁 玩家队伍已定位到探索区，{playerTeam.Count}名角色就位");
    }

    /// <summary>
    /// 将敌人队伍定位到战斗区域
    /// 敌人直接传送到最终战斗位置，无需接战动画
    /// </summary>
    /// <param name="enemyTeam">敌人队伍</param>
    private void PositionEnemyTeamInBattleArea(List<CharacterStats> enemyTeam) {
        if (HorizontalBattleFormationManager.Instance == null) {
            Debug.LogError("❌ HorizontalBattleFormationManager实例为空！");
            return;
        }

        // 使用HorizontalBattleFormationManager的现有系统来排列敌人
        HorizontalBattleFormationManager.Instance.ArrangeExistingTeam(enemyTeam, BattleSide.Enemy);

        // 让敌人面朝左（朝向玩家）
        foreach (CharacterStats enemy in enemyTeam) {
            if (enemy != null && enemy.transform != null) {
                enemy.transform.localScale = new Vector3(-1, 1, 1);
            }
        }

        Debug.Log($"⚔️ 敌人队伍已就位到战斗区域，{enemyTeam.Count}名敌人等待接战");
    }

    /// <summary>
    /// 延迟后开始接战序列的协程
    /// </summary>
    /// <param name="delay">延迟时间</param>
    /// <returns>协程枚举器</returns>
    private IEnumerator DelayedApproachSequence(float delay) {
        yield return new WaitForSeconds(delay);
        StartApproachSequence();
    }

    /// <summary>
    /// 开始接战动画序列
    /// 玩家队伍从探索区动态移动到战斗位置
    /// </summary>
    public void StartApproachSequence() {
        Debug.Log("🚀 开始接战动画序列");

        currentState = CombatState.Approaching;

        // 重置动画完成计数器
        approachCompletedCount = 0;
        totalApproachingCharacters = playerTeamForApproach.Count;

        // 获取玩家队伍的目标战斗位置
        Vector3[] battlePositions = GetPlayerBattlePositions();

        if (battlePositions == null || battlePositions.Length < playerTeamForApproach.Count) {
            Debug.LogError("❌ 无法获取足够的战斗位置！");
            return;
        }

        // 为每个玩家角色启动移动动画
        for (int i = 0; i < playerTeamForApproach.Count; i++) {
            if (playerTeamForApproach[i] != null && i < battlePositions.Length) {
                StartCharacterApproachAnimation(playerTeamForApproach[i], battlePositions[i], i);
            }
        }
    }

    /// <summary>
    /// 获取玩家队伍在战斗中的目标位置
    /// </summary>
    /// <returns>战斗位置数组</returns>
    private Vector3[] GetPlayerBattlePositions() {
        if (HorizontalBattleFormationManager.Instance == null) {
            Debug.LogError("❌ HorizontalBattleFormationManager实例为空！");
            return null;
        }

        // 获取自动生成的玩家spawn点位置
        Transform[] playerSpawnPoints = HorizontalBattleFormationManager.Instance.GetPlayerSpawnPoints();

        if (playerSpawnPoints == null) {
            Debug.LogError("❌ 无法获取玩家spawn点！");
            return null;
        }

        Vector3[] positions = new Vector3[playerSpawnPoints.Length];
        for (int i = 0; i < playerSpawnPoints.Length && i < playerTeamForApproach.Count; i++) {
            if (playerSpawnPoints[i] != null) {
                positions[i] = playerSpawnPoints[i].position;
            }
        }

        return positions;
    }

    /// <summary>
    /// 启动单个角色的接战移动动画
    /// </summary>
    /// <param name="character">要移动的角色</param>
    /// <param name="targetPosition">目标位置</param>
    /// <param name="characterIndex">角色索引，用于计算延迟</param>
    private void StartCharacterApproachAnimation(CharacterStats character, Vector3 targetPosition, int characterIndex) {
        if (character == null || character.transform == null) {
            OnCharacterApproachCompleted();
            return;
        }

        // 计算移动延迟，让角色依次启动移动，创造更自然的效果
        float moveDelay = characterIndex * characterMoveDelay;

        // 计算移动距离和时间
        float distance = Vector3.Distance(character.transform.position, targetPosition);
        float moveDuration = distance / approachSpeed;

        Debug.Log($"🏃 {character.characterName} 开始接战移动：从 {character.transform.position} 到 {targetPosition}，延迟 {moveDelay}s，持续时间 {moveDuration}s");

        // 使用DOTween创建移动动画
        character.transform.DOMove(targetPosition, moveDuration)
            .SetDelay(moveDelay)
            .SetEase(approachEaseType)
            .OnComplete(() => {
                Debug.Log($"✅ {character.characterName} 接战移动完成");
                OnCharacterApproachCompleted();
            });
    }

    /// <summary>
    /// 当单个角色完成接战移动时调用
    /// </summary>
    private void OnCharacterApproachCompleted() {
        approachCompletedCount++;

        Debug.Log($"📊 接战进度：{approachCompletedCount}/{totalApproachingCharacters}");

        // 检查是否所有角色都完成了移动
        if (approachCompletedCount >= totalApproachingCharacters) {
            OnAllCharactersApproachCompleted();
        }
    }

    /// <summary>
    /// 当所有角色完成接战移动时调用
    /// </summary>
    private void OnAllCharactersApproachCompleted() {
        Debug.Log("🎉 所有角色接战移动完成，进入阵型就位阶段");

        currentState = CombatState.Formation;

        // 短暂停顿后开始正式战斗
        StartCoroutine(StartFormalCombatAfterDelay(1f));
    }

    /// <summary>
    /// 延迟后开始正式战斗的协程
    /// </summary>
    /// <param name="delay">延迟时间</param>
    /// <returns>协程枚举器</returns>
    private IEnumerator StartFormalCombatAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        StartFormalCombat();
    }

    /// <summary>
    /// 开始正式战斗
    /// 从接战阶段过渡到标准战斗流程
    /// </summary>
    private void StartFormalCombat() {
        Debug.Log("⚔️ 开始正式战斗！");

        // 合并玩家和敌人队伍
        List<CharacterStats> allCombatants = new List<CharacterStats>();
        allCombatants.AddRange(playerTeamForApproach);
        allCombatants.AddRange(enemyTeamForApproach);

        // 使用标准战斗流程
        StartCombat(allCombatants);
    }
}
