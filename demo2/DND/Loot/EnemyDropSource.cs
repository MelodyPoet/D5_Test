using UnityEngine;
using System.Collections.Generic;
using demo2.DND.InventoryTetris; // CharacterInventory / ItemBaseSO
using demo2.DND.Core.Events.Channels; // RequestAddItemChannel_SO
using demo2.DND.Core.Events.Data; // InventoryAddItemRequest
using demo2.DND; // CharacterStats

namespace demo2.DND.Loot
{
    /// <summary>
    /// 敌人掉落源：挂在敌人身上。死亡时评估掉落表并：
    /// 1) delegateToManager=true -> 调用 LootDropManager.RegisterDeath(this)
    /// 2) false -> 直接通过 requestAddItemChannel 发送拾取事件到目标背包
    /// </summary>
    public class EnemyDropSource : MonoBehaviour
    {
        [Header("掉落表 (ScriptableObject)")] public ItemDropTableSO dropTable;
        [Header("死亡后自动评估")] public bool autoEvaluateOnDeath = true;
        [Tooltip("true: 交给 LootDropManager 统一分发；false: 本组件直接发送拾取事件")] public bool delegateToManager = true;
        [Header("直接分发所需通道 (delegateToManager=false)")] public RequestAddItemChannel_SO requestAddItemChannel;
        [Tooltip("直接分发模式的目标背包，空则自动找第一个玩家背包")] public CharacterInventory overrideTargetInventory;

        private CharacterStats stats;
        private bool dropped; // 防重复

        private void Awake()
        {
            stats = GetComponent<CharacterStats>();
        }

        private void OnEnable()
        {
            if (stats != null) stats.OnHealthChanged += HandleHealthChanged;
        }
        private void OnDisable()
        {
            if (stats != null) stats.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (!autoEvaluateOnDeath || dropped) return;
            if (current <= 0)
            {
                dropped = true;
                if (delegateToManager)
                {
                    var mgr = FindObjectOfType<LootDropManager>();
                    if (mgr != null) mgr.RegisterDeath(this);
                }
                else
                {
                    DirectDispatch();
                }
            }
        }

        /// <summary>
        /// 评估掉落表，返回物品+数量列表。外部（LootDropManager）调用或本组件内部直接使用。
        /// </summary>
        public List<(ItemBaseSO item, int amount)> Evaluate()
        {
            if (dropTable == null) return new List<(ItemBaseSO, int)>();
            return dropTable.Evaluate();
        }

        private void DirectDispatch()
        {
            var results = Evaluate();
            if (results.Count == 0) return;
            var inv = overrideTargetInventory;
            if (inv == null) inv = AutoFindFirstPlayerInventory();
            if (inv == null) return;
            if (requestAddItemChannel == null) return;
            foreach (var (item, amount) in results)
            {
                if (item == null) continue;
                requestAddItemChannel.RaiseEvent(new InventoryAddItemRequest(inv, item, amount));
            }
        }

        private CharacterInventory AutoFindFirstPlayerInventory()
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
                    return inv;
                }
            }
            return null;
        }
    }
}

