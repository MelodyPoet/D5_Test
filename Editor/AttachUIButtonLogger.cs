using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

namespace Editor
{
    public static class AttachUIButtonLogger
    {
        [MenuItem("Tools/Attach UIButtonClickLogger to Backpack Buttons")]
        public static void AttachLogger()
        {
            var names = new[] { "Btn_BpNext", "Btn_BpPrev", "Btn_BpNext(Clone)", "Btn_BpPrev(Clone)", "BpNext", "BpPrev" };
            var buttons = Object.FindObjectsOfType<Button>(true);
            int attached = 0;
            foreach (var b in buttons)
            {
                if (b == null) continue;
                string n = b.gameObject.name;
                bool match = false;
                foreach (var mn in names) if (n.Contains(mn)) { match = true; break; }
                if (!match) continue;
                var comp = b.gameObject.GetComponent<demo2.DND.InventoryTetris.Tools.UIButtonClickLogger>();
                if (comp == null)
                {
                    b.gameObject.AddComponent<demo2.DND.InventoryTetris.Tools.UIButtonClickLogger>();
                    attached++;
                    Debug.Log($"[AttachUIButtonLogger] Attached to '{b.gameObject.name}'");
                    EditorUtility.SetDirty(b.gameObject);
                }
                else
                {
                    Debug.Log($"[AttachUIButtonLogger] Already present on '{b.gameObject.name}'");
                }
            }
            if (attached > 0) EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[AttachUIButtonLogger] Done. Attached to {attached} Button(s).");
        }
    }
}
