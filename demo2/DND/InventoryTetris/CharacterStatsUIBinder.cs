using UnityEngine;
using UnityEngine.UI;
using System.Text;
using demo2.DND.Stats; // use FinalStatsSnapshot
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

        [Header("扩展属性（手动拖拽） - UnityEngine.UI.Text 或下方 TMP_Text")]
        [Tooltip("实际伤害（当前装备武器或徒手的伤害骰 + 能力修正）")] public Text damageText;
        [Tooltip("耐性/免疫/易伤 + 当前正负状态汇总")] public Text effectsText;
        [Header("扩展属性（手动拖拽） - TextMeshPro 可选")]
#if TMP_PRESENT
        public TMP_Text damageTMP;
        public TMP_Text effectsTMP;
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
                bound.OnStatsChanged -= HandleStatsChanged;
            }

            bound = stats;
            if (bound != null)
            {
                // 订阅血量变化/属性变化以自动刷新 UI
                bound.OnHealthChanged += HandleHealthChanged;
                bound.OnStatsChanged += HandleStatsChanged;
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
                bound.OnStatsChanged -= HandleStatsChanged;
            }
            bound = null;
            ClearTexts();
        }

        private void HandleHealthChanged(int current, int max)
        {
            Refresh();
        }
        private void HandleStatsChanged(FinalStatsSnapshot snap)
        {
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

            var snap = bound.CurrentSnapshot; // struct; 初始 Awake 已计算

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

            // 战斗属性（max 使用快照，current 取运行时当前值）
            int uiMaxHp = snap.maxHitPoints > 0 ? snap.maxHitPoints : bound.MaxHitPoints;
            SetText(hpText, $"HP: {bound.CurrentHitPoints}/{uiMaxHp}");
#if TMP_PRESENT
            SetText(hpTMP, $"HP: {bound.CurrentHitPoints}/{uiMaxHp}");
#endif
            // AC 优先用最终快照；否则使用绑定对象的安全只读属性
            int uiAc = (snap.armorClass > 0 ? snap.armorClass : bound.CurrentArmorClass);
            SetText(acText, $"AC: {uiAc}");
#if TMP_PRESENT
            SetText(acTMP, $"AC: {uiAc}");
#endif

            // 能力值（显示分数与修正）
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
            if (bound.StatusEffects != null && bound.StatusEffects.Count > 0)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < bound.StatusEffects.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(bound.StatusEffects[i].ToString());
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

            // === 新增：实际伤害值与耐性/状态汇总 ===
            UpdateDamageDisplay();
            UpdateEffectsDisplay();
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
            SetText(damageText, string.Empty);
            SetText(effectsText, string.Empty);
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
            SetText(damageTMP, string.Empty);
            SetText(effectsTMP, string.Empty);
#endif
        }

        private bool HasAnyTextAssigned()
        {
            bool any = nameText || classText || levelText || hpText || acText || strText || dexText || conText || intText || wisText || chaText || statusText || damageText || effectsText;
#if TMP_PRESENT
            any = any || nameTMP || classTMP || levelTMP || hpTMP || acTMP || strTMP || dexTMP || conTMP || intTMP || wisTMP || chaTMP || statusTMP || damageTMP || effectsTMP;
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

        private void UpdateDamageDisplay()
        {
            if (damageText == null &&
#if TMP_PRESENT
                damageTMP == null &&
#endif
                !debugLogs) return;
            if (bound == null)
            {
                SetText(damageText, string.Empty);
#if TMP_PRESENT
                SetText(damageTMP, string.Empty);
#endif
                return;
            }
            var eq = bound.GetComponent<CharacterEquipment>()
                     ?? bound.GetComponentInParent<CharacterEquipment>()
                     ?? bound.GetComponentInChildren<CharacterEquipment>(true);

            string dmgStr;
            var mh = eq != null ? eq.GetEquipped(EquipmentSlot.MainHand) : null;
            if (mh != null && mh.data != null && mh.data.isWeapon)
            {
                var d = mh.data;
                var dice = d.weaponDamageDice;
                int abilityMod = 0; string abilityLabel = "";
                switch (d.weaponHitAbilityMode)
                {
                    case PhysicalHitAbilityMode.Strength: abilityMod = bound.StrMod; abilityLabel = "STR"; break;
                    case PhysicalHitAbilityMode.Dexterity: abilityMod = bound.DexMod; abilityLabel = "DEX"; break;
                    case PhysicalHitAbilityMode.BestOfStrDex:
                        abilityMod = bound.StrMod >= bound.DexMod ? bound.StrMod : bound.DexMod;
                        abilityLabel = bound.StrMod >= bound.DexMod ? "STR" : "DEX"; break;
                }
                string modPart = abilityMod != 0 ? (abilityMod > 0 ? $"+{abilityMod}" : abilityMod.ToString()) : "";
                dmgStr = $"{dice.diceCount}d{dice.diceSize}{modPart} ({abilityLabel})";
            }
            else
            {
                // 使用角色模板上的徒手配置（严格使用原始数值，不做 UI 层面的强制修正）
                var tpl = bound.template;
                DiceFormula dice = new DiceFormula { diceCount = 1, diceSize = 4 }; // 模板缺失时的兜底
                PhysicalHitAbilityMode mode = PhysicalHitAbilityMode.Strength;
                if (tpl != null)
                {
                    dice = tpl.unarmedDamageDice; // 不再修改 diceCount / diceSize，下层如需校验请在数据侧处理
                    mode = tpl.unarmedDamageAbilityMode;
                }
                int abilityMod = 0; string abilityLabel = "";
                switch (mode)
                {
                    case PhysicalHitAbilityMode.Strength: abilityMod = bound.StrMod; abilityLabel = "STR"; break;
                    case PhysicalHitAbilityMode.Dexterity: abilityMod = bound.DexMod; abilityLabel = "DEX"; break;
                    case PhysicalHitAbilityMode.BestOfStrDex:
                        abilityMod = bound.StrMod >= bound.DexMod ? bound.StrMod : bound.DexMod;
                        abilityLabel = bound.StrMod >= bound.DexMod ? "STR" : "DEX"; break;
                }
                string modPart = abilityMod != 0 ? (abilityMod > 0 ? $"+{abilityMod}" : abilityMod.ToString()) : "";
                dmgStr = $"{dice.diceCount}d{dice.diceSize}{modPart} (Unarmed {abilityLabel})";
            }
            SetText(damageText, "实际伤害: " + dmgStr);
#if TMP_PRESENT
            SetText(damageTMP, "实际伤害: " + dmgStr);
#endif
        }

        private void UpdateEffectsDisplay()
        {
            if (effectsText == null &&
#if TMP_PRESENT
                effectsTMP == null &&
#endif
                !debugLogs) return;
            if (bound == null)
            {
                SetText(effectsText, string.Empty);
#if TMP_PRESENT
                SetText(effectsTMP, string.Empty);
#endif
                return;
            }

            // 汇总：免疫/抗性/易伤（从模板 + 运行时快照可扩展）+ 当前状态效果
            var tpl = bound.template;
            var sb = new StringBuilder();
            // 免疫
            if (tpl != null && tpl.immunities != null && tpl.immunities.Count > 0)
            {
                sb.Append("免疫:");
                for (int i = 0; i < tpl.immunities.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(tpl.immunities[i]);
                }
                sb.Append("  ");
            }
            // 抗性
            if (tpl != null && tpl.resistances != null && tpl.resistances.Count > 0)
            {
                sb.Append("抗性:");
                for (int i = 0; i < tpl.resistances.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(tpl.resistances[i]);
                }
                sb.Append("  ");
            }
            // 易伤
            if (tpl != null && tpl.vulnerabilities != null && tpl.vulnerabilities.Count > 0)
            {
                sb.Append("易伤:");
                for (int i = 0; i < tpl.vulnerabilities.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(tpl.vulnerabilities[i]);
                }
                sb.Append("  ");
            }
            // 当前状态
            if (bound.StatusEffects != null && bound.StatusEffects.Count > 0)
            {
                sb.Append("状态:");
                for (int i = 0; i < bound.StatusEffects.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(bound.StatusEffects[i]);
                }
            }
            string effectsStr = sb.ToString();
            SetText(effectsText, string.IsNullOrEmpty(effectsStr) ? "" : effectsStr);
#if TMP_PRESENT
            SetText(effectsTMP, string.IsNullOrEmpty(effectsStr) ? "" : effectsStr);
#endif
        }
    }
}
