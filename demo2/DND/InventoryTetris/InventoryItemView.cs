using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 物品在背包中的视图，负责响应点击、显示状态等。
    /// </summary>
    public class InventoryItemView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI 组件（自动查找或手动绑定）")]
        public Image bgImage;
        public Image iconImage;

        [Header("Cell-based Shape")]
        public bool useCellSystem = true;
        public RectTransform cellContainer;
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

        private Vector2 _cellSize; // 新增：用于存储网格的单元格尺寸

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

            // After visuals rebuilt ensure equip color matches current equipment state
            UpdateEquipColor();
        }

        private void HandleEquipmentChanged()
        {
            RefreshEquipLabel();
            // Ensure equip color/visuals update when equipment on the character changes
            UpdateEquipColor();
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
                // also ensure equip color cleared
                UpdateEquipColor();
                return;
            }

            var eq = (Grid != null ? Grid.SourceEquipment : null) ?? ResolveEquipment();
            if (eq == null || BoundItem.data == null)
            {
                stateText.text = string.Empty;
                stateText.gameObject.SetActive(false);
                Debug.LogWarning("[RefreshEquipLabel] 无法解析装备组件或 BoundItem.data 为空，隐藏 StateText。");
                UpdateEquipColor();
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

            // Ensure equip-state visual (cell/icon/bg) is synchronized with label state
            UpdateEquipColor();
        }

        public void SetCellSize(Vector2 cellSize)
        {
            _cellSize = cellSize;
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

                // Reset cell container position and ensure it's top-left aligned
                cellContainer.anchorMin = new Vector2(0, 1);
                cellContainer.anchorMax = new Vector2(0, 1);
                cellContainer.pivot = new Vector2(0, 1);
                cellContainer.anchoredPosition = Vector2.zero;

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
                        // Position cells relative to the container's top-left, including spacing
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

        private void GenerateShape()
        {
            if (cellContainer == null || cellPrefab == null) return;

            // Clear previous cells
            foreach (Transform child in cellContainer)
            {
                Destroy(child.gameObject);
            }

            var shape = BoundItem.GetCurrentShape();
            if (shape == null) return;

            foreach (var coord in shape)
            {
                var cell = Instantiate(cellPrefab, cellContainer);
                var rt = cell.transform as RectTransform;
                rt.anchoredPosition = new Vector2(
                    coord.x * (_cellSize.x + Grid.spacing.x),
                   -coord.y * (_cellSize.y + Grid.spacing.y)
                );
                rt.sizeDelta = _cellSize;
                cell.SetActive(true);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (BoundItem == null || Grid == null) return;

            // 仅用于调试双击与装备逻辑
            bool hasDragCtrl = ItemDragController.Current != null;
            bool isHolding = ItemDragController.Current?.IsHoldingItem ?? false;
            Debug.Log($"[ItemView Click] {BoundItem?.data?.displayName} button={eventData.button} clicks={eventData.clickCount}, hasDragCtrl={hasDragCtrl}, isHolding={isHolding}");

            // 左键双击：装备/卸下切换（仅在未拿起时生效）
            if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
            {
                // Only equippable items respond to double-click equip/unequip
                if (!IsEquippable())
                {
                    Debug.Log("[ItemView] 双击被忽略：该物品不可装备。");
                    return;
                }

                // 如果拖拽控制器存在且当前处于拿起状态，则忽略双击
                if (ItemDragController.Current != null && ItemDragController.Current.IsHoldingItem)
                {
                    Debug.Log("[ItemView] 双击忽略：当前处于拿起状态（holding）。");
                    return;
                }

                // 必须确保该物品当前已经落在背包格子上并且位置合法（防止在未放下或非法位置时装备）
                if (Grid != null)
                {
                    if (Grid.TryGetGridPosition(BoundItem, out var pos))
                    {
                        // 进一步确认当前位置对该物品仍然是合法的放置位置
                        if (Grid.CanPlaceItemAt(BoundItem, pos.x, pos.y))
                        {
                            ToggleEquipState();
                        }
                        else
                        {
                            Debug.LogWarning($"[ItemView] 双击忽略：物品在网格位置({pos.x},{pos.y})不再是合法放置位置。");
                        }
                    }
                    else
                    {
                        Debug.Log("[ItemView] 双击忽略：物品未落在任何网格位置（未放下）。");
                    }
                }
                else
                {
                    Debug.LogWarning("[ItemView] 双击操作失败：未找到 Grid 引用。");
                }
            }
        }

        /// <summary>
        /// Returns whether the bound item is equippable (weapon/armor/shield) or explicitly marked as equippable on the SO.
        /// </summary>
        private bool IsEquippable()
        {
            if (BoundItem == null || BoundItem.data == null) return false;
            var d = BoundItem.data;
            // Prefer explicit isEquippable flag if set by designer; fallback to legacy flags for compatibility.
            if (d.isEquippable) return true;
            return d.isWeapon || d.isArmor || d.isShield;
        }

        private void ToggleEquipState()
        {
            // Only equippable items can be toggled
            if (!IsEquippable())
            {
                Debug.LogWarning("[ToggleEquipState] 物品不可装备，忽略切换请求。");
                return;
            }

            // 防御：仅在物品当前有绑定且位于网格上并且位置合法时才尝试装备/卸下
            if (BoundItem == null || BoundItem.data == null)
            {
                Debug.LogWarning("[ToggleEquipState] BoundItem 或 BoundItem.data 为 null，不能切换装备。");
                return;
            }

            if (Grid == null)
            {
                Debug.LogWarning("[ToggleEquipState] 无法切换装备：未绑定 Grid 视图。");
                return;
            }

            if (!Grid.TryGetGridPosition(BoundItem, out var currentPos))
            {
                Debug.LogWarning("[ToggleEquipState] 物品当前未放置在网格上（可能正在被拿起），无法装备/卸下。");
                return;
            }

            if (!Grid.CanPlaceItemAt(BoundItem, currentPos.x, currentPos.y))
            {
                Debug.LogWarning($"[ToggleEquipState] 当前物品在位置({currentPos.x},{currentPos.y})不构成合法放置，禁止装备/卸下。");
                return;
            }

            var eq = ResolveEquipment();
            if (eq == null)
            {
                Debug.LogWarning($"[ToggleEquipState] eq 为 null，无法切换装备状态。Item={BoundItem.data.displayName}");
                return;
            }

            bool isEquipped = eq.IsEquipped(BoundItem);
            Debug.Log($"[ToggleEquipState] {BoundItem.data.displayName} 原状态 isEquipped={isEquipped}");

            if (!isEquipped)
            {
                // 尝试装备
                if (!eq.CanEquip(BoundItem))
                {
                    Debug.LogWarning($"[ToggleEquipState] CanEquip 返回 false，无法装备 {BoundItem.data.displayName}");
                    return;
                }
                if (BoundItem.data.isWeapon) eq.EquipToSlot(EquipmentSlot.MainHand, BoundItem);
                else if (BoundItem.data.isArmor) eq.EquipToSlot(EquipmentSlot.Armor, BoundItem);
                else if (BoundItem.data.isShield) eq.EquipToSlot(EquipmentSlot.OffHand, BoundItem);
            }
            else
            {
                // 尝试卸下
                var mh = eq.GetEquipped(EquipmentSlot.MainHand);
                var ar = eq.GetEquipped(EquipmentSlot.Armor);
                var sh = eq.GetEquipped(EquipmentSlot.OffHand);
                if (BoundItem.data.isWeapon && ReferenceEquals(mh, BoundItem)) eq.UnequipSlot(EquipmentSlot.MainHand);
                else if (BoundItem.data.isArmor && ReferenceEquals(ar, BoundItem)) eq.UnequipSlot(EquipmentSlot.Armor);
                else if (BoundItem.data.isShield && ReferenceEquals(sh, BoundItem)) eq.UnequipSlot(EquipmentSlot.OffHand);
            }

            // 更新视觉：仅对可装备物品改变 cell 色彩
            UpdateEquipColor();
        }

        private void UpdateEquipColor()
        {
            // Only change color for equippable items
            if (!IsEquippable()) return;

            var eq = ResolveEquipment();
            bool equipped = false;
            if (eq != null && BoundItem != null && BoundItem.data != null)
            {
                equipped = eq.IsEquipped(BoundItem);
            }
            else
            {
                // Fallback: if equipment component not resolvable (timing/scene order), try use CharacterInventory saved equipped IDs
                if (Grid != null && Grid.SourceInventory != null && BoundItem != null && BoundItem.data != null)
                {
                    var inv = Grid.SourceInventory;
                    string id = BoundItem.instanceId;
                    if (BoundItem.data.isWeapon && !string.IsNullOrEmpty(inv.equippedMainHandId) && inv.equippedMainHandId == id) equipped = true;
                    else if (BoundItem.data.isArmor && !string.IsNullOrEmpty(inv.equippedArmorId) && inv.equippedArmorId == id) equipped = true;
                    else if (BoundItem.data.isShield && !string.IsNullOrEmpty(inv.equippedShieldId) && inv.equippedShieldId == id) equipped = true;
                }
            }
            Color targetColor = equipped ? Color.gray : Color.white;

            Debug.Log($"[UpdateEquipColor] item={BoundItem.data.displayName} id={BoundItem.instanceId} resolvedEq={(eq!=null?eq.gameObject.name:"null")}, isEquipped={equipped}, targetColor={targetColor}");

            bool appliedAny = false;

            // Update cell images if using cell system
            if (cellContainer != null)
            {
                foreach (Transform child in cellContainer)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = targetColor;
                        appliedAny = true;
                    }
                }
            }

            // Also update the icon and background so the visual clearly shows equipped state
            if (iconImage != null)
            {
                iconImage.color = targetColor;
                appliedAny = true;
            }
            if (bgImage != null)
            {
                bgImage.color = bgOriginalColor * (equipped ? 0.8f : 1f);
                appliedAny = true;
            }

            // Defensive: if nothing was applied (no cell container and no images), try to color any Image on this view
            if (!appliedAny)
            {
                var imgs = GetComponentsInChildren<Image>(true);
                foreach (var i in imgs)
                {
                    i.color = targetColor;
                }
                Debug.LogWarning($"[UpdateEquipColor] No primary images found; applied color to {imgs.Length} child Image(s).");
            }
        }

        /// <summary>
        /// Returns true if the currently bound item is equippable and currently equipped on the resolved CharacterEquipment.
        /// Used by drag controller to lock picking up equipped items.
        /// </summary>
        public bool IsEquippedForBoundItem()
        {
            if (BoundItem == null || BoundItem.data == null) return false;
            if (!IsEquippable()) return false;
            var eq = ResolveEquipment();
            if (eq == null) return false;
            return eq.IsEquipped(BoundItem);
        }

        private void AutoBindStateTextIfNeeded()
        {
            if (stateText != null) return;
            // Try find a child Text named 'StateText' first
            var candidates = GetComponentsInChildren<Text>(true);
            foreach (var t in candidates)
            {
                if (t == null) continue;
                if (t.name.Equals("StateText", System.StringComparison.OrdinalIgnoreCase))
                {
                    stateText = t;
                    return;
                }
            }
            // Fallback to first found Text
            if (candidates.Length > 0) stateText = candidates[0];
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Show tooltip if available and item bound
            if (BoundItem != null)
            {
                // Anchor tooltip to this item's root rect (so it stays fixed at item front)
                TooltipSystem.Show(BoundItem, this.GetComponent<RectTransform>(), false);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipSystem.Hide();
        }

        /// <summary>
        /// Public wrapper to allow external callers to refresh visuals after runtime changes (rotate/etc.).
        /// </summary>
        public void RefreshVisuals()
        {
            RebuildVisuals();
        }

        /// <summary>
        /// Public helper to force sync equip visuals (safe to call from external code).
        /// </summary>
        public void SyncEquipVisual()
        {
            UpdateEquipColor();
        }
    }
}
