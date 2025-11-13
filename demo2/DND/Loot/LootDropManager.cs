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
        [Header("拾取请求通道（拖入资产）")] public RequestAddItemChannel_SO requestAddItemChannel;
        [Header("奖励分发模式")]
        [Tooltip("分发到第一个玩家背包（按场景扫描 CharacterInventory + battleSide Player）")] public bool awardToFirstPlayerInventory = true;
        [Tooltip("若为 true：优先尝试使用当前阵型的首个存活角色背包（需要阵型管理器）")] public bool preferFormationFirstAlive;
        [Tooltip("是否允许多敌人同时死亡合并为一次批量处理（减少多次 UI 刷新）")] public bool batchDispatch = true;
        [Tooltip("批量模式下的最大缓冲延迟（秒），在窗口期内合并掉落")] public float batchWindowSeconds = 0.15f;
        [Tooltip("调试日志")] public bool debugLogs;

        private readonly List<(EnemyDropSource source, List<(ItemBaseSO item, int amount)> drops)> pending = new();
        private float batchWindowEnd;

        private void Update()
        {
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
            if (debugLogs) Debug.Log($"[LootDropManager] 收到死亡掉落 {results.Count} 条目，来源={src.name}");
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

        private void FlushPending()
        {
            // 合并所有结果
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
                Debug.LogWarning("[LootDropManager] 未配置 RequestAddItemChannel 资产，无法发送拾取事件。");
                return;
            }
            int sent = 0;
            for (int i = 0; i < drops.Count; i++)
            {
                var (item, amount) = drops[i];
                if (item == null) continue;
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
