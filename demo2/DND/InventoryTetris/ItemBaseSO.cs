using UnityEngine;

namespace demo2.DND.InventoryTetris
{
    [CreateAssetMenu(menuName = "DND/Inventory/Item Base", fileName = "ItemBase_SO")]
    public class ItemBaseSO : ScriptableObject
    {
        [Header("基础信息")]
        public string itemId;
        public string displayName;
        public Sprite icon;

        [Header("占用格子尺寸（单位：格）")]
        [Min(1)] public int slotWidth = 1;
        [Min(1)] public int slotHeight = 1;

        [Tooltip("是否允许旋转（90°）")]
        public bool canRotate = true;
    }
}
