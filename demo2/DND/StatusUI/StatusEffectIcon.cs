using UnityEngine;
using UnityEngine.UI;
using DND5E;

/// <summary>
/// 状态效果图标组件
/// 用于显示单个状态效果的图标和提示信息
/// </summary>
public class StatusEffectIcon : MonoBehaviour {
    [Header("UI组件")]
    public Image iconImage;
    public Text iconText;
    public GameObject tooltipPanel;
    public Text tooltipText;

    [Header("状态效果颜色")]
    public Color debuffColor = Color.red;
    public Color buffColor = Color.green;
    public Color neutralColor = Color.yellow;

    private DND5E.StatusEffectType currentStatusType;

    /// <summary>
    /// 设置状态效果类型并更新显示
    /// </summary>
    public void SetStatusEffect(DND5E.StatusEffectType statusType) {
        currentStatusType = statusType;
        UpdateDisplay();
    }

    /// <summary>
    /// 更新显示
    /// </summary>
    private void UpdateDisplay() {
        // 设置图标文本
        if (iconText != null) {
            iconText.text = GetStatusEffectShortName(currentStatusType);
        }

        // 设置图标颜色
        if (iconImage != null) {
            iconImage.color = GetStatusEffectColor(currentStatusType);
        }

        // 设置提示文本
        if (tooltipText != null) {
            tooltipText.text = GetStatusEffectDescription(currentStatusType);
        }
    }

    /// <summary>
    /// 获取状态效果的简短名称（用于图标显示）
    /// </summary>
    private string GetStatusEffectShortName(DND5E.StatusEffectType statusType) {
        switch (statusType) {
            case DND5E.StatusEffectType.Blinded: return "盲";
            case DND5E.StatusEffectType.Charmed: return "魅";
            case DND5E.StatusEffectType.Deafened: return "聋";
            case DND5E.StatusEffectType.Frightened: return "恐";
            case DND5E.StatusEffectType.Grappled: return "抱";
            case DND5E.StatusEffectType.Incapacitated: return "失";
            case DND5E.StatusEffectType.Invisible: return "隐";
            case DND5E.StatusEffectType.Paralyzed: return "瘫";
            case DND5E.StatusEffectType.Petrified: return "石";
            case DND5E.StatusEffectType.Poisoned: return "毒";
            case DND5E.StatusEffectType.Prone: return "倒";
            case DND5E.StatusEffectType.Restrained: return "束";
            case DND5E.StatusEffectType.Stunned: return "昏";
            case DND5E.StatusEffectType.Unconscious: return "昏";
            case DND5E.StatusEffectType.Dodging: return "防";
            default: return "?";
        }
    }

    /// <summary>
    /// 获取状态效果的颜色
    /// </summary>
    private Color GetStatusEffectColor(DND5E.StatusEffectType statusType) {
        switch (statusType) {
            // Debuff（负面状态）- 红色
            case DND5E.StatusEffectType.Blinded:
            case DND5E.StatusEffectType.Charmed:
            case DND5E.StatusEffectType.Deafened:
            case DND5E.StatusEffectType.Frightened:
            case DND5E.StatusEffectType.Grappled:
            case DND5E.StatusEffectType.Incapacitated:
            case DND5E.StatusEffectType.Paralyzed:
            case DND5E.StatusEffectType.Petrified:
            case DND5E.StatusEffectType.Poisoned:
            case DND5E.StatusEffectType.Prone:
            case DND5E.StatusEffectType.Restrained:
            case DND5E.StatusEffectType.Stunned:
            case DND5E.StatusEffectType.Unconscious:
                return debuffColor;

            // Buff（正面状态）- 绿色
            case DND5E.StatusEffectType.Invisible:
            case DND5E.StatusEffectType.Dodging:
                return buffColor;

            // 中性状态 - 黄色
            default:
                return neutralColor;
        }
    }

    /// <summary>
    /// 获取状态效果的详细描述
    /// </summary>
    private string GetStatusEffectDescription(DND5E.StatusEffectType statusType) {
        switch (statusType) {
            case DND5E.StatusEffectType.Blinded:
                return "目盲：无法看见，攻击检定有劣势，针对该生物的攻击检定有优势";

            case DND5E.StatusEffectType.Charmed:
                return "魅惑：无法攻击魅惑者或以其为目标使用有害能力或魔法效应";

            case DND5E.StatusEffectType.Deafened:
                return "耳聋：无法听见，任何依赖听觉的能力检定自动失败";

            case DND5E.StatusEffectType.Frightened:
                return "恐慌：对恐惧源的能力检定和攻击检定有劣势，且无法自愿靠近恐惧源";

            case DND5E.StatusEffectType.Grappled:
                return "擒抱：速度变为0，且无法获得速度加值";

            case DND5E.StatusEffectType.Incapacitated:
                return "失能：无法进行动作或反应";

            case DND5E.StatusEffectType.Invisible:
                return "隐形：被视为重度遮蔽，攻击检定有优势，针对该生物的攻击检定有劣势";

            case DND5E.StatusEffectType.Paralyzed:
                return "麻痹：失能，无法移动和说话，力量和敏捷豁免自动失败，攻击检定有优势";

            case DND5E.StatusEffectType.Petrified:
                return "石化：被转化为固体无机物质，重量增加十倍，停止老化，失能";

            case DND5E.StatusEffectType.Poisoned:
                return "中毒：攻击检定和能力检定有劣势";

            case DND5E.StatusEffectType.Prone:
                return "倒地：只能爬行移动，攻击检定有劣势，5尺内的近战攻击对其有优势";

            case DND5E.StatusEffectType.Restrained:
                return "束缚：速度变为0，攻击检定有劣势，敏捷豁免有劣势，针对该生物的攻击检定有优势";

            case DND5E.StatusEffectType.Stunned:
                return "昏迷：失能，无法移动，只能说出支离破碎的话语，力量和敏捷豁免自动失败";

            case DND5E.StatusEffectType.Unconscious:
                return "失去意识：失能，无法移动和说话，不知道周围环境，掉落携带的物品，倒地";

            case DND5E.StatusEffectType.Dodging:
                return "防御姿态：护甲等级+2，直到下一回合开始";

            default:
                return "未知状态效果";
        }
    }

    /// <summary>
    /// 显示提示
    /// </summary>
    public void ShowTooltip() {
        if (tooltipPanel != null) {
            tooltipPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏提示
    /// </summary>
    public void HideTooltip() {
        if (tooltipPanel != null) {
            tooltipPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 鼠标进入时显示提示
    /// </summary>
    public void OnPointerEnter() {
        ShowTooltip();
    }

    /// <summary>
    /// 鼠标离开时隐藏提示
    /// </summary>
    public void OnPointerExit() {
        HideTooltip();
    }
}
