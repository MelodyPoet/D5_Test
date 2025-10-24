using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace demo2.DND.InventoryTetris
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("绑定组件")]
        public Image bgImage;           // 背景（可为九宫格）
        public Image iconImage;         // 图标
        public Button button;           // 可选

        // 运行时绑定（通过 Bind 方法设置）
        public ItemInstance BoundItem { get; private set; }
        public InventoryGridView Grid { get; private set; }

        private RectTransform rect;
        private CanvasGroup group;

        private Vector2 startAnchoredPos;
        private Vector2Int startGridPos;
        private bool hasStartGridPos;

        // 拖拽中的可放置提示
        private bool dragging;
        private Color bgOriginalColor;

        public RectTransform Rect => rect;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            group = GetComponent<CanvasGroup>();
            if (bgImage != null) bgOriginalColor = bgImage.color;
        }

        public void Bind(ItemInstance item, InventoryGridView grid)
        {
            BoundItem = item;
            Grid = grid;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (BoundItem == null || Grid == null) return;
            dragging = true;
            startAnchoredPos = rect.anchoredPosition;
            hasStartGridPos = Grid.TryGetGridPosition(BoundItem, out startGridPos);
            group.blocksRaycasts = false; // 避免阻拦事件
            BringToFront();
            if (Grid.debugLogs)
            {
                Debug.Log($"[ItemView] BeginDrag '{BoundItem?.data?.displayName ?? BoundItem?.instanceId}' hasStartGridPos={hasStartGridPos} startPos={startGridPos}");
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || BoundItem == null || Grid == null) return;

            if (Grid.PointerToGrid(eventData, out int gx, out int gy))
            {
                bool can = Grid.Model.CanPlaceIgnoring(BoundItem, gx, gy);
                rect.anchoredPosition = Grid.GridToLocalTopLeft(gx, gy);
                rect.sizeDelta = new Vector2(Grid.ItemPixelWidth(BoundItem), Grid.ItemPixelHeight(BoundItem));

                if (bgImage != null)
                {
                    bgImage.color = can ? new Color(0.75f, 1f, 0.75f, bgOriginalColor.a) : new Color(1f, 0.6f, 0.6f, bgOriginalColor.a);
                }
                if (Grid.debugLogs)
                {
                    Debug.Log($"[ItemView] Drag preview -> ({gx},{gy}) can={can}");
                }
            }
            else
            {
                if (Grid.debugLogs)
                {
                    Debug.Log("[ItemView] Drag preview -> out of container");
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging || BoundItem == null || Grid == null) return;
            dragging = false;
            group.blocksRaycasts = true;
            if (bgImage != null) bgImage.color = bgOriginalColor;

            if (Grid.PointerToGrid(eventData, out int gx, out int gy))
            {
                bool ok = Grid.TryMove(BoundItem, gx, gy);
                if (Grid.debugLogs)
                {
                    Debug.Log($"[ItemView] EndDrag drop -> ({gx},{gy}) move={(ok ? "OK" : "FAIL")}");
                }
                if (ok) return;
            }
            else if (Grid.debugLogs)
            {
                Debug.Log("[ItemView] EndDrag drop -> out of container, rollback");
            }

            // 回滚
            rect.anchoredPosition = startAnchoredPos;
            if (hasStartGridPos)
            {
                Grid.PositionViewAtGrid(this, startGridPos.x, startGridPos.y);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (BoundItem == null || Grid == null) return;
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                TryRotateInPlace();
            }
        }

        private void TryRotateInPlace()
        {
            if (BoundItem.data == null || !BoundItem.data.canRotate) return;

            // 记下当前状态
            Vector2Int curPos;
            bool hasPos = Grid.TryGetGridPosition(BoundItem, out curPos);
            if (!hasPos) return;

            // 先请求模型在原位旋转（正确处理占用的清与标记），成功后再修改实例旋转标志
            bool rotatedOk = Grid.Model.TryRotateInPlace(BoundItem);
            if (!rotatedOk) return;

            // 更新实例状态与视图尺寸/位置
            BoundItem.ToggleRotate();
            rect.sizeDelta = new Vector2(Grid.ItemPixelWidth(BoundItem), Grid.ItemPixelHeight(BoundItem));
            Grid.PositionViewAtGrid(this, curPos.x, curPos.y);
        }

        private void BringToFront()
        {
            transform.SetAsLastSibling();
        }
    }
}
