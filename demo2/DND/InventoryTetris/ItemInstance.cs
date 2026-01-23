using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace demo2.DND.InventoryTetris
{
    [Serializable]
    public class ItemInstance
    {
        public string instanceId;
        public ItemBaseSO data;
        public int rotation; // 0=0, 1=90, 2=180, 3=270

        private List<Vector2Int> _currentShape;

        public ItemInstance(ItemBaseSO data)
        {
            this.instanceId = Guid.NewGuid().ToString("N");
            this.data = data;
            this.rotation = 0;
            UpdateShape();
        }

        public int Width => _currentShape.Any() ? _currentShape.Max(c => c.x) + 1 : 0;
        public int Height => _currentShape.Any() ? _currentShape.Max(c => c.y) + 1 : 0;

        public List<Vector2Int> GetCurrentShape()
        {
            return _currentShape;
        }

        public void ToggleRotate()
        {
            if (data != null && data.canRotate)
            {
                rotation = (rotation + 1) % 4;
                UpdateShape();
            }
        }

        private void UpdateShape()
        {
            if (data == null || data.shapeCoords == null)
            {
                _currentShape = new List<Vector2Int>();
                return;
            }

            _currentShape = new List<Vector2Int>();
            int max_x = data.shapeCoords.Any() ? data.shapeCoords.Max(c => c.x) : 0;
            int max_y = data.shapeCoords.Any() ? data.shapeCoords.Max(c => c.y) : 0;

            foreach (var coord in data.shapeCoords)
            {
                switch (rotation)
                {
                    case 0: // 0 degrees
                        _currentShape.Add(coord);
                        break;
                    case 1: // 90 degrees
                        _currentShape.Add(new Vector2Int(max_y - coord.y, coord.x));
                        break;
                    case 2: // 180 degrees
                        _currentShape.Add(new Vector2Int(max_x - coord.x, max_y - coord.y));
                        break;
                    case 3: // 270 degrees
                        _currentShape.Add(new Vector2Int(coord.y, max_x - coord.x));
                        break;
                }
            }
        }

        public InventoryItemView view { get; set; } // Link to the associated InventoryItemView
    }
}
