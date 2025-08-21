using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.UI; // 如果项目没有UI模块，则注释掉
using DND5E;

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

    [Header("队伍配置")]
    [Tooltip("玩家队伍配置：直接拖入场景中的玩家角色")]
    public List<CharacterStats> playerParty = new List<CharacterStats>();

    [Header("预制体配置")]
    [Tooltip("玩家角色预制体列表")]
    public List<GameObject> playerPrefabs = new List<GameObject>();
    [Tooltip("敌人角色预制体列表")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();

    [Header("队伍生成设置")]
    [Tooltip("玩家队伍人数")]
    public int playerPartySize = 3;
    [Tooltip("是否使用预制体生成玩家队伍（否则使用手动配置）")]
    public bool usePlayerPrefabs = false;

    [Header("系统组件")]
    public HorizontalBattleFormationManager formationManager;
    public AutoBattleAI autoBattleAI;

    // 私有变量
    private bool isInBattle = false;
    private float nextEncounterTime;
    private Coroutine idleCoroutine;
    private IdleRewards accumulatedRewards;

    // 阶段配置
    private Dictionary<int, StageData> stageConfigs;

    void Start() {
        InitializeIdleSystem();
        SetupUI();
        LoadStageConfigs();
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
        if (formationManager == null)
            formationManager = FindObjectOfType<HorizontalBattleFormationManager>();

        if (autoBattleAI == null)
            autoBattleAI = FindObjectOfType<AutoBattleAI>();

        accumulatedRewards = new IdleRewards();
        nextEncounterTime = Time.time + encounterInterval;

        // 如果启用预制体生成且玩家队伍为空，则生成玩家队伍
        if (usePlayerPrefabs && playerParty.Count == 0) {
            GeneratePlayerParty();
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
    /// 探索阶段
    /// </summary>
    private IEnumerator ExploreStage() {
        if (!stageConfigs.ContainsKey(currentStage)) {
            Debug.LogWarning($"未找到阶段 {currentStage} 的配置");
            yield break;
        }

        StageData stageData = stageConfigs[currentStage];

        // 更新探索进度
        stageProgressPercent += Time.deltaTime * 10f; // 每秒增加10%进度

        if (stageProgressPercent >= 100f) {
            CompleteCurrentWave();
        }

        yield return null;
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
        List<CharacterStats> enemyParty = GenerateEnemyParty();        // 使用现有角色列表初始化战斗
        if (formationManager != null) {
            formationManager.InitializeBattleWithExistingCharacters(validPlayerParty, enemyParty);
        }

        // 开始自动战斗
        yield return StartCoroutine(AutoBattleSequence(validPlayerParty, enemyParty));

        isInBattle = false;
    }

    /// <summary>
    /// 自动战斗序列
    /// </summary>
    private IEnumerator AutoBattleSequence(List<CharacterStats> playerParty, List<CharacterStats> enemyParty) {
        int round = 1;

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
    }

    /// <summary>
    /// 生成敌人队伍
    /// </summary>
    private List<CharacterStats> GenerateEnemyParty() {
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

        return enemies;
    }

    /// <summary>
    /// 创建随机敌人
    /// </summary>
    private CharacterStats CreateRandomEnemy(int level) {
        GameObject enemyObj;

        // 如果有敌人预制体，使用预制体创建
        if (enemyPrefabs.Count > 0) {
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            enemyObj = Instantiate(prefab);
            enemyObj.name = $"敌人_等级{level}";
        }
        else {
            // 否则创建空GameObject
            enemyObj = new GameObject($"敌人_等级{level}");
        }

        // 获取或添加CharacterStats组件
        CharacterStats enemyStats = enemyObj.GetComponent<CharacterStats>();
        if (enemyStats == null) {
            enemyStats = enemyObj.AddComponent<CharacterStats>();
        }

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
    /// 从场景中查找并配置玩家队伍（辅助方法）
    /// </summary>
    [ContextMenu("从场景中查找玩家队伍")]
    public void FindPlayerPartyFromScene() {
        playerParty.Clear();

        // 查找所有标签为Player和Ally的角色
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] allyObjects = GameObject.FindGameObjectsWithTag("Ally");

        // 添加主角
        foreach (GameObject obj in playerObjects) {
            CharacterStats stats = obj.GetComponent<CharacterStats>();
            if (stats != null && stats.currentHitPoints > 0) {
                playerParty.Add(stats);
                Debug.Log($"添加玩家角色: {stats.characterName}");
            }
        }

        // 添加队友
        foreach (GameObject obj in allyObjects) {
            CharacterStats stats = obj.GetComponent<CharacterStats>();
            if (stats != null && stats.currentHitPoints > 0) {
                playerParty.Add(stats);
                Debug.Log($"添加队友角色: {stats.characterName}");
            }
        }

        Debug.Log($"查找完成，玩家队伍共 {playerParty.Count} 人");
    }

    /// <summary>
    /// 从预制体生成玩家队伍
    /// </summary>
    private void GeneratePlayerParty() {
        if (playerPrefabs.Count == 0) {
            Debug.LogWarning("玩家预制体列表为空，无法生成队伍！");
            return;
        }

        playerParty.Clear();

        for (int i = 0; i < playerPartySize; i++) {
            // 随机选择预制体
            GameObject prefab = playerPrefabs[Random.Range(0, playerPrefabs.Count)];

            // 实例化预制体
            GameObject playerObj = Instantiate(prefab);
            playerObj.name = $"玩家角色_{i + 1}";

            // 获取CharacterStats组件
            CharacterStats stats = playerObj.GetComponent<CharacterStats>();
            if (stats == null) {
                // 如果预制体没有CharacterStats，添加一个
                stats = playerObj.AddComponent<CharacterStats>();
                InitializePlayerStats(stats, i + 1);
            }

            // 确保是玩家阵营
            stats.battleSide = BattleSide.Player;
            stats.characterName = $"勇士{i + 1}";

            // 设置标签
            playerObj.tag = "Player";

            // 添加到队伍
            playerParty.Add(stats);

            Debug.Log($"生成玩家角色: {stats.characterName}");
        }

        Debug.Log($"玩家队伍生成完成，共 {playerParty.Count} 人");
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
    /// 获取有效的玩家队伍（单一配置方式）
    /// </summary>
    private List<CharacterStats> GetValidPlayerParty() {
        List<CharacterStats> validPlayers = new List<CharacterStats>();

        // 过滤掉null和死亡的角色
        foreach (CharacterStats player in playerParty) {
            if (player != null && player.currentHitPoints > 0) {
                validPlayers.Add(player);
            }
        }

        return validPlayers;
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
