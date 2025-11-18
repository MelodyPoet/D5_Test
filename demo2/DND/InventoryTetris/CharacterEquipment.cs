using System;
using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND.InventoryTetris
{
#pragma warning disable 618 // allow using obsolete compatibility properties inside this file without noisy warnings
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

        // 为兼容旧代码，保留原小写公开成员名作为 Obsolete 包装，鼓励使用 PascalCase 属性
        [Obsolete("Use MainHand/Armor/Shield or GetEquipped/EquipToSlot/UneqiupSlot instead.")]
        public ItemInstance mainHand { get => GetSlot(EquipmentSlot.MainHand); set => SetSlot(EquipmentSlot.MainHand, value); }
        [Obsolete("Use MainHand/Armor/Shield or GetEquipped/EquipToSlot/UneqiupSlot instead.")]
        public ItemInstance armor { get => GetSlot(EquipmentSlot.Armor); set => SetSlot(EquipmentSlot.Armor, value); }
        [Obsolete("Use MainHand/Armor/Shield or GetEquipped/EquipToSlot/UneqiupSlot instead.")]
        public ItemInstance shield { get => GetSlot(EquipmentSlot.OffHand); set => SetSlot(EquipmentSlot.OffHand, value); }

        // 推荐的 PascalCase 属性（符合代码风格）
        public ItemInstance MainHand { get => GetSlot(EquipmentSlot.MainHand); set => SetSlot(EquipmentSlot.MainHand, value); }
        public ItemInstance Armor { get => GetSlot(EquipmentSlot.Armor); set => SetSlot(EquipmentSlot.Armor, value); }
        public ItemInstance Shield { get => GetSlot(EquipmentSlot.OffHand); set => SetSlot(EquipmentSlot.OffHand, value); }

        public event Action OnEquipmentChanged;

        private CharacterStats stats; // 修改字段命名

        private void Awake()
        {
            // 缓存 CharacterStats
            stats = GetComponent<CharacterStats>()
                ?? GetComponentInParent<CharacterStats>()
                ?? GetComponentInChildren<CharacterStats>(true);
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

        public bool IsEquipped(ItemInstance inst)
        {
            if (inst == null) return false;
            // Check dictionary values for the same reference
            foreach (var kv in slotMap)
            {
                if (ReferenceEquals(kv.Value, inst))
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

        public bool ToggleEquip(ItemInstance inst)
        {
            if (inst == null || inst.data == null) return false;
            if (!CanEquip(inst)) return false;

            // If already equipped in any slot, unequip that slot
            foreach (var kv in new List<KeyValuePair<EquipmentSlot, ItemInstance>>(slotMap))
            {
                if (ReferenceEquals(kv.Value, inst))
                {
                    return UnequipSlot(kv.Key);
                }
            }

            // Not equipped: equip to an appropriate slot
            if (inst.data.isWeapon)
            {
                return EquipToSlot(EquipmentSlot.MainHand, inst);
            }
            if (inst.data.isShield)
            {
                return EquipToSlot(EquipmentSlot.OffHand, inst);
            }
            if (inst.data.isArmor)
            {
                return EquipToSlot(EquipmentSlot.Armor, inst);
            }

            // Generic: place into first available non-core slot
            foreach (EquipmentSlot s in Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (s == EquipmentSlot.MainHand || s == EquipmentSlot.Armor || s == EquipmentSlot.OffHand) continue;
                if (!slotMap.ContainsKey(s))
                {
                    return EquipToSlot(s, inst);
                }
            }

            return false;
        }

        [Obsolete("Use EquipToSlot(EquipmentSlot.MainHand, item) instead.")]
        public bool EquipMainHand(ItemInstance inst)
        {
            if (inst == null || inst.data == null || !inst.data.isWeapon) return false;
            MainHand = inst;
            Debug.Log($"[CharacterEquipment] EquipMainHand -> {inst.data.displayName} on {gameObject.name}");
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
        }

        [Obsolete("Use EquipToSlot(EquipmentSlot.Armor, item) instead.")]
        public bool EquipArmor(ItemInstance inst)
        {
            if (inst == null || inst.data == null || !inst.data.isArmor) return false;
            Armor = inst;
            Debug.Log($"[CharacterEquipment] EquipArmor -> {inst.data.displayName} on {gameObject.name}");
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
        }

        [Obsolete("Use EquipToSlot(EquipmentSlot.OffHand, item) instead.")]
        public bool EquipShield(ItemInstance inst)
        {
            if (inst == null || inst.data == null || !inst.data.isShield) return false;
            Shield = inst;
            Debug.Log($"[CharacterEquipment] EquipShield -> {inst.data.displayName} on {gameObject.name}");
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
        }

        [Obsolete("Use UnequipSlot(EquipmentSlot.MainHand) instead.")]
        public bool UnequipMainHand()
        {
            if (MainHand == null) return false;
            Debug.Log($"[CharacterEquipment] UnequipMainHand -> {MainHand?.data?.displayName ?? MainHand?.instanceId} on {gameObject.name}");
            MainHand = null;
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
        }

        [Obsolete("Use UnequipSlot(EquipmentSlot.Armor) instead.")]
        public bool UnequipArmor()
        {
            if (Armor == null) return false;
            Debug.Log($"[CharacterEquipment] UnequipArmor -> {Armor?.data?.displayName ?? Armor?.instanceId} on {gameObject.name}");
            Armor = null;
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
        }

        [Obsolete("Use UnequipSlot(EquipmentSlot.OffHand) instead.")]
        public bool UnequipShield()
        {
            if (Shield == null) return false;
            Debug.Log($"[CharacterEquipment] UnequipShield -> {Shield?.data?.displayName ?? Shield?.instanceId} on {gameObject.name}");
            Shield = null;
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
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

            // Add main slots via properties (these fetch from slotMap)
            AddFor(MainHand);
            AddFor(Armor);
            AddFor(Shield);

            // Add generic slots
            foreach (var kv in slotMap)
            {
                // Skip the main slots already processed
                if (kv.Key == EquipmentSlot.MainHand || kv.Key == EquipmentSlot.Armor || kv.Key == EquipmentSlot.OffHand) continue;
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

        // Public slot-based API for new enum/dictionary model
        public ItemInstance GetEquipped(EquipmentSlot slot)
        {
            return GetSlot(slot);
        }

        /// <summary>
        /// Equip an ItemInstance into the given slot. For MainHand/Armor/Shield this will use the existing
        /// EquipMainHand/EquipArmor/EquipShield methods to preserve validation and modifier application.
        /// For other slots the instance will be placed into the slotMap and modifiers reapplied.
        /// Returns true if equip succeeded.
        /// </summary>
        public bool EquipToSlot(EquipmentSlot slot, ItemInstance inst)
        {
            if (inst == null || inst.data == null) return false;
            switch (slot)
            {
                case EquipmentSlot.MainHand:
                    return EquipMainHand(inst);
                case EquipmentSlot.Armor:
                    return EquipArmor(inst);
                case EquipmentSlot.OffHand:
                    return EquipShield(inst);
                default:
                    // Generic slot: just assign and reapply modifiers
                    SetSlot(slot, inst);
                    ReapplyEquippedModifiers();
                    RaiseChanged();
                    return true;
            }
        }

        /// <summary>
        /// Unequip the given slot. For main slots uses existing Unequip* helpers; for generic slots removes mapping.
        /// Returns true if something was unequipped.
        /// </summary>
        public bool UnequipSlot(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.MainHand:
                    return UnequipMainHand();
                case EquipmentSlot.Armor:
                    return UnequipArmor();
                case EquipmentSlot.OffHand:
                    return UnequipShield();
                default:
                    if (slotMap.ContainsKey(slot))
                    {
                        var inst = slotMap[slot];
                        RemoveModifiers(inst);
                        slotMap.Remove(slot);
                        ReapplyEquippedModifiers();
                        RaiseChanged();
                        RefreshEquipLabel(inst);
                        return true;
                    }
                    return false;
            }
        }
    }
#pragma warning restore 618
}
