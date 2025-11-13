using UnityEngine;
using demo2.DND.Core.Events;
using demo2.DND.InventoryTetris;

namespace demo2.DND.Core.Events.Channels
{
    /// <summary>
    /// 背包数据变更 事件通道（控制器 -> 视图）。
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryChangedChannel", menuName = "DND/Events/Inventory Changed Channel")]
    public class InventoryChangedChannel_SO : EventChannelSO<CharacterInventory> { }
}

