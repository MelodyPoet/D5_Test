using UnityEngine;
using demo2.DND.HorizontalFormation;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 阵型调试器 - 用于诊断角色生成问题
    /// </summary>
    public class FormationDebugger : MonoBehaviour
    {
        [Header("调试目标")]
        public HorizontalBattleFormationManager formationManager;
        public IdleGameManager idleGameManager;

        [Header("调试选项")]
        public bool enableDetailedLogging = true;
        public bool forceGenerateOnStart = true;

        void Start()
        {
            if (forceGenerateOnStart)
            {
                Invoke(nameof(DebugFormationGeneration), 1f); // 延迟1秒执行，确保所有组件初始化完成
            }
        }

        /// <summary>
        /// 调试角色生成过程
        /// </summary>
        [ContextMenu("Debug Formation Generation")]
        public void DebugFormationGeneration()
        {
            Debug.Log("=== 开始调试角色生成 ===");

            // 1. 检查FormationManager
            if (formationManager == null)
            {
                formationManager = FindObjectOfType<HorizontalBattleFormationManager>();
                if (formationManager == null)
                {
                    Debug.LogError("❌ 场景中没有找到 HorizontalBattleFormationManager！");
                    return;
                }
                Debug.Log("✅ 找到 HorizontalBattleFormationManager");
            }

            // 2. 检查预制体配置
            CheckPrefabConfiguration();

            // 3. 检查Spawn点配置
            CheckSpawnPointConfiguration();

            // 4. 尝试生成角色
            TryGenerateCharacters();

            Debug.Log("=== 调试完成 ===");
        }

        /// <summary>
        /// 检查预制体配置
        /// </summary>
        private void CheckPrefabConfiguration()
        {
            Debug.Log("--- 检查预制体配置 ---");

            GameObject[] playerPrefabs = {
                formationManager.playerFrontLeft,
                formationManager.playerFrontCenter,
                formationManager.playerFrontRight,
                formationManager.playerBackLeft,
                formationManager.playerBackCenter,
                formationManager.playerBackRight
            };

            string[] prefabNames = {
                "playerFrontLeft",
                "playerFrontCenter",
                "playerFrontRight",
                "playerBackLeft",
                "playerBackCenter",
                "playerBackRight"
            };

            int validPrefabs = 0;
            for (int i = 0; i < playerPrefabs.Length; i++)
            {
                if (playerPrefabs[i] != null)
                {
                    // 检查预制体是否有必要的组件
                    CharacterStats stats = playerPrefabs[i].GetComponent<CharacterStats>();
                    DND_CharacterAdapter adapter = playerPrefabs[i].GetComponent<DND_CharacterAdapter>();

                    Debug.Log($"✅ {prefabNames[i]}: {playerPrefabs[i].name} " +
                             $"(CharacterStats: {stats != null}, DND_CharacterAdapter: {adapter != null})");

                    if (stats == null)
                    {
                        Debug.LogWarning($"⚠️ {prefabNames[i]} 缺少 CharacterStats 组件！");
                    }
                    if (adapter == null)
                    {
                        Debug.LogWarning($"⚠️ {prefabNames[i]} 缺少 DND_CharacterAdapter 组件！");
                    }

                    validPrefabs++;
                }
                else
                {
                    Debug.LogWarning($"❌ {prefabNames[i]} 预制体为null");
                }
            }

            Debug.Log($"有效预制体数量: {validPrefabs}/6");
        }

        /// <summary>
        /// 检查Spawn点配置
        /// </summary>
        private void CheckSpawnPointConfiguration()
        {
            Debug.Log("--- 检查Spawn点配置 ---");

            if (formationManager.playerSpawnPoints == null)
            {
                Debug.LogError("❌ playerSpawnPoints 数组为null！");
                return;
            }

            if (formationManager.playerSpawnPoints.Length < 6)
            {
                Debug.LogError($"❌ playerSpawnPoints 数组长度不足！当前: {formationManager.playerSpawnPoints.Length}, 需要: 6");
                return;
            }

            int validSpawnPoints = 0;
            for (int i = 0; i < formationManager.playerSpawnPoints.Length; i++)
            {
                if (formationManager.playerSpawnPoints[i] != null)
                {
                    Vector3 pos = formationManager.playerSpawnPoints[i].position;
                    Debug.Log($"✅ SpawnPoint[{i}]: {formationManager.playerSpawnPoints[i].name} at {pos}");
                    validSpawnPoints++;
                }
                else
                {
                    Debug.LogError($"❌ SpawnPoint[{i}] 为null！");
                }
            }

            Debug.Log($"有效Spawn点数量: {validSpawnPoints}/{formationManager.playerSpawnPoints.Length}");
        }

        /// <summary>
        /// 尝试生成角色
        /// </summary>
        private void TryGenerateCharacters()
        {
            Debug.Log("--- 尝试生成角色 ---");

            try
            {
                // 先清空现有角色
                formationManager.ClearPlayerFormation();
                Debug.Log("✅ 清空现有玩家阵型");

                // 生成新角色
                formationManager.GeneratePlayerFormation();
                Debug.Log("✅ 调用 GeneratePlayerFormation()");

                // 检查生成结果
                var aliveCharacters = formationManager.GetAllAliveCharacters(BattleSide.Player);
                Debug.Log($"✅ 生成完成，存活角色数量: {aliveCharacters.Count}");

                foreach (var character in aliveCharacters)
                {
                    Debug.Log($"   - {character.GetDisplayName()} (HP: {character.currentHitPoints}/{character.maxHitPoints})");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 生成角色时发生错误: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 强制重新生成角色（可在Inspector中调用）
        /// </summary>
        [ContextMenu("Force Regenerate Characters")]
        public void ForceRegenerateCharacters()
        {
            if (formationManager != null)
            {
                formationManager.ClearPlayerFormation();
                formationManager.GeneratePlayerFormation();
                Debug.Log("强制重新生成角色完成");
            }
            else
            {
                Debug.LogError("FormationManager 为null，无法重新生成角色");
            }
        }

        /// <summary>
        /// 检查IdleGameManager状态
        /// </summary>
        [ContextMenu("Check IdleGameManager Status")]
        public void CheckIdleGameManagerStatus()
        {
            if (idleGameManager == null)
            {
                idleGameManager = FindObjectOfType<IdleGameManager>();
            }

            if (idleGameManager != null)
            {
                Debug.Log($"IdleGameManager状态:");
                Debug.Log($"  - useFormationManager: {idleGameManager.useFormationManager}");
                Debug.Log($"  - formationManager引用: {idleGameManager.formationManager != null}");
                Debug.Log($"  - idleModeEnabled: {idleGameManager.idleModeEnabled}");
            }
            else
            {
                Debug.LogError("场景中没有找到 IdleGameManager");
            }
        }
    }
}
