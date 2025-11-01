using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using demo2.DND.InventoryTetris;
using demo2.DND.Stats;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 横版战斗规则系统：先攻、命中与伤害结算。
    /// 约束：只读取装备栏（CharacterEquipment）中的武器/护甲/盾牌；未装备时一律按“默认徒手/未着甲”处理，与背包内容无关。
    /// </summary>
    public static class HorizontalCombatRules
    {
        // ---------- 基础工具 ----------
        private static FinalStatsSnapshot? TryGetSnapshot(CharacterStats c)
        {
            return c != null ? c.CurrentSnapshot : (FinalStatsSnapshot?)null;
        }
        private static int GetAbilityModifier(int score) => (score - 10) / 2;
        private static string NormalizeAbilityName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "strength";
            string s = input.Trim().ToLowerInvariant();
            if (s.StartsWith("str")) return "strength";
            if (s.StartsWith("dex")) return "dexterity";
            if (s.StartsWith("con")) return "constitution";
            if (s.StartsWith("int")) return "intelligence";
            if (s.StartsWith("wis")) return "wisdom";
            if (s.StartsWith("cha")) return "charisma";
            switch (s)
            {
                case "strength":
                case "dexterity":
                case "constitution":
                case "intelligence":
                case "wisdom":
                case "charisma":
                    return s;
                default: return "strength";
            }
        }
        private static int GetAbilityModifierFromSnapshot(FinalStatsSnapshot? snap, CharacterStats c, string abilityName)
        {
            string n = NormalizeAbilityName(abilityName);
            if (snap.HasValue)
            {
                switch (n)
                {
                    case "strength": return snap.Value.StrMod;
                    case "dexterity": return snap.Value.DexMod;
                    case "constitution": return snap.Value.ConMod;
                    case "intelligence": return snap.Value.IntMod;
                    case "wisdom": return snap.Value.WisMod;
                    case "charisma": return snap.Value.ChaMod;
                    default: return 0;
                }
            }
            else
            {
                switch (n)
                {
                    case "strength": return GetAbilityModifier(c != null ? c.strength : 10);
                    case "dexterity": return GetAbilityModifier(c != null ? c.dexterity : 10);
                    case "constitution": return GetAbilityModifier(c != null ? c.constitution : 10);
                    case "intelligence": return GetAbilityModifier(c != null ? c.intelligence : 10);
                    case "wisdom": return GetAbilityModifier(c != null ? c.wisdom : 10);
                    case "charisma": return GetAbilityModifier(c != null ? c.charisma : 10);
                    default: return 0;
                }
            }
        }

        // ---------- 先攻 ----------
        public static List<InitiativeEntry> RollAndSortInitiative(List<CharacterStats> combatants)
        {
            var list = new List<InitiativeEntry>();
            if (combatants == null) return list;

            foreach (var c in combatants)
            {
                if (c == null) continue;
                var snap = TryGetSnapshot(c);
                int dexMod = snap.HasValue ? snap.Value.DexMod : GetAbilityModifier(c.dexterity);
                int roll = Random.Range(1, 21);
                int total = roll + dexMod;
                list.Add(new InitiativeEntry(c, total));
                Debug.Log($"{c.GetDisplayName()} 先攻检定: {roll} + {dexMod} = {total}");
                try { GameLog.LogInitiative(c.GetDisplayName(), roll, dexMod, total); } catch { }
            }

            return list.OrderByDescending(e => e.initiativeValue).ToList();
        }

        // ---------- 攻击 ----------
        public struct AttackResult
        {
            public bool isHit;
            public int damage;
            public bool isCritical;
            public string description;
            public DamageType damageType;
        }

        public static AttackResult ResolveAttack(CharacterStats attacker, CharacterStats target, int advantageFlag = 0, bool isMeleeAttack = true)
        {
            AttackResult r = new AttackResult { isHit = false, damage = 0, isCritical = false, description = "" };
            if (attacker == null || target == null) { r.description = "无效攻击"; return r; }

            bool isSpell = attacker.template != null && attacker.template.defaultAttackType == DefaultAttackType.Spell;
            var weapon = isSpell ? null : FindEquippedWeapon(attacker);

            string hitAbility;
            string attackName;
            int attackBonus = GetAttackBonus(attacker, isMeleeAttack, out hitAbility, out attackName, weapon);

            int roll1 = Random.Range(1, 21);
            int roll2 = Random.Range(1, 21);
            int d20 = (advantageFlag > 0) ? Mathf.Max(roll1, roll2) : (advantageFlag < 0 ? Mathf.Min(roll1, roll2) : roll1);
            int totalAttack = d20 + attackBonus;

            r.isCritical = (advantageFlag > 0) ? (roll1 == 20 || roll2 == 20) : (advantageFlag < 0 ? (roll1 == 20 && roll2 == 20) : (d20 == 20));

            var tsnap = TryGetSnapshot(target);
            int targetAc = tsnap.HasValue ? tsnap.Value.armorClass : target.armorClass;
            r.isHit = r.isCritical || totalAttack >= targetAc;

            try { GameLog.LogHit(attacker.GetDisplayName(), target.GetDisplayName(), attackName, hitAbility, d20, attackBonus, totalAttack, targetAc, r.isHit); } catch { }

            if (!r.isHit)
            {
                r.description = "攻击未命中";
                return r;
            }

            int diceSize, rolled, dmgAbilityMod;
            string dmgAbilityName;
            r.damage = CalculateDamageUnified(attacker, r.isCritical, isSpell, hitAbility, out diceSize, out rolled, out dmgAbilityMod, weapon, out dmgAbilityName);
            r.description = r.isCritical ? $"暴击命中！伤害: {r.damage}" : $"命中！伤害: {r.damage}";
            r.damageType = isSpell
                ? ((attacker.template != null && attacker.template.defaultCantrip != null) ? attacker.template.defaultCantrip.damageType : DamageType.Force)
                : (weapon != null ? weapon.weaponDamageType : DamageType.Bludgeoning);

            int baseDice = isSpell
                ? (attacker.template != null && attacker.template.defaultCantrip != null ? attacker.template.defaultCantrip.GetDamageDiceAtCasterLevel(attacker.Level).diceCount : 1)
                : (weapon != null ? Mathf.Max(1, weapon.weaponDamageDice.diceCount) : (attacker.template != null ? Mathf.Max(1, attacker.template.unarmedDamageDice.diceCount) : 1));
            string diceExpr = (r.isCritical ? baseDice * 2 : baseDice) + "d" + diceSize + (r.isCritical ? "（暴击）" : "");

            try
            {
                if (isSpell)
                    GameLog.LogDamage(attacker.GetDisplayName(), target.GetDisplayName(), r.damageType.ToString(), diceExpr, rolled, "未应用抗性/易伤", r.damage);
                else
                    GameLog.LogDamage(attacker.GetDisplayName(), target.GetDisplayName(), r.damageType.ToString(), diceExpr, rolled, dmgAbilityName, dmgAbilityMod, "未应用抗性/易伤", r.damage);
            }
            catch { }

            return r;
        }

        // 仅读取装备栏主手；未装备=>null（徒手）
        private static ItemBaseSO FindEquippedWeapon(CharacterStats c)
        {
            if (c == null) return null;
            var eq = c.GetComponent<CharacterEquipment>()
                     ?? c.GetComponentInParent<CharacterEquipment>()
                     ?? c.GetComponentInChildren<CharacterEquipment>(true);
            if (eq != null && eq.mainHand != null && eq.mainHand.data != null && eq.mainHand.data.isWeapon)
                return eq.mainHand.data;
            return null;
        }

        private static int GetAttackBonus(CharacterStats c, bool isMelee, out string abilityNameForHit, out string attackTypeName, ItemBaseSO weapon = null)
        {
            abilityNameForHit = "strength";
            attackTypeName = "物理普通攻击";
            var snap = TryGetSnapshot(c);

            bool isSpell = c.template != null && c.template.defaultAttackType == DefaultAttackType.Spell;
            if (isSpell)
            {
                attackTypeName = (c.template != null && c.template.defaultCantrip != null && !string.IsNullOrEmpty(c.template.defaultCantrip.spellName)) ? c.template.defaultCantrip.spellName : "法术普通攻击";
                abilityNameForHit = NormalizeAbilityName(c.template != null ? c.template.primarySpellAbility : "intelligence");
                int mod = GetAbilityModifierFromSnapshot(snap, c, abilityNameForHit);
                int prof = (c.template != null && c.template.IsProficientForAttack(true, isMelee)) ? c.template.GetProficiencyBonusByLevel(c.Level) : 0;
                return mod + prof;
            }

            int str = GetAbilityModifierFromSnapshot(snap, c, "strength");
            int dex = GetAbilityModifierFromSnapshot(snap, c, "dexterity");

            if (weapon != null && weapon.isWeapon)
            {
                var mode = weapon.isFinesse ? PhysicalHitAbilityMode.BestOfStrDex : weapon.weaponHitAbilityMode;
                switch (mode)
                {
                    case PhysicalHitAbilityMode.Strength: abilityNameForHit = "strength"; break;
                    case PhysicalHitAbilityMode.Dexterity: abilityNameForHit = "dexterity"; break;
                    default: abilityNameForHit = (dex > str) ? "dexterity" : "strength"; break;
                }
                int mod = abilityNameForHit == "dexterity" ? dex : str;
                int prof = (c.template != null && c.template.IsProficientForAttack(false, isMelee)) ? c.template.GetProficiencyBonusByLevel(c.Level) : 0;
                return mod + prof;
            }
            else
            {
                // 徒手命中：取 STR/DEX 较大者；是否加熟练由模板 unarmedProficient 决定（5e：默认加熟练）
                abilityNameForHit = (dex > str) ? "dexterity" : "strength";
                int mod = (dex > str) ? dex : str;
                int prof = (c.template != null && c.template.unarmedProficient) ? c.template.GetProficiencyBonusByLevel(c.Level) : 0;
                return mod + prof;
            }
        }

        private static int CalculateDamageUnified(CharacterStats c, bool isCritical, bool isSpell, string damageAbilityFromHit, out int diceSize, out int rolledTotal, out int abilityModForDmg, ItemBaseSO weapon, out string damageAbilityName)
        {
            rolledTotal = 0;
            abilityModForDmg = 0;
            damageAbilityName = "strength";

            if (isSpell)
            {
                DiceFormula dice = (c.template != null && c.template.defaultCantrip != null) ? c.template.defaultCantrip.GetDamageDiceAtCasterLevel(c.Level) : new DiceFormula { diceCount = 1, diceSize = 8 };
                int count = isCritical ? dice.diceCount * 2 : dice.diceCount;
                diceSize = dice.diceSize;
                for (int i = 0; i < count; i++) rolledTotal += Random.Range(1, diceSize + 1);
                return Mathf.Max(1, rolledTotal);
            }

            // 物理
            DiceFormula pdice = (weapon != null && weapon.isWeapon)
                ? new DiceFormula { diceCount = Mathf.Max(1, weapon.weaponDamageDice.diceCount), diceSize = Mathf.Max(2, weapon.weaponDamageDice.diceSize) }
                : (c.template != null
                    ? new DiceFormula { diceCount = Mathf.Max(1, c.template.unarmedDamageDice.diceCount), diceSize = Mathf.Max(2, c.template.unarmedDamageDice.diceSize) }
                    : new DiceFormula { diceCount = 1, diceSize = 6 });
            int pcount = isCritical ? pdice.diceCount * 2 : pdice.diceCount;
            diceSize = pdice.diceSize;
            for (int i = 0; i < pcount; i++) rolledTotal += Random.Range(1, diceSize + 1);

            var snap = TryGetSnapshot(c);
            int str = GetAbilityModifierFromSnapshot(snap, c, "strength");
            int dex = GetAbilityModifierFromSnapshot(snap, c, "dexterity");

            if (weapon != null && weapon.isWeapon)
            {
                if (weapon.useSeparateDamageAbility)
                {
                    var mode = weapon.isFinesse ? PhysicalHitAbilityMode.BestOfStrDex : weapon.weaponDamageAbilityMode;
                    switch (mode)
                    {
                        case PhysicalHitAbilityMode.Strength: damageAbilityName = "strength"; abilityModForDmg = str; break;
                        case PhysicalHitAbilityMode.Dexterity: damageAbilityName = "dexterity"; abilityModForDmg = dex; break;
                        default: damageAbilityName = (dex > str) ? "dexterity" : "strength"; abilityModForDmg = (dex > str) ? dex : str; break;
                    }
                }
                else
                {
                    string hit = NormalizeAbilityName(damageAbilityFromHit);
                    if (hit == "dexterity") { damageAbilityName = "dexterity"; abilityModForDmg = dex; }
                    else { damageAbilityName = "strength"; abilityModForDmg = str; }
                }
            }
            else
            {
                // 徒手伤害：按模板配置的徒手能力模式取加值（默认 STR）
                var mode = c.template != null ? c.template.unarmedDamageAbilityMode : PhysicalHitAbilityMode.Strength;
                switch (mode)
                {
                    case PhysicalHitAbilityMode.Dexterity:
                        damageAbilityName = "dexterity"; abilityModForDmg = dex; break;
                    case PhysicalHitAbilityMode.BestOfStrDex:
                        if (dex > str) { damageAbilityName = "dexterity"; abilityModForDmg = dex; }
                        else { damageAbilityName = "strength"; abilityModForDmg = str; }
                        break;
                    default:
                        damageAbilityName = "strength"; abilityModForDmg = str; break;
                }
            }

            return Mathf.Max(1, rolledTotal + abilityModForDmg);
        }
    }
}
