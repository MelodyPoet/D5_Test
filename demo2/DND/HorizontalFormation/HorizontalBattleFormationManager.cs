using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using DG.Tweening;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 横版战斗阵型管理器 - 负责阵型配置和位置管理
    /// 使用容器化配置简化预制体管理
    /// 修复版本：解决敌人朝向时序问题和重复方法定义
    /// </summary>
    public class HorizontalBattleFormationManager : MonoBehaviour {
        [Header("阵型容器配置")]
        [Tooltip("阵型配置容器 - 统一管理所有预制体")]
        [SerializeField] private FormationContainer formationContainer;

        [Space(15)]
        [Header("阵型参数设置")]
        [Tooltip("战场宽度 - 整个战场的总宽度")]
        public float battlefieldWidth = 16f;
        [Tooltip("角色间距 - 同排角色之间的距离")]
        public float characterSpacing = 2f;
        [Tooltip("前后排距离 - 前排和后排之间的距离")]
        public float rankDistance = 3f;

        [Space(10)]
        [Header("位置Transform配置")]
        [Tooltip("玩家阵型spawn点 - 严格按序：前排左/中/右，后排左/中/右")]
        public Transform[] playerSpawnPoints = new Transform[6];
        [Tooltip("敌人阵型spawn点 - 严格按序：前排左/中/右，后排左/中/右")]
        public Transform[] enemySpawnPoints = new Transform[6];
        [Tooltip("敌人生成点的父节点 - 用于整体移动控制")]
        public Transform enemySpawnParent;

        [Space(10)]
        [Header("敌人进场配置")]
        [Tooltip("敌人整体进场起始偏移距离（屏幕右侧）")]
        public float enemyEntranceOffset = 8f;
        [Tooltip("敌人整体进场动画时长")]
        public float enemyEntranceDuration = 2f;

        [Space(15)]
        [Header("UI Prefab")]
        [Tooltip("血条UI预制体")]
        [SerializeField] private GameObject healthBarPrefab;
        [Tooltip("玩家血条UI的容器（屏幕左侧）")]
        [SerializeField] private Transform playerHealthBarContainer;
        [Tooltip("敌人血条UI的容器（屏幕右侧）")]
        [SerializeField] private Transform enemyHealthBarContainer;
        [SerializeField] private DamageEventChannel_SO globalDamageEventChannel; // Inspector拖拽赋值

        // 运行时数据
        private List<GameObject> activePlayerCharacters = new List<GameObject>();
        private List<GameObject> activeEnemyCharacters = new List<GameObject>();
        private int _currentWaveIndex = 0;

        public int GetEnemyWaveCount()
        {
            if (formationContainer == null) return 0;
            return formationContainer.GetEnemyWaveCount();
        }

        private void Awake()
        {
            // 确保 HealthBarUIManager 单例存在并与本 manager 的容器/预制体保持一致
            if (HealthBarUIManager.Instance == null)
            {
                GameObject go = new GameObject("HealthBarUIManager");
                var mgr = go.AddComponent<HealthBarUIManager>();
                // 复制配置到新创建的管理器（如果本脚本上配置了容器/预制体）
                mgr.healthBarPrefab = healthBarPrefab;
                mgr.playerHealthBarContainer = playerHealthBarContainer;
                mgr.enemyHealthBarContainer = enemyHealthBarContainer;

                Debug.Log("HorizontalBattleFormationManager: Created HealthBarUIManager singleton and synced containers/prefab.");
            }
            else
            {
                // 如果已经存在单例，但其容器未配置，则同步当前配置（以避免引用不一致）
                if (HealthBarUIManager.Instance.playerHealthBarContainer == null && playerHealthBarContainer != null)
                {
                    HealthBarUIManager.Instance.playerHealthBarContainer = playerHealthBarContainer;
                    Debug.Log("HorizontalBattleFormationManager: Synced playerHealthBarContainer to existing HealthBarUIManager.");
                }
                if (HealthBarUIManager.Instance.enemyHealthBarContainer == null && enemyHealthBarContainer != null)
                {
                    HealthBarUIManager.Instance.enemyHealthBarContainer = enemyHealthBarContainer;
                    Debug.Log("HorizontalBattleFormationManager: Synced enemyHealthBarContainer to existing HealthBarUIManager.");
                }
                if (HealthBarUIManager.Instance.healthBarPrefab == null && healthBarPrefab != null)
                {
                    HealthBarUIManager.Instance.healthBarPrefab = healthBarPrefab;
                    Debug.Log("HorizontalBattleFormationManager: Synced healthBarPrefab to existing HealthBarUIManager.");
                }
            }
        }

        /// <summary>
        /// 生成玩家阵型
        /// </summary>
        public void GeneratePlayerFormation()
        {
            try
            {
                ClearPlayerFormation();
                if (formationContainer == null)
                {
                    Debug.LogError("阵型容器未配置！请在HorizontalBattleFormationManager中设置FormationContainer");
                    return;
                }
                if (playerSpawnPoints.Length < 6)
                {
                    Debug.LogError("玩家spawn点配置不足！需要6个位置点");
                    return;
                }
                activePlayerCharacters.Clear();
                for (int i = 0; i < 6; i++)
                {
                    activePlayerCharacters.Add(null);
                }
                for (int i = 0; i < 6; i++)
                {
                    GameObject prefab = formationContainer.GetPlayerPrefab(i);
                    InstantiatePlayerCharacterAtIndex(prefab, playerSpawnPoints[i], BattleSide.Player, i);
                }
                SetPlayerFormationWalkingState();
                Debug.Log($"玩家阵型完成，列表状态: {GetFormationDebugInfo(activePlayerCharacters)}");
                // 新增：收集所有非null角色并初始化血条UI
                var playerStats = new List<CharacterStats>();
                foreach (var go in activePlayerCharacters)
                {
                    if (go != null)
                    {
                        var stats = go.GetComponent<CharacterStats>();
                        if (stats != null) playerStats.Add(stats);
                    }
                }

                if (HealthBarUIManager.Instance != null)
                {
                    HealthBarUIManager.Instance.InitializeBars(playerStats);
                }
                else
                {
                    Debug.LogWarning("HealthBarUIManager.Instance 为 null，无法初始化玩家血条 UI。请确保场景中有该单例。参考 HorizontalBattleFormationManager.CreateHealthBarForCharacter");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GeneratePlayerFormation 发生异常: {ex}");
            }
        }

        /// <summary>
        /// 生成敌人阵型（带整体进场动画）
        /// 使用DOTween事件驱动，摒弃协程
        /// </summary>
        public void GenerateEnemyFormation(int waveIndex)
        {
            try
            {
                ClearEnemyFormation();
                if (formationContainer == null)
                {
                    Debug.LogError("阵型容器未配置！请在HorizontalBattleFormationManager中设置FormationContainer");
                    return;
                }

                if (waveIndex >= formationContainer.GetEnemyWaveCount())
                {
                    Debug.LogWarning($"请求的波次索引 {waveIndex} 超出总波次数 {formationContainer.GetEnemyWaveCount()}。战斗结束或无更多波次。");
                    // 在这里可以触发战斗胜利或结束所有波次的逻辑
                    return;
                }
                _currentWaveIndex = waveIndex;

                if (enemySpawnPoints.Length < 6)
                {
                    Debug.LogError("敌人spawn点配置不足！需要6个位置点");
                    return;
                }
                if (enemySpawnParent == null)
                {
                    Debug.LogError("敌人生成点父节点未配置！请在Inspector中设置enemySpawnParent");
                    return;
                }
                activeEnemyCharacters.Clear();
                for (int i = 0; i < 6; i++)
                {
                    activeEnemyCharacters.Add(null);
                }

                GameObject[] enemyPrefabs = formationContainer.GetEnemyFormation(_currentWaveIndex);

                Vector3 originalParentPosition = enemySpawnParent.position;
                enemySpawnParent.position = originalParentPosition + Vector3.right * enemyEntranceOffset;
                for (int i = 0; i < 6; i++)
                {
                    GameObject prefab = enemyPrefabs[i];
                    InstantiateEnemyCharacterAtCurrentPosition(prefab, enemySpawnPoints[i], BattleSide.Enemy, i);
                }
                ExecuteFormationEntranceAnimationDoTween(originalParentPosition);
                Debug.Log($"敌人阵型(波次: {_currentWaveIndex})生成完成，整体进场动画开始，列表状态: {GetFormationDebugInfo(activeEnemyCharacters)}");
                // 新增：收集所有非null角色并初始化血条UI
                var enemyStats = new List<CharacterStats>();
                foreach (var go in activeEnemyCharacters)
                {
                    if (go != null)
                    {
                        var stats = go.GetComponent<CharacterStats>();
                        if (stats != null) enemyStats.Add(stats);
                    }
                }

                if (HealthBarUIManager.Instance != null)
                {
                    HealthBarUIManager.Instance.InitializeBars(enemyStats);
                }
                else
                {
                    Debug.LogWarning("HealthBarUIManager.Instance 为 null，无法初始化敌人血条 UI。");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GenerateEnemyFormation 发生异常: {ex}");
            }
        }

        [System.Obsolete("此方法已过时，请使用 GenerateEnemyFormation(int waveIndex) 替代")]
        public void GenerateEnemyFormation()
        {
            GenerateEnemyFormation(0);
        }

        /// <summary>
        /// 执行整个阵型的进场动画 - DOTween版本
        /// </summary>
        private void ExecuteFormationEntranceAnimationDoTween(Vector3 targetParentPosition)
        {
            // 创建DOTween序列
            Sequence entranceSequence = DOTween.Sequence();

            // 小延迟确保所有角色完成初始化
            entranceSequence.AppendInterval(0.1f);

            // 整体移动父节点回到目标位置
            entranceSequence.Append(enemySpawnParent.DOMove(targetParentPosition, enemyEntranceDuration));

            // 进场动画完成后的处理
            entranceSequence.OnComplete(() => {
                // 切换所有敌人到待机动画
                SetAllEnemyToIdleAnimation();

                // 关键修复：更新所有敌人的原始位置
                UpdateAllEnemyOriginalPositions();

                Debug.Log("敌人阵型整体进场完成，已切换到待机状态并更新原始位置");
            });
        }

        /// <summary>
        /// 更新所有敌人的原始位置 - 修复战斗移动问题
        /// </summary>
        private void UpdateAllEnemyOriginalPositions()
        {
            foreach (GameObject enemy in activeEnemyCharacters)
            {
                if (enemy != null)
                {
                    DND_CharacterAdapter adapter = enemy.GetComponent<DND_CharacterAdapter>();
                    if (adapter != null)
                    {
                        adapter.UpdateOriginalPosition();
                    }
                }
            }
        }

        /// <summary>
        /// 设置所有敌人切换到待机动画
        /// </summary>
        private void SetAllEnemyToIdleAnimation()
        {
            foreach (GameObject enemy in activeEnemyCharacters)
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
        }

        /// <summary>
        /// 实例化玩家角色（指定索引位置）
        /// </summary>
        private void InstantiatePlayerCharacterAtIndex(GameObject prefab, Transform spawnPoint, BattleSide battleSide, int index)
        {
            if (prefab == null || spawnPoint == null)
            {
                Debug.LogWarning($"玩家角色索引{index}的预制体或spawn点为null，保持null占位");
                return; // 保持null占位，不影响其他角色的索引
            }

            // 记录预制体的原始缩放值
            Vector3 originalScale = prefab.transform.localScale;

            GameObject instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

            // 保持原始缩放值（解决缩放值不对的问题）
            instance.transform.localScale = originalScale;

            // 配置角色阵营
            CharacterStats stats = instance.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.battleSide = battleSide;
                // 为角色创建并关联血条
                CreateHealthBarForCharacter(stats);
            }
            else
            {
                Debug.LogError($"角色预制体 {prefab.name} 缺少 CharacterStats 组件！");
                DestroyImmediate(instance);
                return;
            }

            // 添加位置组件并设置正确的位置信息
            BattlePositionComponent positionComponent = instance.GetComponent<BattlePositionComponent>();
            if (positionComponent == null)
            {
                positionComponent = instance.AddComponent<BattlePositionComponent>();
            }

            // 根据索引设置正确的位置枚举
            positionComponent.currentPosition = (HorizontalPosition)index;
            positionComponent.isOccupied = true;

            Debug.Log($"玩家角色 {prefab.name} 重置组件设置: {positionComponent.currentPosition}");

            // 添加到指定索引位置
            activePlayerCharacters[index] = instance;

            Debug.Log($"玩家角色 {prefab.name} 已生成在索引{index}，缩放值: {instance.transform.localScale}");
        }

        /// <summary>
        /// 在当前位置实例化敌人角色（修复版本 - 解决朝向继承问题）
        /// 关键修复：先生成敌人，设置正确朝向，再设置为spawn点的子物体
        /// </summary>
        private void InstantiateEnemyCharacterAtCurrentPosition(GameObject prefab, Transform spawnPoint, BattleSide battleSide, int index)
        {
            if (prefab == null || spawnPoint == null)
            {
                Debug.LogWarning($"敌人角色索引{index}的预制体或spawn点为null，保持null占位");
                return; // 保持null占位，不影响其他角色的索引
            }

            // 记录预制体的原始缩放值
            Vector3 originalScale = prefab.transform.localScale;

            // 🔥 关键修复：先在世界坐标系中生成，避免父子继承问题
            GameObject instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

            // 保持原始缩放值
            instance.transform.localScale = originalScale;

            // 🔥 新的朝向控制方法：使用Spine的ScaleX属性
            SkeletonAnimation skeletonAnimation = instance.GetComponent<SkeletonAnimation>();
            if (skeletonAnimation != null)
            {
                // 敌人面向左侧（面向玩家），设置ScaleX为负值
                if (skeletonAnimation.skeleton != null)
                {
                    skeletonAnimation.skeleton.ScaleX = -Mathf.Abs(skeletonAnimation.skeleton.ScaleX);
                    Debug.Log($"敌人 {instance.name} 使用Spine ScaleX设置朝向：{skeletonAnimation.skeleton.ScaleX}");
                }
                else
                {
                    Debug.LogWarning($"SkeletonAnimation 组件存在但 skeleton 为 null，无法设置 ScaleX（{instance.name}）");
                }
            }
            else
            {
                // 如果没有SkeletonAnimation组件，回退到缩放方法
                Debug.LogWarning($"敌人 {instance.name} 没有SkeletonAnimation组件，使用缩放方法设置朝向");
                Vector3 enemyScale = instance.transform.localScale;
                enemyScale.x = -Mathf.Abs(enemyScale.x);
                instance.transform.localScale = enemyScale;
            }

            // 🔥 在朝向设置完成后，再设置父子关系
            instance.transform.SetParent(spawnPoint, true); // worldPositionStays = true 保持世界位置

            // 配置角色阵营
            CharacterStats stats = instance.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.battleSide = battleSide;
                // 为角色创建并关联血条
                CreateHealthBarForCharacter(stats);
            }
            else
            {
                Debug.LogError($"角色预制体 {prefab.name} 缺少 CharacterStats 组件！");
                DestroyImmediate(instance);
                return;
            }

            // 添加位置组件并设置正确的位置信息
            BattlePositionComponent positionComponent = instance.GetComponent<BattlePositionComponent>();
            if (positionComponent == null)
            {
                positionComponent = instance.AddComponent<BattlePositionComponent>();
            }

            // 根据索引设置正确的位置枚举
            positionComponent.currentPosition = (HorizontalPosition)(index + 6);
            positionComponent.isOccupied = true;

            Debug.Log($"敌人角色 {prefab.name} 位置组件设置: {positionComponent.currentPosition}");

            // 获取动画适配器并播放走路动画（朝向已经正确设置）
            DND_CharacterAdapter adapter = instance.GetComponent<DND_CharacterAdapter>();
            if (adapter != null)
            {
                // 播放走路动画，整体进场时保持走路状态
                adapter.PlayWalkAnimation();
            }

            // 添加到指定索引位置
            activeEnemyCharacters[index] = instance;

            Debug.Log($"敌人角色 {prefab.name} 已生成在索引{index}，最终缩放值: {instance.transform.localScale}，父物体: {instance.transform.parent?.name}");
        }

        /// <summary>
        /// 设置玩家队伍为走路状态
        /// </summary>
        private void SetPlayerFormationWalkingState()
        {
            foreach (GameObject character in activePlayerCharacters)
            {
                if (character != null)
                {
                    DND_CharacterAdapter adapter = character.GetComponent<DND_CharacterAdapter>();
                    if (adapter != null)
                    {
                        adapter.PlayWalkAnimation();
                    }
                }
            }
            Debug.Log("玩家队伍开始探索（走路动画）");
        }

        /// <summary>
        /// 检查角色是否在前排
        /// 根据技术规格：SpawnPoints[0]~[2]为前排近战，SpawnPoints[3]~[5]为后排远程
        /// </summary>
        public bool IsCharacterInFrontRow(CharacterStats character)
        {
            if (character == null) return false;

            // 优先通过BattlePositionComponent组件判断
            BattlePositionComponent positionComponent = character.GetComponent<BattlePositionComponent>();
            if (positionComponent != null)
            {
                return positionComponent.rowPosition == RowPosition.Front;
            }

            // 基于SpawnPoints位置索引的正确判断逻辑
            int spawnIndex = GetCharacterSpawnIndex(character);
            if (spawnIndex >= 0)
            {
                // SpawnPoints[0]~[2]：前排近战，SpawnPoints[3]~[5]：后排远程
                bool isFrontRow = spawnIndex <= 2;
                Debug.Log($"[DEBUG] {character.GetDisplayName()} SpawnIndex: {spawnIndex}, 判断为: {(isFrontRow ? "前排近战" : "后排远程")}");
                return isFrontRow;
            }

            // 如果找不到SpawnIndex，使用默认前排（近战）
            Debug.LogWarning($"[DEBUG] {character.GetDisplayName()} 找不到SpawnIndex，默认判断为前排近战");
            return true;
        }

        /// <summary>
        /// 获取角色在SpawnPoints数组中的索引位置
        /// </summary>
        private int GetCharacterSpawnIndex(CharacterStats character)
        {
            if (character == null) return -1;

            // 根据角色阵营选择对应的spawn点数组和角色列表
            if (character.battleSide == BattleSide.Player)
            {
                // 在玩家角色列表中查找
                for (int i = 0; i < activePlayerCharacters.Count && i < playerSpawnPoints.Length; i++)
                {
                    if (activePlayerCharacters[i] != null)
                    {
                        CharacterStats stats = activePlayerCharacters[i].GetComponent<CharacterStats>();
                        if (stats == character)
                        {
                            return i; // 返回在SpawnPoints数组中的索引
                        }
                    }
                }
            }
            else if (character.battleSide == BattleSide.Enemy)
            {
                // 在敌人角色列表中查找
                for (int i = 0; i < activeEnemyCharacters.Count && i < enemySpawnPoints.Length; i++)
                {
                    if (activeEnemyCharacters[i] != null)
                    {
                        CharacterStats stats = activeEnemyCharacters[i].GetComponent<CharacterStats>();
                        if (stats == character)
                        {
                            return i; // 返回在SpawnPoints数组中的索引
                        }
                    }
                }
            }

            return -1; // 未找到
        }

        /// <summary>
        /// 获取指定阵营的所有存活角色
        /// </summary>
        public List<CharacterStats> GetAllAliveCharacters(BattleSide side)
        {
            List<CharacterStats> aliveCharacters = new List<CharacterStats>();

            List<GameObject> characterList = (side == BattleSide.Player) ?
                activePlayerCharacters : activeEnemyCharacters;

            foreach (GameObject characterObj in characterList)
            {
                if (characterObj != null)
                {
                    CharacterStats stats = characterObj.GetComponent<CharacterStats>();
                    if (stats != null && stats.currentHitPoints > 0)
                    {
                        aliveCharacters.Add(stats);
                    }
                }
            }

            return aliveCharacters;
        }

        /// <summary>
        /// 检查指定阵营是否还有存活角色
        /// </summary>
        public bool HasAliveCharacters(BattleSide side)
        {
            return GetAllAliveCharacters(side).Count > 0;
        }

        /// <summary>
        /// 设置阵型为战斗状态
        /// </summary>
        public void SetFormationBattleState()
        {
            // 停止所有角色的行走动画
            StopAllCharacterWalkAnimations();

            Debug.Log("阵型已切换到战斗状态");
        }

        /// <summary>
        /// 停止所有角色的行走动画
        /// </summary>
        private void StopAllCharacterWalkAnimations()
        {
            // 停止玩家角色行走动画
            foreach (GameObject player in activePlayerCharacters)
            {
                if (player != null)
                {
                    DND_CharacterAdapter adapter = player.GetComponent<DND_CharacterAdapter>();
                    adapter?.StopWalkWithTransition();
                }
            }

            // 停止敌人角色行走动画
            foreach (GameObject enemy in activeEnemyCharacters)
            {
                if (enemy != null)
                {
                    DND_CharacterAdapter adapter = enemy.GetComponent<DND_CharacterAdapter>();
                    adapter?.StopWalkWithTransition();
                }
            }
        }

        /// <summary>
        /// 清理玩家阵型
        /// </summary>
        public void ClearPlayerFormation()
        {
            foreach (GameObject character in activePlayerCharacters)
            {
                if (character != null)
                {
                    // 停止DOTween動畫
                    character.transform.DOKill();
                    DestroyImmediate(character);
                }
            }
            activePlayerCharacters.Clear();
            // 只清理玩家血条 - 委托给 HealthBarUIManager 以保持映射一致
            if (HealthBarUIManager.Instance != null)
            {
                Debug.Log("HorizontalBattleFormationManager: Delegating ClearPlayerHealthBars to HealthBarUIManager");
                HealthBarUIManager.Instance.ClearPlayerHealthBars();
            }
            else
            {
                // 如果单例不存在，出于安全考虑不要执行回退的激进销毁逻辑，避免误删玩家血条
                Debug.LogWarning("HorizontalBattleFormationManager: HealthBarUIManager.Instance is null - skipping fallback ClearPlayerHealthBars to avoid accidental deletion of player health bars. Ensure HealthBarUIManager exists in the scene or is created earlier.");
                // 之前的回退方法 ClearPlayerHealthBars() 会基于 Slider 值等启发式判断销毁，易误删 —— 改为显式提示并跳过
                // ClearPlayerHealthBars(); // 已移除激进回退
            }
        }

        /// <summary>
        /// 清理敌人阵型
        /// </summary>
        public void ClearEnemyFormation()
        {
            foreach (GameObject enemy in activeEnemyCharacters)
            {
                if (enemy != null)
                {
                    var stats = enemy.GetComponent<CharacterStats>();
                    // 如果敌人还有血（异常场景），直接销毁；否则交由角色自身的3秒尸体消失逻辑处理
                    if (stats == null || stats.currentHitPoints > 0)
                    {
                        Destroy(enemy);
                    }
                    else
                    {
                        // 让死亡动画+3秒消失自行完成，这里不立即销毁
                        // 可选：取消父子关系以避免后续父节点动画或清理影响
                        try { enemy.transform.SetParent(null, true); } catch { }
                    }
                }
            }
            activeEnemyCharacters.Clear();

            // 清理敌人血条 - 委托给 HealthBarUIManager
            if (HealthBarUIManager.Instance != null)
            {
                Debug.Log("HorizontalBattleFormationManager: Delegating ClearEnemyHealthBars to HealthBarUIManager");
                HealthBarUIManager.Instance.ClearEnemyHealthBars();
            }
            else
            {
                Debug.LogWarning("HorizontalBattleFormationManager: HealthBarUIManager.Instance is null, falling back to direct destroy");
                ClearEnemyHealthBars();
            }

            Debug.Log("敌人阵型已清理");

            // 确保敌人生成点父节点回到原始位置，以防清理时父节点还在偏移位置
            if (enemySpawnParent != null)
            {
                enemySpawnParent.DOKill(); // 停止父节点的动画
            }
        }

        private void ClearPlayerHealthBars()
        {
            Debug.Log($"[HorizontalBattleFormationManager] (fallback) ClearPlayerHealthBars called. Stack:\n{System.Environment.StackTrace}");
            if (playerHealthBarContainer != null)
            {
                var toDestroy = new List<GameObject>();
                foreach (Transform child in playerHealthBarContainer)
                {
                    if (child == null) continue;
                    var uiBar = child.GetComponent<UI_HealthBar>();
                    if (uiBar == null)
                    {
                        Debug.LogWarning($"[HorizontalBattleFormationManager] (fallback) Child {child.name} does not have UI_HealthBar, skipping destroy to avoid accidental deletion.");
                        continue;
                    }

                    // bool destroy = false;
                    if (!child.gameObject.activeInHierarchy)
                    {
                        toDestroy.Add(child.gameObject);
                    }
                    else
                    {
                        var slider = child.GetComponentInChildren<UnityEngine.UI.Slider>(true);
                        if (slider == null)
                        {
                            Debug.LogWarning($"[HorizontalBattleFormationManager] (fallback) Child {child.name} has no Slider component, skipping destroy.");
                            continue;
                        }

                        if (slider.maxValue <= 1f && Mathf.Approximately(slider.value, 0f))
                        {
                            toDestroy.Add(child.gameObject);
                        }
                    }
                }

                foreach (var go in toDestroy)
                {
                    if (go != null) Destroy(go);
                }

                Debug.Log($"[HorizontalBattleFormationManager] (fallback) ClearPlayerHealthBars completed. destroyedCount={toDestroy.Count}");
            }
        }

        private void ClearEnemyHealthBars()
        {
            Debug.Log($"[HorizontalBattleFormationManager] (fallback) ClearEnemyHealthBars called. Stack:\n{System.Environment.StackTrace}");
            if (enemyHealthBarContainer != null)
            {
                var toDestroy = new List<GameObject>();
                foreach (Transform child in enemyHealthBarContainer)
                {
                    if (child == null) continue;
                    var uiBar = child.GetComponent<UI_HealthBar>();
                    if (uiBar == null)
                    {
                        Debug.LogWarning($"[HorizontalBattleFormationManager] (fallback) Child {child.name} does not have UI_HealthBar, skipping destroy.");
                        continue;
                    }

                    // bool destroy = false;
                    if (!child.gameObject.activeInHierarchy)
                    {
                        toDestroy.Add(child.gameObject);
                    }
                    else
                    {
                        var slider = child.GetComponentInChildren<UnityEngine.UI.Slider>(true);
                        if (slider == null)
                        {
                            Debug.LogWarning($"[HorizontalBattleFormationManager] (fallback) Child {child.name} has no Slider component, skipping destroy.");
                            continue;
                        }

                        if (slider.maxValue <= 1f && Mathf.Approximately(slider.value, 0f))
                        {
                            toDestroy.Add(child.gameObject);
                        }
                    }
                }

                foreach (var go in toDestroy)
                {
                    if (go != null) Destroy(go);
                }

                Debug.Log($"[HorizontalBattleFormationManager] (fallback) ClearEnemyHealthBars completed. destroyedCount={toDestroy.Count}");
            }
        }

        /// <summary>
        /// 获取指定阵营的前排角色
        /// </summary>
        public List<CharacterStats> GetFrontlineCharacters(BattleSide battleSide)
        {
            List<CharacterStats> frontline = new List<CharacterStats>();
            List<GameObject> targetList = (battleSide == BattleSide.Player) ? activePlayerCharacters : activeEnemyCharacters;

            // 前排是数组索引 0,1,2
            for (int i = 0; i < 3 && i < targetList.Count; i++)
            {
                if (targetList[i] != null)
                {
                    CharacterStats stats = targetList[i].GetComponent<CharacterStats>();
                    if (stats != null && stats.currentHitPoints > 0)
                    {
                        frontline.Add(stats);
                    }
                }
            }

            return frontline;
        }

        /// <summary>
        /// 获取指定阵营的后排角色
        /// </summary>
        public List<CharacterStats> GetBacklineCharacters(BattleSide battleSide)
        {
            List<CharacterStats> backline = new List<CharacterStats>();
            List<GameObject> targetList = (battleSide == BattleSide.Player) ? activePlayerCharacters : activeEnemyCharacters;

            // 后排是数组索引 3,4,5
            for (int i = 3; i < 6 && i < targetList.Count; i++)
            {
                if (targetList[i] != null)
                {
                    CharacterStats stats = targetList[i].GetComponent<CharacterStats>();
                    if (stats != null && stats.currentHitPoints > 0)
                    {
                        backline.Add(stats);
                    }
                }
            }

            return backline;
        }

        /// <summary>
        /// 判断角色是否为近战职业（基于位置判断）
        /// 直接通过SpawnPoints索引判断：[0-2]为前排近战，[3-5]为后排远程
        /// </summary>
        public bool IsMeleeClass(CharacterStats character)
        {
            if (character == null)
            {
                Debug.LogError("IsMeleeClass: 传入的character为null");
                return false;
            }

            List<GameObject> targetList = (character.battleSide == BattleSide.Player) ? activePlayerCharacters : activeEnemyCharacters;

            Debug.Log($"职业判断开始: {character.GetDisplayName()} - 阵营:{character.battleSide} - 列表角色数:{targetList.Count}");

            for (int i = 0; i < targetList.Count; i++)
            {
                if (targetList[i] != null && targetList[i].GetComponent<CharacterStats>() == character)
                {
                    // 前排位置（索引0,1,2）判断为近战，后排位置（索引3,4,5）判断为远程
                    bool isMelee = i < 3;
                    Debug.Log($"✅ 职业判断成功: {character.GetDisplayName()} - 阵型索引:{i} - 判定:{(isMelee ? "近战" : "远程")}职业");
                    return isMelee;
                }
            }

            // 如果未找到角色，输出详细调试信息
            Debug.LogError($"❌ 未找到角色 {character.GetDisplayName()} 在阵型列表中！");
            Debug.LogError($"当前 {character.battleSide} 阵营列表内容:");
            for (int i = 0; i < targetList.Count; i++)
            {
                if (targetList[i] != null)
                {
                    CharacterStats stats = targetList[i].GetComponent<CharacterStats>();
                    Debug.LogError($"  索引{i}: {targetList[i].name} - CharacterStats: {stats?.GetDisplayName()}");
                }
                else
                {
                    Debug.LogError($"  索引{i}: null");
                }
            }
            return false;
        }

        /// <summary>
        /// 判断角色是否为远程职业（基于位置判断）
        /// </summary>
        public bool IsRangedClass(CharacterStats character)
        {
            return !IsMeleeClass(character);
        }

        /// <summary>
        /// 获取角色在阵型中的索引位置（调试用）
        /// </summary>
        public int GetCharacterFormationIndex(CharacterStats character)
        {
            if (character == null) return -1;

            List<GameObject> targetList = (character.battleSide == BattleSide.Player) ? activePlayerCharacters : activeEnemyCharacters;

            for (int i = 0; i < targetList.Count; i++)
            {
                if (targetList[i] != null && targetList[i].GetComponent<CharacterStats>() == character)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 获取阵型调试信息
        /// </summary>
        private string GetFormationDebugInfo(List<GameObject> characterList)
        {
            string info = "";
            for (int i = 0; i < characterList.Count; i++)
            {
                if (characterList[i] != null)
                {
                    CharacterStats stats = characterList[i].GetComponent<CharacterStats>();
                    info += $"索引{i}: {characterList[i].name} - 血量: {stats?.currentHitPoints}\n";
                }
                else
                {
                    info += $"索引{i}: null\n";
                }
            }
            return info;
        }

        /// <summary>
        /// 为角色创建并关联血条
        /// </summary>
        private void CreateHealthBarForCharacter(CharacterStats characterStats)
        {
            if (healthBarPrefab == null || characterStats == null)
            {
                Debug.LogWarning("CreateHealthBarForCharacter: healthBarPrefab or characterStats is null");
                return;
            }

            Transform container = null;
            // 优先使用 HealthBarUIManager 中配置的容器，避免两个管理器使用不同的 Transform 导致血条被错误清理
            if (HealthBarUIManager.Instance != null)
            {
                container = characterStats.battleSide == BattleSide.Player ? HealthBarUIManager.Instance.playerHealthBarContainer : HealthBarUIManager.Instance.enemyHealthBarContainer;
            }

            // 回退到本地配置的容器（兼容历史 Inspector 配置）
            if (container == null)
            {
                container = characterStats.battleSide == BattleSide.Player ? playerHealthBarContainer : enemyHealthBarContainer;
            }

            if (container == null)
            {
                Debug.LogWarning($"CreateHealthBarForCharacter: HealthBar container for {characterStats.battleSide} is not assigned.");
                return;
            }

            GameObject healthBarGo = Instantiate(healthBarPrefab, container);
            healthBarGo.name = $"HealthBar_{characterStats.GetDisplayName()}";

            // 获取 UI_HealthBar 组件（这是我们统一的血条脚本）
            UI_HealthBar uiBar = healthBarGo.GetComponent<UI_HealthBar>();
            if (uiBar == null)
            {
                Debug.LogError($"CreateHealthBarForCharacter: healthBarPrefab '{healthBarPrefab.name}' 缺少 UI_HealthBar 组件，或使用了错误的血条预制体。");
                Destroy(healthBarGo);
                return;
            }

            // 初始化并注册到管理器
            try
            {
                uiBar.SetOwner(characterStats);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"CreateHealthBarForCharacter: SetOwner 异常 - {ex}");
            }

            if (HealthBarUIManager.Instance != null)
            {
                HealthBarUIManager.Instance.RegisterBar(characterStats, uiBar);
            }
            else
            {
                Debug.LogWarning("CreateHealthBarForCharacter: HealthBarUIManager.Instance 为 null，已直接创建血条但未注册到管理器。请检查场景中是否存在 HealthBarUIManager 单例。");
            }
        }

        /// <summary>
        /// 恢复玩家探索状态（从战斗返回时调用）
        /// 恢复行走动画并确保玩家血条存在
        /// </summary>
        public void RestorePlayerExplorationState()
        {
            // 恢复玩家为走路/探索动画状态
            SetPlayerFormationWalkingState();

            // 确保玩家相关的血条存在并已初始化
            var playerStats = new List<CharacterStats>();
            foreach (var go in activePlayerCharacters)
            {
                if (go != null)
                {
                    var stats = go.GetComponent<CharacterStats>();
                    if (stats != null) playerStats.Add(stats);
                }
            }

            if (HealthBarUIManager.Instance != null)
            {
                HealthBarUIManager.Instance.InitializeBars(playerStats);
                HealthBarUIManager.Instance.DumpStatus("RestorePlayerExplorationState");
            }
            else
            {
                Debug.LogWarning("RestorePlayerExplorationState: HealthBarUIManager.Instance 为 null，无法初始化玩家血条 UI。请确保场景中有该单例。");
            }

            Debug.Log("HorizontalBattleFormationManager: 玩家探索状态已恢复（行走动画 + 血条初始化）");
        }
    }
}
