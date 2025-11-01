using System;
using UnityEngine;

namespace demo2.DND.InventoryTetris
{
    [Serializable]
    public class ItemInstance
    {
        public string instanceId;
        public ItemBaseSO data;
        public bool rotated; // true=旋转90°（宽高对调）

        public ItemInstance(ItemBaseSO data)
        {
            this.instanceId = Guid.NewGuid().ToString("N");
            this.data = data;
            this.rotated = false;
        }

        public int Width => rotated ? data.slotHeight : data.slotWidth;
        public int Height => rotated ? data.slotWidth : data.slotHeight;

        public void ToggleRotate()
        {
            if (data != null && data.canRotate)
            {
                rotated = !rotated;
            }
        }

        public InventoryItemView view { get; set; } // Link to the associated InventoryItemView
    }
}
