using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 用于传递法术数据的结构体
[System.Serializable]
public class SpellData {
    public GameObject caster;
    public GameObject target;
    public int damage;
    public DND5E.DamageType damageType;
}

namespace DND5E {
    // 法术学派枚举
    public enum SpellSchool {
        Abjuration,     // 防护系
        Conjuration,    // 咒法系
        Divination,     // 预言系
        Enchantment,    // 附魔系
        Evocation,      // 塑能系
        Illusion,       // 幻术系
        Necromancy,     // 死灵系
        Transmutation   // 变化系
    }

    // 法术施放时间枚举
    public enum CastingTime {
        Action,         // 动作
        BonusAction,    // 附赠动作
        Reaction,       // 反应
        Minute,         // 分钟
        Hour,           // 小时
        Ritual          // 仪式
    }

    // 法术成分枚举
    [System.Flags]
    public enum SpellComponent {
        None = 0,
        Verbal = 1,     // 言语成分
        Somatic = 2,    // 姿势成分
        Material = 4    // 材料成分
    }

    // 法术持续时间类型枚举
    public enum DurationType {
        Instantaneous,  // 瞬间
        Concentration,  // 专注
        NonConcentration // 非专注
    }

    // 法术范围类型枚举
    public enum AreaOfEffectType {
        None,           // 无范围效果
        Sphere,         // 球形
        Cube,           // 立方体
        Cone,           // 锥形
        Line,           // 线形
        Cylinder        // 圆柱形
    }

    // 法术类
    [System.Serializable]
    public class Spell {
        // 基本信息
        public string name;                   // 法术名称
        public string description;            // 法术描述
        public int level;                     // 法术环级（0为戏法）
        public SpellSchool school;            // 法术学派

        // 施法信息
        public CastingTime castingTime;       // 施法时间
        public int castingTimeValue = 1;      // 施法时间值（用于分钟、小时等）
        public SpellComponent components;     // 法术成分
        public string materialComponents;     // 材料成分描述

        // 法术范围
        public int range;                     // 施法距离（尺）
        public AreaOfEffectType areaOfEffect; // 效果范围类型
        public int areaOfEffectSize;          // 效果范围大小

        // 持续时间
        public DurationType durationType;     // 持续时间类型
        public int duration;                  // 持续时间（回合数/分钟数/小时数）

        // 豁免检定
        public bool requiresSavingThrow;      // 是否需要豁免检定
        public string savingThrowAbility;     // 豁免属性（Str/Dex/Con/Int/Wis/Cha）

        // 攻击掷骰
        public bool requiresAttackRoll;       // 是否需要攻击掷骰
        public bool isRangedAttack;           // 是否为远程攻击

        // 伤害信息
        public bool dealsDamage;              // 是否造成伤害
        public string damageFormula;          // 伤害公式（如"1d8"）
        public DND5E.DamageType damageType;   // 伤害类型

        // 治疗信息
        public bool heals;                    // 是否治疗
        public string healingFormula;         // 治疗公式（如"1d8"）

        // 状态效果
        public List<DND5E.StatusEffectType> statusEffects = new List<DND5E.StatusEffectType>(); // 状态效果

        // 等级缩放
        public bool scalesWithLevel = false;  // 是否随等级缩放
        public int levelScalingInterval = 3;  // 等级缩放间隔（每X级增加1个骰子）

        // 构造函数
        public Spell(string name, int level, SpellSchool school) {
            this.name = name;
            this.level = level;
            this.school = school;
        }

        // 获取法术描述
        public string GetFullDescription() {
            string desc = $"{name}\n";
            desc += $"{level}环 {school} 法术\n";

            // 施法时间
            desc += $"施法时间: ";
            switch (castingTime) {
                case CastingTime.Action:
                    desc += "1个动作";
                    break;
                case CastingTime.BonusAction:
                    desc += "1个附赠动作";
                    break;
                case CastingTime.Reaction:
                    desc += "1个反应";
                    break;
                case CastingTime.Minute:
                    desc += $"{castingTimeValue}分钟";
                    break;
                case CastingTime.Hour:
                    desc += $"{castingTimeValue}小时";
                    break;
                case CastingTime.Ritual:
                    desc += "仪式 (10分钟)";
                    break;
            }
            desc += "\n";

            // 施法成分
            desc += "成分: ";
            List<string> componentsList = new List<string>();
            if ((components & SpellComponent.Verbal) != 0) componentsList.Add("言语(V)");
            if ((components & SpellComponent.Somatic) != 0) componentsList.Add("姿势(S)");
            if ((components & SpellComponent.Material) != 0) componentsList.Add($"材料(M: {materialComponents})");
            desc += string.Join(", ", componentsList) + "\n";

            // 施法距离
            desc += $"施法距离: {range}尺\n";

            // 持续时间
            desc += "持续时间: ";
            switch (durationType) {
                case DurationType.Instantaneous:
                    desc += "瞬间";
                    break;
                case DurationType.Concentration:
                    if (duration <= 10)
                        desc += $"专注，至多{duration}回合";
                    else if (duration <= 600)
                        desc += $"专注，至多{duration / 10}分钟";
                    else
                        desc += $"专注，至多{duration / 600}小时";
                    break;
                case DurationType.NonConcentration:
                    if (duration <= 10)
                        desc += $"{duration}回合";
                    else if (duration <= 600)
                        desc += $"{duration / 10}分钟";
                    else
                        desc += $"{duration / 600}小时";
                    break;
            }
            desc += "\n\n";

            // 法术描述
            desc += description;

            return desc;
        }
    }

    // 法术列表类
    [System.Serializable]
    public class SpellList {
        public List<Spell> knownSpells = new List<Spell>();
        public int[] spellSlots = new int[9]; // 索引0对应1环法术位

        // 添加法术
        public void AddSpell(Spell spell) {
            knownSpells.Add(spell);
        }

        // 移除法术
        public void RemoveSpell(Spell spell) {
            knownSpells.Remove(spell);
        }

        // 获取特定环级的法术
        public List<Spell> GetSpellsByLevel(int level) {
            return knownSpells.FindAll(s => s.level == level);
        }

        // 获取特定学派的法术
        public List<Spell> GetSpellsBySchool(SpellSchool school) {
            return knownSpells.FindAll(s => s.school == school);
        }

        // 使用法术位
        public bool UseSpellSlot(int level) {
            if (level <= 0 || level > 9) return false;

            if (spellSlots[level - 1] > 0) {
                spellSlots[level - 1]--;
                return true;
            }

            return false;
        }

        // 恢复法术位
        public void RestoreSpellSlot(int level, int amount = 1) {
            if (level <= 0 || level > 9) return;

            // 计算最大法术位（这里需要根据职业和等级来确定）
            int maxSlots = 4; // 临时值，实际应该根据职业和等级计算

            spellSlots[level - 1] = Mathf.Min(spellSlots[level - 1] + amount, maxSlots);
        }

        // 恢复所有法术位（长休息）
        public void RestoreAllSpellSlots() {
            // 这里需要根据职业和等级来确定每环的最大法术位
            // 临时值，实际应该根据职业和等级计算
            spellSlots[0] = 4; // 1环
            spellSlots[1] = 3; // 2环
            spellSlots[2] = 3; // 3环
            spellSlots[3] = 3; // 4环
            spellSlots[4] = 2; // 5环
            spellSlots[5] = 1; // 6环
            spellSlots[6] = 1; // 7环
            spellSlots[7] = 1; // 8环
            spellSlots[8] = 1; // 9环
        }
    }

    // 法术系统组件
    public class SpellSystem : MonoBehaviour {
        // 法术列表
        public SpellList spellList = new SpellList();

        // 所有可用法术的字典，用于快速查找
        private Dictionary<string, Spell> allAvailableSpells = new Dictionary<string, Spell>();

        // 角色引用
        private string characterName = "角色";
        private ActionSystem actionSystem;

        private void Awake() {
            // 获取行动系统
            actionSystem = GetComponent<ActionSystem>();
            if (actionSystem == null) {
                Debug.LogWarning("SpellSystem需要ActionSystem组件!");
                actionSystem = gameObject.AddComponent<ActionSystem>();
            }

            // 尝试获取角色名称
            if (TryGetComponent(out MonoBehaviour charComponent)) {
                characterName = charComponent.name;
            }

            // 获取角色属性
            global::CharacterStats charStats = GetComponent<global::CharacterStats>();
            if (charStats != null && charStats.characterClass == DND5E.CharacterClass.Fighter) {
                // 战士不应该有法术
                return; // 直接返回，不初始化法术列表
            }

            // 初始化所有可用法术（仅用于参考和修正范围）
            InitializeAllAvailableSpells();

            // 检查预制体上是否已经设置了已知法术
            if (spellList.knownSpells.Count > 0) {
                // 构建法术名称列表
                List<string> spellNames = new List<string>();
                foreach (Spell spell in spellList.knownSpells) {
                    spellNames.Add(spell.name);

                    // 确保法术范围正确设置
                    if (spell.range <= 0) {
                        // 尝试从allAvailableSpells中获取正确的法术对象
                        if (allAvailableSpells.TryGetValue(spell.name, out Spell correctSpell)) {
                            spell.range = correctSpell.range;
                            Debug.Log($"修正法术 {spell.name} 的范围为 {spell.range} 尺");
                        }
                        else {
                            // 如果找不到，设置一个默认值
                            spell.range = 30; // 根据自定义5E规则，统一为30尺
                            Debug.Log($"无法找到法术 {spell.name} 的正确范围，设置默认值30尺");
                        }
                    }
                }

                // 初始化法术位
                spellList.RestoreAllSpellSlots();
            }
            else {
                Debug.LogWarning($"预制体 {characterName} 没有设置已知法术，请在Unity编辑器中设置");
            }
        }

        // 初始化所有可用法术（现在只用于范围修复，不再硬编码法术）
        private void InitializeAllAvailableSpells() {
            // 清空字典
            allAvailableSpells.Clear();

            // 从预制体上的已知法术列表构建字典，用于范围修复
            foreach (Spell spell in spellList.knownSpells) {
                if (!allAvailableSpells.ContainsKey(spell.name)) {
                    allAvailableSpells.Add(spell.name, spell);
                }
            }

        }

        // 恢复法术位
        public void RestoreSpellSlots() {
            // 初始化法术位
            spellList.RestoreAllSpellSlots();
            Debug.Log($"为 {characterName} 恢复了所有法术位");
        }

        // 检查是否可以施放法术
        public bool CanCastSpell(Spell spell) {
            // 检查法术是否在已知法术列表中
            bool spellKnown = spellList.knownSpells.Contains(spell);
            Debug.Log($"法术 {spell.name} 是否在已知法术列表中: {spellKnown}");

            if (!spellKnown) {
                Debug.LogWarning($"{characterName} 不知道法术 {spell.name}!");
                return false;
            }

            // 检查法术位（戏法除外）
            if (spell.level > 0) {
                // 检查是否有足够的法术位
                if (spellList.spellSlots[spell.level - 1] <= 0) {
                    Debug.LogWarning($"{characterName} 没有足够的 {spell.level} 环法术位!");
                    return false;
                }
            }

            // 检查施法时间
            ActionType actionType = ActionType.Action;
            switch (spell.castingTime) {
                case CastingTime.Action:
                    actionType = ActionType.Action;
                    break;
                case CastingTime.BonusAction:
                    actionType = ActionType.BonusAction;
                    break;
                case CastingTime.Reaction:
                    actionType = ActionType.Reaction;
                    break;
                default:
                    Debug.LogWarning($"{spell.name} 的施法时间不是战斗中可用的动作类型!");
                    return false;
            }

            // 检查是否有足够的动作
            bool hasRequiredAction = false;
            switch (actionType) {
                case ActionType.Action:
                    hasRequiredAction = actionSystem.hasAction;
                    break;
                case ActionType.BonusAction:
                    hasRequiredAction = actionSystem.hasBonusAction;
                    break;
                case ActionType.Reaction:
                    hasRequiredAction = actionSystem.hasReaction;
                    break;
                default:
                    Debug.LogWarning($"未知的动作类型: {actionType}");
                    return false;
            }

            if (!hasRequiredAction) {
                Debug.LogWarning($"{characterName} 没有足够的 {actionType} 来施放 {spell.name}!");
                return false;
            }

            // 所有检查都通过
            Debug.Log($"{characterName} 可以施放法术 {spell.name}");
            return true;
        }

        // 施放法术
        public bool CastSpell(Spell spell, GameObject target) {
            // 确保在施法前恢复默认光标
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            Debug.Log("SpellSystem确保施法前恢复默认光标");

            // 详细记录法术施放前的状态
            Debug.Log($"尝试施放法术: {spell.name}, 施法者: {characterName}");
            Debug.Log($"当前已知法术数量: {spellList.knownSpells.Count}");

            // 列出所有已知法术
            if (spellList.knownSpells.Count > 0) {
                // 构建法术名称列表
                List<string> spellNames = new List<string>();
                foreach (Spell spellItem in spellList.knownSpells) {
                    spellNames.Add(spellItem.name);
                }
                string knownSpellsList = string.Join(", ", spellNames);
                Debug.Log($"已知法术列表: {knownSpellsList}");
            }
            else {
                Debug.LogWarning("已知法术列表为空!");
            }

            // 检查法术是否存在
            bool spellKnown = spellList.knownSpells.Contains(spell);
            Debug.Log($"法术 {spell.name} 是否在已知法术列表中: {spellKnown}");

            if (!spellKnown) {
                Debug.LogWarning($"{characterName} 不知道法术 {spell.name}!");
                return false;
            }

            // 检查法术位（戏法除外）
            if (spell.level > 0) {
                if (!spellList.UseSpellSlot(spell.level)) {
                    Debug.LogWarning($"{characterName} 没有足够的 {spell.level} 环法术位!");
                    return false;
                }
            }

            // 检查施法时间
            ActionType actionType = ActionType.Action;
            switch (spell.castingTime) {
                case CastingTime.Action:
                    actionType = ActionType.Action;
                    break;
                case CastingTime.BonusAction:
                    actionType = ActionType.BonusAction;
                    break;
                case CastingTime.Reaction:
                    actionType = ActionType.Reaction;
                    break;
                default:
                    Debug.LogWarning($"{spell.name} 的施法时间不是战斗中可用的动作类型!");
                    return false;
            }

            // 使用行动
            if (!actionSystem.UseAction(actionType)) {
                return false;
            }

            // 检查施法距离
            if (target != null) {
                float distance = Vector3.Distance(transform.position, target.transform.position);

                // 详细记录法术范围信息
                Debug.Log($"法术 {spell.name} 的施法范围: {spell.range}尺");
                Debug.Log($"与目标的距离: {distance}尺");

                // 如果法术范围为0，尝试修复
                if (spell.range <= 0) {
                    Debug.LogWarning($"法术 {spell.name} 的施法范围为0或负值，尝试修复...");

                    // 尝试从allAvailableSpells中获取正确的法术对象
                    if (allAvailableSpells.TryGetValue(spell.name, out Spell correctSpell)) {
                        Debug.Log($"从allAvailableSpells中找到了法术 {spell.name}，其范围为: {correctSpell.range}尺");
                        spell.range = correctSpell.range;
                    }
                    else {
                        // 如果找不到，设置一个默认值
                        Debug.LogWarning($"无法从allAvailableSpells中找到法术 {spell.name}，设置默认范围为30尺");
                        spell.range = 30; // 根据自定义5E规则，统一为30尺
                    }

                    Debug.Log($"修复后的法术 {spell.name} 的施法范围: {spell.range}尺");
                }

                // 再次检查距离
                if (distance > spell.range) {
                    string rangeMessage = $"目标超出 {spell.name} 的施法范围 ({distance:F1} > {spell.range})!";
                    Debug.LogWarning(rangeMessage);

                    // 在战斗日志中添加提示
                    // 战斗日志已转移至挂机系统自动记录
                    Debug.Log(rangeMessage);

                    return false;
                }
                else {
                    Debug.Log($"目标在 {spell.name} 的施法范围内 ({distance:F1} <= {spell.range})");
                }
            }

            // 施放法术
            Debug.Log($"{characterName} 施放 {spell.name}!");

            // 如果有目标，让施法者面朝目标
            if (target != null) {
                FaceTarget(target);
            }

            // 播放施法动画
            PlayCastAnimation();

            // 获取施法者和目标的CharacterStats组件
            global::CharacterStats casterStats = GetComponent<global::CharacterStats>();
            global::CharacterStats targetStats = null;
            if (target != null) {
                targetStats = target.GetComponent<global::CharacterStats>();
            }

            // 战斗日志已转移至挂机系统自动记录
            {
                string casterName = casterStats != null ? casterStats.GetDisplayName() : characterName;
                string targetName = targetStats != null ? targetStats.GetDisplayName() : "未知目标";

                Debug.Log($"{casterName} 施放 {spell.name}");

                if (target != null) {
                    Debug.Log($"目标: {targetName}");
                }
            }

            // 确保在施法后恢复默认光标
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            Debug.Log("SpellSystem确保施法后恢复默认光标");

            // 处理法术效果
            if (spell.dealsDamage) {
                // 获取伤害公式
                string damageFormula = spell.damageFormula;
                string originalDamageFormula = damageFormula; // 保存原始公式用于显示

                // 通用的等级缩放处理（如果法术配置了等级缩放）
                if (spell.scalesWithLevel) {
                    // 获取施法者等级
                    int casterLevel = 1;
                    global::CharacterStats characterStatsComponent = GetComponent<global::CharacterStats>();
                    if (characterStatsComponent != null) {
                        casterLevel = characterStatsComponent.level;
                        Debug.Log($"从CharacterStats组件获取到施法者等级: {casterLevel}");
                    }
                    else {
                        Debug.LogWarning("无法获取CharacterStats组件，使用默认施法者等级: 1");
                    }

                    // 根据配置的等级间隔计算额外骰子
                    int extraDice = 0;
                    if (spell.levelScalingInterval > 0) {
                        extraDice = (casterLevel - 1) / spell.levelScalingInterval;
                    }

                    // 解析原始伤害公式，获取骰子类型
                    string[] diceParts = spell.damageFormula.Split('d');
                    if (diceParts.Length == 2) {
                        int baseDiceCount = int.Parse(diceParts[0]);
                        string diceType = diceParts[1].Split('+')[0]; // 处理可能的加值部分

                        // 使用预制体配置的骰子类型，但根据等级调整数量
                        damageFormula = $"{baseDiceCount + extraDice}d{diceType}";
                        originalDamageFormula = damageFormula; // 更新原始公式

                        Debug.Log($"{spell.name} - 施法者等级: {casterLevel}, 原始公式: {spell.damageFormula}, 调整后: {damageFormula}");
                    }
                    else {
                        Debug.LogWarning($"无法解析{spell.name}的伤害公式: {spell.damageFormula}，使用原始公式");
                    }
                }

                // 如果需要攻击掷骰（如奥术冲击）
                if (spell.requiresAttackRoll && target != null && targetStats != null) {
                    // 获取施法者的CharacterStats组件
                    global::CharacterStats attackCasterStats = GetComponent<global::CharacterStats>();
                    if (attackCasterStats != null) {
                        // 确定施法关键属性加值
                        int abilityMod = 0;
                        string abilityName = "int"; // 默认使用智力

                        switch (attackCasterStats.characterClass) {
                            case DND5E.CharacterClass.Wizard:
                                abilityMod = attackCasterStats.stats.IntMod;
                                abilityName = "int";
                                break;
                            case DND5E.CharacterClass.Cleric:
                                abilityMod = attackCasterStats.stats.WisMod;
                                abilityName = "wis";
                                break;
                            case DND5E.CharacterClass.Bard:
                            case DND5E.CharacterClass.Sorcerer:
                            case DND5E.CharacterClass.Warlock:
                                abilityMod = attackCasterStats.stats.ChaMod;
                                abilityName = "cha";
                                break;
                            default:
                                abilityMod = attackCasterStats.stats.IntMod; // 默认使用智力
                                break;
                        }

                        // 获取熟练加值
                        int profBonus = attackCasterStats.proficiencyBonus;

                        // 掷骰
                        int d20Roll = Random.Range(1, 21);
                        int attackRoll = d20Roll + abilityMod + profBonus;

                        // 构建攻击公式字符串
                        string attackFormula = $"d20({d20Roll})";
                        if (abilityMod != 0) attackFormula += $" + {abilityName}({abilityMod})";
                        if (profBonus != 0) attackFormula += $" + 熟练({profBonus})";

                        // 添加到战斗日志
                        string attackTypeStr = spell.isRangedAttack ? "远程法术" : "近战法术";
                        Debug.Log($"法术攻击检定: {attackFormula} = {attackRoll} vs AC {targetStats.armorClass}");

                        // 检查是否命中
                        if (attackRoll >= targetStats.armorClass) {
                            // 命中
                            Debug.Log($"法术命中!");
                        }
                        else {
                            // 未命中
                            Debug.Log($"法术未命中!");

                            // 显示Miss文本
                            if (DamageNumberManager.Instance != null && target != null) {
                                DamageNumberManager.Instance.ShowMissText(target.transform);
                            }

                            return true; // 法术已施放但未命中，直接返回
                        }
                    }
                }

                // 解析伤害公式
                int damage = RollDamage(damageFormula);

                // 获取施法关键属性加值（用于伤害计算）
                int spellAbilityMod = 0;
                string spellAbilityName = "int"; // 默认使用智力

                global::CharacterStats spellCasterStats = GetComponent<global::CharacterStats>();
                if (spellCasterStats != null) {
                    switch (spellCasterStats.characterClass) {
                        case DND5E.CharacterClass.Wizard:
                            spellAbilityMod = spellCasterStats.stats.IntMod;
                            spellAbilityName = "int";
                            break;
                        case DND5E.CharacterClass.Cleric:
                            spellAbilityMod = spellCasterStats.stats.WisMod;
                            spellAbilityName = "wis";
                            break;
                        case DND5E.CharacterClass.Bard:
                        case DND5E.CharacterClass.Sorcerer:
                        case DND5E.CharacterClass.Warlock:
                            spellAbilityMod = spellCasterStats.stats.ChaMod;
                            spellAbilityName = "cha";
                            break;
                        default:
                            spellAbilityMod = spellCasterStats.stats.IntMod; // 默认使用智力
                            break;
                    }
                }

                // 如果需要豁免检定
                if (spell.requiresSavingThrow && target != null && targetStats != null) {
                    // 计算豁免DC
                    int saveDC = 8; // 基础DC
                    if (spellCasterStats != null) {
                        saveDC += spellAbilityMod + spellCasterStats.proficiencyBonus;
                    }

                    // 获取施法者的CharacterStats组件（用于显示名称）
                    global::CharacterStats casterStatsForName = GetComponent<global::CharacterStats>();

                    // 确定豁免属性
                    string saveAbility = "dex"; // 默认敏捷
                    string saveAbilityName = "敏捷";

                    switch (spell.savingThrowAbility.ToLower()) {
                        case "str":
                            saveAbility = "str";
                            saveAbilityName = "力量";
                            break;
                        case "dex":
                            saveAbility = "dex";
                            saveAbilityName = "敏捷";
                            break;
                        case "con":
                            saveAbility = "con";
                            saveAbilityName = "体质";
                            break;
                        case "int":
                            saveAbility = "int";
                            saveAbilityName = "智力";
                            break;
                        case "wis":
                            saveAbility = "wis";
                            saveAbilityName = "感知";
                            break;
                        case "cha":
                            saveAbility = "cha";
                            saveAbilityName = "魅力";
                            break;
                    }

                    // 进行豁免检定
                    int saveResult = targetStats.SavingThrow(saveAbility);
                    bool savingThrowSuccess = saveResult >= saveDC;

                    // 添加到战斗日志
                    string casterName = casterStatsForName != null ? casterStatsForName.GetDisplayName() : characterName;
                    string targetName = targetStats.GetDisplayName();
                    Debug.Log($"{targetName} 进行 {saveAbilityName} 豁免检定: {saveResult} vs DC {saveDC}");

                    if (savingThrowSuccess) {
                        damage /= 2;
                        Debug.Log($"豁免成功，伤害减半!");
                    }
                    else {
                        Debug.Log($"目标豁免失败，受到全额伤害: {damage}");
                        Debug.Log($"豁免失败，受到全额伤害!");
                    }
                }

                // 造成伤害
                if (target != null) {
                    // 尝试播放法术特效（基于法术名称动态查找）
                    bool effectPlayed = TryPlaySpellEffect(spell, target, damage);

                    if (!effectPlayed) {
                        // 如果没有特效或特效播放失败，直接造成伤害
                        ApplyDirectSpellDamage(spell, target, damage, originalDamageFormula, spellAbilityMod, spellAbilityName);
                    }
                }
                else {
                    Debug.LogWarning("目标为空，无法造成伤害!");
                }
            }

            // 处理治疗法术
            if (spell.heals && target != null && targetStats != null) {
                ApplySpellHealing(spell, target, targetStats);
            }

            // 应用状态效果
            if (spell.statusEffects.Count > 0 && target != null && targetStats != null) {
                ApplySpellStatusEffects(spell, target, targetStats);
            }

            return true;
        }

        /// <summary>
        /// 尝试播放法术特效
        /// </summary>
        private bool TryPlaySpellEffect(Spell spell, GameObject target, int damage) {
            // 查找SpellEffects组件
            SpellEffects spellEffectsComponent = SpellEffects.Instance;
            if (spellEffectsComponent == null) {
                spellEffectsComponent = FindObjectOfType<SpellEffects>();
            }

            if (spellEffectsComponent == null) {
                Debug.LogWarning($"未找到SpellEffects组件，无法播放{spell.name}特效");
                return false;
            }

            // 根据法术名称动态调用对应的特效方法
            try {
                switch (spell.name) {
                    case "奥术冲击":
                        if (spellEffectsComponent.arcaneBlastPrefab != null) {
                            spellEffectsComponent.PlayArcaneBlast(gameObject, target, damage, spell.damageType, null);
                            return true;
                        }
                        break;

                        // 可以在这里添加其他法术的特效处理
                        // case "魔法飞弹":
                        //     if (spellEffectsComponent.magicMissilePrefab != null)
                        //     {
                        //         spellEffectsComponent.PlayMagicMissile(gameObject, target, damage, spell.damageType, null);
                        //         return true;
                        //     }
                        //     break;
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"播放{spell.name}特效时出错: {e.Message}");
            }

            return false;
        }

        /// <summary>
        /// 直接应用法术伤害（无特效）
        /// </summary>
        private void ApplyDirectSpellDamage(Spell spell, GameObject target, int damage, string originalDamageFormula, int spellAbilityMod, string spellAbilityName) {
            global::CharacterStats targetStats = target.GetComponent<global::CharacterStats>();
            if (targetStats == null) return;

            // 获取目标适配器和动画控制器
            DND_CharacterAdapter targetAdapter = target.GetComponent<DND_CharacterAdapter>();
            AnimationController targetAnimController = null;

            if (targetAdapter == null) {
                targetAnimController = target.GetComponent<AnimationController>();
            }

            // 记录伤害结果到战斗日志
            RecordDamageToLog(originalDamageFormula, damage, spellAbilityMod, spellAbilityName, spell.damageType);

            // 应用伤害
            ApplySpellDamage(targetStats, damage, spell.damageType, targetAdapter, targetAnimController);
        }

        /// <summary>
        /// 记录伤害到战斗日志
        /// </summary>
        private void RecordDamageToLog(string originalDamageFormula, int damage, int spellAbilityMod, string spellAbilityName, DND5E.DamageType damageType) {
            // 改为Debug.Log输出

            // 构建伤害公式字符串
            string[] diceParts = originalDamageFormula.Split('d');
            if (diceParts.Length >= 2) {
                int diceCount = int.Parse(diceParts[0]);
                string diceType = diceParts[1].Split('+')[0];

                // 计算骰子伤害值
                int diceRoll = damage;
                if (spellAbilityMod > 0) diceRoll -= spellAbilityMod;

                // 构建伤害公式字符串
                string damageFormulaPretty = $"{diceCount}d{diceType}({diceRoll})";
                if (spellAbilityMod != 0) damageFormulaPretty += $" + {spellAbilityName}({spellAbilityMod})";

                Debug.Log($"伤害: {damageFormulaPretty} = {damage} 点{damageType}伤害");
            }
        }

        /// <summary>
        /// 应用法术治疗
        /// </summary>
        private void ApplySpellHealing(Spell spell, GameObject target, global::CharacterStats targetStats) {
            // 解析治疗公式
            int healing = RollDamage(spell.healingFormula);
            string originalHealingFormula = spell.healingFormula;

            // 获取施法关键属性加值
            int spellAbilityMod = 0;
            string spellAbilityName = "wis";

            global::CharacterStats healCasterStats = GetComponent<global::CharacterStats>();
            if (healCasterStats != null) {
                switch (healCasterStats.characterClass) {
                    case DND5E.CharacterClass.Wizard:
                        spellAbilityMod = healCasterStats.stats.IntMod;
                        spellAbilityName = "int";
                        break;
                    case DND5E.CharacterClass.Cleric:
                        spellAbilityMod = healCasterStats.stats.WisMod;
                        spellAbilityName = "wis";
                        break;
                    case DND5E.CharacterClass.Bard:
                    case DND5E.CharacterClass.Sorcerer:
                    case DND5E.CharacterClass.Warlock:
                        spellAbilityMod = healCasterStats.stats.ChaMod;
                        spellAbilityName = "cha";
                        break;
                    default:
                        spellAbilityMod = healCasterStats.stats.WisMod;
                        break;
                }
            }

            // 记录治疗结果
            string casterName = GetComponent<global::CharacterStats>() != null ?
                GetComponent<global::CharacterStats>().GetDisplayName() : characterName;
            string targetName = targetStats.GetDisplayName();

            // 构建治疗公式字符串
            string[] diceParts = originalHealingFormula.Split('d');
            if (diceParts.Length >= 2) {
                int diceCount = int.Parse(diceParts[0]);
                string diceType = diceParts[1].Split('+')[0];
                int diceRoll = healing;
                if (spellAbilityMod > 0) diceRoll -= spellAbilityMod;

                string healingFormulaPretty = $"{diceCount}d{diceType}({diceRoll})";
                if (spellAbilityMod != 0) healingFormulaPretty += $" + {spellAbilityName}({spellAbilityMod})";

                Debug.Log($"{casterName} 治疗 {targetName}");
                Debug.Log($"治疗: {healingFormulaPretty} = {healing} 点生命值");
            }

            // 治疗目标
            targetStats.HealDamage(healing);
            Debug.Log($"{characterName} 治疗目标 {healing} 点生命值!");
        }

        /// <summary>
        /// 应用法术状态效果
        /// </summary>
        private void ApplySpellStatusEffects(Spell spell, GameObject target, global::CharacterStats targetStats) {
            // 获取施法者名称
            string casterName = GetComponent<global::CharacterStats>() != null ?
                GetComponent<global::CharacterStats>().GetDisplayName() : characterName;
            string targetName = targetStats.GetDisplayName();

            // 记录状态效果信息
            Debug.Log($"{casterName} 对 {targetName} 施加状态效果: {string.Join(", ", spell.statusEffects)}");

            // 应用状态效果
            foreach (StatusEffectType effect in spell.statusEffects) {
                targetStats.AddStatusEffect(effect);
            }

            Debug.Log($"{characterName} 对目标施加状态效果: {string.Join(", ", spell.statusEffects)}");
        }

        // 让施法者面朝目标
        private void FaceTarget(GameObject target) {
            if (target == null) return;

            // 计算朝向目标的方向
            Vector3 direction = (target.transform.position - transform.position).normalized;

            if (direction.x != 0) {
                // 如果角色有SpriteRenderer，设置flipX
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null) {
                    spriteRenderer.flipX = direction.x < 0;
                    Debug.Log($"{characterName} 使用SpriteRenderer面朝目标，flipX: {spriteRenderer.flipX}");
                }

                // 如果角色有SkeletonAnimation，设置scale.x
                Spine.Unity.SkeletonAnimation skeletonAnimation = GetComponent<Spine.Unity.SkeletonAnimation>();
                if (skeletonAnimation != null) {
                    Vector3 scale = skeletonAnimation.transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * (direction.x < 0 ? -1 : 1);
                    skeletonAnimation.transform.localScale = scale;
                    Debug.Log($"{characterName} 使用SkeletonAnimation面朝目标，scale.x: {scale.x}");
                }
            }
        }

        // 播放施法动画
        private void PlayCastAnimation() {
            // 首先尝试使用DND_CharacterAdapter
            DND_CharacterAdapter characterAdapter = GetComponent<DND_CharacterAdapter>();
            if (characterAdapter != null) {
                characterAdapter.PlayCastAnimation();
                Debug.Log($"使用DND_CharacterAdapter播放施法动画: {characterAdapter.animationMapping.castAnimation}");
                return;
            }

            // 如果没有DND_CharacterAdapter，尝试使用AnimationController
            AnimationController animController = GetComponent<AnimationController>();
            if (animController != null) {
                animController.PlayCastSpell();
                Debug.Log($"使用AnimationController播放施法动画: {animController.castSpellAnimation}");
                return;
            }

            Debug.LogWarning($"{characterName} 既没有DND_CharacterAdapter也没有AnimationController组件，无法播放施法动画");
        }

        // 统一的法术伤害应用方法
        private void ApplySpellDamage(global::CharacterStats targetStats, int damage, DND5E.DamageType damageType, DND_CharacterAdapter targetAdapter, AnimationController targetAnimController) {
            if (targetStats == null) {
                Debug.LogError("ApplySpellDamage: targetStats为null");
                return;
            }

            // 应用伤害
            targetStats.TakeDamage(damage, damageType);
            Debug.Log($"{characterName} 对目标造成 {damage} 点 {damageType} 伤害!");

            // 等待一小段时间，确保有时间准备播放受击动画
            StartCoroutine(PlaySpellHitEffects(targetStats, targetAdapter, targetAnimController));
        }

        // 播放法术命中效果和处理死亡
        private IEnumerator PlaySpellHitEffects(global::CharacterStats targetStats, DND_CharacterAdapter targetAdapter, AnimationController targetAnimController) {
            // 等待一小段时间，确保有时间准备播放受击动画
            yield return new WaitForSeconds(0.1f);

            // 播放受击动画
            if (targetAdapter != null) {
                // 使用DND_CharacterAdapter的TakeDamage方法，它会播放受击动画，并在生命值为0时播放死亡动画
                targetAdapter.TakeDamage();
                Debug.Log($"调用 {targetStats.characterName} 的TakeDamage方法播放受击动画");
            }
            else if (targetAnimController != null) {
                // 如果没有DND_CharacterAdapter，使用AnimationController
                targetAnimController.PlayHit();
                Debug.Log($"使用AnimationController播放目标受击动画: {targetAnimController.hitAnimation}");
            }                // 确保UI更新
            Debug.Log($"更新角色状态UI: {targetStats.GetDisplayName()}");

            // 检查目标是否死亡
            if (targetStats.currentHitPoints <= 0) {
                yield return new WaitForSeconds(0.5f); // 给受击动画更多时间

                if (targetAdapter != null) {
                    // 使用DND_CharacterAdapter播放死亡动画
                    targetAdapter.PlayDeathAnimation();
                    Debug.Log($"使用DND_CharacterAdapter播放目标死亡动画: {targetAdapter.animationMapping.deathAnimation}");
                }
                else if (targetAnimController != null) {
                    // 如果没有DND_CharacterAdapter，使用AnimationController
                    targetAnimController.PlayDeath();
                    Debug.Log($"使用AnimationController播放目标死亡动画: {targetAnimController.deathAnimation}");
                }
            }
        }

        // 掷骰计算伤害
        private int RollDamage(string damageFormula) {
            // 解析伤害公式，例如 "3d6+5"
            string[] parts = damageFormula.Split('+');

            int damage = 0;

            // 处理骰子部分
            if (parts.Length > 0) {
                string dicePart = parts[0];
                string[] diceParts = dicePart.Split('d');

                if (diceParts.Length == 2) {
                    int diceCount = int.Parse(diceParts[0]);
                    int diceType = int.Parse(diceParts[1]);

                    for (int i = 0; i < diceCount; i++) {
                        damage += Random.Range(1, diceType + 1);
                    }
                }
            }

            // 处理固定加值部分
            if (parts.Length > 1) {
                damage += int.Parse(parts[1]);
            }

            return damage;
        }
    }
}
