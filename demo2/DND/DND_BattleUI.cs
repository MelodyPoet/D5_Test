using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DND5E;
using UnityEngine.EventSystems;
using DG.Tweening;
using Spine.Unity;

public class DND_BattleUI : MonoBehaviour
{
    // 单例实例
    public static DND_BattleUI Instance { get; private set; }

    // 当前选中的按钮
    private Button currentSelectedButton = null;

    // 当前操作类型
    private enum OperationType
    {
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

    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化组件
        combatManager = FindObjectOfType<CombatManager>();
    }

    void Start()
    {
        Debug.Log("【ActionPanel调试】DND_BattleUI Start方法开始");
        
        // 如果在Awake中没有找到CombatManager，在Start中重新查找
        if (combatManager == null)
        {
            Debug.Log("【ActionPanel调试】combatManager在Awake中为null，在Start中重新查找");
            combatManager = FindObjectOfType<CombatManager>();
        }
        
        InitializeUI();
        EnsureUIElementsVisible();
        
        if (combatManager != null)
        {
            Debug.Log("【ActionPanel调试】注册OnTurnStart事件监听器");
            combatManager.OnTurnStart += OnTurnStart;
            combatManager.OnCombatEnd += OnCombatEnd;
        }
        else
        {
            Debug.LogError("【ActionPanel调试】combatManager仍然为null，无法注册事件!");
            Debug.LogError("【ActionPanel调试】请检查场景中是否有CombatManager或DND_BattleSceneSetup组件");
        }

        EnsureActionPanelRaycaster();
        EnsureButtonsInteractable();
        
        Debug.Log($"【ActionPanel调试】actionPanel引用状态: {(actionPanel != null ? "正常" : "为null")}");
        
        Invoke(nameof(CheckAllColliders), 1.0f);
        Invoke(nameof(EnsureActionPanelRaycaster), 2.0f);
    }

    private void InitializeUI()
    {
        // 隐藏所有面板
        if (actionPanel != null)
            actionPanel.SetActive(false);
        if (spellPanel != null)
            spellPanel.SetActive(false);
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);

        // 设置按钮监听
        if (attackButton != null)
        {
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(OnAttackButtonClicked);
        }

        if (moveButton != null)
        {
            moveButton.onClick.RemoveAllListeners();
            moveButton.onClick.AddListener(OnMoveButtonClicked);
        }

        if (dashButton != null)
        {
            dashButton.onClick.RemoveAllListeners();
            dashButton.onClick.AddListener(OnDashButtonClicked);
        }

        if (dodgeButton != null)
        {
            dodgeButton.onClick.RemoveAllListeners();
            dodgeButton.onClick.AddListener(OnDodgeButtonClicked);
        }

        if (spellButton != null)
        {
            spellButton.onClick.RemoveAllListeners();
            spellButton.onClick.AddListener(OnSpellButtonClicked);
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        }

        EnsureRaycastSettings();
        UpdateCombatLogText();
    }

    public void UpdateCharacterStatusUI(CharacterStats character)
    {
        if (character == null) return;

        if (characterStatusUIs.ContainsKey(character))
        {
            GameObject statusUI = characterStatusUIs[character];
            if (statusUI != null)
            {
                // 更新血量条
                Transform hpSliderTrans = statusUI.transform.Find("HealthSlider");
                if (hpSliderTrans != null)
                {
                    Slider hpSlider = hpSliderTrans.GetComponent<Slider>();
                    if (hpSlider != null)
                    {
                        float healthPercentage = (float)character.currentHitPoints / character.maxHitPoints;
                        hpSlider.value = Mathf.Clamp01(healthPercentage);
                    }
                }
            }
        }
    }

    public void RegisterCharacterStatusUI(CharacterStats character)
    {
        if (character == null) return;

        // 角色已注册则跳过
        if (characterStatusUIs.ContainsKey(character))
        {
            Debug.Log($"角色 {character.characterName} 的UI已注册，跳过");
            return;
        }

        // 尝试设置UI，但不影响角色生成
        try
        {
            if (character.gameObject.CompareTag("Player") || character.gameObject.CompareTag("Ally"))
            {
                SetupPlayerStatusUI(character);
            }
            else if (character.gameObject.CompareTag("Enemy"))
            {
                SetupEnemyStatusUI(character);
            }
            else
            {
                Debug.LogWarning($"角色 {character.characterName} 的标签 '{character.gameObject.tag}' 不是 Player、Ally 或 Enemy");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"为角色 {character.characterName} 设置状态UI失败，但角色仍将正常参与战斗: {e.Message}");
            // 即使UI设置失败，也要在字典中记录这个角色，避免重复尝试
            characterStatusUIs[character] = null;
        }
    }

    private void SetupPlayerStatusUI(CharacterStats character)
    {
        try
        {
            // 尝试查找场景中的Status Canvas
            GameObject statusCanvasObj = GameObject.Find("Status");
            if (statusCanvasObj == null)
            {
                Debug.LogWarning($"无法为 {character.characterName} 找到Status Canvas，跳过UI设置");
                characterStatusUIs[character] = null; // 记录但设为null
                return;
            }

            Canvas statusCanvas = statusCanvasObj.GetComponent<Canvas>();
            if (statusCanvas == null)
            {
                Debug.LogWarning($"Status游戏对象上没有Canvas组件，跳过 {character.characterName} 的UI设置");
                characterStatusUIs[character] = null;
                return;
            }

            // 在Status Canvas下查找CharacterStatusTemplate
            Transform characterStatusTransform = statusCanvas.transform.Find("CharacterStatusTemplate");
            if (characterStatusTransform == null)
            {
                Debug.LogWarning($"无法在Status Canvas下找到CharacterStatusTemplate，跳过 {character.characterName} 的UI设置");
                characterStatusUIs[character] = null;
                return;
            }

            GameObject statusUI = characterStatusTransform.gameObject;

            // 确保UI对象是激活的
            statusUI.SetActive(true);

            // 设置名称
            Transform nameTextTrans = statusUI.transform.Find("NameText");
            if (nameTextTrans != null)
            {
                nameTextTrans.gameObject.SetActive(true);
                Text nameText = nameTextTrans.GetComponent<Text>();
                if (nameText != null)
                {
                    nameText.text = character.characterName;
                }
            }

            // 设置生命值
            Transform hpSliderTrans = statusUI.transform.Find("HealthSlider");
            if (hpSliderTrans != null)
            {
                hpSliderTrans.gameObject.SetActive(true);
                Slider hpSlider = hpSliderTrans.GetComponent<Slider>();
                if (hpSlider != null)
                {
                    hpSlider.maxValue = 1.0f;
                    float healthPercentage = (float)character.currentHitPoints / character.maxHitPoints;
                    healthPercentage = Mathf.Clamp01(healthPercentage);
                    hpSlider.value = healthPercentage;
                }
            }

            // 注册到字典
            characterStatusUIs[character] = statusUI;
            UpdateCharacterStatusUI(character);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"为 {character.characterName} 设置玩家状态UI时出错，但不影响角色参与战斗: {e.Message}");
            characterStatusUIs[character] = null;
        }
    }

    private void SetupEnemyStatusUI(CharacterStats character)
    {
        try
        {
            // 尝试查找场景中的Status Canvas
            GameObject statusCanvasObj = GameObject.Find("Status");
            if (statusCanvasObj == null)
            {
                Debug.LogError("无法在场景中找到Status Canvas");
                return;
            }

            Canvas statusCanvas = statusCanvasObj.GetComponent<Canvas>();
            if (statusCanvas == null)
            {
                Debug.LogError("Status游戏对象上没有Canvas组件");
                return;
            }

            // 在Status Canvas下查找EnemyStatusTemplate
            Transform enemyStatusTransform = statusCanvas.transform.Find("EnemyStatusTemplate");
            if (enemyStatusTransform == null)
            {
                Debug.LogError("无法在Status Canvas下找到EnemyStatusTemplate");
                return;
            }

            GameObject statusUI = enemyStatusTransform.gameObject;
            statusUI.SetActive(true);

            // 设置名称和生命值
            Transform nameTextTrans = statusUI.transform.Find("NameText");
            if (nameTextTrans != null)
            {
                Text nameText = nameTextTrans.GetComponent<Text>();
                if (nameText != null)
                {
                    nameText.text = character.characterName;
                }
            }

            Transform hpSliderTrans = statusUI.transform.Find("HealthSlider");
            if (hpSliderTrans != null)
            {
                Slider hpSlider = hpSliderTrans.GetComponent<Slider>();
                if (hpSlider != null)
                {
                    hpSlider.maxValue = 1.0f;
                    float healthPercentage = (float)character.currentHitPoints / character.maxHitPoints;
                    hpSlider.value = Mathf.Clamp01(healthPercentage);
                }
            }

            characterStatusUIs[character] = statusUI;
            UpdateCharacterStatusUI(character);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"为 {character.characterName} 设置敌人状态UI时出错: {e.Message}");
        }
    }

    // 添加战斗日志
    public void AddCombatLog(string message)
    {
        combatLogEntries.Add(message);
        UpdateCombatLogText();
    }

    private void UpdateCombatLogText()
    {
        if (combatLogText != null)
        {
            string logContent = string.Join("\n", combatLogEntries);
            combatLogText.text = logContent;
        }
    }

    private void EnsureRaycastSettings()
    {
        // 确保UI组件有正确的射线检测设置
        if (combatLogScrollRect != null)
        {
            ReplaceWithPassThroughRaycaster(combatLogScrollRect.gameObject);
        }
    }

    private void ReplaceWithPassThroughRaycaster(GameObject uiObject)
    {
        // 替换射线检测器
        GraphicRaycaster existingRaycaster = uiObject.GetComponent<GraphicRaycaster>();
        if (existingRaycaster != null)
        {
            Destroy(existingRaycaster);
        }
    }

    public void UpdateCharacterInfo(CharacterStats character)
    {
        if (character == null || characterInfoText == null) return;

        string info = $"角色: {character.characterName}\n";
        info += $"生命: {character.currentHitPoints}/{character.maxHitPoints}\n";
        info += $"护甲: {character.armorClass}\n";
        
        characterInfoText.text = info;
    }

    // 显示行动面板
    public void ShowActionPanel(CharacterStats character)
    {
        Debug.Log($"【ActionPanel调试】ShowActionPanel被调用，角色: {character?.characterName}, actionPanel是否为null: {actionPanel == null}");
        
        if (character == null) return;

        selectedCharacter = character;
        UpdateCharacterInfo(character);

        if (actionPanel != null)
        {
            Debug.Log($"【ActionPanel调试】激活actionPanel，当前状态: {actionPanel.activeInHierarchy}");
            actionPanel.SetActive(true);
            
            // 确保所有按钮可见
            if (attackButton != null) attackButton.gameObject.SetActive(true);
            if (moveButton != null) moveButton.gameObject.SetActive(true);
            if (dashButton != null) dashButton.gameObject.SetActive(true);
            if (dodgeButton != null) dodgeButton.gameObject.SetActive(true);
            if (spellButton != null) spellButton.gameObject.SetActive(true);
            if (endTurnButton != null)
            {
                endTurnButton.gameObject.SetActive(true);
                endTurnButton.interactable = true;
            }
        }
        else
        {
            Debug.LogError("【ActionPanel调试】actionPanel为null！请检查UI设置");
        }

        // 隐藏其他面板
        if (spellPanel != null) spellPanel.SetActive(false);
        if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);

        UpdateActionButtons();
        
        Debug.Log($"【ActionPanel调试】ShowActionPanel完成，actionPanel激活状态: {actionPanel?.activeInHierarchy}");
    }

    // 隐藏行动面板
    public void HideActionPanel()
    {
        if (actionPanel != null) actionPanel.SetActive(false);
        if (spellPanel != null) spellPanel.SetActive(false);
        if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);
    }

    private void OnTurnStart(CharacterStats character)
    {
        Debug.Log($"【ActionPanel调试】角色 {character.characterName} 的回合开始，标签: {character.gameObject.tag}");
        
        if (turnInfoText != null)
        {
            turnInfoText.gameObject.SetActive(true);
            turnInfoText.text = $"{character.GetDisplayName()}的回合";
        }

        AddCombatLog($"--- {character.GetDisplayName()} 的回合开始 ---");

        if (character.gameObject.CompareTag("Player"))
        {
            Debug.Log($"【ActionPanel调试】检测到玩家角色，显示行动面板");
            ShowActionPanel(character);
        }
        else
        {
            Debug.Log($"【ActionPanel调试】非玩家角色，隐藏行动面板");
            HideActionPanel();
        }
    }

    private void OnCombatEnd()
    {
        Debug.Log("战斗结束");
        
        HideActionPanel();
        
        if (turnInfoText != null)
        {
            turnInfoText.text = "战斗结束！";
        }        // 更新所有角色状态UI
        foreach (var kvp in characterStatusUIs)
        {
            UpdateCharacterStatusUI(kvp.Key);
        }
    }

    // 按钮点击事件
    private void OnAttackButtonClicked()
    {
        if (selectedCharacter == null) return;

        // 检查角色是否有可用的攻击动作
        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasAction)
        {
            AddCombatLog("没有可用的攻击动作");
            return;
        }

        currentSelectedButton = attackButton;
        currentOperation = OperationType.Attack;

        if (RangeManager.Instance != null)
        {
            RangeManager.Instance.ShowAttackRange(selectedCharacter, AttackType.Melee);
        }
        else
        {
            Debug.LogError("RangeManager.Instance 为null！请确保场景中有RangeManager组件");
            AddCombatLog("攻击系统尚未准备就绪，请稍后再试");
            ResetCurrentOperation();
            return;
        }
          StartCoroutine(SelectTargetForAttack());
    }    private void OnMoveButtonClicked()
    {
        if (selectedCharacter == null) return;

        // 检查角色是否有可用的移动动作
        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasMovement)
        {
            AddCombatLog("没有可用的移动动作");
            return;
        }

        currentSelectedButton = moveButton;
        currentOperation = OperationType.Move;

        if (RangeManager.Instance != null)
        {
            RangeManager.Instance.ShowMovementRange(selectedCharacter);
            StartCoroutine(SelectDestinationForMove());
        }
        else
        {
            Debug.LogError("RangeManager.Instance 为null！请确保场景中有RangeManager组件");
            AddCombatLog("移动系统尚未准备就绪，请稍后再试");
            ResetCurrentOperation();
        }
    }    private void OnDashButtonClicked()
    {
        if (selectedCharacter == null) return;

        // 检查角色是否有可用的主要动作（冲刺消耗主要动作）
        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasAction)
        {
            AddCombatLog("没有可用的主要动作进行冲刺");
            return;
        }

        currentSelectedButton = dashButton;
        currentOperation = OperationType.Dash;

        if (RangeManager.Instance != null)
        {
            RangeManager.Instance.ShowMovementRange(selectedCharacter, true);
            StartCoroutine(SelectTargetForDashAttack());
        }
        else
        {            Debug.LogError("RangeManager.Instance 为null！请确保场景中有RangeManager组件");
            AddCombatLog("冲刺系统尚未准备就绪，请稍后再试");
            ResetCurrentOperation();
        }
    }

    private void OnDodgeButtonClicked()
    {
        if (selectedCharacter == null) return;

        // 检查角色是否有可用的反应动作（闪避消耗反应动作）
        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || !actionSystem.hasReaction)
        {
            AddCombatLog("没有可用的反应动作进行闪避");
            return;
        }

        currentSelectedButton = dodgeButton;
        currentOperation = OperationType.Dodge;        StartCoroutine(ConfirmDodgeAction());
    }

    private void OnSpellButtonClicked()
    {
        if (selectedCharacter == null) return;

        // 检查角色是否有可用的动作（施法消耗动作或奖励动作）
        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem == null || (!actionSystem.hasAction && !actionSystem.hasBonusAction))
        {
            AddCombatLog("没有可用的动作进行施法");
            return;
        }

        currentSelectedButton = spellButton;
        currentOperation = OperationType.Spell;

        SpellSystem spellSystem = selectedCharacter.GetComponent<SpellSystem>();
        if (spellSystem == null || spellSystem.spellList == null || spellSystem.spellList.knownSpells.Count == 0)
        {
            AddCombatLog("你没有掌握任何法术");
            ResetCurrentOperation();
            return;
        }        AddCombatLog("准备施放法术...");
        ShowSpellPanel();
    }

    private void OnEndTurnButtonClicked()
    {
        if (selectedCharacter == null || combatManager == null) return;

        AddCombatLog($"--- {selectedCharacter.characterName} 的回合结束 ---");        HideActionPanel();
        currentOperation = OperationType.None;
        currentSelectedButton = null;

        combatManager.EndCurrentTurn();
    }

    // 协程方法
    private IEnumerator SelectTargetForAttack()
    {
        ShowTargetSelectionPanel("攻击");
        CharacterStats target = null;

        AddCombatLog("点击敌人进行攻击...");

        while (target == null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 检查是否点击了UI元素
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        GameObject hitObject = hit.collider.gameObject;
                        CharacterStats hitCharacter = hitObject.GetComponent<CharacterStats>();
                        
                        if (hitCharacter != null && hitObject.CompareTag("Enemy") && hitCharacter.currentHitPoints > 0)
                        {
                            target = hitCharacter;
                            AddCombatLog($"选择攻击目标: {target.characterName}");
                        }
                        else
                        {
                            AddCombatLog("无效目标，请选择一个活着的敌人");
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                AddCombatLog("取消攻击");
                break;
            }

            yield return null;
        }

        // 隐藏目标选择面板
        if (targetSelectionPanel != null)
        {
            targetSelectionPanel.SetActive(false);
        }

        // 清理范围指示器
        if (RangeManager.Instance != null)
        {
            RangeManager.Instance.ClearAllIndicators();
        }        if (target != null && combatManager != null)
        {
            currentOperation = OperationType.None;
            currentSelectedButton = null;

            yield return StartCoroutine(combatManager.ExecuteAttack(selectedCharacter, target, AttackType.Melee, "str", "1d8", DamageType.Slashing));

            if (combatManager.currentCharacter == selectedCharacter)
            {
                ShowActionPanel(selectedCharacter);
                UpdateActionButtons();            }
        }
        else
        {
            // 如果取消了操作，恢复UI状态
            currentOperation = OperationType.None;
            currentSelectedButton = null;
            if (combatManager.currentCharacter == selectedCharacter)
            {
                ShowActionPanel(selectedCharacter);
                UpdateActionButtons();
            }
        }
    }

    private IEnumerator SelectDestinationForMove()
    {
        bool destinationSelected = false;
        Vector3 destination = Vector3.zero;

        AddCombatLog("点击地面选择移动目标...");

        while (!destinationSelected)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 检查是否点击了UI元素
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                    
                    if (groundPlane.Raycast(ray, out float distance))
                    {
                        destination = ray.GetPoint(distance);
                        destinationSelected = true;
                        AddCombatLog($"选择移动目标: {destination}");
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                AddCombatLog("取消移动");
                break;
            }

            yield return null;
        }

        // 清理范围指示器
        if (RangeManager.Instance != null)
        {
            RangeManager.Instance.ClearAllIndicators();
        }        if (destinationSelected && combatManager != null)
        {
            currentOperation = OperationType.None;
            currentSelectedButton = null;

            yield return StartCoroutine(combatManager.ExecuteMovement(selectedCharacter, destination));

            if (combatManager.currentCharacter == selectedCharacter)
            {
                ShowActionPanel(selectedCharacter);
                UpdateActionButtons();
            }        }
        else
        {
            // 如果取消了操作，恢复UI状态
            currentOperation = OperationType.None;
            currentSelectedButton = null;
            if (combatManager.currentCharacter == selectedCharacter)
            {
                ShowActionPanel(selectedCharacter);
                UpdateActionButtons();
            }
        }
    }

    private IEnumerator SelectTargetForDashAttack()
    {
        // 冲刺攻击的目标选择逻辑
        yield return SelectTargetForAttack();
    }

    private IEnumerator ConfirmDodgeAction()
    {
        // 闪避确认逻辑
        if (selectedCharacter != null)
        {            DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
            if (actionSystem != null && actionSystem.hasAction)
            {
                currentOperation = OperationType.None;
                currentSelectedButton = null;

                AddCombatLog($"{selectedCharacter.characterName} 进入防御姿态");
                UpdateActionButtons();
                ShowActionPanel(selectedCharacter);
            }
        }
        yield return null;
    }

    private void ShowSpellPanel()
    {
        if (spellPanel != null)
        {
            spellPanel.SetActive(true);
        }
    }    private void ResetCurrentOperation()
    {
        currentOperation = OperationType.None;
        currentSelectedButton = null;
    }

    private void UpdateActionButtons()
    {
        // 更新按钮状态的逻辑
        if (selectedCharacter == null) return;

        // 根据角色状态更新按钮的可交互性
        DND5E.ActionSystem actionSystem = selectedCharacter.GetComponent<DND5E.ActionSystem>();
        if (actionSystem != null)
        {
            if (attackButton != null)
                attackButton.interactable = actionSystem.hasAction;
            if (moveButton != null)
                moveButton.interactable = actionSystem.hasMovement;
            if (dashButton != null)
                dashButton.interactable = actionSystem.hasAction;
            if (dodgeButton != null)
                dodgeButton.interactable = actionSystem.hasAction;
            if (spellButton != null)
                spellButton.interactable = actionSystem.hasAction;
        }
    }

    private void EnsureUIElementsVisible()
    {
        Debug.Log("确保UI元素可见...");
        
        if (endTurnButton != null)
        {
            endTurnButton.interactable = true;
        }
    }

    private void EnsureActionPanelRaycaster()
    {
        if (actionPanel == null) return;

        GraphicRaycaster raycaster = actionPanel.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            actionPanel.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureButtonsInteractable()
    {
        // 确保按钮可交互
        UpdateActionButtons();
    }

    private void EnsureButtonRaycastTarget(Button button)
    {
        if (button != null)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
            }
        }
    }

    void Update()
    {
        // 更新逻辑
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ResetCurrentOperation();
            HideActionPanel();
        }
    }

    void OnEnable()
    {
        InitializeUI();
        UpdateActionButtons();
    }

    void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.OnTurnStart -= OnTurnStart;
            combatManager.OnCombatEnd -= OnCombatEnd;
        }
    }

    private void CheckAllColliders()
    {
        Collider[] allColliders = FindObjectsOfType<Collider>();
        Debug.Log($"场景中共有 {allColliders.Length} 个碰撞体");
    }

    private void ShowTargetSelectionPanel(string actionType)
    {
        if (targetSelectionPanel != null)
        {
            targetSelectionPanel.SetActive(true);
        }
    }

    private IEnumerator HighlightEnemyWhileHovering(SkeletonAnimation skeleton)
    {
        if (skeleton == null || skeleton.Skeleton == null)
            yield break;

        Color highlightColor = new Color(1f, 0.5f, 0.5f, 1f);
        Color originalColor = skeleton.Skeleton.GetColor();

        skeleton.Skeleton.SetColor(highlightColor);

        while (currentHighlightedEnemy != null && 
               (currentOperation == OperationType.Attack || currentOperation == OperationType.Spell || currentOperation == OperationType.Dash))
        {
            yield return null;
        }

        if (skeleton != null && skeleton.Skeleton != null)
        {
            skeleton.Skeleton.SetColor(originalColor);
        }
    }
}