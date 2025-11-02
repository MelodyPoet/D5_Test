using UnityEngine;
using UnityEngine.EventSystems;

namespace demo2.DND.Utility
{
    // Ensure this runs early so we can clear selection before the StandaloneInputModule processes Submit
    [DefaultExecutionOrder(-1000)]
    public class PreventSpaceSubmit : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var es = EventSystem.current;
                if (es != null)
                {
                    // Temporarily disable StandaloneInputModule to prevent it from processing a Submit on this frame
                    var standalone = es.currentInputModule as StandaloneInputModule;
                    if (standalone != null && standalone.enabled)
                    {
                        try { standalone.enabled = false; } catch { }
                        StartCoroutine(ReenableInputModuleNextFrame(standalone));
                    }

                    // Clear selection so even if some other input module processes submit, nothing is selected
                    if (es.currentSelectedGameObject != null)
                    {
                        es.SetSelectedGameObject(null);
                    }

                    if (Debug.isDebugBuild) Debug.Log("[PreventSpaceSubmit] Suppressed Space submit by disabling input module and clearing selection.");
                }
            }
        }

        private System.Collections.IEnumerator ReenableInputModuleNextFrame(StandaloneInputModule module)
        {
            // Wait a single frame to let any pending input processing finish without a selected target
            yield return null;
            if (module != null)
            {
                try { module.enabled = true; } catch { }
            }
            if (Debug.isDebugBuild) Debug.Log("[PreventSpaceSubmit] Re-enabled StandaloneInputModule");
        }

        // Auto-register helper: ensure one instance exists at runtime so the suppression works without manual scene wiring
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            // If any existing instance exists in scene, nothing to do
            var existing = Object.FindObjectOfType<PreventSpaceSubmit>();
            if (existing != null) return;

            // Create a hidden GameObject to host the component
            var go = new GameObject("__PreventSpaceSubmit");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<PreventSpaceSubmit>();
            if (Debug.isDebugBuild) Debug.Log("[PreventSpaceSubmit] Auto-created singleton instance.");
        }
    }
}
