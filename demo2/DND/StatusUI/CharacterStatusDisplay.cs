using UnityEngine;
using UnityEngine.UI;
using DND5E;

/// <summary>
/// 角色状态显示UI组件
/// 用于显示角色的血量、法力值、状态效果等信息
/// </summary>
public class CharacterStatusDisplay : MonoBehaviour {
    [Header("基础信息")]
    public Text characterNameText;
    public Text levelText;

    [Header("血量显示")]
    public Slider healthSlider;
    public Text healthText;
    public Image healthFillImage;

    [Header("法力值显示")]
    public Slider manaSlider;
    public Text manaText;
    public Image manaFillImage;

    [Header("护甲等级显示")]
    public Text acText;

    [Header("状态效果显示")]
    public Transform statusEffectsContainer;
    public GameObject statusEffectIconPrefab;

    [Header("颜色配置")]
    public Color healthColorHigh = Color.green;
    public Color healthColorMid = Color.yellow;
    public Color healthColorLow = Color.red;
    public Color manaColor = Color.blue;

    [Header("背景配置")]
    public Image backgroundImage;
    public Color playerBackgroundColor = new Color(0.2f, 0.5f, 0.8f, 0.8f);
    public Color allyBackgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    public Color enemyBackgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);

    private CharacterStats boundCharacter;

    /// <summary>
    /// 绑定角色并初始化UI显示
    /// </summary>
    public void BindCharacter(CharacterStats character) {
        if (character == null) {
            Debug.LogError("CharacterStatusDisplay: 尝试绑定空角色");
            return;
        }

        boundCharacter = character;
        SetupUI();
        UpdateDisplay();
    }

    /// <summary>
    /// 设置UI基础配置
    /// </summary>
    private void SetupUI() {
        if (boundCharacter == null) return;

        // 设置背景颜色
        if (backgroundImage != null) {
            if (CharacterTypeHelper.IsPlayerCharacter(boundCharacter)) {
                backgroundImage.color = playerBackgroundColor;
            }
            else if (CharacterTypeHelper.IsAllyCharacter(boundCharacter)) {
                backgroundImage.color = allyBackgroundColor;
            }
            else if (CharacterTypeHelper.IsEnemyCharacter(boundCharacter)) {
                backgroundImage.color = enemyBackgroundColor;
            }
        }

        // 设置血量条颜色
        if (healthFillImage != null) {
            healthFillImage.color = healthColorHigh;
        }

        // 设置法力条颜色
        if (manaFillImage != null) {
            manaFillImage.color = manaColor;
        }

        Debug.Log($"CharacterStatusDisplay: 为角色 {boundCharacter.GetDisplayName()} 设置UI");
    }

    /// <summary>
    /// 更新显示内容
    /// </summary>
    public void UpdateDisplay() {
        if (boundCharacter == null) return;

        UpdateBasicInfo();
        UpdateHealth();
        UpdateMana();
        UpdateArmorClass();
        UpdateStatusEffects();
    }

    /// <summary>
    /// 更新基础信息
    /// </summary>
    private void UpdateBasicInfo() {
        if (characterNameText != null) {
            characterNameText.text = boundCharacter.GetDisplayName();
        }

        if (levelText != null) {
            levelText.text = $"等级 {boundCharacter.level}";
        }
    }

    /// <summary>
    /// 更新血量显示
    /// </summary>
    private void UpdateHealth() {
        float healthPercentage = (float)boundCharacter.currentHitPoints / boundCharacter.maxHitPoints;
        healthPercentage = Mathf.Clamp01(healthPercentage);

        // 更新血量条
        if (healthSlider != null) {
            healthSlider.value = healthPercentage;
        }

        // 更新血量文本
        if (healthText != null) {
            healthText.text = $"{boundCharacter.currentHitPoints}/{boundCharacter.maxHitPoints}";
        }

        // 更新血量条颜色
        if (healthFillImage != null) {
            if (healthPercentage > 0.6f) {
                healthFillImage.color = healthColorHigh;
            }
            else if (healthPercentage > 0.3f) {
                healthFillImage.color = healthColorMid;
            }
            else {
                healthFillImage.color = healthColorLow;
            }
        }
    }    /// <summary>
         /// 更新法力值显示
         /// </summary>
    private void UpdateMana() {
        // 检查是否是法术使用者
        DND5E.SpellSystem spellSystem = boundCharacter.GetComponent<DND5E.SpellSystem>();
        if (spellSystem == null || spellSystem.spellList == null || spellSystem.spellList.knownSpells.Count == 0) {
            if (manaSlider != null) manaSlider.gameObject.SetActive(false);
            if (manaText != null) manaText.gameObject.SetActive(false);
            return;
        }

        if (manaSlider != null) manaSlider.gameObject.SetActive(true);
        if (manaText != null) manaText.gameObject.SetActive(true);

        // 计算当前和最大法术位总数
        int currentSpellSlots = 0;
        int maxSpellSlots = 0;

        // 获取最大法术位数量（通过创建临时恢复状态来获取）
        int[] currentSlots = new int[spellSystem.spellList.spellSlots.Length];
        System.Array.Copy(spellSystem.spellList.spellSlots, currentSlots, currentSlots.Length);

        // 临时恢复所有法术位以获取最大值
        spellSystem.spellList.RestoreAllSpellSlots();
        for (int i = 0; i < spellSystem.spellList.spellSlots.Length; i++) {
            maxSpellSlots += spellSystem.spellList.spellSlots[i];
        }

        // 恢复当前法术位状态
        System.Array.Copy(currentSlots, spellSystem.spellList.spellSlots, currentSlots.Length);
        for (int i = 0; i < currentSlots.Length; i++) {
            currentSpellSlots += currentSlots[i];
        }

        // 如果没有法术位，也隐藏法力UI
        if (maxSpellSlots == 0) {
            if (manaSlider != null) manaSlider.gameObject.SetActive(false);
            if (manaText != null) manaText.gameObject.SetActive(false);
            return;
        }

        float manaPercentage = (float)currentSpellSlots / maxSpellSlots;
        manaPercentage = Mathf.Clamp01(manaPercentage);

        // 更新法力条
        if (manaSlider != null) {
            manaSlider.value = manaPercentage;
        }

        // 更新法力文本
        if (manaText != null) {
            manaText.text = $"{currentSpellSlots}/{maxSpellSlots}";
        }
    }

    /// <summary>
    /// 更新护甲等级显示
    /// </summary>
    private void UpdateArmorClass() {
        if (acText != null) {
            acText.text = $"AC: {boundCharacter.armorClass}";
        }
    }

    /// <summary>
    /// 更新状态效果显示
    /// </summary>
    private void UpdateStatusEffects() {
        if (statusEffectsContainer == null) return;

        // 清空现有状态效果图标
        foreach (Transform child in statusEffectsContainer) {
            Destroy(child.gameObject);
        }

        // 为每个状态效果创建图标
        foreach (DND5E.StatusEffectType statusEffect in boundCharacter.statusEffects) {
            CreateStatusEffectIcon(statusEffect);
        }
    }

    /// <summary>
    /// 创建状态效果图标
    /// </summary>
    private void CreateStatusEffectIcon(DND5E.StatusEffectType statusType) {
        if (statusEffectIconPrefab == null) return;

        GameObject iconObj = Instantiate(statusEffectIconPrefab, statusEffectsContainer);
        StatusEffectIcon iconScript = iconObj.GetComponent<StatusEffectIcon>();

        if (iconScript != null) {
            iconScript.SetStatusEffect(statusType);
        }
        else {
            // 如果没有专门的脚本，至少显示文本
            Text iconText = iconObj.GetComponentInChildren<Text>();
            if (iconText != null) {
                iconText.text = GetStatusEffectDisplayName(statusType);
            }
        }
    }

    /// <summary>
    /// 获取状态效果的显示名称
    /// </summary>
    private string GetStatusEffectDisplayName(DND5E.StatusEffectType statusType) {
        switch (statusType) {
            case DND5E.StatusEffectType.Blinded: return "目盲";
            case DND5E.StatusEffectType.Charmed: return "魅惑";
            case DND5E.StatusEffectType.Deafened: return "耳聋";
            case DND5E.StatusEffectType.Frightened: return "恐慌";
            case DND5E.StatusEffectType.Grappled: return "擒抱";
            case DND5E.StatusEffectType.Incapacitated: return "失能";
            case DND5E.StatusEffectType.Invisible: return "隐形";
            case DND5E.StatusEffectType.Paralyzed: return "麻痹";
            case DND5E.StatusEffectType.Petrified: return "石化";
            case DND5E.StatusEffectType.Poisoned: return "中毒";
            case DND5E.StatusEffectType.Prone: return "倒地";
            case DND5E.StatusEffectType.Restrained: return "束缚";
            case DND5E.StatusEffectType.Stunned: return "昏迷";
            case DND5E.StatusEffectType.Unconscious: return "失去意识";
            case DND5E.StatusEffectType.Dodging: return "防御姿态";
            default: return statusType.ToString();
        }
    }

    /// <summary>
    /// 外部调用的更新方法
    /// </summary>
    public void RefreshDisplay() {
        UpdateDisplay();
    }

    /// <summary>
    /// 获取绑定的角色
    /// </summary>
    public CharacterStats GetBoundCharacter() {
        return boundCharacter;
    }
}
