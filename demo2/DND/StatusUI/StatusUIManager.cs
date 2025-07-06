using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Status UI管理器
/// 统一管理所有角色的状态UI显示
/// </summary>
public class StatusUIManager : MonoBehaviour {
    [Header("UI容器")]
    public Transform playerStatusContainer;
    public Transform allyStatusContainer;
    public Transform enemyStatusContainer;

    [Header("状态UI预制体")]
    public GameObject playerStatusPrefab;
    public GameObject allyStatusPrefab;
    public GameObject enemyStatusPrefab;

    [Header("UI设置")]
    public bool autoHideOnDeath = true;
    public bool showACInfo = true;
    public bool showManaInfo = true;
    public bool showStatusEffects = true;

    [Header("布局设置")]
    public float spacing = 10f;
    public bool useHorizontalLayout = true;

    // 状态UI字典
    private Dictionary<CharacterStats, CharacterStatusDisplay> statusDisplays = new Dictionary<CharacterStats, CharacterStatusDisplay>();

    // 单例模式
    public static StatusUIManager Instance { get; private set; }

    void Awake() {
        // 设置单例
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
            return;
        }

        // 初始化容器
        InitializeContainers();
    }

    /// <summary>
    /// 初始化UI容器
    /// </summary>
    private void InitializeContainers() {
        // 为容器添加布局组件
        if (playerStatusContainer != null) {
            SetupLayoutGroup(playerStatusContainer);
        }

        if (allyStatusContainer != null) {
            SetupLayoutGroup(allyStatusContainer);
        }

        if (enemyStatusContainer != null) {
            SetupLayoutGroup(enemyStatusContainer);
        }
    }

    /// <summary>
    /// 设置布局组件
    /// </summary>
    private void SetupLayoutGroup(Transform container) {
        if (useHorizontalLayout) {
            HorizontalLayoutGroup layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null) {
                layoutGroup = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layoutGroup.spacing = spacing;
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
        }
        else {
            VerticalLayoutGroup layoutGroup = container.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null) {
                layoutGroup = container.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layoutGroup.spacing = spacing;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
        }
    }

    /// <summary>
    /// 注册角色状态UI
    /// </summary>
    public void RegisterCharacter(CharacterStats character) {
        if (character == null) {
            Debug.LogError("StatusUIManager: 尝试注册空角色");
            return;
        }

        // 如果已经注册过，先移除
        if (statusDisplays.ContainsKey(character)) {
            RemoveCharacter(character);
        }

        // 创建状态UI
        CharacterStatusDisplay statusDisplay = CreateStatusUI(character);
        if (statusDisplay != null) {
            statusDisplays[character] = statusDisplay;
            statusDisplay.BindCharacter(character);
            Debug.Log($"StatusUIManager: 为角色 {character.GetDisplayName()} 注册状态UI");
        }
    }

    /// <summary>
    /// 创建状态UI
    /// </summary>
    private CharacterStatusDisplay CreateStatusUI(CharacterStats character) {
        GameObject prefab = null;
        Transform container = null;

        // 根据角色类型选择预制体和容器
        if (CharacterTypeHelper.IsPlayerCharacter(character)) {
            prefab = playerStatusPrefab;
            container = playerStatusContainer;
        }
        else if (CharacterTypeHelper.IsAllyCharacter(character)) {
            prefab = allyStatusPrefab;
            container = allyStatusContainer;
        }
        else if (CharacterTypeHelper.IsEnemyCharacter(character)) {
            prefab = enemyStatusPrefab;
            container = enemyStatusContainer;
        }

        if (prefab == null) {
            Debug.LogWarning($"StatusUIManager: 未找到角色 {character.GetDisplayName()} 对应的状态UI预制体");
            return null;
        }

        if (container == null) {
            Debug.LogWarning($"StatusUIManager: 未找到角色 {character.GetDisplayName()} 对应的UI容器");
            return null;
        }

        // 实例化状态UI
        GameObject statusObj = Instantiate(prefab, container);
        CharacterStatusDisplay statusDisplay = statusObj.GetComponent<CharacterStatusDisplay>();

        if (statusDisplay == null) {
            Debug.LogError($"StatusUIManager: 状态UI预制体缺少CharacterStatusDisplay组件");
            Destroy(statusObj);
            return null;
        }

        return statusDisplay;
    }

    /// <summary>
    /// 移除角色状态UI
    /// </summary>
    public void RemoveCharacter(CharacterStats character) {
        if (character == null) return;

        if (statusDisplays.TryGetValue(character, out CharacterStatusDisplay statusDisplay)) {
            if (statusDisplay != null && statusDisplay.gameObject != null) {
                Destroy(statusDisplay.gameObject);
            }
            statusDisplays.Remove(character);
            Debug.Log($"StatusUIManager: 移除角色 {character.GetDisplayName()} 的状态UI");
        }
    }

    /// <summary>
    /// 更新指定角色的状态UI
    /// </summary>
    public void UpdateCharacterStatus(CharacterStats character) {
        if (character == null) return;

        if (statusDisplays.TryGetValue(character, out CharacterStatusDisplay statusDisplay)) {
            if (statusDisplay != null) {
                statusDisplay.UpdateDisplay();
            }
        }
    }    /// <summary>
         /// 更新所有角色的状态UI
         /// </summary>
    public void UpdateAllCharacterStatus() {
        foreach (KeyValuePair<CharacterStats, CharacterStatusDisplay> kvp in statusDisplays) {
            if (kvp.Value != null) {
                kvp.Value.UpdateDisplay();
            }
        }
    }

    /// <summary>
    /// 处理角色死亡
    /// </summary>
    public void OnCharacterDeath(CharacterStats character) {
        if (character == null) return;

        if (autoHideOnDeath) {
            RemoveCharacter(character);
        }
        else {
            UpdateCharacterStatus(character);
        }
    }

    /// <summary>
    /// 获取角色的状态显示组件
    /// </summary>
    public CharacterStatusDisplay GetCharacterStatusDisplay(CharacterStats character) {
        statusDisplays.TryGetValue(character, out CharacterStatusDisplay statusDisplay);
        return statusDisplay;
    }    /// <summary>
         /// 清除所有状态UI
         /// </summary>
    public void ClearAllStatusUI() {
        foreach (KeyValuePair<CharacterStats, CharacterStatusDisplay> kvp in statusDisplays) {
            if (kvp.Value != null && kvp.Value.gameObject != null) {
                Destroy(kvp.Value.gameObject);
            }
        }
        statusDisplays.Clear();
        Debug.Log("StatusUIManager: 清除所有状态UI");
    }

    /// <summary>
    /// 获取已注册的角色数量
    /// </summary>
    public int GetRegisteredCharacterCount() {
        return statusDisplays.Count;
    }

    /// <summary>
    /// 检查角色是否已注册
    /// </summary>
    public bool IsCharacterRegistered(CharacterStats character) {
        return character != null && statusDisplays.ContainsKey(character);
    }

    void OnDestroy() {
        // 清理单例
        if (Instance == this) {
            Instance = null;
        }
    }
}
