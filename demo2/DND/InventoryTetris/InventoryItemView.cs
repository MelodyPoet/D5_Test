using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace demo2.DND.InventoryTetris
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("视觉表现模式")]
        [Tooltip("True: 使用 `shapeCoords` 动态生成单元格来精确显示形状。\nFalse: 使用传统的 `bgImage` 和 `iconImage` 拉伸填充。")]
        public bool useCellSystem = true;

        [Header("传统视觉组件 (useCellSystem = false)")]
        public Image bgImage;           // 背景（可为九宫格）
        public Image iconImage;         // 图标

        [Header("单元格视觉系统 (useCellSystem = true)")]
        [Tooltip("用于容纳动态生成单元格的容器")]
        public RectTransform cellContainer;
        [Tooltip("代表单个格子的预制体 (UI Image)")]
        public GameObject cellPrefab;

        [Header("其他组件")]
        public Button button;           // 可选
        [Tooltip("可选：在物品图标上方显示装备槽状态。")]
        public Text stateText;

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

        private CharacterEquipment eqSubscribed;

        public RectTransform Rect => rect;

        // 新增：记录鼠标抓取时相对于物品左上角的“格子偏移”（整数格)
        private int grabOffsetCellX;
        private int grabOffsetCellY;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.interactable = true;
                group.blocksRaycasts = true; // 初始允许接收射线；拖拽时会临时关闭
            }
            if (bgImage != null) bgOriginalColor = bgImage.color;

            // 确保至少有一个可射线的图层来接收鼠标事件
            if (iconImage != null)
            {
                iconImage.raycastTarget = true;
            }
            else if (bgImage != null)
            {
                bgImage.raycastTarget = true;
            }

            // 新增：自动绑定子物体中的状态 Text（避免在 Inspector 手动挂多个 Text）
            AutoBindStateTextIfNeeded();
            // 初始先隐藏；后续在 OnEnable/Bind/事件中按需显示
            if (stateText != null) stateText.gameObject.SetActive(false);

            // Disable the prefab template itself to prevent it from being rendered
            if (useCellSystem && cellPrefab != null)
            {
                cellPrefab.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // 运行时可能在 Bind 之前先启用，尝试解析并订阅一次装备组件
            TrySubscribeEquipment();
            RefreshEquipLabel();
        }

        private void OnDisable()
        {
            if (eqSubscribed != null)
            {
                eqSubscribed.OnEquipmentChanged -= HandleEquipmentChanged;
                eqSubscribed = null;
            }
        }

        private void OnDestroy()
        {
            if (eqSubscribed != null)
            {
                eqSubscribed.OnEquipmentChanged -= HandleEquipmentChanged;
                eqSubscribed = null;
            }
        }

        public void Bind(ItemInstance item, InventoryGridView grid)
        {
            BoundItem = item;
            Grid = grid;

            Debug.Log($"[Bind] BoundItem 设置为: {BoundItem?.data?.displayName ?? "null"}, Grid: {Grid?.name ?? "null"}");

            // 取消旧订阅
            if (eqSubscribed != null)
            {
                eqSubscribed.OnEquipmentChanged -= HandleEquipmentChanged;
                eqSubscribed = null;
            }
            // 优先使用 Grid.SourceEquipment，否则向上/下解析
            TrySubscribeEquipment();

            // 确保绑定时也完成一次自动绑定（预制体可能在运行时才生成子节点）
            AutoBindStateTextIfNeeded();

            RefreshEquipLabel();

            // Rebuild visuals based on the new item data
            RebuildVisuals();
        }

        private void HandleEquipmentChanged()
        {
            RefreshEquipLabel();
        }

        private CharacterEquipment ResolveEquipment()
        {
            // 优先 Grid.SourceEquipment
            if (Grid != null && Grid.SourceEquipment != null) return Grid.SourceEquipment;

            CharacterEquipment eq;
            // 从本节点上下查找
            eq = GetComponent<CharacterEquipment>()
                 ?? GetComponentInParent<CharacterEquipment>()
                 ?? GetComponentInChildren<CharacterEquipment>(true);
            if (eq != null) return eq;

            // 从 Grid 节点上下查找
            if (Grid != null)
            {
                eq = Grid.GetComponent<CharacterEquipment>()
                     ?? Grid.GetComponentInParent<CharacterEquipment>()
                     ?? Grid.GetComponentInChildren<CharacterEquipment>(true);
                if (eq != null) return eq;
            }

            // 兜底：在整个场景中查找第一个可用的 CharacterEquipment（含未激活），以防组件挂载在意外位置
            var all = FindObjectsByType<CharacterEquipment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all != null && all.Length > 0)
            {
                // 优先选择在场景中有效的实例
                foreach (var candidate in all)
                {
                    if (candidate == null) continue;
                    if (!candidate.gameObject.scene.IsValid()) continue;
                    Debug.Log($"[ResolveEquipment] 兜底匹配到 CharacterEquipment: {candidate.gameObject.name} for ItemView on {gameObject.name}");
                    return candidate;
                }
            }
            Debug.LogWarning($"[ResolveEquipment] 未能解析到 CharacterEquipment (Grid={Grid?.gameObject.name ?? "null"}, ItemView={gameObject.name})");
            return null;
        }

        private void TrySubscribeEquipment()
        {
            var eq = ResolveEquipment();
            if (eq != null && eq != eqSubscribed)
            {
                if (eqSubscribed != null)
                {
                    eqSubscribed.OnEquipmentChanged -= HandleEquipmentChanged;
                }
                eqSubscribed = eq;
                eqSubscribed.OnEquipmentChanged += HandleEquipmentChanged;
            }
        }

        public void RefreshEquipLabel()
        {
            if (stateText == null)
            {
                Debug.LogWarning("[RefreshEquipLabel] StateText 未绑定，无法更新状态。");
                return;
            }

            if (BoundItem == null)
            {
                stateText.text = string.Empty;
                stateText.gameObject.SetActive(false);
                Debug.Log("[RefreshEquipLabel] BoundItem 为空，隐藏 StateText。");
                return;
            }

            var eq = (Grid != null ? Grid.SourceEquipment : null) ?? ResolveEquipment();
            if (eq == null || BoundItem.data == null)
            {
                stateText.text = string.Empty;
                stateText.gameObject.SetActive(false);
                Debug.LogWarning("[RefreshEquipLabel] 无法解析装备组件或 BoundItem.data 为空，隐藏 StateText。");
                return;
            }

            bool equipped = eq.IsEquipped(BoundItem);
            if (equipped)
            {
                if (BoundItem.data.isWeapon)
                {
                    stateText.text = "主手武器：已装备";
                }
                else if (BoundItem.data.isArmor)
                {
                    stateText.text = "护甲：已装备";
                }
                else if (BoundItem.data.isShield)
                {
                    stateText.text = "盾牌：已装备";
                }
                else
                {
                    stateText.text = "已装备";
                }
                stateText.gameObject.SetActive(true);
                Debug.Log($"[RefreshEquipLabel] 更新 StateText 内容为: {stateText.text}");
            }
            else
            {
                stateText.text = string.Empty;
                stateText.gameObject.SetActive(false);
                Debug.Log("[RefreshEquipLabel] 物品未装备，隐藏 StateText。");
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (BoundItem == null || Grid == null) return;
            dragging = true;
            startAnchoredPos = rect.anchoredPosition;
            hasStartGridPos = Grid.TryGetGridPosition(BoundItem, out startGridPos);
            group.blocksRaycasts = false; // 避免阻拦事件
            BringToFront();

            // 计算抓取偏移（以格为单位），保证拖动时鼠标下的格子保持为物品内部的同一格
            grabOffsetCellX = 0;
            grabOffsetCellY = 0;
            if (hasStartGridPos && Grid.TryGetPointerGridLocal(eventData, out float lx, out float ly))
            {
                float pitchX = Grid.cellSize.x + Grid.spacing.x;
                float pitchY = Grid.cellSize.y + Grid.spacing.y;
                // 物品左上角（去除 padding 后的局部偏移）
                float itemLx = startGridPos.x * pitchX;
                float itemLy = startGridPos.y * pitchY;
                float dx = Mathf.Max(0f, lx - itemLx);
                float dy = Mathf.Max(0f, ly - itemLy);
                int offX = Mathf.FloorToInt(dx / pitchX);
                int offY = Mathf.FloorToInt(dy / pitchY);
                // clamp 到物品内部格子范围
                grabOffsetCellX = Mathf.Clamp(offX, 0, Mathf.Max(0, BoundItem.Width - 1));
                grabOffsetCellY = Mathf.Clamp(offY, 0, Mathf.Max(0, BoundItem.Height - 1));
            }

            if (Grid.debugLogs)
            {
                Debug.Log($"[ItemView] BeginDrag '{BoundItem?.data?.displayName ?? BoundItem?.instanceId}' hasStartGridPos={hasStartGridPos} startPos={startGridPos} grabOff=({grabOffsetCellX},{grabOffsetCellY})");
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || BoundItem == null || Grid == null) return;

            if (Grid.PointerToGrid(eventData, out int gx, out int gy))
            {
                // 将鼠标下的格子转为物品应当放置的左上角格子
                int tx = gx - grabOffsetCellX;
                int ty = gy - grabOffsetCellY;
                // 限制在边界内，保证物品整体在网格中
                tx = Mathf.Clamp(tx, 0, Mathf.Max(0, Grid.Model.cols - BoundItem.Width));
                ty = Mathf.Clamp(ty, 0, Mathf.Max(0, Grid.Model.rows - BoundItem.Height));

                bool can = Grid.Model.CanPlace(BoundItem, tx, ty);
                rect.anchoredPosition = Grid.GridToLocalTopLeft(tx, ty);

                // Update cell colors for drag feedback
                if (useCellSystem && cellContainer != null)
                {
                    Color feedbackColor = can ? new Color(0.75f, 1f, 0.75f, 0.8f) : new Color(1f, 0.6f, 0.6f, 0.8f);
                    foreach (Transform child in cellContainer)
                    {
                        var img = child.GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = feedbackColor;
                        }
                    }
                }
                else if (bgImage != null) // Fallback for old system
                {
                    bgImage.color = can ? new Color(0.75f, 1f, 0.75f, bgOriginalColor.a) : new Color(1f, 0.6f, 0.6f, bgOriginalColor.a);
                }

                if (Grid.debugLogs)
                {
                    Debug.Log($"[ItemView] Drag preview -> hover=({gx},{gy}) place=({tx},{ty}) can={can}");
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

            // Restore original cell colors
            if (useCellSystem)
            {
                RebuildVisuals(); // Just rebuild to restore colors and icon position
            }
            else if (bgImage != null)
            {
                bgImage.color = bgOriginalColor;
            }

            if (Grid.PointerToGrid(eventData, out int gx, out int gy))
            {
                int tx = gx - grabOffsetCellX;
                int ty = gy - grabOffsetCellY;
                tx = Mathf.Clamp(tx, 0, Mathf.Max(0, Grid.Model.cols - BoundItem.Width));
                ty = Mathf.Clamp(ty, 0, Mathf.Max(0, Grid.Model.rows - BoundItem.Height));

                bool ok = Grid.TryMove(BoundItem, tx, ty);
                if (Grid.debugLogs)
                {
                    Debug.Log($"[ItemView] EndDrag drop -> hover=({gx},{gy}) place=({tx},{ty}) move={(ok ? "OK" : "FAIL")}");
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
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (shift)
                {
                    RotateInPlace();
                    return;
                }
                // 打开右键菜单
                ItemContextMenu.ShowForItem(this, eventData.position);
            }
        }

        // 提供给菜单调用的公开旋转方法
        public void RotateInPlace()
        {
            TryRotateInPlace();
        }

        private void TryRotateInPlace()
        {
            if (BoundItem.data == null || !BoundItem.data.canRotate) return;

            Vector2Int curPos;
            bool hasPos = Grid.TryGetGridPosition(BoundItem, out curPos);
            if (!hasPos) {
                if (Grid != null && Grid.debugLogs) Debug.Log("[ItemView] Rotate failed: item has no grid position mapping");
                return;
            }

            // 预旋转，获取新形状
            BoundItem.ToggleRotate();

            // 尝试在原位置“移动”到新形状
            bool rotatedOk = Grid.TryMove(BoundItem, curPos.x, curPos.y);

            if (!rotatedOk) {
                // 旋转失败，回滚旋转状态
                BoundItem.ToggleRotate();
                BoundItem.ToggleRotate();
                BoundItem.ToggleRotate();
                if (Grid != null && Grid.debugLogs) Debug.Log($"[ItemView] Rotate failed: model refused rotation at {curPos} (collision/bounds) ");
                return;
            }

            // 旋转成功，更新视图
            rect.sizeDelta = new Vector2(Grid.ItemPixelWidth(BoundItem), Grid.ItemPixelHeight(BoundItem));
            Grid.PositionViewAtGrid(this, curPos.x, curPos.y);

            // Rebuild visuals to reflect the new rotation
            RebuildVisuals();
        }

        private void RebuildVisuals()
        {
            if (useCellSystem)
            {
                if (cellContainer == null || cellPrefab == null)
                {
                    Debug.LogError($"[ItemView] Cell system is enabled but `cellContainer` or `cellPrefab` is not assigned on {gameObject.name}.");
                    return;
                }

                // Disable old system and hide original icon
                if (bgImage != null) bgImage.enabled = false;
                if (iconImage != null) iconImage.enabled = false;

                // Clear old cells
                foreach (Transform child in cellContainer)
                {
                    Destroy(child.gameObject);
                }

                var shape = BoundItem.GetCurrentShape();
                if (shape == null || shape.Count == 0) return;

                bool isFirstCell = true;

                // Create new cells
                foreach (var coord in shape)
                {
                    var cellGO = Instantiate(cellPrefab, cellContainer);
                    cellGO.SetActive(true); // Ensure the instantiated cell is active
                    var cellRT = cellGO.GetComponent<RectTransform>();
                    if (cellRT != null)
                    {
                        cellRT.anchorMin = new Vector2(0, 1);
                        cellRT.anchorMax = new Vector2(0, 1);
                        cellRT.pivot = new Vector2(0, 1);
                        cellRT.sizeDelta = Grid.cellSize;
                        cellRT.anchoredPosition = new Vector2(
                            coord.x * (Grid.cellSize.x + Grid.spacing.x),
                           -coord.y * (Grid.cellSize.y + Grid.spacing.y)
                        );

                        if (isFirstCell)
                        {
                            if (iconImage != null)
                            {
                                iconImage.transform.SetParent(cellRT, false);
                                iconImage.rectTransform.anchorMin = Vector2.zero;
                                iconImage.rectTransform.anchorMax = Vector2.one;
                                iconImage.rectTransform.sizeDelta = Vector2.zero;
                                iconImage.rectTransform.anchoredPosition = Vector2.zero;
                                iconImage.enabled = true;
                            }
                            isFirstCell = false;
                        }
                    }
                }
            }
            else
            {
                // Enable old system
                if (bgImage != null) bgImage.enabled = true;
                if (iconImage != null)
                {
                    iconImage.enabled = true;
                    iconImage.rectTransform.localRotation = Quaternion.Euler(0, 0, -90f * BoundItem.rotation);
                }
            }
        }

        private void BringToFront()
        {
            transform.SetAsLastSibling();
        }

        // 新增：自动找到当前物品预制体下用于显示“已装备”状态的 Text
        private void AutoBindStateTextIfNeeded()
        {
            if (stateText != null)
            {
                Debug.Log("[AutoBindStateTextIfNeeded] StateText 已绑定，无需重复绑定。");
                return;
            }

            // 1) 优先按常见命名查找（包含常见空格写法）
            var candidatesByName = new[] { "StateText", "State Text", "EquipState", "EquippedText", "Stats Text" };
            foreach (var n in candidatesByName)
            {
                var t = transform.Find(n);
                if (t != null)
                {
                    var txt = t.GetComponent<Text>();
                    if (txt != null)
                    {
                        stateText = txt;
                        Debug.Log($"[AutoBindStateTextIfNeeded] 成功绑定 StateText: {n}");
                        return;
                    }
                }
            }

            // 2) 兜底：查找第一个子级 Text（包含隐藏对象）
            var any = GetComponentInChildren<Text>(true);
            if (any != null)
            {
                stateText = any;
                Debug.Log("[AutoBindStateTextIfNeeded] 兜底绑定到第一个 Text 组件。");
            }
            else
            {
                Debug.LogWarning("[AutoBindStateTextIfNeeded] 未找到任何 Text 组件，绑定失败。");
            }
        }
    }
}
