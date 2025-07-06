using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DND5E;
using UnityEngine.EventSystems;
using DG.Tweening;
using Spine.Unity;

public class DND_BattleUI : MonoBehaviour {
    // 单例实例
    public static DND_BattleUI Instance { get; private set; }

    // 当前选中的按钮
    private Button currentSelectedButton = null;

    // 当前操作类型
    private enum OperationType {
        None,
        Attack,
        Move,
        Dash,
        Dodge,
        Spell
    }

    private OperationType currentOperation = OperationType.None;

    // UI面板
    public GameObject actionPanel;
    public GameObject spellPanel;
    public GameObject targetSelectionPanel;

    // 信息文本
    public Text turnInfoText;
    public Text characterInfoText;

    // 行动按钮
    public Button attackButton;
    public Button moveButton;
    public Button dashButton;
    public Button dodgeButton;
    public Button spellButton;
    public Button endTurnButton;

    // 法术按钮预制体
    public GameObject spellButtonPrefab;

    // 法术按钮容器
    public Transform spellButtonContainer;

    // 角色状态UI预制体（保留用于扩展）
    public GameObject characterStatusPrefab;
    public GameObject allyStatusPrefab;
    public GameObject enemyStatusPrefab;

    // 角色状态UI字典
    private Dictionary<CharacterStats, GameObject> characterStatusUIs = new Dictionary<CharacterStats, GameObject>();

    // 当前选中的角色
    private CharacterStats selectedCharacter;

    // 当前高亮的敌人
    private GameObject currentHighlightedEnemy;

    // 当前选中的法术
    private Spell currentSelectedSpell;

    // 战斗管理器引用
    private CombatManager combatManager;

    // 战斗日志
    public ScrollRect combatLogScrollRect;
    public Text combatLogText;
    private List<string> combatLogEntries = new List<string>();

    // 移动指示器
    private GameObject moveRangeIndicator;
    private GameObject hoverIndicator;

    // 高亮协程
    private Coroutine highlightCoroutine;

    void Awake() {
        // 单例模式
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
            return;
        }

        // 自动查找UI引用并添加必要的Raycaster
        FindUIReferences();
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.GetComponent<GraphicRaycaster>() == null) {
            parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        // 初始化组件
        combatManager = FindObjectOfType<CombatManager>();
        // 立即订阅战斗事件，确保在开始战斗时接收回合开始通知
        if (combatManager != null) {
            combatManager.OnTurnStart += OnTurnStart;
            combatManager.OnCombatEnd += OnCombatEnd;
        }
        else {
            Debug.LogError("【ActionPanel调试】combatManager仍然为null，无法注册事件!");
            Debug.LogError("【ActionPanel调试】请检查场景中是否有CombatManager或DND_BattleSceneSetup组件");
        }

        // 打印所有子节点名称以便调试UI结构
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms) {
            Debug.Log($"【ActionPanel调试】子节点: {t.name}");
        }
    }

    void Start() {
        // 确保引用已找到
        FindUIReferences();
        Debug.Log("【ActionPanel调试】DND_BattleUI Start方法开始");

        // 如果在Awake中没有找到CombatManager，在Start中重新查找
        if (combatManager == null) {
            Debug.Log("【ActionPanel调试】combatManager在Awake中为null，在Start中重新查找");
            combatManager = FindObjectOfType<CombatManager>();
        }

        InitializeUI();
        EnsureUIElementsVisible();

        // 不再在此处订阅事件，以免错过首个回合开始通知

        EnsureActionPanelRaycaster();
        EnsureButtonsInteractable();

        Debug.Log($"【ActionPanel调试】actionPanel引用状态: {(actionPanel != null ? "正常" : "为null")}");

        Invoke(nameof(CheckAllColliders), 1.0f);
        Invoke(nameof(EnsureActionPanelRaycaster), 2.0f);
    }

    // Auto-find UI panels and buttons if not assigned in Inspector
    private void FindUIReferences() {
        // Panels
        if (actionPanel == null) actionPanel = transform.Find("ActionPanel")?.gameObject;
        if (spellPanel == null) spellPanel = transform.Find("SpellPanel")?.gameObject;
        if (targetSelectionPanel == null) targetSelectionPanel = transform.Find("TargetSelectionPanel")?.gameObject;
        // 备用：遍历所有子节点查找名称中包含"ActionPanel"的节点
        if (actionPanel == null) {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            Debug.Log("[BattleUI] 现有子节点: " + string.Join(",", System.Array.ConvertAll(all, t => t.name)));
            foreach (Transform t in all) {
                if (t.name.IndexOf("ActionPanel", System.StringComparison.OrdinalIgnoreCase) >= 0) {
                    actionPanel = t.gameObject;
                    Debug.Log("[BattleUI] 备用查找：找到 ActionPanel -> " + t.name);
                    break;
                }
            }
        }
        // Buttons under action panel
        Transform ap = actionPanel?.transform;
        if (ap != null) {
            if (attackButton == null) attackButton = ap.Find("AttackButton")?.GetComponent<Button>();
            if (moveButton == null) moveButton = ap.Find("MoveButton")?.GetComponent<Button>();
            if (dashButton == null) dashButton = ap.Find("DashButton")?.GetComponent<Button>();
            if (dodgeButton == null) dodgeButton = ap.Find("DodgeButton")?.GetComponent<Button>();
            if (spellButton == null) spellButton = ap.Find("SpellButton")?.GetComponent<Button>();
            if (endTurnButton == null) endTurnButton = ap.Find("EndTurnButton")?.GetComponent<Button>();
        }
        // Spell button container under spell panel
        Transform sp = spellPanel?.transform;
        if (sp != null && spellButtonContainer == null) {
            spellButtonContainer = sp.Find("SpellButtonContainer");
        }
    }

    private void InitializeUI() {
        // 隐藏所有面板
        if (actionPanel != null)
            actionPanel.SetActive(false);
        if (spellPanel != null)
            spellPanel.SetActive(false);
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);

        // 设置按钮监听
        if (attackButton != null) {
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(OnAttackButtonClicked);
        }

        if (moveButton != null) {
            moveButton.onClick.RemoveAllListeners();
            moveButton.onClick.AddListener(OnMoveButtonClicked);
        }

        if (dashButton != null) {
            dashButton.onClick.RemoveAllListeners();
            dashButton.onClick.AddListener(OnDashButtonClicked);
        }

        if (dodgeButton != null) {
            dodgeButton.onClick.RemoveAllListeners();
            dodgeButton.onClick.AddListener(OnDodgeButtonClicked);
        }

        if (spellButton != null) {
            spellButton.onClick.RemoveAllListeners();
            spellButton.onClick.AddListener(OnSpellButtonClicked);
        }

        if (endTurnButton != null) {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        }

        EnsureRaycastSettings();
        UpdateCombatLogText();
    }
    public void UpdateCharacterStatusUI(CharacterStats character) {
        if (character == null) return;

        // 优先尝试使用新的StatusUIManager
        StatusUIManager statusUIManager = FindObjectOfType<StatusUIManager>();
        if (statusUIManager != null) {
            statusUIManager.UpdateCharacterStatus(character);
            return;
        }

        // 兼容旧的UI系统
        if (characterStatusUIs.ContainsKey(character)) {
            GameObject statusUI = characterStatusUIs[character];
            if (statusUI != null) {
                // 更新血量条
                Transform hpSliderTrans = statusUI.transform.Find("HealthSlider");
                if (hpSliderTrans != null) {
                    Slider hpSlider = hpSliderTrans.GetComponent<Slider>();
                    if (hpSlider != null) {
                        float healthPercentage = (float)character.currentHitPoints / character.maxHitPoints;
                        hpSlider.value = Mathf.Clamp01(healthPercentage);
                    }
                }
            }
        }
    }
    public void RegisterCharacterStatusUI(CharacterStats character) {
        if (character == null) return;

        // 角色已注册则跳过
        if (characterStatusUIs.ContainsKey(character)) {
            Debug.Log($"角色 {character.characterName} 的UI已注册，跳过");
            return;
        }

        // 优先尝试使用新的StatusUIManager
        StatusUIManager statusUIManager = FindObjectOfType<StatusUIManager>();
        if (statusUIManager != null) {
            statusUIManager.RegisterCharacter(character);
            // 在旧系统中标记为已注册，避免重复处理
            characterStatusUIs[character] = null;
            Debug.Log($"使用StatusUIManager为角色 {character.characterName} 注册状态UI");
            return;
        }

        // 回退到旧的UI系统
        try {
            if (character.gameObject.CompareTag("Player") || character.gameObject.CompareTag("Ally")) {
                SetupPlayerStatusUI(character);
            }
            else if (character.gameObject.CompareTag("Enemy")) {
                SetupEnemyStatusUI(character);
            }
            else {
                Debug.LogWarning($"角色 {character.characterName} 的标签 '{character.gameObject.tag}' 不是 Player、Ally 或 Enemy");
            }
        }
        catch (System.Exception e) {
            Debug.LogWarning($"为角色 {character.characterName} 设置状态UI失败，但角色仍将正常参与战斗: {e.Message}");
            // 即使UI设置失败，也要在字典中记录这个角色，避免重复尝试
            characterStatusUIs[character] = null;
        }
    }
    private void SetupPlayerStatusUI(CharacterStats character) {
        // 如果指定了玩家状态UI预制体，则直接实例化并使用
        if (allyStatusPrefab != null) {
            GameObject statusUI = Instantiate(allyStatusPrefab, this.transform);
            statusUI.SetActive(true);
            characterStatusUIs[character] = statusUI;
            UpdateCharacterStatusUI(character);
            Debug.Log($"使用allyStatusPrefab为角色 {character.characterName} 创建状态UI");
            return;
        }

        // 如果没有prefab，直接跳过UI设置，不查找Status Canvas
        Debug.LogWarning($"未配置allyStatusPrefab，跳过为角色 {character.characterName} 设置状态UI");
        characterStatusUIs[character] = null;
    }
    private void SetupEnemyStatusUI(CharacterStats character) {
        // 如果指定了敌人状态UI预制体，则直接实例化并使用
        if (enemyStatusPrefab != null) {
            GameObject statusUI = Instantiate(enemyStatusPrefab, this.transform);
            statusUI.SetActive(true);
            characterStatusUIs[character] = statusUI;
            UpdateCharacterStatusUI(character);
            Debug.Log($"使用enemyStatusPrefab为角色 {character.characterName} 创建状态UI");
            return;
        }

        // 如果没有prefab，直接跳过UI设置，不查找Status Canvas
        Debug.LogWarning($"未配置enemyStatusPrefab，跳过为角色 {character.characterName} 设置状态UI");
        characterStatusUIs[character] = null;
    }

    // 添加战斗日志
    public void AddCombatLog(string message) {
        combatLogEntries.Add(message);
        UpdateCombatLogText();
    }

    private void UpdateCombatLogText() {
        if (combatLogText != null) {
            string logContent = string.Join("\n", combatLogEntries);
            combatLogText.text = logContent;
        }
    }

    private void EnsureRaycastSettings() {
        // 确保UI组件有正确的射线检测设置
        if (combatLogScrollRect != null) {
            ReplaceWithPassThroughRaycaster(combatLogScrollRect.gameObject);
        }
    }

    private void ReplaceWithPassThroughRaycaster(GameObject uiObject) {
        // 替换射线检测器
        GraphicRaycaster existingRaycaster = uiObject.GetComponent<GraphicRaycaster>();
        if (existingRaycaster != null) {
            Destroy(existingRaycaster);
        }
    }

    public void UpdateCharacterInfo(CharacterStats character) {
        if (character == null || characterInfoText == null) return;

        string info = $"角色: {character.characterName}\n";
        info += $"生命: {character.currentHitPoints}/{character.maxHitPoints}\n";
        info += $"护甲: {character.armorClass}\n";

        characterInfoText.text = info;
    }

    // 显示行动面板
    public void ShowActionPanel(CharacterStats character) {
        Debug.Log($"【ActionPanel调试】ShowActionPanel被调用，角色: {character?.characterName}, actionPanel是否为null: {actionPanel == null}");

        if (character == null) return;

        selectedCharacter = character;
        UpdateCharacterInfo(character);

        if (actionPanel != null) {
            Debug.Log($"【ActionPanel调试】激活actionPanel，当前状态: {actionPanel.activeInHierarchy}");
            actionPanel.SetActive(true);

            // Bring action panel to front
            actionPanel.transform.SetAsLastSibling();

            // 确保所有按钮可见
            if (attackButton != null) attackButton.gameObject.SetActive(true);
            if (moveButton != null) moveButton.gameObject.SetActive(true);
            if (dashButton != null) dashButton.gameObject.SetActive(true);
            if (dodgeButton != null) dodgeButton.gameObject.SetActive(true);
            if (spellButton != null) spellButton.gameObject.SetActive(true);
            if (endTurnButton != null) {
                endTurnButton.gameObject.SetActive(true);
                endTurnButton.interactable = true;
            }
        }
        else {
            Debug.LogError("【ActionPanel调试】actionPanel为null！请检查UI设置");
        }

        // 隐藏其他面板
        if (spellPanel != null) spellPanel.SetActive(false);
        if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);

        UpdateActionButtons();

        Debug.Log($"【ActionPanel调试】ShowActionPanel完成，actionPanel激活状态: {actionPanel?.activeInHierarchy}");
    }

    // 隐藏行动面板
    public void HideActionPanel() {
        if (actionPanel != null) actionPanel.SetActive(false);
        if (spellPanel != null) spellPanel.SetActive(false);
        if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);
    }

    private void OnTurnStart(CharacterStats character) {
        Debug.Log($"【ActionPanel调试】角色 {character.characterName} 的回合开始，标签: {character.gameObject.tag}");

        if (turnInfoText != null) {
            turnInfoText.gameObject.SetActive(true);
            turnInfoText.text = $"{character.GetDisplayName()}的回合";
        }

        AddCombatLog($"--- {character.GetDisplayName()} 的回合开始 ---");

        if (character.gameObject.CompareTag("Player")) {
            Debug.Log($"【ActionPanel调试】检测到玩家角色，显示行动面板");
            ShowActionPanel(character);
        }
        else {
            Debug.Log($"【ActionPanel调试】非玩家角色，隐藏行动面板");
            HideActionPanel();
        }
    }

    private void OnCombatEnd() {
        Debug.Log("战斗结束");

        HideActionPanel();

        if (turnInfoText != null) {
            turnInfoText.text = "战斗结束！";
        }
        // 更新所有角色状态UI
        foreach (KeyValuePair<CharacterStats, GameObject> kvp in characterStatusUIs) {
            UpdateCharacterStatusUI(kvp.Key);
        }
    }

    // 按钮点击事件
    private void OnAttackButtonClicked() {
        if (selectedCharacter == null) return;

        // 检查角色是否有可用的攻击动作
        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasAction) {
            AddCombatLog("没有可用的攻击动作");
            return;
        }

        currentSelectedButton = attackButton;
        currentOperation = OperationType.Attack;

        if (RangeManager.Instance != null) {
            RangeManager.Instance.ShowAttackRange(selectedCharacter, AttackType.Melee);
        }
        else {
            Debug.LogError("RangeManager.Instance 为null！请确保场景中有RangeManager组件");
            AddCombatLog("攻击系统尚未准备就绪，请稍后再试");
            ResetCurrentOperation();
            return;
        }
        StartCoroutine(SelectTargetForAttack());
    }
    private void OnMoveButtonClicked() {
        if (selectedCharacter == null) return;

        // 检查角色是否有可用的移动动作
        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasMovement) {
            AddCombatLog("没有可用的移动动作");
            return;
        }

        currentSelectedButton = moveButton;
        currentOperation = OperationType.Move;

        if (RangeManager.Instance != null) {
            RangeManager.Instance.ShowMovementRange(selectedCharacter);
            StartCoroutine(SelectDestinationForMove());
        }
        else {
            Debug.LogError("RangeManager.Instance 为null！请确保场景中有RangeManager组件");
            AddCombatLog("移动系统尚未准备就绪，请稍后再试");
            ResetCurrentOperation();
        }
    }
    private void OnDashButtonClicked() {
        if (selectedCharacter == null) return;

        // 检查角色是否有可用的主要动作（冲刺消耗主要动作）
        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasAction) {
            AddCombatLog("没有可用的主要动作进行冲刺");
            return;
        }

        currentSelectedButton = dashButton;
        currentOperation = OperationType.Dash;

        if (RangeManager.Instance != null) {
            RangeManager.Instance.ShowMovementRange(selectedCharacter, true);
            StartCoroutine(SelectTargetForDashAttack());
        }
        else {
            Debug.LogError("RangeManager.Instance 为null！请确保场景中有RangeManager组件");
            AddCombatLog("冲刺系统尚未准备就绪，请稍后再试");
            ResetCurrentOperation();
        }
    }

    private void OnDodgeButtonClicked() {
        if (selectedCharacter == null) return;

        // 检查角色是否有可用的反应动作（闪避消耗反应动作）
        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasReaction) {
            AddCombatLog("没有可用的反应动作进行闪避");
            return;
        }

        currentSelectedButton = dodgeButton;
        currentOperation = OperationType.Dodge; StartCoroutine(ConfirmDodgeAction());
    }

    private void OnSpellButtonClicked() {
        if (selectedCharacter == null) return;

        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || (!actionSystem.hasAction && !actionSystem.hasBonusAction)) {
            AddCombatLog("没有可用的动作进行施法");
            return;
        }

        currentSelectedButton = spellButton;
        currentOperation = OperationType.Spell;

        SpellSystem spellSystem = selectedCharacter.GetComponent<SpellSystem>();
        if (spellSystem == null || spellSystem.spellList == null || spellSystem.spellList.knownSpells.Count == 0) {
            AddCombatLog("你没有掌握任何法术");
            ResetCurrentOperation();
            return;
        }

        AddCombatLog("准备施放法术...");
        PopulateSpellPanel();
        ShowSpellPanel();
    }

    /// <summary>
    /// 根据当前角色的 SpellSystem 动态填充法术列表
    /// </summary>
    private void PopulateSpellPanel() {
        if (spellPanel == null || spellButtonPrefab == null || spellButtonContainer == null) return;
        // 清空旧的按钮
        foreach (Transform child in spellButtonContainer) {
            Destroy(child.gameObject);
        }
        SpellSystem spellSystem = selectedCharacter.GetComponent<SpellSystem>();
        if (spellSystem == null || spellSystem.spellList == null) return;
        foreach (Spell spell in spellSystem.spellList.knownSpells) {
            GameObject btnObj = Instantiate(spellButtonPrefab, spellButtonContainer);
            Button btn = btnObj.GetComponent<Button>();
            Text txt = btnObj.GetComponentInChildren<Text>();
            if (txt != null) txt.text = spell.name;
            if (btn != null) {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => StartCoroutine(ExecuteSpell(spell)));
            }
        }
    }

    /// <summary>
    /// 协程执行选定的法术逻辑
    /// </summary>
    private IEnumerator ExecuteSpell(Spell spell) {
        // 隐藏法术面板并清理范围指示
        if (spellPanel != null) spellPanel.SetActive(false);
        if (RangeManager.Instance != null) RangeManager.Instance.ClearAllIndicators();

        // 调用当前角色的 SpellSystem 施法
        SpellSystem ss = selectedCharacter.GetComponent<SpellSystem>();
        if (ss != null) {
            // 目标设为自身或由具体法术选择，暂用当前对象
            bool success = ss.CastSpell(spell, selectedCharacter.gameObject);
            if (!success) {
                AddCombatLog($"{selectedCharacter.characterName} 无法施放 {spell.name}");
            }
        }
        else {
            Debug.LogError($"SpellSystem missing on {selectedCharacter.characterName}");
        }

        // 等待一帧以保证效果触发
        yield return null;

        // 回到行动面板
        ShowActionPanel(selectedCharacter);
        UpdateActionButtons();
    }

    private void OnEndTurnButtonClicked() {
        if (selectedCharacter == null || combatManager == null) return;

        AddCombatLog($"--- {selectedCharacter.characterName} 的回合结束 ---"); HideActionPanel();
        currentOperation = OperationType.None;
        currentSelectedButton = null;

        combatManager.EndCurrentTurn();
    }

    // 协程方法
    private IEnumerator SelectTargetForAttack() {
        ShowTargetSelectionPanel("攻击");
        CharacterStats target = null;

        AddCombatLog("点击敌人进行攻击...");

        while (target == null) {
            if (Input.GetMouseButtonDown(0)) {
                // 检查是否点击了UI元素
                if (!EventSystem.current.IsPointerOverGameObject()) {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit)) {
                        GameObject hitObject = hit.collider.gameObject;
                        CharacterStats hitCharacter = hitObject.GetComponent<CharacterStats>();

                        if (hitCharacter != null && hitObject.CompareTag("Enemy") && hitCharacter.currentHitPoints > 0) {
                            target = hitCharacter;
                            AddCombatLog($"选择攻击目标: {target.characterName}");
                        }
                        else {
                            AddCombatLog("无效目标，请选择一个活着的敌人");
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                AddCombatLog("取消攻击");
                break;
            }

            yield return null;
        }

        // 隐藏目标选择面板
        if (targetSelectionPanel != null) {
            targetSelectionPanel.SetActive(false);
        }

        // 清理范围指示器
        if (RangeManager.Instance != null) {
            RangeManager.Instance.ClearAllIndicators();
        }
        if (target != null && combatManager != null) {
            currentOperation = OperationType.None;
            currentSelectedButton = null;

            yield return StartCoroutine(combatManager.ExecuteAttack(selectedCharacter, target, AttackType.Melee, "str", "1d8", DamageType.Slashing));

            if (combatManager.currentCharacter == selectedCharacter) {
                ShowActionPanel(selectedCharacter);
                UpdateActionButtons();
            }
        }
        else {
            // 如果取消了操作，恢复UI状态
            currentOperation = OperationType.None;
            currentSelectedButton = null;
            if (combatManager.currentCharacter == selectedCharacter) {
                ShowActionPanel(selectedCharacter);
                UpdateActionButtons();
            }
        }
    }

    private IEnumerator SelectDestinationForMove() {
        bool destinationSelected = false;
        Vector3 destination = Vector3.zero;

        AddCombatLog("点击地面选择移动目标...");

        while (!destinationSelected) {
            if (Input.GetMouseButtonDown(0)) {
                // 检查是否点击了UI元素
                if (!EventSystem.current.IsPointerOverGameObject()) {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

                    if (groundPlane.Raycast(ray, out float distance)) {
                        destination = ray.GetPoint(distance);
                        destinationSelected = true;
                        AddCombatLog($"选择移动目标: {destination}");
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                AddCombatLog("取消移动");
                yield break;
            }

            yield return null;
        }

        // 隐藏目标选择面板
        if (targetSelectionPanel != null) {
            targetSelectionPanel.SetActive(false);
        }

        // 清理范围指示器
        if (RangeManager.Instance != null) {
            RangeManager.Instance.ClearAllIndicators();
        }

        // 执行移动
        if (combatManager != null) {
            currentOperation = OperationType.None;
            currentSelectedButton = null;

            // 这里假设移动不需要消耗动作点，如果需要，需在CombatManager中处理
            yield return StartCoroutine(combatManager.ExecuteMovement(selectedCharacter, destination));

            // 移动后自动结束回合
            combatManager.EndCurrentTurn();
        }

        yield break; // 确保返回
    }

    private void EnsureUIElementsVisible() {
        if (actionPanel != null) actionPanel.SetActive(actionPanel.activeSelf);
        if (spellPanel != null) spellPanel.SetActive(spellPanel.activeSelf);
        if (targetSelectionPanel != null) targetSelectionPanel.SetActive(targetSelectionPanel.activeSelf);
    }

    private void EnsureActionPanelRaycaster() {
        if (actionPanel != null && actionPanel.GetComponent<GraphicRaycaster>() == null) {
            actionPanel.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureButtonsInteractable() {
        UpdateActionButtons();
    }

    private void CheckAllColliders() {
        Collider[] all = FindObjectsOfType<Collider>();
        Debug.Log($"场景中共有 {all.Length} 个碰撞体");
    }

    private void UpdateActionButtons() {
        if (selectedCharacter == null) return;
        DND5E.ActionSystem asys = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (attackButton != null) attackButton.interactable = asys != null && asys.hasAction;
        if (moveButton != null) moveButton.interactable = asys != null && asys.hasMovement;
        if (dashButton != null) dashButton.interactable = asys != null && asys.hasAction;
        if (dodgeButton != null) dodgeButton.interactable = asys != null && asys.hasReaction;
        if (spellButton != null) {
            SpellSystem ss = selectedCharacter.GetComponent<SpellSystem>();
            spellButton.interactable = ss != null && ss.spellList != null && ss.spellList.knownSpells.Count > 0;
        }
        if (endTurnButton != null) endTurnButton.interactable = true;
    }

    private void ResetCurrentOperation() {
        currentOperation = OperationType.None;
        if (currentSelectedButton != null) currentSelectedButton.interactable = true;
        currentSelectedButton = null;
        if (RangeManager.Instance != null) RangeManager.Instance.ClearAllIndicators();
        if (actionPanel != null) actionPanel.SetActive(true);
        if (spellPanel != null) spellPanel.SetActive(false);
        if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);
    }

    private System.Collections.IEnumerator SelectTargetForDashAttack() {
        // TODO: 实现冲刺攻击目标选择逻辑
        yield break;
    }

    private System.Collections.IEnumerator ConfirmDodgeAction() {
        // TODO: 实现闪避确认逻辑
        yield break;
    }

    private void ShowSpellPanel() {
        if (spellPanel != null) spellPanel.SetActive(true);
        if (actionPanel != null) actionPanel.SetActive(false);
    }

    private void ShowTargetSelectionPanel(string actionType) {
        if (targetSelectionPanel != null) targetSelectionPanel.SetActive(true);
    }
}
