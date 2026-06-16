using System;
using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND
{
    /// <summary>
    /// D&D 5E 27点购点法系统
    ///
    /// 规则：
    /// - 初始值：六维属性各从 8 开始
    /// - 总点数：27 点可分配
    /// - 每提升1点属性消耗点数：
    ///     8→9: 1点,  9→10: 1点, 10→11: 1点, 11→12: 1点, 12→13: 1点
    ///    13→14: 2点, 14→15: 2点
    /// - 属性上限：15（购点阶段，不含种族加成）
    /// - 属性下限：8（不能低于初始值）
    ///
    /// 种族加成（变体人类 Variant Human）：
    /// - 自选两个不同属性各+1（上限20），不赠送专长
    ///
    /// 使用方式：
    ///   1. new PointBuySystem() 创建实例，自动初始化六维为8
    ///   2. IncreaseStat(statType) / DecreaseStat(statType) 加减属性
    ///   3. GetAvailablePoints() 查看剩余点数
    ///   4. ApplyRacialBonus() 应用种族加成
    ///   5. SetRacialBonusChoice() / ClearRacialBonusChoice() 选择种族加成分配目标
    ///   6. GetFinalStats() 获取最终属性值
    /// </summary>
    public class PointBuySystem
    {
        public const int StartingValue = 8;
        public const int MaxBuyable = 15;
        public const int MinValue = 8;
        public const int MaxAfterRacial = 20;
        public const int TotalPoints = 27;

        /// <summary>
        /// 变体人类可自选的种族加成数量
        /// </summary>
        public const int VariantHumanBonusCount = 2;
        public const int VariantHumanBonusPerStat = 1;

        /// <summary>
        /// 种族类型定义
        /// </summary>
        public enum RaceType
        {
            Human  // 变体人类：自选两个不同属性各+1
        }

        /// <summary>
        /// 当前六维属性（购点阶段，不含种族加成）
        /// </summary>
        public int Strength { get; private set; }
        public int Dexterity { get; private set; }
        public int Constitution { get; private set; }
        public int Intelligence { get; private set; }
        public int Wisdom { get; private set; }
        public int Charisma { get; private set; }

        /// <summary>
        /// 种族加成后的最终值（购点值 + 种族加成）
        /// </summary>
        public int FinalStrength => Mathf.Min(MaxAfterRacial, Strength + GetRacialBonus(StatType.Strength));
        public int FinalDexterity => Mathf.Min(MaxAfterRacial, Dexterity + GetRacialBonus(StatType.Dexterity));
        public int FinalConstitution => Mathf.Min(MaxAfterRacial, Constitution + GetRacialBonus(StatType.Constitution));
        public int FinalIntelligence => Mathf.Min(MaxAfterRacial, Intelligence + GetRacialBonus(StatType.Intelligence));
        public int FinalWisdom => Mathf.Min(MaxAfterRacial, Wisdom + GetRacialBonus(StatType.Wisdom));
        public int FinalCharisma => Mathf.Min(MaxAfterRacial, Charisma + GetRacialBonus(StatType.Charisma));

        /// <summary>
        /// 剩余可分配点数
        /// </summary>
        public int AvailablePoints => TotalPoints - SpentPoints;

        /// <summary>
        /// 已消耗点数
        /// </summary>
        public int SpentPoints { get; private set; }

        /// <summary>
        /// 是否已应用种族加成
        /// </summary>
        public bool RacialBonusApplied { get; private set; }
        private RaceType _selectedRace = RaceType.Human;

        /// <summary>
        /// 变体人类种族加成选择：记录玩家选了哪两个属性+1
        /// 最多 2 个，且不能重复
        /// </summary>
        private readonly List<StatType> _racialBonusChoices = new List<StatType>();

        /// <summary>
        /// 获取已选的种族加成属性列表（只读）
        /// </summary>
        public IReadOnlyList<StatType> RacialBonusChoices => _racialBonusChoices;

        /// <summary>
        /// 种族加成是否已选满（变体人类需选2个）
        /// </summary>
        public bool RacialBonusChoicesFull => _racialBonusChoices.Count >= VariantHumanBonusCount;

        public event Action<StatType, int> OnStatChanged;  // 参数：属性类型, 新值
        public event Action<int> OnPointsChanged;           // 参数：剩余点数
        public event Action OnRacialBonusChanged;           // 种族加成选择变化

        public PointBuySystem()
        {
            Reset();
        }

        /// <summary>
        /// 重置为初始状态（所有属性=8，点数=27）
        /// </summary>
        public void Reset()
        {
            Strength = StartingValue;
            Dexterity = StartingValue;
            Constitution = StartingValue;
            Intelligence = StartingValue;
            Wisdom = StartingValue;
            Charisma = StartingValue;
            SpentPoints = 0;
            RacialBonusApplied = false;
            _selectedRace = RaceType.Human;
            _racialBonusChoices.Clear();
        }

        /// <summary>
        /// 从模板的默认属性值加载（覆盖购点初始值）
        /// 例如：战士模板默认 STR=15, DEX=13... → 直接设置为这些值并计算点数消耗
        /// </summary>
        public void LoadFromDefaults(int str, int dex, int con, int intel, int wis, int cha)
        {
            Reset();
            // 直接设置（绕过增减逻辑，计算点数消耗）
            Strength = Mathf.Clamp(str, MinValue, MaxBuyable);
            Dexterity = Mathf.Clamp(dex, MinValue, MaxBuyable);
            Constitution = Mathf.Clamp(con, MinValue, MaxBuyable);
            Intelligence = Mathf.Clamp(intel, MinValue, MaxBuyable);
            Wisdom = Mathf.Clamp(wis, MinValue, MaxBuyable);
            Charisma = Mathf.Clamp(cha, MinValue, MaxBuyable);
            RecalculateSpentPoints();
        }

        /// <summary>
        /// 根据当前六维值重新计算已消耗点数
        /// </summary>
        private void RecalculateSpentPoints()
        {
            SpentPoints = 0;
            SpentPoints += CostToReach(Strength);
            SpentPoints += CostToReach(Dexterity);
            SpentPoints += CostToReach(Constitution);
            SpentPoints += CostToReach(Intelligence);
            SpentPoints += CostToReach(Wisdom);
            SpentPoints += CostToReach(Charisma);
        }

        /// <summary>
        /// 获取指定属性类型的当前值（购点阶段）
        /// </summary>
        public int GetStat(StatType statType)
        {
            switch (statType)
            {
                case StatType.Strength: return Strength;
                case StatType.Dexterity: return Dexterity;
                case StatType.Constitution: return Constitution;
                case StatType.Intelligence: return Intelligence;
                case StatType.Wisdom: return Wisdom;
                case StatType.Charisma: return Charisma;
                default: return 0;
            }
        }

        /// <summary>
        /// 获取种族加成后的最终值
        /// </summary>
        public int GetFinalStat(StatType statType)
        {
            switch (statType)
            {
                case StatType.Strength: return FinalStrength;
                case StatType.Dexterity: return FinalDexterity;
                case StatType.Constitution: return FinalConstitution;
                case StatType.Intelligence: return FinalIntelligence;
                case StatType.Wisdom: return FinalWisdom;
                case StatType.Charisma: return FinalCharisma;
                default: return 0;
            }
        }

        /// <summary>
        /// 增加1点属性（如果点数够且未达上限）
        /// </summary>
        /// <returns>是否成功</returns>
        public bool IncreaseStat(StatType statType)
        {
            int current = GetStat(statType);
            if (current >= MaxBuyable) return false;

            int nextValue = current + 1;
            int cost = CostForStep(current, nextValue);
            if (AvailablePoints < cost) return false;

            SetStat(statType, nextValue);
            SpentPoints += cost;
            OnPointsChanged?.Invoke(AvailablePoints);
            OnStatChanged?.Invoke(statType, nextValue);
            return true;
        }

        /// <summary>
        /// 减少1点属性（如果未达下限）
        /// </summary>
        /// <returns>是否成功</returns>
        public bool DecreaseStat(StatType statType)
        {
            int current = GetStat(statType);
            if (current <= MinValue) return false;

            int prevValue = current - 1;
            int refund = CostForStep(prevValue, current);

            SetStat(statType, prevValue);
            SpentPoints -= refund;
            OnPointsChanged?.Invoke(AvailablePoints);
            OnStatChanged?.Invoke(statType, prevValue);
            return true;
        }

        /// <summary>
        /// 应用种族加成（购点结束后调用）
        /// </summary>
        public void ApplyRacialBonus(RaceType race)
        {
            _selectedRace = race;
            RacialBonusApplied = true;
        }

        /// <summary>
        /// 尝试将种族加成分配给指定属性（变体人类规则：自选两个不同属性各+1）
        /// </summary>
        /// <returns>是否成功</returns>
        public bool TryAddRacialBonusChoice(StatType statType)
        {
            if (_racialBonusChoices.Count >= VariantHumanBonusCount) return false;
            if (_racialBonusChoices.Contains(statType)) return false;
            _racialBonusChoices.Add(statType);
            OnRacialBonusChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 移除指定属性的种族加成选择
        /// </summary>
        public bool TryRemoveRacialBonusChoice(StatType statType)
        {
            bool removed = _racialBonusChoices.Remove(statType);
            if (removed) OnRacialBonusChanged?.Invoke();
            return removed;
        }

        /// <summary>
        /// 清空所有种族加成选择
        /// </summary>
        public void ClearRacialBonusChoices()
        {
            if (_racialBonusChoices.Count > 0)
            {
                _racialBonusChoices.Clear();
                OnRacialBonusChanged?.Invoke();
            }
        }

        /// <summary>
        /// 获取种族对某属性的加成值
        /// 变体人类：已选该属性则+1，否则+0
        /// </summary>
        public int GetRacialBonus(StatType statType)
        {
            if (!RacialBonusApplied) return 0;
            switch (_selectedRace)
            {
                case RaceType.Human:
                    // 变体人类：仅自选的两个属性各+1
                    return _racialBonusChoices.Contains(statType) ? VariantHumanBonusPerStat : 0;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 获取当前种族对各属性的加成数组 [STR, DEX, CON, INT, WIS, CHA]
        /// </summary>
        public int[] GetAllRacialBonuses()
        {
            return new int[]
            {
                GetRacialBonus(StatType.Strength),
                GetRacialBonus(StatType.Dexterity),
                GetRacialBonus(StatType.Constitution),
                GetRacialBonus(StatType.Intelligence),
                GetRacialBonus(StatType.Wisdom),
                GetRacialBonus(StatType.Charisma),
            };
        }

        /// <summary>
        /// 是否可以增加该属性
        /// </summary>
        public bool CanIncrease(StatType statType)
        {
            int current = GetStat(statType);
            if (current >= MaxBuyable) return false;
            int nextValue = current + 1;
            int cost = CostForStep(current, nextValue);
            return AvailablePoints >= cost;
        }

        /// <summary>
        /// 是否可以减少该属性
        /// </summary>
        public bool CanDecrease(StatType statType)
        {
            return GetStat(statType) > MinValue;
        }

        /// <summary>
        /// 从 8 升到指定值需要消耗的总点数
        /// </summary>
        private int CostToReach(int targetValue)
        {
            if (targetValue <= StartingValue) return 0;
            int totalCost = 0;
            for (int v = StartingValue + 1; v <= targetValue; v++)
            {
                totalCost += CostForStep(v - 1, v);
            }
            return totalCost;
        }

        /// <summary>
        /// 从 fromValue 升到 toValue 这一步消耗的点数
        /// 8→13: 每步1点, 13→14: 2点, 14→15: 2点
        /// </summary>
        private int CostForStep(int fromValue, int toValue)
        {
            // 升到 toValue 这步的花费取决于 toValue 所在区间
            if (toValue <= 13) return 1;
            if (toValue <= 15) return 2;
            return 0; // 不应该到达这里
        }

        private void SetStat(StatType statType, int value)
        {
            switch (statType)
            {
                case StatType.Strength: Strength = value; break;
                case StatType.Dexterity: Dexterity = value; break;
                case StatType.Constitution: Constitution = value; break;
                case StatType.Intelligence: Intelligence = value; break;
                case StatType.Wisdom: Wisdom = value; break;
                case StatType.Charisma: Charisma = value; break;
            }
        }

        /// <summary>
        /// 获取属性调整值（(值-10)/2 向下取整）
        /// </summary>
        public static int GetModifier(int statValue)
        {
            return (statValue - 10) / 2;
        }
    }

    /// <summary>
    /// 六维属性类型
    /// </summary>
    public enum StatType
    {
        Strength,
        Dexterity,
        Constitution,
        Intelligence,
        Wisdom,
        Charisma
    }
}
