using UnityEngine;

/// <summary>
/// 进入场景自动打印几条日志用于验证 UILogPanel 是否正常。
/// 使用方法：把该脚本挂到任意激活的 GameObject 上（可选）。
/// </summary>
public class GameLogDemoSpawner : MonoBehaviour
{
    [Tooltip("是否在 Start() 时打印演示日志")] public bool printOnStart = true;
    [Tooltip("是否启用演示日志（默认关闭，防止干扰实时日志）")] public bool enableDemo = false;

    private void Start()
    {
        // 只有当明确启用演示开关时，才会输出固定示例日志
        if (!enableDemo || !printOnStart) return;

        PrintDemoLogs();
    }

    [ContextMenu("打印演示日志（一次）")]
    private void PrintDemoLogs()
    {
        // 探索进度
        GameLog.LogExplorationProgress(1, 3, false);

        // 先攻
        GameLog.LogInitiative("玩家A", 14, 3, 17);
        GameLog.LogInitiative("敌人哥布林", 12, 2, 14);

        // 命中检定
        GameLog.LogHit("玩家A", "哥布林", "物理普通攻击", "dexterity", 16, 5, 21, 13, true);
        GameLog.LogHit("哥布林", "玩家A", "弓箭", "dexterity", 7, 4, 11, 16, false);

        // 伤害（含属性修正）
        GameLog.LogDamage("玩家A", "哥布林", "Slashing", "1d8", 6, "strength", 3, "", 9);

        // 行动描述
        GameLog.LogAction("玩家A", "使用了治疗药水");
    }
}
