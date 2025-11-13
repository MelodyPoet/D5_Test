using UnityEngine;
using System;

namespace demo2.DND.Core.Events
{
    /// <summary>
    /// 通用的、无参数的事件通道。用于广播不需要携带数据的简单信号。
    /// </summary>
    [CreateAssetMenu(menuName = "Game Events/Event Channel (Void)")]
    public class EventChannelSO : ScriptableObject
    {
        public event Action OnEventRaised;

        public void RaiseEvent()
        {
            OnEventRaised?.Invoke();
        }
    }

    /// <summary>
    /// 通用的、可传递一种参数的事件通道的基类。
    /// </summary>
    /// <typeparam name="T">要传递的数据类型</typeparam>
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        public event Action<T> OnEventRaised;

        public void RaiseEvent(T value)
        {
            OnEventRaised?.Invoke(value);
        }
    }
}
