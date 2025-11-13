using demo2.DND.InventoryTetris;

namespace demo2.DND.Core.Events.Data
{
    /// <summary>
    /// 背包添加物品的请求数据：由控制器/掉落系统发送，UI或控制器接收处理。
    /// </summary>
    public struct InventoryAddItemRequest
    {
        /// <summary>目标背包（归属的角色）。</summary>
        public readonly CharacterInventory inventory;
        /// <summary>要添加的物品数据（ScriptableObject）。</summary>
        public readonly ItemBaseSO item;
        /// <summary>数量（默认 1）。</summary>
        public readonly int amount;

        public InventoryAddItemRequest(CharacterInventory inventory, ItemBaseSO item, int amount = 1)
        {
            this.inventory = inventory;
            this.item = item;
            this.amount = amount < 1 ? 1 : amount;
        }
    }
}
