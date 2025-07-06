using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简单的状态效果图标预制体
/// 用于当没有专门的StatusEffectIcon组件时显示状态效果
/// </summary>
public class SimpleStatusIcon : MonoBehaviour {
    [Header("UI组件")]
    public Text iconText;
    public Image backgroundImage;

    [Header("默认颜色")]
    public Color debuffColor = Color.red;
    public Color buffColor = Color.green;
    public Color neutralColor = Color.yellow;

    /// <summary>
    /// 设置图标文本和颜色
    /// </summary>
    public void SetStatus(string text, bool isDebuff = true) {
        if (iconText != null) {
            iconText.text = text;
        }

        if (backgroundImage != null) {
            backgroundImage.color = isDebuff ? debuffColor : buffColor;
        }
    }

    /// <summary>
    /// 设置图标文本、颜色和提示
    /// </summary>
    public void SetStatus(string text, Color color, string tooltip = "") {
        if (iconText != null) {
            iconText.text = text;
        }

        if (backgroundImage != null) {
            backgroundImage.color = color;
        }

        // 可以在这里添加Tooltip组件的设置
        // 例如：GetComponent<TooltipTrigger>()?.SetTooltip(tooltip);
    }
}
