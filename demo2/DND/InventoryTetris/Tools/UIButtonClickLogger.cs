using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace demo2.DND.InventoryTetris.Tools
{
    // Attach this to any Button to log clicks and useful state for debugging.
    public class UIButtonClickLogger : MonoBehaviour, IPointerClickHandler
    {
        public string tagInfo;

        private Button _btn;

        private void Awake()
        {
            _btn = GetComponent<Button>();
        }

        private void OnEnable()
        {
            bool interactable = (_btn != null) ? _btn.interactable : false;
            Debug.Log($"[UIButtonClickLogger] OnEnable -> GO={gameObject.name}, activeInHierarchy={gameObject.activeInHierarchy}, interactable={interactable}, tagInfo={tagInfo}");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            bool interactable = (_btn != null) ? _btn.interactable : false;
            Debug.Log($"[UIButtonClickLogger] OnPointerClick -> GO={gameObject.name}, activeInHierarchy={gameObject.activeInHierarchy}, interactable={interactable}, clickCount={eventData.clickCount}, button={eventData.button}");
        }
    }
}
