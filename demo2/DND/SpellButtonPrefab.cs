using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DND5E;

public class SpellButtonPrefab : MonoBehaviour
{
    // 法术引用
    public Spell spell;

    // UI组件
    public Text spellNameText;
    public Image spellIcon;

    // 初始化法术按钮
    public void Initialize(Spell spell)
    {
        this.spell = spell;

        // 设置法术名称
        if (spellNameText != null)
        {
            spellNameText.text = spell.name;
        }

        // 设置法术图标（如果有）
        // 这里可以根据法术类型或名称设置不同的图标
        if (spellIcon != null)
        {
            // 根据法术学派设置不同颜色
            switch (spell.school)
            {
                case SpellSchool.Evocation:
                    spellIcon.color = new Color(1f, 0.5f, 0f); // 橙色
                    break;
                case SpellSchool.Abjuration:
                    spellIcon.color = new Color(0.5f, 0.5f, 1f); // 蓝色
                    break;
                case SpellSchool.Conjuration:
                    spellIcon.color = new Color(0.7f, 0.3f, 0.7f); // 紫色
                    break;
                case SpellSchool.Divination:
                    spellIcon.color = new Color(0.3f, 0.7f, 0.7f); // 青色
                    break;
                case SpellSchool.Enchantment:
                    spellIcon.color = new Color(1f, 0.5f, 0.5f); // 粉色
                    break;
                case SpellSchool.Illusion:
                    spellIcon.color = new Color(0.7f, 0.7f, 0.7f); // 灰色
                    break;
                case SpellSchool.Necromancy:
                    spellIcon.color = new Color(0.3f, 0.3f, 0.3f); // 深灰色
                    break;
                case SpellSchool.Transmutation:
                    spellIcon.color = new Color(0.5f, 1f, 0.5f); // 绿色
                    break;
                default:
                    spellIcon.color = Color.white;
                    break;
            }
        }
    }

    // 显示法术详细信息（可以在鼠标悬停时调用）
    public void ShowSpellDetails()
    {
        if (spell != null)
        {
            Debug.Log(spell.GetFullDescription());
            // 这里可以显示一个详细信息面板
        }
    }
}
