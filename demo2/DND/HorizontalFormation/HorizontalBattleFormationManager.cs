using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

/// <summary>
/// 横版战斗阵型管理器 - 负责阵型配置和位置管理
/// 提供清晰的中文标识阵型配置面板
/// </summary>
public class HorizontalBattleFormationManager : MonoBehaviour {
    [Header("🔵 玩家阵型配置 (左侧)")]
    [Space(5)]

    [Header("前排")]
    [Tooltip("玩家前排��翼角色预制体")]
    public GameObject 玩家前排左翼;
    [Tooltip("玩家前排中锋角色预制体")]
    public GameObject 玩家前排中锋;
    [Tooltip("玩家前排右翼角色预制体")]
    public GameObject 玩家前排右翼;

    [Header("后排")]
    [Tooltip("玩家后排左翼角色预制体")]
    public GameObject 玩家后排左翼;
    [Tooltip("玩��后排中路角色预制体")]
    public GameObject 玩家后排中路;
    [Tooltip("玩家后排右翼角色预制体")]
    public GameObject 玩家后排右翼;

    [Space(15)]
    [Header("🔴 敌人阵型配置 (右侧)")]
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
    [Header("⚙️ 阵型参数设置")]
    [Tooltip("战场宽度 - 整个战场的总宽度")]
    public float battlefieldWidth = 20f;
    [Tooltip("战场深度 - 前排到后排的X轴距离，控制前后排的深度间隔（当前使用固定位置，此参数为预留扩展用）")]
    public float battlefieldDepth = 4f;
    [Tooltip("角色间的间距 - 同一排内角色之间的Y轴距离")]
    public float positionSpacing = 2f;

    [Space(15)]
    [Header("🎯 Spawn点位置配置")]
    [Tooltip("手动配置每个阵型位置的spawn点Transform，必须全部配置才能正常工作")]
    [Space(5)]
    [Header("🔵 玩家Spawn点 (左侧)")]
    public Transform playerFrontLeftSpawn;
    [Tooltip("玩家前排中锋spawn点Transform")]
    public Transform playerFrontCenterSpawn;
    [Tooltip("玩家前排右翼spawn点Transform")]
    public Transform playerFrontRightSpawn;
    [Tooltip("玩家后排左翼spawn点Transform")]
    public Transform playerBackLeftSpawn;
    [Tooltip("玩家后排中路spawn点Transform")]
    public Transform playerBackCenterSpawn;
    [Tooltip("玩家后排右翼spawn点Transform")]
    public Transform playerBackRightSpawn;

    [Space(5)]
    [Header("🔴 敌人Spawn点 (右侧)")]
    [Tooltip("敌人前排左翼spawn点Transform")]
    public Transform enemyFrontLeftSpawn;
    [Tooltip("敌人前排中锋spawn点Transform")]
    public Transform enemyFrontCenterSpawn;
    [Tooltip("敌人前排右翼spawn点Transform")]
    public Transform enemyFrontRightSpawn;
    [Tooltip("敌人后排左翼spawn点Transform")]
    public Transform enemyBackLeftSpawn;
    [Tooltip("敌人后排中路spawn点Transform")]
    public Transform enemyBackCenterSpawn;
    [Tooltip("敌人后排右翼spawn点Transform")]
    public Transform enemyBackRightSpawn;

    // 位置占用状态
    private Dictionary<HorizontalPosition, CharacterStats> positionOccupancy =
        new Dictionary<HorizontalPosition, CharacterStats>();

    // 单例
    public static HorizontalBattleFormationManager Instance { get; private set; }

    void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
        InitializePositions();
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
    /// 角色生成请直接调用IdleGameManager API或手动配置
    /// </summary>
    public void InitializeBattle() {
        Debug.Log("BattleFormationManager初始化 - 专注于spawn点管理");
        Debug.Log("注意：角色生成请使用IdleGameManager.GenerateInitialTeams()或手动配置");

        // 只负责生成spawn点，不生成角色
        // 角色生成由IdleGameManager或其他挂机系统负责
    }

    /// <summary>
    /// 使用现有角色列表初始化战斗（兼容旧系统）
    /// </summary>
    public void InitializeBattleWithExistingCharacters(List<CharacterStats> playerTeam, List<CharacterStats> enemyTeam) {
        // 检查角色队伍参数
        if (playerTeam == null) {
            Debug.LogError("玩家队伍参数为null！");
            return;
        }

        if (enemyTeam == null) {
            Debug.LogError("敌人队伍参数为null！");
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

        // 记录角色��战斗位置
        BattlePositionComponent positionComponent = character.GetComponent<BattlePositionComponent>();
        if (positionComponent != null) {
            positionComponent.currentPosition = position;
        }

        // 移动角色到实际位置
        MoveCharacterToTransform(character, GetPositionTransform(position));

        Debug.Log($"角色 {character.characterName} 放置到位置 {position}");
    }

    /// <summary>
    /// 将角色移动到指定Transform位置
    /// 🎯 只设置position，保持预制体原有的scale和rotation不被修改
    /// </summary>
    private void MoveCharacterToTransform(CharacterStats character, Transform targetTransform) {
        if (character != null && targetTransform != null) {
            // 🎯 只设置位置，不修改缩放和旋转
            // 保持预制体手动配置的scale和rotation值
            Vector3 originalScale = character.transform.localScale;
            Quaternion originalRotation = character.transform.rotation;

            character.transform.position = targetTransform.position;

            // 确保缩放和旋转保持不变
            character.transform.localScale = originalScale;
            character.transform.rotation = originalRotation;

            Debug.Log($"🎯 角色 {character.characterName} 移动到位置 {targetTransform.position}，保持原有缩放 {originalScale} 和旋转 {originalRotation}");
        }
    }

    /// <summary>
    /// 从阵型中移除角色
    /// </summary>
    public void RemoveCharacterFromFormation(CharacterStats character) {
        if (character == null) return;

        // 查找并清空角色占用的位置
        foreach (var kvp in positionOccupancy) {
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
                    return new[] {
                        HorizontalPosition.PlayerFrontLeft,
                        HorizontalPosition.PlayerBackLeft,
                        HorizontalPosition.PlayerBackCenter,
                        HorizontalPosition.PlayerBackRight
                    };
                case FormationType.Balanced:
                    return new[] {
                        HorizontalPosition.PlayerFrontLeft,
                        HorizontalPosition.PlayerFrontCenter,
                        HorizontalPosition.PlayerBackLeft,
                        HorizontalPosition.PlayerBackCenter,
                        HorizontalPosition.PlayerBackRight
                    };
                case FormationType.Aggressive:
                default:
                    return new[] {
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
                    return new[] {
                        HorizontalPosition.EnemyFrontLeft,
                        HorizontalPosition.EnemyBackLeft,
                        HorizontalPosition.EnemyBackCenter,
                        HorizontalPosition.EnemyBackRight
                    };
                case FormationType.Balanced:
                    return new[] {
                        HorizontalPosition.EnemyFrontLeft,
                        HorizontalPosition.EnemyFrontCenter,
                        HorizontalPosition.EnemyBackLeft,
                        HorizontalPosition.EnemyBackCenter,
                        HorizontalPosition.EnemyBackRight
                    };
                case FormationType.Aggressive:
                default:
                    return new[] {
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
    /// 直接使用手动配置的spawn点Transform
    /// </summary>
    public Transform GetPositionTransform(HorizontalPosition position) {
        Transform spawnPoint = GetManualSpawnPoint(position);

        if (spawnPoint == null) {
            Debug.LogError($"位置 {position} 的spawn点未配置！请在Inspector中配置所有spawn点Transform");
            return null;
        }

        return spawnPoint;
    }

    /// <summary>
    /// 获取手动配置的spawn点
    /// </summary>
    private Transform GetManualSpawnPoint(HorizontalPosition position) {
        switch (position) {
            // 玩家方位置
            case HorizontalPosition.PlayerFrontLeft:
                return playerFrontLeftSpawn;
            case HorizontalPosition.PlayerFrontCenter:
                return playerFrontCenterSpawn;
            case HorizontalPosition.PlayerFrontRight:
                return playerFrontRightSpawn;
            case HorizontalPosition.PlayerBackLeft:
                return playerBackLeftSpawn;
            case HorizontalPosition.PlayerBackCenter:
                return playerBackCenterSpawn;
            case HorizontalPosition.PlayerBackRight:
                return playerBackRightSpawn;

            // 敌人方位置
            case HorizontalPosition.EnemyFrontLeft:
                return enemyFrontLeftSpawn;
            case HorizontalPosition.EnemyFrontCenter:
                return enemyFrontCenterSpawn;
            case HorizontalPosition.EnemyFrontRight:
                return enemyFrontRightSpawn;
            case HorizontalPosition.EnemyBackLeft:
                return enemyBackLeftSpawn;
            case HorizontalPosition.EnemyBackCenter:
                return enemyBackCenterSpawn;
            case HorizontalPosition.EnemyBackRight:
                return enemyBackRightSpawn;

            default:
                return null;
        }
    }

    /// <summary>
    /// 验证spawn点配置的完整性
    /// </summary>
    [ContextMenu("验证Spawn点配置")]
    public void ValidateSpawnPoints() {
        List<string> missingSpawns = new List<string>();

        // 检查所有位置的spawn点配置
        for (int i = 0; i < 12; i++) {
            HorizontalPosition position = (HorizontalPosition)i;
            Transform spawn = GetManualSpawnPoint(position);
            if (spawn == null) {
                missingSpawns.Add(position.ToString());
            }
        }

        if (missingSpawns.Count > 0) {
            Debug.LogWarning($"以下位置的spawn点未配置: {string.Join(", ", missingSpawns)}");
        }
        else {
            Debug.Log("✅ 所有spawn点配置完整");
        }
    }

    /// <summary>
    /// 🎯 生成完整玩家阵型（按具体位置生成，不按数组顺序）
    /// </summary>
    public List<CharacterStats> GeneratePlayerFormation() {
        List<CharacterStats> playerTeam = new List<CharacterStats>();

        // 按位置而非数组顺序生成角色
        CreateCharacterAtPosition(玩家前排左翼, "玩家前排左翼", HorizontalPosition.PlayerFrontLeft, playerTeam);
        CreateCharacterAtPosition(玩家前排中锋, "玩家前排中锋", HorizontalPosition.PlayerFrontCenter, playerTeam);
        CreateCharacterAtPosition(玩家前排右翼, "玩家前排右翼", HorizontalPosition.PlayerFrontRight, playerTeam);
        CreateCharacterAtPosition(玩家后排左翼, "玩家后排左翼", HorizontalPosition.PlayerBackLeft, playerTeam);
        CreateCharacterAtPosition(玩家后排中路, "玩家后排中路", HorizontalPosition.PlayerBackCenter, playerTeam);
        CreateCharacterAtPosition(玩家后排右翼, "玩家后排右翼", HorizontalPosition.PlayerBackRight, playerTeam);

        Debug.Log($"🔵 生成玩家阵型完成，共 {playerTeam.Count} 人");
        return playerTeam;
    }

    /// <summary>
    /// 🎯 生成完整敌人阵型（按具体位置生成，不按数组顺序）
    /// </summary>
    public List<CharacterStats> GenerateEnemyFormation() {
        List<CharacterStats> enemyTeam = new List<CharacterStats>();

        // 按位置而非数组顺序生成角色
        CreateCharacterAtPosition(敌人前排左翼, "敌人前排左翼", HorizontalPosition.EnemyFrontLeft, enemyTeam);
        CreateCharacterAtPosition(敌人前排中锋, "敌人前排��锋", HorizontalPosition.EnemyFrontCenter, enemyTeam);
        CreateCharacterAtPosition(敌人前排右翼, "敌人前排右翼", HorizontalPosition.EnemyFrontRight, enemyTeam);
        CreateCharacterAtPosition(敌人后排左翼, "敌人后排左翼", HorizontalPosition.EnemyBackLeft, enemyTeam);
        CreateCharacterAtPosition(敌人后排中路, "敌人后排中路", HorizontalPosition.EnemyBackCenter, enemyTeam);
        CreateCharacterAtPosition(敌人后排右翼, "敌人后排右���", HorizontalPosition.EnemyBackRight, enemyTeam);

        Debug.Log($"🔴 生成敌人阵型完成，共 {enemyTeam.Count} 人");
        return enemyTeam;
    }

    /// <summary>
    /// 在指定位置创建角色
    /// </summary>
    private void CreateCharacterAtPosition(GameObject prefab, string positionName, HorizontalPosition position, List<CharacterStats> team) {
        if (prefab == null) return;

        GameObject instance = Instantiate(prefab);
        instance.name = positionName;

        // 🎯 完全保持预制体原有的Transform配置，不进行任何重置
        // 预制体已经手动调整了正确���缩放、位置和朝向信息，不应被修改

        CharacterStats stats = instance.GetComponent<CharacterStats>();
        if (stats == null) {
            stats = instance.AddComponent<CharacterStats>();
        }

        // 设置角色属性
        if (positionName.Contains("玩家")) {
            stats.battleSide = BattleSide.Player;
            instance.tag = "Player";
        }
        else {
            stats.battleSide = BattleSide.Enemy;
            instance.tag = "Enemy";
        }

        stats.characterName = positionName;

        // 🎯 从prefab获取配置好的DND_CharacterAdapter组件（不硬编码添加）
        DND_CharacterAdapter adapter = instance.GetComponent<DND_CharacterAdapter>();
        if (adapter == null) {
            Debug.LogError($"❌ 角色预制体 {positionName} 缺少DND_CharacterAdapter组件！请在prefab中预���配置此组件");
            return;
        }

        // 🎯 从prefab获取配置好的SkeletonAnimation组件（不硬编码添加）
        SkeletonAnimation skeletonAnim = instance.GetComponent<SkeletonAnimation>();
        if (skeletonAnim == null) {
            Debug.LogError($"❌ 角色预制体 {positionName} 缺少SkeletonAnimation组件！请在prefab中预先配置此组件和Spine数据");
            return;
        }

        // 仅设置必要的运行时引用，其他属性通���prefab配置
        adapter.characterStats = stats;

        Debug.Log($"✅ 角色 {positionName} 的组件配置验证完成，完全保持预制体原有Transform设置");

        // 🎯 先设置位置组件，再移动到spawn点，避免错位问题
        BattlePositionComponent positionComponent = instance.GetComponent<BattlePositionComponent>();
        if (positionComponent == null) {
            positionComponent = instance.AddComponent<BattlePositionComponent>();
        }
        positionComponent.currentPosition = position;

        // 直接放置到指定位置（但不调用PlaceCharacterAtPosition，避免重复设置）
        Transform spawnTransform = GetPositionTransform(position);
        if (spawnTransform != null) {
            // 🎯 只设置位��，保持预制体原有的scale和rotation
            Vector3 originalScale = instance.transform.localScale;
            Quaternion originalRotation = instance.transform.rotation;

            instance.transform.position = spawnTransform.position;

            // 确保缩放和旋转保持不变
            instance.transform.localScale = originalScale;
            instance.transform.rotation = originalRotation;

            Debug.Log($"🎯 角色 {positionName} 直接放置到位置 {spawnTransform.position}，保持原有缩放 {originalScale} 和旋转 {originalRotation}");
        }

        // 占用位置
        positionOccupancy[position] = stats;

        team.Add(stats);
        Debug.Log($"✅ 创建角色 {positionName} 到位置 {position}");
    }
    /// 🎯 获取阵型配置摘要（��于调试）
    /// <summary>
    /// 🎯 获取阵型配置摘要（用于调试）
    /// </summary>
    public string GetFormationSummary() {
        string summary = "🎯 当前阵型配置:\n";
        summary += "🔵 玩家阵型:\n";
        summary += $"  前排: {GetPrefabName(玩家前排左翼)} | {GetPrefabName(玩家前排中锋)} | {GetPrefabName(玩家前排右翼)}\n";
        summary += $"  后排: {GetPrefabName(玩家后排左翼)} | {GetPrefabName(玩家后排中路)} | {GetPrefabName(玩家后排右翼)}\n";
        summary += "🔴 敌人阵型:\n";
        summary += $"  前排: {GetPrefabName(敌人前排左翼)} | {GetPrefabName(敌人前排中锋)} | {GetPrefabName(敌人前排右翼)}\n";
        summary += $"  后排: {GetPrefabName(敌人后排左翼)} | {GetPrefabName(敌人后排中路)} | {GetPrefabName(敌人后排右翼)}\n";
        return summary;
    }

    /// <summary>
    /// 安全获取预制体名称
    /// </summary>
    private string GetPrefabName(GameObject prefab) {
        if (prefab == null) return "空";
        try {
            return prefab.name;
        }
        catch {
            return "空";
        }
    }
}
