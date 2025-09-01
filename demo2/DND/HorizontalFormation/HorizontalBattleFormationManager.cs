using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

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

        // 按阵型顺序生成角色
        InstantiateCharacterAtPosition(玩家前排左翼, playerSpawnPoints[0], BattleSide.Player);
        InstantiateCharacterAtPosition(玩家前排中锋, playerSpawnPoints[1], BattleSide.Player);
        InstantiateCharacterAtPosition(玩家前排右翼, playerSpawnPoints[2], BattleSide.Player);
        InstantiateCharacterAtPosition(玩家后排左翼, playerSpawnPoints[3], BattleSide.Player);
        InstantiateCharacterAtPosition(玩家后排中路, playerSpawnPoints[4], BattleSide.Player);
        InstantiateCharacterAtPosition(玩家后排右翼, playerSpawnPoints[5], BattleSide.Player);

        SetFormationAnimationState(activePlayerCharacters, true);
    }

    /// <summary>
    /// 生成敌人阵型
    /// </summary>
    public void GenerateEnemyFormation()
    {
        ClearEnemyFormation();

        if (enemySpawnPoints.Length < 6)
        {
            Debug.LogError("敌人spawn点配置不足！需要6个位置点");
            return;
        }

        // 按阵型顺序生成角色
        InstantiateCharacterAtPosition(敌人前排左翼, enemySpawnPoints[0], BattleSide.Enemy);
        InstantiateCharacterAtPosition(敌人前排中锋, enemySpawnPoints[1], BattleSide.Enemy);
        InstantiateCharacterAtPosition(敌人前排右翼, enemySpawnPoints[2], BattleSide.Enemy);
        InstantiateCharacterAtPosition(敌人后排左翼, enemySpawnPoints[3], BattleSide.Enemy);
        InstantiateCharacterAtPosition(敌人后排中路, enemySpawnPoints[4], BattleSide.Enemy);
        InstantiateCharacterAtPosition(敌人后排右翼, enemySpawnPoints[5], BattleSide.Enemy);

        SetFormationAnimationState(activeEnemyCharacters, false);
    }

    /// <summary>
    /// 在指定位置实例化角色
    /// </summary>
    private void InstantiateCharacterAtPosition(GameObject prefab, Transform spawnPoint, BattleSide battleSide)
    {
        if (prefab == null || spawnPoint == null) return;

        GameObject instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

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

        // 添加到对应列表
        if (battleSide == BattleSide.Player)
        {
            activePlayerCharacters.Add(instance);
        }
        else
        {
            activeEnemyCharacters.Add(instance);
        }
    }

    /// <summary>
    /// 设置阵型动画状态
    /// </summary>
    private void SetFormationAnimationState(List<GameObject> characters, bool isWalking)
    {
        foreach (GameObject character in characters)
        {
            DND_CharacterAdapter adapter = character.GetComponent<DND_CharacterAdapter>();
            if (adapter != null)
            {
                if (isWalking)
                {
                    adapter.PlayWalkAnimation();
                }
                else
                {
                    adapter.PlayIdleAnimation();
                }
            }
        }
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
    /// </summary>
    public bool IsMeleeClass(CharacterStats character)
    {
        if (character == null) return false;

        List<GameObject> targetList = (character.battleSide == BattleSide.Player) ? activePlayerCharacters : activeEnemyCharacters;

        for (int i = 0; i < targetList.Count; i++)
        {
            if (targetList[i] != null && targetList[i].GetComponent<CharacterStats>() == character)
            {
                // 前排位置（索引0,1,2）判断为近战
                return i < 3;
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
}
