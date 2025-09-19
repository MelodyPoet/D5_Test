using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 事件频道管理器 - 单例模式
    /// 管理游戏中的各种事件频道
    /// </summary>
    public class EventChannelManager : MonoBehaviour
    {
        private static EventChannelManager _instance;
        public static EventChannelManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EventChannelManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("EventChannelManager");
                        _instance = go.AddComponent<EventChannelManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private Dictionary<string, ScriptableObject> eventChannels = new Dictionary<string, ScriptableObject>();

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeEventChannels();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化事件频道
        /// </summary>
        private void InitializeEventChannels()
        {
            // 创建伤害事件频道
            DamageEventChannel_SO damageChannel = ScriptableObject.CreateInstance<DamageEventChannel_SO>();
            eventChannels["DamageEventChannel"] = damageChannel;
        }

        /// <summary>
        /// 获取指定类型的事件频道
        /// </summary>
        public T GetChannel<T>(string channelName) where T : ScriptableObject
        {
            if (eventChannels.TryGetValue(channelName, out ScriptableObject channel))
            {
                return channel as T;
            }
            return null;
        }

        /// <summary>
        /// 注册事件频道
        /// </summary>
        public void RegisterChannel<T>(string channelName, T channel) where T : ScriptableObject
        {
            eventChannels[channelName] = channel;
        }
    }
}
