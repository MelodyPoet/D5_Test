using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 物品右键菜单控制器：基于“UI菜单预制体”（ItemContextMenuView）实例化并绑定逻辑，
    /// 不再硬编码生成UI。请在场景放置 ItemContextMenuProvider，并在其 Inspector 指定菜单预制体。
    /// </summary>
    public class ItemContextMenu : MonoBehaviour
    {
        // 保证同一时间只有一个菜单
        private static ItemContextMenu _current;

        public static void SetCurrent(ItemContextMenu instance)
        {
            if (_current != null && _current != instance)
            {
                Destroy(_current.gameObject);
            }
            _current = instance;
        }

        private ItemContextMenuView _view;   // 从预制体上获取的视图引用
        private Canvas _canvas;

        private InventoryItemView _itemView;
        private InventoryGridView _grid;
        private ItemInstance _item;

        // Track overlay/button created at runtime so we can clean it up without modifying external scene objects.
        private Button overlayCloseBtnInstance;
        private bool createdOverlayInstance = false;

        // 增加一个 isHolding 参数，用于区分是“在背包里右键”还是“拿在手上右键”
        private void Awake()
        {
            _view = GetComponent<ItemContextMenuView>();
            _canvas = GetComponentInParent<Canvas>();

            if (_view != null)
            {
                if (_view.equipButton != null)
                    _view.equipButton.onClick.AddListener(OnClickEquip);
                if (_view.unequipButton != null)
                    _view.unequipButton.onClick.AddListener(OnClickUnequip);
                if (_view.rotateButton != null)
                    _view.rotateButton.onClick.AddListener(OnClickRotate);
            }
        }

        public static void ShowForItem(InventoryItemView itemView, Vector2 screenPos, bool isHolding = false)
        {
            // 再次右键同一个物品：视为关闭菜单
            if (_current != null && _current._itemView == itemView && _current.gameObject.activeSelf)
            {
                _current.gameObject.SetActive(false);
                return;
            }

            if (_current == null)
            {
                var provider = UnityEngine.Object.FindObjectOfType<ItemContextMenuProvider>();
                if (provider != null)
                {
                    _current = provider.CreateInstance();
                }
            }

            if (_current == null)
            {
                Debug.LogError("[ItemContextMenu] Instance not found. Ensure ItemContextMenuProvider is in the scene.");
                return;
            }

            _current.Show(itemView, screenPos, isHolding);
        }

        public static void CloseCurrent()
        {
            if (_current != null)
            {
                _current.gameObject.SetActive(false);
            }
        }

        private void Show(InventoryItemView itemView, Vector2 screenPos, bool isHolding)
        {
            if (_view == null)
            {
                Debug.LogError("[ItemContextMenu] ItemContextMenuView not assigned on prefab.", this);
                return;
            }
            if (itemView == null)
            {
                Debug.LogError("[ItemContextMenu] Show called with null itemView.");
                return;
            }

            _itemView = itemView;
            _item = itemView.BoundItem;
            _grid = itemView.Grid;

            if (_item == null || _item.data == null)
            {
                Debug.LogWarning("[ItemContextMenu] BoundItem or its data is null. Menu will not be shown.");
                gameObject.SetActive(false);
                return;
            }

            bool isHoldingItem = isHolding || (ItemDragController.Current != null && ItemDragController.Current.IsHoldingItem);

            // 规则：
            // - 在背包格子里（未拿起，isHoldingItem=false）：只允许 装备/卸下，禁用旋转
            // - 被拿起（isHoldingItem=true）：只允许 旋转，禁用装备/卸下

            if (_view.equipButton != null)
            {
                bool canBeEquipped = _item.data.isWeapon || _item.data.isArmor || _item.data.isShield;
                _view.equipButton.gameObject.SetActive(!isHoldingItem && canBeEquipped);
            }

            if (_view.unequipButton != null)
            {
                var eq = ResolveEquipment();
                bool isEquipped = eq != null && eq.IsEquipped(_item);
                _view.unequipButton.gameObject.SetActive(!isHoldingItem && isEquipped);
            }

            if (_view.rotateButton != null)
            {
                // 只有拿在手上时才允许旋转
                _view.rotateButton.gameObject.SetActive(isHoldingItem && _item.data.canRotate);
            }

            // 激活菜单并设置位置
            gameObject.SetActive(true);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenPos,
                _canvas.worldCamera,
                out Vector2 localPoint);

            (transform as RectTransform).localPosition = localPoint;
        }

        // ...

        private void OnClickRotate()
        {
            if (_itemView == null || _item == null) return;

            _item.Rotate();

            // 如果是通过 ItemDragController 持有状态下旋转，需要通知它刷新图标
            if (ItemDragController.Current != null && ItemDragController.Current.IsHoldingItem)
            {
                ItemDragController.Current.RefreshHoldingItemIcon();
            }
            else
            {
                // 否则，执行原地旋转的刷新
                _itemView.RefreshVisuals();
            }

            gameObject.SetActive(false);
        }

        private void OnClickEquip()
        {
            // 持有状态下不允许装备
            if (ItemDragController.Current != null && ItemDragController.Current.IsHoldingItem)
            {
                Debug.LogWarning("[ItemContextMenu] Cannot equip while holding item. Place it first.");
                return;
            }

            var eq = ResolveEquipment();
            if (eq == null || _item == null || _item.data == null) return;

            if (!eq.CanEquip(_item)) return;

            if (_item.data.isWeapon) eq.EquipToSlot(EquipmentSlot.MainHand, _item);
            else if (_item.data.isArmor) eq.EquipToSlot(EquipmentSlot.Armor, _item);
            else if (_item.data.isShield) eq.EquipToSlot(EquipmentSlot.OffHand, _item);

            // 刷新UI（GridView会刷新所有物品的标签，更可靠）
            if (_grid != null) _grid.RefreshAllEquipLabels();
            else _itemView?.RefreshEquipLabel();

            gameObject.SetActive(false);
        }

        private void OnClickUnequip()
        {
            // 持有状态下不允许卸下
            if (ItemDragController.Current != null && ItemDragController.Current.IsHoldingItem)
            {
                return;
            }

            var eq = ResolveEquipment();
            if (eq == null || _item == null || _item.data == null) return;

            var mh = eq.GetEquipped(EquipmentSlot.MainHand);
            var ar = eq.GetEquipped(EquipmentSlot.Armor);
            var sh = eq.GetEquipped(EquipmentSlot.OffHand);

            if (_item.data.isWeapon && ReferenceEquals(mh, _item)) eq.UnequipSlot(EquipmentSlot.MainHand);
            else if (_item.data.isArmor && ReferenceEquals(ar, _item)) eq.UnequipSlot(EquipmentSlot.Armor);
            else if (_item.data.isShield && ReferenceEquals(sh, _item)) eq.UnequipSlot(EquipmentSlot.OffHand);

            if (_grid != null) _grid.RefreshAllEquipLabels();
            else _itemView?.RefreshEquipLabel();

            gameObject.SetActive(false);
        }

        private CharacterEquipment ResolveEquipment()
        {
            if (_itemView == null) return null;
            return _itemView.Grid?.SourceEquipment;
        }
    }
}
