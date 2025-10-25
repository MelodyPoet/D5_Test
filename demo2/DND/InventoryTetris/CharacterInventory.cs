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
        private const int MaxRows = 7;
        private const int MaxCols = 15;

        [Header("网格容量（单位：格）")]
        [Range(1, MaxRows)] public int rows = 6;
        [Range(1, MaxCols)] public int cols = 10;

        [Header("初始道具（仅数据源，运行时自动转为 ItemInstance）")]
        [SerializeField] private List<ItemBaseSO> initialItems = new List<ItemBaseSO>();

        // 运行时实例集合
        private readonly List<ItemInstance> items = new List<ItemInstance>();

        /// <summary>
        /// 背包变更事件（增/删/清空时触发）。
        /// </summary>
        public event Action OnInventoryChanged;

        /// <summary>
        /// 全局事件：任意 CharacterInventory 实例在 Start 后就绪时发布（包含 initialItems -> ItemInstance 转换完成）。
        /// </summary>
        public static event Action<CharacterInventory> OnAnyInventoryReady;

        /// <summary>
        /// 全局事件：任意 CharacterInventory 实例销毁时发布。
        /// </summary>
        public static event Action<CharacterInventory> OnAnyInventoryDestroyed;

        public IReadOnlyList<ItemInstance> Items => items;

        private void ClampCapacity()
        {
            int newRows = Mathf.Clamp(rows, 1, MaxRows);
            int newCols = Mathf.Clamp(cols, 1, MaxCols);
            if (newRows != rows || newCols != cols)
            {
                rows = newRows;
                cols = newCols;
#if UNITY_EDITOR
                Debug.Log($"[CharacterInventory] 已将网格容量约束到 Rows={rows} (<= {MaxRows}), Cols={cols} (<= {MaxCols})。");
#endif
            }
        }

        private void OnValidate()
        {
            // 编辑器中修改时也进行约束
            ClampCapacity();
        }

        private void Awake()
        {
            // 运行时再次保障约束
            ClampCapacity();

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
            // 通知全局：该实例已就绪（无论是否有初始物品）
            OnAnyInventoryReady?.Invoke(this);
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

        private void OnDestroy()
        {
            OnAnyInventoryDestroyed?.Invoke(this);
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
