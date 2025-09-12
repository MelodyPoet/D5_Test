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

            // 立即更新血条显示
            UpdateHealthDisplay();

            // 确保血条可见
            gameObject.SetActive(true);

            Debug.Log($"血条初始化完成 - 角色: {owner.characterName}, 血量: {owner.currentHitPoints}/{owner.maxHitPoints}");
        }

        /// <summary>
        /// Start方法 - 确保血条在游戏开始时正确显示
        /// </summary>
        private void Start()
        {
            // 如果已经有owner，确保显示正确
            if (owner != null)
            {
                UpdateHealthDisplay();
            }
            else
            {
                // 如果没有owner，尝试从父对象或同级对象中查找CharacterStats
                CharacterStats foundStats = GetComponentInParent<CharacterStats>();
                if (foundStats == null)
                {
                    foundStats = GetComponentInChildren<CharacterStats>();
                }

                if (foundStats != null)
                {
                    Initialize(foundStats);
                }
                else
                {
                    Debug.LogWarning($"血条 {gameObject.name} 没有找到关联的CharacterStats组件");
                }
            }
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
            if (owner == null)
            {
                Debug.LogWarning($"血条 {gameObject.name} 的owner为null，无法更新显示");
                return;
            }

            if (nameText != null)
            {
                nameText.text = owner.characterName;
            }

            if (healthSlider != null)
            {
                // 确保maxHitPoints不为0，避免除零错误
                if (owner.maxHitPoints <= 0)
                {
                    Debug.LogError($"角色 {owner.characterName} 的maxHitPoints为 {owner.maxHitPoints}，这是无效值！");
                    healthSlider.value = 0f;
                    return;
                }

                // 计算血量百分比并更新Slider
                float healthPercent = Mathf.Clamp01((float)owner.currentHitPoints / owner.maxHitPoints);
                healthSlider.value = healthPercent;

                Debug.Log($"血条更新 - {owner.characterName}: {owner.currentHitPoints}/{owner.maxHitPoints} = {healthPercent:F2}%");
            }
            else
            {
                Debug.LogWarning($"血条 {gameObject.name} 的healthSlider为null");
            }
        }

        /// <summary>
        /// 手动刷新血条显示 - 供外部调用
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateHealthDisplay();
        }
    }
}
