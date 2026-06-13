﻿﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;

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
    /// 6. 同步皮肤配置到游戏角色（确认按钮）
    /// 7. 支持外部调用：直接设置某个角色的皮肤
    ///
    /// 分层：
    ///   内层（基础装饰层，仅外观）：SkinBase、Hair、Eyes、Mouth
    ///   外层（装备外观层，与游戏逻辑关联）：Helmet、Armor、Gloves、Boots、Belt、Cloak、
    ///     MainHandWeapon、OffHandShield
    ///   注意：测试期外层也在面板中显示，后续正式版由背包系统驱动
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

            // 关键修复：更换部件后，重新调整摄像机以适应新的模型边界
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

            // 获取 UI 角色的当前皮肤配置（包括内层装饰 + 外层装备）
            var currentParts = uiCharacterAppearance.GetAllCurrentParts();

            // 同步到游戏角色
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

