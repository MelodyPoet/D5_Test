using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色属性组件 - 简化版本，使用CharacterTemplate作为数据源
/// </summary>
public class CharacterStats : MonoBehaviour {
    [Header("角色模板")]
    public CharacterTemplate template;

    [Header("运行时数据")]
    public string characterName = "角色";
    public CharacterClass characterClass = CharacterClass.Fighter;
    public int characterLevel = 1;
    public BattleSide battleSide = BattleSide.Player;

    [Header("当前状态")]
    public int maxHitPoints = 10;
    public int currentHitPoints = 10;
    public int temporaryHitPoints = 0;
    public int armorClass = 10;

    [Header("状态效果")]
    public List<StatusEffectType> statusEffects = new List<StatusEffectType>();

    // 从模板初始化时的属性值
    [HideInInspector] public int strength = 10;
    [HideInInspector] public int dexterity = 10;
    [HideInInspector] public int constitution = 10;
    [HideInInspector] public int intelligence = 10;
    [HideInInspector] public int wisdom = 10;
    [HideInInspector] public int charisma = 10;

    // 属性调整值
    public int StrMod => (strength - 10) / 2;
    public int DexMod => (dexterity - 10) / 2;
    public int ConMod => (constitution - 10) / 2;
    public int IntMod => (intelligence - 10) / 2;
    public int WisMod => (wisdom - 10) / 2;
    public int ChaMod => (charisma - 10) / 2;

    void Start() {
        // 如果有模板，从模板初始化
        if (template != null) {
            InitializeFromTemplate();
        }
    }

    /// <summary>
    /// 从模板初始化角色数据
    /// </summary>
    public void InitializeFromTemplate() {
        if (template == null) return;

        // 复制基本信息
        characterName = template.characterName;
        characterClass = template.characterClass;
        characterLevel = template.level;
        battleSide = template.defaultSide;

        // 复制属性
        strength = template.strength;
        dexterity = template.dexterity;
        constitution = template.constitution;
        intelligence = template.intelligence;
        wisdom = template.wisdom;
        charisma = template.charisma;

        // 计算战斗属性
        maxHitPoints = template.CalculateHitPoints();
        currentHitPoints = maxHitPoints;
        armorClass = template.baseArmorClass;

        Debug.Log($"✅ {characterName} 从模板初始化完成 - 等级{characterLevel} - 血量{maxHitPoints} - AC{armorClass}");
    }

    /// <summary>
    /// 获取显示名称
    /// </summary>
    public string GetDisplayName() {
        return !string.IsNullOrEmpty(characterName) ? characterName : "未命名角色";
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage, DamageType damageType = DamageType.Bludgeoning) {
        if (template != null) {
            // 检查免疫
            if (template.immunities.Contains(damageType)) {
                Debug.Log($"{GetDisplayName()} 免疫 {damageType} 伤害!");
                return;
            }

            // 检查抗性和弱点
            if (template.resistances.Contains(damageType)) {
                damage = Mathf.Max(1, damage / 2);
                Debug.Log($"{GetDisplayName()} 对 {damageType} 伤害有抗性!");
            }
            else if (template.vulnerabilities.Contains(damageType)) {
                damage *= 2;
                Debug.Log($"{GetDisplayName()} 对 {damageType} 伤害有弱点!");
            }
        }

        // 先扣除临时生命值
        if (temporaryHitPoints > 0) {
            if (temporaryHitPoints >= damage) {
                temporaryHitPoints -= damage;
                damage = 0;
            }
            else {
                damage -= temporaryHitPoints;
                temporaryHitPoints = 0;
            }
        }

        // 扣除实际生命值
        currentHitPoints = Mathf.Max(0, currentHitPoints - damage);

        Debug.Log($"{GetDisplayName()} 受到 {damage} 点 {damageType} 伤害! 剩余生命值: {currentHitPoints}/{maxHitPoints}");

        // 检查是否失去意识
        if (currentHitPoints <= 0) {
            AddStatusEffect(StatusEffectType.Unconscious);
            Debug.Log($"{GetDisplayName()} 失去意识!");
        }
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    public void HealDamage(int amount) {
        currentHitPoints = Mathf.Min(maxHitPoints, currentHitPoints + amount);
        Debug.Log($"{GetDisplayName()} 恢复 {amount} 点生命值! 当前生命值: {currentHitPoints}/{maxHitPoints}");

        // 如果恢复意识
        if (currentHitPoints > 0 && HasStatusEffect(StatusEffectType.Unconscious)) {
            RemoveStatusEffect(StatusEffectType.Unconscious);
            Debug.Log($"{GetDisplayName()} 恢复意识!");
        }
    }

    /// <summary>
    /// 检查是否���有特定状态效果
    /// </summary>
    public bool HasStatusEffect(StatusEffectType type) {
        return statusEffects.Contains(type);
    }

    /// <summary>
    /// 添加状态效果
    /// </summary>
    public void AddStatusEffect(StatusEffectType type) {
        if (!statusEffects.Contains(type)) {
            statusEffects.Add(type);

            // 如果是闪避状态，更新AC
            if (type == StatusEffectType.Dodging) {
                UpdateArmorClass();
            }
        }
    }

    /// <summary>
    /// 移除状态效果
    /// </summary>
    public void RemoveStatusEffect(StatusEffectType type) {
        bool removed = statusEffects.Remove(type);

        // 如果移除了闪避状态，更新AC
        if (removed && type == StatusEffectType.Dodging) {
            UpdateArmorClass();
        }
    }

    /// <summary>
    /// 更新护甲等级
    /// </summary>
    public void UpdateArmorClass() {
        // 从基础AC开始
        int baseAc = template != null ? template.baseArmorClass : 10;
        armorClass = baseAc;

        // 应用状态效果的修正
        if (HasStatusEffect(StatusEffectType.Dodging)) {
            armorClass += 2; // 防御姿态提供+2 AC
            Debug.Log($"{GetDisplayName()} 处于防御姿态，AC+2，当前AC: {armorClass}");
        }
    }

    /// <summary>
    /// 进行技能检定
    /// </summary>
    public int SkillCheck(Skill skill) {
        if (template == null) {
            Debug.LogWarning($"{GetDisplayName()} 没有角色模板，无法进行技能检定");
            return Random.Range(1, 21);
        }

        int bonus = template.GetSkillBonus(skill);
        int roll = Random.Range(1, 21);
        int total = roll + bonus;

        Debug.Log($"{GetDisplayName()} 进行 {skill} 检定: 掷骰 {roll} + 加值 {bonus} = {total}");
        return total;
    }

    /// <summary>
    /// 进行豁免检定
    /// </summary>
    public int SavingThrow(string ability) {
        if (template == null) {
            Debug.LogWarning($"{GetDisplayName()} 没有角色模板，无法进行豁免检定");
            return Random.Range(1, 21);
        }

        int bonus = template.GetSavingThrowBonus(ability);
        int roll = Random.Range(1, 21);
        int total = roll + bonus;

        Debug.Log($"{GetDisplayName()} 进行 {ability} 豁免检定: 掷骰 {roll} + 加值 {bonus} = {total}");
        return total;
    }
}
