using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 物品模板查询表：按 itemId 反查 ItemBaseSO。
    /// 内存桥路径直接通过 SerializableItem.runtimeRef 持有引用，不经此表；
    /// 落盘重新加载（或 runtimeRef 为空）时，由 SaveManager 调用本表按 itemId 重建 ItemBaseSO。
    /// </summary>
    public static class ItemDatabase
    {
        private static Dictionary<string, ItemBaseSO> _byId;
        private static bool _loaded;

        /// <summary>
        /// 按 itemId 获取物品模板；首次调用时扫描 Resources 下所有 ItemBaseSO 并缓存。
        /// 若物品未放在 Resources 目录，请改用 Register 手动注册。
        /// </summary>
        public static ItemBaseSO Get(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            EnsureLoaded();
            return _byId != null && _byId.TryGetValue(itemId, out var so) ? so : null;
        }

        /// <summary>
        /// 手动注册物品模板（用于非 Resources 目录的资产，或运行时动态加载后登记）。
        /// </summary>
        public static void Register(ItemBaseSO so)
        {
            if (so == null || string.IsNullOrEmpty(so.itemId)) return;
            EnsureLoaded();
            _byId[so.itemId] = so;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _byId = new Dictionary<string, ItemBaseSO>();
            var all = Resources.LoadAll<ItemBaseSO>("");
            foreach (var so in all)
            {
                if (so != null && !string.IsNullOrEmpty(so.itemId))
                    _byId[so.itemId] = so;
            }
        }

        /// <summary>清空缓存（场景切换/重新加载资产时调用）</summary>
        public static void ClearCache()
        {
            _loaded = false;
            _byId = null;
        }
    }
}
