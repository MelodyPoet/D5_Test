using UnityEngine;

namespace demo2.DND
{
    /// <summary>
    /// 法术数据 - 用于定义普通法术/戏法的伤害骰、伤害类型、等级升级规则等
    /// 通过 ScriptableObject 创建，并在 CharacterTemplate.defaultCantrip 中引用
    /// </summary>
    [CreateAssetMenu(fileName = "New Spell Data", menuName = "DND/Spell Data")]
    public class SpellData : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("法术名称（如 Fire Bolt、Shocking Grasp 等）")]
        public string spellName = "New Spell";

        [Tooltip("法术描述")]
        [TextArea(2, 4)]
        public string spellDescription = "";

        [Header("伤害配置")]
        [Tooltip("基础伤害骰（如 1d10、2d6 等）")]
        public DiceFormula baseDamageDice = new DiceFormula(1, 10);

        [Tooltip("伤害类型（Fire/Cold/Force/Psychic/等）")]
        public DamageType damageType = DamageType.Force;

        [Header("等级升级规则")]
        [Tooltip("是否在特定施法者等级升级伤害骰（如 5级升至 2d10、11级升至 3d10 等）")]
        public bool upgradeAtCantriplevel = true;

        [Tooltip("升级规则：等级阈值与对应伤害骰")]
        [SerializeField]
        private DamageUpgradeEntry[] upgradeEntries;

        private void OnEnable()
        {
            // 在首次启用时初始化升级规则（如果为空）
            if (upgradeEntries == null || upgradeEntries.Length == 0)
            {
                upgradeEntries = new DamageUpgradeEntry[4];

                upgradeEntries[0] = new DamageUpgradeEntry { characterLevel = 1, upgradedDice = new DiceFormula(1, 10) };
                upgradeEntries[1] = new DamageUpgradeEntry { characterLevel = 5, upgradedDice = new DiceFormula(2, 10) };
                upgradeEntries[2] = new DamageUpgradeEntry { characterLevel = 11, upgradedDice = new DiceFormula(3, 10) };
                upgradeEntries[3] = new DamageUpgradeEntry { characterLevel = 17, upgradedDice = new DiceFormula(4, 10) };
            }
        }

        /// <summary>
        /// 根据施法者等级获取伤害骰
        /// </summary>
        public DiceFormula GetDamageDiceAtCasterLevel(int casterLevel)
        {
            // 确保 baseDamageDice 初始化
            if (baseDamageDice == null)
                baseDamageDice = new DiceFormula(1, 10);

            if (!upgradeAtCantriplevel || upgradeEntries == null || upgradeEntries.Length == 0)
                return baseDamageDice;

            // 倒序遍历，找到满足条件的最高等级阈值
            for (int i = upgradeEntries.Length - 1; i >= 0; i--)
            {
                if (casterLevel >= upgradeEntries[i].characterLevel)
                {
                    if (upgradeEntries[i].upgradedDice != null)
                        return upgradeEntries[i].upgradedDice;
                }
            }

            return baseDamageDice;
        }

        /// <summary>
        /// 检查是否需要投攻击检定（大多数法术需要，但某些法术可能不需要）
        /// </summary>
        [Header("规则")]
        [Tooltip("是否需要投攻击检定（大多数戏法为 true）")]
        public bool requiresAttackRoll = true;

        [Tooltip("若无需攻击检定，则使用该属性进行豁免（如 dexterity / constitution / wisdom）")]
        public string saveAbility = "constitution";

        [Tooltip("豁免成功是否减半伤害；为 false 则成功时伤害为 0")]
        public bool saveHalvesOnSuccess = true;

        [Tooltip("攻击类型（如 attack roll 或 saving throw）")]
        public string attackType = "Spell Attack";

        /// <summary>
        /// 升级规则数据结构
        /// </summary>
        [System.Serializable]
        public class DamageUpgradeEntry
        {
            [Tooltip("升级触发的施法者等级")]
            public int characterLevel = 1;

            [Tooltip("该等级对应的伤害骰")]
            public DiceFormula upgradedDice;
        }
    }
}
