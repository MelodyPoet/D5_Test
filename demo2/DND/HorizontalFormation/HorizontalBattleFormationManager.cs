using System.Collections.Generic;
using UnityEngine;
using DND5E;

/// <summary>
/// 横版战斗阵型管理器 - 专注于spawn点和阵型管理
/// 不再负责角色生成，只提供位置计算和排列服务
/// </summary>
public class HorizontalBattleFormationManager : MonoBehaviour {
    [Header("⚠️ 说明：BattleFormationManager专注于spawn点和阵型管理")]
    [Header("⚠️ 角色生成请使用CombatManager API或手动配置")]
    [Space(10)]

    [Header("自动位置计算")]
    [Tooltip("战场宽度 - 整个战场的总宽度")]
    public float battlefieldWidth = 20f;
    [Tooltip("战场深度 - 前排到后排的X轴距离，控制前后排的深度间隔")]
    public float battlefieldDepth = 4f;
    [Tooltip("角色间的间距 - 同一排内角色之间的Y轴距离")]
    public float positionSpacing = 2f;

    // 位置占用状态
    private Dictionary<HorizontalPosition, CharacterStats> positionOccupancy =
        new Dictionary<HorizontalPosition, CharacterStats>();

    // 自动生成的spawn点缓存
    private Transform[] generatedPlayerSpawnPoints = new Transform[6];
    private Transform[] generatedEnemySpawnPoints = new Transform[6];    // 单例
    public static HorizontalBattleFormationManager Instance { get; private set; }

    void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
        InitializePositions();

        // 自动生成spawn点
        AutoGenerateSpawnPoints();
    }

    /// <summary>
    /// 初始化所有战斗位置
    /// </summary>
    private void InitializePositions() {
        // 清空位置占用状态
        positionOccupancy.Clear();

        // 初始化所有位置为空
        for (int i = 0; i < 12; i++) {
            HorizontalPosition pos = (HorizontalPosition)i;
            positionOccupancy[pos] = null;
        }
    }

    /// <summary>
    /// 初始化战斗 - 仅负责spawn点生成，不再负责角色生成
    /// 角色生成请直接调用CombatManager API或手动配置
    /// </summary>
    public void InitializeBattle() {
        Debug.Log("🏗️ BattleFormationManager初始化 - 专注于spawn点管理");
        Debug.Log("⚠️ 注意：角色生成请使用CombatManager.StartExplorationToCombatSequence()或手动配置");

        // 只负责生成spawn点，不生成角色
        // 角色生成由CombatManager或其他系统负责
    }

    /// <summary>
    /// 使用现有角色列表初始化战斗（兼容旧系统）
    /// </summary>
    public void InitializeBattleWithExistingCharacters(List<CharacterStats> playerTeam, List<CharacterStats> enemyTeam) {
        // 检查角色队伍参数
        if (playerTeam == null) {
            Debug.LogError("❌ 玩家队伍参数为null！");
            return;
        }

        if (enemyTeam == null) {
            Debug.LogError("❌ 敌人队伍参数为null！");
            return;
        }

        // 清空当前位置占用状态
        InitializePositions();

        // 排列玩家队伍
        ArrangeExistingTeam(playerTeam, BattleSide.Player);

        // 排列敌人队伍
        ArrangeExistingTeam(enemyTeam, BattleSide.Enemy);

        Debug.Log($"使用现有角色初始化战斗完成 - 玩家队伍: {playerTeam.Count}人, 敌人队伍: {enemyTeam.Count}人");
    }

    /// <summary>
    /// 排列现有角色队伍到战斗位置
    /// </summary>
    /// <param name="team">要排列的角色队伍</param>
    /// <param name="side">所属阵营</param>
    public void ArrangeExistingTeam(List<CharacterStats> team, BattleSide side) {
        if (team == null || team.Count == 0) {
            Debug.LogWarning($"队伍为空，无法排列 {side} 阵营");
            return;
        }

        // 根据队伍大小确定阵型
        FormationType formation = DetermineFormation(team.Count);

        // 获取该阵营的位置列表
        HorizontalPosition[] positions = GetPositionsForSide(side, formation);

        // 将角色放置到相应位置
        for (int i = 0; i < team.Count && i < positions.Length; i++) {
            if (team[i] != null) {
                PlaceCharacterAtPosition(team[i], positions[i]);
            }
        }
    }

    /// <summary>
    /// 将角色放置到指定的战斗位置
    /// </summary>
    public void PlaceCharacterAtPosition(CharacterStats character, HorizontalPosition position) {
        if (character == null) {
            Debug.LogWarning("尝试放置空角色");
            return;
        }

        // 检查位置是否被占用
        if (IsPositionOccupied(position)) {
            Debug.LogWarning($"位置 {position} 已被占用，移除原角色");
        }

        // 从原位置移除角色
        RemoveCharacterFromFormation(character);

        // 占用新位置
        positionOccupancy[position] = character;

        // 记录角色的战斗位置
        BattlePositionComponent positionComponent = character.GetComponent<BattlePositionComponent>();
        if (positionComponent != null) {
            positionComponent.currentPosition = position;
        }

        // 移动角色到实际位置
        MoveCharacterToTransform(character, GetPositionTransform(position));

        Debug.Log($"角色 {character.characterName} 放置到位置 {position}");
    }

    /// <summary>
    /// 从阵型中移除角色
    /// </summary>
    public void RemoveCharacterFromFormation(CharacterStats character) {
        if (character == null) return;

        // 查找并清空角色占用的位置
        foreach (System.Collections.Generic.KeyValuePair<HorizontalPosition, CharacterStats> kvp in positionOccupancy) {
            if (kvp.Value == character) {
                positionOccupancy[kvp.Key] = null;
                break;
            }
        }
    }

    /// <summary>
    /// 检查位置是否被占用
    /// </summary>
    public bool IsPositionOccupied(HorizontalPosition position) {
        return positionOccupancy.ContainsKey(position) && positionOccupancy[position] != null;
    }

    /// <summary>
    /// 获取位置上的角色
    /// </summary>
    public CharacterStats GetCharacterAtPosition(HorizontalPosition position) {
        return positionOccupancy.ContainsKey(position) ? positionOccupancy[position] : null;
    }

    /// <summary>
    /// 根据队伍大小确定阵型
    /// </summary>
    private FormationType DetermineFormation(int teamSize) {
        switch (teamSize) {
            case 1:
            case 2:
                return FormationType.Defensive; // 小队伍采用防御阵型
            case 3:
            case 4:
                return FormationType.Balanced; // 中等队伍采用平衡阵型
            case 5:
            case 6:
            default:
                return FormationType.Aggressive; // 大队伍采用攻击阵型
        }
    }

    /// <summary>
    /// 获取指定阵营和阵型的位置列表
    /// </summary>
    private HorizontalPosition[] GetPositionsForSide(BattleSide side, FormationType formation) {
        if (side == BattleSide.Player) {
            // 玩家方位置（左侧）
            switch (formation) {
                case FormationType.Defensive:
                    return new HorizontalPosition[] {
                        HorizontalPosition.PlayerFrontLeft,
                        HorizontalPosition.PlayerBackLeft,
                        HorizontalPosition.PlayerBackCenter,
                        HorizontalPosition.PlayerBackRight
                    };
                case FormationType.Balanced:
                    return new HorizontalPosition[] {
                        HorizontalPosition.PlayerFrontLeft,
                        HorizontalPosition.PlayerFrontCenter,
                        HorizontalPosition.PlayerBackLeft,
                        HorizontalPosition.PlayerBackCenter,
                        HorizontalPosition.PlayerBackRight
                    };
                case FormationType.Aggressive:
                default:
                    return new HorizontalPosition[] {
                        HorizontalPosition.PlayerFrontLeft,
                        HorizontalPosition.PlayerFrontCenter,
                        HorizontalPosition.PlayerFrontRight,
                        HorizontalPosition.PlayerBackLeft,
                        HorizontalPosition.PlayerBackCenter,
                        HorizontalPosition.PlayerBackRight
                    };
            }
        }
        else {
            // 敌人方位置（右侧）
            switch (formation) {
                case FormationType.Defensive:
                    return new HorizontalPosition[] {
                        HorizontalPosition.EnemyFrontLeft,
                        HorizontalPosition.EnemyBackLeft,
                        HorizontalPosition.EnemyBackCenter,
                        HorizontalPosition.EnemyBackRight
                    };
                case FormationType.Balanced:
                    return new HorizontalPosition[] {
                        HorizontalPosition.EnemyFrontLeft,
                        HorizontalPosition.EnemyFrontCenter,
                        HorizontalPosition.EnemyBackLeft,
                        HorizontalPosition.EnemyBackCenter,
                        HorizontalPosition.EnemyBackRight
                    };
                case FormationType.Aggressive:
                default:
                    return new HorizontalPosition[] {
                        HorizontalPosition.EnemyFrontLeft,
                        HorizontalPosition.EnemyFrontCenter,
                        HorizontalPosition.EnemyFrontRight,
                        HorizontalPosition.EnemyBackLeft,
                        HorizontalPosition.EnemyBackCenter,
                        HorizontalPosition.EnemyBackRight
                    };
            }
        }
    }

    /// <summary>
    /// 获取位置对应的Transform
    /// </summary>
    public Transform GetPositionTransform(HorizontalPosition position) {
        // 首先尝试从自动生成的spawn点获取
        if (position <= HorizontalPosition.PlayerBackRight) {
            // 玩家方位置
            int index = (int)position;
            if (generatedPlayerSpawnPoints != null && index < generatedPlayerSpawnPoints.Length && generatedPlayerSpawnPoints[index] != null) {
                return generatedPlayerSpawnPoints[index];
            }
        }
        else {
            // 敌人方位置
            int enemyIndex = (int)position - 6; // 敌人位置从6开始
            if (generatedEnemySpawnPoints != null && enemyIndex < generatedEnemySpawnPoints.Length && generatedEnemySpawnPoints[enemyIndex] != null) {
                return generatedEnemySpawnPoints[enemyIndex];
            }
        }

        // 如果没有预设spawn点，创建虚拟Transform
        return CreateVirtualTransform(position);
    }

    /// <summary>
    /// 创建虚拟Transform用于位置计算
    /// </summary>
    private Transform CreateVirtualTransform(HorizontalPosition position) {
        GameObject virtualObj = new GameObject($"VirtualPosition_{position}");
        virtualObj.transform.SetParent(this.transform);
        virtualObj.transform.position = CalculatePosition(position);

        // 添加标记组件便于识别
        virtualObj.AddComponent<BattlePositionComponent>().currentPosition = position;

        return virtualObj.transform;
    }

    /// <summary>
    /// 根据位置枚举计算实际世界坐标
    /// </summary>
    private Vector3 CalculatePosition(HorizontalPosition position) {
        switch (position) {
            // 玩家方位置（左侧，X为负值）
            case HorizontalPosition.PlayerFrontLeft:
                return new Vector3(-2f, -positionSpacing, 0f);
            case HorizontalPosition.PlayerFrontCenter:
                return new Vector3(-3.5f, -positionSpacing * 2, 0f);
            case HorizontalPosition.PlayerFrontRight:
                return new Vector3(-2f, 0f, 0f);
            case HorizontalPosition.PlayerBackLeft:
                return new Vector3(-5f, -positionSpacing, 0f);
            case HorizontalPosition.PlayerBackCenter:
                return new Vector3(-5f, -positionSpacing * 2, 0f);
            case HorizontalPosition.PlayerBackRight:
                return new Vector3(-5f, 0f, 0f);

            // 敌人方位置（右侧，X为正值）
            case HorizontalPosition.EnemyFrontLeft:
                return new Vector3(2f, -positionSpacing, 0f);
            case HorizontalPosition.EnemyFrontCenter:
                return new Vector3(3.5f, -positionSpacing * 2, 0f);
            case HorizontalPosition.EnemyFrontRight:
                return new Vector3(2f, 0f, 0f);
            case HorizontalPosition.EnemyBackLeft:
                return new Vector3(5f, -positionSpacing, 0f);
            case HorizontalPosition.EnemyBackCenter:
                return new Vector3(5f, -positionSpacing * 2, 0f);
            case HorizontalPosition.EnemyBackRight:
                return new Vector3(5f, 0f, 0f);

            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    /// 自动生成所有spawn点
    /// </summary>
    private void AutoGenerateSpawnPoints() {
        // 清理现有spawn点
        ClearExistingSpawnPoints();

        // 计算战场中心
        Vector3 battlefieldCenter = CalculateBattlefieldCenter();

        // 生成玩家spawn点
        GeneratePlayerSpawnPoints(battlefieldCenter);

        // 生成敌人spawn点
        GenerateEnemySpawnPoints(battlefieldCenter);
    }

    /// <summary>
    /// 获取玩家spawn点数组
    /// </summary>
    public Transform[] GetPlayerSpawnPoints() {
        return generatedPlayerSpawnPoints;
    }

    /// <summary>
    /// 获取敌人spawn点数组
    /// </summary>
    public Transform[] GetEnemySpawnPoints() {
        return generatedEnemySpawnPoints;
    }

    /// <summary>
    /// 计算战场中心位置
    /// </summary>
    private Vector3 CalculateBattlefieldCenter() {
        // 简单返回世界原点作为战场中心
        return Vector3.zero;
    }

    /// <summary>
    /// 生成玩家spawn点
    /// </summary>
    private void GeneratePlayerSpawnPoints(Vector3 center) {
        // 初始化数组
        generatedPlayerSpawnPoints = new Transform[6];

        // 计算玩家方的X坐标（左侧）
        float playerFrontX = center.x - battlefieldDepth / 4f; // 前排稍微向前
        float playerBackX = center.x - battlefieldDepth / 2f;  // 后排更靠后

        // 定义玩家spawn点位置（楔形阵，Y≤0，前排突出）
        Vector3[] playerPositions = new Vector3[] {
            new Vector3(playerFrontX, center.y, 0), // 前排左翼 (Y=0，最高点)
            new Vector3(playerFrontX + 1.5f, center.y - positionSpacing, 0), // 前排中锋（向敌人突出1.5单位，X=-2）
            new Vector3(playerFrontX, center.y - positionSpacing * 2, 0), // 前排右翼 (Y最小)

            new Vector3(playerBackX, center.y, 0), // 后排左翼 (Y=0，最高点)
            new Vector3(playerBackX, center.y - positionSpacing, 0), // 后排中路
            new Vector3(playerBackX, center.y - positionSpacing * 2, 0), // 后排右翼 (Y最小)
        };

        // 创建spawn点GameObject
        for (int i = 0; i < playerPositions.Length; i++) {
            GameObject spawnPoint = new GameObject($"PlayerSpawn_{i}");
            spawnPoint.transform.SetParent(this.transform);
            spawnPoint.transform.position = playerPositions[i];            // 添加标记组件
            BattlePositionComponent posComp = spawnPoint.AddComponent<BattlePositionComponent>();
            posComp.currentPosition = (HorizontalPosition)i;
            // posComp.side = BattleSide.Player; // 如果BattlePositionComponent没有side属性，则注释掉

            generatedPlayerSpawnPoints[i] = spawnPoint.transform;
        }
    }

    /// <summary>
    /// 生成敌人spawn点
    /// </summary>
    private void GenerateEnemySpawnPoints(Vector3 center) {
        // 初始化数组
        generatedEnemySpawnPoints = new Transform[6];

        // 计算敌人方的X坐标（右侧）
        float enemyFrontX = center.x + battlefieldDepth / 4f; // 前排稍微向前
        float enemyBackX = center.x + battlefieldDepth / 2f;  // 后排更靠后

        // 定义敌人spawn点位置（楔形阵，Y≤0，前排突出）
        Vector3[] enemyPositions = new Vector3[] {
            new Vector3(enemyFrontX, center.y, 0), // 前排左翼 (Y=0，最高点)
            new Vector3(enemyFrontX - 1.5f, center.y - positionSpacing, 0), // 前排中锋（向玩家突出1.5单位，X=2）
            new Vector3(enemyFrontX, center.y - positionSpacing * 2, 0), // 前排右翼 (Y最小)

            new Vector3(enemyBackX, center.y, 0), // 后排左翼 (Y=0，最高点)
            new Vector3(enemyBackX, center.y - positionSpacing, 0), // 后排中路
            new Vector3(enemyBackX, center.y - positionSpacing * 2, 0), // 后排右翼 (Y最小)
        };

        // 创建spawn点GameObject
        for (int i = 0; i < enemyPositions.Length; i++) {
            GameObject spawnPoint = new GameObject($"EnemySpawn_{i}");
            spawnPoint.transform.SetParent(this.transform);
            spawnPoint.transform.position = enemyPositions[i];            // 添加标记组件
            BattlePositionComponent posComp = spawnPoint.AddComponent<BattlePositionComponent>();
            posComp.currentPosition = (HorizontalPosition)(i + 6); // 敌人位置从6开始
            // posComp.side = BattleSide.Enemy; // 如果BattlePositionComponent没有side属性，则注释掉

            generatedEnemySpawnPoints[i] = spawnPoint.transform;
        }
    }

    /// <summary>
    /// 将角色移动到指定Transform位置
    /// </summary>
    private void MoveCharacterToTransform(CharacterStats character, Transform targetTransform) {
        if (character != null && targetTransform != null) {
            character.transform.position = targetTransform.position;
        }
    }

    /// <summary>
    /// 清理现有的spawn点
    /// </summary>
    private void ClearExistingSpawnPoints() {
        // 清理玩家spawn点
        if (generatedPlayerSpawnPoints != null) {
            foreach (Transform spawnPoint in generatedPlayerSpawnPoints) {
                if (spawnPoint != null) {
                    DestroyImmediate(spawnPoint.gameObject);
                }
            }
        }

        // 清理敌人spawn点
        if (generatedEnemySpawnPoints != null) {
            foreach (Transform spawnPoint in generatedEnemySpawnPoints) {
                if (spawnPoint != null) {
                    DestroyImmediate(spawnPoint.gameObject);
                }
            }
        }

        // 重新初始化数组
        generatedPlayerSpawnPoints = new Transform[6];
        generatedEnemySpawnPoints = new Transform[6];
    }
}
