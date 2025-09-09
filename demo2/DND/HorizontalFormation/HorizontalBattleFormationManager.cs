using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 横版战斗阵型管理器 - 负责阵型配置和位置管理
/// 提供清晰的中文标识阵型配置面板
/// </summary>
public class HorizontalBattleFormationManager : MonoBehaviour {
    [Header("玩家阵型配置 (左侧)")]
    [Space(5)]

    [Header("前排")]
    [Tooltip("玩家前排左翼角色预制体")]
    public GameObject 玩家前排左翼;
    [Tooltip("玩家前排中锋角色预制体")]
    public GameObject 玩家前排中锋;
    [Tooltip("玩家前排右翼角色预制体")]
    public GameObject 玩家前排右翼;

    [Header("后排")]
    [Tooltip("玩家后排左翼角色预制体")]
    public GameObject 玩家后排左翼;
    [Tooltip("玩家后排中路角色预制体")]
    public GameObject 玩家后排中路;
    [Tooltip("玩家后排右翼角色预制体")]
    public GameObject 玩家后排右翼;

    [Space(15)]
    [Header("敌人阵型配置 (右侧)")]
    [Space(5)]

    [Header("前排")]
    [Tooltip("敌人前排左翼角色预制体")]
    public GameObject 敌人前排左翼;
    [Tooltip("敌人前排中锋角色预制体")]
    public GameObject 敌人前排中锋;
    [Tooltip("敌人前排右翼角色预制体")]
    public GameObject 敌人前排右翼;

    [Header("后排")]
    [Tooltip("敌人后排左翼角色预制体")]
    public GameObject 敌人后排左翼;
    [Tooltip("敌人后排中路角色预制体")]
    public GameObject 敌人后排中路;
    [Tooltip("敌人后排右翼角色预制体")]
    public GameObject 敌人后排右翼;

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

    // 运行时数据
    private List<GameObject> activePlayerCharacters = new List<GameObject>();
    private List<GameObject> activeEnemyCharacters = new List<GameObject>();

    /// <summary>
    /// 生成玩家阵型
    /// </summary>
    public void GeneratePlayerFormation()
    {
        ClearPlayerFormation();

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

        // 按阵型顺序生成角色，保持索引对应关系
        InstantiatePlayerCharacterAtIndex(玩家前排左翼, playerSpawnPoints[0], BattleSide.Player, 0);
        InstantiatePlayerCharacterAtIndex(玩家前排中锋, playerSpawnPoints[1], BattleSide.Player, 1);
        InstantiatePlayerCharacterAtIndex(玩家前排右翼, playerSpawnPoints[2], BattleSide.Player, 2);
        InstantiatePlayerCharacterAtIndex(玩家后排左翼, playerSpawnPoints[3], BattleSide.Player, 3);
        InstantiatePlayerCharacterAtIndex(玩家后排中路, playerSpawnPoints[4], BattleSide.Player, 4);
        InstantiatePlayerCharacterAtIndex(玩家后排右翼, playerSpawnPoints[5], BattleSide.Player, 5);

        // 玩家角色立即播放走路动画（探索状态）
        SetPlayerFormationWalkingState();

        Debug.Log($"玩家阵型生成完成，列表状态: {GetFormationDebugInfo(activePlayerCharacters)}");
    }

    /// <summary>
    /// 生成敌人阵型（带进场动画）
    /// </summary>
    public void GenerateEnemyFormation()
    {
        ClearEnemyFormation();

        if (enemySpawnPoints.Length < 6)
        {
            Debug.LogError("敌人spawn点配置不足！需要6个位置点");
            return;
        }

        // 确保列表有6个位置，即使某些预制体为null
        activeEnemyCharacters.Clear();
        for (int i = 0; i < 6; i++)
        {
            activeEnemyCharacters.Add(null); // 先用null占位
        }

        // 按阵型顺序生成角色，保持索引对应关系
        InstantiateEnemyCharacterAtIndex(敌人前排左翼, enemySpawnPoints[0], BattleSide.Enemy, 0, 0f);
        InstantiateEnemyCharacterAtIndex(敌人前排中锋, enemySpawnPoints[1], BattleSide.Enemy, 1, 0.1f);
        InstantiateEnemyCharacterAtIndex(敌人前排右翼, enemySpawnPoints[2], BattleSide.Enemy, 2, 0.2f);
        InstantiateEnemyCharacterAtIndex(敌人后排左翼, enemySpawnPoints[3], BattleSide.Enemy, 3, 0.8f);
        InstantiateEnemyCharacterAtIndex(敌人后排中路, enemySpawnPoints[4], BattleSide.Enemy, 4, 0.9f);
        InstantiateEnemyCharacterAtIndex(敌人后排右翼, enemySpawnPoints[5], BattleSide.Enemy, 5, 1.0f);

        Debug.Log($"敌人阵型生成完成，列表状态: {GetFormationDebugInfo(activeEnemyCharacters)}");
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
        }
        else
        {
            Debug.LogError($"角色预制体 {prefab.name} 缺少 CharacterStats 组件！");
            DestroyImmediate(instance);
            return;
        }

        // 添加到指定索引位置
        activePlayerCharacters[index] = instance;

        Debug.Log($"玩家角色 {prefab.name} 已生成在索引{index}，缩放值: {instance.transform.localScale}");
    }

    /// <summary>
    /// 实例化敌人角色（指定索引位置，带进场动画）
    /// </summary>
    private void InstantiateEnemyCharacterAtIndex(GameObject prefab, Transform spawnPoint, BattleSide battleSide, int index, float entranceDelay)
    {
        if (prefab == null || spawnPoint == null)
        {
            Debug.LogWarning($"敌人角色索引{index}的预制体或spawn点为null，保持null占位");
            return; // 保持null占位，不影响其他角色的索引
        }

        // 记录预制体的原始缩放值
        Vector3 originalScale = prefab.transform.localScale;

        // 计算进场起始位置（屏幕右侧外）
        Vector3 entrancePosition = spawnPoint.position + Vector3.right * 5f;

        GameObject instance = Instantiate(prefab, entrancePosition, spawnPoint.rotation);

        // 保持原始缩放值（解决敌人后排缩放问题）
        instance.transform.localScale = originalScale;

        // 配置角色阵营
        CharacterStats stats = instance.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.battleSide = battleSide;
        }
        else
        {
            Debug.LogError($"角色预制体 {prefab.name} 缺少 CharacterStats 组件！");
            DestroyImmediate(instance);
            return;
        }

        // 获取动画适配器并设置敌人朝向
        DND_CharacterAdapter adapter = instance.GetComponent<DND_CharacterAdapter>();
        if (adapter != null)
        {
            // 修复敌人朝向：敌人应该面向左侧（玩家方向）
            SkeletonAnimation skeletonAnim = adapter.skeletonAnimation;
            if (skeletonAnim != null && skeletonAnim.skeleton != null)
            {
                skeletonAnim.skeleton.FlipX = true; // 敌人水平翻转，面向玩家
                Debug.Log($"{prefab.name} 敌人朝向已设置为面向玩家");
            }
            else
            {
                // 如果Spine组件还没初始化，延迟设置朝向
                StartCoroutine(SetEnemyDirectionDelayed(adapter));
            }

            // 立即播放走路动画
            adapter.PlayWalkAnimation();

            // 使用DOTween进场动画（解决飘移问题）
            Sequence entranceSequence = DOTween.Sequence();
            entranceSequence.AppendInterval(entranceDelay);
            entranceSequence.Append(instance.transform.DOMove(spawnPoint.position, 1.0f));
            entranceSequence.OnComplete(() => {
                // 进场完成后播放过渡动画
                adapter.StopWalkWithTransition();
            });
        }

        // 添加到指定索引位置
        activeEnemyCharacters[index] = instance;

        Debug.Log($"敌人角色 {prefab.name} 已生成在索引{index}并开始进场，缩放值: {instance.transform.localScale}，朝向: 面向玩家");
    }

    /// <summary>
    /// 延迟设置敌人朝向（当Spine组件还未完全初始化时）
    /// </summary>
    private IEnumerator SetEnemyDirectionDelayed(DND_CharacterAdapter adapter)
    {
        // 等待几帧让Spine组件完全初始化
        yield return new WaitForSeconds(0.1f);

        if (adapter != null && adapter.skeletonAnimation != null && adapter.skeletonAnimation.skeleton != null)
        {
            adapter.skeletonAnimation.skeleton.FlipX = true; // 敌人水平翻转，面向玩家
            Debug.Log($"{adapter.gameObject.name} 延迟设置敌人朝向完成");
        }
        else
        {
            Debug.LogWarning($"{adapter?.gameObject.name} 无法设置敌人朝向，Spine组件初始化失败");
        }
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
    /// 设置阵型动画状态（战斗模式）
    /// </summary>
    public void SetFormationBattleState()
    {
        // 玩家队伍切换到待机状态
        foreach (GameObject character in activePlayerCharacters)
        {
            if (character != null)
            {
                DND_CharacterAdapter adapter = character.GetComponent<DND_CharacterAdapter>();
                if (adapter != null)
                {
                    adapter.StopWalkWithTransition();
                }
            }
        }

        // 敌人队伍也切换到待机状态（如果已经进场完毕）
        foreach (GameObject character in activeEnemyCharacters)
        {
            if (character != null)
            {
                DND_CharacterAdapter adapter = character.GetComponent<DND_CharacterAdapter>();
                if (adapter != null && adapter.CurrentAnimation != "walk")
                {
                    adapter.PlayIdleAnimation();
                }
            }
        }

        Debug.Log("双方队伍进入战斗状态（待机动画）");
    }

    /// <summary>
    /// 恢复玩家队伍为探索状态
    /// </summary>
    public void RestorePlayerExplorationState()
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
        Debug.Log("玩家队伍恢复探索状态（走路动画）");
    }

    /// <summary>
    /// 清空玩家阵型
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
    }

    /// <summary>
    /// 清空敌人阵型
    /// </summary>
    public void ClearEnemyFormation()
    {
        foreach (GameObject character in activeEnemyCharacters)
        {
            if (character != null)
            {
                // 停止DOTween动画
                character.transform.DOKill();
                DestroyImmediate(character);
            }
        }
        activeEnemyCharacters.Clear();
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
    /// 获取指定阵营的所有存活角色
    /// </summary>
    public List<CharacterStats> GetAllAliveCharacters(BattleSide battleSide)
    {
        List<CharacterStats> aliveCharacters = new List<CharacterStats>();
        List<GameObject> targetList = (battleSide == BattleSide.Player) ? activePlayerCharacters : activeEnemyCharacters;

        foreach (GameObject character in targetList)
        {
            if (character != null)
            {
                CharacterStats stats = character.GetComponent<CharacterStats>();
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
    public bool HasAliveCharacters(BattleSide battleSide)
    {
        return GetAllAliveCharacters(battleSide).Count > 0;
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
}
