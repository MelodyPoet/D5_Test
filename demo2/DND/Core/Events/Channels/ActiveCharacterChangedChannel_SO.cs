using UnityEngine;
using demo2.DND.Core.Events;
using demo2.DND;

namespace demo2.DND.Core.Events.Channels
{
    /// <summary>
    /// 当前角色切换 事件通道（管理器 -> 视图/控制器）。
    /// </summary>
    [CreateAssetMenu(fileName = "ActiveCharacterChangedChannel", menuName = "DND/Events/Active Character Changed Channel")]
    public class ActiveCharacterChangedChannel_SO : EventChannelSO<CharacterStats> { }
}

