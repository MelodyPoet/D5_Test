using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 游戏日志服务：统一记录并广播可供 UI 展示的文本日志。
/// 用途：
/// - 探索进度（波次）
/// - 战斗：命中判定、伤害判定、先攻、动作描述
/// 其它系统只需调用对应的 Log* 方法或通用 Log 方法。
/// </summary>
public static class GameLog
{
    public enum LogChannel
    {
        System,
        Exploration,   // 探索/波次
        Initiative,    // 先攻
        CombatHit,     // 命中检定
        CombatDamage,  // 伤害结算
        Action         // 行动描述
    }

    public sealed class LogEntry
    {
        public DateTime Time;
        public LogChannel Channel;
        public string Message;
        public override string ToString()
        {
            return Message ?? string.Empty;
        }
    }

    /// <summary>
    /// 新日志项事件。UI 可订阅以显示。
    /// </summary>
    public static event Action<LogEntry> OnEntryAdded;

    private static readonly StringBuilder _sb = new StringBuilder(128);

    // 历史缓存：用于回放已产生的日志，避免订阅时序问题
    private const int MaxHistory = 1000;
    private static readonly List<LogEntry> _history = new List<LogEntry>(256);

    /// <summary>
    /// 拷贝当前历史到给定列表（不会暴露内部引用）。
    /// </summary>
    public static void GetHistory(List<LogEntry> target)
    {
        if (target == null) return;
        target.Clear();
        target.AddRange(_history);
    }

    // 通用接口 --------------------------------------------------------

    public static void Log(LogChannel channel, string message)
    {
        var entry = new LogEntry
        {
            Time = DateTime.Now,
            Channel = channel,
            Message = message
        };
        // 写入历史
        _history.Add(entry);
        if (_history.Count > MaxHistory)
        {
            int remove = _history.Count - MaxHistory;
            _history.RemoveRange(0, remove);
        }
        // 广播
        OnEntryAdded?.Invoke(entry);
        Debug.unityLogger?.Log(channel.ToString(), message);
    }

    public static void Logf(LogChannel channel, string format, params object[] args)
    {
        _sb.Length = 0;
        _sb.AppendFormat(format, args);
        Log(channel, _sb.ToString());
    }

    // 语义化便捷接口 --------------------------------------------------

    // 探索/波次进度
    public static void LogExplorationProgress(int currentWave, int totalWaves, bool completed)
    {
        if (totalWaves <= 0)
        {
            Log(LogChannel.Exploration, "探索进度：未知总波次（当前：" + currentWave + ")");
            return;
        }
        if (completed)
        {
            Logf(LogChannel.Exploration, "探索进度：第 {0}/{1} 波完成（已通关本波）", currentWave, totalWaves);
        }
        else
        {
            Logf(LogChannel.Exploration, "探索进度：进行到第 {0}/{1} 波", currentWave, totalWaves);
        }
    }

    // 先攻
    public static void LogInitiative(string actorName, int d20, int modifier, int total)
    {
        Logf(LogChannel.Initiative, "先攻：{0} 掷骰 d20={1}，修正 {2:+#;-#;0}，总计 {3}", actorName, d20, modifier, total);
    }

    // 命中检定（例：攻击者 vs 目标，使用属性，d20 与修正，总和 vs AC，命中/未命中）
    public static void LogHit(string attacker, string target, string attackType, string ability, int d20, int modifier, int total, int targetAC, bool isHit)
    {
        string special = d20 == 20 ? "（暴击！）" : (d20 == 1 ? "（致命失手！）" : string.Empty);
        Logf(LogChannel.CombatHit,
            "命中检定：{0} 对 {1} 发动 {2}（{3}）｜d20={4}{9}，修正 {5:+#;-#;0}，总计 {6} vs AC {7} → {8}",
            attacker, target, attackType, ability, d20, modifier, total, targetAC, isHit ? "命中" : "未命中", special);
    }

    // 伤害结算（简版）：骰子表达、实际掷骰合计、抗性/易伤、最终伤害
    public static void LogDamage(string attacker, string target, string damageType, string diceExpr, int rolledTotal, string resistOrVulnNote, int finalDamage)
    {
        if (string.IsNullOrEmpty(resistOrVulnNote))
        {
            Logf(LogChannel.CombatDamage, "伤害：{0} → {1} | {2} {3}，掷骰合计 {4}，最终 {5}",
                attacker, target, damageType, diceExpr, rolledTotal, finalDamage);
        }
        else
        {
            Logf(LogChannel.CombatDamage, "伤害：{0} → {1} | {2} {3}，掷骰合计 {4}，{5}，最终 {6}",
                attacker, target, damageType, diceExpr, rolledTotal, resistOrVulnNote, finalDamage);
        }
    }

    // 伤害结算（含属性修正）：增加 ability 与 abilityMod 的描述
    public static void LogDamage(string attacker, string target, string damageType, string diceExpr, int rolledTotal, string ability, int abilityMod, string resistOrVulnNote, int finalDamage)
    {
        string abilityPart = string.IsNullOrEmpty(ability) ? string.Empty : $"（{ability} 修正 {abilityMod:+#;-#;0}）";
        if (string.IsNullOrEmpty(resistOrVulnNote))
        {
            Logf(LogChannel.CombatDamage, "伤害：{0} → {1} | {2} {3}{4}，掷骰合计 {5}，最终 {6}",
                attacker, target, damageType, diceExpr, abilityPart, rolledTotal, finalDamage);
        }
        else
        {
            Logf(LogChannel.CombatDamage, "伤害：{0} → {1} | {2} {3}{4}，掷骰合计 {5}，{6}，最终 {7}",
                attacker, target, damageType, diceExpr, abilityPart, rolledTotal, resistOrVulnNote, finalDamage);
        }
    }

    // 行动描述（移动、施法、使用物品等）
    public static void LogAction(string actorName, string description)
    {
        Logf(LogChannel.Action, "行动：{0} {1}", actorName, description);
    }
}
