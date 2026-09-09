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
        private const int MaxRows = 10;
        private const int MaxCols = 16;

        [Header("网格容量（单位：格）")]
        [Range(1, MaxRows)] public int rows = 6;
        [Range(1, MaxCols)] public int cols = 10;

        // 注：初始装备不再由本组件负责。角色初始装备由自定义界面（CharacterCustomizationPanel /
        // HorizontalBattleFormationManager.EquipInitialItems）决定，进入战斗时创建 ItemInstance 并
        // 调用 AddInstance + CharacterEquipment.EquipToSlot 完成初始装备。本组件只负责游戏进程中的
        // 物品/装备“更新”的序列化（AddInstance / RemoveInstance / 装备槽持久化）。

        // 运行时实例集合
        private readonly List<ItemInstance> items = new List<ItemInstance>();

        private CharacterStats cachedStats;

        public event Action OnInventoryChanged;
        public static event Action<CharacterInventory> OnAnyInventoryReady;
        public static event Action<CharacterInventory> OnAnyInventoryDestroyed;

        public IReadOnlyList<ItemInstance> Items => items;

        [Header("持久化/保存（运行时/序列化辅助）")]
        [Tooltip("记录当前已装备的实例 ID（用于在 UI 重新绑定时恢复玩家的装备选择）。由系统自动维护，也可通过序列化工具备份。")]
        public string equippedMainHandId;
        public string equippedArmorId;
        public string equippedShieldId;

        private CharacterEquipment _persistenceEqSubscribed;

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
            ClampCapacity();
        }

        private void Awake()
        {
            ClampCapacity();
            // 不在 Awake 创建实例，避免与绑定器启动顺序产生竞态；统一在 Start 里处理
        }

        private void Start()
        {
            // 初始装备不在此处创建：由自定义界面在进入战斗时通过 AddInstance + EquipToSlot 注入。
            // 本组件启动只做：恢复已保存的装备选择（序列化）、订阅变更、广播就绪、应用一次属性。

            // Attempt to restore previous equip choices (by instance IDs) from saved fields
            var eqForRestore = GetComponent<CharacterEquipment>()
                         ?? GetComponentInParent<CharacterEquipment>()
                         ?? GetComponentInChildren<CharacterEquipment>(true);
            if (eqForRestore != null)
            {
                Debug.Log($"[CharacterInventory] RestoreEquippedFromSavedIds using eq={eqForRestore.gameObject.name}");
                RestoreEquippedFromSavedIds(eqForRestore);
                // Subscribe to equipment change to persist updates
                try {
                    eqForRestore.OnEquipmentChanged += HandleEquipmentChangedFromEquipment;
                    _persistenceEqSubscribed = eqForRestore;
                } catch { }
            }

            // 广播：背包就绪/变更
            if (items.Count > 0) OnInventoryChanged?.Invoke();
            OnAnyInventoryReady?.Invoke(this);

            // 订阅并应用一次装备属性
            OnInventoryChanged += ApplyEquipmentModifiers;
            ApplyEquipmentModifiers();
        }

        // 注：初始装备的创建与装备已移至自定义界面流程（HorizontalBattleFormationManager.EquipInitialItems），
        // 通过 inventory.AddInstance(inst) + equipment.EquipToSlot(slot, inst) 完成。本组件不再自动装备。

        private void ApplyEquipmentModifiers()
        {
            var stats = GetOrFindStats();
            if (stats == null) return;

            var eq = GetComponent<CharacterEquipment>()
                     ?? GetComponentInParent<CharacterEquipment>()
                     ?? GetComponentInChildren<CharacterEquipment>(true);

            // 先移除由背包来源的修饰，防止残留
            stats.RemoveModifiersBySource(this);

            if (eq != null)
            {
                // 背包变更时，同步校正装备槽（物品移出则卸下）
                var mh = eq.GetEquipped(EquipmentSlot.MainHand);
                if (mh != null)
                {
                    if (!items.Contains(mh)) eq.UnequipSlot(EquipmentSlot.MainHand);
                }
                var ar = eq.GetEquipped(EquipmentSlot.Armor);
                if (ar != null)
                {
                    if (!items.Contains(ar)) eq.UnequipSlot(EquipmentSlot.Armor);
                }
                var sh = eq.GetEquipped(EquipmentSlot.OffHand);
                if (sh != null)
                {
                    if (!items.Contains(sh)) eq.UnequipSlot(EquipmentSlot.OffHand);
                }

                // 仅装备槽里的条目生效
                eq.ReapplyEquippedModifiers();
            }
            else
            {
                stats.RequestRecalculateStats();
            }
        }

        public void AddInstance(ItemInstance inst)
        {
            if (inst == null) return;
            items.Add(inst);
            OnInventoryChanged?.Invoke();
        }

        public bool RemoveInstance(ItemInstance inst)
        {
            if (inst == null) return false;
            bool removed = items.Remove(inst);
            if (removed) OnInventoryChanged?.Invoke();
            return removed;
        }

        // ==================== 游戏进程入口（拾取 / 装备 / 卸下） ====================
        // 生命周期约定（供后续“战斗掉落 / 搜索拾取 / 存档读档”等逻辑复用）：
        //   1) 拾取：调用 AddInstance(inst) —— 仅入背包，更新序列化与属性，不改变外观。
        //   2) 装备：调用 EquipItem(inst) —— 经 CharacterEquipment.EquipToSlot 触发外观同步，
        //      角色 Spine 皮肤随装备实时改变（物品自身 appearanceSkinID 优先，否则回落职业默认）。
        //   3) 卸下：调用 Unequip(slot) —— 同样实时刷新外观，回落到职业默认外观（若有）。
        // 注意：不要只调 AddInstance 就期望外观变化；外观仅在“装备/卸下”时刷新。

        /// <summary>
        /// 将一件已入背包的物品装备到其类型对应的槽位（武器→主手 / 盾牌→副手 / 护甲→护甲）。
        /// 会强制覆盖该槽位现有装备，并实时同步属性与 Spine 外观。
        /// 返回是否成功装备。
        /// </summary>
        public bool EquipItem(ItemInstance inst)
        {
            if (inst == null || inst.data == null) return false;
            var eq = GetComponent<CharacterEquipment>()
                     ?? GetComponentInParent<CharacterEquipment>()
                     ?? GetComponentInChildren<CharacterEquipment>(true);
            if (eq == null)
            {
                Debug.LogWarning($"[CharacterInventory] EquipItem: 未找到 CharacterEquipment，无法装备 {inst.instanceId}");
                return false;
            }
            if (!eq.CanEquip(inst)) return false;

            EquipmentSlot slot;
            if (inst.data.isWeapon) slot = EquipmentSlot.MainHand;
            else if (inst.data.isShield) slot = EquipmentSlot.OffHand;
            else if (inst.data.isArmor) slot = EquipmentSlot.Armor;
            else
            {
                Debug.LogWarning($"[CharacterInventory] EquipItem: 物品 {inst.data.displayName} 既非武器/盾牌/护甲，无法装备。");
                return false;
            }
            return eq.EquipToSlot(slot, inst); // 内部会触发 SyncAppearance -> 实时刷新外观
        }

        /// <summary>
        /// 卸下指定槽位（实时刷新属性与外观）。
        /// </summary>
        public bool Unequip(EquipmentSlot slot)
        {
            var eq = GetComponent<CharacterEquipment>()
                     ?? GetComponentInParent<CharacterEquipment>()
                     ?? GetComponentInChildren<CharacterEquipment>(true);
            if (eq == null) return false;
            return eq.UnequipSlot(slot); // 内部会触发 SyncAppearance -> 实时刷新外观
        }

        /// <summary>
        /// 序列化读档 / 外部逻辑在重建背包与装备后，可调用本方法强制按当前装备状态刷新一次外观。
        /// （RestoreSavedEquipment 已会按保存的装备 ID 重新装备并触发外观同步。）
        /// </summary>
        public void ForceResyncAppearance()
        {
            var eq = GetComponent<CharacterEquipment>()
                     ?? GetComponentInParent<CharacterEquipment>()
                     ?? GetComponentInChildren<CharacterEquipment>(true);
            if (eq != null) eq.RefreshAppearance();
        }

        private CharacterStats GetOrFindStats()
        {
            if (cachedStats != null) return cachedStats;
            cachedStats = GetComponent<CharacterStats>()
                ?? GetComponentInParent<CharacterStats>()
                ?? GetComponentInChildren<CharacterStats>(true);
            if (cachedStats == null)
            {
                Debug.LogWarning($"[CharacterInventory] 在 {gameObject.name} 的自身或父级未找到 CharacterStats 组件。某些功能可能无法正常工作.");
            }
            return cachedStats;
        }

        private void OnDestroy()
        {
            // 取消订阅
            OnInventoryChanged -= ApplyEquipmentModifiers;

            // unsubscribe equipment change subscription used for persistence
            if (_persistenceEqSubscribed != null)
            {
                try { _persistenceEqSubscribed.OnEquipmentChanged -= HandleEquipmentChangedFromEquipment; } catch { }
                _persistenceEqSubscribed = null;
            }

            // 广播：背包被销毁
            OnAnyInventoryDestroyed?.Invoke(this);
        }

        private void HandleEquipmentChangedFromEquipment()
        {
            // Synchronize equipped instance IDs with current equipment state
            var eq = GetComponent<CharacterEquipment>()
                     ?? GetComponentInParent<CharacterEquipment>()
                     ?? GetComponentInChildren<CharacterEquipment>(true);
            if (eq == null) return;
            var mh = eq.GetEquipped(EquipmentSlot.MainHand);
            var ar = eq.GetEquipped(EquipmentSlot.Armor);
            var sh = eq.GetEquipped(EquipmentSlot.OffHand);

            equippedMainHandId = mh != null ? mh.instanceId : null;
            equippedArmorId = ar != null ? ar.instanceId : null;
            equippedShieldId = sh != null ? sh.instanceId : null;

#if UNITY_EDITOR
            Debug.Log($"[CharacterInventory] HandleEquipmentChangedFromEquipment: 更新持久化装备 ID -> 主手: {equippedMainHandId}, 护甲: {equippedArmorId}, 盾牌: {equippedShieldId}");
#endif
        }

        private void RestoreEquippedFromSavedIds(CharacterEquipment eq)
        {
            // Restore equipped items based on saved instance IDs
            ItemInstance toEquipMH = null;
            ItemInstance toEquipAR = null;
            ItemInstance toEquipSH = null;

            if (!string.IsNullOrEmpty(equippedMainHandId))
            {
                toEquipMH = items.Find(x => x.instanceId == equippedMainHandId);
                if (toEquipMH == null)
                {
                    Debug.LogWarning($"[CharacterInventory] RestoreEquippedFromSavedIds: 未能找到对应的主手实例 ({equippedMainHandId}) 来恢复装备。");
                }
            }
            if (!string.IsNullOrEmpty(equippedArmorId))
            {
                toEquipAR = items.Find(x => x.instanceId == equippedArmorId);
                if (toEquipAR == null)
                {
                    Debug.LogWarning($"[CharacterInventory] RestoreEquippedFromSavedIds: 未能找到对应的护甲实例 ({equippedArmorId}) 来恢复装备。");
                }
            }
            if (!string.IsNullOrEmpty(equippedShieldId))
            {
                toEquipSH = items.Find(x => x.instanceId == equippedShieldId);
                if (toEquipSH == null)
                {
                    Debug.LogWarning($"[CharacterInventory] RestoreEquippedFromSavedIds: 未能找到对应的盾牌实例 ({equippedShieldId}) 来恢复装备。");
                }
            }

            // Equip the found instances to the corresponding slots
            if (toEquipMH != null) eq.EquipToSlot(EquipmentSlot.MainHand, toEquipMH);
            if (toEquipAR != null) eq.EquipToSlot(EquipmentSlot.Armor, toEquipAR);
            if (toEquipSH != null) eq.EquipToSlot(EquipmentSlot.OffHand, toEquipSH);
        }

        /// <summary>
        /// Public: restore equipped slots from the saved instance ID fields. Safe to call multiple times.
        /// </summary>
        public void RestoreSavedEquipment()
        {
            var eq = GetComponent<CharacterEquipment>()
                     ?? GetComponentInParent<CharacterEquipment>()
                     ?? GetComponentInChildren<CharacterEquipment>(true);
            if (eq == null) return;
            RestoreEquippedFromSavedIds(eq);
        }
    }
}
