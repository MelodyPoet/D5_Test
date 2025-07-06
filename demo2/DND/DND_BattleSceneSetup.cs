using System.Collections.Generic;
using UnityEngine;
using DND5E;

/// <summary>
/// DND战斗场景设置 - 简化版本，支持手动配置生成点
/// 通过在Inspector中直接设置角色预制体和生成点来配置战斗
/// </summary>
public class DND_BattleSceneSetup : MonoBehaviour {
    [System.Serializable]
    public class CharacterSpawnPoint {
        [Tooltip("角色预制体")]
        public GameObject characterPrefab;

        [Tooltip("生成位置")]
        public Transform spawnPoint;

        [Tooltip("角色自定义名称(可选)")]
        public string customName = "";
    }

    [Header("玩家角色生成点")]
    [SerializeField]
    public List<CharacterSpawnPoint> playerSpawnPoints = new List<CharacterSpawnPoint>();

    [Header("敌人角色生成点")]
    [SerializeField]
    public List<CharacterSpawnPoint> enemySpawnPoints = new List<CharacterSpawnPoint>();

    [Header("范围和目标指示器预制体")]
    [Tooltip("移动范围指示器预制体 - 蓝色圆形范围，用于显示移动和冲刺范围")]
    public GameObject movementRangeIndicatorPrefab;

    [Tooltip("施法范围指示器预制体 - 紫色圆形范围，用于显示法术施放范围")]
    public GameObject spellRangeIndicatorPrefab;

    [Tooltip("敌人目标高亮预制体 - 红色高亮效果，用于标记可攻击的敌人")]
    public GameObject enemyTargetHighlightPrefab;

    [Tooltip("自身高亮预制体 - 绿色高亮效果，用于闪避时高亮自己")]
    public GameObject selfHighlightPrefab;

    [Header("UI 相关")]
    [Tooltip("场景中已放置的 DND_BattleUI 组件引用，可以直接将含 ActionPanel 的 Canvas 拖到此处")]
    public DND_BattleUI battleUIReference;

    // 生成的角色列表
    private List<GameObject> spawnedPlayers = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Awake() {
        InitializeManagers();
    }

    private void Start() {
        SpawnCharacters();
    }

    private void InitializeManagers() {
        Debug.Log("[BattleSceneSetup] 开始初始化管理器");

        // 确保单例管理器初始化
        CombatManager existingCombatManager = FindObjectOfType<CombatManager>();
        if (existingCombatManager == null) {
            Debug.Log("[BattleSceneSetup] Creating CombatManager");
            GameObject combatManagerObj = new GameObject("CombatManager");
            CombatManager newCombatManager = combatManagerObj.AddComponent<CombatManager>();
            Debug.Log($"[BattleSceneSetup] CombatManager created: {newCombatManager != null}");
        }
        else {
            Debug.Log("[BattleSceneSetup] CombatManager already exists");
        }

        if (RangeManager.Instance == null) {
            Debug.Log("[BattleSceneSetup] Creating RangeManager");
            GameObject rangeManagerObj = new GameObject("RangeManager");
            RangeManager rangeManager = rangeManagerObj.AddComponent<RangeManager>();

            // 设置预制体引用
            SetupRangeManagerPrefabs(rangeManager);
        }
        else {
            Debug.Log("[BattleSceneSetup] RangeManager already exists");
            // 即使已存在，也要确保预制体设置正确
            SetupRangeManagerPrefabs(RangeManager.Instance);
        }

        Debug.Log("[BattleSceneSetup] 管理器初始化完成");
    }

    /// <summary>
    /// 设置 RangeManager 的预制体引用
    /// </summary>
    private void SetupRangeManagerPrefabs(RangeManager rangeManager) {
        if (rangeManager == null) {
            Debug.LogError("[BattleSceneSetup] RangeManager 为空，无法设置预制体");
            return;
        }

        // 设置移动范围指示器 (蓝色圆形，用于移动和冲刺)
        if (movementRangeIndicatorPrefab != null) {
            rangeManager.rangeIndicatorPrefab = movementRangeIndicatorPrefab;
            Debug.Log("[BattleSceneSetup] 设置移动范围指示器预制体");
        }
        else {
            Debug.LogWarning("[BattleSceneSetup] 移动范围指示器预制体未设置");
        }

        // 设置目标高亮预制体 (红色高亮，用于攻击目标)
        if (enemyTargetHighlightPrefab != null) {
            rangeManager.targetIndicatorPrefab = enemyTargetHighlightPrefab;
            Debug.Log("[BattleSceneSetup] 设置敌人目标高亮预制体");
        }
        else {
            Debug.LogWarning("[BattleSceneSetup] 敌人目标高亮预制体未设置");
        }

        Debug.Log("[BattleSceneSetup] RangeManager 预制体设置完成");
    }

    private void SpawnCharacters() {
        Debug.Log("[BattleSceneSetup] 开始生成角色");

        // 生成玩家角色
        SpawnCharactersFromList(playerSpawnPoints, spawnedPlayers, "Player");

        // 生成敌人角色
        SpawnCharactersFromList(enemySpawnPoints, spawnedEnemies, "Enemy");

        Debug.Log($"[BattleSceneSetup] 角色生成完成，玩家: {spawnedPlayers.Count}, 敌人: {spawnedEnemies.Count}");

        // 启动战斗
        StartBattle();
    }
    private void StartBattle() {
        // 使用场景中已放置的 Battle UI
        if (battleUIReference == null) {
            battleUIReference = FindObjectOfType<DND_BattleUI>();
        }
        if (battleUIReference == null) {
            Debug.LogError("[BattleSceneSetup] 未找到 DND_BattleUI，请确保场景中存在并在 Inspector 中赋值");
        }

        // 收集所有角色的CharacterStats
        List<CharacterStats> allCharacters = new List<CharacterStats>();

        // 添加玩家角色
        foreach (GameObject player in spawnedPlayers) {
            if (player == null) continue;
            CharacterStats stats = player.GetComponent<CharacterStats>();
            if (stats != null) {
                allCharacters.Add(stats); Debug.Log($"[BattleSceneSetup] 添加玩家到战斗: {stats.characterName}");

                // 优先使用StatusUIManager注册Status UI
                StatusUIManager statusUIManager = FindObjectOfType<StatusUIManager>();
                if (statusUIManager != null) {
                    statusUIManager.RegisterCharacter(stats);
                    Debug.Log($"[BattleSceneSetup] 使用StatusUIManager为玩家 {stats.characterName} 注册Status UI");
                }
                // 回退到BattleUI的方法
                else if (battleUIReference != null) {
                    battleUIReference.RegisterCharacterStatusUI(stats);
                    Debug.Log($"[BattleSceneSetup] 使用BattleUI为玩家 {stats.characterName} 注册Status UI");
                }
            }
        }

        // 添加敌人角色
        foreach (GameObject enemy in spawnedEnemies) {
            if (enemy == null) continue;
            CharacterStats stats = enemy.GetComponent<CharacterStats>();
            if (stats != null) {
                allCharacters.Add(stats); Debug.Log($"[BattleSceneSetup] 添加敌人到战斗: {stats.characterName}");

                // 优先使用StatusUIManager注册Status UI
                StatusUIManager statusUIManager = FindObjectOfType<StatusUIManager>();
                if (statusUIManager != null) {
                    statusUIManager.RegisterCharacter(stats);
                    Debug.Log($"[BattleSceneSetup] 使用StatusUIManager为敌人 {stats.characterName} 注册Status UI");
                }
                // 回退到BattleUI的方法
                else if (battleUIReference != null) {
                    battleUIReference.RegisterCharacterStatusUI(stats);
                    Debug.Log($"[BattleSceneSetup] 使用BattleUI为敌人 {stats.characterName} 注册Status UI");
                }
            }
        }

        // 启动战斗
        if (allCharacters.Count > 0) {
            CombatManager combatManager = FindObjectOfType<CombatManager>();
            if (combatManager != null) {
                Debug.Log($"[BattleSceneSetup] 启动战斗，参与者数量: {allCharacters.Count}");
                combatManager.StartCombat(allCharacters);
            }
            else {
                Debug.LogError("[BattleSceneSetup] 无法找到 CombatManager 来启动战斗!");
            }
        }
        else {
            Debug.LogWarning("[BattleSceneSetup] 没有角色参与战斗!");
        }
    }

    private void SpawnCharactersFromList(List<CharacterSpawnPoint> spawnPoints, List<GameObject> spawnedList, string tag) {
        for (int i = 0; i < spawnPoints.Count; i++) {
            CharacterSpawnPoint spawnPoint = spawnPoints[i];

            // 只检查必要的配置：预制体和生成位置
            if (spawnPoint.characterPrefab == null || spawnPoint.spawnPoint == null) {
                Debug.LogWarning($"[BattleSceneSetup] 跳过无效的{tag}生成点 [{i}] - 缺少预制体或生成位置");
                continue;
            }

            // 生成角色
            Vector3 spawnPosition = spawnPoint.spawnPoint.position;
            Quaternion spawnRotation = spawnPoint.spawnPoint.rotation;
            GameObject character = Instantiate(spawnPoint.characterPrefab, spawnPosition, spawnRotation);
            character.tag = tag;

            Debug.Log($"【ActionPanel调试】生成角色: {character.name}, 设置标签为: {tag}, 实际标签: {character.tag}");

            // 设置自定义名称
            if (!string.IsNullOrEmpty(spawnPoint.customName)) {
                character.name = spawnPoint.customName;

                CharacterStats stats = character.GetComponent<CharacterStats>();
                if (stats != null) {
                    stats.characterName = spawnPoint.customName;
                }
            }

            spawnedList.Add(character);
            Debug.Log($"[BattleSceneSetup] 生成{tag}角色: {character.name} 在位置 {spawnPosition}");
        }
    }

    /// <summary>
    /// 清除所有生成的角色
    /// </summary>
    public void ClearSpawnedCharacters() {
        foreach (GameObject character in spawnedPlayers) {
            if (character != null)
                DestroyImmediate(character);
        }

        foreach (GameObject character in spawnedEnemies) {
            if (character != null)
                DestroyImmediate(character);
        }

        spawnedPlayers.Clear();
        spawnedEnemies.Clear();
        Debug.Log("[BattleSceneSetup] 已清除所有生成的角色");
    }

    #region 动作范围和目标显示方法

    /// <summary>
    /// 显示移动范围 (蓝色圆形)
    /// </summary>
    /// <param name="character">执行移动的角色</param>
    /// <param name="isDash">是否为冲刺移动</param>
    public void ShowMovementRange(CharacterStats character, bool isDash = false) {
        if (RangeManager.Instance != null) {
            Debug.Log($"[BattleSceneSetup] 显示{(isDash ? "冲刺" : "移动")}范围: {character.characterName}");
            RangeManager.Instance.ShowMovementRange(character, isDash);
        }
        else {
            Debug.LogError("[BattleSceneSetup] RangeManager 未找到，无法显示移动范围");
        }
    }

    /// <summary>
    /// 显示攻击范围和目标高亮 (红色高亮敌人)
    /// </summary>
    /// <param name="character">执行攻击的角色</param>
    /// <param name="attackType">攻击类型</param>
    public void ShowAttackTargets(CharacterStats character, AttackType attackType = AttackType.Melee) {
        if (RangeManager.Instance != null) {
            Debug.Log($"[BattleSceneSetup] 显示攻击目标: {character.characterName}, 攻击类型: {attackType}");
            RangeManager.Instance.ShowAttackRange(character, attackType);

            // 高亮可攻击的敌人目标
            HighlightAttackableEnemies(character, attackType);
        }
        else {
            Debug.LogError("[BattleSceneSetup] RangeManager 未找到，无法显示攻击范围");
        }
    }

    /// <summary>
    /// 显示法术施放范围 (紫色圆形，默认30尺)
    /// </summary>
    /// <param name="character">施法者</param>
    /// <param name="spellRange">法术范围，默认30尺</param>
    public void ShowSpellRange(CharacterStats character, float spellRange = 30f) {
        if (RangeManager.Instance != null) {
            Debug.Log($"[BattleSceneSetup] 显示法术范围: {character.characterName}, 范围: {spellRange}尺");

            // 使用法术范围指示器预制体（如果设置了）或默认范围指示器
            GameObject rangeIndicator = spellRangeIndicatorPrefab != null ? spellRangeIndicatorPrefab : movementRangeIndicatorPrefab;

            if (rangeIndicator != null) {
                // 临时替换 RangeManager 的预制体来显示法术范围
                GameObject originalPrefab = RangeManager.Instance.rangeIndicatorPrefab;
                RangeManager.Instance.rangeIndicatorPrefab = rangeIndicator;
                // 显示法术范围（使用攻击范围方法显示，但设置为法术类型）
                RangeManager.Instance.ShowAttackRange(character, AttackType.Spell);

                // 恢复原始预制体
                RangeManager.Instance.rangeIndicatorPrefab = originalPrefab;
            }
        }
        else {
            Debug.LogError("[BattleSceneSetup] RangeManager 未找到，无法显示法术范围");
        }
    }

    /// <summary>
    /// 显示闪避自身高亮 (绿色高亮自己)
    /// </summary>
    /// <param name="character">执行闪避的角色</param>
    public void ShowDodgeHighlight(CharacterStats character) {
        Debug.Log($"[BattleSceneSetup] 显示闪避高亮: {character.characterName}");

        if (selfHighlightPrefab != null) {
            // 清除之前的高亮
            ClearAllHighlights();

            // 在角色位置创建绿色自身高亮
            GameObject highlight = Instantiate(selfHighlightPrefab, character.transform.position, Quaternion.identity);
            highlight.name = $"SelfHighlight_{character.characterName}";

            // 可以添加一些特效，比如缩放动画或自动销毁
            StartCoroutine(DestroyHighlightAfterDelay(highlight, 3f));
        }
        else {
            Debug.LogWarning("[BattleSceneSetup] 自身高亮预制体未设置");
        }
    }

    /// <summary>
    /// 高亮可攻击的敌人目标
    /// </summary>
    private void HighlightAttackableEnemies(CharacterStats attacker, AttackType attackType) {
        // 清除之前的目标高亮
        ClearTargetHighlights();

        if (enemyTargetHighlightPrefab == null) {
            Debug.LogWarning("[BattleSceneSetup] 敌人目标高亮预制体未设置");
            return;
        }

        // 获取攻击范围
        float attackRange = GetAttackRange(attackType);
        // 根据攻击者类型确定目标类型
        bool attackerIsPlayer = CharacterTypeHelper.IsPlayerControlled(attacker);
        List<GameObject> potentialTargets = attackerIsPlayer ? spawnedEnemies : spawnedPlayers;

        foreach (GameObject target in potentialTargets) {
            if (target != null) {
                CharacterStats targetStats = target.GetComponent<CharacterStats>();
                if (targetStats != null && targetStats.currentHitPoints > 0) {
                    float distance = Vector3.Distance(attacker.transform.position, target.transform.position);

                    // 转换距离为尺（feet）
                    float distanceInFeet = distance / (RangeManager.Instance != null ? RangeManager.Instance.unitToFeetRatio : 0.2f);

                    if (distanceInFeet <= attackRange) {
                        // 在目标位置创建红色高亮
                        GameObject highlight = Instantiate(enemyTargetHighlightPrefab, target.transform.position, Quaternion.identity);
                        highlight.name = $"TargetHighlight_{targetStats.characterName}";

                        Debug.Log($"[BattleSceneSetup] 高亮攻击目标: {targetStats.characterName}, 距离: {distanceInFeet:F1}尺");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取指定攻击类型的攻击范围
    /// </summary>
    private float GetAttackRange(AttackType attackType) {
        if (RangeManager.Instance != null) {
            switch (attackType) {
                case AttackType.Melee:
                    return RangeManager.Instance.meleeRange;
                case AttackType.Ranged:
                    return RangeManager.Instance.defaultRangedRange;
                case AttackType.Spell:
                    return RangeManager.Instance.defaultSpellRange;
                default:
                    return RangeManager.Instance.meleeRange;
            }
        }
        return 15f; // 默认近战范围
    }

    /// <summary>
    /// 清除所有目标高亮
    /// </summary>
    public void ClearTargetHighlights() {
        GameObject[] highlights = GameObject.FindGameObjectsWithTag("TargetHighlight");
        foreach (GameObject highlight in highlights) {
            if (highlight != null)
                DestroyImmediate(highlight);
        }

        // 也清除通过名称查找的高亮
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects) {
            if (obj.name.StartsWith("TargetHighlight_")) {
                DestroyImmediate(obj);
            }
        }
    }

    /// <summary>
    /// 清除所有高亮效果
    /// </summary>
    public void ClearAllHighlights() {
        ClearTargetHighlights();

        // 清除自身高亮
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects) {
            if (obj.name.StartsWith("SelfHighlight_")) {
                DestroyImmediate(obj);
            }
        }

        // 清除范围指示器
        if (RangeManager.Instance != null) {
            RangeManager.Instance.ClearRangeIndicator();
        }
    }

    /// <summary>
    /// 延迟销毁高亮效果
    /// </summary>
    private System.Collections.IEnumerator DestroyHighlightAfterDelay(GameObject highlight, float delay) {
        yield return new WaitForSeconds(delay);
        if (highlight != null) {
            DestroyImmediate(highlight);
        }
    }

    #endregion

    // Unity编辑器中的辅助方法
#if UNITY_EDITOR
    [ContextMenu("重新生成所有角色")]
    public void RegenerateAllCharacters() {
        ClearSpawnedCharacters();
        SpawnCharacters();
    }

    [ContextMenu("清除生成的角色")]
    public void ClearCharacters() {
        ClearSpawnedCharacters();
    }
#endif
}
