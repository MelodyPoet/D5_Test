using UnityEngine;

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

public enum BattleSide {
    Player, // 玩家方 (X轴左侧)
    Enemy   // 敌人方 (X轴右侧)
}

public enum FormationType {
    Defensive,      // 防御阵型: 坦克前排，脆皮后排
    Aggressive,     // 进攻阵型: 输出角色前置
    Balanced,       // 平衡阵型: 均匀分布
    Ranged,         // 远程阵型: 最大化射程优势
    Custom          // 自定义阵型
}

/// <summary>
/// 横版阵型AI决策系统
/// </summary>
public static class HorizontalFormationAI {
    /// <summary>
    /// 根据角色职业获取最佳线性位置
    /// </summary>
    public static HorizontalPosition GetOptimalPosition(CharacterStats character, BattleSide side) {
        switch (character.characterClass) {
            case DND5E.CharacterClass.Fighter:
            case DND5E.CharacterClass.Paladin:
                // 坦克职业站前排中央，承担主要伤害
                return side == BattleSide.Player ?
                    HorizontalPosition.PlayerFrontCenter :
                    HorizontalPosition.EnemyFrontCenter;

            case DND5E.CharacterClass.Wizard:
            case DND5E.CharacterClass.Sorcerer:
                // 法师站后排中央，获得最大保护和射程
                return side == BattleSide.Player ?
                    HorizontalPosition.PlayerBackCenter :
                    HorizontalPosition.EnemyBackCenter;

            case DND5E.CharacterClass.Rogue:
                // 盗贼站前排侧翼，准备潜行和背刺
                return side == BattleSide.Player ?
                    HorizontalPosition.PlayerFrontLeft :
                    HorizontalPosition.EnemyFrontRight;

            case DND5E.CharacterClass.Cleric:
                // 牧师站后排侧翼，兼顾治疗和输出
                return side == BattleSide.Player ?
                    HorizontalPosition.PlayerBackRight :
                    HorizontalPosition.EnemyBackLeft;

            case DND5E.CharacterClass.Ranger:
                // 游侠站后排侧翼，远程输出
                return side == BattleSide.Player ?
                    HorizontalPosition.PlayerBackLeft :
                    HorizontalPosition.EnemyBackRight;

            default:
                // 默认位置
                return side == BattleSide.Player ?
                    HorizontalPosition.PlayerFrontRight :
                    HorizontalPosition.EnemyFrontLeft;
        }
    }
    /// <summary>
    /// 获取位置所在的排 - 线性布局版本
    /// </summary>
    public static BattleRow GetPositionRow(HorizontalPosition position) {
        switch (position) {
            case HorizontalPosition.PlayerBackLeft:
            case HorizontalPosition.PlayerBackCenter:
            case HorizontalPosition.PlayerBackRight:
                return BattleRow.PlayerBack;

            case HorizontalPosition.PlayerFrontLeft:
            case HorizontalPosition.PlayerFrontCenter:
            case HorizontalPosition.PlayerFrontRight:
                return BattleRow.PlayerFront;

            case HorizontalPosition.EnemyFrontLeft:
            case HorizontalPosition.EnemyFrontCenter:
            case HorizontalPosition.EnemyFrontRight:
                return BattleRow.EnemyFront;

            case HorizontalPosition.EnemyBackLeft:
            case HorizontalPosition.EnemyBackCenter:
            case HorizontalPosition.EnemyBackRight:
                return BattleRow.EnemyBack;

            default:
                return BattleRow.PlayerFront;
        }
    }

    /// <summary>
    /// 获取位置所属阵营
    /// </summary>
    public static BattleSide GetPositionSide(HorizontalPosition position) {
        int positionIndex = (int)position;
        return positionIndex < 6 ? BattleSide.Player : BattleSide.Enemy;
    }
    /// <summary>
    /// 获取最近的敌方前排位置 - 线性布局版本
    /// </summary>
    public static HorizontalPosition[] GetNearestEnemyFrontRow(BattleSide side) {
        if (side == BattleSide.Player) {
            // 玩家方最近的敌人是敌人前排
            return new HorizontalPosition[]
            {
                HorizontalPosition.EnemyFrontLeft,
                HorizontalPosition.EnemyFrontCenter,
                HorizontalPosition.EnemyFrontRight
            };
        }
        else {
            // 敌人方最近的敌人是玩家前排
            return new HorizontalPosition[]
            {
                HorizontalPosition.PlayerFrontLeft,
                HorizontalPosition.PlayerFrontCenter,
                HorizontalPosition.PlayerFrontRight
            };
        }
    }

    /// <summary>
    /// 获取对面阵营的所有位置 - 线性布局版本
    /// </summary>
    public static HorizontalPosition[] GetOppositeAllPositions(BattleSide side) {
        if (side == BattleSide.Player) {
            return new HorizontalPosition[]
            {
                HorizontalPosition.EnemyFrontLeft,
                HorizontalPosition.EnemyFrontCenter,
                HorizontalPosition.EnemyFrontRight,
                HorizontalPosition.EnemyBackLeft,
                HorizontalPosition.EnemyBackCenter,
                HorizontalPosition.EnemyBackRight
            };
        }
        else {
            return new HorizontalPosition[]
            {
                HorizontalPosition.PlayerFrontLeft,
                HorizontalPosition.PlayerFrontCenter,
                HorizontalPosition.PlayerFrontRight,
                HorizontalPosition.PlayerBackLeft,
                HorizontalPosition.PlayerBackCenter,
                HorizontalPosition.PlayerBackRight
            };
        }
    }
    /// <summary>
    /// 检查两个位置是否相邻 - 线性布局版本
    /// </summary>
    public static bool ArePositionsAdjacent(HorizontalPosition pos1, HorizontalPosition pos2) {
        int distance = GetLinearDistance(pos1, pos2);
        return distance <= 1; // 在线性布局中，相邻位置距离为1
    }

    /// <summary>
    /// 计算线性布局中两个位置的距离
    /// 距离 = |pos1_index - pos2_index|
    /// </summary>
    public static int GetLinearDistance(HorizontalPosition pos1, HorizontalPosition pos2) {
        return Mathf.Abs((int)pos1 - (int)pos2);
    }

    /// <summary>
    /// 检查是否能进行近战攻击 - 线性布局版本
    /// 近战攻击要求: 距离 <= 1
    /// </summary>
    public static bool CanMeleeAttack(HorizontalPosition attackerPos, HorizontalPosition targetPos) {
        return GetLinearDistance(attackerPos, targetPos) <= 1;
    }

    /// <summary>
    /// 检查是否能进行远程攻击 - 线性布局版本
    /// 远程攻击可以跨越任何距离，但需要考虑掩护
    /// </summary>
    public static bool CanRangedAttack(HorizontalPosition attackerPos, HorizontalPosition targetPos) {
        // 远程攻击总是可能的，但可能有掩护惩罚
        return true;
    }

    /// <summary>
    /// 获取两个位置之间的所有阻挡位置 - 线性布局版本
    /// </summary>
    public static HorizontalPosition[] GetBlockingPositions(HorizontalPosition start, HorizontalPosition end) {
        int startIndex = (int)start;
        int endIndex = (int)end;

        if (startIndex == endIndex) return new HorizontalPosition[0];

        var blockingPositions = new System.Collections.Generic.List<HorizontalPosition>();

        int min = Mathf.Min(startIndex, endIndex);
        int max = Mathf.Max(startIndex, endIndex);

        // 添加起始和结束位置之间的所有位置
        for (int i = min + 1; i < max; i++) {
            blockingPositions.Add((HorizontalPosition)i);
        }

        return blockingPositions.ToArray();
    }
    /// <summary>
    /// 获取位置的前排对应位置 - 线性布局版本
    /// </summary>
    public static HorizontalPosition GetFrontRowProtector(HorizontalPosition backRowPos) {
        switch (backRowPos) {
            case HorizontalPosition.PlayerBackLeft:
                return HorizontalPosition.PlayerFrontLeft;
            case HorizontalPosition.PlayerBackCenter:
                return HorizontalPosition.PlayerFrontCenter;
            case HorizontalPosition.PlayerBackRight:
                return HorizontalPosition.PlayerFrontRight;
            case HorizontalPosition.EnemyBackLeft:
                return HorizontalPosition.EnemyFrontLeft;
            case HorizontalPosition.EnemyBackCenter:
                return HorizontalPosition.EnemyFrontCenter;
            case HorizontalPosition.EnemyBackRight:
                return HorizontalPosition.EnemyFrontRight;
            default:
                return backRowPos; // 如果已经是前排，返回自身
        }
    }

    /// <summary>
    /// 获取位置的世界坐标 - 线性布局版本
    /// 从左到右：玩家后排 → 玩家前排 → 敌人前排 → 敌人后排
    /// </summary>
    public static Vector3 GetWorldPosition(HorizontalPosition position) {
        float spacing = 2.0f; // 位置间的间距
        int index = (int)position;

        // X坐标从左到右递增
        float x = index * spacing - 11.0f; // 居中显示
        float y = 0f;

        // Y坐标可以根据排次有所区别
        BattleRow row = GetPositionRow(position);
        switch (row) {
            case BattleRow.PlayerBack:
            case BattleRow.EnemyBack:
                y = -0.5f; // 后排稍微靠后
                break;
            case BattleRow.PlayerFront:
            case BattleRow.EnemyFront:
                y = 0.5f; // 前排稍微靠前
                break;
        }

        return new Vector3(x, y, 0);
    }
}
