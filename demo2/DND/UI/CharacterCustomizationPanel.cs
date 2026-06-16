﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using demo2.DND.InventoryTetris;

namespace demo2.DND.UI
{
    /// <summary>
    /// 角色换装UI面板 - 管理角色皮肤的定制化界面
    ///
    /// 职责：
    /// 1. 创建 UI 预览角色（副本）
    /// 2. 显示部件分类标签页（内层装饰 + 外层装备，分两区）
    /// 3. 显示分类下的所有皮肤 icon（可点击）
    /// 4. 处理玩家交互（点击 icon 更新预览）
    /// 5. 提供动画预览（从 CharacterAnimationConfig 读取）
    /// 6. 提供属性调整区（六维属性 + 等级）
    /// 7. 同步皮肤配置 + 属性 + 装备物品到游戏角色（确认按钮）
    /// 8. 支持外部调用：直接设置某个角色的皮肤
    ///
    /// 分层：
    ///   内层（基础装饰层，仅外观）：SkinBase、Hair、Eyes、Mouth
    ///   外层（装备外观层，与游戏逻辑关联）：Helmet、Armor、Gloves、Boots、Belt、Cloak、
    ///     MainHandWeapon、OffHandShield
    ///   注意：测试期外层也在面板中显示，后续正式版由背包系统驱动
    ///
    /// 确认时同步内容：
    ///   - 外观皮肤 → gameCharacter.CharacterAppearance
    ///   - 属性值 → gameCharacter.CharacterStats（直接修改运行时字段 + RequestRecalculateStats）
    ///   - 装备物品 → gameCharacter.CharacterInventory（创建 ItemInstance 加入背包）
    ///                → gameCharacter.CharacterEquipment（装备到对应槽位）
    /// </summary>
    public class CharacterCustomizationPanel : MonoBehaviour
    {
        [Header("配置数据（通过 Inspector 拖入）")]
        [SerializeField] private SkinConfig skinConfig;
        [SerializeField] private GameObject characterPrefab;  // 角色预制体（包含 SkeletonAnimation、CharacterAppearance 等）
        [SerializeField] private CharacterAnimationConfig animationConfig;  // 动画配置
        [SerializeField] private SkeletonAnimation gameCharacter;  // 游戏中的实际角色，用于最终同步

        [Header("UI 容器（通过 Inspector 拖入）")]
        [SerializeField] private RawImage characterDisplayImage;      // 左侧：角色显示区域 (使用 RawImage)
        [SerializeField] private Camera uiCharacterCamera;            // 用于渲染UI角色的专用摄像机
        [SerializeField] private Transform categoryTabsContainer;      // 右侧：标签页容器
        [SerializeField] private Transform iconGridContainer;          // 右侧：icon 列表容器
        [SerializeField] private Transform animationButtonsContainer;  // 下方：动画按钮容器

        [Header("UI 预制体")]
        [SerializeField] private Button categoryTabPrefab;   // 标签页按钮预制体
        [SerializeField] private Button iconButtonPrefab;    // icon 按钮预制体
        [SerializeField] private Button animationButtonPrefab; // 动画按钮预制体

        [Header("区域标题预制体（可选，用于分组）")]
        [SerializeField] private GameObject sectionHeaderPrefab;  // 区域标题文本预制体（如 "=== 内层装饰 ==="）

        [Header("确认/取消按钮")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Tuning")]
        [SerializeField, Tooltip("摄像机自动缩放时的边距，1.0 表示无边距，1.1 表示 10% 的边距。值越小，角色越大。")]
        private float cameraFitMargin = 1.1f;

        /// <summary>
        /// 锁定后的摄像机参数（初始化时计算一次，后续不再动态变化，保证预览大小一致）
        /// </summary>
        private float lockedOrthographicSize;
        private Vector3 lockedCameraPosition;
        private bool cameraLocked = false;

        [Header("测试开关")]
        [SerializeField, Tooltip("测试期启用：勾选后在换装面板中显示装备外观部位（后续正式版由背包系统驱动）")]
        private bool showEquipmentPartsInTest = true;

        [Header("职业选择")]
        [SerializeField, Tooltip("可选的职业模板列表（拖入不同职业的 CharacterTemplate SO）")]
        private List<CharacterTemplate> availableClasses = new List<CharacterTemplate>();
        [SerializeField, Tooltip("职业按钮容器（用于放置职业选择按钮）")]
        private Transform classButtonContainer;
        [SerializeField, Tooltip("职业按钮预制体")]
        private Button classButtonPrefab;
        [SerializeField, Tooltip("当前选中职业的显示文本")]
        private Text selectedClassText;

        [Header("属性调整区（27点购点法）")]
        [SerializeField, Tooltip("属性调整区根节点（包含六维属性+/-按钮和剩余点数显示）")]
        private GameObject statsAdjustPanel;
        [SerializeField, Tooltip("剩余可分配点数显示文本")]
        private Text availablePointsText;

        [Header("属性行 - 共用预制体（一行包含：属性名Text + 值Text + 减号Btn + 加号Btn + 调整值Text）")]
        [SerializeField, Tooltip("属性行预制体（需挂 StatRow 组件）")]
        private StatRow statRowPrefab;
        [SerializeField, Tooltip("属性行父容器")]
        private Transform statRowsContainer;

        [Header("等级设置")]
        [SerializeField, Tooltip("等级显示文本（创建角色固定为1级，后续游戏进程升级）")]
        private Text levelLabelText;

        // 内部引用
        private SkeletonAnimation uiCharacter;
        private CharacterAppearance uiCharacterAppearance;
        private DND_CharacterAdapter uiCharacterAdapter;

        // UI 状态
        private SkinBodyPartType currentCategory = SkinBodyPartType.SkinBase;
        private Dictionary<Button, SkinBodyPartType> categoryTabMap = new Dictionary<Button, SkinBodyPartType>();
        private List<Button> activeCategoryTabs = new List<Button>();
        private List<GameObject> activeSectionHeaders = new List<GameObject>();
        private List<Button> activeIconButtons = new List<Button>();
        private List<Button> activeAnimationButtons = new List<Button>();
        private List<Button> activeClassButtons = new List<Button>();

        // 购点系统（核心）
        private PointBuySystem pointBuy;
        private CharacterTemplate selectedTemplate; // 当前选中的职业模板

        // 运行时生成的 6 行属性控件
        private List<StatRow> activeStatRows = new List<StatRow>();

        // 初始等级固定为1（后续由游戏进程升级，创建角色时不输入）
        private const int StartLevel = 1;

        // 装备→物品映射（用户在面板中选择装备外观后，缓存对应的 ItemBaseSO；确认时创建 ItemInstance）
        // Key = SkinBodyPartType (装备部位), Value = 关联的 ItemBaseSO（从 SkinPartEntry.linkedItemSO 获取）
        private Dictionary<SkinBodyPartType, ItemBaseSO> pendingEquipmentItems = new Dictionary<SkinBodyPartType, ItemBaseSO>();

        // 事件
        public event Action OnConfirm;
        public event Action OnCancel;

        private void OnEnable()
        {
            Debug.Log("[CharacterCustomizationPanel] UI 面板打开");

            // 每次打开面板时重置摄像机锁定，让首次 FitCamera 重新计算
            cameraLocked = false;

            try
            {
                // 初始化 UI 角色
                InitializeUICharacter();

                // 初始化分类标签页
                InitializeCategoryTabs();

                // 初始化动画按钮
                InitializeAnimationButtons();

                // 初始化职业选择区
                InitializeClassSelection();

                // 初始化属性调整区（27点购点法）
                InitializePointBuyPanel();

                // 初始化确认/取消按钮
                if (confirmButton != null)
                    confirmButton.onClick.AddListener(OnConfirmClicked);
                if (cancelButton != null)
                    cancelButton.onClick.AddListener(OnCancelClicked);

                // 默认显示第一个分类（SkinBase）
                ShowCategory(SkinBodyPartType.SkinBase);

                Debug.Log("[CharacterCustomizationPanel] 初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharacterCustomizationPanel] 初始化失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void OnDisable()
        {
            Debug.Log("[CharacterCustomizationPanel] UI 面板关闭");

            try
            {
                // 清理事件监听
                if (confirmButton != null)
                    confirmButton.onClick.RemoveListener(OnConfirmClicked);
                if (cancelButton != null)
                    cancelButton.onClick.RemoveListener(OnCancelClicked);

                // 清理属性行（销毁实例化的 StatRow）
                foreach (var row in activeStatRows)
                {
                    if (row != null) Destroy(row.gameObject);
                }
                activeStatRows.Clear();

                // 清理购点事件
                if (pointBuy != null)
                {
                    pointBuy.OnStatChanged -= OnPointBuyStatChanged;
                    pointBuy.OnPointsChanged -= OnPointBuyPointsChanged;
                    pointBuy.OnRacialBonusChanged -= OnPointBuyRacialBonusChanged;
                }

                // 清理职业按钮
                foreach (var btn in activeClassButtons)
                {
                    if (btn != null) btn.onClick.RemoveAllListeners();
                }

                // 清理标签页
                foreach (var tab in activeCategoryTabs)
                {
                    if (tab != null)
                        tab.onClick.RemoveAllListeners();
                }

                // 清理区域标题
                foreach (var header in activeSectionHeaders)
                {
                    if (header != null)
                        Destroy(header);
                }
                activeSectionHeaders.Clear();

                // 清理 icon 按钮
                foreach (var btn in activeIconButtons)
                {
                    if (btn != null)
                        btn.onClick.RemoveAllListeners();
                }

                // 清理动画按钮
                foreach (var btn in activeAnimationButtons)
                {
                    if (btn != null)
                        btn.onClick.RemoveAllListeners();
                }

                // 清空装备物品缓存
                pendingEquipmentItems.Clear();

                // 销毁 UI 角色
                if (uiCharacter != null)
                {
                    Destroy(uiCharacter.gameObject);
                    uiCharacter = null;
                    uiCharacterAppearance = null;
                    uiCharacterAdapter = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CharacterCustomizationPanel] 清理资源失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化 UI 用的角色预制体副本
        /// </summary>
        private void InitializeUICharacter()
        {
            if (characterPrefab == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] Character Prefab 未设置");
                return;
            }
            if (uiCharacterCamera == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] UI Character Camera 未设置");
                return;
            }
            if (characterDisplayImage == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] Character Display Image 未设置");
                return;
            }

            // 实例化角色到场景根目录，并设置一个远离主场景的位置，例如 (1000, 1000, 1000)
            var charObj = Instantiate(characterPrefab);
            charObj.transform.position = new Vector3(1000, 1000, 1000);
            charObj.name = "UICharacter_Preview";

            // --- 关键步骤：将角色的所有子对象都设置为 "UICharacter" 层 ---
            // (请确保你已经在 Unity 的 Layer 设置中创建了 "UICharacter" 层)
            foreach (var t in charObj.GetComponentsInChildren<Transform>())
            {
                t.gameObject.layer = LayerMask.NameToLayer("UICharacter");
            }

            // 获取必要的组件
            uiCharacter = charObj.GetComponent<SkeletonAnimation>();
            uiCharacterAppearance = charObj.GetComponent<CharacterAppearance>();
            uiCharacterAdapter = charObj.GetComponent<DND_CharacterAdapter>();

            if (uiCharacter == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] Character Prefab 不包含 SkeletonAnimation 组件");
                Destroy(charObj);
                return;
            }

            if (uiCharacterAppearance == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] Character Prefab 不包含 CharacterAppearance 组件");
                Destroy(charObj);
                return;
            }

            // --- 关键步骤：设置摄像机和 Render Texture ---
            // 1. 将摄像机对准角色
            // uiCharacterCamera.transform.position = charObj.transform.position + new Vector3(0, 0, -5); // 从角色前方观察
            // uiCharacterCamera.transform.LookAt(charObj.transform.position);

            // 2. 确保摄像机只渲染 "UICharacter" 层
            uiCharacterCamera.cullingMask = 1 << LayerMask.NameToLayer("UICharacter");

            // 3. 确保 Render Texture 被分配给摄像机和 RawImage
            if (uiCharacterCamera.targetTexture == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] UI Character Camera 没有设置 Target Texture!");
                return;
            }
            characterDisplayImage.texture = uiCharacterCamera.targetTexture;
            characterDisplayImage.color = Color.white; // 确保 RawImage 是不透明的

            // --- 新增：自动调整摄像机视野以适应角色大小 ---
            FitCameraToCharacter(uiCharacter, uiCharacterCamera);

            if (skinConfig == null)
            {
                Debug.LogWarning("[CharacterCustomizationPanel] SkinConfig 未设置，UI 角色皮肤初始化可能失败");
            }

            // 初始化皮肤配置
            try
            {
                uiCharacterAppearance.InitializeAppearance();
                Debug.Log("[CharacterCustomizationPanel] UI 角色皮肤配置初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharacterCustomizationPanel] UI 角色皮肤初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 调整摄像机以容纳角色（仅在首次调用时计算并锁定，后续调用直接复用锁定值）
        /// 这样无论用户怎么切换部件，预览中的角色大小始终保持一致。
        /// </summary>
        private void FitCameraToCharacter(SkeletonAnimation character, Camera cam)
        {
            if (character == null || cam == null) return;

            // 已锁定则直接应用锁定值，不再重新计算
            if (cameraLocked)
            {
                cam.orthographicSize = lockedOrthographicSize;
                cam.transform.position = lockedCameraPosition;
                return;
            }

            // 延迟一帧获取包围盒，确保Spine网格已更新
            StartCoroutine(DelayedFitCamera(character, cam));
        }

        /// <summary>
        /// 强制重新计算并锁定摄像机参数（例如角色预制体更换时需要调用）
        /// </summary>
        public void ResetCameraLock()
        {
            cameraLocked = false;
        }

        private System.Collections.IEnumerator DelayedFitCamera(SkeletonAnimation character, Camera cam)
        {
            // 等待一帧，让Spine的Mesh生成和更新完成
            yield return null;

            var meshRenderer = character.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshRenderer.bounds.size == Vector3.zero)
            {
                Debug.LogWarning("[CharacterCustomizationPanel] 无法获取角色有效的包围盒，自动缩放失败。请确保模型可见。");
                yield break;
            }

            Bounds bounds = meshRenderer.bounds;

            // 对于正交摄像机，根据模型高度调整 Orthographic Size
            float verticalSize = bounds.size.y;
            lockedOrthographicSize = verticalSize / 2f * cameraFitMargin;

            // 将摄像机移动到模型中心点，并保持原有的Z轴距离
            lockedCameraPosition = bounds.center;
            lockedCameraPosition.z = cam.transform.position.z;

            // 应用锁定值
            cam.orthographicSize = lockedOrthographicSize;
            cam.transform.position = lockedCameraPosition;
            cameraLocked = true;

            Debug.Log($"[CharacterCustomizationPanel] 摄像机参数已锁定。角色高度: {verticalSize:F2}, OrthographicSize: {lockedOrthographicSize:F2}, 位置: {lockedCameraPosition}");
        }

        /// <summary>
        /// 初始化分类标签页（分两区：内层装饰 + 外层装备）
        /// </summary>
        private void InitializeCategoryTabs()
        {
            if (categoryTabsContainer == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] categoryTabsContainer 未设置");
                return;
            }

            if (categoryTabPrefab == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] categoryTabPrefab 未设置");
                return;
            }

            // 清空现有的标签页
            foreach (var tab in activeCategoryTabs)
            {
                if (tab != null)
                    Destroy(tab.gameObject);
            }
            foreach (var header in activeSectionHeaders)
            {
                if (header != null)
                    Destroy(header);
            }
            activeCategoryTabs.Clear();
            activeSectionHeaders.Clear();
            categoryTabMap.Clear();

            // === 内层：基础装饰层（仅外观） ===
            CreateSectionHeader("=== 内层装饰 ===");

            var innerParts = new[]
            {
                SkinBodyPartType.SkinBase,
                SkinBodyPartType.Hair,
                SkinBodyPartType.Eyes,
                SkinBodyPartType.Mouth,
            };
            foreach (var partType in innerParts)
            {
                CreateCategoryTab(partType);
            }

            // === 外层：装备外观层（测试期可见；后续正式版由背包系统驱动） ===
            if (showEquipmentPartsInTest)
            {
                CreateSectionHeader("=== 外层装备（测试） ===");

                var outerParts = new[]
                {
                    SkinBodyPartType.Helmet,
                    SkinBodyPartType.Armor,
                    SkinBodyPartType.Gloves,
                    SkinBodyPartType.Boots,
                    SkinBodyPartType.Belt,
                    SkinBodyPartType.Cloak,
                    SkinBodyPartType.MainHandWeapon,
                    SkinBodyPartType.OffHandShield,
                };
                foreach (var partType in outerParts)
                {
                    CreateCategoryTab(partType);
                }
            }

            Debug.Log($"[CharacterCustomizationPanel] 创建了 {activeCategoryTabs.Count} 个分类标签页 (内层+{(showEquipmentPartsInTest ? "外层" : "仅内层")})");
        }

        /// <summary>
        /// 创建区域分隔标题
        /// </summary>
        private void CreateSectionHeader(string title)
        {
            if (sectionHeaderPrefab != null)
            {
                var header = Instantiate(sectionHeaderPrefab, categoryTabsContainer);
                header.name = $"Header_{title}";
                var text = header.GetComponentInChildren<Text>();
                if (text != null) text.text = title;
                activeSectionHeaders.Add(header);
            }
        }

        /// <summary>
        /// 创建单个分类标签按钮
        /// </summary>
        private void CreateCategoryTab(SkinBodyPartType partType)
        {
            var tabBtn = Instantiate(categoryTabPrefab, categoryTabsContainer);
            tabBtn.name = $"Tab_{partType}";

            // 设置按钮文本
            var text = tabBtn.GetComponentInChildren<Text>();
            if (text != null)
                text.text = GetCategoryDisplayName(partType);

            // 添加点击事件
            tabBtn.onClick.AddListener(() => OnCategoryTabClicked(partType));

            activeCategoryTabs.Add(tabBtn);
            categoryTabMap[tabBtn] = partType;
        }

        /// <summary>
        /// 初始化动画按钮
        /// </summary>
        private void InitializeAnimationButtons()
        {
            if (animationButtonsContainer == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] animationButtonsContainer 未设置");
                return;
            }

            if (animationButtonPrefab == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] animationButtonPrefab 未设置");
                return;
            }

            if (animationConfig == null)
            {
                Debug.LogWarning("[CharacterCustomizationPanel] animationConfig 未设置，无法加载动画列表");
                return;
            }

            // 清空现有的动画按钮
            foreach (var btn in activeAnimationButtons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            activeAnimationButtons.Clear();

            // 从 AnimationConfig 读取动画列表
            var animationNames = GetAnimationNamesFromConfig();

            // 为每个动画创建按钮
            foreach (var animName in animationNames)
            {
                var btn = Instantiate(animationButtonPrefab, animationButtonsContainer);
                btn.name = $"Btn_{animName}";

                // 设置按钮文本
                var text = btn.GetComponentInChildren<Text>();
                if (text != null)
                    text.text = animName;

                // 添加点击事件
                btn.onClick.AddListener(() => OnAnimationButtonClicked(animName));

                activeAnimationButtons.Add(btn);
            }

            Debug.Log($"[CharacterCustomizationPanel] 创建了 {activeAnimationButtons.Count} 个动画按钮");
        }

        // ==================== 职业选择区 ====================

        /// <summary>
        /// 初始化职业选择区：为每个可用职业创建按钮
        /// </summary>
        private void InitializeClassSelection()
        {
            if (classButtonContainer == null || classButtonPrefab == null) return;

            // 清理旧按钮
            foreach (var btn in activeClassButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            activeClassButtons.Clear();

            if (availableClasses == null || availableClasses.Count == 0)
            {
                Debug.LogWarning("[CharacterCustomizationPanel] availableClasses 为空，跳过职业选择初始化");
                return;
            }

            // 默认选中第一个职业
            if (selectedTemplate == null && availableClasses.Count > 0)
                SelectClass(availableClasses[0]);

            foreach (var template in availableClasses)
            {
                if (template == null) continue;
                var btn = Instantiate(classButtonPrefab, classButtonContainer);
                btn.name = $"ClassBtn_{template.characterClass}";

                var text = btn.GetComponentInChildren<Text>();
                if (text != null) text.text = GetClassName(template.characterClass);

                btn.onClick.AddListener(() => SelectClass(template));
                activeClassButtons.Add(btn);
            }

            Debug.Log($"[CharacterCustomizationPanel] 创建了 {activeClassButtons.Count} 个职业选择按钮");
        }

        /// <summary>
        /// 选择职业：切换 CharacterTemplate，重置购点并刷新属性显示
        /// </summary>
        private void SelectClass(CharacterTemplate template)
        {
            if (template == null) return;
            selectedTemplate = template;

            if (selectedClassText != null)
                selectedClassText.text = GetClassName(template.characterClass);

            Debug.Log($"[CharacterCustomizationPanel] 选择职业: {template.characterClass}");

            // 如果面板还没初始化（首次选择），先初始化
            if (pointBuy == null)
            {
                InitializePointBuyPanel();
                return;
            }

            // 重置购点系统并加载职业推荐属性
            pointBuy.Reset();
            var defaults = template.GetPointBuyDefaults();
            pointBuy.LoadFromDefaults(defaults[0], defaults[1], defaults[2], defaults[3], defaults[4], defaults[5]);

            // 应用种族加成（变体人类：自选两个不同属性各+1）
            pointBuy.ApplyRacialBonus(template.race);

            // 确保属性行已创建
            if (activeStatRows.Count == 0)
                CreateStatRows();

            // 刷新属性面板 UI
            RefreshAllStatDisplays();
            UpdatePointsDisplay(pointBuy.AvailablePoints);
        }

        /// <summary>
        /// 获取职业中文名
        /// </summary>
        private string GetClassName(CharacterClass cls)
        {
            switch (cls)
            {
                case CharacterClass.Fighter: return "战士";
                case CharacterClass.Wizard: return "法师";
                case CharacterClass.Rogue: return "盗贼";
                case CharacterClass.Cleric: return "牧师";
                case CharacterClass.Ranger: return "游侠";
                case CharacterClass.Barbarian: return "野蛮人";
                case CharacterClass.Paladin: return "圣骑士";
                case CharacterClass.Warlock: return "术士";
                case CharacterClass.Sorcerer: return "术士(蓝)";
                case CharacterClass.Bard: return "诗人";
                case CharacterClass.Druid: return "德鲁伊";
                case CharacterClass.Monk: return "武僧";
                default: return cls.ToString();
            }
        }

        // ==================== 27点购点法属性调整区 ====================

        /// <summary>
        /// 初始化购点属性面板 —— 动态实例化 6 行 StatRow 并绑定
        /// </summary>
        private void InitializePointBuyPanel()
        {
            if (pointBuy == null)
            {
                pointBuy = new PointBuySystem();
                if (selectedTemplate != null)
                {
                    var defaults = selectedTemplate.GetPointBuyDefaults();
                    pointBuy.LoadFromDefaults(defaults[0], defaults[1], defaults[2], defaults[3], defaults[4], defaults[5]);
                }
                else if (gameCharacter != null)
                {
                    var stats = gameCharacter.GetComponent<CharacterStats>();
                    if (stats != null)
                    {
                        pointBuy.LoadFromDefaults(stats.strength, stats.dexterity, stats.constitution,
                            stats.intelligence, stats.wisdom, stats.charisma);
                    }
                }

                // 立即应用种族加成（变体人类：自选两个不同属性各+1，玩家需手动点击分配）
                // 面板上显示未分配的提示，确认时同步到角色
                if (selectedTemplate != null)
                    pointBuy.ApplyRacialBonus(selectedTemplate.race);
                else
                    pointBuy.ApplyRacialBonus(PointBuySystem.RaceType.Human);
            }

            // 订阅购点事件
            pointBuy.OnStatChanged += OnPointBuyStatChanged;
            pointBuy.OnPointsChanged += OnPointBuyPointsChanged;
            pointBuy.OnRacialBonusChanged += OnPointBuyRacialBonusChanged;

            // ---- 动态实例化 6 行属性控件 ----
            CreateStatRows();

            // 等级固定为1（后续游戏进程升级，创建角色时不输入）
            if (levelLabelText != null)
                levelLabelText.text = $"等级: {StartLevel}";

            // 刷新 UI 显示
            RefreshAllStatDisplays();
            UpdatePointsDisplay(pointBuy.AvailablePoints);

            if (gameCharacter == null && statsAdjustPanel != null)
            {
                statsAdjustPanel.SetActive(false);
                Debug.Log("[CharacterCustomizationPanel] gameCharacter 未设置，隐藏属性调整面板");
            }
        }

        /// <summary>
        /// 创建 6 行属性行（STR/DEX/CON/INT/WIS/CHA），绑定 +/- 事件 和 种族加成选择按钮
        /// </summary>
        private void CreateStatRows()
        {
            if (statRowPrefab == null || statRowsContainer == null)
            {
                Debug.LogWarning("[CharacterCustomizationPanel] statRowPrefab 或 statRowsContainer 未设置，跳过属性行创建");
                return;
            }

            // 清理旧行
            foreach (var row in activeStatRows)
            {
                if (row != null) Destroy(row.gameObject);
            }
            activeStatRows.Clear();

            // 六维属性的顺序和显示名
            var statDefs = new (StatType type, string name)[]
            {
                (StatType.Strength,     "力量"),
                (StatType.Dexterity,    "敏捷"),
                (StatType.Constitution, "体质"),
                (StatType.Intelligence, "智力"),
                (StatType.Wisdom,       "感知"),
                (StatType.Charisma,     "魅力"),
            };

            foreach (var def in statDefs)
            {
                var row = Instantiate(statRowPrefab, statRowsContainer);
                row.statType = def.type;

                if (row.labelText != null) row.labelText.text = def.name;
                if (row.minusBtn != null)
                    row.minusBtn.onClick.AddListener(() => pointBuy.DecreaseStat(def.type));
                if (row.plusBtn != null)
                    row.plusBtn.onClick.AddListener(() => pointBuy.IncreaseStat(def.type));

                // 绑定种族加成选择按钮（变体人类：点击分配+1到该属性）
                if (row.racialBonusBtn != null)
                {
                    var capturedType = def.type;
                    row.racialBonusBtn.onClick.AddListener(() => OnRacialBonusBtnClicked(capturedType));
                }

                activeStatRows.Add(row);
            }
        }

        /// <summary>
        /// 根据 StatType 查找对应的 StatRow
        /// </summary>
        private StatRow FindStatRow(StatType type)
        {
            foreach (var row in activeStatRows)
            {
                if (row != null && row.statType == type) return row;
            }
            return null;
        }

        /// <summary>
        /// 购点属性值变化回调
        /// </summary>
        private void OnPointBuyStatChanged(StatType statType, int newValue)
        {
            RefreshStatDisplay(statType);
        }

        /// <summary>
        /// 剩余点数变化回调
        /// </summary>
        private void OnPointBuyPointsChanged(int availablePoints)
        {
            UpdatePointsDisplay(availablePoints);
            // 点数变化影响所有属性的 +/- 按钮状态
            RefreshAllPlusMinusInteractable();
        }

        /// <summary>
        /// 种族加成选择变化回调
        /// </summary>
        private void OnPointBuyRacialBonusChanged()
        {
            RefreshAllStatDisplays();
        }

        /// <summary>
        /// 种族加成按钮被点击：切换该属性的+1分配状态
        /// 变体人类规则：最多选2个不同属性，再次点击取消选择
        /// </summary>
        private void OnRacialBonusBtnClicked(StatType statType)
        {
            if (pointBuy == null) return;

            bool isSelected = pointBuy.RacialBonusChoices.Contains(statType);
            if (isSelected)
            {
                // 取消选择
                pointBuy.TryRemoveRacialBonusChoice(statType);
                Debug.Log($"[CharacterCustomizationPanel] 取消种族加成: {statType}");
            }
            else
            {
                // 尝试选择
                bool success = pointBuy.TryAddRacialBonusChoice(statType);
                if (success)
                    Debug.Log($"[CharacterCustomizationPanel] 分配种族加成: {statType} (已选{pointBuy.RacialBonusChoices.Count}/2)");
                else
                    Debug.Log($"[CharacterCustomizationPanel] 无法分配种族加成到 {statType}（已选满或重复）");
            }
        }

        /// <summary>
        /// 刷新单个属性的显示（值 + 种族加成 + 调整值 + 按钮状态 + 种族选择高亮）
        /// 
        /// 显示逻辑（变体人类）：
        ///   - valueText 显示购点基础值，若该属性被选为种族加成则追加绿色 (+1种族) 提示
        ///   - modText 始终基于最终值（基础+种族）计算调整值
        ///   - racialBonusSelectedMark 高亮当前已选的种族加成属性
        ///   - racialBonusHintText 提示"点击分配+1"或"已选(+1)"
        /// </summary>
        private void RefreshStatDisplay(StatType statType)
        {
            var row = FindStatRow(statType);
            if (row == null) return;

            int baseVal = pointBuy.GetStat(statType);
            int racialBonus = pointBuy.GetRacialBonus(statType);
            int finalVal = baseVal + racialBonus;
            int mod = PointBuySystem.GetModifier(finalVal);
            string modStr = mod >= 0 ? $"+{mod}" : mod.ToString();

            bool isRacialSelected = pointBuy.RacialBonusChoices.Contains(statType);
            bool canSelectMore = !pointBuy.RacialBonusChoicesFull || isRacialSelected;

            if (row.valueText != null)
            {
                // 购点基础值 + 种族加成提示（仅已选属性显示）
                row.valueText.text = racialBonus > 0
                    ? $"{baseVal} <color=#4CAF50>(+{racialBonus}种族)</color>"
                    : baseVal.ToString();
            }
            if (row.modText != null) row.modText.text = modStr;
            if (row.minusBtn != null) row.minusBtn.interactable = pointBuy.CanDecrease(statType);
            if (row.plusBtn != null) row.plusBtn.interactable = pointBuy.CanIncrease(statType);

            // 种族加成选中标记
            if (row.racialBonusSelectedMark != null)
                row.racialBonusSelectedMark.SetActive(isRacialSelected);

            // 种族加成按钮可点击状态
            if (row.racialBonusBtn != null)
                row.racialBonusBtn.interactable = canSelectMore;

            // 种族加成提示文本
            if (row.racialBonusHintText != null)
            {
                if (isRacialSelected)
                    row.racialBonusHintText.text = "<color=#4CAF50>种族+1 ✓</color>";
                else if (pointBuy.RacialBonusChoicesFull)
                    row.racialBonusHintText.text = "<color=#888888>已满</color>";
                else
                    row.racialBonusHintText.text = "<color=#FFD54F>点击分配+1</color>";
            }
        }

        /// <summary>
        /// 刷新所有属性显示
        /// </summary>
        private void RefreshAllStatDisplays()
        {
            RefreshStatDisplay(StatType.Strength);
            RefreshStatDisplay(StatType.Dexterity);
            RefreshStatDisplay(StatType.Constitution);
            RefreshStatDisplay(StatType.Intelligence);
            RefreshStatDisplay(StatType.Wisdom);
            RefreshStatDisplay(StatType.Charisma);
        }

        /// <summary>
        /// 刷新所有 +/- 按钮状态
        /// </summary>
        private void RefreshAllPlusMinusInteractable()
        {
            foreach (var row in activeStatRows)
            {
                if (row == null) continue;
                if (row.minusBtn != null) row.minusBtn.interactable = pointBuy.CanDecrease(row.statType);
                if (row.plusBtn != null) row.plusBtn.interactable = pointBuy.CanIncrease(row.statType);
            }
        }

        /// <summary>
        /// 更新剩余点数显示
        /// </summary>
        private void UpdatePointsDisplay(int points)
        {
            if (availablePointsText != null)
                availablePointsText.text = $"剩余点数: {points}";
        }

        /// <summary>
        /// <summary>
        /// 获取分类的显示名称
        /// </summary>
        private string GetCategoryDisplayName(SkinBodyPartType partType)
        {
            switch (partType)
            {
                // 内层装饰
                case SkinBodyPartType.SkinBase: return "基础身体";
                case SkinBodyPartType.Hair: return "头发";
                case SkinBodyPartType.Eyes: return "眼睛";
                case SkinBodyPartType.Mouth: return "嘴";

                // 外层装备
                case SkinBodyPartType.Helmet: return "头盔";
                case SkinBodyPartType.Armor: return "躯干护甲";
                case SkinBodyPartType.Gloves: return "护腕/手套";
                case SkinBodyPartType.Boots: return "靴子";
                case SkinBodyPartType.Belt: return "腰带";
                case SkinBodyPartType.Cloak: return "披风";
                case SkinBodyPartType.MainHandWeapon: return "主手武器";
                case SkinBodyPartType.OffHandShield: return "副手盾牌";
                case SkinBodyPartType.OffHandWeapon: return "副手武器";

                // 特殊
                case SkinBodyPartType.FullSkin: return "整套";

                // 向后兼容（已废弃）
                case SkinBodyPartType.Clothes: return "衣服(旧)";
                case SkinBodyPartType.Legs: return "腿(旧)";
                case SkinBodyPartType.Eyelids: return "眼皮(旧)";
                case SkinBodyPartType.Nose: return "鼻子(旧)";
                case SkinBodyPartType.Accessory: return "配件(旧)";

                default: return partType.ToString();
            }
        }

        /// <summary>
        /// 从 AnimationConfig 读取动画列表
        /// </summary>
        private List<string> GetAnimationNamesFromConfig()
        {
            var result = new List<string>();

            if (animationConfig == null)
                return result;

            // 从 CharacterAnimationConfig 中读取所有动画字段
            if (!string.IsNullOrEmpty(animationConfig.idleAnimation))
                result.Add(animationConfig.idleAnimation);
            if (!string.IsNullOrEmpty(animationConfig.walkAnimation))
                result.Add(animationConfig.walkAnimation);
            if (!string.IsNullOrEmpty(animationConfig.runAnimation))
                result.Add(animationConfig.runAnimation);
            if (!string.IsNullOrEmpty(animationConfig.attackAnimation))
                result.Add(animationConfig.attackAnimation);
            if (!string.IsNullOrEmpty(animationConfig.hitAnimation))
                result.Add(animationConfig.hitAnimation);
            if (!string.IsNullOrEmpty(animationConfig.deathAnimation))
                result.Add(animationConfig.deathAnimation);

            return result;
        }

        /// <summary>
        /// 显示指定分类的所有皮肤 icon
        /// </summary>
        private void ShowCategory(SkinBodyPartType partType)
        {
            currentCategory = partType;

            // 清空当前 icon 列表
            foreach (var btn in activeIconButtons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            activeIconButtons.Clear();

            if (skinConfig == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] SkinConfig 未设置");
                return;
            }

            // 从 SkinConfig 获取该分类的所有皮肤
            var parts = skinConfig.GetPartsByType(partType);
            Debug.Log($"[CharacterCustomizationPanel] 显示分类 {partType}，包含 {parts.Count} 个皮肤");

            // 为每个皮肤创建 icon 按钮
            foreach (var part in parts)
            {
                CreatePartIconButton(part);
            }
        }

        /// <summary>
        /// 为单个皮肤创建 icon 按钮
        /// </summary>
        private void CreatePartIconButton(SkinPartEntry part)
        {
            if (iconButtonPrefab == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] iconButtonPrefab 未设置");
                return;
            }

            if (iconGridContainer == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] iconGridContainer 未设置");
                return;
            }

            var btn = Instantiate(iconButtonPrefab, iconGridContainer);
            btn.name = $"Icon_{part.skinID}";

            // 设置图片
            var image = btn.GetComponent<Image>();
            if (image != null && part.previewIcon != null)
            {
                image.sprite = part.previewIcon;
            }

            // 设置提示文本（可选）
            var text = btn.GetComponentInChildren<Text>();
            if (text != null)
                text.text = part.displayName;

            // 添加点击事件
            btn.onClick.AddListener(() => OnPartIconClicked(part));

            activeIconButtons.Add(btn);
        }

        /// <summary>
        /// 分类标签页被点击
        /// </summary>
        private void OnCategoryTabClicked(SkinBodyPartType partType)
        {
            Debug.Log($"[CharacterCustomizationPanel] 点击分类标签：{partType}");
            ShowCategory(partType);
        }

        /// <summary>
        /// 皮肤 icon 被点击
        /// </summary>
        private void OnPartIconClicked(SkinPartEntry part)
        {
            Debug.Log($"[CharacterCustomizationPanel] 点击皮肤 icon：{part.skinID} ({part.partType})");

            if (uiCharacterAppearance == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] uiCharacterAppearance 为空");
                return;
            }

            // 更新 UI 角色的皮肤
            uiCharacterAppearance.SetPart(part.partType, part.skinID);

            // 如果该部位关联了 ItemBaseSO（装备→物品映射），缓存下来，确认时同步到背包/装备栏
            if (part.linkedItemSO != null)
            {
                pendingEquipmentItems[part.partType] = part.linkedItemSO;
                Debug.Log($"[CharacterCustomizationPanel] 装备映射已缓存: {part.partType} → {part.linkedItemSO.displayName}");
            }
            else if (pendingEquipmentItems.ContainsKey(part.partType))
            {
                // 用户换成了一个不关联物品的皮肤（如"卸下"），清除缓存
                pendingEquipmentItems.Remove(part.partType);
            }

            // 更换部件后，重新调整摄像机以适应新的模型边界
            FitCameraToCharacter(uiCharacter, uiCharacterCamera);
        }

        /// <summary>
        /// 动画按钮被点击
        /// </summary>
        private void OnAnimationButtonClicked(string animationName)
        {
            Debug.Log($"[CharacterCustomizationPanel] 点击动画按钮：{animationName}");

            if (uiCharacter == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] uiCharacter 为空");
                return;
            }

            if (uiCharacter.AnimationState == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] uiCharacter.AnimationState 为空");
                return;
            }

            // 直接播放动画（使用 SkeletonAnimation 的 AnimationState）
            try
            {
                uiCharacter.AnimationState.SetAnimation(0, animationName, true);
                Debug.Log($"[CharacterCustomizationPanel] 播放动画：{animationName}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CharacterCustomizationPanel] 清理资源失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 确认按钮被点击 - 同步皮肤配置到游戏角色
        /// </summary>
        private void OnConfirmClicked()
        {
            Debug.Log("[CharacterCustomizationPanel] 点击确认按钮");

            try
            {
                // 同步 UI 角色的皮肤配置到游戏角色
                SyncToGameCharacter();

                // 触发事件
                OnConfirm?.Invoke();

                // 关闭 UI
                gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharacterCustomizationPanel] 确认失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消按钮被点击
        /// </summary>
        private void OnCancelClicked()
        {
            Debug.Log("[CharacterCustomizationPanel] 点击取消按钮");

            try
            {
                // 触发事件
                OnCancel?.Invoke();

                // 关闭 UI
                gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharacterCustomizationPanel] 取消失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步 UI 角色的皮肤配置到游戏角色（包括内层装饰和外层装备）
        /// </summary>
        private void SyncToGameCharacter()
        {
            if (gameCharacter == null)
            {
                Debug.LogWarning("[CharacterCustomizationPanel] gameCharacter 未设置，无法同步皮肤");
                return;
            }

            // ---- 1. 同步外观皮肤 ----
            SyncAppearanceToGameCharacter();

            // ---- 2. 同步属性值 ----
            SyncStatsToGameCharacter();

            // ---- 3. 同步装备物品到背包/装备栏 ----
            SyncEquipmentItemsToGameCharacter();

            Debug.Log("[CharacterCustomizationPanel] 全部同步完成（外观 + 属性 + 装备物品）");
        }

        /// <summary>
        /// 同步外观皮肤到游戏角色
        /// </summary>
        private void SyncAppearanceToGameCharacter()
        {
            var gameAppearance = gameCharacter.GetComponent<CharacterAppearance>();
            if (gameAppearance == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] gameCharacter 不包含 CharacterAppearance 组件");
                return;
            }

            if (uiCharacterAppearance == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] uiCharacterAppearance 为空");
                return;
            }

            var currentParts = uiCharacterAppearance.GetAllCurrentParts();
            foreach (var kvp in currentParts)
            {
                var partType = kvp.Key;
                var skinID = kvp.Value;
                Debug.Log($"[CharacterCustomizationPanel] 同步皮肤：{partType} = {skinID}");
                gameAppearance.SetPart(partType, skinID);
            }
            Debug.Log("[CharacterCustomizationPanel] 皮肤同步完成");
        }

        /// <summary>
        /// 同步属性值到游戏角色的 CharacterStats（使用购点结果+种族加成）
        /// CharacterStats 是运行时实际属性决定者（CharacterTemplate 只是静态蓝图）
        /// </summary>
        private void SyncStatsToGameCharacter()
        {
            var gameStats = gameCharacter.GetComponent<CharacterStats>();
            if (gameStats == null)
            {
                Debug.LogWarning("[CharacterCustomizationPanel] gameCharacter 不包含 CharacterStats 组件，跳过属性同步");
                return;
            }

            if (pointBuy == null)
            {
                Debug.LogWarning("[CharacterCustomizationPanel] 购点系统未初始化，跳过属性同步");
                return;
            }

            // 使用购点系统的最终值（基础值 + 种族加成）
            int finalStr = pointBuy.FinalStrength;
            int finalDex = pointBuy.FinalDexterity;
            int finalCon = pointBuy.FinalConstitution;
            int finalInt = pointBuy.FinalIntelligence;
            int finalWis = pointBuy.FinalWisdom;
            int finalCha = pointBuy.FinalCharisma;

            // 如果选定了职业模板，使用 InitializeFromPointBuy（含种族加成+自选列表）
            if (selectedTemplate != null)
            {
                gameStats.template = selectedTemplate;
                // 传递自选的种族加成属性列表（变体人类）
                var racialChoices = new List<StatType>(pointBuy.RacialBonusChoices);
                gameStats.InitializeFromPointBuy(
                    pointBuy.Strength, pointBuy.Dexterity, pointBuy.Constitution,
                    pointBuy.Intelligence, pointBuy.Wisdom, pointBuy.Charisma,
                    selectedTemplate.race, racialChoices);
            }
            else
            {
                // 无模板时直接设置属性
                gameStats.strength = pointBuy.Strength;
                gameStats.dexterity = pointBuy.Dexterity;
                gameStats.constitution = pointBuy.Constitution;
                gameStats.intelligence = pointBuy.Intelligence;
                gameStats.wisdom = pointBuy.Wisdom;
                gameStats.charisma = pointBuy.Charisma;
                // 传递自选列表
                var racialChoices = new List<StatType>(pointBuy.RacialBonusChoices);
                gameStats.SetRacialBonuses(PointBuySystem.RaceType.Human, racialChoices);
                gameStats.RequestRecalculateStats();
            }

            // 设置等级（创建角色固定为1级）
            gameStats.SetLevel(StartLevel, healToFull: true);

            Debug.Log($"[CharacterCustomizationPanel] 属性同步完成(购点+种族): STR={finalStr} DEX={finalDex} CON={finalCon} " +
                      $"INT={finalInt} WIS={finalWis} CHA={finalCha} LVL={StartLevel}");
        }

        /// <summary>
        /// 同步装备物品到游戏角色的 CharacterInventory 和 CharacterEquipment
        ///
        /// 流程：
        ///   1. 根据 pendingEquipmentItems（用户在面板中选中的装备外观 → ItemBaseSO 映射）
        ///      为每个 ItemBaseSO 创建 ItemInstance
        ///   2. 将 ItemInstance 加入 gameCharacter 的 CharacterInventory（背包）
        ///   3. 将 ItemInstance 装备到 gameCharacter 的 CharacterEquipment 对应槽位
        ///   4. CharacterEquipment.EquipToSlot 会自动触发：
        ///      - ReapplyEquippedModifiers() → 属性修正生效
        ///      - SyncAppearance() → 通知 CharacterAppearance 换装（与第1步外观同步协同）
        /// </summary>
        private void SyncEquipmentItemsToGameCharacter()
        {
            if (pendingEquipmentItems.Count == 0)
            {
                Debug.Log("[CharacterCustomizationPanel] 没有待同步的装备物品");
                return;
            }

            // 查找游戏角色上的背包和装备组件
            var gameInventory = gameCharacter.GetComponent<CharacterInventory>();
            if (gameInventory == null)
            {
                Debug.LogWarning("[CharacterCustomizationPanel] gameCharacter 不包含 CharacterInventory 组件，跳过装备物品同步");
                return;
            }

            var gameEquipment = gameCharacter.GetComponent<CharacterEquipment>();
            if (gameEquipment == null)
            {
                Debug.LogWarning("[CharacterCustomizationPanel] gameCharacter 不包含 CharacterEquipment 组件，跳过装备物品同步");
                return;
            }

            // SkinBodyPartType → EquipmentSlot 映射（与 CharacterAppearance.MapPartTypeToEquipmentSlot 一致）
            foreach (var kvp in pendingEquipmentItems)
            {
                var partType = kvp.Key;
                var itemSO = kvp.Value;
                if (itemSO == null) continue;

                // 映射到装备槽位
                EquipmentSlot? slot = MapSkinPartToEquipmentSlot(partType);
                if (!slot.HasValue)
                {
                    Debug.LogWarning($"[CharacterCustomizationPanel] 无法映射 {partType} 到 EquipmentSlot，跳过");
                    continue;
                }

                // 创建 ItemInstance
                var itemInst = new ItemInstance(itemSO);
                Debug.Log($"[CharacterCustomizationPanel] 创建物品实例: {itemSO.displayName} (ID: {itemInst.instanceId})");

                // 加入背包
                gameInventory.AddInstance(itemInst);

                // 装备到对应槽位（CharacterEquipment.EquipToSlot 会触发属性修正 + 外观同步）
                if (gameEquipment.CanEquip(itemInst))
                {
                    bool equipped = gameEquipment.EquipToSlot(slot.Value, itemInst);
                    Debug.Log($"[CharacterCustomizationPanel] 装备物品到 {slot.Value}: {(equipped ? "成功" : "失败")}");
                }
                else
                {
                    Debug.LogWarning($"[CharacterCustomizationPanel] 角色无法装备 {itemSO.displayName}（熟练度不足或类型不匹配），已放入背包但未装备");
                }
            }

            // 清空待处理列表
            pendingEquipmentItems.Clear();
            Debug.Log("[CharacterCustomizationPanel] 装备物品同步完成");
        }

        /// <summary>
        /// SkinBodyPartType → EquipmentSlot 映射（与 CharacterAppearance 保持一致）
        /// </summary>
        private static EquipmentSlot? MapSkinPartToEquipmentSlot(SkinBodyPartType partType)
        {
            switch (partType)
            {
                case SkinBodyPartType.Helmet: return EquipmentSlot.Helmet;
                case SkinBodyPartType.Armor: return EquipmentSlot.Armor;
                case SkinBodyPartType.Gloves: return EquipmentSlot.Gauntlets;
                case SkinBodyPartType.Boots: return EquipmentSlot.Boots;
                case SkinBodyPartType.Belt: return EquipmentSlot.Belt;
                case SkinBodyPartType.Cloak: return EquipmentSlot.Cloak;
                case SkinBodyPartType.MainHandWeapon: return EquipmentSlot.MainHand;
                case SkinBodyPartType.OffHandShield: return EquipmentSlot.OffHand;
                case SkinBodyPartType.OffHandWeapon: return EquipmentSlot.OffHand;
                default: return null;
            }
        }

        /// <summary>
        /// 外部接口：直接设置某个角色的皮肤
        /// 用于游戏系统：掉落装备、购买皮肤等场景
        /// </summary>
        public void SetCharacterSkin(SkeletonAnimation targetCharacter, SkinBodyPartType partType, string skinID)
        {
            if (targetCharacter == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] targetCharacter 为空");
                return;
            }

            var appearance = targetCharacter.GetComponent<CharacterAppearance>();
            if (appearance == null)
            {
                Debug.LogError("[CharacterCustomizationPanel] targetCharacter 不包含 CharacterAppearance 组件");
                return;
            }

            Debug.Log($"[CharacterCustomizationPanel] 设置角色皮肤：{partType} = {skinID}");
            appearance.SetPart(partType, skinID);
        }
    }
}

