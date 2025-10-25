// filepath: d:\UnityProject\Archive\Assets\demo2\DND\InventoryTetris\InventoryUIBinder.cs
using UnityEngine;
using System.Collections.Generic;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 供属性UI绑定器实现的最小接口（避免在此处强依赖具体类名，便于Inspector手动挂载任意实现）。
    /// </summary>
    public interface ICharacterStatsUIBinder
    {
        void Bind(CharacterStats stats);
        void Unbind();
    }

    /// <summary>
    /// 背包 UI 绑定器：将 CharacterInventory 的运行时物品实例“落地”到 InventoryGridView。
    /// 严格手动挂载：需在 Inspector 中手动将多个“角色预制件或实例”上的 CharacterInventory 拖入 sourceInventories。
    /// 不做 AddComponent；不包含任何战斗/装备逻辑。
    /// 说明：本组件不再支持任何全局 PlayerData 或“单一默认来源”。
    /// </summary>
    public class InventoryUIBinder : MonoBehaviour
    {
        [Header("网格视图（手动挂载）")]
        public InventoryGridView gridView;

        [Header("角色属性UI（手动挂载，可选）")]
        [Tooltip("可选：若提供，将在切换活动角色时刷新属性显示（字段应挂载一个实现了 ICharacterStatsUIBinder 的组件）。")]
        public MonoBehaviour statsUIBinder; // 运行时转换为 ICharacterStatsUIBinder

        [Header("绑定选项")]
        public bool clearAndRebuildOnBind = true;
        public bool bindOnStart = true;

        [Header("网格显示模式")]
        [Tooltip("启用后，使用固定的格子大小/间距/内边距；不同角色仅改变格子数量（rows/cols），图标大小保持一致。建议配合 ScrollRect，使内容可滚动。")]
        public bool useUniformCellSize = true;
        public Vector2 uniformCellSize = new Vector2(96, 96);
        public Vector2 uniformSpacing = new Vector2(8, 8);
        public Vector2 uniformPadding = new Vector2(8, 8);

        [Header("切换策略（运行时自动收集）")]
        [Tooltip("当自动收集到第一个可用背包，且当前无活动来源时，是否自动切换为该背包。")]
        public bool autoSwitchToFirstReady = true;

        [Header("调试（可选）")]
        public bool debugLogs;

        // 内部数据源：仅运行时维护
        private readonly List<CharacterInventory> sourceInventories = new List<CharacterInventory>();
        public IReadOnlyList<CharacterInventory> Sources => sourceInventories;
        public int activeSourceIndex;

        private CharacterInventory subscribedSource; // 当前已订阅事件的来源

        private CharacterInventory ActiveSource
        {
            get
            {
                if (sourceInventories.Count == 0) return null;
                if (activeSourceIndex < 0 || activeSourceIndex >= sourceInventories.Count) return null;
                return sourceInventories[activeSourceIndex];
            }
        }

        private void OnEnable()
        {
            CharacterInventory.OnAnyInventoryReady += HandleInventoryReady;
            CharacterInventory.OnAnyInventoryDestroyed += HandleInventoryDestroyed;
            CollectExistingInventoriesInScene();
            SubscribeActive();
            UpdateStatsUI();
        }

        private void OnDisable()
        {
            CharacterInventory.OnAnyInventoryReady -= HandleInventoryReady;
            CharacterInventory.OnAnyInventoryDestroyed -= HandleInventoryDestroyed;
            UnsubscribeActive();
        }

        private void Start()
        {
            if (bindOnStart)
            {
                if (debugLogs)
                {
                    string src = ActiveSource != null ? ActiveSource.gameObject.name : "<null>";
                    string grid = gridView != null ? gridView.gameObject.name : "<null>";
                    Debug.Log($"[InventoryUIBinder] Start -> activeSource= {src}, grid= {grid}");
                }
                RefreshFromInventory();
                UpdateStatsUI();
            }
        }

        // 收集当前场景中已存在的 CharacterInventory（包含已激活/未激活）
        private void CollectExistingInventoriesInScene()
        {
            var arr = FindObjectsByType<CharacterInventory>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool hadAny = sourceInventories.Count > 0;
            for (int i = 0; i < arr.Length; i++)
            {
                var inv = arr[i];
                if (inv == null) continue;
                if (!inv.gameObject.scene.IsValid()) continue; // 过滤预制体资源
                if (sourceInventories.Contains(inv)) continue;
                sourceInventories.Add(inv);
            }
            if (!hadAny && autoSwitchToFirstReady && sourceInventories.Count > 0)
            {
                SetActiveSourceIndex(0);
            }
        }

        private void HandleInventoryReady(CharacterInventory inv)
        {
            if (inv == null) return;
            if (!inv.gameObject.scene.IsValid()) return;
            if (!sourceInventories.Contains(inv))
            {
                sourceInventories.Add(inv);
                if (debugLogs) Debug.Log($"[InventoryUIBinder] 收集到运行时背包: {inv.name}");
            }

            // 若目前没有活动来源且允许自动切换到第一个
            if (sourceInventories.Count == 1 && autoSwitchToFirstReady)
            {
                SetActiveSourceIndex(0);
                return;
            }

            if (ActiveSource == inv)
            {
                RefreshFromInventory();
                UpdateStatsUI();
            }
        }

        private void HandleInventoryDestroyed(CharacterInventory inv)
        {
            if (inv == null) return;
            int idx = sourceInventories.IndexOf(inv);
            if (idx < 0) return;

            bool wasActive = (ActiveSource == inv);
            sourceInventories.RemoveAt(idx);
            if (wasActive)
            {
                UnsubscribeActive();
                // 重新夹紧索引并订阅/刷新
                if (sourceInventories.Count == 0)
                {
                    activeSourceIndex = 0;
                }
                else
                {
                    activeSourceIndex = Mathf.Clamp(activeSourceIndex, 0, sourceInventories.Count - 1);
                }
                SubscribeActive();
                RefreshFromInventory();
                UpdateStatsUI();
            }
        }

        private void SubscribeActive()
        {
            var src = ActiveSource;
            if (src == null) return;
            if (subscribedSource == src) return;
            UnsubscribeActive();
            src.OnInventoryChanged += RefreshFromInventory;
            subscribedSource = src;
        }

        private void UnsubscribeActive()
        {
            if (subscribedSource != null)
            {
                subscribedSource.OnInventoryChanged -= RefreshFromInventory;
                subscribedSource = null;
            }
        }

        /// <summary>
        /// 在多来源列表中设置活动索引。
        /// </summary>
        public void SetActiveSourceIndex(int index)
        {
            if (index < 0 || index >= sourceInventories.Count)
            {
                if (debugLogs) Debug.LogWarning($"[InventoryUIBinder] SetActiveSourceIndex invalid: {index}");
                return;
            }
            if (activeSourceIndex == index && subscribedSource == ActiveSource)
            {
                RefreshFromInventory();
                UpdateStatsUI();
                return;
            }
            UnsubscribeActive();
            activeSourceIndex = index;
            SubscribeActive();
            RefreshFromInventory();
            UpdateStatsUI();
        }

        /// <summary>
        /// 将数据源中的 ItemInstance 按首个可用位置落地到网格；
        /// 若 clearAndRebuildOnBind=true，会先清空现有UI再逐一放置，确保映射一致性。
        /// 现已支持按照“活动来源”的 rows/cols 自动配置 GridView。
        /// </summary>
        public void RefreshFromInventory()
        {
            var src = ActiveSource;
            if (src == null || gridView == null)
            {
                if (debugLogs) Debug.LogWarning("[InventoryUIBinder] Refresh aborted: ActiveSource or gridView is null.");
                return;
            }

            // 若当前来源不是场景实例（可能是误填了预制体资产），跳过刷新，等待自动收集到真正的场景实例
            if (!src.gameObject.scene.IsValid())
            {
                if (debugLogs)
                {
                    Debug.LogWarning("[InventoryUIBinder] ActiveSource 不在场景中（可能是预制体资产），已跳过刷新；等待运行时实例化与收集。");
                }
                return;
            }

            // 统一背包表现：固定格子尺寸（保持图标大小一致），只随 rows/cols 改变内容范围
            if (useUniformCellSize && gridView != null)
            {
                gridView.autoFitToContainer = false;
                gridView.autoResizeContainer = true;
                gridView.cellSize = uniformCellSize;
                gridView.spacing = uniformSpacing;
                gridView.padding = uniformPadding;
            }

            // 确保 GridView 行列与来源容量一致
            if (gridView.rows != src.rows || gridView.cols != src.cols)
            {
                gridView.Configure(src.rows, src.cols);
            }
            else if (clearAndRebuildOnBind)
            {
                gridView.ClearAndRebuild();
            }

            // 根据当前模式刷新容器尺寸或自适应 cell
            gridView.RefreshLayoutSize();

            var items = src.Items;
            if (debugLogs)
            {
                Debug.Log($"[InventoryUIBinder] Refresh items count = {items.Count} from '{src.gameObject.name}'");
            }
            for (int i = 0; i < items.Count; i++)
            {
                var inst = items[i];
                if (inst == null)
                {
                    if (debugLogs) Debug.LogWarning($"[InventoryUIBinder] item[{i}] is null, skip");
                    continue;
                }
                var view = gridView.SpawnInstance(inst);
                if (debugLogs)
                {
                    string itemName = inst.data != null ? inst.data.displayName : inst.instanceId;
                    Debug.Log(view != null ? $"[InventoryUIBinder] Spawned: {itemName}" : $"[InventoryUIBinder] Spawn failed: {itemName}");
                }
            }
        }

        /// <summary>
        /// 可选：根据当前活动来源所在对象上的 CharacterStats 刷新属性 UI。
        /// </summary>
        public void UpdateStatsUI()
        {
            if (statsUIBinder == null)
            {
                return;
            }
            var binder = statsUIBinder as ICharacterStatsUIBinder;
            if (binder == null)
            {
                Debug.LogWarning("statsUIBinder 未实现 ICharacterStatsUIBinder，已忽略属性UI刷新。");
                return;
            }

            var src = ActiveSource;
            CharacterStats stats = null;
            if (src != null)
            {
                stats = src.GetComponent<CharacterStats>();
                if (stats == null)
                {
                    // 兼容：在父级上查找
                    stats = src.GetComponentInParent<CharacterStats>();
                }
            }
            if (stats != null)
            {
                binder.Bind(stats);
            }
            else
            {
                binder.Unbind();
            }
        }

        // 便捷方法：切换到下一个/上一个角色（环绕）
        public void NextCharacter()
        {
            int count = sourceInventories.Count;
            if (count <= 0) return;
            int next = (activeSourceIndex + 1) % count;
            SetActiveSourceIndex(next);
        }

        public void PrevCharacter()
        {
            int count = sourceInventories.Count;
            if (count <= 0) return;
            int prev = (activeSourceIndex - 1 + count) % count;
            SetActiveSourceIndex(prev);
        }

        /// <summary>
        /// 作为示例的最小“新增”接口：尝试将一个新 SO 物品加入“当前活动背包”。
        /// 仅当能在当前网格找到位置并成功落地时，才真正加入数据源；否则不改动数据源。
        /// </summary>
        public bool TryAddNew(ItemBaseSO so)
        {
            var src = ActiveSource;
            if (so == null || src == null || gridView == null) return false;
            var inst = CharacterInventory.CreateInstance(so);
            if (inst == null) return false;

            var view = gridView.SpawnInstance(inst);
            if (view == null) return false; // 没有空间

            src.AddInstance(inst);
            return true;
        }

        /// <summary>
        /// 从 UI 与“当前活动数据源”同时移除实例。
        /// </summary>
        public bool Remove(ItemInstance inst)
        {
            var src = ActiveSource;
            if (inst == null || src == null || gridView == null) return false;
            bool ok = gridView.Remove(inst);
            ok = src.RemoveInstance(inst) && ok;
            return ok;
        }
    }
}
