using UnityEngine;
using UnityEngine.UI;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// UI血条组件 - 监听伤害事件并更新血条显示
    /// 作为伤害事件的订阅者，展示事件通道的解耦效果
    /// </summary>
    public class UI_HealthBar : MonoBehaviour
    {
        [Header("UI组件")]
        public Slider healthSlider; // 血条滑动条
        public Text healthText; // 血量文字显示

        [Header("角色关联")]
        public CharacterStats owner; // 关联的角色

        [Header("事件通道")]
        public DamageEventChannel_SO damageEventChannel; // 拖入伤害事件通道资产

        private void Start()
        {
            // 初始化血条显示
            if (owner != null)
            {
                UpdateHealthDisplay();
            }
        }

        private void OnEnable()
        {
            // 订阅伤害事件
            if (damageEventChannel != null)
            {
                damageEventChannel.OnEventRaised += HandleDamageEvent;
            }
        }

        private void OnDisable()
        {
            // 取消订阅伤害事件
            if (damageEventChannel != null)
            {
                damageEventChannel.OnEventRaised -= HandleDamageEvent;
            }
        }

        /// <summary>
        /// 处理伤害事件 - 只有当关联角色受伤时才更新血条
        /// </summary>
        private void HandleDamageEvent(CharacterStats recipient, CharacterStats dealer, int damage)
        {
            if (recipient == owner)
            {
                // 更新血条UI显示
                UpdateHealthDisplay();

                // 可以添加血条闪烁或其他视觉效果
                PlayDamageAnimation();

                Debug.Log($"UI血条更新: {owner.GetDisplayName()} 受到 {damage} 点伤害");
            }
        }

        /// <summary>
        /// 更新血条显示
        /// </summary>
        private void UpdateHealthDisplay()
        {
            if (owner == null) return;

            // 更新血条进度
            if (healthSlider != null)
            {
                float healthPercent = (float)owner.currentHitPoints / owner.maxHitPoints;
                healthSlider.value = healthPercent;
            }

            // 更新文字显示
            if (healthText != null)
            {
                healthText.text = $"{owner.currentHitPoints}/{owner.maxHitPoints}";
            }
        }

        /// <summary>
        /// 播放受伤动画效果
        /// </summary>
        private void PlayDamageAnimation()
        {
            // 这里可以添加血条闪烁、颜色变化等效果
            // 例如：血条变红然后恢复正常颜色
            if (healthSlider != null && healthSlider.fillRect != null)
            {
                Image fillImage = healthSlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    StartCoroutine(FlashHealthBar(fillImage));
                }
            }
        }

        /// <summary>
        /// 血条闪烁效果协程
        /// </summary>
        private System.Collections.IEnumerator FlashHealthBar(Image fillImage)
        {
            Color originalColor = fillImage.color;
            Color flashColor = Color.red;

            // 闪烁效果
            fillImage.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            fillImage.color = originalColor;
        }

        /// <summary>
        /// 手动设置关联角色（可在Inspector中调用）
        /// </summary>
        public void SetOwner(CharacterStats character)
        {
            owner = character;
            UpdateHealthDisplay();
        }
    }
}
