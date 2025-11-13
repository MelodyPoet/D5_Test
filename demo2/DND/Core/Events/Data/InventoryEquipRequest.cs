using demo2.DND.InventoryTetris;

namespace demo2.DND.Core.Events.Data
{
    /// <summary>
    /// 背包装备请求的动作类型。
    /// </summary>
    public enum InventoryEquipAction
    {
        Toggle = 0,
        Equip = 1,
        Unequip = 2
    }

    /// <summary>
    /// UI 或其他系统发起的装备/卸下请求数据。
    /// </summary>
    public struct InventoryEquipRequest
    {
        public readonly CharacterInventory inventory; // 目标背包（归属角色）
        public readonly ItemInstance item;            // 目标物品实例
        public readonly InventoryEquipAction action;  // 动作类型

        public InventoryEquipRequest(CharacterInventory inventory, ItemInstance item, InventoryEquipAction action = InventoryEquipAction.Toggle)
        {
            this.inventory = inventory;
            this.item = item;
            this.action = action;
        }
    }
}

