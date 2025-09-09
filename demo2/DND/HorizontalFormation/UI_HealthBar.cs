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
        public Text nameText; // 角色名字显示（新增）

        [Header("角色关联")]
        public CharacterStats owner; // 关联的角色

        [Header("事件通道")]
        public DamageEventChannel_SO damageEventChannel; // 拖入伤害事件通道资产

        [Header("调试信息")]
        public bool showDebugInfo = true; // 显示调试信息
        public bool useNameMatching = false; // 使用名字匹配而不是对象引用匹配（调试用）

        private void Start()
        {
            if (showDebugInfo)
            {
                Debug.Log($"=== UI_HealthBar[{gameObject.name}] 启动调试 ===");
                Debug.Log($"healthSlider: {(healthSlider != null ? healthSlider.gameObject.name : "NULL")}");
                Debug.Log($"healthText: {(healthText != null ? healthText.gameObject.name : "NULL")}");
                Debug.Log($"nameText: {(nameText != null ? nameText.gameObject.name : "NULL")}");
                Debug.Log($"owner: {(owner != null ? owner.GetDisplayName() : "NULL")}");
                Debug.Log($"damageEventChannel: {(damageEventChannel != null ? damageEventChannel.name : "NULL")}");
            }

            // 初始化血条显示
            if (owner != null)
            {
                UpdateHealthDisplay();
            }
            else
            {
                if (showDebugInfo) Debug.LogWarning($"UI_HealthBar {gameObject.name}: owner未设置！");
            }

            // 检查事件通道配置
            if (damageEventChannel == null && showDebugInfo)
            {
                Debug.LogWarning($"UI_HealthBar {gameObject.name}: damageEventChannel未设置！");
            }

            // 检查所有UI组件的当前文本内容
            if (showDebugInfo)
            {
                CheckUIComponentsText();
            }
        }

        /// <summary>
        /// 检查UI组件当前的文本内容
        /// </summary>
        private void CheckUIComponentsText()
        {
            if (healthText != null)
            {
                Debug.Log($"healthText当前内容: '{healthText.text}'");
            }

            if (nameText != null)
            {
                Debug.Log($"nameText当前内容: '{nameText.text}'");
            }
            else
            {
                // 如果nameText没有设置，检查是否有其他Text组件被意外修改
                Text[] allTexts = GetComponentsInChildren<Text>();
                Debug.Log($"发现 {allTexts.Length} 个Text组件:");
                for (int i = 0; i < allTexts.Length; i++)
                {
                    Debug.Log($"  Text[{i}] '{allTexts[i].gameObject.name}': '{allTexts[i].text}'");
                }
            }
        }

        private void Update()
        {
            // 移除动态名字更新，保持手动配置的UI文本
            // if (owner != null)
            // {
            //     UpdateNameDisplay();
            // }
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
            if (showDebugInfo)
            {
                Debug.Log($"UI_HealthBar[{gameObject.name}]收到伤害事件: {recipient?.GetDisplayName()} 受到 {dealer?.GetDisplayName()} 的 {damage} 点伤害");
                Debug.Log($"当前关联的owner: {owner?.GetDisplayName() ?? "null"}");

                // 添加详细的对象实例调试信息
                if (recipient != null && owner != null)
                {
                    Debug.Log($"recipient对象: {recipient.gameObject.name} (InstanceID: {recipient.GetInstanceID()})");
                    Debug.Log($"owner对象: {owner.gameObject.name} (InstanceID: {owner.GetInstanceID()})");
                    Debug.Log($"recipient GameObject: {recipient.gameObject.GetInstanceID()}");
                    Debug.Log($"owner GameObject: {owner.gameObject.GetInstanceID()}");
                }

                Debug.Log($"recipient == owner: {recipient == owner}");
            }

            bool isMatched = false;
            CharacterStats actualCharacter = owner; // 默认使用owner

            // 检查匹配条件
            if (recipient == owner)
            {
                isMatched = true;
                actualCharacter = owner;
            }
            else if (useNameMatching && recipient != null && owner != null && recipient.GetDisplayName() == owner.GetDisplayName())
            {
                isMatched = true;
                actualCharacter = recipient; // 使用实际受伤的角色实例
                if (showDebugInfo)
                {
                    Debug.Log($"名字匹配成功！使用受伤角色实例的数据: {recipient.gameObject.name}");
                }
            }

            if (isMatched)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"匹配成功！开始更新UI血条...");
                }

                // 使用协程延迟更新，确保角色血量数据已经同步
                StartCoroutine(DelayedUpdateHealthDisplay(actualCharacter));

                // 可以添加血条闪烁或其他视觉效果
                PlayDamageAnimation();
            }
            else if (showDebugInfo)
            {
                Debug.Log($"不匹配，跳过更新。recipient: {recipient?.GetDisplayName()}, owner: {owner?.GetDisplayName()}");
            }
        }

        /// <summary>
        /// 延迟更新血条显示，确保角色数据已经同步
        /// </summary>
        private System.Collections.IEnumerator DelayedUpdateHealthDisplay(CharacterStats character)
        {
            // 等待一帧，确保所有事件处理完成
            yield return null;

            if (showDebugInfo)
            {
                Debug.Log($"延迟更新前检查角色血量: {character.GetDisplayName()} - {character.currentHitPoints}/{character.maxHitPoints}");
            }

            // 更新血条UI显示，使用正确的角色实例
            UpdateHealthDisplay(character);

            if (showDebugInfo)
            {
                Debug.Log($"UI血条延迟更新完成: {character.GetDisplayName()} 血量 {character.currentHitPoints}/{character.maxHitPoints}");
            }
        }

        /// <summary>
        /// 更新血条显示（重载版本 - 使用指定角色）
        /// </summary>
        private void UpdateHealthDisplay(CharacterStats character)
        {
            if (character == null)
            {
                if (showDebugInfo) Debug.LogError($"UpdateHealthDisplay: 传入的character为null! GameObject: {gameObject.name}");
                return;
            }

            if (showDebugInfo)
            {
                Debug.Log($"开始更新血条显示（使用指定角色） - 角色: {character.GetDisplayName()}, 血量: {character.currentHitPoints}/{character.maxHitPoints}");
            }

            // 更新血条进度
            if (healthSlider != null)
            {
                float healthPercent = (float)character.currentHitPoints / character.maxHitPoints;

                if (showDebugInfo)
                {
                    Debug.Log($"计算血量百分比: {character.currentHitPoints}/{character.maxHitPoints} = {healthPercent:F2}");
                    Debug.Log($"更新前Slider.value: {healthSlider.value:F2}");
                }

                healthSlider.value = healthPercent;

                if (showDebugInfo)
                {
                    Debug.Log($"更新后Slider.value: {healthSlider.value:F2}");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning($"healthSlider为null! 角色: {character.GetDisplayName()}, GameObject: {gameObject.name}");
            }

            // 更新血量文字显示
            if (healthText != null)
            {
                string healthString = $"{character.currentHitPoints}/{character.maxHitPoints}";
                healthText.text = healthString;

                if (showDebugInfo)
                {
                    Debug.Log($"更新血量文字: {healthString}");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning($"healthText为null! 角色: {character.GetDisplayName()}, GameObject: {gameObject.name}");
            }
        }

        /// <summary>
        /// 更新血条显示
        /// </summary>
        private void UpdateHealthDisplay()
        {
            // 调用重载版本
            UpdateHealthDisplay(owner);
        }

        /// <summary>
        /// 更新角色名字显示
        /// </summary>
        private void UpdateNameDisplay()
        {
            if (owner != null && nameText != null)
            {
                string displayName = owner.GetDisplayName();

                if (showDebugInfo && nameText.text != displayName)
                {
                    Debug.Log($"角色名字更新: '{nameText.text}' -> '{displayName}' (GameObject: {gameObject.name})");
                }

                nameText.text = displayName;
            }
            else if (showDebugInfo)
            {
                if (owner == null)
                    Debug.LogWarning($"UpdateNameDisplay: owner为null! GameObject: {gameObject.name}");
                if (nameText == null)
                    Debug.LogWarning($"UpdateNameDisplay: nameText为null! GameObject: {gameObject.name}");
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
