using UnityEngine;
using UnityEngine.UI;

namespace demo2.DND.HorizontalFormation
{
    public class UI_HealthBar : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private DamageEventChannel_SO damageEventChannel;

        [Header("UI Components")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text nameText; // 使用普通的Text组件

        private CharacterStats owner;

        private void OnEnable()
        {
            if (damageEventChannel != null)
            {
                damageEventChannel.OnEventRaised += HandleDamageEvent;
            }
        }

        private void OnDisable()
        {
            if (damageEventChannel != null)
            {
                damageEventChannel.OnEventRaised -= HandleDamageEvent;
            }
        }

        /// <summary>
        /// 初始化血条，将其与一个角色实例关联
        /// </summary>
        /// <param name="characterStats">该血条所属的角色</param>
        public void Initialize(CharacterStats characterStats)
        {
            owner = characterStats;
            // 确保owner不为null
            if (owner == null)
            {
                Debug.LogError("Initialize被调用，但传入的characterStats为null！");
                gameObject.SetActive(false); // 如果没有owner，则隐藏血条
                return;
            }

            gameObject.name = $"HealthBar_{owner.characterName}";
            UpdateHealthDisplay();
        }

        /// <summary>
        /// 处理伤害事件，仅当受伤者是此血条的拥有者时才更新
        /// </summary>
        private void HandleDamageEvent(CharacterStats recipient, CharacterStats dealer, int damageAmount)
        {
            // 直接比较CharacterStats实例引用，确保是同一个对象
            if (recipient == owner)
            {
                UpdateHealthDisplay();
            }
        }

        /// <summary>
        /// 根据owner的当前状态更新UI显示
        /// </summary>
        private void UpdateHealthDisplay()
        {
            if (owner == null) return;

            if (nameText != null)
            {
                nameText.text = owner.characterName;
            }

            if (healthSlider != null)
            {
                // 计算血量百分比并更新Slider
                float healthPercent = (float)owner.currentHitPoints / owner.maxHitPoints;
                healthSlider.value = healthPercent;
            }
        }
    }
}
