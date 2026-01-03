using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace demo2.DND.Editor
{
    /// <summary>
    /// Spine 类型验证工具 - 诊断 Spine-Unity 是否正确安装
    /// </summary>
    public class SpineTypeVerifier
    {
        [MenuItem("Assets/DND/Debug/Verify Spine Installation")]
        public static void ManualVerifySpine()
        {
            var result = new Dictionary<string, object>();
            result["timestamp"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                // 1. 检查已加载的程序集
                result["LoadedAssemblies"] = GetSpineRelatedAssemblies();

                // 2. 检查 Spine 类型
                result["SpineTypes"] = GetSpineTypes();

                // 3. 检查 SkeletonDataAsset 方法
                result["SkeletonDataAssetMethods"] = GetSkeletonDataAssetMethods();

                // 4. 检查文件系统
                result["FileSystem"] = CheckFileSystem();
            }
            catch (Exception ex)
            {
                result["Error"] = ex.Message;
            }

            // 显示结果
            ShowVerificationWindow(result);
        }

        private static List<string> GetSpineRelatedAssemblies()
        {
            var result = new List<string>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly asm in assemblies)
            {
                string name = asm.GetName().Name;
                if (name.Contains("Spine") || name.Contains("spine"))
                {
                    result.Add($"{name} ({asm.Location})");
                }
            }

            if (result.Count == 0)
            {
                result.Add("未找到任何 Spine 相关程序集");
            }

            return result;
        }

        private static Dictionary<string, bool> GetSpineTypes()
        {
            var result = new Dictionary<string, bool>();

            // 尝试加载 Spine 类型
            Type[] typesToCheck = new Type[]
            {
                Type.GetType("Spine.Unity.SkeletonDataAsset"),
                Type.GetType("Spine.SkeletonData"),
                Type.GetType("Spine.Skin"),
                Type.GetType("Spine.Attachment"),
                Type.GetType("Spine.Skeleton")
            };

            string[] typeNames = new string[]
            {
                "Spine.Unity.SkeletonDataAsset",
                "Spine.SkeletonData",
                "Spine.Skin",
                "Spine.Attachment",
                "Spine.Skeleton"
            };

            for (int i = 0; i < typesToCheck.Length; i++)
            {
                result[typeNames[i]] = typesToCheck[i] != null;
            }

            return result;
        }

        private static List<string> GetSkeletonDataAssetMethods()
        {
            var result = new List<string>();

            try
            {
                Type skeletonDataAssetType = Type.GetType("Spine.Unity.SkeletonDataAsset");
                if (skeletonDataAssetType == null)
                {
                    result.Add("SkeletonDataAsset 类型未加载");
                    return result;
                }

                MethodInfo[] methods = skeletonDataAssetType.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
                );

                if (methods.Length == 0)
                {
                    result.Add("未找到公开方法");
                }
                else
                {
                    foreach (var method in methods)
                    {
                        var parameters = method.GetParameters();
                        string paramStr = string.Join(", ",
                            System.Array.ConvertAll(parameters, p => p.ParameterType.Name)
                        );
                        result.Add($"{method.Name}({paramStr})");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Add($"异常: {ex.Message}");
            }

            return result;
        }

        private static Dictionary<string, bool> CheckFileSystem()
        {
            var result = new Dictionary<string, bool>();

            // 检查常见路径
            string[] pathsToCheck = new string[]
            {
                "Assets/Spine",
                "Assets/Plugins/Spine",
                "Assets/Spine/Editor",
                "Assets/Spine/Runtime"
            };

            foreach (var path in pathsToCheck)
            {
                bool exists = System.IO.Directory.Exists(path);
                result[path] = exists;
            }

            return result;
        }

        private static void ShowVerificationWindow(Dictionary<string, object> result)
        {
            var window = ScriptableObject.CreateInstance<VerificationWindow>();
            window.SetData(result);
            window.ShowModal();
        }
    }

    /// <summary>
    /// 验证结果展示窗口
    /// </summary>
    public class VerificationWindow : EditorWindow
    {
        private Dictionary<string, object> data;
        private Vector2 scrollPos;
        private string displayText = "";

        public void SetData(Dictionary<string, object> result)
        {
            data = result;
            FormatData();
        }

        private void FormatData()
        {
            var sb = new System.Text.StringBuilder();

            foreach (var kvp in data)
            {
                sb.AppendLine($"=== {kvp.Key} ===");

                if (kvp.Value is List<string> list)
                {
                    foreach (var item in list)
                    {
                        sb.AppendLine($"  • {item}");
                    }
                }
                else if (kvp.Value is Dictionary<string, bool> dict)
                {
                    foreach (var item in dict)
                    {
                        string status = item.Value ? "✓" : "✗";
                        sb.AppendLine($"  {status} {item.Key}");
                    }
                }
                else if (kvp.Value is Dictionary<string, object> objDict)
                {
                    foreach (var item in objDict)
                    {
                        sb.AppendLine($"  {item.Key}: {item.Value}");
                    }
                }
                else
                {
                    sb.AppendLine($"  {kvp.Value}");
                }

                sb.AppendLine();
            }

            displayText = sb.ToString();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Spine 安装验证报告", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.TextArea(displayText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("关闭", GUILayout.Height(30)))
            {
                Close();
            }
        }
    }
}

