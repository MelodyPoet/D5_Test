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

        [Header("初始道具（会自动尝试装备到对应槽位：武器/护甲/盾牌）")]
        [Tooltip("将初始物品的SO拖入此列表。启动时会创建实例并尝试按类型自动装备：\n- 武器 -> 主手\n- 护甲 -> 护甲位\n- 盾牌 -> 盾牌位\n若槽位已有装备或不满足 CanEquip，将仅放入背包。")]
        [SerializeField] private List<ItemBaseSO> initialItems = new List<ItemBaseSO>();

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
            // 创建初始物品实例并放入背包
            if (initialItems != null && initialItems.Count > 0)
            {
                for (int i = 0; i < initialItems.Count; i++)
                {
                    var so = initialItems[i];
                    if (so == null) continue;
                    var inst = new ItemInstance(so);
                    items.Add(inst);
                }
            }

            // Attempt to restore previous equip choices (by instance IDs) before auto-equip defaults
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

            // 启动时按物品类型自动尝试装备到槽位（不覆盖已存在装备）
            AutoEquipInitialItems();

            // 广播：背包就绪/变更
            if (items.Count > 0) OnInventoryChanged?.Invoke();
            OnAnyInventoryReady?.Invoke(this);

            // 订阅并应用一次装备属性
            OnInventoryChanged += ApplyEquipmentModifiers;
            ApplyEquipmentModifiers();
        }

        private void AutoEquipInitialItems()
        {
            var eq = GetComponent<CharacterEquipment>()
                     ?? GetComponentInParent<CharacterEquipment>()
                     ?? GetComponentInChildren<CharacterEquipment>(true);
            if (eq == null)
            {
                Debug.Log($"[CharacterInventory] AutoEquipInitialItems: 未找到 CharacterEquipment 在 {gameObject.name} 的自身/父/子 中。跳过自动装备。");
                return;
            }

            Debug.Log($"[CharacterInventory] AutoEquipInitialItems: 找到 Equipment on {eq.gameObject.name}. itemsCount={items?.Count ?? 0}");

            // 清理陈旧的装备槽（若槽位引用的实例不在当前背包中，视为过期并卸下）
            var mhInst = eq.GetEquipped(EquipmentSlot.MainHand);
            if (mhInst != null)
            {
                if (!items.Contains(mhInst))
                {
                    Debug.Log($"[CharacterInventory] Detected stale mainHand reference ({(mhInst?.data != null ? mhInst.data.displayName : mhInst?.instanceId ?? "<no-id>")}) on {eq.gameObject.name} - unequipping.");
                    eq.UnequipSlot(EquipmentSlot.MainHand);
                }
            }
            var arInst = eq.GetEquipped(EquipmentSlot.Armor);
            if (arInst != null)
            {
                if (!items.Contains(arInst))
                {
                    Debug.Log($"[CharacterInventory] Detected stale armor reference ({(arInst?.data != null ? arInst.data.displayName : arInst?.instanceId ?? "<no-id>")}) on {eq.gameObject.name} - unequipping.");
                    eq.UnequipSlot(EquipmentSlot.Armor);
                }
            }
            var shInst = eq.GetEquipped(EquipmentSlot.OffHand);
            if (shInst != null)
            {
                if (!items.Contains(shInst))
                {
                    Debug.Log($"[CharacterInventory] Detected stale shield reference ({(shInst?.data != null ? shInst.data.displayName : shInst?.instanceId ?? "<no-id>")}) on {eq.gameObject.name} - unequipping.");
                    eq.UnequipSlot(EquipmentSlot.OffHand);
                }
            }

            // 新增：打印当前装备槽状态，帮助诊断为何未自动装备（使用 null-safe 访问以避免分析器警告）
            string mhNameStr = mhInst != null ? (mhInst.data != null ? mhInst.data.displayName : mhInst.instanceId ?? "<no-id>") : "null";
            string arNameStr = arInst != null ? (arInst.data != null ? arInst.data.displayName : arInst.instanceId ?? "<no-id>") : "null";
            string shNameStr = shInst != null ? (shInst.data != null ? shInst.data.displayName : shInst.instanceId ?? "<no-id>") : "null";
            Debug.Log($"[CharacterInventory] Equipment slots at AutoEquip start: mainHand={mhNameStr}, armor={arNameStr}, shield={shNameStr} on GameObject={eq.gameObject.name}");

            ItemInstance firstWeapon = null;
            ItemInstance firstArmor = null;
            ItemInstance firstShield = null;

            for (int i = 0; i < items.Count; i++)
            {
                var inst = items[i];
                if (inst == null || inst.data == null) continue;
                Debug.Log($"[CharacterInventory] Inspect item[{i}] = {inst.data.displayName} (isWeapon={inst.data.isWeapon}, isArmor={inst.data.isArmor}, isShield={inst.data.isShield})");

                if (firstWeapon == null && inst.data.isWeapon && eq.CanEquip(inst))
                {
                    firstWeapon = inst;
                    Debug.Log($"[CharacterInventory] Candidate firstWeapon = {inst.data.displayName}");
                }
                if (firstArmor == null && inst.data.isArmor && eq.CanEquip(inst))
                {
                    firstArmor = inst;
                    Debug.Log($"[CharacterInventory] Candidate firstArmor = {inst.data.displayName}");
                }
                if (firstShield == null && inst.data.isShield && eq.CanEquip(inst))
                {
                    firstShield = inst;
                    Debug.Log($"[CharacterInventory] Candidate firstShield = {inst.data.displayName}");
                }

                if (firstWeapon != null && firstArmor != null && firstShield != null)
                    break;
            }

            // 若槽位为空则装备；若已有人为预设的起始装备则尊重现状不覆盖
            if (mhInst == null && firstWeapon != null)
            {
                Debug.Log($"[CharacterInventory] Auto-equipping weapon {firstWeapon.data.displayName} to {eq.gameObject.name}");
                eq.EquipToSlot(EquipmentSlot.MainHand, firstWeapon);
            }
            else
            {
                if (firstWeapon == null)
                {
                    Debug.Log("[CharacterInventory] No candidate weapon found to auto-equip.");
                }
                else if (mhInst != null)
                {
                    var mhName = mhInst?.data != null ? mhInst.data.displayName : mhInst?.instanceId ?? "<no-id>";
                    Debug.Log($"[CharacterInventory] Skipping auto-equip weapon because mainHand is already occupied by {mhName} on {eq.gameObject.name}");
                }
            }
            if (arInst == null && firstArmor != null)
            {
                Debug.Log($"[CharacterInventory] Auto-equipping armor {firstArmor.data.displayName} to {eq.gameObject.name}");
                eq.EquipToSlot(EquipmentSlot.Armor, firstArmor);
            }
            else
            {
                if (firstArmor == null)
                {
                    Debug.Log("[CharacterInventory] No candidate armor found to auto-equip.");
                }
                else if (arInst != null)
                {
                    var arName = arInst?.data != null ? arInst.data.displayName : arInst?.instanceId ?? "<no-id>";
                    Debug.Log($"[CharacterInventory] Skipping auto-equip armor because armor slot is already occupied by {arName} on {eq.gameObject.name}");
                }
            }
            if (shInst == null && firstShield != null)
            {
                Debug.Log($"[CharacterInventory] Auto-equipping shield {firstShield.data.displayName} to {eq.gameObject.name}");
                eq.EquipToSlot(EquipmentSlot.OffHand, firstShield);
            }
            else
            {
                if (firstShield == null)
                {
                    Debug.Log("[CharacterInventory] No candidate shield found to auto-equip.");
                }
                else if (shInst != null)
                {
                    var shName = shInst?.data != null ? shInst.data.displayName : shInst?.instanceId ?? "<no-id>";
                    Debug.Log($"[CharacterInventory] Skipping auto-equip shield because shield slot is already occupied by {shName} on {eq.gameObject.name}");
                }
            }
        }

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
