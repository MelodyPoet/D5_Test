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

        public int StrMod => (strength - 10) / 2;
        public int DexMod => (dexterity - 10) / 2;
        public int ConMod => (constitution - 10) / 2;
        public int IntMod => (intelligence - 10) / 2;
        public int WisMod => (wisdom - 10) / 2;
        public int ChaMod => (charisma - 10) / 2;
    }
}

