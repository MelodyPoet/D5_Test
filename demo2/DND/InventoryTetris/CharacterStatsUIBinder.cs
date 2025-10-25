using UnityEngine;
using UnityEngine.UI;
using System.Text;
using demo2.DND; // ensure CharacterStats type is resolved
#if TMP_PRESENT
using TMPro; // optional TMP support when define is set
#endif

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
        [Header("基础信息（手动拖拽） - UnityEngine.UI.Text 或下方 TMP_Text")]
        public Text nameText;
        public Text classText;
        public Text levelText;
        [Header("基础信息（手动拖拽） - TextMeshPro 可选")]
#if TMP_PRESENT
        public TMP_Text nameTMP;
        public TMP_Text classTMP;
        public TMP_Text levelTMP;
#endif

        [Header("战斗属性（手动拖拽） - UnityEngine.UI.Text 或下方 TMP_Text")]
        public Text hpText;   // 显示为 "HP: current/max"
        public Text acText;   // 显示为 "AC: xx"
        [Header("战斗属性（手动拖拽） - TextMeshPro 可选")]
#if TMP_PRESENT
        public TMP_Text hpTMP;
        public TMP_Text acTMP;
#endif

        [Header("能力值（手动拖拽） - UnityEngine.UI.Text 或下方 TMP_Text")]
        public Text strText;
        public Text dexText;
        public Text conText;
        public Text intText;
        public Text wisText;
        public Text chaText;
        [Header("能力值（手动拖拽） - TextMeshPro 可选")]
#if TMP_PRESENT
        public TMP_Text strTMP;
        public TMP_Text dexTMP;
        public TMP_Text conTMP;
        public TMP_Text intTMP;
        public TMP_Text wisTMP;
        public TMP_Text chaTMP;
#endif

        [Header("状态（手动拖拽，以逗号分隔） - UnityEngine.UI.Text 或下方 TMP_Text")]
        public Text statusText;
        [Header("状态（手动拖拽，以逗号分隔） - TextMeshPro 可选")]
#if TMP_PRESENT
        public TMP_Text statusTMP;
#endif

        [Header("调试")]
        public bool debugLogs;

        private CharacterStats bound;

        private void OnDisable()
        {
            // 面板被隐藏/禁用时，安全解绑，避免事件泄漏
            Unbind();
        }

        private void OnDestroy()
        {
            // 对象销毁时确保解绑
            Unbind();
        }

        public void Bind(CharacterStats stats)
        {
            // 先解除旧订阅
            if (bound != null)
            {
                bound.OnHealthChanged -= HandleHealthChanged;
            }

            bound = stats;
            if (bound != null)
            {
                // 订阅血量变化以自动刷新 UI
                bound.OnHealthChanged += HandleHealthChanged;
                if (debugLogs)
                {
                    Debug.Log($"[CharacterStatsUIBinder] 已绑定: {bound.characterName}");
                }
            }
            Refresh();
        }

        public void Unbind()
        {
            if (bound != null)
            {
                bound.OnHealthChanged -= HandleHealthChanged;
            }
            bound = null;
            ClearTexts();
        }

        private void HandleHealthChanged(int current, int max)
        {
            // 仅刷新相关字段，或直接整体 Refresh（简单可靠）
            Refresh();
        }

        public void Refresh()
        {
            if (bound == null)
            {
                if (debugLogs) Debug.LogWarning("[CharacterStatsUIBinder] bound 为空，已清空文本");
                ClearTexts();
                return;
            }

            if (!HasAnyTextAssigned() && debugLogs)
            {
                Debug.LogWarning("[CharacterStatsUIBinder] 所有文本字段均未在 Inspector 中赋值，无法显示文本。");
            }

            // 基础信息
            SetText(nameText, bound.characterName);
#if TMP_PRESENT
            SetText(nameTMP, bound.characterName);
#endif
            SetText(classText, bound.characterClass.ToString());
#if TMP_PRESENT
            SetText(classTMP, bound.characterClass.ToString());
#endif
            SetText(levelText, $"Lv.{bound.characterLevel}");
#if TMP_PRESENT
            SetText(levelTMP, $"Lv.{bound.characterLevel}");
#endif

            // 战斗属性
            SetText(hpText, $"HP: {bound.currentHitPoints}/{bound.maxHitPoints}");
#if TMP_PRESENT
            SetText(hpTMP, $"HP: {bound.currentHitPoints}/{bound.maxHitPoints}");
#endif
            SetText(acText, $"AC: {bound.armorClass}");
#if TMP_PRESENT
            SetText(acTMP, $"AC: {bound.armorClass}");
#endif

            // 能力值
            SetText(strText, FormatAbility(bound.strength, bound.StrMod, "STR"));
            SetText(dexText, FormatAbility(bound.dexterity, bound.DexMod, "DEX"));
            SetText(conText, FormatAbility(bound.constitution, bound.ConMod, "CON"));
            SetText(intText, FormatAbility(bound.intelligence, bound.IntMod, "INT"));
            SetText(wisText, FormatAbility(bound.wisdom, bound.WisMod, "WIS"));
            SetText(chaText, FormatAbility(bound.charisma, bound.ChaMod, "CHA"));
#if TMP_PRESENT
            SetText(strTMP, FormatAbility(bound.strength, bound.StrMod, "STR"));
            SetText(dexTMP, FormatAbility(bound.dexterity, bound.DexMod, "DEX"));
            SetText(conTMP, FormatAbility(bound.constitution, bound.ConMod, "CON"));
            SetText(intTMP, FormatAbility(bound.intelligence, bound.IntMod, "INT"));
            SetText(wisTMP, FormatAbility(bound.wisdom, bound.WisMod, "WIS"));
            SetText(chaTMP, FormatAbility(bound.charisma, bound.ChaMod, "CHA"));
#endif

            // 状态效果
            if (bound.statusEffects != null && bound.statusEffects.Count > 0)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < bound.statusEffects.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(bound.statusEffects[i].ToString());
                }
                SetText(statusText, sb.ToString());
#if TMP_PRESENT
                SetText(statusTMP, sb.ToString());
#endif
            }
            else
            {
                SetText(statusText, string.Empty);
#if TMP_PRESENT
                SetText(statusTMP, string.Empty);
#endif
            }
        }

        private static string FormatAbility(int score, int mod, string label)
        {
            string sign = mod >= 0 ? "+" : string.Empty;
            return $"{label}: {score} ({sign}{mod})";
        }

        private void ClearTexts()
        {
            SetText(nameText, string.Empty);
            SetText(classText, string.Empty);
            SetText(levelText, string.Empty);
            SetText(hpText, string.Empty);
            SetText(acText, string.Empty);
            SetText(strText, string.Empty);
            SetText(dexText, string.Empty);
            SetText(conText, string.Empty);
            SetText(intText, string.Empty);
            SetText(wisText, string.Empty);
            SetText(chaText, string.Empty);
            SetText(statusText, string.Empty);
#if TMP_PRESENT
            SetText(nameTMP, string.Empty);
            SetText(classTMP, string.Empty);
            SetText(levelTMP, string.Empty);
            SetText(hpTMP, string.Empty);
            SetText(acTMP, string.Empty);
            SetText(strTMP, string.Empty);
            SetText(dexTMP, string.Empty);
            SetText(conTMP, string.Empty);
            SetText(intTMP, string.Empty);
            SetText(wisTMP, string.Empty);
            SetText(chaTMP, string.Empty);
            SetText(statusTMP, string.Empty);
#endif
        }

        private bool HasAnyTextAssigned()
        {
            bool any = nameText || classText || levelText || hpText || acText || strText || dexText || conText || intText || wisText || chaText || statusText;
#if TMP_PRESENT
            any = any || nameTMP || classTMP || levelTMP || hpTMP || acTMP || strTMP || dexTMP || conTMP || intTMP || wisTMP || chaTMP || statusTMP;
#endif
            return any;
        }

        private static void SetText(Text ui, string value)
        {
            if (ui != null) ui.text = value;
        }

#if TMP_PRESENT
        private static void SetText(TMP_Text tmp, string value)
        {
            if (tmp != null) tmp.text = value;
        }
#endif
    }
}
