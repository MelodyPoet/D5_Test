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

        private readonly bool[,] occupied;
        private readonly Dictionary<ItemInstance, Vector2Int> itemPositions = new Dictionary<ItemInstance, Vector2Int>();

        public InventoryGridModel(int rows, int cols)
        {
            this.rows = Mathf.Max(1, rows);
            this.cols = Mathf.Max(1, cols);
            occupied = new bool[this.rows, this.cols];
        }

        public bool TryPlace(ItemInstance item, int x, int y)
        {
            if (!CanPlace(item, x, y)) return false;
            Mark(item, x, y, true);
            itemPositions[item] = new Vector2Int(x, y);
            return true;
        }

        public bool TryMove(ItemInstance item, int x, int y)
        {
            if (!itemPositions.ContainsKey(item)) return false;
            // 临时清除占用再校验
            var old = itemPositions[item];
            Mark(item, old.x, old.y, false);
            if (CanPlace(item, x, y))
            {
                Mark(item, x, y, true);
                itemPositions[item] = new Vector2Int(x, y);
                return true;
            }
            // 回滚
            Mark(item, old.x, old.y, true);
            return false;
        }

        public bool Remove(ItemInstance item)
        {
            if (!itemPositions.TryGetValue(item, out var pos)) return false;
            Mark(item, pos.x, pos.y, false);
            itemPositions.Remove(item);
            return true;
        }

        public bool CanPlace(ItemInstance item, int x, int y)
        {
            if (item == null || item.Width <= 0 || item.Height <= 0) return false;
            if (x < 0 || y < 0) return false;
            if (x + item.Width > cols || y + item.Height > rows) return false;
            for (int yy = 0; yy < item.Height; yy++)
            {
                for (int xx = 0; xx < item.Width; xx++)
                {
                    if (occupied[y + yy, x + xx]) return false;
                }
            }
            return true;
        }

        // 新增：用于拖拽预览，忽略该物品当前占用区域
        public bool CanPlaceIgnoring(ItemInstance item, int x, int y)
        {
            if (item == null || item.Width <= 0 || item.Height <= 0) return false;
            if (x < 0 || y < 0) return false;
            if (x + item.Width > cols || y + item.Height > rows) return false;

            // 获取自身已占用区域
            Vector2Int selfPos;
            bool hasSelf = itemPositions.TryGetValue(item, out selfPos);

            for (int yy = 0; yy < item.Height; yy++)
            {
                for (int xx = 0; xx < item.Width; xx++)
                {
                    int gx = x + xx;
                    int gy = y + yy;
                    if (!occupied[gy, gx]) continue;

                    // 若占用的是自己当前所在区域，则忽略
                    if (hasSelf && gx >= selfPos.x && gx < selfPos.x + item.Width &&
                        gy >= selfPos.y && gy < selfPos.y + item.Height)
                    {
                        continue;
                    }
                    return false;
                }
            }
            return true;
        }

        public bool TryGetPosition(ItemInstance item, out Vector2Int pos)
        {
            return itemPositions.TryGetValue(item, out pos);
        }

        private void Mark(ItemInstance item, int x, int y, bool flag)
        {
            for (int yy = 0; yy < item.Height; yy++)
            {
                for (int xx = 0; xx < item.Width; xx++)
                {
                    occupied[y + yy, x + xx] = flag;
                }
            }
        }

        // 新增：原地旋转（宽高互换），若与其他占用冲突则失败且不改动占用表
        public bool TryRotateInPlace(ItemInstance item)
        {
            if (item == null) return false;
            if (!itemPositions.TryGetValue(item, out var pos)) return false;

            int oldW = item.Width;
            int oldH = item.Height;
            int newW = oldH;
            int newH = oldW;

            // 边界检查
            if (pos.x < 0 || pos.y < 0) return false;
            if (pos.x + newW > cols || pos.y + newH > rows) return false;

            // 检查新区域是否与其他物体冲突（忽略与自己原区域重叠部分）
            for (int yy = 0; yy < newH; yy++)
            {
                for (int xx = 0; xx < newW; xx++)
                {
                    int gx = pos.x + xx;
                    int gy = pos.y + yy;
                    if (!occupied[gy, gx]) continue;

                    bool withinOld = (gx >= pos.x && gx < pos.x + oldW && gy >= pos.y && gy < pos.y + oldH);
                    if (!withinOld) return false;
                }
            }

            // 清除旧占用（按旧尺寸），再按新尺寸标记
            for (int yy = 0; yy < oldH; yy++)
            {
                for (int xx = 0; xx < oldW; xx++)
                {
                    occupied[pos.y + yy, pos.x + xx] = false;
                }
            }
            for (int yy = 0; yy < newH; yy++)
            {
                for (int xx = 0; xx < newW; xx++)
                {
                    occupied[pos.y + yy, pos.x + xx] = true;
                }
            }

            // 位置不变；itemPositions 无需更新
            return true;
        }
    }
}
