using UnityEngine;
using UnityEngine.UI;

namespace demo2.DND.InventoryTetris
{
    public class TooltipSystem : MonoBehaviour
    {
        private static TooltipSystem _instance;
        public Text tooltipText;
        public RectTransform tooltipBackground;
        private CanvasGroup _canvasGroup;

        void Awake()
        {
            _instance = this;
            // 获取或添加 CanvasGroup 组件
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            gameObject.SetActive(false);
        }

        public static void Show(ItemInstance item)
        {
            if (item == null || item.data == null) return;

            _instance.tooltipText.text = item.data.GetTooltipInfo();
            _instance.gameObject.SetActive(true);
            _instance._canvasGroup.blocksRaycasts = false; // 禁用射线检测，防止 Tooltip 自身捕获鼠标事件
            _instance.UpdatePosition();
        }

        public static void Hide()
        {
            if (_instance != null && _instance.gameObject.activeSelf)
            {
                _instance.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent as RectTransform, Input.mousePosition, null, out localPoint);
            transform.localPosition = localPoint;
        }
    }
}
