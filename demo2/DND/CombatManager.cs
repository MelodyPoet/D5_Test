using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Spine.Unity; // 引用Spine命名空间
using DG.Tweening; // 引用DOTween命名空间

namespace DND5E
{
    // 攻击类型枚举
    public enum AttackType
    {
        Melee,      // 近战
        Ranged,     // 远程
        Spell       // 法术
    }
}

public class CombatManager : MonoBehaviour
{
    // 单例实例
    public static CombatManager Instance { get; private set; }

    // 战斗状态枚举
    public enum CombatState
    {
        Inactive,       // 未激活
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

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 开始战斗
    public void StartCombat(List<CharacterStats> participants)
    {
        if (isCombatActive)
        {
            Debug.LogWarning("战斗已经在进行中!");
            return;
        }

        // 清空先攻顺序
        initiativeOrder.Clear();

        // 添加参与者
        initiativeOrder.AddRange(participants);

        foreach (var character in participants)
        {
            if (character != null)
            {
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
        OnCombatStart?.Invoke();

        // 在战斗日志中添加战斗开始信息
        if (DND_BattleUI.Instance != null)
        {
            DND_BattleUI.Instance.AddCombatLog("=== 战斗开始 ===");
        }

        // 开始第一个回合
        StartNextTurn();
    }

    // 掷先攻
    private void RollInitiative()
    {
        // 为每个角色掷先攻
        Dictionary<CharacterStats, int> initiativeRolls = new Dictionary<CharacterStats, int>();

        foreach (var character in initiativeOrder)
        {
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
        for (int i = 0; i < initiativeOrder.Count; i++)
        {
            Debug.Log($"{i+1}. {initiativeOrder[i].characterName} - {initiativeRolls[initiativeOrder[i]]}");
        }
    }

    // 开始下一个回合
    public void StartNextTurn()
    {
        Debug.Log("【重要】开始下一个回合");

        // 检查战斗是否结束
        if (CheckCombatEnd())
        {
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
        if (currentTurnIndex == 0)
        {
            currentRound++;
            OnRoundStart?.Invoke();
            Debug.Log($"【重要】第 {currentRound} 回合开始!");

            // 在战斗日志中添加回合开始信息
            if (DND_BattleUI.Instance != null)
            {
                DND_BattleUI.Instance.AddCombatLog($"=== 第 {currentRound} 回合开始 ===");
            }
        }

        // 获取当前角色
        CharacterStats character = currentCharacter;
        if (character == null)
        {
            Debug.LogError("当前角色为null，无法开始回合");
            return;
        }

        Debug.Log($"当前角色: {character.characterName}, 标签: {character.gameObject.tag}");

        // 检查角色是否失去意识
        if (character.HasStatusEffect(DND5E.StatusEffectType.Unconscious))
        {
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
        if (DND_BattleUI.Instance != null)
        {
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
    public void EndCurrentTurn()
    {
        Debug.Log($"【重要】尝试结束当前回合");

        // 获取当前角色
        CharacterStats character = currentCharacter;

        if (character == null)
        {
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
        if (DND_BattleUI.Instance != null)
        {
            DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 的回合结束");
        }

        // 如果是最后一个角色，触发回合结束事件
        if (currentTurnIndex == initiativeOrder.Count - 1)
        {
            OnRoundEnd?.Invoke();
            Debug.Log($"第 {currentRound} 回合结束!");

            // 在战斗日志中添加回合结束信息
            if (DND_BattleUI.Instance != null)
            {
                DND_BattleUI.Instance.AddCombatLog($"=== 第 {currentRound} 回合结束 ===");
            }
        }

        // 开始下一个回合
        StartNextTurn();
    }

    // 结束战斗
    public void EndCombat()
    {
        // 设置战斗状态
        currentState = CombatState.CombatEnd;

        // 触发战斗结束事件
        OnCombatEnd?.Invoke();

        Debug.Log("战斗结束!");

        // 在战斗日志中添加战斗结束信息
        if (DND_BattleUI.Instance != null)
        {
            DND_BattleUI.Instance.AddCombatLog("=== 战斗结束 ===");
        }

        // 重置战斗状态
        currentState = CombatState.Inactive;
        initiativeOrder.Clear();
        currentTurnIndex = 0;
        currentRound = 0;
    }

    // 检查战斗是否结束
    private bool CheckCombatEnd()
    {
        // 检查是否所有敌人都已经失去意识
        bool allEnemiesDefeated = true;
        bool allPlayersDefeated = true;

        foreach (var character in initiativeOrder)
        {
            // 假设玩家角色的标签是"Player"，敌人的标签是"Enemy"
            if (character.gameObject.CompareTag("Player"))
            {
                if (!character.HasStatusEffect(DND5E.StatusEffectType.Unconscious))
                {
                    allPlayersDefeated = false;
                }
            }
            else if (character.gameObject.CompareTag("Enemy"))
            {
                if (!character.HasStatusEffect(DND5E.StatusEffectType.Unconscious))
                {
                    allEnemiesDefeated = false;
                }
            }
        }

        return allEnemiesDefeated || allPlayersDefeated;
    }

    // 重置角色行动状态
    private void ResetCharacterActions(CharacterStats character)
    {
        // 获取角色的ActionSystem组件
        DND5E.ActionSystem actionSystem = character.GetComponent<DND5E.ActionSystem>();
        if (actionSystem != null)
        {
            // 重置行动状态
            actionSystem.ResetActions();
        }
        else
        {
            Debug.LogWarning($"{character.characterName} 没有ActionSystem组件，无法重置行动状态");
        }
    }

    // 处理状态效果
    private void ProcessStatusEffects(CharacterStats character)
    {
        // 处理各种状态效果
        foreach (var effect in character.statusEffects.ToList())
        {
            switch (effect)
            {
                case DND5E.StatusEffectType.Poisoned:
                    // 中毒状态每回合造成伤害
                    int poisonDamage = Random.Range(1, 5); // 1d4伤害
                    character.TakeDamage(poisonDamage, DND5E.DamageType.Poison);

                    // 确保UI更新
                    if (DND_BattleUI.Instance != null)
                    {
                        DND_BattleUI.Instance.UpdateCharacterStatusUI(character);
                    }

                    Debug.Log($"{character.characterName} 受到中毒效果影响，损失{poisonDamage}点生命值");

                    // 在战斗日志中添加中毒效果信息
                    if (DND_BattleUI.Instance != null)
                    {
                        DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 受到中毒效果影响，损失 {poisonDamage} 点生命值");
                    }
                    break;

                case DND5E.StatusEffectType.Stunned:
                    // 震慑状态无法行动
                    Debug.Log($"{character.characterName} 处于震慑状态，本回合无法行动");

                    // 在战斗日志中添加震慑状态信息
                    if (DND_BattleUI.Instance != null)
                    {
                        DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 处于震慑状态，本回合无法行动");
                    }
                    break;

                case DND5E.StatusEffectType.Paralyzed:
                    // 麻痹状态无法行动
                    Debug.Log($"{character.characterName} 处于麻痹状态，本回合无法行动");

                    // 在战斗日志中添加麻痹状态信息
                    if (DND_BattleUI.Instance != null)
                    {
                        DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 处于麻痹状态，本回合无法行动");
                    }
                    break;

                case DND5E.StatusEffectType.Dodging:
                    // 防御姿态在下回合开始时移除
                    Debug.Log($"{character.characterName} 处于防御姿态，AC+2，将在下回合开始时移除");

                    // 在战斗日志中添加防御姿态状态信息
                    if (DND_BattleUI.Instance != null)
                    {
                        DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {character.GetDisplayName()} 处于防御姿态，AC+2");
                    }
                    break;

                // 其他状态效果...
            }
        }

        // 输出当前状态效果
        if (character.statusEffects.Count > 0)
        {
            Debug.Log($"{character.characterName} 当前状态效果: {string.Join(", ", character.statusEffects)}");
        }
    }

    // 处理回合开始时的状态效果清理
    private void ProcessStartOfTurnStatusEffects(CharacterStats character)
    {
        // 移除防御姿态（在新回合开始时移除）
        if (character.HasStatusEffect(DND5E.StatusEffectType.Dodging))
        {
            Debug.Log($"{character.characterName} 的防御姿态在新回合开始时移除");
            character.RemoveStatusEffect(DND5E.StatusEffectType.Dodging);

            // 确保UI更新
            if (DND_BattleUI.Instance != null)
            {
                DND_BattleUI.Instance.UpdateCharacterStatusUI(character);
            }
        }

        // 可以在这里处理其他在回合开始时需要清理的状态效果
    }

    // 处理回合结束时的状态效果
    private void ProcessEndOfTurnStatusEffects(CharacterStats character)
    {
        // 防御姿态不在回合结束时移除，而是在下回合开始时移除
        // 这样可以确保防御姿态持续整个回合周期

        // 可以在这里处理其他在回合结束时需要处理的状态效果
        // 例如：中毒、燃烧等持续伤害效果
    }

    // 执行攻击
    public IEnumerator ExecuteAttack(CharacterStats attacker, CharacterStats target, DND5E.AttackType attackType, string ability, string damageFormula, DND5E.DamageType damageType)
    {
        // 检查是否有主要动作
        DND5E.ActionSystem actionSystem = attacker.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasAction)
        {
            Debug.LogWarning($"{attacker.characterName} 没有主要动作可用!");
            yield break;
        }

        // 检查攻击距离
        float rangeInFeet = 0;
        switch (attackType)
        {
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
        if (distanceInFeet > rangeInFeet)
        {
            Debug.LogWarning($"{attacker.characterName} 与目标距离 {distanceInFeet:F1} 尺，超出 {attackType} 攻击范围 {rangeInFeet} 尺!");
            yield break;
        }

        // 设置战斗状态
        currentState = CombatState.ExecutingAction;

        // 使用动作
        actionSystem.UseAction(DND5E.ActionType.Action);

        // 确定朝向
        Vector3 direction = (target.transform.position - attacker.transform.position).normalized;
        if (direction.x != 0)
        {
            // 如果角色有SpriteRenderer，设置flipX
            SpriteRenderer spriteRenderer = attacker.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0;
            }

            // 如果角色有SkeletonAnimation，设置scale.x
            Spine.Unity.SkeletonAnimation skeletonAnimation = attacker.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (skeletonAnimation != null)
            {
                Vector3 scale = skeletonAnimation.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (direction.x < 0 ? -1 : 1);
                skeletonAnimation.transform.localScale = scale;
            }
        }

        // 首先尝试使用DND_CharacterAdapter播放攻击动画
        DND_CharacterAdapter characterAdapter = attacker.GetComponent<DND_CharacterAdapter>();
        AnimationController animController = null;
        float animationDelay = 0.5f;

        if (characterAdapter != null)
        {
            // 根据攻击类型播放不同动画
            switch (attackType)
            {
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
            if (characterAdapter.skeletonAnimation != null && characterAdapter.skeletonAnimation.AnimationState.GetCurrent(0) != null)
            {
                float duration = characterAdapter.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Duration;
                animationDelay = duration * 0.5f; // 在动画中间点计算伤害
                Debug.Log($"攻击动画持续时间: {duration}秒, 延迟: {animationDelay}秒");
            }
        }
        else
        {
            // 如果没有DND_CharacterAdapter，尝试使用AnimationController
            animController = attacker.GetComponent<AnimationController>();
            if (animController != null)
            {
                // 根据攻击类型播放不同动画
                switch (attackType)
                {
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
                switch (attackType)
                {
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
                if (duration > 0)
                {
                    animationDelay = duration * 0.5f; // 在动画中间点计算伤害
                    Debug.Log($"攻击动画持续时间: {duration}秒, 延迟: {animationDelay}秒");
                }
            }
            else
            {
                Debug.LogWarning("角色既没有DND_CharacterAdapter也没有AnimationController组件，无法播放攻击动画");
            }
        }

        yield return new WaitForSeconds(animationDelay);

        // 确定是否有优势或劣势
        bool hasAdvantage = false;
        bool hasDisadvantage = false;

        // 检查目标是否有影响攻击的状态效果
        if (target.HasStatusEffect(DND5E.StatusEffectType.Prone) && attackType == DND5E.AttackType.Melee)
        {
            hasAdvantage = true;
        }

        if (target.HasStatusEffect(DND5E.StatusEffectType.Invisible))
        {
            hasDisadvantage = true;
        }

        // 进行攻击掷骰
        int attackRoll = attacker.AttackRoll(ability, hasAdvantage, hasDisadvantage);

        // 记录攻击掷骰结果
        string attackTypeStr = "";
        switch (attackType)
        {
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
        switch(ability.ToLower())
        {
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
        if (DND_BattleUI.Instance != null)
        {
            DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {attacker.GetDisplayName()} 进行{attackTypeStr}攻击{advantageStr}");
            DND_BattleUI.Instance.AddCombatLog($"攻击检定: {attackFormula} = {attackRoll} vs AC {target.armorClass}");
        }

        // 检查是否命中
        if (attackRoll >= target.armorClass)
        {
            // 计算伤害
            int damage = attacker.CalculateDamage(ability, damageFormula);

            // 重用之前计算的属性加值，不需要重新声明变量
            // 根据能力值重新确定属性加值
            switch(ability.ToLower())
            {
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

            if(parts.Length == 2)
            {
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
            if (DND_BattleUI.Instance != null)
            {
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
            if (targetAdapter != null)
            {
                Debug.Log($"调用 {target.characterName} 的TakeDamage方法播放受击动画");
                targetAdapter.TakeDamage();
            }
            else
            {
                // 如果没有DND_CharacterAdapter，尝试使用AnimationController
                targetAnimController = target.GetComponent<AnimationController>();
                if (targetAnimController != null)
                {
                    targetAnimController.PlayHit();
                    Debug.Log($"使用AnimationController播放目标受击动画: {targetAnimController.hitAnimation}");
                }
                else
                {
                    Debug.LogWarning($"目标 {target.characterName} 既没有DND_CharacterAdapter也没有AnimationController组件，无法播放受击动画");
                }
            }

            // 等待一小段时间，确保受击动画有时间播放
            yield return new WaitForSeconds(0.2f);

            // 造成伤害
            target.TakeDamage(damage, damageType);

            // 确保UI更新
            if (DND_BattleUI.Instance != null)
            {
                DND_BattleUI.Instance.UpdateCharacterStatusUI(target);
            }

            Debug.Log($"{attacker.characterName} 对 {target.characterName} 进行 {attackType} 攻击，命中! 造成 {damage} 点 {damageType} 伤害!");

            // 检查目标是否死亡
            if (target.currentHitPoints <= 0)
            {
                yield return new WaitForSeconds(0.5f); // 给受击动画更多时间

                if (targetAdapter != null)
                {
                    // 使用DND_CharacterAdapter播放死亡动画
                    targetAdapter.PlayDeathAnimation();
                    Debug.Log($"使用DND_CharacterAdapter播放目标死亡动画: {targetAdapter.animationMapping.deathAnimation}");
                }
                else if (targetAnimController != null)
                {
                    // 如果没有DND_CharacterAdapter，使用AnimationController
                    targetAnimController.PlayDeath();
                    Debug.Log($"使用AnimationController播放目标死亡动画: {targetAnimController.deathAnimation}");
                }
            }
        }
        else
        {
            // 播放闪避动画
            // 注意：DND_CharacterAdapter没有专门的闪避动画方法，可以考虑添加
            // 这里我们暂时只使用AnimationController
            AnimationController targetAnimController = target.GetComponent<AnimationController>();
            if (targetAnimController != null)
            {
                targetAnimController.PlayDodge();
                Debug.Log($"使用AnimationController播放目标闪避动画: {targetAnimController.dodgeAnimation}");
            }

            Debug.Log($"{attacker.characterName} 对 {target.characterName} 进行 {attackType} 攻击，未命中!");

            // 显示Miss文本
            if (DamageNumberManager.Instance != null)
            {
                DamageNumberManager.Instance.ShowMissText(target.transform);
            }

            // 记录未命中结果
            if (DND_BattleUI.Instance != null)
            {
                DND_BattleUI.Instance.AddCombatLog($"{attacker.GetDisplayName()} 未命中 {target.GetDisplayName()}!");
            }
        }

        // 等待动画完成
        yield return new WaitForSeconds(0.5f);

        // 只有在攻击者没有死亡的情况下才恢复待机动画
        if (attacker.currentHitPoints > 0)
        {
            if (characterAdapter != null)
            {
                // 使用DND_CharacterAdapter播放待机动画
                characterAdapter.PlayAnimation(characterAdapter.animationMapping.idleAnimation, true);
                Debug.Log($"攻击后使用DND_CharacterAdapter播放待机动画: {characterAdapter.animationMapping.idleAnimation}");
            }
            else if (animController != null)
            {
                // 如果没有DND_CharacterAdapter，使用AnimationController
                animController.PlayIdle();
                Debug.Log($"攻击后使用AnimationController播放待机动画: {animController.idleAnimation}");
            }
        }
        else
        {
            Debug.Log($"{attacker.characterName} 已死亡，不恢复待机动画");
        }

        // 恢复战斗状态
        currentState = CombatState.ActionSelection;

        // 清除范围指示器
        RangeManager.Instance.ClearAllIndicators();
    }

    // 执行移动
    public IEnumerator ExecuteMovement(CharacterStats character, Vector3 destination)
    {
        Debug.Log("开始执行移动");

        if (character == null)
        {
            Debug.LogError("ExecuteMovement: character为null");
            yield break;
        }

        // 获取ActionSystem组件
        DND5E.ActionSystem actionSystem = character.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasMovement)
        {
            Debug.LogWarning("没有移动动作可用!");
            yield break;
        }

        // 记录起始位置
        Vector3 startPos = character.transform.position;

        // 检查目标点附近是否有敌人，如果有，则调整目标位置到攻击范围边缘
        // 现在支持Player和Ally标签
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();
        CharacterStats nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        // 查找最近的敌人
        foreach (var potentialTarget in allCharacters)
        {
            bool isValidTarget = false;

            // 如果移动的是玩家控制的角色，寻找敌人
            if (character.gameObject.CompareTag("Player") || character.gameObject.CompareTag("Ally"))
            {
                isValidTarget = potentialTarget.gameObject.CompareTag("Enemy");
            }
            // 如果移动的是敌人，寻找玩家控制的角色（主角或队友）
            else if (character.gameObject.CompareTag("Enemy"))
            {
                isValidTarget = potentialTarget.gameObject.CompareTag("Player") || potentialTarget.gameObject.CompareTag("Ally");
            }

            if (isValidTarget && potentialTarget.currentHitPoints > 0)
            {
                float distToTarget = Vector3.Distance(potentialTarget.transform.position, destination);
                if (distToTarget < nearestDistance)
                {
                    nearestDistance = distToTarget;
                    nearestEnemy = potentialTarget;
                }
            }
        }

        // 如果目标点附近有敌人，且距离小于某个阈值，则调整目标位置
        float proximityThreshold = 2.0f; // 如果目标点距离敌人小于2个单位，认为玩家想要接近这个敌人
        if (nearestEnemy != null && nearestDistance < proximityThreshold)
        {
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
        if (validDestination != destination)
        {
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
        if (totalDistanceInFeet > actionSystem.movementRemaining)
        {
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
        if (characterAdapter != null)
        {
            // 使用DND_CharacterAdapter播放walk_shield动画
            characterAdapter.PlayWalkAnimation();
            Debug.Log($"使用DND_CharacterAdapter播放行走动画: {characterAdapter.animationMapping.walkAnimation}");
        }
        else
        {
            // 如果没有DND_CharacterAdapter，尝试使用AnimationController
            AnimationController animController = character.GetComponent<AnimationController>();
            if (animController != null)
            {
                animController.PlayWalk();
                Debug.Log($"使用AnimationController播放行走动画: {animController.walkAnimation}");
            }
            else
            {
                Debug.LogWarning("角色既没有DND_CharacterAdapter也没有AnimationController组件，无法播放动画");
            }
        }

        // 确定朝向
        if (direction.x != 0)
        {
            // 如果角色有SpriteRenderer，设置flipX
            SpriteRenderer spriteRenderer = character.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0;
            }

            // 如果角色有SkeletonAnimation，设置scale.x
            Spine.Unity.SkeletonAnimation skeletonAnimation = character.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (skeletonAnimation != null)
            {
                Vector3 scale = skeletonAnimation.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (direction.x < 0 ? -1 : 1);
                skeletonAnimation.transform.localScale = scale;
            }
        }

        // 使用DOTween进行平滑移动（如果可用）
        if (DOTween.instance != null)
        {
            // 计算移动时间（基于距离）
            float moveTime = totalDistance / 5f; // 移动速度调整
            moveTime = Mathf.Clamp(moveTime, 0.3f, 2f); // 限制移动时间在合理范围内

            Debug.Log($"使用DOTween移动，距离: {totalDistance:F2}，时间: {moveTime:F2}秒");

            // 使用DOTween移动
            character.transform.DOMove(destination, moveTime).SetEase(Ease.OutQuad);

            // 等待移动完成
            yield return new WaitForSeconds(moveTime);
        }
        else
        {
            // 备用方案：简单移动
            float moveTime = totalDistance / 5f; // 移动速度调整
            moveTime = Mathf.Clamp(moveTime, 0.3f, 2f); // 限制移动时间在合理范围内
            float elapsedTime = 0;

            Debug.Log($"使用简单移动，距离: {totalDistance:F2}，时间: {moveTime:F2}秒");

            while (elapsedTime < moveTime)
            {
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
        if (DND_BattleUI.Instance != null)
        {
            DND_BattleUI.Instance.AddCombatLog($"{character.GetDisplayName()} 移动了 {totalDistanceInFeet:F1} 尺，剩余 {actionSystem.movementRemaining} 尺");
        }

        // 移动完成后，让角色面朝最近的敌人
        FaceNearestEnemy(character);

        // 只有在角色没有死亡的情况下才恢复待机动画
        if (character.currentHitPoints > 0)
        {
            if (characterAdapter != null)
            {
                // 使用DND_CharacterAdapter播放过渡动画
                characterAdapter.StopWalkWithTransition();
                Debug.Log($"移动后使用DND_CharacterAdapter播放过渡动画: {characterAdapter.animationMapping.moveToIdleAnimation}");
            }
            else
            {
                // 如果没有DND_CharacterAdapter，使用AnimationController的过渡动画
                AnimationController animController = character.GetComponent<AnimationController>();
                if (animController != null)
                {
                    animController.StopWalkWithTransition();
                    Debug.Log($"移动后使用AnimationController播放过渡动画: {animController.moveToIdleAnimation}");
                }
            }
        }
        else
        {
            Debug.Log($"{character.characterName} 已死亡，不恢复待机动画");
        }

        // 恢复战斗状态
        currentState = CombatState.ActionSelection;

        // 清除范围指示器
        RangeManager.Instance.ClearAllIndicators();
    }

    // 让角色面朝最近的敌人
    private void FaceNearestEnemy(CharacterStats character)
    {
        if (character == null || character.currentHitPoints <= 0)
        {
            return; // 死亡角色不需要调整朝向
        }

        // 确定敌对标签
        string[] enemyTags;
        if (character.gameObject.CompareTag("Player") || character.gameObject.CompareTag("Ally"))
        {
            enemyTags = new string[] { "Enemy" }; // 玩家和队友的敌人是怪物
        }
        else if (character.gameObject.CompareTag("Enemy"))
        {
            enemyTags = new string[] { "Player", "Ally" }; // 怪物的敌人是玩家和队友
        }
        else
        {
            return; // 未知标签，不处理
        }

        // 查找最近的敌人
        CharacterStats nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();
        foreach (var otherCharacter in allCharacters)
        {
            // 跳过自己和死亡的角色
            if (otherCharacter == character || otherCharacter.currentHitPoints <= 0)
                continue;

            // 检查是否是敌人
            bool isEnemy = false;
            foreach (string enemyTag in enemyTags)
            {
                if (otherCharacter.gameObject.CompareTag(enemyTag))
                {
                    isEnemy = true;
                    break;
                }
            }

            if (!isEnemy) continue;

            // 计算距离
            float distance = Vector3.Distance(character.transform.position, otherCharacter.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = otherCharacter;
            }
        }

        // 如果找到了最近的敌人，让角色面朝敌人
        if (nearestEnemy != null)
        {
            FaceTarget(character, nearestEnemy.gameObject);
            Debug.Log($"{character.GetDisplayName()} 移动后面朝最近的敌人: {nearestEnemy.GetDisplayName()}，距离: {nearestDistance:F1}");
        }
        else
        {
            Debug.Log($"{character.GetDisplayName()} 移动后没有找到敌人，保持当前朝向");
        }
    }

    // 让角色面朝目标
    private void FaceTarget(CharacterStats character, GameObject target)
    {
        if (character == null || target == null) return;

        // 计算朝向目标的方向
        Vector3 direction = (target.transform.position - character.transform.position).normalized;

        if (direction.x != 0)
        {
            // 如果角色有SpriteRenderer，设置flipX
            SpriteRenderer spriteRenderer = character.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0;
                Debug.Log($"{character.GetDisplayName()} 使用SpriteRenderer面朝目标，flipX: {spriteRenderer.flipX}");
            }

            // 如果角色有SkeletonAnimation，设置scale.x
            Spine.Unity.SkeletonAnimation skeletonAnimation = character.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (skeletonAnimation != null)
            {
                Vector3 scale = skeletonAnimation.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (direction.x < 0 ? -1 : 1);
                skeletonAnimation.transform.localScale = scale;
                Debug.Log($"{character.GetDisplayName()} 使用SkeletonAnimation面朝目标，scale.x: {scale.x}");
            }
        }
    }

    // 执行技能检定
    public bool ExecuteSkillCheck(CharacterStats character, DND5E.CharacterStats.Skill skill, int difficultyClass)
    {
        // 进行技能检定
        int result = character.SkillCheck(skill);

        // 检查是否成功
        bool success = result >= difficultyClass;

        // 记录技能检定结果
        if (DND_BattleUI.Instance != null)
        {
            DND_BattleUI.Instance.AddCombatLog($"{character.GetDisplayName()} 进行 {skill} 检定: {result} vs DC {difficultyClass}, {(success ? "成功!" : "失败!")}");
        }

        return success;
    }

    // 执行豁免检定
    public bool ExecuteSavingThrow(CharacterStats character, string ability, int difficultyClass)
    {
        // 进行豁免检定
        int result = character.SavingThrow(ability);

        // 检查是否成功
        bool success = result >= difficultyClass;

        // 记录豁免检定结果
        if (DND_BattleUI.Instance != null)
        {
            DND_BattleUI.Instance.AddCombatLog($"{character.GetDisplayName()} 进行 {ability} 豁免检定: {result} vs DC {difficultyClass}, {(success ? "成功!" : "失败!")}");
        }

        return success;
    }


}
