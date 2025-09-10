using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
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

        // 当前活跃的队伍（运行时生成）
        private List<CharacterStats> currentPlayerTeam = new List<CharacterStats>();

        void Start()
        {
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
            Debug.Log("=== 开始初始化挂机系统 ===");

            // 自动查找组件引用（如果没有手动设置）
            if (formationManager == null)
            {
                formationManager = FindObjectOfType<HorizontalBattleFormationManager>();
                if (formationManager == null)
                {
                    Debug.LogError("❌ IdleGameManager: 场景中没有找到 HorizontalBattleFormationManager 组件！");
                    return;
                }
                else
                {
                    Debug.Log("✅ 自动找到 HorizontalBattleFormationManager 组件");
                }
            }

            if (autoBattleAI == null)
            {
                autoBattleAI = FindObjectOfType<AutoBattleAI>();
                if (autoBattleAI == null)
                {
                    Debug.LogError("❌ IdleGameManager: 场景中没有找到 AutoBattleAI 组件！");
                    return;
                }
                else
                {
                    Debug.Log("✅ 自动找到 AutoBattleAI 组件");
                }
            }

            nextEncounterTime = Time.time + encounterInterval;

            if (useFormationManager)
            {
                Debug.Log("🎯 开始生成初始队伍...");
                GenerateInitialTeams();

                // 添加延迟检查，确保角色生成完成
                StartCoroutine(CheckTeamGenerationResult());
            }
            else
            {
                Debug.LogWarning("⚠️ useFormationManager 已禁用，跳过队伍生成");
            }
        }

        /// <summary>
        /// 检查队伍生成结果的协程
        /// </summary>
        private IEnumerator CheckTeamGenerationResult()
        {
            yield return new WaitForSeconds(0.5f); // 等待生成完成

            var aliveCharacters = formationManager.GetAllAliveCharacters(BattleSide.Player);
            if (aliveCharacters.Count > 0)
            {
                Debug.Log($"✅ 队伍生成成功！存活角色数量: {aliveCharacters.Count}");
                foreach (var character in aliveCharacters)
                {
                    Debug.Log($"   - {character.GetDisplayName()} (HP: {character.currentHitPoints}/{character.maxHitPoints})");
                }
                StartExploreMode();
            }
            else
            {
                Debug.LogError("❌ 队伍生成失败！没有找到任何存活的玩家角色");
                Debug.LogError("请检查以下配置:");
                Debug.LogError("1. HorizontalBattleFormationManager 的预制体字段是否已设置");
                Debug.LogError("2. playerSpawnPoints 数组是否已正确配置（需要6个Transform）");
                Debug.LogError("3. 预制体是否包含 CharacterStats 和 DND_CharacterAdapter 组件");

                // 尝试重新生成一次
                Debug.Log("🔄 尝试重新生成队伍...");
                yield return new WaitForSeconds(1f);
                GenerateInitialTeams();

                yield return new WaitForSeconds(0.5f);
                var retryResult = formationManager.GetAllAliveCharacters(BattleSide.Player);
                if (retryResult.Count > 0)
                {
                    Debug.Log($"✅ 重试成功！存活角色数量: {retryResult.Count}");
                    StartExploreMode();
                }
                else
                {
                    Debug.LogError("❌ 重试失败！请检查配置或使用 FormationDebugger 进行诊断");
                }
            }
        }

        /// <summary>
        /// 启动探索模式 - 使用新的动画系统
        /// </summary>
        private void StartExploreMode()
        {
            idleModeEnabled = true;
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
        /// 挂机游戏主循环
        /// </summary>
        private IEnumerator IdleGameLoop()
        {
            while (idleModeEnabled)
            {
                if (Time.time >= nextEncounterTime)
                {
                    yield return StartCoroutine(StartRandomEncounter());
                    nextEncounterTime = Time.time + encounterInterval;
                }

                yield return new WaitForSeconds(1f / battleSpeed);
            }
        }

        /// <summary>
        /// 开始随机遭遇 - 使用新的阵型管理器
        /// </summary>
        private IEnumerator StartRandomEncounter()
        {
            if (isInBattle) yield break;

            // 检查玩家队伍是否还有存活成员
            if (!formationManager.HasAliveCharacters(BattleSide.Player))
            {
                Debug.LogWarning("⚠️ 没有有效的玩家角色！");
                yield break;
            }

            isInBattle = true;

            // 切换到战斗模式
            formationManager.SetFormationBattleState();
            StopBackgroundScrolling();

            // 生成敌人并等待进场完成
            formationManager.GenerateEnemyFormation();
            yield return new WaitForSeconds(2f); // 等待敌人进场动画完成

            // 使用先攻系统开始战斗
            if (autoBattleAI != null)
            {
                Debug.Log("🎯 敌人进场完成，启动先攻系统...");
                autoBattleAI.StartBattleSequence();

                // 等待先攻系统战斗完成
                yield return new WaitUntil(() => !autoBattleAI.isBattleActive);
            }

            // 战斗结束后的清理
            yield return StartCoroutine(HandleBattleEnd());

            isInBattle = false;
        }

        /// <summary>
        /// 处理战斗结束 - 简化版本
        /// </summary>
        private IEnumerator HandleBattleEnd()
        {
            // 判断战斗结果
            if (formationManager.HasAliveCharacters(BattleSide.Player))
            {
                Debug.Log("🎉 玩家胜利！");
                GiveBattleVictoryRewards();

                // 清理敌人
                formationManager.ClearEnemyFormation();
            }
            else
            {
                Debug.Log("💀 玩家败北！");
                HandleBattleDefeat();
            }

            yield return new WaitForSeconds(1f);

            // 恢复探索状态
            if (formationManager.HasAliveCharacters(BattleSide.Player))
            {
                formationManager.RestorePlayerExplorationState();
                ResumeBackgroundScrolling();
            }
        }

        /// <summary>
        /// 生成初始队伍 - 使用阵型管理器
        /// </summary>
        public void GenerateInitialTeams()
        {
            if (formationManager == null)
            {
                Debug.LogError("❌ FormationManager为null，无法生成队伍");
                return;
            }

            // 使用阵型管理器生成玩家队伍
            formationManager.GeneratePlayerFormation();

            Debug.Log($"✅ 队伍生成完成 - 玩家队伍: {formationManager.GetAllAliveCharacters(BattleSide.Player).Count}人");
        }

        /// <summary>
        /// 设置UI
        /// </summary>
        private void SetupUI()
        {
            // UI初始化代码，如果项目没有UI模块则注释掉
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
        /// 给予战斗胜利奖励
        /// </summary>
        private void GiveBattleVictoryRewards()
        {
            Debug.Log("战斗胜利！获得经验和金币");
        }

        /// <summary>
        /// 处理战斗失败
        /// </summary>
        private void HandleBattleDefeat()
        {
            Debug.Log("玩家队伍全灭，游戏结束！");
            idleModeEnabled = false;
        }
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
    }
}
