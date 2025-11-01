using System;
using demo2.DND; // for CharacterStats reference

namespace demo2.DND.Stats
{
    [Serializable]
    public class StatModifier
    {
        public StatType stat;
        public ModifierOp op = ModifierOp.Add;
        public float value;
        public ModifierLayer layer = ModifierLayer.Effect;

        // 冲突/堆叠控制
        public string stackKey; // 同key用于去重或取最大/最新
        public StackPolicy policy = StackPolicy.Sum;

        // 持续时间（秒/回合/集中/装备）
        public DurationType durationType = DurationType.Instant;
        public float seconds;     // TimedSeconds
        public int rounds;        // TimedRounds
        public bool removeOnExpire = true;

        // 来源对象（装备/专长/效果/技能等）
        public object source;

        // 可选条件：返回true时生效
        public Func<CharacterStats, bool> condition;

        public StatModifier() { }

        public StatModifier(StatType stat, ModifierOp op, float value, string stackKey = null, StackPolicy policy = StackPolicy.Sum, ModifierLayer layer = ModifierLayer.Effect)
        {
            this.stat = stat;
            this.op = op;
            this.value = value;
            this.stackKey = stackKey;
            this.policy = policy;
            this.layer = layer;
        }

        public StatModifier WithDurationSeconds(float sec)
        {
            durationType = DurationType.TimedSeconds;
            seconds = sec;
            return this;
        }

        public StatModifier WithDurationRounds(int r)
        {
            durationType = DurationType.TimedRounds;
            rounds = r;
            return this;
        }

        public bool IsExpired => durationType == DurationType.Instant ? false : (durationType == DurationType.TimedSeconds ? seconds <= 0f : (durationType == DurationType.TimedRounds ? rounds <= 0 : false));

        public bool IsActiveFor(CharacterStats owner)
        {
            if (condition == null) return true;
            try { return condition(owner); }
            catch { return false; }
        }
    }
}
