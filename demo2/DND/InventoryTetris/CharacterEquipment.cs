using System;
using UnityEngine;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 角色装备栏（最小实现）：仅允许一把主手武器 + 一件护甲 + 一面盾牌。
    /// - 仅“已装备”的物品修饰生效；与 CharacterInventory 的“背包”区分。
    /// - AC/命中/伤害只从本组件的装备槽读取；未装备则按“默认徒手/未着甲”处理，与背包未装备物品无关。
    /// </summary>
    public class CharacterEquipment : MonoBehaviour
    {
        [Header("当前装备（运行时）")]
        [HideInInspector] public ItemInstance mainHand;
        [HideInInspector] public ItemInstance armor;
        [HideInInspector] public ItemInstance shield;

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

        public bool IsEquipped(ItemInstance inst)
        {
            if (inst == null) return false;
            bool res = ReferenceEquals(inst, mainHand) || ReferenceEquals(inst, armor) || ReferenceEquals(inst, shield);
            Debug.Log($"[CharacterEquipment] IsEquipped check -> item={InstanceName(inst)}, mainHand={InstanceName(mainHand)}, armor={InstanceName(armor)}, shield={InstanceName(shield)}, result={res}, componentGameObject={gameObject.name}");
            return res;
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

            if (IsEquipped(inst))
            {
                // 已装备 → 卸下
                if (inst.data.isWeapon && ReferenceEquals(mainHand, inst))
                {
                    RemoveModifiers(inst);
                    bool result = UnequipMainHand();
                    RefreshEquipLabel(inst);
                    return result;
                }
                if (inst.data.isShield && ReferenceEquals(shield, inst))
                {
                    RemoveModifiers(inst);
                    bool result = UnequipShield();
                    RefreshEquipLabel(inst);
                    return result;
                }
                if (inst.data.isArmor && ReferenceEquals(armor, inst))
                {
                    RemoveModifiers(inst);
                    bool result = UnequipArmor();
                    RefreshEquipLabel(inst);
                    return result;
                }
                return false;
            }
            else
            {
                // 未装备 → 装备到对应槽位（替换同槽旧物）
                if (inst.data.isWeapon)
                {
                    bool result = EquipMainHand(inst);
                    if (result) ApplyModifiers(inst);
                    RefreshEquipLabel(inst);
                    return result;
                }
                if (inst.data.isShield)
                {
                    bool result = EquipShield(inst);
                    if (result) ApplyModifiers(inst);
                    RefreshEquipLabel(inst);
                    return result;
                }
                if (inst.data.isArmor)
                {
                    bool result = EquipArmor(inst);
                    if (result) ApplyModifiers(inst);
                    RefreshEquipLabel(inst);
                    return result;
                }
                return false;
            }
        }

        public bool EquipMainHand(ItemInstance inst)
        {
            if (inst == null || inst.data == null || !inst.data.isWeapon) return false;
            mainHand = inst;
            Debug.Log($"[CharacterEquipment] EquipMainHand -> {inst.data.displayName} on {gameObject.name}");
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
        }

        public bool EquipArmor(ItemInstance inst)
        {
            if (inst == null || inst.data == null || !inst.data.isArmor) return false;
            armor = inst;
            Debug.Log($"[CharacterEquipment] EquipArmor -> {inst.data.displayName} on {gameObject.name}");
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
        }

        public bool EquipShield(ItemInstance inst)
        {
            if (inst == null || inst.data == null || !inst.data.isShield) return false;
            shield = inst;
            Debug.Log($"[CharacterEquipment] EquipShield -> {inst.data.displayName} on {gameObject.name}");
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
        }

        public bool UnequipMainHand()
        {
            if (mainHand == null) return false;
            Debug.Log($"[CharacterEquipment] UnequipMainHand -> {mainHand?.data?.displayName ?? mainHand?.instanceId} on {gameObject.name}");
            mainHand = null;
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
        }

        public bool UnequipArmor()
        {
            if (armor == null) return false;
            Debug.Log($"[CharacterEquipment] UnequipArmor -> {armor?.data?.displayName ?? armor?.instanceId} on {gameObject.name}");
            armor = null;
            ReapplyEquippedModifiers();
            RaiseChanged();
            return true;
        }

        public bool UnequipShield()
        {
            if (shield == null) return false;
            Debug.Log($"[CharacterEquipment] UnequipShield -> {shield?.data?.displayName ?? shield?.instanceId} on {gameObject.name}");
            shield = null;
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
            Debug.Log($"[CharacterEquipment] ReapplyEquippedModifiers called on {gameObject.name}. mainHand={(mainHand?.data?.displayName ?? "null")}, armor={(armor?.data?.displayName ?? "null")}, shield={(shield?.data?.displayName ?? "null")} ");
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
            AddFor(mainHand);
            AddFor(armor);
            AddFor(shield);

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
    }
}
