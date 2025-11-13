using UnityEngine;
using demo2.DND.Core.Events;
using demo2.DND.Core.Events.Data;

namespace demo2.DND.Core.Events.Channels
{
    /// <summary>
    /// 请求装备/卸下 事件通道（UI -> 控制器）。
    /// </summary>
    [CreateAssetMenu(fileName = "RequestEquipItemChannel", menuName = "DND/Events/Request Equip Item Channel")]
    public class RequestEquipItemChannel_SO : EventChannelSO<InventoryEquipRequest> { }
}

