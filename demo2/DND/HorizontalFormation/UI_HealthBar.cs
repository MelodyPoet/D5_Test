using UnityEngine;
using UnityEngine.UI;

namespace demo2.DND.HorizontalFormation
{
    public class UI_HealthBar : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text nameText;

        private CharacterStats owner;

        // 外部只读访问器：用于管理器判断血条是否仍有关联的 owner
        public CharacterStats Owner => owner;

        private void Awake()
        {
            Debug.Log($"[UI_HealthBar] Awake: {gameObject.name}");
            // 如果在 Inspector 中未绑定，则尝试自动查找
            TryAutoBindComponents();
        }

        private void OnEnable()
        {
            Debug.Log($"[UI_HealthBar] OnEnable: {gameObject.name}, activeSelf={gameObject.activeSelf}");
            // 再次确保绑定（运行时 prefab 可能在 Instantiate 后出现）
            TryAutoBindComponents();
        }

        private void OnDisable()
        {
            // 取消订阅 owner 的事件，防止内存泄露
            UnsubscribeFromOwner();
        }

        private void OnDestroy()
        {
            // 确保取消订阅
            UnsubscribeFromOwner();
        }

        private void TryAutoBindComponents()
        {
            if (healthSlider == null)
            {
                healthSlider = GetComponentInChildren<Slider>();
                if (healthSlider != null)
                    Debug.Log($"[UI_HealthBar] 自动绑定 healthSlider: {healthSlider.name} for {gameObject.name}");
                else
                    Debug.LogWarning($"[UI_HealthBar] healthSlider 未在 {gameObject.name} 找到，请在 prefab Inspector 中绑定 Slider。");
            }

            if (nameText == null)
            {
                // 优先查找名为 "Name" 的 Text
                Text[] texts = GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.name.ToLower().Contains("name"))
                    {
                        nameText = t;
                        break;
                    }
                }
                if (nameText == null && texts.Length > 0)
                {
                    nameText = texts[0];
                }

                if (nameText != null)
                    Debug.Log($"[UI_HealthBar] 自动绑定 nameText: {nameText.name} for {gameObject.name}");
                else
                    Debug.LogWarning($"[UI_HealthBar] nameText 未在 {gameObject.name} 找到，请在 prefab Inspector 中绑定 Text。");
            }
        }

        private void UnsubscribeFromOwner()
        {
            if (owner != null)
            {
                try
                {
                    owner.OnHealthChanged -= OnOwnerHealthChanged;
                }
                catch (System.Exception)
                {
                    // 忽略已移除或其它异常
                }
            }
        }

        /// <summary>
        /// 设置角色信息（初始化时调用）
        /// </summary>
        /// <param name="characterStats">该血条所属的角色</param>
        public void SetOwner(CharacterStats characterStats)
        {
            Debug.Log($"[UI_HealthBar] SetOwner called for {gameObject.name} with characterStats={(characterStats != null ? characterStats.characterName : "null")}");

            // 取消订阅之前的 owner（如果存在）
            UnsubscribeFromOwner();

            owner = characterStats;
            if (owner == null)
            {
                Debug.LogError("SetOwner被调用，但传入的characterStats为null！");
                gameObject.SetActive(false); // 如果没有owner，则隐藏血条
                return;
            }
            Debug.Log($"[UI_HealthBar] SetOwner: owner instanceID={owner.GetInstanceID()} for {gameObject.name}");
            gameObject.name = $"HealthBar_{owner.characterName}";

            TryAutoBindComponents();

            if (nameText != null)
            {
                nameText.text = owner.characterName;
            }

            // 订阅角色本地血量变更事件，优先使用本地事件驱动刷新
            try
            {
                owner.OnHealthChanged += OnOwnerHealthChanged;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UI_HealthBar] 订阅 owner.OnHealthChanged 时异常: {ex}");
            }

            // 确保 Slider 的范围与角色血量一致并立即更新显示
            UpdateHealthDisplay();

            // 确保血条可见
            gameObject.SetActive(true);

            Debug.Log($"血条初始化完成 - 角色: {owner.characterName}, 血量: {owner.currentHitPoints}/{owner.maxHitPoints}");
        }

        /// <summary>
        /// 刷新血量显示（受击后调用）
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateHealthDisplay();
        }

        private void OnOwnerHealthChanged(int currentHp, int maxHp)
        {
            if (healthSlider == null)
            {
                TryAutoBindComponents();
            }

            if (healthSlider != null)
            {
                int maxVal = Mathf.Max(1, maxHp);
                if (!Mathf.Approximately(healthSlider.maxValue, maxVal))
                {
                    healthSlider.maxValue = maxVal;
                }

                healthSlider.value = Mathf.Clamp(currentHp, 0, maxVal);

                Debug.Log($"[UI_HealthBar] OnOwnerHealthChanged: {owner?.characterName} {currentHp}/{maxHp} -> slider={healthSlider.value} (max={healthSlider.maxValue})");
            }
            else
            {
                Debug.LogWarning($"[UI_HealthBar] 在 OnOwnerHealthChanged 时未找到 healthSlider: {gameObject.name}");
            }
        }

        private void UpdateHealthDisplay()
        {
            if (owner == null)
            {
                Debug.LogWarning($"血条 {gameObject.name} 的owner为null，无法更新显示");
                return;
            }

            if (healthSlider != null)
            {
                // 确保 maxHitPoints 合法并同步到 Slider
                int maxHp = Mathf.Max(1, owner.maxHitPoints);
                if (!Mathf.Approximately(healthSlider.maxValue, maxHp))
                {
                    healthSlider.maxValue = maxHp;
                }

                // 直接使用 currentHitPoints 作为 slider 的 value，避免与 prefab 的 maxValue 不一致导致的显示异常
                float clampedHp = Mathf.Clamp(owner.currentHitPoints, 0, maxHp);
                healthSlider.value = clampedHp;

                Debug.Log($"[UI_HealthBar] UpdateHealthDisplay: {owner.characterName} {owner.currentHitPoints}/{owner.maxHitPoints} healthSlider.value={healthSlider.value} (max={healthSlider.maxValue})");
            }
            else
            {
                Debug.LogWarning($"血条 {gameObject.name} 的healthSlider为null");
            }
        }
    }
}
