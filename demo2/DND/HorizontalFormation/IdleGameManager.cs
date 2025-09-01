using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂机模式管理器
/// 实现自动探索和战斗的挂机游戏系统
/// </summary>
public class IdleGameManager : MonoBehaviour
{
    [Header("挂机模式设置")]
    public bool idleModeEnabled;
    public float encounterInterval = 10f; // 遭遇间隔时间
    public float battleSpeed = 1f; // 战斗速度倍率

    [Header("探索设置")]
    public int currentStage = 1;
    public int currentWave = 1;
    public float stageProgressPercent;

    [Header("队伍生成设置")]
    [Tooltip("是否使用阵型管理器生成队伍（推荐开启）")]
    public bool useFormationManager = true;
    [Tooltip("玩家队伍人数上限")]
    public int playerPartySize = 3;

    [Header("系统组件")]
    public HorizontalBattleFormationManager formationManager;
    public AutoBattleAI autoBattleAI;

    // 私有变量
    private bool isInBattle;
    private float nextEncounterTime;
    private Coroutine idleCoroutine;
    private IdleRewards accumulatedRewards;

    // 当前活跃的队伍（运行时生成）
    private List<CharacterStats> currentPlayerTeam = new List<CharacterStats>();

    // 阶段配置
    private Dictionary<int, StageData> stageConfigs;

    void Start()
    {
        LoadStageConfigs();
        SetupUI();
        InitializeIdleSystem();
    }

    void Update()
    {
        if (idleModeEnabled && !isInBattle)
        {
            UpdateExploreProgress();
        }
        UpdateUI();
    }

    /// <summary>
    /// 初始化挂机系统
    /// </summary>
    private void InitializeIdleSystem()
    {
        if (formationManager == null)
        {
            Debug.LogError("IdleGameManager: formationManager 引用未设置！请在Inspector中手动拖入HorizontalBattleFormationManager组件");
            return;
        }

        if (autoBattleAI == null)
        {
            Debug.LogError("IdleGameManager: autoBattleAI 引用未设置！请在Inspector中手动拖入AutoBattleAI组件");
            return;
        }

        accumulatedRewards = new IdleRewards();
        nextEncounterTime = Time.time + encounterInterval;

        if (useFormationManager)
        {
            GenerateInitialTeams();
            if (currentPlayerTeam.Count > 0)
            {
                Debug.Log("🎯 队伍生成完成，启动探索模式...");
                StartExploreMode();
            }
        }
    }

    /// <summary>
    /// 启动探索模式
    /// </summary>
    private void StartExploreMode()
    {
        idleModeEnabled = true;
        InitializeAllCharacterAnimations();
        SetPlayerPartyAnimation("walk");
        StartBackgroundScrolling();

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
        }
        idleCoroutine = StartCoroutine(IdleGameLoop());
    }

    /// <summary>
    /// 启动背景滚动
    /// </summary>
    private void StartBackgroundScrolling()
    {
        ScrollLayer[] scrollLayers = FindObjectsOfType<ScrollLayer>();
        if (scrollLayers.Length > 0)
        {
            foreach (ScrollLayer layer in scrollLayers)
            {
                if (layer != null)
                {
                    layer.SetScrollSpeed(2f);
                }
            }
        }
    }

    /// <summary>
    /// 停止背景滚动
    /// </summary>
    private void StopBackgroundScrolling()
    {
        ScrollLayer[] scrollLayers = FindObjectsOfType<ScrollLayer>();
        foreach (ScrollLayer layer in scrollLayers)
        {
            if (layer != null)
            {
                layer.StopScrolling();
            }
        }
    }

    /// <summary>
    /// 恢复背景滚动
    /// </summary>
    private void ResumeBackgroundScrolling()
    {
        ScrollLayer[] scrollLayers = FindObjectsOfType<ScrollLayer>();
        foreach (ScrollLayer layer in scrollLayers)
        {
            if (layer != null)
            {
                layer.SetScrollSpeed(2f);
            }
        }
    }

    /// <summary>
    /// 设置UI
    /// </summary>
    private void SetupUI()
    {
        // UI初始化代码，如果项目没有UI模块则注释掉
    }

    /// <summary>
    /// 加载阶段配置
    /// </summary>
    private void LoadStageConfigs()
    {
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
    /// 挂机游戏主循环
    /// </summary>
    private IEnumerator IdleGameLoop()
    {
        while (idleModeEnabled)
        {
            yield return StartCoroutine(ExploreStage());

            if (Time.time >= nextEncounterTime)
            {
                yield return StartCoroutine(StartRandomEncounter());
                nextEncounterTime = Time.time + encounterInterval;
            }

            yield return new WaitForSeconds(1f / battleSpeed);
        }
    }

    /// <summary>
    /// 探索阶段
    /// </summary>
    private IEnumerator ExploreStage()
    {
        if (stageConfigs == null || !stageConfigs.ContainsKey(currentStage))
        {
            yield break;
        }

        if (!isInBattle)
        {
            SetPlayerPartyAnimation("walk");
        }

        stageProgressPercent += Time.deltaTime * 10f;

        if (stageProgressPercent >= 100f)
        {
            CompleteCurrentWave();
        }

        yield return null;
    }

    /// <summary>
    /// 完成当前波次
    /// </summary>
    private void CompleteCurrentWave()
    {
        stageProgressPercent = 0f;
        currentWave++;

        if (stageConfigs.ContainsKey(currentStage))
        {
            StageData stageData = stageConfigs[currentStage];
            GiveWaveRewards(stageData);

            if (currentWave > stageData.wavesPerStage)
            {
                CompleteCurrentStage();
            }
        }
    }

    /// <summary>
    /// 完成当前阶段
    /// </summary>
    private void CompleteCurrentStage()
    {
        if (stageConfigs.ContainsKey(currentStage))
        {
            StageData stageData = stageConfigs[currentStage];
            GiveStageCompletionRewards(stageData);
        }

        currentStage++;
        currentWave = 1;
    }

    /// <summary>
    /// 开始随机遭遇
    /// </summary>
    private IEnumerator StartRandomEncounter()
    {
        if (isInBattle) yield break;

        isInBattle = true;

        List<CharacterStats> validPlayerParty = GetValidPlayerParty();
        if (validPlayerParty.Count == 0)
        {
            isInBattle = false;
            yield break;
        }

        // 先切换玩家到待机状态和停止背景
        SetPlayerPartyAnimation("idle");
        StopBackgroundScrolling();

        // 生成敌人队伍并执行进场动画
        List<CharacterStats> enemyParty = GenerateEnemyParty();
        yield return StartCoroutine(PlayEnemyEntranceAnimation(enemyParty));

        // 使用先攻系统开始战斗
        if (autoBattleAI != null)
        {
            Debug.Log("🎯 敌人进场完成，启动先攻系统...");
            autoBattleAI.StartBattleSequence();

            // 等待先攻系统战斗完成
            yield return new WaitUntil(() => !autoBattleAI.isBattleActive);
        }

        // 战斗结束后的清理
        yield return StartCoroutine(HandleBattleEnd(validPlayerParty, enemyParty));

        isInBattle = false;
    }

    /// <summary>
    /// 敌人进场动画序列
    /// </summary>
    private IEnumerator PlayEnemyEntranceAnimation(List<CharacterStats> enemyParty)
    {
        Debug.Log("🚶‍♂️ 播放敌人进场动画...");

        foreach (CharacterStats enemy in enemyParty)
        {
            if (enemy != null)
            {
                // 将敌人初始位置设置在屏幕右侧外
                Vector3 currentPos = enemy.transform.position;
                Vector3 startPos = new Vector3(currentPos.x + 8f, currentPos.y, currentPos.z);
                enemy.transform.position = startPos;

                // 播放走路动画
                DND_CharacterAdapter adapter = enemy.GetComponent<DND_CharacterAdapter>();
                if (adapter != null)
                {
                    adapter.PlayWalkAnimation();
                }
            }

            yield return new WaitForSeconds(0.2f); // 错开敌人进场时间
        }

        // 移动到目标位置
        float moveSpeed = 3f;
        bool allReachedTarget = false;

        while (!allReachedTarget)
        {
            allReachedTarget = true;

            foreach (CharacterStats enemy in enemyParty)
            {
                if (enemy != null)
                {
                    BattlePositionComponent posComp = enemy.GetComponent<BattlePositionComponent>();
                    if (posComp != null)
                    {
                        Vector3 targetPos = GetSpawnPosition(posComp.currentPosition);
                        Vector3 currentPos = enemy.transform.position;

                        if (Vector3.Distance(currentPos, targetPos) > 0.1f)
                        {
                            allReachedTarget = false;
                            Vector3 newPos = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);
                            enemy.transform.position = newPos;
                        }
                    }
                }
            }

            yield return null;
        }

        // 切换到待机动画
        foreach (CharacterStats enemy in enemyParty)
        {
            if (enemy != null)
            {
                DND_CharacterAdapter adapter = enemy.GetComponent<DND_CharacterAdapter>();
                if (adapter != null)
                {
                    adapter.PlayIdleAnimation();
                }
            }
        }

        Debug.Log("✅ 敌人进场动画完成");
    }

    /// <summary>
    /// 处理战斗结束
    /// </summary>
    private IEnumerator HandleBattleEnd(List<CharacterStats> playerParty, List<CharacterStats> enemyParty)
    {
        // 判断战斗结果
        if (HasLivingMembers(playerParty))
        {
            Debug.Log("🎉 玩家胜利！");
            GiveBattleVictoryRewards();

            // 销毁敌人
            foreach (CharacterStats enemy in enemyParty)
            {
                if (enemy != null && enemy.gameObject != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
        }
        else
        {
            Debug.Log("💀 玩家败北！");
            HandleBattleDefeat();
        }

        yield return new WaitForSeconds(1f);

        // 恢复探索状态
        SetPlayerPartyAnimation("walk");
        ResumeBackgroundScrolling();
    }

    /// <summary>
    /// 获取指定位置的spawn点坐标
    /// </summary>
    private Vector3 GetSpawnPosition(HorizontalPosition position)
    {
        if (formationManager == null) return Vector3.zero;

        Transform spawnPoint = null;

        switch (position)
        {
            // 敌人位置 - 使用 enemySpawnPoints 数组
            case HorizontalPosition.EnemyFrontLeft:
                spawnPoint = formationManager.enemySpawnPoints.Length > 0 ? formationManager.enemySpawnPoints[0] : null;
                break;
            case HorizontalPosition.EnemyFrontCenter:
                spawnPoint = formationManager.enemySpawnPoints.Length > 1 ? formationManager.enemySpawnPoints[1] : null;
                break;
            case HorizontalPosition.EnemyFrontRight:
                spawnPoint = formationManager.enemySpawnPoints.Length > 2 ? formationManager.enemySpawnPoints[2] : null;
                break;
            case HorizontalPosition.EnemyBackLeft:
                spawnPoint = formationManager.enemySpawnPoints.Length > 3 ? formationManager.enemySpawnPoints[3] : null;
                break;
            case HorizontalPosition.EnemyBackCenter:
                spawnPoint = formationManager.enemySpawnPoints.Length > 4 ? formationManager.enemySpawnPoints[4] : null;
                break;
            case HorizontalPosition.EnemyBackRight:
                spawnPoint = formationManager.enemySpawnPoints.Length > 5 ? formationManager.enemySpawnPoints[5] : null;
                break;

            // 玩家位置 - 使用 playerSpawnPoints 数组
            case HorizontalPosition.PlayerFrontLeft:
                spawnPoint = formationManager.playerSpawnPoints.Length > 0 ? formationManager.playerSpawnPoints[0] : null;
                break;
            case HorizontalPosition.PlayerFrontCenter:
                spawnPoint = formationManager.playerSpawnPoints.Length > 1 ? formationManager.playerSpawnPoints[1] : null;
                break;
            case HorizontalPosition.PlayerFrontRight:
                spawnPoint = formationManager.playerSpawnPoints.Length > 2 ? formationManager.playerSpawnPoints[2] : null;
                break;
            case HorizontalPosition.PlayerBackLeft:
                spawnPoint = formationManager.playerSpawnPoints.Length > 3 ? formationManager.playerSpawnPoints[3] : null;
                break;
            case HorizontalPosition.PlayerBackCenter:
                spawnPoint = formationManager.playerSpawnPoints.Length > 4 ? formationManager.playerSpawnPoints[4] : null;
                break;
            case HorizontalPosition.PlayerBackRight:
                spawnPoint = formationManager.playerSpawnPoints.Length > 5 ? formationManager.playerSpawnPoints[5] : null;
                break;
        }

        return spawnPoint != null ? spawnPoint.position : Vector3.zero;
    }

    /// <summary>
    /// 生成敌人队伍
    /// </summary>
    private List<CharacterStats> GenerateEnemyParty()
    {
        List<CharacterStats> enemyParty = new List<CharacterStats>();

        if (formationManager == null)
        {
            Debug.LogError("❌ FormationManager为null，无法生成敌人队伍");
            return enemyParty;
        }

        // 获取阵型管理器中配置的敌人预制体
        GameObject[] enemyPrefabs = {
            formationManager.敌人前排左翼,
            formationManager.敌人前排中锋,
            formationManager.敌人前排右翼,
            formationManager.敌人后排左翼,
            formationManager.敌人后排中路,
            formationManager.敌人后排右翼
        };

        HorizontalPosition[] enemyPositions = {
            HorizontalPosition.EnemyFrontLeft,
            HorizontalPosition.EnemyFrontCenter,
            HorizontalPosition.EnemyFrontRight,
            HorizontalPosition.EnemyBackLeft,
            HorizontalPosition.EnemyBackCenter,
            HorizontalPosition.EnemyBackRight
        };

        // 按照阵型管理器配置生成敌人，有预制体才生成
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            GameObject enemyPrefab = enemyPrefabs[i];
            if (enemyPrefab == null) continue; // 跳过未配置的位置

            Vector3 spawnPos = GetSpawnPosition(enemyPositions[i]);
            GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            // 设置敌人朝向（面向玩家）
            enemyObj.transform.localScale = new Vector3(-1, 1, 1);

            CharacterStats enemyStats = enemyObj.GetComponent<CharacterStats>();
            if (enemyStats == null)
            {
                Debug.LogError($"❌ 敌人预制体 {enemyPrefab.name} 缺少CharacterStats组件！");
                continue;
            }

            // 确保敌人属于敌方阵营
            enemyStats.battleSide = BattleSide.Enemy;

            BattlePositionComponent posComp = enemyObj.GetComponent<BattlePositionComponent>();
            if (posComp == null)
            {
                posComp = enemyObj.AddComponent<BattlePositionComponent>();
            }
            posComp.currentPosition = enemyPositions[i];

            enemyParty.Add(enemyStats);

            Debug.Log($"���� 生成敌人: {enemyStats.GetDisplayName()} 在位置 {enemyPositions[i]} 使用预制体 {enemyPrefab.name}");
        }

        return enemyParty;
    }

    /// <summary>
    /// 获取有效的玩家队伍
    /// </summary>
    private List<CharacterStats> GetValidPlayerParty()
    {
        List<CharacterStats> validParty = new List<CharacterStats>();

        foreach (CharacterStats player in currentPlayerTeam)
        {
            if (player != null && player.currentHitPoints > 0)
            {
                validParty.Add(player);
            }
        }

        if (validParty.Count == 0)
        {
            Debug.LogWarning("⚠️ 没有有效的玩家角色！");
        }

        return validParty;
    }

    /// <summary>
    /// 检查队伍是否还有存活成员
    /// </summary>
    private bool HasLivingMembers(List<CharacterStats> party)
    {
        if (party == null || party.Count == 0) return false;

        foreach (CharacterStats character in party)
        {
            if (character != null && character.currentHitPoints > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 生成初始队伍
    /// </summary>
    public void GenerateInitialTeams()
    {
        if (formationManager == null)
        {
            Debug.LogError("❌ FormationManager为null，无法生成队伍");
            return;
        }

        GeneratePlayerTeam();
        Debug.Log($"✅ 队伍生成完成 - 玩家队伍: {currentPlayerTeam.Count}人");
    }

    /// <summary>
    /// 生成玩家队伍
    /// </summary>
    private void GeneratePlayerTeam()
    {
        currentPlayerTeam.Clear();

        HorizontalPosition[] playerPositions = {
            HorizontalPosition.PlayerFrontCenter,
            HorizontalPosition.PlayerBackLeft,
            HorizontalPosition.PlayerBackRight
        };

        GameObject[] playerPrefabs = {
            formationManager.玩家前排中锋,
            formationManager.玩家后排左翼,
            formationManager.玩家后排右翼
        };

        for (int i = 0; i < Mathf.Min(playerPartySize, playerPositions.Length, playerPrefabs.Length); i++)
        {
            if (playerPrefabs[i] == null) continue;

            Vector3 spawnPos = GetSpawnPosition(playerPositions[i]);
            GameObject playerObj = Instantiate(playerPrefabs[i], spawnPos, Quaternion.identity);

            CharacterStats playerStats = playerObj.GetComponent<CharacterStats>();
            if (playerStats == null)
            {
                playerStats = playerObj.AddComponent<CharacterStats>();
            }

            ConfigurePlayerStats(playerStats, i);

            BattlePositionComponent posComp = playerObj.GetComponent<BattlePositionComponent>();
            if (posComp == null)
            {
                posComp = playerObj.AddComponent<BattlePositionComponent>();
            }
            posComp.currentPosition = playerPositions[i];

            currentPlayerTeam.Add(playerStats);

            Debug.Log($"🦸 生成玩家: {playerStats.GetDisplayName()} 在位置 {playerPositions[i]}");
        }
    }

    /// <summary>
    /// 配置玩家属性
    /// </summary>
    private void ConfigurePlayerStats(CharacterStats playerStats, int index)
    {
        playerStats.characterLevel = 3;
        playerStats.battleSide = BattleSide.Player;

        CharacterClass[] playerClasses = {
            CharacterClass.Fighter,
            CharacterClass.Wizard,
            CharacterClass.Cleric
        };

        if (index < playerClasses.Length)
        {
            playerStats.characterClass = playerClasses[index];
        }
        else
        {
            playerStats.characterClass = CharacterClass.Fighter;
        }

        playerStats.maxHitPoints = 30;
        playerStats.currentHitPoints = 30;

        playerStats.strength = 14;
        playerStats.dexterity = 12;
        playerStats.constitution = 14;
        playerStats.intelligence = 10;
        playerStats.wisdom = 12;
        playerStats.charisma = 10;
        playerStats.armorClass = 15;
    }

    /// <summary>
    /// 设置玩家队伍动画
    /// </summary>
    private void SetPlayerPartyAnimation(string animationName)
    {
        foreach (CharacterStats player in currentPlayerTeam)
        {
            if (player != null)
            {
                DND_CharacterAdapter adapter = player.GetComponent<DND_CharacterAdapter>();
                if (adapter != null)
                {
                    switch (animationName)
                    {
                        case "walk":
                            adapter.PlayWalkAnimation();
                            break;
                        case "idle":
                            adapter.PlayIdleAnimation();
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 初始化所有角色动画
    /// </summary>
    private void InitializeAllCharacterAnimations()
    {
        foreach (CharacterStats player in currentPlayerTeam)
        {
            if (player != null)
            {
                DND_CharacterAdapter adapter = player.GetComponent<DND_CharacterAdapter>();
                if (adapter != null)
                {
                    adapter.PlayIdleAnimation();
                }
            }
        }
    }

    /// <summary>
    /// 更新探索进度
    /// </summary>
    private void UpdateExploreProgress()
    {
        // 探索进度更新逻辑
    }

    /// <summary>
    /// 更新UI
    /// </summary>
    private void UpdateUI()
    {
        // UI更新逻辑
    }

    /// <summary>
    /// 给予波次奖励
    /// </summary>
    private void GiveWaveRewards(StageData stageData)
    {
        int expReward = stageData.baseExpReward / 5;
        int goldReward = stageData.baseGoldReward / 5;

        accumulatedRewards.totalExp += expReward;
        accumulatedRewards.totalGold += goldReward;

        Debug.Log($"💰 完成第{currentWave}波，获得 {expReward}经验 + {goldReward}金币");
    }

    /// <summary>
    /// 给���阶段完成奖励
    /// </summary>
    private void GiveStageCompletionRewards(StageData stageData)
    {
        accumulatedRewards.totalExp += stageData.baseExpReward;
        accumulatedRewards.totalGold += stageData.baseGoldReward;
        accumulatedRewards.stagesCompleted++;

        Debug.Log($"🎉 完成阶段 {stageData.stageName}！获得 {stageData.baseExpReward}经验 + {stageData.baseGoldReward}金币");
    }

    /// <summary>
    /// 给予战斗胜利奖励
    /// </summary>
    private void GiveBattleVictoryRewards()
    {
        int battleExp = 50 + (currentStage * 10);
        int battleGold = 25 + (currentStage * 5);

        accumulatedRewards.totalExp += battleExp;
        accumulatedRewards.totalGold += battleGold;
        accumulatedRewards.battlesWon++;

        Debug.Log($"⚔️ 战斗胜利！获得 {battleExp}经验 + {battleGold}金币");
    }

    /// <summary>
    /// 处理战斗失败
    /// </summary>
    private void HandleBattleDefeat()
    {
        Debug.Log("💀 玩家队伍全灭，游戏结束！");
        idleModeEnabled = false;
    }
}

/// <summary>
/// 阶段数据结构
/// </summary>
[System.Serializable]
public class StageData
{
    public string stageName;
    public int enemyLevel;
    public int wavesPerStage;
    public int baseExpReward;
    public int baseGoldReward;
}

/// <summary>
/// 挂机奖励数据结构
/// </summary>
[System.Serializable]
public class IdleRewards
{
    public int totalExp;
    public int totalGold;
    public int battlesWon;
    public int stagesCompleted;
}
