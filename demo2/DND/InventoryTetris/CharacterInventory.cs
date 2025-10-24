// filepath: d:\UnityProject\Archive\Assets\demo2\DND\InventoryTetris\CharacterInventory.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 角色背包（乱斗式网格）——仅存储与事件，不包含任何 UI 逻辑。
    /// 严格手动挂载：将本组件挂到具体角色 GameObject 或专用数据容器上。
    /// </summary>
    public class CharacterInventory : MonoBehaviour
    {
        [Header("网格容量（单位：格）")]
        [Min(1)] public int rows = 6;
        [Min(1)] public int cols = 10;

        [Header("初始道具（仅数据源，运行时自动转为 ItemInstance）")]
        [SerializeField] private List<ItemBaseSO> initialItems = new List<ItemBaseSO>();

        // 运行时实例集合
        private readonly List<ItemInstance> items = new List<ItemInstance>();

        /// <summary>
        /// 背包变更事件（增/删/清空时触发）。
        /// </summary>
        public event Action OnInventoryChanged;

        public IReadOnlyList<ItemInstance> Items => items;

        private void Awake()
        {
            // 将初始 SO 转为运行时实例
            if (initialItems != null)
            {
                for (int i = 0; i < initialItems.Count; i++)
                {
                    var so = initialItems[i];
                    if (so == null) continue;
                    items.Add(new ItemInstance(so));
                }
            }
            // 注意：不在 Awake 中触发事件，避免与 UI 绑定器/网格视图的 Awake/Start 顺序产生竞态
        }

        private void Start()
        {
            // 在 Start 再主动触发一次变更，确保订阅者（通常是 UI）已经完成 Awake/OnEnable
            if (items.Count > 0)
            {
                OnInventoryChanged?.Invoke();
            }
        }

        /// <summary>
        /// 直接添加一个已有的运行时物品实例（不涉及 UI 放置）。
        /// 成功加入列表后触发事件。
        /// </summary>
        public void AddInstance(ItemInstance inst)
        {
            if (inst == null) return;
            items.Add(inst);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// 从背包移除指定实例（不涉及 UI）。
        /// </summary>
        public bool RemoveInstance(ItemInstance inst)
        {
            if (inst == null) return false;
            bool ok = items.Remove(inst);
            if (ok) OnInventoryChanged?.Invoke();
            return ok;
        }

        /// <summary>
        /// 清空背包。
        /// </summary>
        public void ClearAll()
        {
            if (items.Count == 0) return;
            items.Clear();
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// 便捷方法：根据 SO 创建运行时实例，但不进行任何 UI 放置。
        /// 调用方（UI 绑定器）应在确认有空间落位后再调用 AddInstance。
        /// </summary>
        public static ItemInstance CreateInstance(ItemBaseSO so)
        {
            if (so == null) return null;
            return new ItemInstance(so);
        }
    }
}
