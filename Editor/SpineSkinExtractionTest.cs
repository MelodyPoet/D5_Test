using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace demo2.DND.Editor
{
    /// <summary>
    /// Spine 皮肤提取功能测试 - 验证改进后的 ExtractSkinNamesFromAsset 是否工作
    /// </summary>
    public class SpineSkinExtractionTest
    {
        [MenuItem("Assets/DND/Debug/Test Skin Extraction")]
        public static void TestExtraction()
        {
            var selected = Selection.activeObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选中一个 SkeletonDataAsset", "OK");
                return;
            }

            Debug.Log("[SpineSkinExtractionTest] 开始测试...");
            var skins = ExtractSkinNames(selected);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"提取结果：{skins.Count} 个皮肤");
            sb.AppendLine();

            foreach (var skin in skins)
            {
                sb.AppendLine($"  • {skin}");
            }

            string result = sb.ToString();
            EditorUtility.DisplayDialog("皮肤提取测试结果", result, "OK");
            Debug.Log(result);
        }

        /// <summary>
        /// 核心提取方法 - 与 SpineSkinImporter 中的逻辑相同
        /// </summary>
        private static List<string> ExtractSkinNames(UnityEngine.Object asset)
        {
            var result = new List<string>();

            try
            {
                if (asset == null)
                {
                    Debug.LogError("[Test] 资源对象为空");
                    return result;
                }

                Type assetType = asset.GetType();
                Debug.Log($"[Test] 资源类型: {assetType.FullName}");

                // 反射调用 GetSkeletonData(bool)
                MethodInfo getSkeletonDataMethod = assetType.GetMethod(
                    "GetSkeletonData",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(bool) },
                    null
                );

                if (getSkeletonDataMethod == null)
                {
                    Debug.LogError($"[Test] 无法找到 GetSkeletonData(bool) 方法");
                    return result;
                }

                object skeletonData = getSkeletonDataMethod.Invoke(asset, new object[] { true });
                if (skeletonData == null)
                {
                    Debug.LogError("[Test] GetSkeletonData 返回 null");
                    return result;
                }

                Type skeletonDataType = skeletonData.GetType();
                Debug.Log($"[Test] SkeletonData 类型: {skeletonDataType.FullName}");

                // 获取 skins 字段
                FieldInfo skinsField = skeletonDataType.GetField(
                    "skins",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (skinsField == null)
                {
                    Debug.LogError($"[Test] 无法找到 skins 字段");
                    return result;
                }

                object skinsObj = skinsField.GetValue(skeletonData);
                if (skinsObj == null)
                {
                    Debug.Log("[Test] skins 字段为 null");
                    return result;
                }

                Type skinsType = skinsObj.GetType();
                Debug.Log($"[Test] skins 类型: {skinsType.FullName}");

                // ===== 方案 1: 通过 Count + Items =====
                PropertyInfo countProperty = skinsType.GetProperty("Count",
                    BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
                PropertyInfo itemsProperty = skinsType.GetProperty("Items",
                    BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);

                if (countProperty != null && itemsProperty != null)
                {
                    try
                    {
                        int count = (int)countProperty.GetValue(skinsObj);
                        Debug.Log($"[Test] 方案1: Count={count}");

                        if (count > 0)
                        {
                            object itemsArray = itemsProperty.GetValue(skinsObj);
                            if (itemsArray is System.Collections.IList itemsList)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    object skinObj = itemsList[i];
                                    if (skinObj == null) continue;

                                    string skinName = ExtractSkinName(skinObj);
                                    if (!string.IsNullOrEmpty(skinName))
                                    {
                                        result.Add(skinName);
                                    }
                                }
                                Debug.Log($"[Test] 方案1 成功，提取 {result.Count} 个皮肤");
                                return result;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Test] 方案1 失败: {ex.Message}");
                    }
                }

                // ===== 方案 2: 作为 ICollection =====
                if (skinsObj is System.Collections.ICollection collection)
                {
                    Debug.Log($"[Test] 方案2: skins 是 ICollection");
                    int idx = 0;
                    foreach (object skinObj in collection)
                    {
                        if (skinObj == null) continue;
                        string skinName = ExtractSkinName(skinObj);
                        if (!string.IsNullOrEmpty(skinName))
                        {
                            result.Add(skinName);
                        }
                        idx++;
                    }

                    if (result.Count > 0)
                    {
                        Debug.Log($"[Test] 方案2 成功，提取 {result.Count} 个皮肤");
                        return result;
                    }
                }

                // ===== 方案 3: 作为 IEnumerable =====
                if (skinsObj is System.Collections.IEnumerable enumerable)
                {
                    Debug.Log($"[Test] 方案3: skins 是 IEnumerable");
                    int idx = 0;
                    foreach (object skinObj in enumerable)
                    {
                        if (skinObj == null) continue;
                        string skinName = ExtractSkinName(skinObj);
                        if (!string.IsNullOrEmpty(skinName))
                        {
                            result.Add(skinName);
                        }
                        idx++;
                    }

                    if (result.Count > 0)
                    {
                        Debug.Log($"[Test] 方案3 成功，提取 {result.Count} 个皮肤");
                        return result;
                    }
                }

                Debug.LogWarning($"[Test] 所有方案都失败");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Test] 异常: {ex.Message}\n{ex.StackTrace}");
            }

            return result;
        }

        private static string ExtractSkinName(object skinObj)
        {
            if (skinObj == null) return null;

            try
            {
                Type skinType = skinObj.GetType();

                // 尝试 name 字段
                FieldInfo nameField = skinType.GetField(
                    "name",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (nameField != null)
                {
                    return nameField.GetValue(skinObj) as string;
                }

                // 尝试 Name 属性
                PropertyInfo nameProperty = skinType.GetProperty(
                    "Name",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                );

                if (nameProperty != null)
                {
                    return nameProperty.GetValue(skinObj) as string;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Test] 提取皮肤名称失败: {ex.Message}");
            }

            return null;
        }
    }
}

