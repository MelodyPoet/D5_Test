using UnityEngine;
using UnityEngine.UI;

namespace demo2.DND.InventoryTetris
{
    [RequireComponent(typeof(Button))]
    public class PanelNavBridge : MonoBehaviour
    {
        public enum NavType { Next, Prev }
        public NavType nav = NavType.Next;

        private Button btn;

        private void Awake()
        {
            btn = GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (btn == null) btn = GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            // Find any active UITabSwitcher in the scene and forward the click.
            var sw = FindObjectOfType<UITabSwitcher>();
            if (sw == null) return;
            if (nav == NavType.Next) sw.PanelNext(); else sw.PanelPrev();
        }
    }
}
