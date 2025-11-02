using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using System.IO;

namespace Editor
{
    // Editor utility to ensure Pause buttons don't also toggle inventory UI
    public static class UnlinkPauseFromInventory
    {
        [MenuItem("Tools/Unlink Pause From Inventory Toggles")]
        public static void Run()
        {
            var buttons = Object.FindObjectsOfType<Button>(true);
            int totalChecked = 0;
            int fixedCount = 0;
            foreach (var b in buttons)
            {
                if (b == null) continue;
                totalChecked++;
                var so = new SerializedObject(b);
                var onClick = so.FindProperty("m_OnClick");
                if (onClick == null) continue;
                var calls = onClick.FindPropertyRelative("m_PersistentCalls.m_Calls");
                if (calls == null) continue;

                bool hasPauseCall = false;
                // First scan to see if this button also calls PauseController.Toggle
                for (int i = 0; i < calls.arraySize; i++)
                {
                    var call = calls.GetArrayElementAtIndex(i);
                    var targetProp = call.FindPropertyRelative("m_Target");
                    var methodName = call.FindPropertyRelative("m_MethodName")?.stringValue ?? "";
                    var targetObj = targetProp?.objectReferenceValue;
                    if (targetObj != null && targetObj.GetType().Name == "PauseController" && methodName == "Toggle")
                    {
                        hasPauseCall = true;
                        break;
                    }
                }

                if (!hasPauseCall) continue;

                // If the button calls PauseController.Toggle, remove any persistent bindings to UITabSwitcher inventory methods
                bool changed = false;
                for (int i = calls.arraySize - 1; i >= 0; i--)
                {
                    var call = calls.GetArrayElementAtIndex(i);
                    var targetProp = call.FindPropertyRelative("m_Target");
                    var methodProp = call.FindPropertyRelative("m_MethodName");
                    var targetObj = targetProp?.objectReferenceValue;
                    var methodName = methodProp?.stringValue ?? "";
                    if (targetObj == null) continue;
                    var typeName = targetObj.GetType().Name;
                    // Remove UITabSwitcher.ToggleBackpack/ShowBackpack/ToggleCharacter/ShowCharacter
                    if (typeName == "UITabSwitcher" && (methodName == "ToggleBackpack" || methodName == "ShowBackpack" || methodName == "ToggleCharacter" || methodName == "ShowCharacter"))
                    {
                        // Remove this persistent call
                        calls.DeleteArrayElementAtIndex(i);
                        changed = true;
                    }
                }

                if (changed)
                {
                    so.ApplyModifiedProperties();
                    EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
                    fixedCount++;
                    Debug.Log($"[UnlinkPauseFromInventory] Fixed Button '{b.gameObject.name}' in scene '{b.gameObject.scene.name}'");
                }
            }

            Debug.Log($"[UnlinkPauseFromInventory] Completed. Buttons scanned: {totalChecked}, Buttons fixed: {fixedCount}");

            // Additionally, scan all prefabs in the Assets folder and fix persistent bindings inside them
            int prefabTotal = 0;
            int prefabFixed = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            for (int gi = 0; gi < guids.Length; gi++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[gi]);
                if (string.IsNullOrEmpty(path)) continue;
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null) continue;
                    var btns = root.GetComponentsInChildren<Button>(true);
                    if (btns == null || btns.Length == 0) { PrefabUtility.UnloadPrefabContents(root); continue; }

                    bool anyChangedOnThisPrefab = false;
                    prefabTotal += btns.Length;
                    for (int i = 0; i < btns.Length; i++)
                    {
                        var b = btns[i];
                        if (b == null) continue;
                        var so = new SerializedObject(b);
                        var onClick = so.FindProperty("m_OnClick");
                        if (onClick == null) continue;
                        var calls = onClick.FindPropertyRelative("m_PersistentCalls.m_Calls");
                        if (calls == null) continue;

                        bool hasPauseCall = false;
                        for (int ci = 0; ci < calls.arraySize; ci++)
                        {
                            var call = calls.GetArrayElementAtIndex(ci);
                            var targetProp = call.FindPropertyRelative("m_Target");
                            var methodName = call.FindPropertyRelative("m_MethodName")?.stringValue ?? "";
                            var targetObj = targetProp?.objectReferenceValue;
                            if (targetObj != null && targetObj.GetType().Name == "PauseController" && methodName == "Toggle")
                            {
                                hasPauseCall = true;
                                break;
                            }
                        }

                        if (!hasPauseCall) continue;

                        bool changed = false;
                        for (int ci = calls.arraySize - 1; ci >= 0; ci--)
                        {
                            var call = calls.GetArrayElementAtIndex(ci);
                            var targetProp = call.FindPropertyRelative("m_Target");
                            var methodProp = call.FindPropertyRelative("m_MethodName");
                            var targetObj = targetProp?.objectReferenceValue;
                            var methodName = methodProp?.stringValue ?? "";
                            if (targetObj == null) continue;
                            var typeName = targetObj.GetType().Name;
                            if (typeName == "UITabSwitcher" && (methodName == "ToggleBackpack" || methodName == "ShowBackpack" || methodName == "ToggleCharacter" || methodName == "ShowCharacter"))
                            {
                                calls.DeleteArrayElementAtIndex(ci);
                                changed = true;
                            }
                        }

                        if (changed)
                        {
                            so.ApplyModifiedProperties();
                            anyChangedOnThisPrefab = true;
                        }
                    }

                    if (anyChangedOnThisPrefab)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        prefabFixed++;
                        Debug.Log($"[UnlinkPauseFromInventory] Fixed prefab at '{path}'");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[UnlinkPauseFromInventory] Error processing prefab '{path}': {ex}");
                }
                finally
                {
                    if (root != null) PrefabUtility.UnloadPrefabContents(root);
                }
            }

            Debug.Log($"[UnlinkPauseFromInventory] Prefab scan completed. Prefabs checked: {guids.Length}, Prefabs fixed: {prefabFixed}");
        }
    }
}
