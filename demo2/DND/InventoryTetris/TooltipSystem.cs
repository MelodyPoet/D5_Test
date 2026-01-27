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
        private ItemInstance _currentItem;
        private RectTransform _currentAnchor;
        private bool _followMouse = false;

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

        /// <summary>
        /// Show tooltip for the given item. If anchor is provided, tooltip will be positioned relative to that RectTransform once and will not follow the mouse.
        /// If followMouse is true, the tooltip will follow the mouse until Hide() is called.
        /// </summary>
        public static void Show(ItemInstance item, RectTransform anchor = null, bool followMouse = false)
        {
            if (_instance == null) return;
            if (item == null || item.data == null) return;

            // If already showing same item with same anchor and followMouse flag, do nothing
            if (_instance.gameObject.activeSelf && _instance._currentItem == item && _instance._currentAnchor == anchor && _instance._followMouse == followMouse)
            {
                return;
            }

            _instance._currentItem = item;
            _instance._currentAnchor = anchor;
            _instance._followMouse = followMouse;

            _instance.tooltipText.text = item.data.GetTooltipInfo();
            _instance.gameObject.SetActive(true);
            _instance._canvasGroup.blocksRaycasts = false; // 禁用射线检测，防止 Tooltip 自身捕获鼠标事件

            // Position once now. If followMouse is true, Update() will continue to reposition.
            _instance.UpdatePosition();
        }

        public static void Hide()
        {
            if (_instance != null && _instance.gameObject.activeSelf)
            {
                _instance._currentItem = null;
                _instance._currentAnchor = null;
                _instance._followMouse = false;
                _instance.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_followMouse)
            {
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            var parentRect = transform.parent as RectTransform;
            if (parentRect == null) return;

            Vector2 screenPoint;
            // If an anchor RectTransform is provided, compute its center in screen space
            if (_currentAnchor != null)
            {
                Vector3[] corners = new Vector3[4];
                _currentAnchor.GetWorldCorners(corners);
                Vector3 worldCenter = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;

                // Determine canvas render mode to select correct camera behavior
                Canvas canvas = parentRect.GetComponentInParent<Canvas>();
                Camera cam = null;
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    cam = canvas.worldCamera;
                }

                screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
            }
            else
            {
                // Default: use current mouse position
                screenPoint = Input.mousePosition;
            }

            // Convert screen point to parent's local point
            Camera camForParent = null;
            var pCanvas = parentRect.GetComponentInParent<Canvas>();
            if (pCanvas != null && pCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camForParent = pCanvas.worldCamera;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, camForParent, out Vector2 localPoint))
            {
                transform.localPosition = localPoint;
            }
        }
    }
}
