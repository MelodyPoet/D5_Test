using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 控制物品的拖拽、旋转和放置（全局捕获 PointerDown）。
    /// </summary>
    public class ItemDragController : MonoBehaviour, IPointerDownHandler
    {
        public static ItemDragController Current { get; private set; }

        private InventoryItemView heldView;
        private ItemInstance heldItem;
        private InventoryGridView sourceGrid;
        private Vector2Int originalPosition;

        private Canvas canvas;
        private RectTransform draggingIcon;
        private bool isHoldingItem;
        public bool IsHoldingItem => isHoldingItem;

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Destroy(gameObject);
                return;
            }
            Current = this;
            canvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            if (!isHoldingItem) return;

            UpdateHeldItemPosition();
            if (sourceGrid != null && heldItem != null)
            {
                sourceGrid.ShowPlacementPreview(heldItem, GetGridPosition(Input.mousePosition));
            }
        }

        /// <summary>
        /// 全局 PointerDown：
        /// - 左键未持有：在当前指针下查找 InventoryItemView 并拾取；
        /// - 左键已持有：尝试放下到指针所在背包格；
        /// - 右键已持有：旋转 Held 物品。
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log($"[DragCtrl] OnPointerDown button={eventData.button} isHolding={isHoldingItem}");

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (!isHoldingItem)
                {
                    TryBeginHoldFromPointer(eventData);
                }
                else
                {
                    TryEndHoldAtPointer(eventData);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (isHoldingItem)
                {
                    RotateHeld();
                }
            }
        }

        private void TryBeginHoldFromPointer(PointerEventData eventData)
        {
            // 从 Pointer 的 Raycast 结果中查找 InventoryItemView
            var go = eventData.pointerPressRaycast.gameObject ?? eventData.pointerCurrentRaycast.gameObject;
            if (go == null) return;

            var view = go.GetComponentInParent<InventoryItemView>();
            if (view == null || view.BoundItem == null || view.Grid == null) return;

            // 如果该物品当前处于已装备状态，则禁止拿起
            if (view.IsEquippedForBoundItem())
            {
                Debug.Log("[DragCtrl] 拒绝拿起：该物品已装备并被锁定（需先双击卸下才能拿起）。");
                return;
            }

            BeginHoldInternal(view);
        }

        private void TryEndHoldAtPointer(PointerEventData eventData)
        {
            if (!isHoldingItem || sourceGrid == null || heldItem == null) return;

            sourceGrid.ClearPlacementPreview();
            Vector2Int dropPosition = GetGridPosition(eventData.position);

            if (sourceGrid.CanPlaceItemAt(heldItem, dropPosition.x, dropPosition.y))
            {
                sourceGrid.PlaceItemAt(heldItem, dropPosition.x, dropPosition.y);
            }
            else
            {
                sourceGrid.PlaceItemAt(heldItem, originalPosition.x, originalPosition.y);
            }

            DestroyDraggingIcon();
            sourceGrid.RefreshGrid();

            isHoldingItem = false;
            heldView = null;
            heldItem = null;
            sourceGrid = null;
        }

        private void BeginHoldInternal(InventoryItemView view)
        {
            TooltipSystem.Hide();

            isHoldingItem = true;
            heldView = view;
            heldItem = view.BoundItem;
            sourceGrid = view.Grid;
            sourceGrid.TryGetGridPosition(heldItem, out originalPosition);

            sourceGrid.RemoveItem(heldItem, false);
            CreateDraggingIcon();
        }

        public void RotateHeld()
        {
            if (!isHoldingItem || heldItem == null) return;

            heldItem.Rotate();
            RefreshHoldingItemIcon();

            // 局部刷新：只重建当前物品的视图，而不是整个 Grid
            if (heldItem.view != null)
            {
                heldItem.view.RefreshVisuals();
            }
        }

        private void CreateDraggingIcon()
        {
            if (heldView == null) return;

            draggingIcon = new GameObject("DraggingIcon", typeof(RectTransform)).GetComponent<RectTransform>();
            draggingIcon.SetParent(canvas.transform, false);
            draggingIcon.SetAsLastSibling();

            var sourceImage = heldView.iconImage;
            var iconImage = draggingIcon.gameObject.AddComponent<Image>();
            iconImage.sprite = sourceImage != null ? sourceImage.sprite : null;
            iconImage.raycastTarget = false; // 不拦截点击

            draggingIcon.localScale = Vector3.one * 1.1f;
            if (sourceImage != null)
            {
                var sourceRect = sourceImage.rectTransform;
                iconImage.rectTransform.sizeDelta = sourceRect.sizeDelta;
            }

            UpdateHeldItemPosition();
        }

        private void DestroyDraggingIcon()
        {
            if (draggingIcon != null)
            {
                Destroy(draggingIcon.gameObject);
                draggingIcon = null;
            }
        }

        private void UpdateHeldItemPosition()
        {
            if (draggingIcon == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out Vector2 localPoint);

            draggingIcon.localPosition = localPoint + new Vector2(2, 0);
        }

        public void RefreshHoldingItemIcon()
        {
            if (draggingIcon == null || heldItem == null || heldView == null) return;

            var iconImage = draggingIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = heldView.iconImage.sprite;
            }
        }

        private Vector2Int GetGridPosition(Vector2 screenPosition)
        {
            if (sourceGrid == null) return Vector2Int.zero;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sourceGrid.container,
                screenPosition,
                canvas.worldCamera,
                out Vector2 localPoint);

            int x = Mathf.FloorToInt(localPoint.x / sourceGrid.cellSize.x);
            int y = Mathf.FloorToInt(-localPoint.y / sourceGrid.cellSize.y);

            return new Vector2Int(x, y);
        }
    }
}
