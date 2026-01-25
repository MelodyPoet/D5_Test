﻿﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 背包网格视图：将格子坐标转换为 UI 位置，负责物品视图的生成与拖拽配合。
    /// 数据来源：CharacterInventory（通过 InventoryUIBinder 进行落地）。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class InventoryGridView : MonoBehaviour
    {
        private const int MaxRows = 7;
        private const int MaxCols = 15;

        [Header("网格尺寸（单位：格）")]
        [Range(1, MaxRows)] public int rows = 6;
        [Range(1, MaxCols)] public int cols = 10;

        [HideInInspector]
        public Vector2 cellSize = new Vector2(96, 96);
        [HideInInspector]
        public Vector2 spacing = new Vector2(8, 8);
        [HideInInspector]
        public Vector2 padding = new Vector2(8, 8); // 左上内边距

        [Header("预制体与父容器")]
        public RectTransform container; // 为空时使用自身 RectTransform
        public InventoryItemView itemViewPrefab;

        [Header("布局（可选）")]
        public bool autoResizeContainer; // 若为 true，则按行列计算 container 尺寸（用于 ScrollRect 内容）

        [Header("自适应容器")]
        [Tooltip("根据 container 实际尺寸反算 cellSize，使网格充满容器而不是由 cellSize 推容器大小（与 autoResizeContainer 互斥，优先使用本选项）")]
        public bool autoFitToContainer = false;
        [Tooltip("当自适应容器时，是否保持方格（x=y，取宽高中较小值）")]
        public bool keepSquareCells = true;

        [Header("调试（可选）")]
        public bool debugLogs;

        [Header("可视化（调试）")]
        [Tooltip("在容器下渲染一个棋盘格，便于观察每个网格单元的位置与大小（仅调试用)")]
        public bool showDebugChessboard = false;
        [Tooltip("棋盘格颜色 A（含透明度）")]
        public Color debugColorA = new Color(1f, 1f, 1f, 0.05f);
        [Tooltip("棋盘格颜色 B（含透明度）")]
        public Color debugColorB = new Color(0f, 0f, 0f, 0.05f);

        [Header("显示选项")]
        [Tooltip("当物品跨越多个格时，是否在视觉尺寸中包含格间距（true=物品覆盖到格间隙，false=只等于格子面积之和，不覆盖间隙)")]
        public bool includeSpacingInItemSize = true;

        [Header("容器对齐")]
        [Tooltip("将 container 的锚点与 pivot 统一为左上，并将 anchoredPosition 归零，使网格从父容器左上角开始布局（避免在父容器中居中)")]
        public bool alignContainerTopLeft = true;
        [Tooltip("若父物体使用 LayoutGroup（Horizontal/Vertical/Grid），强制其 Child Alignment 为 Upper Left，避免子对象被居中对齐")]
        public bool forceParentLayoutUpperLeft = true;

        // 内部状态
        private RectTransform _rect;
        private InventoryGridModel _model;
        private readonly Dictionary<ItemInstance, InventoryItemView> _views = new Dictionary<ItemInstance, InventoryItemView>();
        private RectTransform _debugBoard; // 棋盘格根结点（调试）

        // 对外属性（供其他组件访问）
        public InventoryGridModel Model => _model;
        public RectTransform Rect => _rect;

        [Header("数据源（由 Binder 赋值）")]
        [Tooltip("当前网格绑定的角色背包实例，供物品视图查找装备组件与属性组件。")]
        [HideInInspector] public CharacterInventory SourceInventory;

        [Tooltip("可选：显式指定角色的装备组件。若设置，则优先使用该引用，而不是从 SourceInventory 层级中自动查找。")]
        [HideInInspector] public CharacterEquipment OverrideEquipment;

        public CharacterEquipment SourceEquipment
        {
            get
            {
                if (OverrideEquipment != null) return OverrideEquipment;
                if (SourceInventory == null) return null;
                var eq = SourceInventory.GetComponent<CharacterEquipment>()
                         ?? SourceInventory.GetComponentInParent<CharacterEquipment>()
                         ?? SourceInventory.GetComponentInChildren<CharacterEquipment>(true);
                return eq;
            }
        }

        private void Awake()
        {
            ClampCapacity();
            _rect = GetComponent<RectTransform>();
            if (container == null) container = _rect;
            if (alignContainerTopLeft && container != null)
            {
                AlignContainerToTopLeft();
            }
            _model = new InventoryGridModel(rows, cols);

            if (autoFitToContainer)
            {
                RecalculateCellSizeFromContainer();
            }
            else if (autoResizeContainer)
            {
                RefreshLayoutSize();
            }
            RebuildDebugBoard();
        }

        private void OnValidate()
        {
            // 编辑器中修改 rows/cols 时进行约束，并刷新布局可视化
            ClampCapacity();
#if UNITY_EDITOR
            if (_model != null)
            {
                // InventoryGridModel 未暴露 Rows/Cols 属性，这里直接重建以匹配新的行列配置
                _model = new InventoryGridModel(rows, cols);
            }
            if (container != null)
            {
                if (autoFitToContainer) RecalculateCellSizeFromContainer();
                else if (autoResizeContainer) RefreshLayoutSize();
            }
            if (showDebugChessboard) RebuildDebugBoard();
#endif
        }

        private void ClampCapacity()
        {
            int newRows = Mathf.Clamp(rows, 1, MaxRows);
            int newCols = Mathf.Clamp(cols, 1, MaxCols);
            if (newRows != rows || newCols != cols)
            {
                rows = newRows;
                cols = newCols;
                if (debugLogs)
                {
                    Debug.Log($"[InventoryGridView] 网格容量已约束为 Rows={rows} (<= {MaxRows}), Cols={cols} (<= {MaxCols})");
                }
            }
        }

        private void OnEnable()
        {
            // 再次对齐，避免外部在 Awake/Start 之后修改父布局导致回弹到居中
            if (alignContainerTopLeft && container != null)
            {
                AlignContainerToTopLeft();
            }
        }

        private void AlignContainerToTopLeft()
        {
            // 统一 container 锚点/枢轴为左上，并归零偏移
            container.anchorMin = new Vector2(0f, 1f);
            container.anchorMax = new Vector2(0f, 1f);
            container.pivot = new Vector2(0f, 1f);
            container.anchoredPosition = Vector2.zero;

            // 若父节点使用布局组件，强制设为左上对齐，避免子对象居中
            FixParentLayoutAlignment();
        }

        private void FixParentLayoutAlignment()
        {
            if (!forceParentLayoutUpperLeft || container == null) return;
            var parent = container.transform.parent as RectTransform;
            if (parent == null) return;
            var lg = parent.GetComponent<LayoutGroup>();
            if (lg != null && lg.childAlignment != TextAnchor.UpperLeft)
            {
                lg.childAlignment = TextAnchor.UpperLeft;
                if (debugLogs)
                {
                    Debug.Log("[InventoryGridView] 父布局组对齐方式已强制为 UpperLeft 以避免子对象居中。");
                }
            }
        }

        /// <summary>
        /// 重新配置网格行列并清空现有物品视图。
        /// </summary>
        public void Configure(int r, int c)
        {
            rows = Mathf.Max(1, r);
            cols = Mathf.Max(1, c);
            ClampCapacity();
            _model = new InventoryGridModel(rows, cols);

            foreach (var view in _views.Values)
            {
                if (view != null) Destroy(view.gameObject);
            }
            _views.Clear();

            if (alignContainerTopLeft && container != null)
            {
                AlignContainerToTopLeft();
            }

            if (autoFitToContainer)
            {
                RecalculateCellSizeFromContainer();
            }
            else if (autoResizeContainer)
            {
                RefreshLayoutSize();
            }
            RebuildDebugBoard();
        }

        /// <summary>
        /// 由 ScriptableObject 直接生成运行时实例并落地（用于独立测试）。
        /// 常规使用建议走 SpawnInstance（由 Binder 驱动）。
        /// </summary>
        public InventoryItemView SpawnItem(ItemBaseSO so)
        {
            if (so == null || itemViewPrefab == null) return null;
            var inst = new ItemInstance(so);
            var view = SpawnInstance(inst);
            if (debugLogs)
            {
                Debug.Log(view != null ? $"SpawnItem success: {so.displayName}" : $"SpawnItem failed (no space): {so.displayName}");
            }
            return view;
        }

        /// <summary>
        /// 将已有实例落地到首个可用位置。
        /// </summary>
        public InventoryItemView SpawnInstance(ItemInstance inst)
        {
            if (inst == null || itemViewPrefab == null) return null;
            if (_views.ContainsKey(inst)) return _views[inst];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (_model.CanPlace(inst, x, y))
                    {
                        // 先创建视图，成功后再占用网格，避免视图创建失败导致数据残留
                        var view = CreateView(inst);
                        if (view == null)
                        {
                            if (debugLogs)
                            {
                                string nameFail = inst.data != null ? inst.data.displayName : inst.instanceId;
                                Debug.LogError($"CreateView failed for '{nameFail}'. Check itemViewPrefab has InventoryItemView component on root or children.");
                            }
                            return null;
                        }

                        _model.TryPlace(inst, x, y);
                        PositionViewAtGrid(view, x, y);
                        _views[inst] = view;
                        if (debugLogs)
                        {
                            string itemDisplayName = inst?.data?.displayName ?? inst?.instanceId ?? "<null>";
                            Debug.Log($"SpawnInstance success: {itemDisplayName} at ({x},{y})");
                        }
                        return view;
                    }
                }
            }

            if (debugLogs)
            {
                string itemDisplayName = inst?.data?.displayName ?? inst?.instanceId ?? "<null>";
                Debug.LogWarning($"SpawnInstance failed (no space): {itemDisplayName}");
            }
            Debug.LogWarning("没有空间放置该实例物品");
            return null;
        }

        /// <summary>
        /// 清空并重建内部模型与视图（保持行列配置）。
        /// </summary>
        public void ClearAndRebuild()
        {
            foreach (var kv in _views)
            {
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            }
            _views.Clear();
            _model = new InventoryGridModel(rows, cols);
            if (autoFitToContainer)
            {
                RecalculateCellSizeFromContainer();
            }
            else if (autoResizeContainer)
            {
                RefreshLayoutSize();
            }
            if (debugLogs) Debug.Log("GridView cleared and rebuilt.");
            RebuildDebugBoard();
        }

        /// <summary>
        /// 按行列/cell/spacing/padding 计算容器像素尺寸（ScrollRect 内容）。
        /// </summary>
        public void RefreshLayoutSize()
        {
            if (container == null) return;
            if (alignContainerTopLeft)
            {
                AlignContainerToTopLeft();
            }
            if (autoFitToContainer)
            {
                RecalculateCellSizeFromContainer();
                if (showDebugChessboard) RebuildDebugBoard();
                return;
            }

            float width = padding.x * 2f + cols * cellSize.x + Mathf.Max(0, cols - 1) * spacing.x;
            float height = padding.y * 2f + rows * cellSize.y + Mathf.Max(0, rows - 1) * spacing.y;
            container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            container.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (debugLogs) Debug.Log($"Container resized to {width}x{height} for grid {cols}x{rows}.");
            if (showDebugChessboard) RebuildDebugBoard();
        }

        /// <summary>
        /// 尝试移动已落地实例到目标格坐标。
        /// </summary>
        public bool TryMove(ItemInstance item, int x, int y)
        {
            // 若模型中没有记录该物品的位置（例如某些初始化/竞态导致丢失），尝试直接按新位置落地
            Vector2Int oldPos;
            bool hasMapping = _model.TryGetPosition(item, out oldPos);
            if (!hasMapping)
            {
                bool can = _model.CanPlace(item, x, y);
                if (debugLogs)
                {
                    string id = item != null ? (item.data != null ? item.data.displayName : item.instanceId) : "<null>";
                    Debug.Log($"[GridView] TryMove (no mapping) '{id}' -> ({x},{y}) can={can}");
                }
                if (!can) return false;
                bool placed = _model.TryPlace(item, x, y);
                if (placed && _views.TryGetValue(item, out var v0))
                {
                    PositionViewAtGrid(v0, x, y);
                }
                return placed;
            }

            var ok = _model.TryMove(item, x, y);
            if (debugLogs)
            {
                string id = item != null ? (item.data != null ? item.data.displayName : item.instanceId) : "<null>";
                Debug.Log($"[GridView] TryMove (mapped {oldPos}) '{id}' -> ({x},{y}) result={(ok?"OK":"FAIL")}");
            }
            if (ok && _views.TryGetValue(item, out var v))
            {
                PositionViewAtGrid(v, x, y);
            }
            return ok;
        }

        /// <summary>
        /// 从网格与视图中移除实例。
        /// </summary>
        public bool Remove(ItemInstance item)
        {
            if (!_model.Remove(item)) return false;
            if (_views.TryGetValue(item, out var v))
            {
                if (v != null)
                {
                    Destroy(v.gameObject);
                }
                _views.Remove(item);
            }
            return true;
        }

        /// <summary>
        /// 刷新所有条目的“已装备”标签。
        /// </summary>
        public void RefreshAllEquipLabels()
        {
            if (_views == null || _views.Count == 0) return;
            foreach (var kv in _views)
            {
                var view = kv.Value;
                if (view != null)
                {
                    view.RefreshEquipLabel();
                }
            }
        }

        /// <summary>
        /// 获取实例当前的格子坐标。
        /// </summary>
        public bool TryGetGridPosition(ItemInstance item, out Vector2Int pos)
        {
            bool ok = _model.TryGetPosition(item, out pos);
            if (debugLogs)
            {
                string id = item != null ? (item.data != null ? item.data.displayName : item.instanceId) : "<null>";
                Debug.Log($"[GridView] TryGetGridPosition for '{id}' -> {(ok ? pos.ToString() : "<none>")}");
            }
            return ok;
        }

        /// <summary>
        /// 获取指针在网格坐标系下（相对容器左上，扣除 padding 后）的像素偏移。
        /// 返回 true 表示转换成功；lx/ly 可为负或超出内容范围。
        /// </summary>
        public bool TryGetPointerGridLocal(PointerEventData eventData, out float lx, out float ly)
        {
            lx = ly = 0f;
            if (container == null || eventData == null)
            {
                if (debugLogs)
                {
                    Debug.Log("[GridView] TryGetPointerGridLocal failed: container or eventData is null");
                }
                return false;
            }

            // 选择相机
            Camera cam = null;
            var canvas = container.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    cam = null;
                }
                else
                {
                    // 优先用 Canvas.worldCamera（比 eventData 上的相机更可靠）
                    cam = canvas.worldCamera;
                    if (cam == null)
                    {
                        cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
                        if (cam == null) cam = Camera.main;
                    }
                }
            }
            else
            {
                cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
                if (cam == null) cam = Camera.main;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, eventData.position, cam, out var local))
            {
                if (debugLogs)
                {
                    Debug.Log("[GridView] TryGetPointerGridLocal failed: ScreenPointToLocalPointInRectangle returned false");
                }
                return false;
            }

            var rect = container.rect;
            float fromLeft = local.x + rect.width * container.pivot.x;
            float fromTop  = rect.height * (1f - container.pivot.y) - local.y;

            lx = fromLeft - padding.x;
            ly = fromTop  - padding.y;
            return true;
        }

        // 内部：创建并绑定物品视图
        private InventoryItemView CreateView(ItemInstance item)
        {
            var go = Instantiate(itemViewPrefab.gameObject, container);
            var view = go.GetComponent<InventoryItemView>();
            if (view == null)
            {
                // 兼容：若组件不在根节点，尝试在子节点查找
                view = go.GetComponentInChildren<InventoryItemView>(true);
            }
            if (view == null)
            {
                Debug.LogError("itemViewPrefab 缺少 InventoryItemView 组件（根或子节点均未找到）。请在条目预制体根对象添加该组件，并在其字段中绑定 bgImage/iconImage。");
                Destroy(go);
                return null;
            }

            view.Bind(item, this);
            view.SetCellSize(cellSize); // Pass the grid's cell size to the view
            // 新增：Bind 后立即刷新一次“已装备”标签，避免首帧未显示
            view.RefreshEquipLabel();

            // 视图使用左上对齐定位（直接获取 RectTransform，避免依赖 InventoryItemView.Awake 初始化时序）
            var rt = view.GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogError("[InventoryGridView] 无法定位视图：缺少 RectTransform。");
                Destroy(go);
                return null;
            }
            // 统一为左上锚点 + 左上 pivot，避免预制体锚点导致的偏移/拉伸
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);

            // 防御：若未在预制体上设置 iconImage，尝试自动查找一个合适的子 Image
            if (view.iconImage == null)
            {
                var images = go.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                UnityEngine.UI.Image candidate = null;
                foreach (var img in images)
                {
                    if (img == view.bgImage) continue;
                    if (img.raycastTarget) { candidate = img; break; }
                }
                if (candidate == null)
                {
                    foreach (var img in images)
                    {
                        if (img != view.bgImage) { candidate = img; break; }
                    }
                }
                if (candidate != null)
                {
                    view.iconImage = candidate;
                    Debug.LogWarning($"[InventoryGridView] 预制体未设置 iconImage，已自动绑定到子节点: {candidate.name}");
                }
                else
                {
                    Debug.LogError("[InventoryGridView] 未找到可用的 iconImage。请在条目预制体上给 InventoryItemView.iconImage 指定一个子 Image。");
                }
            }

            // 设置图标
            if (view.iconImage != null)
            {
                if (item != null && item.data != null && item.data.icon != null)
                {
                    view.iconImage.sprite = item.data.icon;
                    view.iconImage.enabled = true;
                    view.iconImage.type = UnityEngine.UI.Image.Type.Simple;
                    view.iconImage.preserveAspect = true;
                }
                else
                {
                    string itemName = (item != null && item.data != null) ? item.data.displayName : item?.instanceId;
                    Debug.LogWarning($"[InventoryGridView] 物品 '{itemName}' 的 icon 为空，使用预制体默认图。");
                }
            }

            // 关键：确保至少有一个 Image 可射线，优先 iconImage
            if (view.iconImage != null)
            {
                view.iconImage.raycastTarget = true;
            }
            else if (view.bgImage != null)
            {
                view.bgImage.raycastTarget = true;
            }

            return view;
        }

        /// <summary>
        /// 将格子坐标转换为本地左上角像素位置。
        /// </summary>
        public Vector2 GridToLocalTopLeft(int x, int y)
        {
            float px = padding.x + x * (cellSize.x + spacing.x);
            float py = -(padding.y + y * (cellSize.y + spacing.y));
            return new Vector2(px, py);
        }

        /// <summary>
        /// 将视图定位到指定格子。
        /// </summary>
        public void PositionViewAtGrid(InventoryItemView view, int x, int y)
        {
            var rt = view != null && view.Rect != null ? view.Rect : view.GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogError("[InventoryGridView] 无法定位视图：缺少 RectTransform。");
                return;
            }
            rt.pivot = new Vector2(0, 1); // Force top-left pivot
            // For cell-based system, the root view's size should not be changed.
            // It acts as an anchor point. The cells inside will define the shape.
            if (!view.useCellSystem)
            {
                rt.sizeDelta = new Vector2(ItemPixelWidth(view.BoundItem), ItemPixelHeight(view.BoundItem));
            }
            rt.anchoredPosition = GridToLocalTopLeft(x, y);
        }

        /// <summary>
        /// 指针屏幕坐标转换为格子坐标（左上为原点）。
        /// </summary>
        public bool PointerToGrid(PointerEventData eventData, out int x, out int y)
        {
            x = -1; y = -1;
            if (container == null || eventData == null)
            {
                if (debugLogs)
                {
                    Debug.Log("[GridView] PointerToGrid failed: container or eventData is null");
                }
                return false;
            }

            // 选择相机：Overlay 传 null；其他模式优先 Canvas.worldCamera
            Camera cam = null;
            var canvas = container.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    cam = null; // Overlay 必须传 null
                }
                else
                {
                    cam = canvas.worldCamera;
                    if (cam == null)
                    {
                        cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
                        if (cam == null) cam = Camera.main;
                    }
                }
            }
            else
            {
                cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
                if (cam == null) cam = Camera.main;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, eventData.position, cam, out var local))
            {
                if (debugLogs)
                {
                    Debug.Log("[GridView] PointerToGrid failed: ScreenPointToLocalPointInRectangle returned false");
                }
                return false;
            }

            var rect = container.rect;
            float fromLeft = local.x + rect.width * container.pivot.x;
            float fromTop  = rect.height * (1f - container.pivot.y) - local.y;

            float lx = fromLeft - padding.x;
            float ly = fromTop  - padding.y;

            float pitchX = cellSize.x + spacing.x;
            float pitchY = cellSize.y + spacing.y;

            if (lx < 0 || ly < 0)
            {
                if (debugLogs)
                {
                    Debug.Log($"[GridView] PointerToGrid out: negative (lx,ly)=({lx:F1},{ly:F1}), local=({local.x:F1},{local.y:F1}), pivot={container.pivot}, rect={rect.size}");
                }
                return false;
            }

            x = Mathf.FloorToInt(lx / pitchX);
            y = Mathf.FloorToInt(ly / pitchY);

            if (x < 0 || y < 0 || x >= cols || y >= rows)
            {
                if (debugLogs)
                {
                    Debug.Log($"[GridView] PointerToGrid out: computed cell=({x},{y}) outside [0..{cols-1}], [0..{rows-1}] from (lx,ly)=({lx:F1},{ly:F1}) pitch=({pitchX:F1},{pitchY:F1})");
                }
                return false;
            }

            if (debugLogs)
            {
                Debug.Log($"[GridView] Pointer local=({local.x:F1},{local.y:F1}) fromLeftTop=({fromLeft:F1},{fromTop:F1}) afterPadding=({lx:F1},{ly:F1}) -> cell=({x},{y}) cam={(cam==null?"null":cam.name)}");
            }
            return true;
        }

        /// <summary>
        /// 计算物品像素宽度（包含格间距）。
        /// </summary>
        public float ItemPixelWidth(ItemInstance item)
        {
            int w = item != null ? item.Width : 1;
            float span = includeSpacingInItemSize ? Mathf.Max(0, w - 1) * spacing.x : 0f;
            return w * cellSize.x + span;
        }

        /// <summary>
        /// 计算物品像素高度（包含格间距）。
        /// </summary>
        public float ItemPixelHeight(ItemInstance item)
        {
            int h = item != null ? item.Height : 1;
            float span = includeSpacingInItemSize ? Mathf.Max(0, h - 1) * spacing.y : 0f;
            return h * cellSize.y + span;
        }

        // ========================= 调试棋盘格 =========================
        private void RebuildDebugBoard()
        {
            // 先尽力清理历史遗留的 DebugBoard（包括场景中可能遗留的同名子物体）
            CleanupDebugBoard();

            // 只在运行时且开启调试时创建，避免在编辑器模式反复生成并保存到场景
            if (!Application.isPlaying || !showDebugChessboard || container == null) return;

            var go = new GameObject("DebugBoard", typeof(RectTransform));
            go.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy;
            _debugBoard = go.GetComponent<RectTransform>();
            _debugBoard.SetParent(container, false);
            _debugBoard.anchorMin = new Vector2(0, 1);
            _debugBoard.anchorMax = new Vector2(0, 1);
            _debugBoard.pivot = new Vector2(0, 1);
            _debugBoard.anchoredPosition = Vector2.zero;
            _debugBoard.sizeDelta = container.rect.size;
            _debugBoard.SetSiblingIndex(0); // 放在最底层

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var cellGo = new GameObject($"cell_{x}_{y}", typeof(RectTransform), typeof(Image));
                    cellGo.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy;
                    var rt = cellGo.GetComponent<RectTransform>();
                    rt.SetParent(_debugBoard, false);
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.sizeDelta = cellSize;
                    rt.anchoredPosition = GridToLocalTopLeft(x, y);

                    var img = cellGo.GetComponent<Image>();
                    img.color = ((x + y) % 2 == 0) ? debugColorA : debugColorB;
                    img.raycastTarget = false;
                }
            }
        }

        private void CleanupDebugBoard()
        {
            // 先销毁已有引用
            if (_debugBoard != null)
            {
                if (Application.isPlaying) Destroy(_debugBoard.gameObject);
                else DestroyImmediate(_debugBoard.gameObject);
                _debugBoard = null;
            }
            // 再尝试按照名称查找并清理（防止因脚本重载/域重载导致引用丢失而残留）
            if (container != null)
            {
                var t = container.Find("DebugBoard");
                if (t != null)
                {
                    if (Application.isPlaying) Destroy(t.gameObject);
                    else DestroyImmediate(t.gameObject);
                }
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            // 当本对象尺寸变化时（通常 container==自身），自适应模式下重算 cellSize
            if (autoFitToContainer)
            {
                RecalculateCellSizeFromContainer();
            }
            // 调试棋盘自适应容器尺寸
            RebuildDebugBoard();
        }

        private void RecalculateCellSizeFromContainer()
        {
            if (container == null) return;
            var rect = container.rect; // 当前像素尺寸
            float availW = Mathf.Max(0f, rect.width - padding.x * 2f - Mathf.Max(0, cols - 1) * spacing.x);
            float availH = Mathf.Max(0f, rect.height - padding.y * 2f - Mathf.Max(0, rows - 1) * spacing.y);
            float cx = availW / Mathf.Max(1, cols);
            float cy = availH / Mathf.Max(1, rows);

            if (keepSquareCells)
            {
                float s = Mathf.Max(0f, Mathf.Min(cx, cy));
                cellSize = new Vector2(s, s);
            }
            else
            {
                cellSize = new Vector2(Mathf.Max(0f, cx), Mathf.Max(0f, cy));
            }

            if (debugLogs)
            {
                Debug.Log($"CellSize recalculated to {cellSize.x}x{cellSize.y} from container {rect.width}x{rect.height} for grid {cols}x{rows}.");
            }
            // 更新现有视图尺寸与位置
            RefreshAllViewRects();
            if (showDebugChessboard) RebuildDebugBoard();
        }

        private void RefreshAllViewRects()
        {
            if (_views == null || _views.Count == 0) return;
            foreach (var kv in _views)
            {
                var item = kv.Key;
                var view = kv.Value;
                if (item == null || view == null) continue;
                if (_model != null && _model.TryGetPosition(item, out var pos))
                {
                    // 直接更新尺寸与位置，避免额外的布局开销
                    var rt = view.Rect != null ? view.Rect : view.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.sizeDelta = new Vector2(ItemPixelWidth(item), ItemPixelHeight(item));
                        rt.anchoredPosition = GridToLocalTopLeft(pos.x, pos.y);
                    }
                }
            }
        }

        private void OnTransformParentChanged()
        {
            // 父对象改变后，若要求左上对齐且父有布局组件，重新校正一次，避免被居中。
            if (alignContainerTopLeft && container != null)
            {
                FixParentLayoutAlignment();
            }
            // 同时清理/重建一次调试棋盘，避免层级变化导致重复
            RebuildDebugBoard();
        }

        private void OnDisable()
        {
            // 退出运行或对象被禁用时清理调试棋盘，避免残留
            CleanupDebugBoard();
        }

        private void OnDestroy()
        {
            // 保底清理
            CleanupDebugBoard();
        }
    }
}
