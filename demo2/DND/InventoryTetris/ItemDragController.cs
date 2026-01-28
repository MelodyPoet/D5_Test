using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 控制物品的拖拽、旋转和放置（全局捕获 PointerDown）。
    /// </summary>
    public class ItemDragController : MonoBehaviour, IPointerDownHandler
    {
        public static ItemDragController Current { get; private set; }

        [Header("Input")]
        [Tooltip("双击判定时间窗（秒），用于延迟拿起以让双击装备逻辑优先生效。")]
        public float doubleClickThreshold = 0.25f;

        private InventoryItemView heldView;
        private ItemInstance heldItem;
        private InventoryGridView sourceGrid;
        private Vector2Int originalPosition;

        private Canvas canvas;
         private bool isHoldingItem;
        public bool IsHoldingItem => isHoldingItem;

        private float lastClickTime;
        private Coroutine pendingHoldRoutine;
        private InventoryItemView pendingHoldView;

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
                    TryScheduleHoldFromPointer(eventData);
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

        private void TryScheduleHoldFromPointer(PointerEventData eventData)
        {
            var go = eventData.pointerPressRaycast.gameObject ?? eventData.pointerCurrentRaycast.gameObject;
            if (go == null) return;

            var view = go.GetComponentInParent<InventoryItemView>();
            if (view == null || view.BoundItem == null || view.Grid == null) return;

            if (view.IsEquippedForBoundItem())
            {
                Debug.Log("[DragCtrl] 拒绝拿起：该物品已装备并被锁定（需先双击卸下才能拿起）。");
                return;
            }

            float now = Time.unscaledTime;
            if (pendingHoldRoutine != null && (now - lastClickTime) <= doubleClickThreshold)
            {
                StopCoroutine(pendingHoldRoutine);
                pendingHoldRoutine = null;
                pendingHoldView = null;
                return; // 交由双击逻辑处理
            }

            lastClickTime = now;
            pendingHoldView = view;
            pendingHoldRoutine = StartCoroutine(DelayedBeginHold(doubleClickThreshold));
        }

        private IEnumerator DelayedBeginHold(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (pendingHoldView != null && !isHoldingItem)
            {
                BeginHoldInternal(pendingHoldView);
            }
            pendingHoldView = null;
            pendingHoldRoutine = null;
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

            Vector2Int finalPosition;
            if (sourceGrid.CanPlaceItemAt(heldItem, dropPosition.x, dropPosition.y))
            {
                sourceGrid.PlaceItemAt(heldItem, dropPosition.x, dropPosition.y);
                finalPosition = dropPosition;
            }
            else
            {
                sourceGrid.PlaceItemAt(heldItem, originalPosition.x, originalPosition.y);
                finalPosition = originalPosition;
            }

            if (heldView != null)
            {
                heldView.gameObject.SetActive(true);
                sourceGrid.PositionViewAtGrid(heldView, finalPosition.x, finalPosition.y);
                heldItem.view = heldView;
                heldView.RefreshVisuals();
                heldView.SyncEquipVisual();
            }

            sourceGrid.RefreshGrid();
            ForceRefreshEquipmentState();

            isHoldingItem = false;
            heldView = null;
            heldItem = null;
            sourceGrid = null;
        }

        private void ForceRefreshEquipmentState()
        {
            if (sourceGrid == null) return;
            var eq = sourceGrid.SourceEquipment;
            if (eq != null)
            {
                eq.ReapplyEquippedModifiers();
            }
            sourceGrid.RefreshAllEquipLabels();
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
            if (heldView != null)
            {
                heldView.gameObject.SetActive(false);
            }
        }

        public void RotateHeld()
        {
            if (!isHoldingItem || heldItem == null) return;

            heldItem.Rotate();
            if (sourceGrid != null)
            {
                sourceGrid.ShowPlacementPreview(heldItem, GetGridPosition(Input.mousePosition));
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
