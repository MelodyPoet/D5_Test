using UnityEngine;

/// <summary>
/// 掩护类型枚举
/// </summary>
public enum CoverType {
    None,           // 无掩护
    Half,           // 半掩护 (+2 AC, -2 攻击命中)
    ThreeQuarter,   // 四分之三掩护 (+5 AC, -5 攻击命中)
    Full            // 完全掩护 (无法攻击)
}

/// <summary>
/// 横版战斗掩护系统 - 线性布局版本
/// 在线性布局中，攻击者和目标之间的任何角色都可能提供掩护
/// </summary>
public static class HorizontalCoverSystem {
    /// <summary>
    /// 检查目标是否受到掩护保护 - 线性布局版本
    /// </summary>
    public static CoverType CheckCover(HorizontalPosition shooterPos, HorizontalPosition targetPos) {
        // 近战攻击不受掩护影响
        if (HorizontalFormationAI.CanMeleeAttack(shooterPos, targetPos)) {
            return CoverType.None;
        }

        // 获取攻击路径上的所有阻挡位置
        HorizontalPosition[] blockingPositions = HorizontalFormationAI.GetBlockingPositions(shooterPos, targetPos);

        if (blockingPositions.Length == 0) {
            return CoverType.None;
        }

        // 检查阻挡位置中是否有活着的角色
        int aliveBlockers = 0;
        int totalBlockers = 0;

        foreach (HorizontalPosition blockPos in blockingPositions) {
            CharacterStats blocker = HorizontalBattleFormationManager.Instance?.GetCharacterAtPosition(blockPos);
            if (blocker != null) {
                totalBlockers++;
                if (blocker.currentHitPoints > 0) {
                    aliveBlockers++;
                }
            }
        }

        return CalculateCoverFromBlockers(aliveBlockers, totalBlockers);
    }
    /// <summary>
    /// 根据阻挡者数量计算掩护类型 - 线性布局版本
    /// </summary>
    private static CoverType CalculateCoverFromBlockers(int aliveBlockers, int totalBlockers) {
        if (aliveBlockers == 0) {
            return CoverType.None;
        }

        // 根据阻挡者数量决定掩护强度
        if (aliveBlockers == 1) {
            return CoverType.Half; // 一个阻挡者提供半掩护
        }
        else if (aliveBlockers == 2) {
            return CoverType.ThreeQuarter; // 两个阻挡者提供四分之三掩护
        }
        else if (aliveBlockers >= 3) {
            return CoverType.Full; // 三个或更多阻挡者提供完全掩护
        }

        return CoverType.None;
    }
    /// <summary>
    /// 获取掩护的AC加值 - 线性布局版本
    /// </summary>
    public static int GetCoverACBonus(CoverType coverType) {
        switch (coverType) {
            case CoverType.Half:
                return 2;
            case CoverType.ThreeQuarter:
                return 5;
            case CoverType.Full:
                return 999; // 完全掩护无法被攻击
            default:
                return 0;
        }
    }

    /// <summary>
    /// 获取掩护的豁免检定加值
    /// </summary>
    public static int GetCoverSaveBonus(CoverType coverType) {
        switch (coverType) {
            case CoverType.Half:
                return 2;
            case CoverType.ThreeQuarter:
                return 5;
            default:
                return 0;
        }
    }

    /// <summary>
    /// 检查掩护是否阻止攻击
    /// </summary>
    public static bool DoesCoverBlockAttack(CoverType coverType) {
        return coverType == CoverType.Full;
    }
}
