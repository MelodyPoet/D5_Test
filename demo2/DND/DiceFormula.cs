using UnityEngine;

namespace demo2.DND
{
    /// <summary>
    /// 骰子公式 - 表示 DND5E 中的伤害/治疗骰数（如 1d6、2d8+3 等）
    /// </summary>
    [System.Serializable]
    public class DiceFormula
    {
        [Tooltip("骰子个数（如 2d6 中的 2）")]
        public int diceCount = 1;

        [Tooltip("骰子面数（如 2d6 中的 6）")]
        public int diceSize = 4;

        [Tooltip("固定修正值（如 2d6+3 中的 +3）")]
        public int modifier = 0;

        // 无参构造函数
        public DiceFormula()
        {
            diceCount = 1;
            diceSize = 4;
            modifier = 0;
        }

        public DiceFormula(int count, int size, int mod = 0)
        {
            diceCount = Mathf.Max(1, count);
            diceSize = Mathf.Max(1, size);
            modifier = mod;
        }

        /// <summary>
        /// 获取骰子公式的文字表示（如 "2d6+3"）
        /// </summary>
        public override string ToString()
        {
            if (modifier == 0)
                return $"{diceCount}d{diceSize}";
            else if (modifier > 0)
                return $"{diceCount}d{diceSize}+{modifier}";
            else
                return $"{diceCount}d{diceSize}{modifier}";
        }

        /// <summary>
        /// 计算该骰子公式的期望值（平均伤害）
        /// </summary>
        public float GetExpectedValue()
        {
            float averageDamagePerDice = (diceSize + 1) / 2f;
            return diceCount * averageDamagePerDice + modifier;
        }
    }
}

