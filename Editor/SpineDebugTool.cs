using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace demo2.DND.Editor
{
    /// <summary>
    /// Spine 皮肤提取调试工具
    /// 用于诊断为什么 skins 为 null 或为空的问题
    /// </summary>
    public class SpineDebugTool : EditorWindow
    {
        private UnityEngine.Object selectedSkeletonAsset;
        private string debugLog = "";

        [MenuItem("Assets/DND/Debug/Spine Skin Debug Tool")]
        public static void ShowWindow()
        {
            GetWindow<SpineDebugTool>("Spine Debug Tool");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Spine 皮肤提取调试工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("拖入 SkeletonDataAsset 来调试皮肤提取问题", MessageType.Info);

            selectedSkeletonAsset = EditorGUILayout.ObjectField(
                "SkeletonDataAsset",
                selectedSkeletonAsset,
                typeof(UnityEngine.Object),
                false
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("分析资源", GUILayout.Height(30)))
            {
                if (selectedSkeletonAsset != null)
                {
                    AnalyzeAsset();
                }
                else
                {
                    debugLog = "[错误] 请先选择一个 SkeletonDataAsset";
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("调试日志：", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(debugLog, GUILayout.Height(400));
        }

        private void AnalyzeAsset()
        {
            debugLog = "[开始分析]\n";

            try
            {
                if (selectedSkeletonAsset == null)
                {
                    debugLog += "[错误] 资源为 null\n";
                    return;
                }

                Type assetType = selectedSkeletonAsset.GetType();
                debugLog += $"资源类型：{assetType.FullName}\n";
                debugLog += $"资源类型名：{assetType.Name}\n\n";

                // 列出所有公开属性
                debugLog += "=== 公开属性 ===\n";
                PropertyInfo[] properties = assetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    debugLog += $"- {prop.Name} ({prop.PropertyType.Name})\n";
                }

                // 列出所有公开方法
                debugLog += "\n=== 公开方法 ===\n";
                MethodInfo[] methods = assetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    string paramStr = string.Join(", ", System.Array.ConvertAll(parameters, p => p.ParameterType.Name));
                    debugLog += $"- {method.Name}({paramStr})\n";
                }

                // 尝试调用 GetSkeletonData
                debugLog += "\n=== 尝试获取 SkeletonData ===\n";
                MethodInfo getSkeletonDataMethod = assetType.GetMethod(
                    "GetSkeletonData",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(bool) },
                    null
                );

                if (getSkeletonDataMethod != null)
                {
                    debugLog += "找到 GetSkeletonData(bool) 方法\n";
                    object skeletonData = getSkeletonDataMethod.Invoke(selectedSkeletonAsset, new object[] { true });

                    if (skeletonData != null)
                    {
                        debugLog += $"SkeletonData 不为 null\n";
                        debugLog += $"SkeletonData 类型：{skeletonData.GetType().FullName}\n\n";

                        Type skeletonDataType = skeletonData.GetType();

                        // 列出所有字段（包含私有）
                        debugLog += "=== SkeletonData 字段 (All) ===\n";
                        FieldInfo[] allFields = skeletonDataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var field in allFields)
                        {
                            debugLog += $"- {field.Name} ({field.FieldType.Name}) [";
                            debugLog += field.IsPublic ? "Public" : field.IsPrivate ? "Private" : "Protected";
                            debugLog += "]\n";
                        }

                        // 检查 skins 字段
                        debugLog += "\n=== 检查 skins 字段 ===\n";
                        FieldInfo skinsField = skeletonDataType.GetField(
                            "skins",
                            BindingFlags.NonPublic | BindingFlags.Instance
                        );

                        if (skinsField != null)
                        {
                            debugLog += "找到 skins 字段\n";
                            object skinsObj = skinsField.GetValue(skeletonData);
                            if (skinsObj != null)
                            {
                                debugLog += $"skins 不为 null\n";
                                debugLog += $"skins 类型：{skinsObj.GetType().FullName}\n";

                                debugLog += "\n=== 尝试获取 Count ===\n";

                                // 列出 ExposedList 所有成员
                                debugLog += "\n=== ExposedList 所有属性和字段 ===\n";
                                PropertyInfo[] allProps = skinsObj.GetType().GetProperties(
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                foreach (var prop in allProps)
                                {
                                    debugLog += $"  P: {prop.Name} ({prop.PropertyType.Name})\n";
                                }
                                FieldInfo[] allFields2 = skinsObj.GetType().GetFields(
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                foreach (var field in allFields2)
                                {
                                    debugLog += $"  F: {field.Name} ({field.FieldType.Name})\n";
                                }

                                // 方式1: 通过 Count 属性（多种 BindingFlags）
                                PropertyInfo countProp = skinsObj.GetType().GetProperty("Count",
                                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                                int count = -1;
                                if (countProp != null)
                                {
                                    try
                                    {
                                        count = (int)countProp.GetValue(skinsObj);
                                        debugLog += $"✓ 通过属性获取 Count: {count}\n";
                                    }
                                    catch (Exception ex)
                                    {
                                        debugLog += $"✗ Count 属性访问失败: {ex.Message}\n";
                                    }
                                }
                                else
                                {
                                    debugLog += "✗ 未找到 Count 属性\n";
                                }

                                // 方式2: 尝试直接作为 IList
                                if (count < 0)
                                {
                                    if (skinsObj is System.Collections.IList ilist)
                                    {
                                        count = ilist.Count;
                                        debugLog += $"✓ 通过 IList 获取 Count: {count}\n";
                                    }
                                }

                                if (count > 0)
                                {
                                    // 获取 Items
                                    debugLog += "\n=== 获取皮肤列表 ===\n";
                                    PropertyInfo itemsProp = skinsObj.GetType().GetProperty("Items",
                                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                                    if (itemsProp != null)
                                    {
                                        try
                                        {
                                            object itemsArray = itemsProp.GetValue(skinsObj);
                                            debugLog += $"✓ Items 类型：{itemsArray.GetType().FullName}\n";

                                            if (itemsArray is System.Collections.IList itemsList)
                                            {
                                                debugLog += $"\n=== {count} 个 Skin ===\n";
                                                for (int i = 0; i < count && i < itemsList.Count; i++)
                                                {
                                                    object skinObj = itemsList[i];
                                                    string skinName = ExtractSkinName(skinObj);
                                                    debugLog += $"[{i}] {skinName}\n";
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            debugLog += $"✗ Items 访问失败: {ex.Message}\n";
                                        }
                                    }
                                    else
                                    {
                                        debugLog += "✗ 未找到 Items 属性，尝试直接迭代\n";

                                        // 方式3: 直接迭代 IList
                                        if (skinsObj is System.Collections.IList ilist)
                                        {
                                            debugLog += $"✓ 直接作为 IList 迭代：\n";
                                            int idx = 0;
                                            foreach (object skinObj in ilist)
                                            {
                                                string skinName = ExtractSkinName(skinObj);
                                                debugLog += $"[{idx}] {skinName}\n";
                                                idx++;
                                            }
                                        }
                                    }
                                }
                                else if (count == 0)
                                {
                                    debugLog += $"✓ skins 数量为 0 (该模型没有多个皮肤配置)\n";
                                }
                                else
                                {
                                    debugLog += $"✗ 无法获取 skins 数量\n";
                                }
                            }
                            else
                            {
                                debugLog += "skins 为 null\n";
                            }
                        }
                        else
                        {
                            debugLog += "未找到 skins 字段\n";
                        }
                    }
                    else
                    {
                        debugLog += "GetSkeletonData 返回 null\n";
                    }
                }
                else
                {
                    debugLog += "未找到 GetSkeletonData(bool) 方法\n";
                }
            }
            catch (Exception ex)
            {
                debugLog += $"\n[异常] {ex.Message}\n{ex.StackTrace}";
            }
        }

        private string ExtractSkinName(object skinObj)
        {
            if (skinObj == null) return "null";

            try
            {
                Type skinType = skinObj.GetType();

                // 尝试 name 字段
                FieldInfo nameField = skinType.GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameField != null)
                {
                    object nameValue = nameField.GetValue(skinObj);
                    return nameValue?.ToString() ?? "null";
                }

                // 尝试 Name 属性
                PropertyInfo nameProp = skinType.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (nameProp != null)
                {
                    object nameValue = nameProp.GetValue(skinObj);
                    return nameValue?.ToString() ?? "null";
                }

                return skinType.Name;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}

