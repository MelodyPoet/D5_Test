// filepath: d:\UnityProject\Archive\Assets\demo2\DND\InventoryTetris\InventoryUIBinder.cs
using UnityEngine;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 背包 UI 绑定器：将 CharacterInventory 的运行时物品实例“落地”到 InventoryGridView。
    /// 严格手动挂载：需在 Inspector 中手动拖拽 sourceInventory 与 gridView。
    /// 不做 AddComponent；不包含任何战斗/装备逻辑。
    /// </summary>
    public class InventoryUIBinder : MonoBehaviour
    {
        [Header("数据源（严格手动挂载）")]
        public CharacterInventory sourceInventory;

        [Header("网格视图（严格手动挂载）")]
        public InventoryGridView gridView;

        [Header("绑定选项")]
        public bool clearAndRebuildOnBind = true;
        public bool bindOnStart = true;

        [Header("调试（可选）")]
        public bool debugLogs;

        private void OnEnable()
        {
            if (sourceInventory != null)
            {
                sourceInventory.OnInventoryChanged += RefreshFromInventory;
            }
        }

        private void OnDisable()
        {
            if (sourceInventory != null)
            {
                sourceInventory.OnInventoryChanged -= RefreshFromInventory;
            }
        }

        private void Start()
        {
            if (bindOnStart)
            {
                if (debugLogs)
                {
                    string src = sourceInventory != null ? sourceInventory.gameObject.name : "<null>";
                    string grid = gridView != null ? gridView.gameObject.name : "<null>";
                    Debug.Log($"[InventoryUIBinder] Start -> source= {src}, grid= {grid}");
                }
                RefreshFromInventory();
            }
        }

        /// <summary>
        /// 将数据源中的 ItemInstance 按首个可用位置落地到网格；
        /// 若 clearAndRebuildOnBind=true，会先清空现有UI再逐一放置，确保映射一致性。
        /// </summary>
        public void RefreshFromInventory()
        {
            if (sourceInventory == null || gridView == null)
            {
                if (debugLogs) Debug.LogWarning("[InventoryUIBinder] Refresh aborted: sourceInventory or gridView is null.");
                return;
            }
            if (clearAndRebuildOnBind) gridView.ClearAndRebuild();

            var items = sourceInventory.Items;
            if (debugLogs)
            {
                Debug.Log($"[InventoryUIBinder] Refresh items count = {items.Count}");
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
                    string name = inst.data != null ? inst.data.displayName : inst.instanceId;
                    Debug.Log(view != null ? $"[InventoryUIBinder] Spawned: {name}" : $"[InventoryUIBinder] Spawn failed: {name}");
                }
            }
        }

        /// <summary>
        /// 作为示例的最小“新增”接口：尝试将一个新 SO 物品加入背包。
        /// 仅当能在当前网格找到位置并成功落地时，才真正加入数据源；否则不改动数据源。
        /// </summary>
        public bool TryAddNew(ItemBaseSO so)
        {
            if (so == null || sourceInventory == null || gridView == null) return false;
            var inst = CharacterInventory.CreateInstance(so);
            if (inst == null) return false;

            var view = gridView.SpawnInstance(inst);
            if (view == null) return false; // 没有空间

            sourceInventory.AddInstance(inst);
            return true;
        }

        /// <summary>
        /// 从 UI 与数据源同时移除实例。
        /// </summary>
        public bool Remove(ItemInstance inst)
        {
            if (inst == null || sourceInventory == null || gridView == null) return false;
            bool ok = gridView.Remove(inst);
            ok = sourceInventory.RemoveInstance(inst) && ok;
            return ok;
        }
    }
}
