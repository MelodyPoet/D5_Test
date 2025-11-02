using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

namespace Editor
{
    // Diagnostic tool: list all Buttons and their persistent onClick bindings,
    // and flag those that call UITabSwitcher.* or PauseController.Toggle
    public static class ListInventoryBindings
    {
        [MenuItem("Tools/List Inventory Button Bindings")]
        public static void Run()
        {
            var buttons = Object.FindObjectsOfType<Button>(true);
            Debug.Log($"[ListInventoryBindings] Found {buttons.Length} Button(s) in loaded scenes.");

            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                if (b == null) continue;
                string sceneName = b.gameObject.scene.IsValid() ? b.gameObject.scene.name : "<no-scene>";
                Debug.Log($"Button #{i}: '{b.gameObject.name}' (Scene='{sceneName}', Active={b.gameObject.activeInHierarchy})");

                int persistent = b.onClick.GetPersistentEventCount();
                Debug.Log($"  - persistent call count: {persistent}");
                bool callsPause = false;
                bool callsUITab = false;

                for (int pi = 0; pi < persistent; pi++)
                {
                    var target = b.onClick.GetPersistentTarget(pi) as Object;
                    var method = b.onClick.GetPersistentMethodName(pi) ?? "<no-method>";
                    string targetName = target != null ? target.name : "<null>";
                    string targetType = target != null ? target.GetType().FullName : "<null>";
                    Debug.Log($"    [{pi}] method='{method}', target={targetName}, type={targetType}");

                    if (method == "Toggle" && target != null && target.GetType().Name == "PauseController") callsPause = true;
                    if ((method == "ToggleBackpack" || method == "ShowBackpack" || method == "ToggleCharacter" || method == "ShowCharacter") && target != null && target.GetType().Name == "UITabSwitcher") callsUITab = true;
                }

                if (callsPause && callsUITab)
                {
                    Debug.LogWarning($"  >> Button '{b.gameObject.name}' calls both PauseController.Toggle and UITabSwitcher inventory methods. This can cause pause-to-UI coupling.");
                }
                else if (callsPause)
                {
                    Debug.Log($"  >> Button '{b.gameObject.name}' calls PauseController.Toggle (pause button).");
                }
                else if (callsUITab)
                {
                    Debug.Log($"  >> Button '{b.gameObject.name}' calls UITabSwitcher inventory methods (inventory button).");
                }
            }

            Debug.Log("[ListInventoryBindings] Done.");
        }
    }
}

