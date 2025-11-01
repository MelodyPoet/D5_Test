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

        private ItemContextMenuView _view;   // 从预制体上获取的视图引用
        private Canvas _canvas;
        private GameObject _rootInstance;    // 实例化出来的预制体根，用于关闭时整体销毁

        private InventoryItemView _itemView;
        private InventoryGridView _grid;
        private ItemInstance _item;

        // Track overlay/button created at runtime so we can clean it up without modifying external scene objects.
        private Button overlayCloseBtnInstance;
        private bool createdOverlayInstance = false;

        public static void ShowForItem(InventoryItemView itemView, Vector2 screenPosition)
        {
            if (itemView == null || itemView.Grid == null) return;
            var canvas = itemView.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[ItemContextMenu] 未找到父 Canvas，已取消打开菜单。");
                return;
            }

            var provider = ItemContextMenuProvider.GetOrFind();
            if (provider == null || provider.menuPrefab == null)
            {
                Debug.LogError("[ItemContextMenu] 未配置菜单预制体：请在场景中放置 ItemContextMenuProvider 并指定 menuPrefab（GameObject）。");
                return;
            }

            // 关闭旧菜单
            if (_current != null)
            {
                _current.CloseInternal();
                _current = null;
            }

            // 实例化 GameObject 预制体
            var rootGo = GameObject.Instantiate(provider.menuPrefab, canvas.transform, false);
            if (rootGo == null)
            {
                Debug.LogError("[ItemContextMenu] 菜单预制体实例化失败。");
                return;
            }

            // 在根或子节点查找视图组件
            var view = rootGo.GetComponent<ItemContextMenuView>() ?? rootGo.GetComponentInChildren<ItemContextMenuView>(true);
            if (view == null)
            {
                Debug.LogError("[ItemContextMenu] 预制体中未找到 ItemContextMenuView，请在菜单预制体上挂载该脚本并指向按钮与面板。");
                GameObject.Destroy(rootGo);
                return;
            }

            // 将控制器挂到根对象上
            var ctrl = rootGo.AddComponent<ItemContextMenu>();
            _current = ctrl;
            ctrl.Init(view, canvas, itemView, screenPosition, rootGo);
        }

        private void Init(ItemContextMenuView view, Canvas canvas, InventoryItemView itemView, Vector2 screenPosition, GameObject rootInstance)
        {
            _view = view;
            _canvas = canvas;
            _rootInstance = rootInstance;
            _itemView = itemView;
            _grid = itemView.Grid;
            _item = itemView.BoundItem;

            // 确保在未手动拖引用时也能自动匹配到按钮/面板
            _view.EnsureRuntimeBindings();

            // 绑定按钮事件
            if (_view.btnEquip != null) _view.btnEquip.onClick.AddListener(OnClickEquip);
            if (_view.btnUnequip != null) _view.btnUnequip.onClick.AddListener(OnClickUnequip);
            if (_view.btnRotate != null) _view.btnRotate.onClick.AddListener(OnClickRotate);

            // 点击空白关闭
            SetupOverlayClose();

            // 根据状态设置按钮可用性
            RefreshButtonsInteractable();

            // 定位到鼠标处并进行边界夹紧
            PositionPanelAt(screenPosition);
        }

        private void SetupOverlayClose()
        {
            // 优先使用显式配置的关闭按钮
            if (_view.overlayCloseButton != null)
            {
                _view.overlayCloseButton.onClick.AddListener(Close);
                overlayCloseBtnInstance = _view.overlayCloseButton;
                createdOverlayInstance = false;
                return;
            }
            // 否则尝试在 overlayRoot 上创建 Button 以接收点击
            if (_view.overlayRoot != null)
            {
                // Avoid mutating external scene objects: if overlayRoot is part of the instantiated menu
                // instance, it's safe to add a Button directly. Otherwise create a sibling overlay under
                // the instantiated root so cleanup is straightforward and we don't leave buttons behind.
                Button btn = _view.overlayRoot.GetComponent<Button>();
                bool overlayRootIsInstanceChild = _rootInstance != null && _view.overlayRoot != null && _view.overlayRoot.IsChildOf(_rootInstance.transform);
                if (btn == null && !overlayRootIsInstanceChild)
                {
                    // Create a transparent overlay under our rootInstance to capture clicks.
                    var newGo = new GameObject("OverlayAutoClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    newGo.transform.SetParent(_rootInstance.transform, false);
                    var rt = newGo.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    var img = newGo.GetComponent<Image>();
                    img.color = new Color(0f, 0f, 0f, 0f);
                    img.raycastTarget = true;
                    btn = newGo.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(Close);
                    overlayCloseBtnInstance = btn;
                    createdOverlayInstance = true;
                }
                else
                {
                    if (btn == null)
                    {
                        // add to overlayRoot which is inside our instantiated menu
                        btn = _view.overlayRoot.gameObject.AddComponent<Button>();
                    }
                    btn.transition = Selectable.Transition.None;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(Close);
                    // Ensure overlayRoot has a Graphic so the Button can receive clicks
                    var graphic = _view.overlayRoot.GetComponent<Graphic>();
                    if (graphic == null)
                    {
                        var img = _view.overlayRoot.gameObject.AddComponent<Image>();
                        img.color = new Color(0, 0, 0, 0);
                        img.raycastTarget = true;
                    }
                    overlayCloseBtnInstance = btn;
                    createdOverlayInstance = overlayRootIsInstanceChild ? false : false; // explicit: not created by us
                }
            }
            else
            {
                // 没有 overlayRoot 则在根节点上添加一个按钮作为兜底
                var rt = _view.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Add the overlay button under our root instance to avoid mutating external objects
                    var btnGo = _view.gameObject.GetComponent<Button>() ? _view.gameObject : null;
                    var btn = _view.GetComponent<Button>() ?? _view.gameObject.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(Close);
                    var graphic = _view.GetComponent<Graphic>();
                    if (graphic == null)
                    {
                        var img = _view.gameObject.AddComponent<Image>();
                        img.color = new Color(0, 0, 0, 0);
                        img.raycastTarget = true;
                    }
                    overlayCloseBtnInstance = btn;
                    createdOverlayInstance = false;
                }
            }
        }

        private CharacterEquipment ResolveEquipment()
        {
            // 优先从 Grid.SourceEquipment 获取
            var eq = _grid != null ? _grid.SourceEquipment : null;
            if (eq != null) return eq;

            // 其次：从物品视图向上/下查找
            if (_itemView != null)
            {
                eq = _itemView.GetComponent<CharacterEquipment>()
                     ?? _itemView.GetComponentInParent<CharacterEquipment>()
                     ?? _itemView.GetComponentInChildren<CharacterEquipment>(true);
                if (eq != null) return eq;
            }

            // 再次：从 Grid 节点上下查找
            if (_grid != null)
            {
                eq = _grid.GetComponent<CharacterEquipment>()
                     ?? _grid.GetComponentInParent<CharacterEquipment>()
                     ?? _grid.GetComponentInChildren<CharacterEquipment>(true);
                if (eq != null) return eq;
            }

            // 兜底：从 Canvas 上下查找
            if (_canvas != null)
            {
                eq = _canvas.GetComponent<CharacterEquipment>()
                     ?? _canvas.GetComponentInParent<CharacterEquipment>()
                     ?? _canvas.GetComponentInChildren<CharacterEquipment>(true);
            }
            return eq;
        }

        private void RefreshButtonsInteractable()
        {
            var eq = ResolveEquipment();
            bool isEquipped = (eq != null && eq.IsEquipped(_item));
            bool canEquip = (eq != null && _item != null && _item.data != null) && eq.CanEquip(_item);
            bool rotatable = _item != null && _item.data != null && _item.data.canRotate;

            if (_view.btnEquip != null) _view.btnEquip.interactable = (eq != null) && canEquip && !isEquipped;
            if (_view.btnUnequip != null) _view.btnUnequip.interactable = (eq != null) && isEquipped;
            if (_view.btnRotate != null) _view.btnRotate.interactable = rotatable;
        }

        private void PositionPanelAt(Vector2 screenPosition)
        {
            if (_view.panelRoot == null)
            {
                Debug.LogWarning("[ItemContextMenu] 预制体未设置 panelRoot，无法定位菜单面板。");
                return;
            }

            // 以 panel 的父节点作为坐标参考，最大限度适配任意锚点/枢轴配置
            var parentRect = _view.panelRoot.parent as RectTransform;
            if (parentRect == null)
            {
                Debug.LogWarning("[ItemContextMenu] panelRoot 没有父 RectTransform，定位可能不正确。");
                parentRect = _view.overlayRoot != null ? _view.overlayRoot : _view.GetComponent<RectTransform>();
                if (parentRect == null) return;
            }

            Camera cam = null;
            if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;
            }

            // 将屏幕坐标转换为父Rect的本地坐标（以父pivot为原点）
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, cam, out var local);
            _view.panelRoot.anchoredPosition = local; // 先把面板pivot放到鼠标点
            Canvas.ForceUpdateCanvases();

            // 依据父Rect与面板Rect的尺寸/枢轴进行边界夹紧
            var pRect = parentRect.rect;
            var panelRect = _view.panelRoot.rect;

            // 父坐标系下的左右上下边界（以父pivot为原点）
            float left = -pRect.width * parentRect.pivot.x;
            float right = pRect.width * (1f - parentRect.pivot.x);
            float bottom = -pRect.height * parentRect.pivot.y;
            float top = pRect.height * (1f - parentRect.pivot.y);

            // 面板pivot到边缘的内/外边距
            float minX = left + panelRect.width * _view.panelRoot.pivot.x;
            float maxX = right - panelRect.width * (1f - _view.panelRoot.pivot.x);
            float minY = bottom + panelRect.height * _view.panelRoot.pivot.y;
            float maxY = top - panelRect.height * (1f - _view.panelRoot.pivot.y);

            var pos = _view.panelRoot.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            _view.panelRoot.anchoredPosition = pos;
        }

        private void OnClickRotate()
        {
            if (_itemView == null) return;
            if (_grid == null || _grid.transform.childCount == 0)
            {
                Debug.LogWarning("[ItemContextMenu] 物品旋转失败：未找到可用的格子位置。");
                return;
            }

            _itemView.RotateInPlace();
            Close();
        }

        private void OnClickEquip()
        {
            var eq = ResolveEquipment();
            if (eq == null || _item == null || _item.data == null) return;
            if (!eq.CanEquip(_item)) return;

            if (_item.data.isWeapon) eq.EquipMainHand(_item);
            else if (_item.data.isArmor) eq.EquipArmor(_item);
            else if (_item.data.isShield) eq.EquipShield(_item);

            if (_grid != null) _grid.RefreshAllEquipLabels();
            else _itemView?.RefreshEquipLabel();
            Close();
        }

        private void OnClickUnequip()
        {
            var eq = ResolveEquipment();
            if (eq == null || _item == null || _item.data == null) return;

            if (_item.data.isWeapon && ReferenceEquals(eq.mainHand, _item)) eq.UnequipMainHand();
            else if (_item.data.isArmor && ReferenceEquals(eq.armor, _item)) eq.UnequipArmor();
            else if (_item.data.isShield && ReferenceEquals(eq.shield, _item)) eq.UnequipShield();

            if (_grid != null) _grid.RefreshAllEquipLabels();
            else _itemView?.RefreshEquipLabel();
            Close();
        }

        private void Close()
        {
            CloseInternal();
        }

        private void CloseInternal()
        {
            // Clean up any overlay/button we created at runtime and remove listeners from others.
            try
            {
                if (overlayCloseBtnInstance != null)
                {
                    try { overlayCloseBtnInstance.onClick.RemoveListener(Close); } catch { }
                    if (createdOverlayInstance && overlayCloseBtnInstance.gameObject != null)
                    {
                        Destroy(overlayCloseBtnInstance.gameObject);
                    }
                    overlayCloseBtnInstance = null;
                    createdOverlayInstance = false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ItemContextMenu] Exception while cleaning up overlay button: {ex}");
            }

            if (_rootInstance != null)
            {
                Destroy(_rootInstance);
                _rootInstance = null;
            }
            _view = null;
            if (_current == this) _current = null;
        }
    }
}
