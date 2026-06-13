using System;
using System.Collections.Generic;
using UnityEngine;
using demo2.DND;

namespace demo2.DND.InventoryTetris
{
    // (Obsolete APIs removed; fully enum/dictionary-driven equipment model)
     /// <summary>
    /// 角色装备栏（最小实现）：仅允许一把主手武器 + 一件护甲 + 一面盾牌。
    /// - 仅“已装备”的物品修饰生效；与 CharacterInventory 的“背包”区分。
    /// - AC/命中/伤害只从本组件的装备槽读取；未装备则按“默认徒手/未着甲”处理，与背包未装备物品无关。
    /// </summary>
    public class CharacterEquipment : MonoBehaviour
    {
        // 使用集中定义的 EquipmentSlot（在 GameEnums.cs 中定义）

        // 新：字典存储槽位到实例的映射
        private readonly Dictionary<EquipmentSlot, ItemInstance> slotMap = new Dictionary<EquipmentSlot, ItemInstance>();

        // Fully enum/dictionary-driven equipment slots. Use the slot-based API
        // (GetEquipped / EquipToSlot / UnequipSlot) or the PascalCase accessors below.
        public ItemInstance MainHand { get => GetSlot(EquipmentSlot.MainHand); set => SetSlot(EquipmentSlot.MainHand, value); }
        public ItemInstance Armor { get => GetSlot(EquipmentSlot.Armor); set => SetSlot(EquipmentSlot.Armor, value); }
        public ItemInstance Shield { get => GetSlot(EquipmentSlot.OffHand); set => SetSlot(EquipmentSlot.OffHand, value); }

        public event Action OnEquipmentChanged;

        private CharacterStats stats; // 修改字段命名
        private CharacterAppearance appearance; // 外观管理器引用

        private void Awake()
        {
            // 缓存 CharacterStats
            stats = GetComponent<CharacterStats>()
                ?? GetComponentInParent<CharacterStats>()
                ?? GetComponentInChildren<CharacterStats>(true);

            // 缓存 CharacterAppearance（装备变化后需通知外观刷新）
            appearance = GetComponent<CharacterAppearance>()
                ?? GetComponentInParent<CharacterAppearance>()
                ?? GetComponentInChildren<CharacterAppearance>(true);
        }

        // 新增：启动时根据当前槽位立即应用一次修饰，确保开局就生效（非仅战斗）。
        private void Start()
        {
            ReapplyEquippedModifiers();
            RaiseChanged();
        }

        // Helper: safe getter for a slot (returns null if not set)
        private ItemInstance GetSlot(EquipmentSlot slot)
        {
            if (slotMap.TryGetValue(slot, out var inst)) return inst;
            return null;
        }

        // Helper: setter for a slot; setting null removes the slot entry
        private void SetSlot(EquipmentSlot slot, ItemInstance inst)
        {
            if (inst == null)
            {
                if (slotMap.ContainsKey(slot)) slotMap.Remove(slot);
            }
            else
            {
                slotMap[slot] = inst;
            }
            // Note: callers will usually call ReapplyEquippedModifiers/RaiseChanged explicitly; we avoid side-effects here to keep behavior explicit.
        }

        private bool SameInstance(ItemInstance a, ItemInstance b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (string.IsNullOrEmpty(a.instanceId) || string.IsNullOrEmpty(b.instanceId)) return false;
            return a.instanceId == b.instanceId;
        }

        public bool IsEquipped(ItemInstance inst)
        {
            if (inst == null) return false;
            // Check dictionary values for the same reference or same instanceId
            foreach (var kv in slotMap)
            {
                if (SameInstance(kv.Value, inst))
                {
                    Debug.Log($"[CharacterEquipment] IsEquipped -> item={InstanceName(inst)} found in slot={kv.Key} on {gameObject.name}");
                    return true;
                }
            }
            Debug.Log($"[CharacterEquipment] IsEquipped check -> item={InstanceName(inst)} not equipped on {gameObject.name}");
            return false;
        }

        // 返回实例的可显示名称（安全访问，避免 null-conditional 警告）
        private string InstanceName(ItemInstance inst)
        {
            if (inst == null) return "null";
            if (inst.data != null && !string.IsNullOrEmpty(inst.data.displayName)) return inst.data.displayName;
            return inst.instanceId ?? "<no-id>";
        }

        public bool CanEquip(ItemInstance inst)
        {
            if (inst == null || inst.data == null) return false;
            var d = inst.data;
            // 仅物品类别本身可装备
            bool typeOk = d.isWeapon || d.isArmor || d.isShield;
            if (!typeOk) return false;

            // 护甲/盾牌：需要职业/模板熟练
            if (stats != null && stats.template != null)
            {
                if (d.isArmor)
                {
                    if (!stats.template.IsProficientForArmor(d.armorType))
                    {
                        Debug.LogWarning($"[CharacterEquipment] {stats.GetDisplayName()} 未熟练 {d.armorType} 护甲，不能穿戴：{d.displayName}");
                        return false;
                    }
                }
                if (d.isShield)
                {
                    if (!stats.template.IsProficientForShield())
                    {
                        Debug.LogWarning($"[CharacterEquipment] {stats.GetDisplayName()} 未熟练盾牌，不能装备：{d.displayName}");
                        return false;
                    }
                }
            }
            return true;
        }

        private string DumpSlots()
        {
            var entries = new List<string>();
            foreach (var kv in slotMap)
            {
                entries.Add($"{kv.Key}={(kv.Value?.data?.displayName ?? kv.Value?.instanceId ?? "null")}");
            }
            return string.Join(",", entries);
        }

        public bool ToggleEquip(ItemInstance inst)
        {
            if (inst == null || inst.data == null) return false;
            if (!CanEquip(inst)) return false;

            // If already equipped in any slot, unequip that slot
            foreach (var kv in new List<KeyValuePair<EquipmentSlot, ItemInstance>>(slotMap))
            {
                if (SameInstance(kv.Value, inst))
                {
                    Debug.Log($"[CharacterEquipment] ToggleEquip -> Unequip {InstanceName(inst)} from {kv.Key}");
                    bool ok = UnequipSlot(kv.Key);
                    Debug.Log($"[CharacterEquipment] ToggleEquip result={ok} slots=[{DumpSlots()}]");
                    return ok;
                }
            }

            // Not equipped: determine preferred slot by item type, then try to equip there;
            // if preferred slot is occupied or invalid, fall back to the first available slot as defined by the enum.
            EquipmentSlot? preferred = null;
            if (inst.data.isWeapon) preferred = EquipmentSlot.MainHand;
            else if (inst.data.isShield) preferred = EquipmentSlot.OffHand;
            else if (inst.data.isArmor) preferred = EquipmentSlot.Armor;

            if (preferred.HasValue)
            {
                // try preferred first
                if (!slotMap.ContainsKey(preferred.Value))
                {
                    bool ok = EquipToSlot(preferred.Value, inst);
                    Debug.Log($"[CharacterEquipment] ToggleEquip -> Equip {InstanceName(inst)} to {preferred.Value} result={ok} slots=[{DumpSlots()}]");
                    return ok;
                }
            }

            // fallback: first empty slot in enum order
            foreach (EquipmentSlot s in Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (!slotMap.ContainsKey(s))
                {
                    bool ok = EquipToSlot(s, inst);
                    Debug.Log($"[CharacterEquipment] ToggleEquip -> Equip {InstanceName(inst)} to {s} result={ok} slots=[{DumpSlots()}]");
                    return ok;
                }
            }

            Debug.LogWarning($"[CharacterEquipment] ToggleEquip -> no available slot for {InstanceName(inst)}. slots=[{DumpSlots()}]");
            return false;
        }

        /// <summary>
        /// 移除旧的“由装备添加”的修饰，并按当前装备槽重新添加；然后请求重算属性。
        /// </summary>
        public void ReapplyEquippedModifiers()
        {
            if (stats == null) return;
            // 构造当前装备概览字符串用于调试
            var entries = new List<string>();
            foreach (var kv in slotMap)
            {
                entries.Add($"{kv.Key}={(kv.Value?.data?.displayName ?? kv.Value?.instanceId ?? "null")}");
            }
            Debug.Log($"[CharacterEquipment] ReapplyEquippedModifiers called on {gameObject.name}. slots=[{string.Join(",", entries)}]");
            // 移除本组件来源的所有修饰
            stats.RemoveModifiersBySource(this);

            // 仅对“当前已装备”的实例添加修饰
            void AddFor(ItemInstance ii)
            {
                if (ii == null || ii.data == null) return;
                foreach (var mod in ii.data.BuildRuntimeModifiers(this))
                {
                    stats.AddModifier(mod);
                }
            }

            // Add modifiers for every equipped slot driven entirely from slotMap (enum/dictionary-driven)
            foreach (var kv in slotMap)
            {
                AddFor(kv.Value);
            }

            stats.RequestRecalculateStats();
        }

        private void RaiseChanged()
        {
            try { OnEquipmentChanged?.Invoke(); } catch (Exception ex) { Debug.LogError(ex); } // 捕获异常并记录日志
        }

        private void ApplyModifiers(ItemInstance inst)
        {
            if (stats != null && inst.data != null)
            {
                stats.AddModifiersFrom(inst.data, ModifierSource.WhileEquipped); // 确保 ModifierSource 定义正确
            }
        }

        private void RemoveModifiers(ItemInstance inst)
        {
            if (stats != null && inst.data != null)
            {
                stats.RemoveModifiersFrom(inst.data, ModifierSource.WhileEquipped); // 确保 ModifierSource 定义正确
            }
        }

        private void RefreshEquipLabel(ItemInstance inst)
        {
            if (inst == null || inst.view == null) return;
            inst.view.RefreshEquipLabel();
        }

        /// <summary>
        /// 同步外观：将当前装备状态推送给 CharacterAppearance 以触发换装渲染
        /// </summary>
        private void SyncAppearance()
        {
            if (appearance != null)
            {
                appearance.SyncFromEquipment(slotMap);
            }
        }

        // Public slot-based API for new enum/dictionary model
        public ItemInstance GetEquipped(EquipmentSlot slot)
        {
            return GetSlot(slot);
        }

        /// <summary>
        /// Equip an ItemInstance into the given slot.
        /// This is the single, unified API for placing equipment into any slot (driven by the EquipmentSlot enum).
        /// The method places the instance into the internal slot map, then reapplies modifiers and raises change events.
        /// It does not move UI elements; the UI should reflect equipped status via item view labels.
        /// Callers should validate compatibility (e.g. weapon -> MainHand) via CanEquip before invoking if desired.
        /// Returns true if the equip operation succeeded.
        /// </summary>
        public bool EquipToSlot(EquipmentSlot slot, ItemInstance inst)
        {
            if (inst == null || inst.data == null) return false;
            // Unified enum/dictionary-driven equip: place into slotMap and reapply modifiers
            SetSlot(slot, inst);
            ReapplyEquippedModifiers();
            RaiseChanged();
            SyncAppearance();
            Debug.Log($"[CharacterEquipment] EquipToSlot {slot} <- {InstanceName(inst)} slots=[{DumpSlots()}]");
            return true;
        }

        /// <summary>
        /// Unequip the given slot. Removes the mapping for the slot (if any), removes modifiers from the instance,
        /// reapplies modifiers for remaining equipped items and raises change events.
        /// Returns true if something was unequipped.
        /// </summary>
        public bool UnequipSlot(EquipmentSlot slot)
        {
            if (slotMap.ContainsKey(slot))
            {
                var inst = slotMap[slot];
                RemoveModifiers(inst);
                slotMap.Remove(slot);
                ReapplyEquippedModifiers();
                RaiseChanged();
                RefreshEquipLabel(inst);
                SyncAppearance();
                Debug.Log($"[CharacterEquipment] UnequipSlot {slot} removed {InstanceName(inst)} slots=[{DumpSlots()}]");
                return true;
            }
            return false;
        }
    }
}
