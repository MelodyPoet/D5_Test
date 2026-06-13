using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace demo2.DND.Editor
{
    /// <summary>
    /// Spine 皮肤自动导入工具
    /// 功能：从 SkeletonDataAsset 中自动读取皮肤，按规则映射到 SkinBodyPartType，生成 SkinPartEntry 列表
    /// </summary>
    public class SpineSkinImporter : EditorWindow
    {
        private Vector2 scrollPos;
        private SkinConfig targetSkinConfig;
        private UnityEngine.Object selectedSkeletonAsset;  // 直接拖入的 SkeletonDataAsset
        private SkeletonDataInfo currentSkeletonInfo;      // 当前分析的 Skeleton 信息
        private List<SkinImportRule> importRules = new List<SkinImportRule>();
        private List<SkinPartEntry> previewEntries = new List<SkinPartEntry>();
        private bool showPreview = false;
        private string importLog = "";
        private bool showRawSkins = false;  // 显示原始皮肤列表

        // Spine 类型的反射缓存
        private Type spineSkeletonDataAssetType;
        private Type spineSkeletonDataType;
        private Type spineSkinType;

        [MenuItem("Assets/DND/Import Spine Skins to SkinConfig...")]
        public static void ShowWindow()
        {
            GetWindow<SpineSkinImporter>("Spine Skin Importer");
        }

        [MenuItem("Assets/DND/Debug/Test Spine Type Loading")]
        public static void TestSpineTypeLoading()
        {
            var window = GetWindow<SpineSkinImporter>("Spine Skin Importer");
            window.InitializeSpineReflection();
            EditorUtility.DisplayDialog("Spine 类型加载测试", window.importLog, "OK");
        }

        private void OnEnable()
        {
            InitializeSpineReflection();
            LoadDefaultRules();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Spine 皮肤自动导入工具", EditorStyles.boldLabel);

            // 检查 Spine 类型是否已初始化
            if (spineSkeletonDataAssetType == null)
            {
                EditorGUILayout.HelpBox(
                    "Spine 类型尚未初始化。可能原因：\n" +
                    "1. 项目仍在编译中\n" +
                    "2. Spine-Unity 插件未完全安装\n" +
                    "3. 有编译错误\n\n" +
                    "请在 Unity 编辑器中等待编译完成，或检查 Console 面板中的错误。",
                    MessageType.Warning
                );

                if (GUILayout.Button("重新初始化", GUILayout.Height(25)))
                {
                    InitializeSpineReflection();
                }

                if (GUILayout.Button("打开诊断", GUILayout.Height(25)))
                {
                    SpineTypeVerifier.ManualVerifySpine();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("初始化日志：", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(importLog, GUILayout.Height(150));
                return;
            }

            EditorGUILayout.HelpBox("将 SkeletonDataAsset 拖入下方，工具会自动分析其中的皮肤列表", MessageType.Info);
            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // ===== [1] 拖入 Spine 素材资源 =====
            EditorGUILayout.LabelField("[1] 选择 Spine 素材资源", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "拖入 *_SkeletonData.asset 文件\n或点击圆形按钮搜索",
                MessageType.None
            );

            UnityEngine.Object newAsset = EditorGUILayout.ObjectField(
                "SkeletonDataAsset",
                selectedSkeletonAsset,
                typeof(UnityEngine.Object),
                false
            );

            // 检测资源变化
            if (newAsset != selectedSkeletonAsset)
            {
                selectedSkeletonAsset = newAsset;
                if (selectedSkeletonAsset != null)
                {
                    AnalyzeSelectedAsset();
                    showPreview = false;
                }
            }

            // 显示当前选中资源的信息
            if (currentSkeletonInfo != null && currentSkeletonInfo.skinNames.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"资源: {currentSkeletonInfo.assetPath}", EditorStyles.helpBox);
                EditorGUILayout.LabelField($"皮肤数量: {currentSkeletonInfo.skinNames.Count}", EditorStyles.label);

                // 显示原始皮肤列表（可折叠）
                showRawSkins = EditorGUILayout.Foldout(showRawSkins, "原始皮肤列表");
                if (showRawSkins)
                {
                    EditorGUILayout.BeginVertical("box");
                    foreach (var skinName in currentSkeletonInfo.skinNames)
                    {
                        EditorGUILayout.LabelField($"• {skinName}", EditorStyles.label);
                    }
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Separator();

            // ===== [2] 选择目标 SkinConfig =====
            EditorGUILayout.LabelField("[2] 选择目标 SkinConfig", EditorStyles.boldLabel);
            targetSkinConfig = EditorGUILayout.ObjectField(
                "Target SkinConfig",
                targetSkinConfig,
                typeof(SkinConfig),
                false
            ) as SkinConfig;

            EditorGUILayout.Space();
            EditorGUILayout.Separator();

            // ===== [3] 映射规则配置 =====
            EditorGUILayout.LabelField("[3] 映射规则", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "根据你的 Spine 素材定制规则。\n" +
                "观察上面的 \"原始皮肤列表\"，确认皮肤命名规律，然后配置规则。",
                MessageType.Info
            );

            for (int i = 0; i < importRules.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var rule = importRules[i];

                rule.pattern = EditorGUILayout.TextField("Pattern", rule.pattern, GUILayout.Width(150));
                rule.ruleType = (SkinImportRuleType)EditorGUILayout.EnumPopup(rule.ruleType, GUILayout.Width(100));
                rule.partType = (SkinBodyPartType)EditorGUILayout.EnumPopup(rule.partType, GUILayout.Width(120));
                rule.priority = EditorGUILayout.IntField("Priority", rule.priority, GUILayout.Width(80));

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    importRules.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Rule"))
            {
                importRules.Add(new SkinImportRule
                {
                    pattern = "",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Clothes,
                    priority = importRules.Count
                });
            }

            EditorGUILayout.Space();
            EditorGUILayout.Separator();

            // ===== [4] 预览和生成 =====
            EditorGUILayout.LabelField("[4] 预览和应用", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(currentSkeletonInfo == null || currentSkeletonInfo.skinNames.Count == 0);

            if (GUILayout.Button("生成预览", GUILayout.Height(30)))
            {
                GeneratePreview();
                showPreview = true;
            }

            EditorGUI.EndDisabledGroup();

            if (showPreview && previewEntries.Count > 0)
            {
                EditorGUILayout.LabelField($"预览条目 ({previewEntries.Count})", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                foreach (var entry in previewEntries)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(entry.skinID, GUILayout.Width(150));
                    EditorGUILayout.LabelField(entry.displayName, GUILayout.Width(150));
                    EditorGUILayout.LabelField(entry.partType.ToString(), GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("导入到 SkinConfig", GUILayout.Height(30)))
                {
                    if (targetSkinConfig == null)
                    {
                        EditorUtility.DisplayDialog("错误", "请先选择一个 SkinConfig", "OK");
                        return;
                    }
                    ApplyImport();
                }

                if (GUILayout.Button("清除预览"))
                {
                    showPreview = false;
                    previewEntries.Clear();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // ===== 日志 =====
            EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(importLog, GUILayout.Height(100));

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 初始化 Spine 的反射类型缓存
        /// </summary>
        private void InitializeSpineReflection()
        {
            try
            {
                // 方案 1: 通过已加载的程序集查找类型
                Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (Assembly assembly in loadedAssemblies)
                {
                    if (spineSkeletonDataAssetType == null)
                    {
                        spineSkeletonDataAssetType = assembly.GetType("Spine.Unity.SkeletonDataAsset", false);
                    }
                    if (spineSkeletonDataType == null)
                    {
                        spineSkeletonDataType = assembly.GetType("Spine.SkeletonData", false);
                    }
                    if (spineSkinType == null)
                    {
                        spineSkinType = assembly.GetType("Spine.Skin", false);
                    }

                    // 如果三个类型都找到了，提前退出
                    if (spineSkeletonDataAssetType != null && spineSkeletonDataType != null && spineSkinType != null)
                    {
                        break;
                    }
                }

                // 检查是否所有类型都成功加载
                if (spineSkeletonDataAssetType == null || spineSkeletonDataType == null || spineSkinType == null)
                {
                    // 尝试 Type.GetType 作为备选方案
                    spineSkeletonDataAssetType = spineSkeletonDataAssetType ?? Type.GetType("Spine.Unity.SkeletonDataAsset");
                    spineSkeletonDataType = spineSkeletonDataType ?? Type.GetType("Spine.SkeletonData");
                    spineSkinType = spineSkinType ?? Type.GetType("Spine.Skin");
                }

                if (spineSkeletonDataAssetType == null || spineSkeletonDataType == null || spineSkinType == null)
                {
                    // 列出所有加载的程序集以供调试
                    string assemblyList = "已加载的程序集：\n";
                    foreach (Assembly asm in loadedAssemblies)
                    {
                        if (asm.GetName().Name.Contains("Spine") || asm.GetName().Name.Contains("Assembly"))
                        {
                            assemblyList += $"- {asm.GetName().Name}\n";
                        }
                    }

                    importLog = "[错误] 无法加载所有 Spine 类型。\n\n" +
                                "类型加载状态：\n" +
                                $"- SkeletonDataAsset: {(spineSkeletonDataAssetType != null ? "✓" : "✗")}\n" +
                                $"- SkeletonData: {(spineSkeletonDataType != null ? "✓" : "✗")}\n" +
                                $"- Skin: {(spineSkinType != null ? "✓" : "✗")}\n\n" +
                                assemblyList +
                                "\n请确保：\n" +
                                "1. Spine-Unity 插件已安装在 Assets/Spine 文件夹\n" +
                                "2. 项目已完全编译\n" +
                                "3. 没有编译错误";
                    return;
                }

                importLog = "[初始化成功] Spine 类型已加载\n" +
                           $"SkeletonDataAsset: {spineSkeletonDataAssetType.Assembly.GetName().Name}\n" +
                           $"SkeletonData: {spineSkeletonDataType.Assembly.GetName().Name}\n" +
                           $"Skin: {spineSkinType.Assembly.GetName().Name}";
            }
            catch (Exception ex)
            {
                importLog = $"[异常] Spine 类型初始化失败: {ex.Message}\n{ex.StackTrace}";
            }
        }

        /// <summary>
        /// 分析选中的 SkeletonDataAsset
        /// </summary>
        private void AnalyzeSelectedAsset()
        {
            if (selectedSkeletonAsset == null)
            {
                importLog = "[错误] 未选中任何资源";
                return;
            }

            try
            {
                // 如果类型未初始化，尝试重新初始化
                if (spineSkeletonDataAssetType == null)
                {
                    InitializeSpineReflection();
                }

                if (spineSkeletonDataAssetType == null)
                {
                    importLog = "[错误] Spine 类型未初始化，请检查是否安装了 Spine-Unity 插件";
                    return;
                }

                // 检查类型是否匹配
                if (!selectedSkeletonAsset.GetType().Name.Contains("SkeletonDataAsset"))
                {
                    importLog = "[错误] 请拖入 SkeletonDataAsset 类型的资源（通常以 _SkeletonData.asset 结尾）";
                    selectedSkeletonAsset = null;
                    currentSkeletonInfo = null;
                    return;
                }

                string assetPath = AssetDatabase.GetAssetPath(selectedSkeletonAsset);
                var skinNames = ExtractSkinNamesFromAsset(selectedSkeletonAsset);

                if (skinNames.Count == 0)
                {
                    importLog = "[警告] 该 Spine 资源中未发现皮肤或皮肤数量为 0\n" +
                                "这可能是一个没有制作换装皮肤的简单 Spine 模型";
                    currentSkeletonInfo = null;
                    return;
                }

                currentSkeletonInfo = new SkeletonDataInfo
                {
                    assetPath = assetPath,
                    asset = selectedSkeletonAsset,
                    skinNames = skinNames
                };

                importLog = $"[分析完成] 找到 {skinNames.Count} 个皮肤\n" +
                            $"资源: {assetPath}\n\n" +
                            $"观察上面的 \"原始皮肤列表\"，根据命名规律配置规则";
            }
            catch (Exception ex)
            {
                importLog = $"[异常] {ex.Message}\n{ex.StackTrace}";
                currentSkeletonInfo = null;
            }
        }

        /// <summary>
        /// 删除旧的 ScanSkeletonDataAssets 方法
        /// </summary>

        /// <summary>
        /// 从 SkeletonDataAsset 提取皮肤名称列表（反射方式）
        /// </summary>
        private List<string> ExtractSkinNamesFromAsset(UnityEngine.Object asset)
        {
            var result = new List<string>();

            try
            {
                if (asset == null)
                {
                    Debug.LogError("[SpineSkinImporter] 资源对象为空");
                    return result;
                }

                Type assetType = asset.GetType();
                Debug.Log($"[SpineSkinImporter] 资源类型: {assetType.FullName}");

                // 反射调用 GetSkeletonData(bool) - 获取运行时 SkeletonData
                MethodInfo getSkeletonDataMethod = assetType.GetMethod(
                    "GetSkeletonData",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(bool) },
                    null
                );

                if (getSkeletonDataMethod == null)
                {
                    Debug.LogError($"[SpineSkinImporter] 无法找到 GetSkeletonData(bool) 方法。资源类型：{assetType.FullName}");
                    Debug.Log("[SpineSkinImporter] 检查是否是正确的 SkeletonDataAsset");
                    return result;
                }

                // 参数为 true 表示强制重新加载
                object skeletonData = getSkeletonDataMethod.Invoke(asset, new object[] { true });
                if (skeletonData == null)
                {
                    Debug.LogError("[SpineSkinImporter] GetSkeletonData 返回 null - 无法加载 Skeleton 数据");
                    return result;
                }

                Type skeletonDataType = skeletonData.GetType();
                Debug.Log($"[SpineSkinImporter] SkeletonData 运行时类型: {skeletonDataType.FullName}");

                // ===== 获取 skins 字段 =====
                // skins 通常是 internal ExposedList<Skin> 类型
                FieldInfo skinsField = skeletonDataType.GetField(
                    "skins",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (skinsField == null)
                {
                    Debug.LogError($"[SpineSkinImporter] 无法找到 'skins' 字段。可能原因：");
                    Debug.Log("  1. Spine-Unity 版本不兼容");
                    Debug.Log("  2. SkeletonData 结构已更改");
                    Debug.Log("  3. 这不是有效的 Spine SkeletonData");

                    // 列出该类型中所有字段用于调试
                    FieldInfo[] allFields = skeletonDataType.GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    );
                    Debug.Log("[SpineSkinImporter] 所有可用字段：");
                    foreach (var field in allFields)
                    {
                        string access = field.IsPublic ? "public" : "internal/private";
                        Debug.Log($"  - {field.Name} ({field.FieldType.Name}) [{access}]");
                    }
                    return result;
                }

                object skinsObj = skinsField.GetValue(skeletonData);
                if (skinsObj == null)
                {
                    Debug.Log("[SpineSkinImporter] skins 字段为 null");
                    Debug.Log("  可能原因：该 Spine 资源中没有定义皮肤（default skin 除外）");
                    return result;
                }

                Type skinsType = skinsObj.GetType();
                Debug.Log($"[SpineSkinImporter] skins 容器类型: {skinsType.FullName}");

                // ===== 尝试提取皮肤列表 =====
                // 方案 0: 直接访问 ExposedList 的 public 字段（Spine 特定优化 - 最高效）
                FieldInfo countField = skinsType.GetField("Count", BindingFlags.Public | BindingFlags.Instance);
                FieldInfo itemsField = skinsType.GetField("Items", BindingFlags.Public | BindingFlags.Instance);

                if (countField != null && itemsField != null)
                {
                    try
                    {
                        int count = (int)countField.GetValue(skinsObj);
                        Debug.Log($"[SpineSkinImporter] 通过字段直接访问: Count={count}");

                        if (count > 0)
                        {
                            object itemsArray = itemsField.GetValue(skinsObj);
                            if (itemsArray is System.Collections.IList itemsList)
                            {
                                for (int i = 0; i < count && i < itemsList.Count; i++)
                                {
                                    object skinObj = itemsList[i];
                                    if (skinObj == null)
                                    {
                                        Debug.LogWarning($"[SpineSkinImporter] Skin[{i}] 为 null");
                                        continue;
                                    }

                                    string skinName = ExtractSkinName(skinObj);
                                    if (!string.IsNullOrEmpty(skinName))
                                    {
                                        result.Add(skinName);
                                        Debug.Log($"[SpineSkinImporter]   [{i}] {skinName}");
                                    }
                                }

                                if (result.Count > 0)
                                {
                                    Debug.Log($"[SpineSkinImporter] ✅ 方案0成功：提取 {result.Count} 个皮肤");
                                    return result;
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("[SpineSkinImporter] Count 为 0，该资源没有多个皮肤配置");
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SpineSkinImporter] 方案0失败: {ex.Message}，尝试其他方案");
                    }
                }

                // 方案 1: 尝试通过 Count 和 Items 属性（更宽松的 BindingFlags）
                PropertyInfo countProperty = skinsType.GetProperty("Count",
                    BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
                PropertyInfo itemsProperty = skinsType.GetProperty("Items",
                    BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);

                if (countProperty != null && itemsProperty != null)
                {
                    try
                    {
                        int count = (int)countProperty.GetValue(skinsObj);
                        Debug.Log($"[SpineSkinImporter] 通过属性获取: Count={count}");

                        if (count > 0)
                        {
                            object itemsArray = itemsProperty.GetValue(skinsObj);
                            if (itemsArray is System.Collections.IList itemsList)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    object skinObj = itemsList[i];
                                    if (skinObj == null)
                                    {
                                        Debug.LogWarning($"[SpineSkinImporter] Skin[{i}] 为 null");
                                        continue;
                                    }

                                    string skinName = ExtractSkinName(skinObj);
                                    if (!string.IsNullOrEmpty(skinName))
                                    {
                                        result.Add(skinName);
                                        Debug.Log($"[SpineSkinImporter]   [{i}] {skinName}");
                                    }
                                }

                                if (result.Count > 0)
                                {
                                    Debug.Log($"[SpineSkinImporter] ✅ 方案1成功：提取 {result.Count} 个皮肤");
                                    return result;
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("[SpineSkinImporter] Count 为 0");
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SpineSkinImporter] 方案1失败: {ex.Message}，尝试下一个方案");
                    }
                }

                // 方案 2: 作为 ICollection 迭代
                if (skinsObj is System.Collections.ICollection collection)
                {
                    Debug.Log($"[SpineSkinImporter] 方案2：skins 是 ICollection，Count={collection.Count}");
                    int idx = 0;
                    foreach (object skinObj in collection)
                    {
                        if (skinObj == null) continue;

                        string skinName = ExtractSkinName(skinObj);
                        if (!string.IsNullOrEmpty(skinName))
                        {
                            result.Add(skinName);
                            Debug.Log($"[SpineSkinImporter]   [{idx}] {skinName}");
                        }
                        idx++;
                    }

                    if (result.Count > 0)
                    {
                        Debug.Log($"[SpineSkinImporter] ✅ 方案2成功：提取 {result.Count} 个皮肤");
                        return result;
                    }
                }

                // 方案 3: 作为 IEnumerable 迭代（备选方案）
                if (skinsObj is System.Collections.IEnumerable enumerable)
                {
                    Debug.Log($"[SpineSkinImporter] 方案3：skins 是 IEnumerable，开始迭代...");
                    int idx = 0;
                    foreach (object skinObj in enumerable)
                    {
                        if (skinObj == null) continue;

                        string skinName = ExtractSkinName(skinObj);
                        if (!string.IsNullOrEmpty(skinName))
                        {
                            result.Add(skinName);
                            Debug.Log($"[SpineSkinImporter]   [{idx}] {skinName}");
                        }
                        idx++;
                    }

                    if (result.Count > 0)
                    {
                        Debug.Log($"[SpineSkinImporter] ✅ 方案3成功：提取 {result.Count} 个皮肤");
                        return result;
                    }
                }

                // 如果以上方法都不行
                Debug.LogWarning($"[SpineSkinImporter] ❌ 所有方案都失败，无法迭代 skins 对象，类型：{skinsType.FullName}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpineSkinImporter] 提取皮肤异常: {ex.Message}");
                Debug.LogError($"[SpineSkinImporter] 堆栈跟踪: {ex.StackTrace}");
            }

            return result;
        }

        /// <summary>
        /// 从单个 Skin 对象提取名称
        /// </summary>
        private string ExtractSkinName(object skinObj)
        {
            if (skinObj == null) return null;

            try
            {
                Type skinType = skinObj.GetType();

                // 尝试获取 name 字段（Spine.Skin 使用字段而不是属性）
                FieldInfo nameField = skinType.GetField(
                    "name",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (nameField != null)
                {
                    return nameField.GetValue(skinObj) as string;
                }

                // 备选方案：尝试 Name 属性
                PropertyInfo nameProperty = skinType.GetProperty(
                    "Name",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                ) ?? skinType.GetProperty(
                    "name",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                );

                if (nameProperty != null)
                {
                    return nameProperty.GetValue(skinObj) as string;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpineSkinImporter] 提取皮肤名称失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 加载默认的映射规则
        /// 规则来源：Assets/demo2/DND/HorizontalFormation/DEVELOPMENT_STANDARDS.md
        ///
        /// 支持多种素材命名规则：
        /// A. 旧规则（mix-and-match-pro）：hair/、clothes/、legs/、eyes/、helm/ 等目录前缀
        /// B. 新规则（war03Test）：e5_helm、e5_armor、p7_glove 等角色前缀+下划线+部位后缀
        ///
        /// 换装系统支持两种方式：
        /// 1. 部件组合式：基础 SkinBase + 附加多个部件皮肤组合
        /// 2. 全身套装式：一键替换 FullSkin，不需要自定义组装
        ///
        /// 注意：default 是虚拟皮肤，被过滤掉不导入。skin-base 是真实基础皮肤，保留导入。
        /// </summary>
        private void LoadDefaultRules()
        {
            importRules = new List<SkinImportRule>
            {
                // === 规则集 A：旧素材命名（目录前缀，保留兼容） ===
                // 优先级 0: FullSkin（整套服装）
                new SkinImportRule {
                    pattern = "^full-skins/",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.FullSkin,
                    priority = 0
                },

                // 旧装饰部件（hair/、clothes/、legs/、eyes/、eyelids/、nose/、accessories/）
                new SkinImportRule {
                    pattern = "hair/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Hair,
                    priority = 1
                },
                new SkinImportRule {
                    pattern = "clothes/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Clothes,
                    priority = 2
                },
                new SkinImportRule {
                    pattern = "legs/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Legs,
                    priority = 3
                },
                new SkinImportRule {
                    pattern = "eyes/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Eyes,
                    priority = 4
                },
                new SkinImportRule {
                    pattern = "eyelids/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Eyelids,
                    priority = 5
                },
                new SkinImportRule {
                    pattern = "nose/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Nose,
                    priority = 6
                },
                new SkinImportRule {
                    pattern = "accessories/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Accessory,
                    priority = 7
                },

                // 旧装备外观部位（目录前缀 helm/、armor/、glove/、boots/、belt/、cloak/）
                new SkinImportRule {
                    pattern = "helm/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Helmet,
                    priority = 10
                },
                new SkinImportRule {
                    pattern = "armor/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Armor,
                    priority = 11
                },
                new SkinImportRule {
                    pattern = "glove/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Gloves,
                    priority = 12
                },
                new SkinImportRule {
                    pattern = "boots/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Boots,
                    priority = 13
                },
                new SkinImportRule {
                    pattern = "belt/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Belt,
                    priority = 14
                },
                new SkinImportRule {
                    pattern = "cloak/",
                    ruleType = SkinImportRuleType.Prefix,
                    partType = SkinBodyPartType.Cloak,
                    priority = 15
                },

                // === 规则集 B：新素材命名（角色前缀_部位后缀） ===
                // 优先级 20-29: 新格式装备部位（Regex 匹配 *_helm、*_armor 等）
                new SkinImportRule {
                    pattern = ".*_helm$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.Helmet,
                    priority = 20
                },
                new SkinImportRule {
                    pattern = ".*_armor$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.Armor,
                    priority = 21
                },
                new SkinImportRule {
                    pattern = ".*_glove$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.Gloves,
                    priority = 22
                },
                new SkinImportRule {
                    pattern = ".*_boots$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.Boots,
                    priority = 23
                },
                new SkinImportRule {
                    pattern = ".*_belt$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.Belt,
                    priority = 24
                },
                new SkinImportRule {
                    pattern = ".*_cloak$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.Cloak,
                    priority = 25
                },
                new SkinImportRule {
                    pattern = ".*_weapon_s$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.MainHandWeapon,
                    priority = 26
                },
                // 主手武器双持变体 _weapon_d
                new SkinImportRule {
                    pattern = ".*_weapon_d$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.MainHandWeapon,
                    priority = 27
                },
                // 盾牌
                new SkinImportRule {
                    pattern = ".*_shield$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.OffHandShield,
                    priority = 28
                },
                // 副手武器
                new SkinImportRule {
                    pattern = ".*_weapon_o$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.OffHandWeapon,
                    priority = 29
                },

                // === 优先级 8-9: SkinBase（基础身体） ===
                // 旧命名
                new SkinImportRule {
                    pattern = "^skin-base$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.SkinBase,
                    priority = 8
                },
                new SkinImportRule {
                    pattern = "^base-skin$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.SkinBase,
                    priority = 9
                },

                // === 新格式: *_alignment 可能是基础/阵营皮肤，先归为 SkinBase ===
                new SkinImportRule {
                    pattern = ".*_alignment$",
                    ruleType = SkinImportRuleType.Regex,
                    partType = SkinBodyPartType.SkinBase,
                    priority = 19
                },

                // 注意：default 虽然也在 Spine 的皮肤列表中，但是虚拟皮肤，会在 GeneratePreview 中被过滤掉
            };
        }

        /// <summary>
        /// 生成预览：根据规则映射皮肤
        /// </summary>
        private void GeneratePreview()
        {
            if (currentSkeletonInfo == null || currentSkeletonInfo.skinNames.Count == 0)
            {
                importLog = "[错误] 没有可分析的皮肤数据";
                return;
            }

            previewEntries.Clear();
            importLog = "[生成预览]\n";

            // 按优先级排序规则
            var sortedRules = importRules.OrderBy(r => r.priority).ToList();

            foreach (string skinName in currentSkeletonInfo.skinNames)
            {
                // ===== 过滤虚拟皮肤 =====
                // default 是虚拟的缺省皮肤，不导入到 SkinConfig
                // skin-base 是真实的基础皮肤部件，需要保留用于换装
                if (skinName == "default")
                {
                    importLog += $"⊘ {skinName} (虚拟皮肤，不导入)\n";
                    continue;
                }

                var entry = new SkinPartEntry
                {
                    skinID = skinName,
                    displayName = FormatDisplayName(skinName),
                    partType = SkinBodyPartType.Clothes, // 默认值
                    overlayColor = Color.white,
                    previewIcon = null
                };

                // 应用规则映射
                bool matched = false;
                foreach (var rule in sortedRules)
                {
                    if (MatchRule(skinName, rule))
                    {
                        entry.partType = rule.partType;
                        matched = true;
                        break;
                    }
                }

                previewEntries.Add(entry);
                string status = matched ? "✓" : "?";
                importLog += $"{status} {skinName} → {entry.partType}\n";
            }

            importLog += $"\n[完成] 生成 {previewEntries.Count} 条预览条目";
        }

        /// <summary>
        /// 判断皮肤名是否匹配规则
        /// </summary>
        private bool MatchRule(string skinName, SkinImportRule rule)
        {
            switch (rule.ruleType)
            {
                case SkinImportRuleType.Prefix:
                    return skinName.StartsWith(rule.pattern, StringComparison.OrdinalIgnoreCase);

                case SkinImportRuleType.Suffix:
                    return skinName.EndsWith(rule.pattern, StringComparison.OrdinalIgnoreCase);

                case SkinImportRuleType.Regex:
                    try
                    {
                        return Regex.IsMatch(skinName, rule.pattern, RegexOptions.IgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }

                default:
                    return false;
            }
        }

        /// <summary>
        /// 格式化皮肤名为显示名称
        /// 例如：head_red → Red Head，e5_helm → E5 Helm，p7_armor → P7 Armor
        /// </summary>
        private string FormatDisplayName(string skinName)
        {
            // 移除旧素材的目录前缀（如 hair/、helm/ 等）
            string result = skinName;
            string[] dirPrefixes = { "hair/", "clothes/", "legs/", "eyes/", "eyelids/", "nose/", "accessories/",
                                     "helm/", "armor/", "glove/", "boots/", "belt/", "cloak/",
                                     "full-skins/" };
            foreach (var prefix in dirPrefixes)
            {
                if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(prefix.Length);
                    break;
                }
            }

            // 处理旧格式的下划线命名
            string[] bodyPrefixes = { "head_", "body_", "clothes_", "leg_", "eye_" };
            foreach (var prefix in bodyPrefixes)
            {
                if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(prefix.Length);
                    break;
                }
            }

            // 下划线转空格并首字母大写
            result = result.Replace("_", " ");
            var words = result.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }
            result = string.Join(" ", words);

            return result;
        }

        /// <summary>
        /// 应用导入：将预览条目写入 SkinConfig
        /// </summary>
        private void ApplyImport()
        {
            if (targetSkinConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "未选择目标 SkinConfig", "OK");
                return;
            }

            if (previewEntries.Count == 0)
            {
                EditorUtility.DisplayDialog("警告", "没有预览条目可导入", "OK");
                return;
            }

            // 记录 Undo
            Undo.RecordObject(targetSkinConfig, "Import Spine Skins");

            // 获取目标列表
            var targetList = targetSkinConfig.GetSkinParts();

            // 合并策略：找出已存在的 skinID，避免重复
            var existingSkinIds = new HashSet<string>(targetList.Select(e => e.skinID));
            int addCount = 0;

            foreach (var newEntry in previewEntries)
            {
                if (!existingSkinIds.Contains(newEntry.skinID))
                {
                    targetList.Add(newEntry);
                    addCount++;
                    existingSkinIds.Add(newEntry.skinID);
                }
            }

            // 保存
            EditorUtility.SetDirty(targetSkinConfig);
            AssetDatabase.SaveAssets();

            importLog = $"[导入成功]\n新增条目：{addCount}\n总条目数：{targetList.Count}";
            EditorUtility.DisplayDialog("导入成功", $"新增 {addCount} 条皮肤配置\n总计 {targetList.Count} 条", "OK");

            showPreview = false;
            previewEntries.Clear();
        }

        /// <summary>
        /// Skeleton 数据模型
        /// </summary>
        private class SkeletonDataInfo
        {
            public string assetPath;
            public UnityEngine.Object asset;
            public List<string> skinNames = new List<string>();
        }
    }

    /// <summary>
    /// 导入规则定义
    /// </summary>
    [System.Serializable]
    public class SkinImportRule
    {
        public string pattern;              // 前缀/后缀/正则模式
        public SkinImportRuleType ruleType; // 匹配方式
        public SkinBodyPartType partType;   // 目标部件类型
        public int priority;                // 优先级
    }

    /// <summary>
    /// 导入规则类型
    /// </summary>
    public enum SkinImportRuleType
    {
        Prefix,
        Suffix,
        Regex
    }
}

