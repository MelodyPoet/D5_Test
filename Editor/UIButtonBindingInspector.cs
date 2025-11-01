using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using demo2.DND.InventoryTetris;
using System.Linq;

// Editor utility: 扫描场景中所有 Button 的持久化 onClick 绑定，列出目标与方法
public static class UIButtonBindingInspector
{
    [MenuItem("Tools/Inspect UI Button Bindings")]
    public static void InspectBindings()
    {
        var buttons = Object.FindObjectsOfType<Button>(true);
        Debug.Log($"[UIButtonBindingInspector] Found {buttons.Length} Button(s) in loaded scenes.");
        foreach (var b in buttons)
        {
            if (b == null) continue;
            string sceneName = b.gameObject.scene.IsValid() ? b.gameObject.scene.name : "<no-scene>";
            Debug.Log($"Button: '{b.gameObject.name}' (Scene='{sceneName}', Active={b.gameObject.activeInHierarchy})");

            var so = new SerializedObject(b);
            var onClick = so.FindProperty("m_OnClick");
            if (onClick == null)
            {
                Debug.Log("  - no onClick property found (unexpected)");
                continue;
            }

            var calls = onClick.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls == null)
            {
                Debug.Log("  - no persistent calls found");
                continue;
            }

            int count = calls.arraySize;
            Debug.Log("  - persistent call count: " + count);
            for (int i = 0; i < count; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                var targetProp = call.FindPropertyRelative("m_Target");
                var methodName = call.FindPropertyRelative("m_MethodName")?.stringValue ?? "<no-method>";
                var targetObj = targetProp?.objectReferenceValue;
                string targetInfo = targetObj == null ? "<null>" : $"{targetObj.name} (type={targetObj.GetType().Name})";
                Debug.Log($"    [{i}] method='{methodName}', target={targetInfo}");
            }
        }

        Debug.Log("[UIButtonBindingInspector] InspectBindings finished.");
    }

    [MenuItem("Tools/Fix InventoryUIBinder Button Targets")]
    public static void FixInventoryBinderBindings()
    {
        var buttons = Object.FindObjectsOfType<Button>(true);
        var binders = Object.FindObjectsOfType<InventoryUIBinder>(true);
        if (binders == null || binders.Length == 0)
        {
            Debug.LogWarning("[UIButtonBindingInspector] No InventoryUIBinder instances found in loaded scenes. Aborting fix.");
            return;
        }

        int fixedCount = 0;
        foreach (var b in buttons)
        {
            if (b == null) continue;
            var so = new SerializedObject(b);
            var onClick = so.FindProperty("m_OnClick");
            if (onClick == null) continue;
            var calls = onClick.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls == null) continue;

            bool changed = false;

            // For this button, pick the best binder candidate
            InventoryUIBinder binderForButton = b.GetComponentInParent<InventoryUIBinder>();
            if (binderForButton == null)
            {
                // Try binder in same root (same transform.root)
                var sameRoot = binders.FirstOrDefault(x => x.gameObject.scene == b.gameObject.scene && x.gameObject.transform.root == b.gameObject.transform.root);
                if (sameRoot != null) binderForButton = sameRoot;
            }
            if (binderForButton == null)
            {
                // Try any binder in same scene
                var sameScene = binders.FirstOrDefault(x => x.gameObject.scene == b.gameObject.scene);
                if (sameScene != null) binderForButton = sameScene;
            }
            if (binderForButton == null)
            {
                // Fallback to first found in project
                binderForButton = binders[0];
            }

            for (int i = 0; i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                var targetProp = call.FindPropertyRelative("m_Target");
                var methodName = call.FindPropertyRelative("m_MethodName")?.stringValue ?? "";
                var targetObj = targetProp?.objectReferenceValue;
                if (targetObj == null && (methodName == "NextCharacter" || methodName == "PrevCharacter"))
                {
                    var chosen = binderForButton as UnityEngine.Object;
                    targetProp.objectReferenceValue = chosen;
                    changed = true;
                    Debug.Log($"[UIButtonBindingInspector] Rewired Button '{b.gameObject.name}' persistent call #{i} method='{methodName}' to InventoryUIBinder on '{binderForButton.gameObject.name}' (reason: selected by proximity/scene).");
                }
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
                fixedCount++;
            }
        }

        Debug.Log($"[UIButtonBindingInspector] Fix completed. Buttons fixed: {fixedCount}");
    }
}
