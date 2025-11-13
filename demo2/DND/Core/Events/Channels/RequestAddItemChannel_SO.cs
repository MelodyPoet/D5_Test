using UnityEngine;
using demo2.DND.Core.Events.Data; // 引用 InventoryAddItemRequest 结构体（定义在 Data 文件中）

namespace demo2.DND.Core.Events.Channels
{
    /// <summary>
    /// 背包添加物品请求事件通道：InventoryController/掉落系统 -> InventoryUIBinder。
    /// </summary>
    [CreateAssetMenu(fileName = "RequestAddItemChannel", menuName = "DND/Events/Request Add Item Channel")]
    public class RequestAddItemChannel_SO : EventChannelSO<InventoryAddItemRequest> { }
}
