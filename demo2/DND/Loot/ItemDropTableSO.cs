using UnityEngine;
using System.Collections.Generic;
using demo2.DND.InventoryTetris; // ItemBaseSO

namespace demo2.DND.Loot
{
    /// <summary>
    /// 简单掉落表：定义一组物品及其掉落规则。可挂在敌人引用的 EnemyDropSource 上。
    /// 评估时按条目逐条计算：
    /// - guaranteed=true => 必定掉落（忽略概率）
    /// - 否则使用 dropChance (0-1)
    /// 支持数量区间（minAmount..maxAmount）。
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDropTable", menuName = "DND/Loot/Item Drop Table")]
    public class ItemDropTableSO : ScriptableObject
    {
        [System.Serializable]
        public class DropEntry
        {
            [Tooltip("要掉落的物品数据（ItemBaseSO）")] public ItemBaseSO item;
            [Range(0f,1f)][Tooltip("掉落概率（0-1），若 guaranteed=true 则忽略")] public float dropChance = 0.5f;
            [Min(1)][Tooltip("最小数量")] public int minAmount = 1;
            [Min(1)][Tooltip("最大数量（>=最小值）")] public int maxAmount = 1;
            [Tooltip("是否必掉（忽略概率）")] public bool guaranteed;
        }

        [Header("掉落配置条目")]
        public List<DropEntry> entries = new List<DropEntry>();

        /// <summary>
        /// 评估表并生成最终掉落结果（物品 + 数量）。不负责背包添加，调用方再发布事件。
        /// </summary>
        public List<(ItemBaseSO item,int amount)> Evaluate()
        {
            var result = new List<(ItemBaseSO,int)>();
            if (entries == null) return result;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.item == null) continue;
                bool ok = e.guaranteed || Random.value <= e.dropChance;
                if (!ok) continue;
                int minA = Mathf.Max(1, e.minAmount);
                int maxA = Mathf.Max(minA, e.maxAmount);
                int amount = (maxA == minA) ? minA : Random.Range(minA, maxA + 1);
                result.Add((e.item, amount));
            }
            return result;
        }
    }
}

