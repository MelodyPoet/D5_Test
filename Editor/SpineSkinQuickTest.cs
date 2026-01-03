using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace demo2.DND.Editor
{
    /// <summary>
    /// Spine 皮肤提取快速诊断 - 最小化的反射方案
    /// 目的：快速找出 skins 是否真的有数据
    /// </summary>
    public class SpineSkinQuickTest
    {
        [MenuItem("Assets/DND/Debug/Quick Spine Skin Test")]
        public static void QuickTest()
        {
            var selected = Selection.activeObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选中一个 SkeletonDataAsset", "OK");
                return;
            }

            string report = TestSkinExtraction(selected);
            EditorUtility.DisplayDialog("Spine 皮肤提取测试", report, "OK");
            Debug.Log($"[SpineSkinQuickTest]\n{report}");
        }

        private static string TestSkinExtraction(UnityEngine.Object asset)
        {
            var sb = new System.Text.StringBuilder();

            try
            {
                Type assetType = asset.GetType();
                sb.AppendLine($"资源类型: {assetType.Name}");

                // 调用 GetSkeletonData
                var method = assetType.GetMethod("GetSkeletonData", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method == null)
                {
                    sb.AppendLine("ERROR: 无法找到 GetSkeletonData 方法");
                    return sb.ToString();
                }

                object skeletonData = method.Invoke(asset, new object[] { true });
                if (skeletonData == null)
                {
                    sb.AppendLine("ERROR: GetSkeletonData 返回 null");
                    return sb.ToString();
                }

                Type skeletonDataType = skeletonData.GetType();
                sb.AppendLine($"SkeletonData 类型: {skeletonDataType.Name}");

                // 获取 skins 字段
                var skinsField = skeletonDataType.GetField("skins", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (skinsField == null)
                {
                    sb.AppendLine("ERROR: 找不到 skins 字段");
                    return sb.ToString();
                }

                object skinsObj = skinsField.GetValue(skeletonData);
                if (skinsObj == null)
                {
                    sb.AppendLine("✓ skins 字段为 null (该模型没有多皮肤)");
                    return sb.ToString();
                }

                Type skinsType = skinsObj.GetType();
                sb.AppendLine($"skins 类型: {skinsType.Name}");

                // 直接尝试作为 ICollection
                if (skinsObj is System.Collections.ICollection collection)
                {
                    sb.AppendLine($"✓ skins 是 ICollection");
                    sb.AppendLine($"Count: {collection.Count}");

                    if (collection.Count > 0)
                    {
                        sb.AppendLine("\nSkins:");
                        int idx = 0;
                        foreach (var skin in collection)
                        {
                            string skinName = ExtractSkinNameQuick(skin);
                            sb.AppendLine($"  [{idx}] {skinName}");
                            idx++;
                        }
                    }
                    else
                    {
                        sb.AppendLine("(Count = 0，该模型没有多皮肤配置)");
                    }
                }
                else if (skinsObj is System.Collections.IEnumerable enumerable)
                {
                    sb.AppendLine($"✓ skins 是 IEnumerable (但不是 ICollection)");
                    sb.AppendLine("\nSkins:");
                    int idx = 0;
                    foreach (var skin in enumerable)
                    {
                        string skinName = ExtractSkinNameQuick(skin);
                        sb.AppendLine($"  [{idx}] {skinName}");
                        idx++;
                    }
                }
                else
                {
                    sb.AppendLine($"ERROR: skins 既不是 ICollection 也不是 IEnumerable");
                    sb.AppendLine($"类型: {skinsType.FullName}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"EXCEPTION: {ex.Message}");
                sb.AppendLine(ex.StackTrace);
            }

            return sb.ToString();
        }

        private static string ExtractSkinNameQuick(object skinObj)
        {
            if (skinObj == null) return "null";

            try
            {
                Type skinType = skinObj.GetType();

                // 尝试 name 字段
                var field = skinType.GetField("name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    object value = field.GetValue(skinObj);
                    return value?.ToString() ?? "null";
                }

                // 尝试 Name 属性
                var prop = skinType.GetProperty("Name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    object value = prop.GetValue(skinObj);
                    return value?.ToString() ?? "null";
                }

                return skinType.Name;
            }
            catch
            {
                return "Error";
            }
        }
    }
}

