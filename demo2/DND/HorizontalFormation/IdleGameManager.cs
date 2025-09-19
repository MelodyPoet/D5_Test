using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 挂机模式管理器
    /// 使用事件驱动模式，摒弃协程依赖
    /// </summary>
    public class IdleGameManager : MonoBehaviour
    {
        [Header("挂机模式设置")]
        public bool idleModeEnabled;
        public float encounterInterval = 10f;
        public float battleSpeed = 1f;

        [Header("队伍生成设置")]
        [Tooltip("是否使用阵型管理器生成队伍（推荐开启）")]
        public bool useFormationManager = true;
        [Tooltip("玩家队伍人数上限")]
        public int playerPartySize = 3;

        [Header("系统组件")]
        public HorizontalBattleFormationManager formationManager;
        public AutoBattleAI autoBattleAI;

        // 私有变量 - 移除协程相关
        private bool isInBattle;
        private float nextEncounterTime;
        private float teamGenerationTimer;
        private bool isWaitingForTeamGeneration;

        // 当前活跃的队伍
        private List<CharacterStats> currentPlayerTeam = new List<CharacterStats>();

        void Start()
        {
            SetupUI();
            InitializeIdleSystem();
        }

        void Update()
        {
            // 处理队伍生成等待
            if (isWaitingForTeamGeneration)
            {
                HandleTeamGenerationWait();
                return;
            }

            // 挂机模式更新
            if (idleModeEnabled && !isInBattle)
            {
                UpdateExploreProgress();
                CheckForRandomEncounter();
            }

            UpdateUI();
        }

        /// <summary>
        /// 初始化挂机系统
        /// </summary>
        private void InitializeIdleSystem()
        {
            Debug.Log("=== 开始初始化挂机系统 ===");

            // 自动查找组件引用
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

                // 设置等待队伍生成完成
                isWaitingForTeamGeneration = true;
                teamGenerationTimer = Time.time + 0.5f; // 等待0.5秒
            }
            else
            {
                Debug.LogWarning("⚠️ useFormationManager 已禁用，跳过队伍生成");
            }
        }

        /// <summary>
        /// 处理队伍生成等待 - 替代协程的Update方式
        /// </summary>
        private void HandleTeamGenerationWait()
        {
            if (Time.time < teamGenerationTimer) return;

            isWaitingForTeamGeneration = false;

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
                Debug.LogError("❌ 队伍生成失败！尝试重新生成...");

                // 重新生成
                GenerateInitialTeams();
                isWaitingForTeamGeneration = true;
                teamGenerationTimer = Time.time + 1f; // 等待1秒后重试
            }
        }

        /// <summary>
        /// 启动探索模式 - 事件驱动版本
        /// </summary>
        private void StartExploreMode()
        {
            idleModeEnabled = true;
            StartBackgroundScrolling();

            Debug.Log("🚀 探索模式已启动！");
        }

        /// <summary>
        /// 检查随机遭遇 - 替代协程的Update检查
        /// </summary>
        private void CheckForRandomEncounter()
        {
            if (Time.time >= nextEncounterTime)
            {
                StartRandomEncounter();
                nextEncounterTime = Time.time + encounterInterval;
            }
        }

        /// <summary>
        /// 开始随机遭遇 - 事件驱动版本
        /// </summary>
        private void StartRandomEncounter()
        {
            if (isInBattle) return;

            // 检查玩家队伍是否还有存活成员
            if (!formationManager.HasAliveCharacters(BattleSide.Player))
            {
                Debug.LogWarning("⚠️ 没有有效的玩家角色！");
                return;
            }

            isInBattle = true;
            Debug.Log("⚔️ 遭遇战斗开始！");

            // 切换到战斗模式
            formationManager.SetFormationBattleState();
            StopBackgroundScrolling();

            // 生成敌人
            formationManager.GenerateEnemyFormation();

            // 使用延迟启动战斗，等待敌人进场
            Invoke(nameof(StartBattleAfterDelay), 2f);
        }

        /// <summary>
        /// 延迟启动战斗 - 替代协程等待
        /// </summary>
        private void StartBattleAfterDelay()
        {
            if (autoBattleAI != null)
            {
                Debug.Log("🎯 敌人进场完成，启动先攻系统...");
                autoBattleAI.StartBattleSequence();
            }
        }

        /// <summary>
        /// 战斗完成回调 - 由AutoBattleAI调用
        /// </summary>
        public void OnBattleCompleted(bool playerVictory)
        {
            Debug.Log($"⚔️ 战斗结束！结果: {(playerVictory ? "玩家胜利" : "玩家失败")}");

            if (playerVictory)
            {
                GiveBattleVictoryRewards();
                formationManager.ClearEnemyFormation();

                // 恢复探索状态
                Invoke(nameof(RestoreExplorationState), 1f);
            }
            else
            {
                HandleBattleDefeat();
            }
        }

        /// <summary>
        /// 恢复探索状态 - 延迟调用
        /// </summary>
        private void RestoreExplorationState()
        {
            if (formationManager.HasAliveCharacters(BattleSide.Player))
            {
                formationManager.RestorePlayerExplorationState();
                ResumeBackgroundScrolling();
                isInBattle = false;

                Debug.Log("🚀 重新开始探索模式");
            }
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
        /// 生成初始队伍
        /// </summary>
        public void GenerateInitialTeams()
        {
            if (formationManager == null)
            {
                Debug.LogError("❌ FormationManager为null，无法生成队伍");
                return;
            }

            formationManager.GeneratePlayerFormation();
            Debug.Log($"✅ 队伍生成完成");
        }

        /// <summary>
        /// 设置UI
        /// </summary>
        private void SetupUI()
        {
            // UI初始化代码
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
            Debug.Log("💰 战斗胜利！获得经验和金币");
        }

        /// <summary>
        /// 处理战斗失败
        /// </summary>
        private void HandleBattleDefeat()
        {
            Debug.Log("💀 玩家队伍全灭，游戏结束！");
            idleModeEnabled = false;
            isInBattle = false;
        }

        /// <summary>
        /// 触发敌人遭遇 - 手动触发接口
        /// </summary>
        public void TriggerEnemyEncounter()
        {
            if (isInBattle)
            {
                Debug.LogWarning("⚠️ 已经在战斗中，跳过新的遭遇");
                return;
            }

            nextEncounterTime = Time.time + encounterInterval;
            StartRandomEncounter();
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
