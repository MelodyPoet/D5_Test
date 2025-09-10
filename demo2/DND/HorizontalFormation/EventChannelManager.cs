using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 事件通道管理器 - 负责初始化和管理所有事件通道
    /// 确保静态类HorizontalCombatRules能够访问到事件通道
    /// </summary>
    public class EventChannelManager : MonoBehaviour
    {
        [Header("事件通道资产")]
        public DamageEventChannel_SO damageEventChannel; // 拖入伤害事件通道资产

        private void Awake()
        {
            // 在游戏开始时设置静态引用
            InitializeEventChannels();
        }

        /// <summary>
        /// 初始化所有事件通道
        /// </summary>
        private void InitializeEventChannels()
        {
            if (damageEventChannel != null)
            {
                // 设置HorizontalCombatRules的静态事件通道引用
                HorizontalCombatRules.DamageEventChannel = damageEventChannel; // 修正字段名
                Debug.Log("伤害事件通道已初始化");
            }
            else
            {
                Debug.LogError("EventChannelManager: 伤害事件通道未设置！请在Inspector中拖入DamageEventChannel_SO资产");
            }
        }

        private void OnDestroy()
        {
            // 清理静态引用
            HorizontalCombatRules.DamageEventChannel = null; // 修正字段名
        }
    }
}
