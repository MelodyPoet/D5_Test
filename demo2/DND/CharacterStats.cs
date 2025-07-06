using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DND5E {
    [System.Serializable]
    public class CharacterStats {
        // 六大属性
        public int Strength = 10;
        public int Dexterity = 10;
        public int Constitution = 10;
        public int Intelligence = 10;
        public int Wisdom = 10;
        public int Charisma = 10;

        // 属性调整值
        public int StrMod => (Strength - 10) / 2;
        public int DexMod => (Dexterity - 10) / 2;
        public int ConMod => (Constitution - 10) / 2;
        public int IntMod => (Intelligence - 10) / 2;
        public int WisMod => (Wisdom - 10) / 2;
        public int ChaMod => (Charisma - 10) / 2;

        // 豁免熟练
        public bool StrSaveProficient = false;
        public bool DexSaveProficient = false;
        public bool ConSaveProficient = false;
        public bool IntSaveProficient = false;
        public bool WisSaveProficient = false;
        public bool ChaSaveProficient = false;

        // 技能列表
        public enum Skill {
            Athletics,      // 运动 (力量)
            Acrobatics,     // 杂技 (敏捷)
            SleightOfHand,  // 手上功夫 (敏捷)
            Stealth,        // 隐匿 (敏捷)
            Arcana,         // 奥秘 (智力)
            History,        // 历史 (智力)
            Investigation,  // 调查 (智力)
            Nature,         // 自然 (智力)
            Religion,       // 宗教 (智力)
            AnimalHandling, // 驯兽 (感知)
            Insight,        // 洞悉 (感知)
            Medicine,       // 医疗 (感知)
            Perception,     // 察觉 (感知)
            Survival,       // 生存 (感知)
            Deception,      // 欺瞒 (魅力)
            Intimidation,   // 威吓 (魅力)
            Performance,    // 表演 (魅力)
            Persuasion      // 说服 (魅力)
        }

        // 技能熟练
        public List<Skill> ProficientSkills = new List<Skill>();

        // 获取豁免加值
        public int GetSavingThrowBonus(string ability, int proficiencyBonus) {
            int abilityMod = 0;
            bool isProficient = false;

            switch (ability.ToLower()) {
                case "str":
                case "strength":
                    abilityMod = StrMod;
                    isProficient = StrSaveProficient;
                    break;

                case "dex":
                case "dexterity":
                    abilityMod = DexMod;
                    isProficient = DexSaveProficient;
                    break;

                case "con":
                case "constitution":
                    abilityMod = ConMod;
                    isProficient = ConSaveProficient;
                    break;

                case "int":
                case "intelligence":
                    abilityMod = IntMod;
                    isProficient = IntSaveProficient;
                    break;

                case "wis":
                case "wisdom":
                    abilityMod = WisMod;
                    isProficient = WisSaveProficient;
                    break;

                case "cha":
                case "charisma":
                    abilityMod = ChaMod;
                    isProficient = ChaSaveProficient;
                    break;
            }

            return abilityMod + (isProficient ? proficiencyBonus : 0);
        }

        // 获取技能加值
        public int GetSkillBonus(Skill skill, int proficiencyBonus) {
            int abilityMod = 0;

            // 确定技能对应的属性调整值
            switch (skill) {
                case Skill.Athletics:
                    abilityMod = StrMod;
                    break;

                case Skill.Acrobatics:
                case Skill.SleightOfHand:
                case Skill.Stealth:
                    abilityMod = DexMod;
                    break;

                case Skill.Arcana:
                case Skill.History:
                case Skill.Investigation:
                case Skill.Nature:
                case Skill.Religion:
                    abilityMod = IntMod;
                    break;

                case Skill.AnimalHandling:
                case Skill.Insight:
                case Skill.Medicine:
                case Skill.Perception:
                case Skill.Survival:
                    abilityMod = WisMod;
                    break;

                case Skill.Deception:
                case Skill.Intimidation:
                case Skill.Performance:
                case Skill.Persuasion:
                    abilityMod = ChaMod;
                    break;
            }

            // 如果熟练，加上熟练加值
            bool isProficient = ProficientSkills.Contains(skill);
            return abilityMod + (isProficient ? proficiencyBonus : 0);
        }

        // 随机生成属性（4d6取3）
        public void RollStats() {
            Strength = RollAbilityScore();
            Dexterity = RollAbilityScore();
            Constitution = RollAbilityScore();
            Intelligence = RollAbilityScore();
            Wisdom = RollAbilityScore();
            Charisma = RollAbilityScore();
        }

        private int RollAbilityScore() {
            int[] rolls = new int[4];
            for (int i = 0; i < 4; i++) {
                rolls[i] = Random.Range(1, 7);
            }

            System.Array.Sort(rolls);
            return rolls[1] + rolls[2] + rolls[3]; // 取最高的3个值
        }

        // 标准数组分配
        public void UseStandardArray() {
            // DND5e标准数组: 15, 14, 13, 12, 10, 8
            Strength = 15;
            Dexterity = 14;
            Constitution = 13;
            Intelligence = 12;
            Wisdom = 10;
            Charisma = 8;
        }

        // 计算生命值
        public int CalculateHitPoints(int level, int hitDie, bool isFirstLevel = false) {
            if (level <= 0) return 0;

            int hp = 0;

            // 1级时生命值为生命骰最大值+体质调整值
            if (isFirstLevel) {
                hp = hitDie + ConMod;
                level--;
            }

            // 之后每级生命值为生命骰平均值(向上取整)+体质调整值
            int averageRoll = (hitDie / 2) + 1;
            hp += level * (averageRoll + ConMod);

            return Mathf.Max(1, hp); // 生命值最小为1
        }

        // 计算被动察觉
        public int PassivePerception(int proficiencyBonus) {
            return 10 + GetSkillBonus(Skill.Perception, proficiencyBonus);
        }

        // 计算先攻加值
        public int InitiativeBonus() {
            return DexMod;
        }
    }

    // 角色职业枚举
    public enum CharacterClass {
        Fighter,    // 战士
        Rogue,      // 盗贼
        Wizard,     // 法师
        Cleric,     // 牧师
        Ranger,     // 游侠
        Paladin,    // 圣武士
        Barbarian,  // 野蛮人
        Bard,       // 吟游诗人
        Druid,      // 德鲁伊
        Monk,       // 武僧
        Sorcerer,   // 术士
        Warlock     // 契术师
    }

    // 伤害类型枚举
    public enum DamageType {
        Slashing,   // 挥砍
        Piercing,   // 穿刺
        Bludgeoning,// 钝击
        Fire,       // 火焰
        Cold,       // 寒冷
        Lightning,  // 闪电
        Poison,     // 毒素
        Acid,       // 酸性
        Necrotic,   // 暗蚀
        Radiant,    // 光耀
        Psychic,    // 心灵
        Force,      // 力场
        Thunder     // 雷鸣
    }

    // 状态效果类型枚举
    public enum StatusEffectType {
        Blinded,        // 目盲
        Charmed,        // 魅惑
        Deafened,       // 耳聋
        Frightened,     // 恐慌
        Grappled,       // 擒抱
        Incapacitated,  // 失能
        Invisible,      // 隐形
        Paralyzed,      // 麻痹
        Petrified,      // 石化
        Poisoned,       // 中毒
        Prone,          // 倒地
        Restrained,     // 束缚
        Stunned,        // 震慑
        Unconscious,    // 昏迷
        Dodging         // 闪避中
    }
}

// 角色属性组件
public class CharacterStats : MonoBehaviour {
    // DND5e角色属性
    public DND5E.CharacterStats stats = new DND5E.CharacterStats();

    // 角色基本信息
    public string characterName = "角色";
    // 从UI中读取的实际显示名称
    [HideInInspector]
    public string displayName = "";
    public DND5E.CharacterClass characterClass = DND5E.CharacterClass.Fighter;
    public int level = 1;
    public int experiencePoints = 0;

    // 战斗属性
    public int baseArmorClass = 10;    // 基础护甲等级
    public int armorClass = 10;        // 当前护甲等级（包含状态效果加成）
    public int maxHitPoints = 10;
    public int currentHitPoints = 10;
    public int temporaryHitPoints = 0;
    public int hitDice = 8; // 生命骰
    public int proficiencyBonus = 2; // 熟练加值

    // 速度
    public int speed = 30; // 单位：尺

    // 伤害抗性和弱点
    public List<DND5E.DamageType> resistances = new List<DND5E.DamageType>();
    public List<DND5E.DamageType> vulnerabilities = new List<DND5E.DamageType>();
    public List<DND5E.DamageType> immunities = new List<DND5E.DamageType>();

    // 状态效果
    public List<DND5E.StatusEffectType> statusEffects = new List<DND5E.StatusEffectType>();    // 获取角色在UI中显示的实际名称
    public string GetDisplayName() {
        // 如果已经设置了displayName，直接返回
        if (!string.IsNullOrEmpty(displayName)) {
            return displayName;
        }

        // 不再查找Status Canvas，直接使用角色名称
        // 这避免了因为UI组件缺失导致的查找卡死问题
        Debug.Log($"GetDisplayName: 使用角色名称 {characterName} 作为显示名称");
        return characterName;
    }

    // 初始化角色属性
    public void InitializeByClass() {
        // 设置熟练加值
        proficiencyBonus = 2; // 1-4级为+2

        // 根据职业设置属性
        switch (characterClass) {
            case DND5E.CharacterClass.Fighter:
                stats.UseStandardArray();
                stats.StrSaveProficient = true;
                stats.ConSaveProficient = true;
                hitDice = 10;
                maxHitPoints = 10 + stats.ConMod;
                baseArmorClass = 16; // 链甲+盾牌
                armorClass = baseArmorClass;

                // 添加熟练技能
                stats.ProficientSkills.Add(DND5E.CharacterStats.Skill.Athletics);
                stats.ProficientSkills.Add(DND5E.CharacterStats.Skill.Perception);
                break;

            case DND5E.CharacterClass.Rogue:
                stats.Strength = 8;
                stats.Dexterity = 15;
                stats.Constitution = 14;
                stats.Intelligence = 12;
                stats.Wisdom = 10;
                stats.Charisma = 13;

                stats.DexSaveProficient = true;
                stats.IntSaveProficient = true;
                hitDice = 8;
                maxHitPoints = 8 + stats.ConMod;
                baseArmorClass = 14; // 皮甲+敏捷
                armorClass = baseArmorClass;

                // 添加熟练技能
                stats.ProficientSkills.Add(DND5E.CharacterStats.Skill.Stealth);
                stats.ProficientSkills.Add(DND5E.CharacterStats.Skill.Acrobatics);
                stats.ProficientSkills.Add(DND5E.CharacterStats.Skill.SleightOfHand);
                stats.ProficientSkills.Add(DND5E.CharacterStats.Skill.Perception);
                break;

            case DND5E.CharacterClass.Wizard:
                stats.Strength = 8;
                stats.Dexterity = 14;
                stats.Constitution = 13;
                stats.Intelligence = 15;
                stats.Wisdom = 12;
                stats.Charisma = 10;

                stats.IntSaveProficient = true;
                stats.WisSaveProficient = true;
                hitDice = 6;
                maxHitPoints = 6 + stats.ConMod;
                baseArmorClass = 12; // 敏捷
                armorClass = baseArmorClass;

                // 添加熟练技能
                stats.ProficientSkills.Add(DND5E.CharacterStats.Skill.Arcana);
                stats.ProficientSkills.Add(DND5E.CharacterStats.Skill.History);
                break;

                // 其他职业...
        }

        // 初始化生命值
        currentHitPoints = maxHitPoints;
    }

    // 检查是否有特定状态效果
    public bool HasStatusEffect(DND5E.StatusEffectType type) {
        return statusEffects.Contains(type);
    }

    // 添加状态效果
    public void AddStatusEffect(DND5E.StatusEffectType type) {
        if (!statusEffects.Contains(type)) {
            statusEffects.Add(type);

            // 如果是闪避状态，更新AC
            if (type == DND5E.StatusEffectType.Dodging) {
                UpdateArmorClass();
            }

            // 添加战斗日志
            if (DND_BattleUI.Instance != null) {
                string effectName = GetStatusEffectName(type);
                DND_BattleUI.Instance.AddCombatLog($"{GetDisplayName()} 获得了 {effectName} 状态");
            }
        }
    }

    // 获取状态效果的中文名称
    private string GetStatusEffectName(DND5E.StatusEffectType type) {
        switch (type) {
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
            case DND5E.StatusEffectType.Stunned: return "震慑";
            case DND5E.StatusEffectType.Unconscious: return "昏迷";
            case DND5E.StatusEffectType.Dodging: return "防御姿态";
            default: return type.ToString();
        }
    }

    // 移除状态效果
    public void RemoveStatusEffect(DND5E.StatusEffectType type) {
        bool removed = statusEffects.Remove(type);

        // 如果移除了闪避状态，更新AC
        if (removed && type == DND5E.StatusEffectType.Dodging) {
            UpdateArmorClass();
        }

        // 添加战斗日志
        if (removed && DND_BattleUI.Instance != null) {
            string effectName = GetStatusEffectName(type);
            if (type == DND5E.StatusEffectType.Dodging) {
                DND_BattleUI.Instance.AddCombatLog($"{GetDisplayName()} 退出防御姿态，AC恢复正常");
            }
            else {
                DND_BattleUI.Instance.AddCombatLog($"{GetDisplayName()} 的 {effectName} 状态已结束");
            }
        }
    }

    // 更新护甲等级，考虑状态效果
    public void UpdateArmorClass() {
        // 从基础AC开始
        armorClass = baseArmorClass;

        // 应用状态效果的修正
        if (HasStatusEffect(DND5E.StatusEffectType.Dodging)) {
            // 防御姿态提供+2 AC
            armorClass += 2;
            Debug.Log($"{GetDisplayName()} 处于防御姿态，AC+2，当前AC: {armorClass}");
        }

        // 可以在这里添加其他影响AC的状态效果

        // 更新UI
        if (DND_BattleUI.Instance != null) {
            DND_BattleUI.Instance.UpdateCharacterStatusUI(this);
        }
    }

    // 受到伤害
    public void TakeDamage(int damage, DND5E.DamageType damageType) {
        // 检查免疫
        if (immunities.Contains(damageType)) {
            Debug.Log($"{GetDisplayName()} 免疫 {damageType} 伤害!");
            return;
        }

        // 检查抗性和弱点
        if (resistances.Contains(damageType)) {
            damage = Mathf.Max(1, damage / 2);
            Debug.Log($"{GetDisplayName()} 对 {damageType} 伤害有抗性!");
        }
        else if (vulnerabilities.Contains(damageType)) {
            damage *= 2;
            Debug.Log($"{GetDisplayName()} 对 {damageType} 伤害有弱点!");
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
        int oldHitPoints = currentHitPoints;
        currentHitPoints = Mathf.Max(0, currentHitPoints - damage);

        Debug.Log($"{GetDisplayName()} 受到 {damage} 点 {damageType} 伤害! 剩余生命值: {currentHitPoints}/{maxHitPoints}");

        // 显示伤害数字
        if (DamageNumberManager.Instance != null && damage > 0) {
            DamageNumberManager.Instance.ShowDamageNumber(transform, damage, false);
        }

        // 添加战斗日志
        if (DND_BattleUI.Instance != null) {
            // 构建伤害描述
            string damageDescription = $"{damage} 点{damageType}伤害";

            // 如果有抗性或弱点，添加说明
            if (resistances.Contains(damageType)) {
                damageDescription += "（抗性减半）";
            }
            else if (vulnerabilities.Contains(damageType)) {
                damageDescription += "（弱点加倍）";
            }

            // 如果有临时生命值抵消了部分伤害，添加说明
            if (temporaryHitPoints > 0) {
                damageDescription += "（部分被临时生命值抵消）";
            }

            // 添加战斗日志
            int currentRound = 0;
            if (CombatManager.Instance != null) {
                currentRound = CombatManager.Instance.currentRound;
            }
            DND_BattleUI.Instance.AddCombatLog($"[回合 {currentRound}] {GetDisplayName()} 受到 {damageDescription}，剩余生命值: {currentHitPoints}/{maxHitPoints}");

            // 如果生命值降为0，添加倒地日志
            if (currentHitPoints <= 0) {
                DND_BattleUI.Instance.AddCombatLog($"{GetDisplayName()} 已倒地！");
            }
        }

        // 确保UI更新
        try {
            if (DND_BattleUI.Instance != null) {
                Debug.Log($"在CharacterStats.TakeDamage中更新UI: {GetDisplayName()} 血量从 {oldHitPoints} 减少到 {currentHitPoints}");
                DND_BattleUI.Instance.UpdateCharacterStatusUI(this);
            }
            else {
                Debug.LogWarning("DND_BattleUI.Instance为null，无法更新UI");
            }
        }
        catch (System.Exception e) {
            Debug.LogError($"更新UI时出错: {e.Message}\n{e.StackTrace}");
        }

        // 检查是否失去意识
        if (currentHitPoints <= 0) {
            AddStatusEffect(DND5E.StatusEffectType.Unconscious);
            Debug.Log($"{GetDisplayName()} 失去意识!");

            // 再次确保UI更新
            if (DND_BattleUI.Instance != null) {
                DND_BattleUI.Instance.UpdateCharacterStatusUI(this);
            }
        }
    }

    // 恢复生命值
    public void HealDamage(int amount) {
        currentHitPoints = Mathf.Min(maxHitPoints, currentHitPoints + amount);
        Debug.Log($"{GetDisplayName()} 恢复 {amount} 点生命值! 当前生命值: {currentHitPoints}/{maxHitPoints}");

        // 更新UI中的血量显示
        if (DND_BattleUI.Instance != null) {
            DND_BattleUI.Instance.UpdateCharacterStatusUI(this);
        }

        // 如果恢复意识
        if (currentHitPoints > 0 && HasStatusEffect(DND5E.StatusEffectType.Unconscious)) {
            RemoveStatusEffect(DND5E.StatusEffectType.Unconscious);
            Debug.Log($"{GetDisplayName()} 恢复意识!");
        }
    }

    // 添加临时生命值
    public void AddTemporaryHitPoints(int amount) {
        // 临时生命值不叠加，取较高值
        temporaryHitPoints = Mathf.Max(temporaryHitPoints, amount);
        Debug.Log($"{GetDisplayName()} 获得 {amount} 点临时生命值! 当前临时生命值: {temporaryHitPoints}");
    }

    // 进行技能检定
    public int SkillCheck(DND5E.CharacterStats.Skill skill) {
        int bonus = stats.GetSkillBonus(skill, proficiencyBonus);
        int roll = Random.Range(1, 21);
        int total = roll + bonus;

        Debug.Log($"{GetDisplayName()} 进行 {skill} 检定: 掷骰 {roll} + 加值 {bonus} = {total}");
        return total;
    }

    // 进行豁免检定
    public int SavingThrow(string ability) {
        int bonus = stats.GetSavingThrowBonus(ability, proficiencyBonus);
        int roll = Random.Range(1, 21);
        int total = roll + bonus;

        Debug.Log($"{GetDisplayName()} 进行 {ability} 豁免检定: 掷骰 {roll} + 加值 {bonus} = {total}");
        return total;
    }

    // 进行攻击掷骰
    public int AttackRoll(string ability, bool hasAdvantage = false, bool hasDisadvantage = false) {
        int abilityMod = 0;

        switch (ability.ToLower()) {
            case "str":
            case "strength":
                abilityMod = stats.StrMod;
                break;

            case "dex":
            case "dexterity":
                abilityMod = stats.DexMod;
                break;

            case "int":
            case "intelligence":
                abilityMod = stats.IntMod;
                break;
        }

        int attackBonus = abilityMod + proficiencyBonus;

        // 掷骰
        int roll = 0;

        if (hasAdvantage && !hasDisadvantage) {
            // 优势：掷两次取高
            int roll1 = Random.Range(1, 21);
            int roll2 = Random.Range(1, 21);
            roll = Mathf.Max(roll1, roll2);
            Debug.Log($"{GetDisplayName()} 以优势进行攻击掷骰: {roll1} 和 {roll2}, 取 {roll}");
        }
        else if (!hasAdvantage && hasDisadvantage) {
            // 劣势：掷两次取低
            int roll1 = Random.Range(1, 21);
            int roll2 = Random.Range(1, 21);
            roll = Mathf.Min(roll1, roll2);
            Debug.Log($"{GetDisplayName()} 以劣势进行攻击掷骰: {roll1} 和 {roll2}, 取 {roll}");
        }
        else {
            // 正常：掷一次
            roll = Random.Range(1, 21);
            Debug.Log($"{GetDisplayName()} 进行攻击掷骰: {roll}");
        }

        int total = roll + attackBonus;
        Debug.Log($"攻击掷骰结果: {roll} + {attackBonus} = {total}");

        // 自然20重击，自然1失误
        if (roll == 20) Debug.Log("重击!");
        if (roll == 1) Debug.Log("失误!");

        return total;
    }

    // 计算伤害
    public int CalculateDamage(string ability, string damageFormula) {
        int abilityMod = 0;

        switch (ability.ToLower()) {
            case "str":
            case "strength":
                abilityMod = stats.StrMod;
                break;

            case "dex":
            case "dexterity":
                abilityMod = stats.DexMod;
                break;

            case "int":
            case "intelligence":
                abilityMod = stats.IntMod;
                break;
        }

        // 解析伤害公式，例如 "1d8"
        string[] parts = damageFormula.Split('d');
        if (parts.Length != 2) return abilityMod;

        int diceCount = int.Parse(parts[0]);
        int diceType = int.Parse(parts[1]);

        // 掷骰
        int damageRoll = 0;
        for (int i = 0; i < diceCount; i++) {
            damageRoll += Random.Range(1, diceType + 1);
        }

        int totalDamage = damageRoll + abilityMod;
        Debug.Log($"伤害掷骰: {damageRoll} + {abilityMod} = {totalDamage}");

        return totalDamage;
    }
}
