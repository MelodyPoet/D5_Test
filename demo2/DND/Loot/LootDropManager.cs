using UnityEngine;
using System.Collections.Generic;
using demo2.DND.Core.Events.Channels; // RequestAddItemChannel_SO
using demo2.DND.Core.Events.Data; // InventoryAddItemRequest
using demo2.DND.InventoryTetris; // CharacterInventory / ItemBaseSO
using demo2.DND.HorizontalFormation; // Battle formation manager

namespace demo2.DND.Loot
{
    /// <summary>
    /// 全局掉落管理器：监听敌人死亡，集中评估 EnemyDropSource 并通过 RequestAddItemChannel 分发到玩家背包。
    /// 安装：放在 GameSystems/Loot 系统节点，配置 requestAddItemChannel。
    /// </summary>
    public class LootDropManager : MonoBehaviour
    {
        [Header("拾取请求通道(拖入资产)")] public RequestAddItemChannel_SO requestAddItemChannel;
        [Header("奖励分发模式")]
        [Tooltip("分发到第一个玩家背包(按场景扫描 CharacterInventory + battleSide Player)")] public bool awardToFirstPlayerInventory = true;
        [Tooltip("若为 true: 优先尝试使用当前阵型的首个存活角色背包(需要阵型管理器)")] public bool preferFormationFirstAlive;
        [Tooltip("是否允许多敌人同时死亡合并为一次批量处理(减少多次 UI 刷新)")] public bool batchDispatch = true;
        [Tooltip("批量模式下的最大缓冲延迟(秒), 在窗口期内合并掉落")] public float batchWindowSeconds = 0.15f;
        [Tooltip("调试日志")] public bool debugLogs;
        [Header("波次统一分发设置")]
        [Tooltip("若开启: 敌人死亡只累计到本波缓冲, 不立即分发; 在波次结束时调用 FinalizeWave() 一次性分发")] public bool accumulateUntilWaveEnd = true;
        [Tooltip("额外的波次奖励掉落表(每次 FinalizeWave 时附加评估一次, 可为空)")] public ItemDropTableSO waveBonusTable;
        [Tooltip("是否在波次分发前合并同类物品(将多条相同Item的记录合并为一条数量相加)")] public bool mergeDuplicatesOnWaveFinalize = true;

        private readonly List<(EnemyDropSource source, List<(ItemBaseSO item, int amount)> drops)> pending = new();
        private float batchWindowEnd;

        // 新增: 波次缓冲 (accumulateUntilWaveEnd=true 时使用)
        private readonly List<(ItemBaseSO item, int amount)> waveBuffer = new();

        private void Update()
        {
            if (accumulateUntilWaveEnd) return; // 波次模式下不按时间窗口自动分发
            if (batchDispatch && pending.Count > 0 && Time.time >= batchWindowEnd)
            {
                FlushPending();
            }
        }

        /// <summary>
        /// 外部可调用：通知有敌人死亡，尝试收集其掉落。
        /// 建议：由 EnemyDropSource 在死亡后（delegateToManager=true）调用 LootDropManager.RegisterDeath(this)。
        /// 若未显式调用，你也可以定时主动扫描死亡状态（此处为简单实现，示例项目中手动调用更清晰）。
        /// </summary>
        public void RegisterDeath(EnemyDropSource src)
        {
            if (src == null) return;
            var results = src.Evaluate();
            if (results == null || results.Count == 0) return;
            if (debugLogs) Debug.Log($"[LootDropManager] 收到死亡掉落 {results.Count} 条目, 来源={src.name}");

            if (accumulateUntilWaveEnd)
            {
                // 直接累加到波次缓冲
                for (int i = 0; i < results.Count; i++)
                {
                    var (item, amount) = results[i];
                    if (item == null) continue;
                    waveBuffer.Add((item, amount));
                }
                if (debugLogs) Debug.Log($"[LootDropManager] 波次缓冲累计 {waveBuffer.Count} 条 (新增 {results.Count})");
                return;
            }

            if (batchDispatch)
            {
                pending.Add((src, results));
                batchWindowEnd = Time.time + batchWindowSeconds;
            }
            else
            {
                Dispatch(results);
            }
        }

        // 新增: 波次结束统一分发
        public void FinalizeWave(int waveIndex)
        {
            if (!accumulateUntilWaveEnd)
            {
                if (debugLogs) Debug.LogWarning("[LootDropManager] FinalizeWave 调用但 accumulateUntilWaveEnd=false, 忽略 (当前已按死亡实时或批次分发)。");
                return;
            }
            if (debugLogs) Debug.Log($"[LootDropManager] FinalizeWave 开始: waveIndex={waveIndex}, 当前缓冲 {waveBuffer.Count} 条");

            // 附加波次奖励表
            if (waveBonusTable != null)
            {
                var bonus = waveBonusTable.Evaluate();
                if (bonus != null && bonus.Count > 0)
                {
                    for (int i = 0; i < bonus.Count; i++)
                    {
                        var (item, amount) = bonus[i];
                        if (item == null) continue;
                        waveBuffer.Add((item, amount));
                    }
                    if (debugLogs) Debug.Log($"[LootDropManager] 波次奖励附加 {bonus.Count} 条, 合并后 {waveBuffer.Count} 条");
                }
            }

            if (waveBuffer.Count == 0)
            {
                if (debugLogs) Debug.Log("[LootDropManager] FinalizeWave: 无累计掉落, 跳过分发");
                return;
            }

            List<(ItemBaseSO item,int amount)> toDispatch = waveBuffer;
            if (mergeDuplicatesOnWaveFinalize)
            {
                var map = new Dictionary<ItemBaseSO,int>();
                for (int i = 0; i < waveBuffer.Count; i++)
                {
                    var (item, amount) = waveBuffer[i];
                    if (item == null) continue;
                    if (!map.ContainsKey(item)) map[item] = amount; else map[item] += amount;
                }
                toDispatch = new List<(ItemBaseSO,int)>(map.Count);
                foreach (var kv in map)
                {
                    toDispatch.Add((kv.Key, kv.Value));
                }
                if (debugLogs) Debug.Log($"[LootDropManager] FinalizeWave 合并重复后: 原 {waveBuffer.Count} 条 -> {toDispatch.Count} 条");
            }

            // 分发后清空
            Dispatch(toDispatch);
            waveBuffer.Clear();
            if (debugLogs) Debug.Log("[LootDropManager] FinalizeWave 分发完成并清空波次缓冲");
        }

        // 新增: 外部可清空当前波次缓冲（例如战斗失败不发放奖励）
        public void ClearWaveBuffer(bool log = true)
        {
            waveBuffer.Clear();
            if (log && debugLogs) Debug.Log("[LootDropManager] 波次缓冲已清空(外部调用)");
        }

        private void FlushPending()
        {
            var combined = new List<(ItemBaseSO item,int amount)>();
            for (int i = 0; i < pending.Count; i++)
            {
                var pack = pending[i];
                foreach (var (item, amount) in pack.drops)
                {
                    combined.Add((item, amount));
                }
            }
            pending.Clear();
            if (debugLogs) Debug.Log($"[LootDropManager] 批量分发 {combined.Count} 条目。");
            Dispatch(combined);
        }

        private void Dispatch(List<(ItemBaseSO item, int amount)> drops)
        {
            var targetInv = ResolveTargetInventory();
            if (targetInv == null)
            {
                Debug.LogWarning("[LootDropManager] 未找到可用的玩家背包接收掉落。");
                return;
            }
            if (requestAddItemChannel == null)
            {
                Debug.LogWarning("[LootDropManager] 未配置 RequestAddItemChannel 资产, 无法发送拾取事件。");
                return;
            }
            int sent = 0;
            for (int i = 0; i < drops.Count; i++)
            {
                var (item, amount) = drops[i];
                if (item == null || amount <= 0) continue;
                requestAddItemChannel.RaiseEvent(new InventoryAddItemRequest(targetInv, item, amount));
                sent++;
            }
            if (debugLogs) Debug.Log($"[LootDropManager] 已分发 {sent}/{drops.Count} 条掉落到背包 {targetInv.gameObject.name}");
        }

        private CharacterInventory ResolveTargetInventory()
        {
            CharacterInventory target = null;
            if (preferFormationFirstAlive)
            {
                var formation = FindObjectOfType<HorizontalBattleFormationManager>();
                if (formation != null)
                {
                    var playerChars = formation.GetAllAliveCharacters(BattleSide.Player);
                    if (playerChars != null && playerChars.Count > 0)
                    {
                        target = playerChars[0].GetComponent<CharacterInventory>();
                    }
                }
            }
            if (target == null && awardToFirstPlayerInventory)
            {
                var allInv = FindObjectsByType<CharacterInventory>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < allInv.Length; i++)
                {
                    var inv = allInv[i];
                    if (inv == null) continue;
                    var cs = inv.GetComponent<CharacterStats>()
                             ?? inv.GetComponentInParent<CharacterStats>()
                             ?? inv.GetComponentInChildren<CharacterStats>(true);
                    if (cs != null && cs.battleSide == BattleSide.Player)
                    {
                        target = inv;
                        break;
                    }
                }
            }
            return target;
        }
    }
}
