using System;
using System.Collections.Generic;
using demo2.DND.InventoryTetris; // access CharacterInventory & ItemBaseSO

namespace demo2.DND.Stats
{
    /// <summary>
    /// 最小可用“修正聚合器”骨架：
    /// - 仅支持少量 StatType（六维/AC/MaxHP/熟练）；
    /// - 支持 Add/Multiply/Override/Flag；
    /// - 支持层级顺序与简单 stackKey 策略；
    /// - 暂不包含抗性/优势等集合类细化（后续扩展）。
    /// </summary>
    public class ModifierAggregator
    {
        private readonly List<StatModifier> modifiers = new List<StatModifier>();

        public IReadOnlyList<StatModifier> Modifiers => modifiers;

        public void Add(StatModifier mod)
        {
            if (mod == null) return;
            modifiers.Add(mod);
        }

        public void RemoveBySource(object source)
        {
            if (source == null) return;
            modifiers.RemoveAll(m => ReferenceEquals(m.source, source));
        }

        public bool PurgeExpired()
        {
            int before = modifiers.Count;
            modifiers.RemoveAll(m => m.IsExpired && m.removeOnExpire);
            return modifiers.Count != before;
        }

        public void TickSeconds(float delta)
        {
            if (delta <= 0f) return;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var m = modifiers[i];
                if (m.durationType == DurationType.TimedSeconds)
                {
                    m.seconds -= delta;
                }
            }
        }

        public void TickRounds(int count = 1)
        {
            if (count <= 0) return;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var m = modifiers[i];
                if (m.durationType == DurationType.TimedRounds)
                {
                    m.rounds -= count;
                }
            }
        }

        public FinalStatsSnapshot Recalculate(CharacterStats owner)
        {
            // 基准：从实例当前字段读取（后续可换为“模板 + 规则公式”）
            int baseStr = owner.strength;
            int baseDex = owner.dexterity;
            int baseCon = owner.constitution;
            int baseInt = owner.intelligence;
            int baseWis = owner.wisdom;
            int baseCha = owner.charisma;

            int baseUnarmoredAc = owner.template != null ? owner.template.baseArmorClass : owner.armorClass;
            int baseMaxHp = owner.MaxHitPoints;
            int baseProf = owner.template != null ? owner.template.GetProficiencyBonusByLevel(owner.Level) : 2;

            // 各属性的叠加桶
            var add = new Dictionary<StatType, float>();
            var mul = new Dictionary<StatType, float>();
            var ovw = new Dictionary<StatType, float>();

            // 初始化乘法为1
            void Ensure(StatType t)
            {
                if (!add.ContainsKey(t)) add[t] = 0f;
                if (!mul.ContainsKey(t)) mul[t] = 1f;
            }

            Ensure(StatType.Strength);
            Ensure(StatType.Dexterity);
            Ensure(StatType.Constitution);
            Ensure(StatType.Intelligence);
            Ensure(StatType.Wisdom);
            Ensure(StatType.Charisma);
            Ensure(StatType.ArmorClass);
            Ensure(StatType.MaxHitPoints);
            Ensure(StatType.ProficiencyBonus);

            // 应用顺序：Permanent → Equipment → Effect → Situational
            var ordered = new List<StatModifier>(modifiers);
            ordered.Sort((a, b) => a.layer.CompareTo(b.layer));

            // 聚合（只应用当前对所有者有效的修正）
            for (int i = 0; i < ordered.Count; i++)
            {
                var m = ordered[i];
                if (m == null) continue;
                if (!m.IsActiveFor(owner)) continue;

                // 最小骨架：先直接按 op 聚合；后续可分组 stackKey 精细化。
                Ensure(m.stat);
                switch (m.op)
                {
                    case ModifierOp.Add:
                        add[m.stat] = add[m.stat] + m.value;
                        break;
                    case ModifierOp.Multiply:
                        mul[m.stat] = mul[m.stat] * m.value;
                        break;
                    case ModifierOp.Override:
                        ovw[m.stat] = m.value;
                        break;
                    case ModifierOp.Flag:
                        // 旗标在本骨架暂不细化，后续扩展 Advantage/Resist 等集合
                        break;
                }
            }

            int ApplyInt(StatType t, int baseVal)
            {
                float v = baseVal;
                if (ovw.TryGetValue(t, out float ov))
                {
                    v = ov;
                }
                v = (v + (add.TryGetValue(t, out float a) ? a : 0f)) * (mul.TryGetValue(t, out float m) ? m : 1f);
                return (int)Math.Round(v);
            }

            // 先计算六维等基础数值（以便得到 DexMod 用于 AC 计算）
            int finalStr = ApplyInt(StatType.Strength, baseStr);
            int finalDex = ApplyInt(StatType.Dexterity, baseDex);
            int finalCon = ApplyInt(StatType.Constitution, baseCon);
            int finalInt = ApplyInt(StatType.Intelligence, baseInt);
            int finalWis = ApplyInt(StatType.Wisdom, baseWis);
            int finalCha = ApplyInt(StatType.Charisma, baseCha);

            int dexMod = (finalDex - 10) / 2;

            // 根据装备/背包计算 5e AC：仅使用装备栏（护甲/盾牌）；未装备按未着甲规则
            int acFromEquipment = baseUnarmoredAc;
            int shieldBonus = 0;
            ItemBaseSO armor = null;
            ItemBaseSO shield = null;

            // 仅：装备栏
            var eq = owner.GetComponent<CharacterEquipment>()
                     ?? owner.GetComponentInParent<CharacterEquipment>()
                     ?? owner.GetComponentInChildren<CharacterEquipment>(true);
            if (eq != null)
            {
                if (eq.armor != null && eq.armor.data != null && eq.armor.data.isArmor)
                {
                    armor = eq.armor.data;
                }
                if (eq.shield != null && eq.shield.data != null && eq.shield.data.isShield)
                {
                    shield = eq.shield.data;
                }
            }

            if (shield != null)
            {
                shieldBonus = shield.shieldACBonus;
            }

            if (armor != null)
            {
                int dexCap = 0;
                switch (armor.armorType)
                {
                    case ArmorType.Light:
                        dexCap = dexMod; // 全额
                        break;
                    case ArmorType.Medium:
                        dexCap = Math.Min(dexMod, 2); // 上限+2（负敏捷保留）
                        break;
                    case ArmorType.Heavy:
                        dexCap = 0; // 不加敏捷
                        break;
                }
                acFromEquipment = Math.Max(0, armor.armorBaseAC) + dexCap + shieldBonus;
            }
            else
            {
                // 未穿甲：使用基础未着甲AC + Dex + 盾（若有装备盾牌）
                acFromEquipment = Math.Max(0, baseUnarmoredAc) + dexMod + shieldBonus;
            }

            // 状态：闪避（Dodging）给 +2 AC
            if (owner != null && owner.HasStatusEffect(StatusEffectType.Dodging))
            {
                acFromEquipment += 2;
            }

            // 将 AC 修饰应用到装备公式上（Override -> Add -> Multiply）
            float finalAcF = acFromEquipment;
            if (ovw.TryGetValue(StatType.ArmorClass, out float ovAc)) finalAcF = ovAc;
            finalAcF = (finalAcF + (add.TryGetValue(StatType.ArmorClass, out float addAc) ? addAc : 0f)) * (mul.TryGetValue(StatType.ArmorClass, out float mulAc) ? mulAc : 1f);
            int finalAc = (int)Math.Round(finalAcF);

            FinalStatsSnapshot snap = new FinalStatsSnapshot
            {
                strength = finalStr,
                dexterity = finalDex,
                constitution = finalCon,
                intelligence = finalInt,
                wisdom = finalWis,
                charisma = finalCha,
                armorClass = finalAc,
                maxHitPoints = ApplyInt(StatType.MaxHitPoints, baseMaxHp),
                proficiencyBonus = ApplyInt(StatType.ProficiencyBonus, baseProf)
            };

            return snap;
        }
    }
}
