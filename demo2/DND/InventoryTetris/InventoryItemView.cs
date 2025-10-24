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
            group.blocksRaycasts = false; // 避免阻挡事件
            BringToFront();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || BoundItem == null || Grid == null) return;

            // 目标格坐标（以物品左上角对齐）
            if (Grid.PointerToGrid(eventData, out int gx, out int gy))
            {
                bool can = Grid.Model.CanPlaceIgnoring(BoundItem, gx, gy);
                // 预览位置
                rect.anchoredPosition = Grid.GridToLocalTopLeft(gx, gy);
                rect.sizeDelta = new Vector2(Grid.ItemPixelWidth(BoundItem), Grid.ItemPixelHeight(BoundItem));

                // 颜色提示
                if (bgImage != null)
                {
                    bgImage.color = can ? new Color(0.75f, 1f, 0.75f, bgOriginalColor.a) : new Color(1f, 0.6f, 0.6f, bgOriginalColor.a);
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
                // 最终尝试移动（占用表更新）
                if (Grid.TryMove(BoundItem, gx, gy))
                {
                    // 成功则位置已由 Grid 更新
                    return;
                }
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
