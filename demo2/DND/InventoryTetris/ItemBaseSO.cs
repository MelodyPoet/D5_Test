using UnityEngine;
using System.Collections.Generic;
using System;
using demo2.DND.Stats;
using demo2.DND;

namespace demo2.DND.InventoryTetris
{
    [CreateAssetMenu(menuName = "DND/Inventory/Item Base", fileName = "ItemBase_SO")]
    public class ItemBaseSO : ScriptableObject
    {
        [Header("基础信息")]
        public string itemId;
        public string displayName;
        public Sprite icon;
        [TextArea(3, 10)]
        [Tooltip("物品的详细描述，将显示在提示框中。")]
        public string description;

        [Tooltip("显式标记该物品是否可被装备（优先于 isWeapon/isArmor/isShield）。勾选后双击将触发装备/卸下；未勾选则视为非装备物品。保持向后兼容，若未勾选将回退到 isWeapon/isArmor/isShield 判定。")]
        public bool isEquippable = false;

        [Header("占用格子形状（单位：格）")]
        [Tooltip("定义物品形状，使用相对坐标列表。原点(0,0)是形状包围盒的左上角。例如，一个T形可以定义为 [(0,0), (1,0), (2,0), (1,1)]")]
        public List<Vector2Int> shapeCoords = new List<Vector2Int> { new Vector2Int(0, 0) };

        [Tooltip("是否允许旋转（90°）")]
        public bool canRotate = true;

        [Header("武器（可选）")]
        [Tooltip("勾选后，该物品作为武器参与普通物理攻击的命中与伤害计算；未勾选则按无武器规则进行。")]
        public bool isWeapon;
        [Tooltip("是否为灵巧武器（Finesse）：启用后命中与伤害能力按 STR/DEX 择优。")]
        public bool isFinesse;
        [Tooltip("物理普通攻击的命中能力选择模式：力量/敏捷/两者择优（仅当 isWeapon=true 时生效；若 isFinesse=true，此项将被视为 BestOfStrDex)")]
        public PhysicalHitAbilityMode weaponHitAbilityMode = PhysicalHitAbilityMode.BestOfStrDex;
        [Tooltip("武器伤害骰（例如 1d8、2d6）；暴击时仅翻倍骰子数量，不翻倍属性加值（当前实现沿用STR/DEX加值）")]
        public DiceFormula weaponDamageDice = new DiceFormula(1, 6);
        [Tooltip("武器伤害类型（用于日志与抗性结算）；未勾选 isWeapon 或不使用时可忽略")]
        public DamageType weaponDamageType = DamageType.Bludgeoning;
        [Header("武器高级（特殊场景，可选）")]
        [Tooltip("是否启用‘伤害能力’与‘命中能力’分离。缺省关闭时：伤害沿用命中所用的能力；启用后：可单独配置伤害能力模式。")]
        public bool useSeparateDamageAbility = false;
        [Tooltip("当启用‘伤害能力分离’时生效：力量/敏捷/两者择优（若 isFinesse=true，则仍按 STR/DEX 择优）")]
        public PhysicalHitAbilityMode weaponDamageAbilityMode = PhysicalHitAbilityMode.BestOfStrDex;

        [Header("护甲/盾牌（可选）")]
        [Tooltip("勾选后，该物品作为护甲参与AC计算")]
        public bool isArmor;
        [Tooltip("护甲类别：轻甲/中甲/重甲——决定敏捷加值的上限规则")]
        public ArmorType armorType = ArmorType.Light;
        [Tooltip("护甲基础AC（例如 轻甲 11，鳞甲 14，锁甲 16 等）")]
        public int armorBaseAC = 11;
        [Tooltip("勾选后，该物品作为盾牌提供额外AC加值（通常为+2）")]
        public bool isShield;
        [Tooltip("盾牌AC加值（标准+2）")]
        public int shieldACBonus = 2;

        [Header("外观换装（可选）")]
        [Tooltip("穿戴此物品时应用到角色身上的Spine皮肤ID（对应Spine中的皮肤名）。为空则不改变外观。")]
        public string appearanceSkinID;

        [Tooltip("此物品影响的外观部位。用于确定覆盖范围，以及卸下时的回退处理。")]
        public EquipmentSlot appearanceSlot;

        [Tooltip("外观行为：Cover（覆盖型，如头盔/铠甲）或 Overlay（叠加型，如头环/王冠）")]
        public EquipmentAppearanceBehavior appearanceBehavior = EquipmentAppearanceBehavior.None;

        [Header("属性加成（装备在背包中即生效，按 WhileEquipped 层应用）")]
        [Tooltip("为该物品配置若干条属性修饰，进入背包后在战斗中生效；移出背包即移除。")]
        public List<ModifierData> modifiers = new List<ModifierData>();

        [Serializable]
        public struct ModifierData
        {
            public StatType stat;
            public ModifierOp op;
            public float value;
            public string stackKey;
            public StackPolicy policy;
            public ModifierLayer layer;
            public DurationType durationType; // 建议设置为 WhileEquipped；允许配置兼容
            public float seconds;
            public int rounds;

            public StatModifier ToRuntime(object source)
            {
                var m = new StatModifier(stat, op, value, stackKey, policy, layer);
                m.source = source;
                m.durationType = durationType;
                m.seconds = seconds;
                m.rounds = rounds;
                // WhileEquipped 统一由背包驱动移除，防止计时移除
                if (durationType == DurationType.WhileEquipped)
                {
                    m.removeOnExpire = false;
                }
                return m;
            }
        }

        public IEnumerable<StatModifier> BuildRuntimeModifiers(object source)
        {
            if (modifiers == null) yield break;
            for (int i = 0; i < modifiers.Count; i++)
            {
                yield return modifiers[i].ToRuntime(source);
            }
        }

        public string GetTooltipInfo()
        {
            if (string.IsNullOrEmpty(description))
            {
                return displayName;
            }
            return $"{displayName}\n{description}";
        }
    }
}
