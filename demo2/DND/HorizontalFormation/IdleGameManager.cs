using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.UI; // 如果项目没有UI模块，则注释掉
using DND5E;
using Spine.Unity; // 添加Spine命名空间

/// <summary>
/// 挂机模式管理器
/// 实现自动探索和战斗的挂机游戏系统
/// </summary>
public class IdleGameManager : MonoBehaviour {
    [Header("挂机模式设置")]
    public bool idleModeEnabled = false;
    public float encounterInterval = 10f; // 遭遇间隔时间
    public float battleSpeed = 1f; // 战斗速度倍率

    [Header("探索设置")]
    public int currentStage = 1;
    public int currentWave = 1;
    public float stageProgressPercent = 0f; [Header("UI组件 - 如果项目没有UI模块则注释掉")]
    // public Button idleModeToggle;
    // public Text stageInfoText;
    // public Text progressText;
    // public Slider progressSlider;
    // public Text rewardsText;

    [Header("队伍生成设置")]
    [Tooltip("是否使用阵型管理器生成队伍（推荐开启）")]
    public bool useFormationManager = true;
    [Tooltip("玩家队伍人数上限")]
    public int playerPartySize = 3;

    [Header("系统组件")]
    public HorizontalBattleFormationManager formationManager;
    public AutoBattleAI autoBattleAI;

    // 私有变量
    private bool isInBattle = false;
    private float nextEncounterTime;
    private Coroutine idleCoroutine;
    private IdleRewards accumulatedRewards;

    // 当前活跃的队伍（运行时生成）
    private List<CharacterStats> currentPlayerTeam = new List<CharacterStats>();
    private List<CharacterStats> currentEnemyTeam = new List<CharacterStats>();

    // 阶段配置
    private Dictionary<int, StageData> stageConfigs;

    void Start() {
        // 🎯 修复：先初始化配置，再初始化系统
        LoadStageConfigs();
        SetupUI();
        InitializeIdleSystem();
    }

    void Update() {
        if (idleModeEnabled && !isInBattle) {
            UpdateExploreProgress();
        }

        UpdateUI();
    }    /// <summary>
         /// 初始化挂机系统
         /// </summary>
    private void InitializeIdleSystem() {
        // 强制手动引用验证 - 移除自动查找逻辑
        if (formationManager == null) {
            Debug.LogError("IdleGameManager: formationManager 引用未设置！请在Inspector中手动拖入HorizontalBattleFormationManager组件");
            return;
        }

        if (autoBattleAI == null) {
            Debug.LogError("IdleGameManager: autoBattleAI 引用未设置！请在Inspector中手动拖入AutoBattleAI组件");
            return;
        }

        accumulatedRewards = new IdleRewards();
        nextEncounterTime = Time.time + encounterInterval;

        // 使用阵型管理器生成初始队伍
        if (useFormationManager) {
            GenerateInitialTeams();

            // 队伍生成后，启动探索模式
            if (currentPlayerTeam.Count > 0) {
                Debug.Log("🎯 队伍生成完成，启动探索模式...");
                StartExploreMode();
            }
        }
    }

    /// <summary>
    /// 启动探索模式（确保角色播放走路动画）
    /// </summary>
    private void StartExploreMode() {
        // 启动挂机模式
        idleModeEnabled = true;

        // 立即设置玩家队伍为走路动画
        SetPlayerPartyAnimation("walk");
        Debug.Log("🎬 玩家队伍开始探索，播放走路动画");

        // 🎯 关键修复：立即启动背景滚动
        StartBackgroundScrolling();

        // 开始挂机循环
        if (idleCoroutine != null) {
            StopCoroutine(idleCoroutine);
        }
        idleCoroutine = StartCoroutine(IdleGameLoop());
    }

    /// <summary>
    /// 启动背景滚动（立即执行）
    /// </summary>
    private void StartBackgroundScrolling() {
        // 通过ScrollLayer实现背景滚动（UV移动）
        ScrollLayer[] scrollLayers = FindObjectsOfType<ScrollLayer>();
        if (scrollLayers.Length > 0) {
            Debug.Log($"🎬 找到 {scrollLayers.Length} 个ScrollLayer组件，开始启动背景滚动");
            foreach (ScrollLayer layer in scrollLayers) {
                if (layer != null) {
                    layer.SetScrollSpeed(2f); // 设置滚动速度
                    Debug.Log($"✅ 启动ScrollLayer: {layer.gameObject.name} - 滚动状态: {layer.IsScrolling()}");
                }
            }

            // 等待一帧后再次检查
            StartCoroutine(VerifyScrollingAfterDelay());
        }
        else {
            Debug.LogError("❌ 未找到任何ScrollLayer组件！请检查以下设置:");
            Debug.LogError("  1. Environment容器下是否有背景GameObject");
            Debug.LogError("  2. 背景GameObject是否添加了ScrollLayer脚本");
            Debug.LogError("  3. 背景GameObject是否有SpriteRenderer组件");
        }
    }

    /// <summary>
    /// 延迟验证滚动状态
    /// </summary>
    private System.Collections.IEnumerator VerifyScrollingAfterDelay() {
        yield return new WaitForSeconds(0.1f);

        ScrollLayer[] scrollLayers = FindObjectsOfType<ScrollLayer>();
        Debug.Log("🔍 验证背景滚动状态:");

        foreach (ScrollLayer layer in scrollLayers) {
            if (layer != null) {
                Debug.Log($"  - {layer.gameObject.name}: 滚动中={layer.IsScrolling()}");
            }
        }
    }

    /// <summary>
    /// 停止背景滚动（战斗时）
    /// </summary>
    private void StopBackgroundScrolling() {
        ScrollLayer[] scrollLayers = FindObjectsOfType<ScrollLayer>();
        if (scrollLayers.Length > 0) {
            Debug.Log("🛑 战斗开始，停止背景滚动");
            foreach (ScrollLayer layer in scrollLayers) {
                if (layer != null) {
                    layer.StopScrolling();
                }
            }
        }
    }

    /// <summary>
    /// 恢复背景滚动（探索时）
    /// </summary>
    private void ResumeBackgroundScrolling() {
        ScrollLayer[] scrollLayers = FindObjectsOfType<ScrollLayer>();
        if (scrollLayers.Length > 0) {
            Debug.Log("🎬 战斗结束，恢复背景滚动");
            foreach (ScrollLayer layer in scrollLayers) {
                if (layer != null) {
                    layer.SetScrollSpeed(2f);
                }
            }
        }
    }    /// <summary>
         /// 设置UI
         /// </summary>
    private void SetupUI() {
        // UI初始化代码，如果项目没有UI模块则注释掉
        /*
        if (idleModeToggle != null) {
            idleModeToggle.onClick.AddListener(ToggleIdleMode);
        }
        */
    }

    /// <summary>
    /// 加载阶段配置
    /// </summary>
    private void LoadStageConfigs() {
        stageConfigs = new Dictionary<int, StageData>
        {
            { 1, new StageData { stageName = "森林入口", enemyLevel = 1, wavesPerStage = 5,
                               baseExpReward = 100, baseGoldReward = 50 } },
            { 2, new StageData { stageName = "深森小径", enemyLevel = 2, wavesPerStage = 6,
                               baseExpReward = 150, baseGoldReward = 75 } },
            { 3, new StageData { stageName = "古树林地", enemyLevel = 3, wavesPerStage = 7,
                               baseExpReward = 200, baseGoldReward = 100 } },
            { 4, new StageData { stageName = "魔法森林", enemyLevel = 4, wavesPerStage = 8,
                               baseExpReward = 300, baseGoldReward = 150 } },
            { 5, new StageData { stageName = "森林之心", enemyLevel = 5, wavesPerStage = 10,
                               baseExpReward = 500, baseGoldReward = 250 } }
        };
    }

    /// <summary>
    /// 切换挂机模式
    /// </summary>
    public void ToggleIdleMode() {
        idleModeEnabled = !idleModeEnabled;

        if (idleModeEnabled) {
            StartIdleMode();
        }
        else {
            StopIdleMode();
        }

        Debug.Log($"挂机模式: {(idleModeEnabled ? "开启" : "关闭")}");
    }

    /// <summary>
    /// 开始挂机模式
    /// </summary>
    private void StartIdleMode() {
        if (autoBattleAI != null) {
            autoBattleAI.enableAutoBattle = true;
        }

        if (idleCoroutine != null) {
            StopCoroutine(idleCoroutine);
        }

        idleCoroutine = StartCoroutine(IdleGameLoop());

        Debug.Log("开始自动探索...");
    }

    /// <summary>
    /// 停止挂机模式
    /// </summary>
    private void StopIdleMode() {
        if (autoBattleAI != null) {
            autoBattleAI.enableAutoBattle = false;
        }

        if (idleCoroutine != null) {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        Debug.Log("停止自动探索");
    }

    /// <summary>
    /// 挂机游戏主循环
    /// </summary>
    private IEnumerator IdleGameLoop() {
        while (idleModeEnabled) {
            // 探索阶段
            yield return StartCoroutine(ExploreStage());

            // 检查是否需要进入战斗
            if (Time.time >= nextEncounterTime) {
                yield return StartCoroutine(StartRandomEncounter());
                nextEncounterTime = Time.time + encounterInterval;
            }

            yield return new WaitForSeconds(1f / battleSpeed);
        }
    }

    /// <summary>
    /// 探索阶段（确保持续播放走路动画和背景滚动）
    /// </summary>
    private IEnumerator ExploreStage() {
        // 🛡️ 增强null检查
        if (stageConfigs == null) {
            Debug.LogError("ExploreStage: stageConfigs未初始化！");
            yield break;
        }

        // 检查基础配置
        if (!stageConfigs.ContainsKey(currentStage)) {
            Debug.LogWarning($"未找到阶段 {currentStage} 的配置");
            yield break;
        }

        // 检查玩家队伍是否存在
        if (currentPlayerTeam == null || currentPlayerTeam.Count == 0) {
            Debug.LogWarning("ExploreStage: 玩家队伍为空，跳过动画设置");
            yield break;
        }

        // 🎯 只有在非战斗状态时才设置走路动画
        if (!isInBattle) {
            // 确保玩家队伍持续播放走路动画
            SetPlayerPartyAnimation("walk");
        }

        // 🎬 确保背景持续滚动（每次都检查）
        EnsureBackgroundScrolling();

        StageData stageData = stageConfigs[currentStage];

        // 更新探索进度
        stageProgressPercent += Time.deltaTime * 10f; // 每秒增加10%进度

        if (stageProgressPercent >= 100f) {
            CompleteCurrentWave();
        }

        yield return null;
    }

    /// <summary>
    /// 确保背景滚动正常运行
    /// </summary>
    private void EnsureBackgroundScrolling() {
        ScrollLayer[] scrollLayers = FindObjectsOfType<ScrollLayer>();
        if (scrollLayers.Length > 0) {
            foreach (ScrollLayer layer in scrollLayers) {
                if (layer != null && !layer.IsScrolling()) {
                    layer.SetScrollSpeed(2f);
                    Debug.Log($"🔄 重新启动背景滚动: {layer.gameObject.name}");
                }
            }
        }
        else if (Time.frameCount % 300 == 0) { // 每5秒提醒一次
            Debug.LogWarning("⚠️ 未找到ScrollLayer组件！请检查背景配置");
        }
    }

    /// <summary>
    /// 完成当前波次
    /// </summary>
    private void CompleteCurrentWave() {
        stageProgressPercent = 0f;
        currentWave++;

        if (!stageConfigs.ContainsKey(currentStage))
            return;

        StageData stageData = stageConfigs[currentStage];

        // 给予波次奖励
        GiveWaveRewards(stageData);

        if (currentWave > stageData.wavesPerStage) {
            CompleteCurrentStage();
        }

        Debug.Log($"完成波次 {currentWave - 1}，当前阶段: {currentStage}-{currentWave}");
    }

    /// <summary>
    /// 完成当前阶段
    /// </summary>
    private void CompleteCurrentStage() {
        if (!stageConfigs.ContainsKey(currentStage))
            return;

        StageData stageData = stageConfigs[currentStage];

        // 给予阶段完成奖励
        GiveStageCompletionRewards(stageData);

        currentStage++;
        currentWave = 1;

        Debug.Log($"完成阶段 {currentStage - 1}！进入新阶段: {currentStage}");
    }    /// <summary>
         /// 开始随机遭遇
         /// </summary>
    private IEnumerator StartRandomEncounter() {
        if (isInBattle) yield break;

        Debug.Log("遭遇敌人！开始自动战斗...");

        isInBattle = true;

        // 获取玩家队伍（单一配置方式）
        List<CharacterStats> validPlayerParty = GetValidPlayerParty();
        if (validPlayerParty.Count == 0) {
            Debug.LogError("无法开始战斗：未找到有效的玩家角色！请在Inspector中配置playerParty字段。");
            isInBattle = false;
            yield break;
        }

        // 生成敌人队伍
        List<CharacterStats> enemyParty = GenerateEnemyParty();

        // 播放敌人进场动画
        Debug.Log("🎬 开始敌人进场动画...");
        yield return StartCoroutine(EnemyEntranceAnimation(enemyParty));

        // 🎯 敌人进场完成后，立即让玩家队伍切换到待机动画
        SetPlayerPartyAnimation("idle");
        Debug.Log("🎬 玩家队伍切换到待机动画，准备战斗");

        // ⚠️ 不再重新排列位置，敌人已经在进场动画中定位好了

        // 开始自动战斗
        yield return StartCoroutine(AutoBattleSequence(validPlayerParty, enemyParty));

        isInBattle = false;
    }

    /// <summary>
    /// 自动战斗序列
    /// </summary>
    private IEnumerator AutoBattleSequence(List<CharacterStats> playerParty, List<CharacterStats> enemyParty) {
        int round = 1;

        // 战斗开始时设置所有角色为空闲动画
        SetPlayerPartyAnimation("idle");
        SetEnemyPartyAnimation(enemyParty, "idle");
        Debug.Log("🎬 战斗开始，所有角色切换到空闲动画");

        // 🎯 战斗开始时停止背景滚动
        StopBackgroundScrolling();

        while (HasLivingMembers(playerParty) && HasLivingMembers(enemyParty)) {
            Debug.Log($"=== 自动战斗回合 {round} ===");            // 玩家回合
            foreach (CharacterStats player in playerParty) {
                if (player.currentHitPoints > 0 && autoBattleAI != null) {
                    autoBattleAI.ExecuteAutoBattleTurn(player);
                    yield return new WaitForSeconds(0.5f / battleSpeed);
                }
            }

            // 敌人回合
            foreach (CharacterStats enemy in enemyParty) {
                if (enemy.currentHitPoints > 0 && autoBattleAI != null) {
                    autoBattleAI.ExecuteAutoBattleTurn(enemy);
                    yield return new WaitForSeconds(0.5f / battleSpeed);
                }
            }

            round++;
            yield return new WaitForSeconds(1f / battleSpeed);
        }

        // 战斗结果
        if (HasLivingMembers(playerParty)) {
            Debug.Log("玩家胜利！");
            GiveBattleVictoryRewards();
        }
        else {
            Debug.Log("玩家失败...");
            HandleBattleDefeat();
        }

        // 🎬 战斗结束后恢复玩家走路动画和背景滚动
        SetPlayerPartyAnimation("walk");
        Debug.Log("🎬 战斗结束，玩家队伍恢复走路动画");

        ResumeBackgroundScrolling();
    }

    /// <summary>
    /// 生成敌人队伍（使用阵型管理器）
    /// </summary>
    private List<CharacterStats> GenerateEnemyParty() {
        if (formationManager != null) {
            // 使用阵型管理器生成敌人队伍
            currentEnemyTeam = formationManager.GenerateEnemyFormation();

            // 根据当前关卡调整敌人属性
            if (stageConfigs.ContainsKey(currentStage)) {
                StageData stageData = stageConfigs[currentStage];
                AdjustEnemyLevels(currentEnemyTeam, stageData.enemyLevel);
            }

            return currentEnemyTeam;
        }
        else {
            Debug.LogError("FormationManager未设置，使用旧方法生成敌人");
            return GenerateEnemyParty_Legacy();
        }
    }

    /// <summary>
    /// 调整敌人队伍等级和属性
    /// </summary>
    private void AdjustEnemyLevels(List<CharacterStats> enemies, int targetLevel) {
        foreach (CharacterStats enemy in enemies) {
            if (enemy != null) {
                enemy.level = targetLevel;
                enemy.maxHitPoints = 30 + (targetLevel * 10);
                enemy.currentHitPoints = enemy.maxHitPoints;
                enemy.armorClass = 10 + targetLevel;

                // 设置基础属性
                enemy.stats.Strength = 12 + targetLevel;
                enemy.stats.Dexterity = 10 + targetLevel;
                enemy.stats.Constitution = 14 + targetLevel;
                enemy.proficiencyBonus = 2 + (targetLevel / 4);
            }
        }
        Debug.Log($"🔴 敌人队伍等级已调整为 {targetLevel}");
    }

    /// <summary>
    /// 旧版敌人生成方法（备用）
    /// </summary>
    private List<CharacterStats> GenerateEnemyParty_Legacy() {
        List<CharacterStats> enemies = new List<CharacterStats>();

        if (!stageConfigs.ContainsKey(currentStage))
            return enemies;

        StageData stageData = stageConfigs[currentStage];

        // 根据阶段等级生成2-4个敌人
        int enemyCount = Random.Range(2, 5);

        for (int i = 0; i < enemyCount; i++) {
            CharacterStats enemy = CreateRandomEnemy(stageData.enemyLevel);
            if (enemy != null) {
                enemies.Add(enemy);
            }
        }

        // 设置敌人队伍阵型位置（关键修复）
        if (formationManager != null && enemies.Count > 0) {
            // 首先将敌人放置在屏幕右侧外面的位置
            StartCoroutine(EnemyEntranceAnimation(enemies));

            formationManager.ArrangeExistingTeam(enemies, BattleSide.Enemy);
            Debug.Log("✅ 敌人队伍已排列到右侧阵型位置");
        }
        else {
            Debug.LogWarning("⚠️ 找不到HorizontalBattleFormationManager或敌人列表为空，敌人位置未设置");
        }

        return enemies;
    }

    /// <summary>
    /// 创建随机敌人（旧版方法，现在使用阵型管理器）
    /// </summary>
    private CharacterStats CreateRandomEnemy(int level) {
        GameObject enemyObj;

        // 创建空GameObject（不再依赖enemyPrefabs）
        enemyObj = new GameObject($"敌人_等级{level}");

        // 获取或添加CharacterStats组件
        CharacterStats enemyStats = enemyObj.GetComponent<CharacterStats>();
        if (enemyStats == null) {
            enemyStats = enemyObj.AddComponent<CharacterStats>();
        }

        // 确保有Role组件用于动画控制
        Role roleComponent = enemyObj.GetComponent<Role>();
        if (roleComponent == null) {
            roleComponent = enemyObj.AddComponent<Role>();
            Debug.Log($"为敌人 {enemyStats.characterName} 添加Role组件");
        }

        // 🎯 验证prefab是否配置了必需的组件（不硬编码添加）
        DND_CharacterAdapter adapter = enemyObj.GetComponent<DND_CharacterAdapter>();
        if (adapter == null) {
            Debug.LogError($"❌ 敌人预制体缺少DND_CharacterAdapter组件！请在prefab中预先配置此组件");
            // 不创建有问题的敌人
            DestroyImmediate(enemyObj);
            return null;
        }

        SkeletonAnimation skeletonAnim = enemyObj.GetComponent<SkeletonAnimation>();
        if (skeletonAnim == null) {
            Debug.LogError($"❌ 敌人预制体缺少SkeletonAnimation组件！请在prefab中预先配置此组件和Spine数据");
            // 不创建有问题的敌人
            DestroyImmediate(enemyObj);
            return null;
        }

        // 仅设置必要的运行时引用，其他属性通过prefab配置
        adapter.characterStats = enemyStats;

        Debug.Log($"✅ 敌人 {enemyStats.characterName} 的组件配置验证完成");

        // 设置敌人属性
        enemyStats.characterName = $"野生怪物 等级{level}";
        enemyStats.level = level;
        enemyStats.battleSide = BattleSide.Enemy;

        // 根据等级设置血量和属性
        enemyStats.maxHitPoints = 30 + (level * 10);
        enemyStats.currentHitPoints = enemyStats.maxHitPoints;
        enemyStats.armorClass = 10 + level;

        // 设置基础属性（随等级增长）
        enemyStats.stats.Strength = 12 + level;
        enemyStats.stats.Dexterity = 10 + level;
        enemyStats.stats.Constitution = 14 + level;
        enemyStats.proficiencyBonus = 2 + (level / 4);

        // 设置标签
        enemyObj.tag = "Enemy";

        Debug.Log($"创建等级 {level} 的敌人: {enemyStats.characterName}");
        return enemyStats;
    }

    /// <summary>
    /// 检查队伍是否有存活成员
    /// </summary>
    private bool HasLivingMembers(List<CharacterStats> party) {
        return party.Exists(member => member != null && member.currentHitPoints > 0);
    }

    /// <summary>
    /// 给予波次奖励
    /// </summary>
    private void GiveWaveRewards(StageData stageData) {
        int expReward = Mathf.RoundToInt(stageData.baseExpReward * 0.3f);
        int goldReward = Mathf.RoundToInt(stageData.baseGoldReward * 0.3f);

        accumulatedRewards.totalExp += expReward;
        accumulatedRewards.totalGold += goldReward;

        Debug.Log($"波次奖励: {expReward} EXP, {goldReward} Gold");
    }

    /// <summary>
    /// 给予阶段完成奖励
    /// </summary>
    private void GiveStageCompletionRewards(StageData stageData) {
        int expReward = stageData.baseExpReward;
        int goldReward = stageData.baseGoldReward;

        accumulatedRewards.totalExp += expReward;
        accumulatedRewards.totalGold += goldReward;

        Debug.Log($"阶段完成奖励: {expReward} EXP, {goldReward} Gold");
    }

    /// <summary>
    /// 给予战斗胜利奖励
    /// </summary>
    private void GiveBattleVictoryRewards() {
        if (!stageConfigs.ContainsKey(currentStage))
            return;

        StageData stageData = stageConfigs[currentStage];

        int expReward = Mathf.RoundToInt(stageData.baseExpReward * 0.5f);
        int goldReward = Mathf.RoundToInt(stageData.baseGoldReward * 0.5f);

        accumulatedRewards.totalExp += expReward;
        accumulatedRewards.totalGold += goldReward;
        accumulatedRewards.battlesWon++;

        Debug.Log($"战斗胜利奖励: {expReward} EXP, {goldReward} Gold");
    }

    /// <summary>
    /// 处理战斗失败
    /// </summary>
    private void HandleBattleDefeat() {
        Debug.Log("战斗失败，队伍需要休息...");

        // 失败惩罚：暂停挂机一段时间
        StartCoroutine(DefeatPenalty());
    }

    /// <summary>
    /// 失败惩罚协程
    /// </summary>
    private IEnumerator DefeatPenalty() {
        float penaltyTime = 30f; // 30秒惩罚时间

        Debug.Log($"队伍休息中... ({penaltyTime}秒)");

        yield return new WaitForSeconds(penaltyTime);        // 恢复部分血量
        List<CharacterStats> validPlayerParty = GetValidPlayerParty();
        foreach (CharacterStats player in validPlayerParty) {
            if (player != null) {
                player.currentHitPoints = Mathf.Min(player.maxHitPoints, player.currentHitPoints + player.maxHitPoints / 4);
            }
        }

        Debug.Log("队伍休息完毕，继续探索！");
    }

    /// <summary>
    /// 更新探索进度
    /// </summary>
    private void UpdateExploreProgress() {
        // 这个方法在Update中调用，用于更新探索进度
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI() {
        // UI更新代码，如果项目没有UI模块则注释掉
        /*
        if (stageInfoText != null && stageConfigs.ContainsKey(currentStage)) {
            StageData stageData = stageConfigs[currentStage];
            stageInfoText.text = $"阶段 {currentStage}: {stageData.stageName} ({currentWave}/{stageData.wavesPerStage})";
        }

        if (progressText != null) {
            progressText.text = $"进度: {stageProgressPercent:F1}%";
        }

        if (progressSlider != null) {
            progressSlider.value = stageProgressPercent / 100f;
        }

        if (rewardsText != null) {
            rewardsText.text = $"累计: {accumulatedRewards.totalExp} EXP, {accumulatedRewards.totalGold} Gold\n" +
                              $"胜利: {accumulatedRewards.battlesWon}";
        }

        if (idleModeToggle != null) {
            idleModeToggle.GetComponentInChildren<Text>().text = idleModeEnabled ? "停止挂机" : "开始挂机";
        }
        */
    }

    /// <summary>
    /// 🎯 生成初始队伍（使用阵型管理器）
    /// </summary>
    private void GenerateInitialTeams() {
        if (formationManager == null) {
            Debug.LogError("FormationManager未设置，无法生成队伍！");
            return;
        }

        // 生成玩家队伍
        currentPlayerTeam = formationManager.GeneratePlayerFormation();
        Debug.Log($"🔵 玩家队伍生成完成，共 {currentPlayerTeam.Count} 人");

        // 显示当前阵型配置
        Debug.Log(formationManager.GetFormationSummary());
    }

    /// <summary>
    /// 初始化玩家角色属性
    /// </summary>
    private void InitializePlayerStats(CharacterStats stats, int characterIndex) {
        stats.level = 1;
        stats.maxHitPoints = 80 + (characterIndex * 10);
        stats.currentHitPoints = stats.maxHitPoints;
        stats.armorClass = 14 + characterIndex;
        stats.proficiencyBonus = 2;

        // 设置基础属性
        stats.stats.Strength = 14 + characterIndex;
        stats.stats.Dexterity = 12 + characterIndex;
        stats.stats.Constitution = 16 + characterIndex;
        stats.stats.Intelligence = 10 + characterIndex;
        stats.stats.Wisdom = 12 + characterIndex;
        stats.stats.Charisma = 10 + characterIndex;
    }

    /// <summary>
    /// 获取有效的玩家队伍（使用当前活跃队伍）
    /// </summary>
    private List<CharacterStats> GetValidPlayerParty() {
        List<CharacterStats> validPlayers = new List<CharacterStats>();

        // 防止空引用异常
        if (currentPlayerTeam == null) {
            Debug.LogWarning("GetValidPlayerParty: currentPlayerTeam为null");
            return validPlayers;
        }

        // 过滤掉null和死亡的角色
        foreach (CharacterStats player in currentPlayerTeam) {
            if (player != null && player.currentHitPoints > 0) {
                validPlayers.Add(player);
            }
        }

        return validPlayers;
    }

    /// <summary>
    /// 设置玩家队伍动画状态 - 统一使用DND_CharacterAdapter映射器
    /// </summary>
    private void SetPlayerPartyAnimation(string animationType) {
        // 防止空引用异常
        if (currentPlayerTeam == null) {
            Debug.LogWarning("SetPlayerPartyAnimation: currentPlayerTeam为null");
            return;
        }

        foreach (CharacterStats player in currentPlayerTeam) {
            if (player != null && player.gameObject != null) {
                DND_CharacterAdapter adapter = player.GetComponent<DND_CharacterAdapter>();
                if (adapter != null) {
                    switch (animationType.ToLower()) {
                        case "idle":
                            adapter.PlayAnimation(adapter.animationMapping.idleAnimation, true);
                            Debug.Log($"✅ 玩家 {player.characterName} 播放待机动画: {adapter.animationMapping.idleAnimation}");
                            break;
                        case "walk":
                            adapter.PlayWalkAnimation();
                            Debug.Log($"✅ 玩家 {player.characterName} 播放走路动画: {adapter.animationMapping.walkAnimation}");
                            break;
                        case "stop_walk":
                            adapter.StopWalkWithTransition();
                            Debug.Log($"✅ 玩家 {player.characterName} 停止走路并过渡到待机动画");
                            break;
                        default:
                            Debug.LogWarning($"未识别的动画类型: {animationType}");
                            break;
                    }
                }
                else {
                    Debug.LogError($"❌ 玩家 {player.characterName} 缺少DND_CharacterAdapter组件！请为所有角色添加DND_CharacterAdapter组件");
                }
            }
        }
    }

    /// <summary>
    /// 设置敌人队伍动画状态
    /// </summary>
    /// <summary>
    /// 设置敌人队伍动画 - 统一使用DND_CharacterAdapter映射器
    /// </summary>
    private void SetEnemyPartyAnimation(List<CharacterStats> enemies, string animationType) {
        foreach (CharacterStats enemy in enemies) {
            if (enemy != null && enemy.gameObject != null) {
                DND_CharacterAdapter adapter = enemy.GetComponent<DND_CharacterAdapter>();
                if (adapter != null) {
                    switch (animationType.ToLower()) {
                        case "idle":
                            adapter.PlayAnimation(adapter.animationMapping.idleAnimation, true);
                            Debug.Log($"✅ 敌人 {enemy.characterName} 播放待机动画: {adapter.animationMapping.idleAnimation}");
                            break;
                        case "walk":
                            adapter.PlayWalkAnimation();
                            Debug.Log($"✅ 敌人 {enemy.characterName} 播放走路动画: {adapter.animationMapping.walkAnimation}");
                            break;
                        case "stop_walk":
                            adapter.StopWalkWithTransition();
                            Debug.Log($"✅ 敌人 {enemy.characterName} 停止走路并过渡到待机动画");
                            break;
                        default:
                            Debug.LogWarning($"未识别的动画类型: {animationType}");
                            break;
                    }
                }
                else {
                    Debug.LogError($"❌ 敌人 {enemy.characterName} 缺少DND_CharacterAdapter组件！请为所有角色添加DND_CharacterAdapter组件");
                }
            }
        }
    }

    /// <summary>
    /// 敌人进入场景动画（从右侧屏幕外进入）
    /// </summary>
    private IEnumerator EnemyEntranceAnimation(List<CharacterStats> enemies) {
        Debug.Log("🎬 敌人从右侧进入战场...");

        // 🎯 先记录敌人的原始阵型位置
        List<Vector3> originalPositions = new List<Vector3>();
        foreach (CharacterStats enemy in enemies) {
            if (enemy != null && enemy.gameObject != null) {
                originalPositions.Add(enemy.transform.position);
            }
        }

        // 将所有敌人移动到屏幕右侧外面并立即设置走路动画
        for (int i = 0; i < enemies.Count; i++) {
            CharacterStats enemy = enemies[i];
            if (enemy != null && enemy.gameObject != null && i < originalPositions.Count) {
                // 将敌人放在屏幕右侧外面
                Vector3 offScreenPosition = originalPositions[i];
                offScreenPosition.x += 15f; // 移动到屏幕右侧
                enemy.transform.position = offScreenPosition;
            }
        }

        // 等待一帧确保所有敌人位置设置完成
        yield return null;

        // 🎯 为所有敌人设置走路动画（在Start方法执行后）
        for (int i = 0; i < enemies.Count; i++) {
            CharacterStats enemy = enemies[i];
            if (enemy != null && enemy.gameObject != null) {
                DND_CharacterAdapter adapter = enemy.GetComponent<DND_CharacterAdapter>();
                if (adapter != null) {
                    // 强制播放走路动画，覆盖Start方法中的待机动画
                    adapter.PlayWalkAnimation();
                    Debug.Log($"✅ 为敌人 {enemy.characterName} 播放走路动画: {adapter.animationMapping.walkAnimation}");
                }
                else {
                    Debug.LogError($"❌ 敌人 {enemy.characterName} 缺少DND_CharacterAdapter组件，无法播放走路动画！请为所有角色添加DND_CharacterAdapter组件");
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 敌人走向战斗位置的动画
        float duration = 2f; // 进入动画持续时间
        float elapsedTime = 0f;

        List<Vector3> startPositions = new List<Vector3>();

        // 记录起始位置（屏幕外）
        foreach (CharacterStats enemy in enemies) {
            if (enemy != null) {
                startPositions.Add(enemy.transform.position);
            }
        }

        // 平滑移动动画，回到原始阵型位置
        float walkAnimationCheckInterval = 0.5f; // 每0.5秒检查一次走路动画
        float lastWalkCheck = 0f;

        while (elapsedTime < duration) {
            float t = elapsedTime / duration;

            for (int i = 0; i < enemies.Count; i++) {
                if (enemies[i] != null && i < startPositions.Count && i < originalPositions.Count) {
                    enemies[i].transform.position = Vector3.Lerp(startPositions[i], originalPositions[i], t);
                }
            }

            // 定期检查并确保敌人正在播放走路动画
            if (elapsedTime - lastWalkCheck >= walkAnimationCheckInterval) {
                for (int i = 0; i < enemies.Count; i++) {
                    if (enemies[i] != null) {
                        DND_CharacterAdapter adapter = enemies[i].GetComponent<DND_CharacterAdapter>();
                        if (adapter != null && adapter.CurrentAnimation != adapter.animationMapping.walkAnimation) {
                            adapter.PlayWalkAnimation();
                            Debug.Log($"🔄 重新为敌人 {enemies[i].characterName} 播放走路动画");
                        }
                    }
                }
                lastWalkCheck = elapsedTime;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 确保敌人到达最终位置（原始阵型位置）
        for (int i = 0; i < enemies.Count; i++) {
            if (enemies[i] != null && i < originalPositions.Count) {
                enemies[i].transform.position = originalPositions[i];

                // 🎯 统一通过DND_CharacterAdapter切换到待机动画
                DND_CharacterAdapter adapter = enemies[i].GetComponent<DND_CharacterAdapter>();
                if (adapter != null) {
                    adapter.StopWalkWithTransition();
                    Debug.Log($"✅ 敌人 {enemies[i].characterName} 使用过渡动画切换到待机状态");
                }
                else {
                    Debug.LogError($"❌ 敌人 {enemies[i].characterName} 缺少DND_CharacterAdapter组件，无法切换到待机动画！请为所有角色添加DND_CharacterAdapter组件");
                }
            }
        }

        Debug.Log("✅ 敌人进入动画完成，切换到待机状态");
    }
}

/// <summary>
/// 阶段数据结构
/// </summary>
[System.Serializable]
public class StageData {
    public string stageName;
    public int enemyLevel;
    public int wavesPerStage;
    public int baseExpReward;
    public int baseGoldReward;
}

/// <summary>
/// 挂机奖励累计
/// </summary>
[System.Serializable]
public class IdleRewards {
    public int totalExp = 0;
    public int totalGold = 0;
    public int battlesWon = 0;
    public int stagesCompleted = 0;
}
