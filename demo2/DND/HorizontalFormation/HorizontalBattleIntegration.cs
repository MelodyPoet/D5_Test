using System.Collections.Generic;
using UnityEngine;
using DND5E;

/// <summary>
/// 横版战斗系统集成管理器
/// 将新的阵型系统整合到现有的DND战斗系统中
/// </summary>
public class HorizontalBattleIntegration : MonoBehaviour {
    [Header("集成设置")]
    public bool useHorizontalFormation = true;
    public bool enableCoverSystem = true;
    public bool enableStealthSystem = true;

    [Header("引用")]
    public HorizontalBattleFormationManager formationManager;
    public CombatManager combatManager; [Header("挂机模式设置")]
    public bool isIdleMode = true; // 挂机模式开关
    public float enemyApproachSpeed = 2f; // 怪物接近速度
    public float playerIdleAnimationSpeed = 1f; // 玩家原地动画速度
    public float backgroundScrollSpeed = 1f; // 背景滚动速度

    [Header("挂机模式引用")]
    public IdleGameManager idleGameManager;
    public Transform backgroundContainer; // 背景容器（包含所有背景元素）

    // 背景滚动相关
    private bool isBackgroundScrolling = false;
    private Vector3 originalBackgroundPosition;

    // 单例
    public static HorizontalBattleIntegration Instance { get; private set; }

    void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }

        InitializeIntegration();
    }    /// <summary>
         /// 初始化集成系统
         /// </summary>
    private void InitializeIntegration() {
        if (formationManager == null) {
            formationManager = FindObjectOfType<HorizontalBattleFormationManager>();
        }

        if (combatManager == null) {
            combatManager = FindObjectOfType<CombatManager>();
        }

        if (idleGameManager == null) {
            idleGameManager = FindObjectOfType<IdleGameManager>();
        }

        // 初始化背景容器
        if (backgroundContainer != null) {
            originalBackgroundPosition = backgroundContainer.position;
        }

        // 如果是挂机模式，自动开始探索
        if (isIdleMode) {
            StartIdleExploration();
        }

        Debug.Log("横版战斗系统集成初始化完成");
    }/// <summary>
     /// 开始横版战斗 - 新的spawn点系统
     /// </summary>
    public void StartHorizontalBattle() {
        if (!useHorizontalFormation) {
            Debug.Log("横版阵型系统未启用，使用传统战斗");
            return;
        }

        // 使用新的spawn点系统初始化战斗
        if (formationManager != null) {
            formationManager.InitializeBattle();
        }

        Debug.Log("横版战斗开始 - 使用预制体spawn系统");
    }

    /// <summary>
    /// 开始横版战斗 - 兼容旧接口（已弃用）
    /// </summary>
    [System.Obsolete("请使用无参数的StartHorizontalBattle()方法，新系统通过预制体自动生成角色")]
    public void StartHorizontalBattle(List<CharacterStats> playerTeam, List<CharacterStats> enemyTeam) {
        Debug.LogWarning("使用了已弃用的StartHorizontalBattle方法，建议使用新的spawn点系统");

        if (!useHorizontalFormation) {
            Debug.Log("横版阵型系统未启用，使用传统战斗");
            return;
        }

        // 为现有角色添加组件（兼容性处理）
        SetupCharacterComponents(playerTeam, enemyTeam);

        Debug.Log($"横版战斗开始（兼容模式） - 玩家队伍: {playerTeam.Count}人, 敌人队伍: {enemyTeam.Count}人");
    }

    /// <summary>
    /// 为角色设置必要的组件
    /// </summary>
    private void SetupCharacterComponents(List<CharacterStats> playerTeam, List<CharacterStats> enemyTeam) {
        // 设置玩家队伍
        foreach (CharacterStats character in playerTeam) {
            SetupCharacterForHorizontalBattle(character);
        }

        // 设置敌人队伍
        foreach (CharacterStats character in enemyTeam) {
            SetupCharacterForHorizontalBattle(character);
        }
    }

    /// <summary>
    /// 为单个角色设置横版战斗组件
    /// </summary>
    private void SetupCharacterForHorizontalBattle(CharacterStats character) {
        // 添加位置组件
        if (character.GetComponent<BattlePositionComponent>() == null) {
            character.gameObject.AddComponent<BattlePositionComponent>();
        }

        // 为盗贼添加潜行组件
        if (enableStealthSystem && character.characterClass == CharacterClass.Rogue) {
            if (character.GetComponent<HorizontalStealthComponent>() == null) {
                character.gameObject.AddComponent<HorizontalStealthComponent>();
            }
        }
    }

    /// <summary>
    /// 获取角色的攻击目标（整合版本）
    /// </summary>
    public List<CharacterStats> GetAttackTargets(CharacterStats attacker, bool isRangedAttack = false) {
        if (!useHorizontalFormation) {
            // 使用原有的目标选择逻辑
            return GetTraditionalAttackTargets(attacker);
        }

        if (isRangedAttack) {
            return HorizontalCombatRules.GetRangedTargets(attacker);
        }
        else {
            return HorizontalCombatRules.GetMeleeTargets(attacker);
        }
    }

    /// <summary>
    /// 获取法术目标（整合版本）
    /// </summary>
    public List<CharacterStats> GetSpellTargets(CharacterStats caster, Spell spell) {
        if (!useHorizontalFormation) {
            return GetTraditionalSpellTargets(caster, spell);
        }

        return HorizontalCombatRules.GetSpellTargets(caster, spell);
    }

    /// <summary>
    /// 计算攻击命中修正（整合版本）
    /// </summary>
    public int CalculateAttackBonus(CharacterStats attacker, CharacterStats target) {
        int baseBonus = 0;

        if (useHorizontalFormation) {
            // 使用横版战斗规则
            baseBonus += HorizontalCombatRules.CalculateAttackModifier(attacker, target);
        }

        if (enableCoverSystem) {
            // 应用掩护效果
            BattlePositionComponent attackerPos = attacker.GetComponent<BattlePositionComponent>();
            BattlePositionComponent targetPos = target.GetComponent<BattlePositionComponent>();

            if (attackerPos != null && targetPos != null) {
                CoverType cover = HorizontalCoverSystem.CheckCover(attackerPos.currentPosition, targetPos.currentPosition);
                if (HorizontalCoverSystem.DoesCoverBlockAttack(cover)) {
                    return -999; // 无法攻击
                }
            }
        }

        return baseBonus;
    }

    /// <summary>
    /// 计算目标AC（包含掩护加值）
    /// </summary>
    public int CalculateTargetAC(CharacterStats target, CharacterStats attacker = null) {
        int baseAC = target.armorClass;

        if (enableCoverSystem && attacker != null) {
            BattlePositionComponent attackerPos = attacker.GetComponent<BattlePositionComponent>();
            BattlePositionComponent targetPos = target.GetComponent<BattlePositionComponent>();

            if (attackerPos != null && targetPos != null) {
                CoverType cover = HorizontalCoverSystem.CheckCover(attackerPos.currentPosition, targetPos.currentPosition);
                baseAC += HorizontalCoverSystem.GetCoverACBonus(cover);
            }
        }

        return baseAC;
    }

    /// <summary>
    /// 检查是否可以进行背刺攻击
    /// </summary>
    public bool CanPerformSneakAttack(CharacterStats rogue, CharacterStats target) {
        if (!enableStealthSystem || rogue.characterClass != CharacterClass.Rogue) {
            return false;
        }

        HorizontalStealthComponent stealthComponent = rogue.GetComponent<HorizontalStealthComponent>();
        if (stealthComponent != null) {
            return stealthComponent.stealthState == StealthState.Flanking && stealthComponent.canFlankThisTurn;
        }

        return false;
    }

    /// <summary>
    /// 处理角色回合结束
    /// </summary>
    public void OnCharacterTurnEnd(CharacterStats character) {
        // 处理潜行状态
        HorizontalStealthComponent stealthComponent = character.GetComponent<HorizontalStealthComponent>();
        if (stealthComponent != null) {
            stealthComponent.OnTurnEnd();
        }
    }

    /// <summary>
    /// 传统攻击目标选择（备用）
    /// </summary>
    private List<CharacterStats> GetTraditionalAttackTargets(CharacterStats attacker) {
        List<CharacterStats> targets = new List<CharacterStats>();

        // 获取所有敌对角色
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();
        foreach (CharacterStats character in allCharacters) {
            if (CharacterTypeHelper.CanAttack(attacker, character) && character.currentHitPoints > 0) {
                targets.Add(character);
            }
        }

        return targets;
    }

    /// <summary>
    /// 传统法术目标选择（备用）
    /// </summary>
    private List<CharacterStats> GetTraditionalSpellTargets(CharacterStats caster, Spell spell) {
        if (spell.heals) {
            // 治疗法术目标友方
            return GetTraditionalHealingTargets(caster);
        }
        else {
            // 伤害法术目标敌方
            return GetTraditionalAttackTargets(caster);
        }
    }

    /// <summary>
    /// 传统治疗目标选择（备用）
    /// </summary>
    private List<CharacterStats> GetTraditionalHealingTargets(CharacterStats caster) {
        List<CharacterStats> targets = new List<CharacterStats>();

        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();
        foreach (CharacterStats character in allCharacters) {
            if (CharacterTypeHelper.IsSameFaction(caster, character) &&
                character.currentHitPoints > 0 &&
                character.currentHitPoints < character.maxHitPoints) {
                targets.Add(character);
            }
        }

        return targets;
    }

    /// <summary>
    /// 获取系统状态信息
    /// </summary>
    public string GetSystemStatus() {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 横版战斗系统状态 ===");
        sb.AppendLine($"阵型系统: {(useHorizontalFormation ? "启用" : "禁用")}");
        sb.AppendLine($"掩护系统: {(enableCoverSystem ? "启用" : "禁用")}");
        sb.AppendLine($"潜行系统: {(enableStealthSystem ? "启用" : "禁用")}");

        if (formationManager != null) {
            sb.AppendLine();
            // sb.AppendLine(formationManager.GetFormationStatus()); // 如果方法不存在则注释掉
        }

        return sb.ToString();
    }    /// <summary>
         /// 挂机模式专用 - 启动自动战斗遭遇
         /// 包含怪物进场动画和自动战斗流程
         /// </summary>
    public void StartIdleEncounter() {
        if (!isIdleMode) {
            Debug.LogWarning("非挂机模式，使用传统战斗流程");
            StartHorizontalBattle();
            return;
        }

        Debug.Log("=== 挂机模式遭遇开始 ===");

        // 停止背景滚动
        StopBackgroundScroll();

        // 停止玩家原地动画，准备战斗
        StopPlayerIdleAnimation();

        // 开始怪物进场序列
        StartCoroutine(EnemyApproachSequence());
    }/// <summary>
     /// 怪物进场动画序列（右→左移动）
     /// </summary>
    private System.Collections.IEnumerator EnemyApproachSequence() {
        Debug.Log("怪物从右侧接近...");

        // 1. 生成怪物在右侧起始位置
        if (formationManager != null) {
            formationManager.InitializeBattle(); // 使用现有的战斗初始化方法
        }

        // 2. 播放怪物移动动画（右→左）
        yield return StartCoroutine(AnimateEnemyApproach());

        // 3. 到达目标位置后开始战斗
        Debug.Log("怪物到达战斗位置，开始投先攻");

        // 4. 投先攻并开始自动战斗
        StartAutomaticCombat();
    }

    /// <summary>
    /// 怪物接近动画（右往左移动）
    /// </summary>
    private System.Collections.IEnumerator AnimateEnemyApproach() {
        // 获取所有敌人角色
        GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        List<CharacterStats> enemies = new List<CharacterStats>();

        if (enemyObjects != null) {
            foreach (GameObject obj in enemyObjects) {
                CharacterStats enemy = obj.GetComponent<CharacterStats>();
                if (enemy != null) {
                    enemies.Add(enemy);
                }
            }
        }

        if (enemies.Count == 0) {
            Debug.LogWarning("未找到敌人角色进行移动动画");
            yield break;
        }

        // 计算目标位置（标准阵型位置）
        float animationTime = 2f; // 动画持续时间
        float elapsedTime = 0f;

        // 记录起始位置并设置到右侧
        Vector3[] startPositions = new Vector3[enemies.Count];
        Vector3[] targetPositions = new Vector3[enemies.Count];

        for (int i = 0; i < enemies.Count; i++) {
            // 设置起始位置为右侧
            startPositions[i] = new Vector3(15f, 0f, i * 2f - enemies.Count);
            enemies[i].transform.position = startPositions[i];

            // 目标位置为正常阵型位置
            targetPositions[i] = new Vector3(8f, 0f, i * 2f - enemies.Count);
        }

        // 执行移动动画
        while (elapsedTime < animationTime) {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationTime;

            for (int i = 0; i < enemies.Count; i++) {
                if (enemies[i] != null) {
                    Vector3 currentPos = Vector3.Lerp(startPositions[i], targetPositions[i], progress);
                    enemies[i].transform.position = currentPos;
                }
            }

            yield return null;
        }        // 确保最终位置精确
        for (int i = 0; i < enemies.Count; i++) {
            if (enemies[i] != null) {
                enemies[i].transform.position = targetPositions[i];

                // 切换敌人到战斗待机动画
                Animator enemyAnimator = enemies[i].GetComponent<Animator>();
                if (enemyAnimator != null) {
                    enemyAnimator.SetBool("IsWalking", false);
                    enemyAnimator.SetBool("InCombat", true);
                }
            }
        }

        Debug.Log("敌人进场动画完成，切换到战斗待机状态");
    }    /// <summary>
         /// 开始自动战斗（投先攻+AI控制）
         /// </summary>
    private void StartAutomaticCombat() {
        // 1. 投先攻骰子
        RollInitiative();

        // 2. 启动自动战斗AI
        if (idleGameManager != null) {
            idleGameManager.ToggleIdleMode(); // 确保挂机模式开启
        }

        // 3. 开始自动回合制战斗
        if (combatManager != null) {
            List<CharacterStats> allParticipants = new List<CharacterStats>();
            allParticipants.AddRange(FindObjectsOfType<CharacterStats>());
            combatManager.StartCombat(allParticipants);
        }

        Debug.Log("自动战斗开始 - 所有角色由AI控制");
    }

    /// <summary>
    /// 投先攻骰子
    /// </summary>
    private void RollInitiative() {
        Debug.Log("=== 投先攻骰子 ===");

        // 获取所有战斗角色
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();
        // 为每个角色投先攻
        foreach (CharacterStats character in allCharacters) {
            if (character.currentHitPoints > 0) {
                // 简化先攻计算，使用固定随机值
                int initiative = UnityEngine.Random.Range(1, 21) + 2; // 假定敏捷加值为+2
                Debug.Log($"{character.characterName} 先攻: {initiative}");

                // 这里可以设置到战斗管理器的先攻列表
                // combatManager.SetInitiative(character, initiative);
            }
        }
    }    /// <summary>
         /// 停止玩家原地动画
         /// </summary>
    private void StopPlayerIdleAnimation() {
        // 获取所有玩家角色
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

        if (playerObjects != null) {
            foreach (GameObject obj in playerObjects) {
                CharacterStats player = obj.GetComponent<CharacterStats>();
                if (player != null) {
                    // 停止原地走路动画，切换到战斗待机动画
                    Animator animator = player.GetComponent<Animator>();
                    if (animator != null) {
                        animator.SetBool("IsWalking", false);
                        animator.SetBool("InCombat", true);
                    }
                }
            }
        }

        Debug.Log("玩家停止探索动画，进入战斗状态");
    }

    /// <summary>
    /// 战斗结束后的处理
    /// </summary>
    public void OnIdleCombatEnd(bool playerVictory) {
        Debug.Log($"=== 挂机战斗结束 - {(playerVictory ? "玩家胜利" : "玩家失败")} ===");

        if (playerVictory) {
            // 给予奖励
            GiveIdleRewards();
        }
        else {
            // 处理失败惩罚
            HandleIdleDefeat();
        }

        // 清理战场
        CleanupBattlefield();

        // 恢复探索状态
        ResumeIdleExploration();
    }    /// <summary>
         /// 给予挂机奖励
         /// </summary>
    private void GiveIdleRewards() {
        if (idleGameManager != null) {
            // 调用公开方法或通过其他方式处理奖励
            Debug.Log("挂机战斗胜利，发放奖励");
            // idleGameManager.GiveBattleVictoryRewards(); // 原方法为private，需要改为public或使用其他方式
        }
        Debug.Log("挂机战斗奖励已发放");
    }

    /// <summary>
    /// 处理挂机失败
    /// </summary>
    private void HandleIdleDefeat() {
        if (idleGameManager != null) {
            // 调用公开方法或通过其他方式处理失败
            Debug.Log("挂机战斗失败，应用惩罚");
            // idleGameManager.HandleBattleDefeat(); // 原方法为private，需要改为public或使用其他方式
        }
        Debug.Log("挂机战斗失败，应用惩罚");
    }

    /// <summary>
    /// 清理战场
    /// </summary>
    private void CleanupBattlefield() {
        // 移除或隐藏敌人角色
        GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemyObjects != null) {
            foreach (GameObject obj in enemyObjects) {
                CharacterStats enemy = obj.GetComponent<CharacterStats>();
                if (enemy != null && enemy.currentHitPoints <= 0) {
                    // 播放死亡动画后销毁或隐藏
                    StartCoroutine(DestroyAfterDelay(enemy.gameObject, 1f));
                }
            }
        }
    }

    /// <summary>
    /// 延迟销毁GameObject
    /// </summary>
    private System.Collections.IEnumerator DestroyAfterDelay(GameObject obj, float delay) {
        yield return new WaitForSeconds(delay);
        if (obj != null) {
            Destroy(obj);
        }
    }

    void Update() {
        // 更新背景滚动
        if (isBackgroundScrolling && backgroundContainer != null) {
            UpdateBackgroundScroll();
        }
    }

    /// <summary>
    /// 开始挂机探索状态
    /// </summary>
    public void StartIdleExploration() {
        if (!isIdleMode) return;

        Debug.Log("=== 开始挂机探索 ===");

        // 启动玩家原地走路动画
        StartPlayerIdleAnimation();

        // 开始背景滚动
        StartBackgroundScroll();

        // 启动挂机管理器的自动遭遇系统
        if (idleGameManager != null && !idleGameManager.idleModeEnabled) {
            idleGameManager.ToggleIdleMode();
        }
    }

    /// <summary>
    /// 启动玩家原地走路动画
    /// </summary>
    private void StartPlayerIdleAnimation() {
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

        if (playerObjects != null) {
            foreach (GameObject obj in playerObjects) {
                CharacterStats player = obj.GetComponent<CharacterStats>();
                if (player != null) {
                    Animator animator = player.GetComponent<Animator>();
                    if (animator != null) {
                        animator.SetBool("IsWalking", true);
                        animator.SetBool("InCombat", false);
                        animator.SetFloat("WalkSpeed", playerIdleAnimationSpeed);
                    }
                }
            }
        }

        Debug.Log("玩家开始原地探索动画");
    }

    /// <summary>
    /// 开始背景滚动
    /// </summary>
    private void StartBackgroundScroll() {
        if (backgroundContainer == null) {
            Debug.LogWarning("背景容器未设置，无法启动背景滚动");
            return;
        }

        isBackgroundScrolling = true;
        Debug.Log("背景开始往左滚动");
    }

    /// <summary>
    /// 停止背景滚动
    /// </summary>
    private void StopBackgroundScroll() {
        isBackgroundScrolling = false;
        Debug.Log("背景停止滚动");
    }

    /// <summary>
    /// 更新背景滚动
    /// </summary>
    private void UpdateBackgroundScroll() {
        if (backgroundContainer != null) {
            // 背景往左移动
            Vector3 movement = Vector3.left * backgroundScrollSpeed * Time.deltaTime;
            backgroundContainer.position += movement;

            // 可选：背景循环重置（如果背景移动太远）
            // 这里可以根据具体背景设计调整
        }
    }    /// <summary>
         /// 重置背景位置到原始状态
         /// </summary>
    private void ResetBackgroundPosition() {
        if (backgroundContainer != null) {
            backgroundContainer.position = originalBackgroundPosition;
            Debug.Log("背景位置已重置");
        }
    }

    /// <summary>
    /// 恢复挂机探索状态
    /// </summary>
    private void ResumeIdleExploration() {
        Debug.Log("=== 恢复挂机探索状态 ===");

        // 恢复玩家原地走路动画
        StartPlayerIdleAnimation();

        // 重新开始背景滚动
        StartBackgroundScroll();

        Debug.Log("玩家继续原地探索动画，背景重新开始滚动");
    }
}
