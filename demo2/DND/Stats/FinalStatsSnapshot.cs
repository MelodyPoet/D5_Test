using System;

namespace demo2.DND.Stats
{
    [Serializable]
    public struct FinalStatsSnapshot
    {
        public int strength;
        public int dexterity;
        public int constitution;
        public int intelligence;
        public int wisdom;
        public int charisma;

        public int armorClass;
        public int maxHitPoints;
        public int proficiencyBonus;

        public int StrMod => PointBuySystem.GetModifier(strength);
        public int DexMod => PointBuySystem.GetModifier(dexterity);
        public int ConMod => PointBuySystem.GetModifier(constitution);
        public int IntMod => PointBuySystem.GetModifier(intelligence);
        public int WisMod => PointBuySystem.GetModifier(wisdom);
        public int ChaMod => PointBuySystem.GetModifier(charisma);
    }
}

