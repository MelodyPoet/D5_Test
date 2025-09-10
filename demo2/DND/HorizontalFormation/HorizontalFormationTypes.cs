namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 横版战斗位置枚举 - 2D线性阵型
    /// X轴从左到右：玩家后排 → 玩家前排 → 敌人前排 → 敌人后排
    /// Y轴控制左中右排列
    /// </summary>
    public enum HorizontalPosition {
        // 玩家前排 (X轴中左位置，接近敌人)
        PlayerFrontLeft = 0,    // 玩家前排左
        PlayerFrontCenter = 1,  // 玩家前排中
        PlayerFrontRight = 2,   // 玩家前排右

        // 玩家后排 (X轴最左位置，远程支援)
        PlayerBackLeft = 3,     // 玩家后排左
        PlayerBackCenter = 4,   // 玩家后排中
        PlayerBackRight = 5,    // 玩家后排右

        // 敌人前排 (X轴中右位置，接近玩家)
        EnemyFrontLeft = 6,     // 敌人前排左
        EnemyFrontCenter = 7,   // 敌人前排中
        EnemyFrontRight = 8,    // 敌人前排右

        // 敌人后排 (X轴最右位置，远程支援)
        EnemyBackLeft = 9,      // 敌人后排左
        EnemyBackCenter = 10,   // 敌人后排中
        EnemyBackRight = 11     // 敌人后排右
    }

    public enum BattleRow {
        PlayerFront,  // 玩家前排 - 近战位置
        PlayerBack,   // 玩家后排 - 远程位置
        EnemyFront,   // 敌人前排 - 近战位置
        EnemyBack     // 敌人后排 - 远程位置
    }

    public enum FormationType {
        Defensive,      // 防御阵型: 坦克前排，脆皮后排
        Aggressive,     // 进攻阵型: 输出角色前置
        Balanced,       // 平衡阵型: 均匀分布
        Ranged,         // 远程阵型: 最大化射程优势
        Custom          // 自定义阵型: 手动指定位置
    }

    /// <summary>
    /// 横版阵型辅助类
    /// 提供位置计算和战术判断功能
    /// </summary>
    public static class HorizontalFormationAI {
        /// <summary>
        /// 根据角色特性获取最佳位置
        /// </summary>
        public static HorizontalPosition GetOptimalPosition(CharacterStats character, BattleSide side) {
            // 根据职业特性决定前后排位置
            bool shouldBeFrontLine = IsFrontLineClass(character.characterClass);

            if (shouldBeFrontLine) {
                // 近战职业优先前排中央
                return side == BattleSide.Player ?
                    HorizontalPosition.PlayerFrontCenter :
                    HorizontalPosition.EnemyFrontCenter;
            } else {
                // 远程职业优先后排中央
                return side == BattleSide.Player ?
                    HorizontalPosition.PlayerBackCenter :
                    HorizontalPosition.EnemyBackCenter;
            }
        }

        /// <summary>
        /// 判断职业是否适合前排
        /// </summary>
        public static bool IsFrontLineClass(CharacterClass characterClass) {
            switch (characterClass) {
                case CharacterClass.Fighter:
                case CharacterClass.Paladin:
                case CharacterClass.Barbarian:
                    return true; // 坦克职业前排

                case CharacterClass.Rogue:
                    return true; // 潜行者也可以前排

                case CharacterClass.Wizard:
                case CharacterClass.Sorcerer:
                case CharacterClass.Warlock:
                case CharacterClass.Ranger:
                    return false; // 法师和射手后排

                case CharacterClass.Cleric:
                case CharacterClass.Druid:
                case CharacterClass.Bard:
                    return false; // 支援职业后排

                default:
                    return true; // 默认前排
            }
        }

        /// <summary>
        /// 获取位置对应的阵营
        /// </summary>
        public static BattleSide GetPositionSide(HorizontalPosition position) {
            return ((int)position <= 5) ? BattleSide.Player : BattleSide.Enemy;
        }

        /// <summary>
        /// 检查是否可以进行近战攻击
        /// </summary>
        public static bool CanMeleeAttack(HorizontalPosition from, HorizontalPosition to) {
            // 简化规则：只有前排可以近战攻击，且只能攻击对面前排
            bool fromIsFront = IsFrontRow(from);
            bool toIsFront = IsFrontRow(to);
            bool differentSides = GetPositionSide(from) != GetPositionSide(to);

            return fromIsFront && toIsFront && differentSides;
        }

        /// <summary>
        /// 判断位置是否为前排
        /// </summary>
        public static bool IsFrontRow(HorizontalPosition position) {
            switch (position) {
                case HorizontalPosition.PlayerFrontLeft:
                case HorizontalPosition.PlayerFrontCenter:
                case HorizontalPosition.PlayerFrontRight:
                case HorizontalPosition.EnemyFrontLeft:
                case HorizontalPosition.EnemyFrontCenter:
                case HorizontalPosition.EnemyFrontRight:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 获取位置对应的排
        /// </summary>
        public static BattleRow GetPositionRow(HorizontalPosition position) {
            switch (position) {
                case HorizontalPosition.PlayerFrontLeft:
                case HorizontalPosition.PlayerFrontCenter:
                case HorizontalPosition.PlayerFrontRight:
                    return BattleRow.PlayerFront;

                case HorizontalPosition.PlayerBackLeft:
                case HorizontalPosition.PlayerBackCenter:
                case HorizontalPosition.PlayerBackRight:
                    return BattleRow.PlayerBack;

                case HorizontalPosition.EnemyFrontLeft:
                case HorizontalPosition.EnemyFrontCenter:
                case HorizontalPosition.EnemyFrontRight:
                    return BattleRow.EnemyFront;

                case HorizontalPosition.EnemyBackLeft:
                case HorizontalPosition.EnemyBackCenter:
                case HorizontalPosition.EnemyBackRight:
                    return BattleRow.EnemyBack;

                default:
                    return BattleRow.PlayerFront; // 默认值
            }
        }
    }
}
