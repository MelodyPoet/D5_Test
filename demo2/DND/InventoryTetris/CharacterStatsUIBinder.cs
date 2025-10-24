using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 角色属性 UI 绑定器：从 CharacterStats 读取并渲染到若干 Text 组件。
    /// 使用方式：
    /// - 在同一 UI 面板上挂载本组件；手动拖拽需要显示的 Text 到对应字段。
    /// - 通过 InventoryUIBinder.statsUIBinder 绑定，切换角色时调用 Bind(stats)/Unbind()。
    /// - 若外部修改了属性，外部可显式调用 Refresh() 以更新显示。
    /// </summary>
    public class CharacterStatsUIBinder : MonoBehaviour, ICharacterStatsUIBinder
    {
        [Header("基础信息（手动拖拽）")]
        public Text nameText;
        public Text classText;
        public Text levelText;

        [Header("战斗属性（手动拖拽）")]
        public Text hpText;   // 显示为 "HP: current/max"
        public Text acText;   // 显示为 "AC: xx"

        [Header("能力值（手动拖拽）")]
        public Text strText;
        public Text dexText;
        public Text conText;
        public Text intText;
        public Text wisText;
        public Text chaText;

        [Header("状态（手动拖拽，以逗号分隔）")]
        public Text statusText;

        private CharacterStats bound;

        public void Bind(CharacterStats stats)
        {
            bound = stats;
            Refresh();
        }

        public void Unbind()
        {
            bound = null;
            ClearTexts();
        }

        public void Refresh()
        {
            if (bound == null)
            {
                ClearTexts();
                return;
            }

            // 基础信息
            if (nameText) nameText.text = bound.characterName;
            if (classText) classText.text = bound.characterClass.ToString();
            if (levelText) levelText.text = $"Lv.{bound.characterLevel}";

            // 战斗属性
            if (hpText) hpText.text = $"HP: {bound.currentHitPoints}/{bound.maxHitPoints}";
            if (acText) acText.text = $"AC: {bound.armorClass}";

            // 能力值（CharacterStats 中公开了原始六维，且提供了 Mod 便捷属性）
            if (strText) strText.text = FormatAbility(bound.strength, bound.StrMod, "STR");
            if (dexText) dexText.text = FormatAbility(bound.dexterity, bound.DexMod, "DEX");
            if (conText) conText.text = FormatAbility(bound.constitution, bound.ConMod, "CON");
            if (intText) intText.text = FormatAbility(bound.intelligence, bound.IntMod, "INT");
            if (wisText) wisText.text = FormatAbility(bound.wisdom, bound.WisMod, "WIS");
            if (chaText) chaText.text = FormatAbility(bound.charisma, bound.ChaMod, "CHA");

            // 状态效果
            if (statusText)
            {
                if (bound.statusEffects != null && bound.statusEffects.Count > 0)
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < bound.statusEffects.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append(bound.statusEffects[i].ToString());
                    }
                    statusText.text = sb.ToString();
                }
                else
                {
                    statusText.text = string.Empty;
                }
            }
        }

        private static string FormatAbility(int score, int mod, string label)
        {
            string sign = mod >= 0 ? "+" : string.Empty;
            return $"{label}: {score} ({sign}{mod})";
        }

        private void ClearTexts()
        {
            if (nameText) nameText.text = string.Empty;
            if (classText) classText.text = string.Empty;
            if (levelText) levelText.text = string.Empty;
            if (hpText) hpText.text = string.Empty;
            if (acText) acText.text = string.Empty;
            if (strText) strText.text = string.Empty;
            if (dexText) dexText.text = string.Empty;
            if (conText) conText.text = string.Empty;
            if (intText) intText.text = string.Empty;
            if (wisText) wisText.text = string.Empty;
            if (chaText) chaText.text = string.Empty;
            if (statusText) statusText.text = string.Empty;
        }
    }
}
