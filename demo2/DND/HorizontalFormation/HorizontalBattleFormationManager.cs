using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using DG.Tweening;
using System.Collections;

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

        // 运行时数据
        private List<GameObject> activePlayerCharacters = new List<GameObject>();
        private List<GameObject> activeEnemyCharacters = new List<GameObject>();

        /// <summary>
        /// 生成玩家阵型
        /// </summary>
        public void GeneratePlayerFormation()
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

            // 确保列表有6个位置，即使某些预制体为null
            activePlayerCharacters.Clear();
            for (int i = 0; i < 6; i++)
            {
                activePlayerCharacters.Add(null); // 先用null占位
            }

            // 按阵型顺序生成角色，使用容器获取预制体
            for (int i = 0; i < 6; i++)
            {
                GameObject prefab = formationContainer.GetPlayerPrefab(i);
                InstantiatePlayerCharacterAtIndex(prefab, playerSpawnPoints[i], BattleSide.Player, i);
            }

            // 玩家角色立即播放走路动画（探索状态）
            SetPlayerFormationWalkingState();

            Debug.Log($"玩家阵型完成，列表状态: {GetFormationDebugInfo(activePlayerCharacters)}");
        }

        /// <summary>
        /// 生成敌人阵型（带整体进场动画）
        /// 使用DOTween事件驱动，摒弃协程
        /// </summary>
        public void GenerateEnemyFormation()
        {
            ClearEnemyFormation();

            if (formationContainer == null)
            {
                Debug.LogError("阵型容器未配置！请在HorizontalBattleFormationManager中设置FormationContainer");
                return;
            }

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

            // 确保列表有6个位置，即使某些预制体为null
            activeEnemyCharacters.Clear();
            for (int i = 0; i < 6; i++)
            {
                activeEnemyCharacters.Add(null); // 先用null占位
            }

            // 记录父节点的原始位置
            Vector3 originalParentPosition = enemySpawnParent.position;

            // 将整个敌人生成点父节点移动到屏幕右侧远端
            enemySpawnParent.position = originalParentPosition + Vector3.right * enemyEntranceOffset;

            // 在当前位置（已偏移的spawn点）生成所有敌人角色
            for (int i = 0; i < 6; i++)
            {
                GameObject prefab = formationContainer.GetEnemyPrefab(i);
                InstantiateEnemyCharacterAtCurrentPosition(prefab, enemySpawnPoints[i], BattleSide.Enemy, i);
            }

            // 使用DOTween执行整体进场动画
            ExecuteFormationEntranceAnimationDOTween(originalParentPosition);

            Debug.Log($"敌人阵型生成完成，整体进场动画开始，列表状态: {GetFormationDebugInfo(activeEnemyCharacters)}");
        }

        /// <summary>
        /// 执行整个阵型的进场动画 - DOTween版本
        /// </summary>
        private void ExecuteFormationEntranceAnimationDOTween(Vector3 targetParentPosition)
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
            positionComponent.currentPosition = GetPlayerPositionByIndex(index);
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
                skeletonAnimation.skeleton.ScaleX = -Mathf.Abs(skeletonAnimation.skeleton.ScaleX);
                Debug.Log($"敌人 {instance.name} 使用Spine ScaleX设置朝向：{skeletonAnimation.skeleton.ScaleX}");
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
            positionComponent.currentPosition = GetEnemyPositionByIndex(index);
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
                    // 停止DOTween动画
                    character.transform.DOKill();
                    DestroyImmediate(character);
                }
            }
            activePlayerCharacters.Clear();
            // 只清理玩家血条
            ClearPlayerHealthBars();
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
                    Destroy(enemy);
                }
            }
            activeEnemyCharacters.Clear();

            // 清理敌人血条
            ClearEnemyHealthBars();

            Debug.Log("敌人阵型已清理");

            // 确保敌人生成点父节点回到原始位置，以防清理时父节点还在偏移位置
            if (enemySpawnParent != null)
            {
                enemySpawnParent.DOKill(); // 停止父节点的动画
            }
        }

        /// <summary>
        /// 恢复玩家探索状态
        /// </summary>
        public void RestorePlayerExplorationState()
        {
            // 恢复玩家角色行走动画
            foreach (GameObject player in activePlayerCharacters)
            {
                if (player != null)
                {
                    DND_CharacterAdapter adapter = player.GetComponent<DND_CharacterAdapter>();
                    adapter?.PlayWalkAnimation();
                }
            }

            Debug.Log("玩家阵型已恢复探索状态");
        }

        /// <summary>
        /// 为指定角色创建并初始化血条UI
        /// </summary>
        private void CreateHealthBarForCharacter(CharacterStats characterStats)
        {
            if (healthBarPrefab == null || characterStats == null)
            {
                Debug.LogWarning("HealthBar Prefab or CharacterStats is null.");
                return;
            }

            Transform container = characterStats.battleSide == BattleSide.Player ? playerHealthBarContainer : enemyHealthBarContainer;
            if (container == null)
            {
                Debug.LogWarning($"HealthBar Container for {characterStats.battleSide} is not set in HorizontalBattleFormationManager.");
                return;
            }

            GameObject healthBarGo = Instantiate(healthBarPrefab, container);
            UI_HealthBar healthBar = healthBarGo.GetComponent<UI_HealthBar>();
            if (healthBar == null)
            {
                Debug.LogError("HealthBar Prefab does not have a UI_HealthBar component.");
                Destroy(healthBarGo);
                return;
            }

            healthBar.Initialize(characterStats);
        }

        private void ClearPlayerHealthBars()
        {
            if (playerHealthBarContainer != null)
            {
                foreach (Transform child in playerHealthBarContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void ClearEnemyHealthBars()
        {
            if (enemyHealthBarContainer != null)
            {
                foreach (Transform child in enemyHealthBarContainer)
                {
                    Destroy(child.gameObject);
                }
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
                    info += $"[{i}]:{stats?.GetDisplayName()} ";
                }
                else
                {
                    info += $"[{i}]:null ";
                }
            }
            return info;
        }

        /// <summary>
        /// 根据索引获取玩家位置枚举
        /// </summary>
        private HorizontalPosition GetPlayerPositionByIndex(int index)
        {
            switch (index)
            {
                case 0: return HorizontalPosition.PlayerFrontLeft;
                case 1: return HorizontalPosition.PlayerFrontCenter;
                case 2: return HorizontalPosition.PlayerFrontRight;
                case 3: return HorizontalPosition.PlayerBackLeft;
                case 4: return HorizontalPosition.PlayerBackCenter;
                case 5: return HorizontalPosition.PlayerBackRight;
                default:
                    Debug.LogError($"无效的玩家索引: {index}");
                    return HorizontalPosition.PlayerFrontCenter;
            }
        }

        /// <summary>
        /// 根据索引获取敌人位置枚举
        /// </summary>
        private HorizontalPosition GetEnemyPositionByIndex(int index)
        {
            switch (index)
            {
                case 0: return HorizontalPosition.EnemyFrontLeft;
                case 1: return HorizontalPosition.EnemyFrontCenter;
                case 2: return HorizontalPosition.EnemyFrontRight;
                case 3: return HorizontalPosition.EnemyBackLeft;
                case 4: return HorizontalPosition.EnemyBackCenter;
                case 5: return HorizontalPosition.EnemyBackRight;
                default:
                    Debug.LogError($"无效的敌人索引: {index}");
                    return HorizontalPosition.EnemyFrontCenter;
            }
        }
    }
}
