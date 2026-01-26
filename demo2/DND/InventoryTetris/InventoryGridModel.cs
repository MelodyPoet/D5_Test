using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 负责网格占用与放置规则（不包含任何 UI 逻辑）
    /// </summary>
    public class InventoryGridModel
    {
        public readonly int rows;
        public readonly int cols;

        private readonly Dictionary<ItemInstance, Vector2Int> itemPositions = new Dictionary<ItemInstance, Vector2Int>();
        private readonly ItemInstance[,] occupiedBy;

        public InventoryGridModel(int rows, int cols)
        {
            this.rows = Mathf.Max(1, rows);
            this.cols = Mathf.Max(1, cols);
            occupiedBy = new ItemInstance[this.rows, this.cols];
        }

        public bool TryPlace(ItemInstance item, int x, int y)
        {
            if (!CanPlace(item, x, y)) return false;
            Mark(item, x, y, item);
            var pos = new Vector2Int(x, y);
            itemPositions[item] = pos;
            item.gridPosition = pos; // 同步回实例，便于序列化保存
            return true;
        }

        public bool TryMove(ItemInstance item, int x, int y)
        {
            if (!itemPositions.ContainsKey(item)) return false;
            var old = itemPositions[item];
            Mark(item, old.x, old.y, null);
            if (CanPlace(item, x, y))
            {
                Mark(item, x, y, item);
                var pos = new Vector2Int(x, y);
                itemPositions[item] = pos;
                item.gridPosition = pos; // 同步当前位置
                return true;
            }
            Mark(item, old.x, old.y, item);
            return false;
        }

        public bool Remove(ItemInstance item)
        {
            if (!itemPositions.TryGetValue(item, out var pos)) return false;
            Mark(item, pos.x, pos.y, null);
            itemPositions.Remove(item);
            item.gridPosition = default; // 可选：重置序列化位置
            return true;
        }

        public bool CanPlace(ItemInstance item, int x, int y)
        {
            var shape = item.GetCurrentShape();
            if (item == null || shape == null || shape.Count == 0) return false;
            if (x < 0 || y < 0) return false;

            foreach (var coord in shape)
            {
                int gridX = x + coord.x;
                int gridY = y + coord.y;

                if (gridX < 0 || gridX >= cols || gridY < 0 || gridY >= rows)
                {
                    return false; // Out of bounds
                }

                if (occupiedBy[gridY, gridX] != null && occupiedBy[gridY, gridX] != item)
                {
                    return false; // Occupied by another item
                }
            }
            return true;
        }

        public bool TryGetPosition(ItemInstance item, out Vector2Int pos)
        {
            return itemPositions.TryGetValue(item, out pos);
        }

        private void Mark(ItemInstance item, int x, int y, ItemInstance instanceToSet)
        {
            var shape = item.GetCurrentShape();
            if (shape == null) return;

            foreach (var coord in shape)
            {
                int gridX = x + coord.x;
                int gridY = y + coord.y;

                if (gridX >= 0 && gridX < cols && gridY >= 0 && gridY < rows)
                {
                    occupiedBy[gridY, gridX] = instanceToSet;
                }
            }
        }
    }
}
