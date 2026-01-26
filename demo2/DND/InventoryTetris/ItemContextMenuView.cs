using UnityEngine;
using UnityEngine.UI;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// A simple view component that holds references to the UI elements of the item context menu.
    /// </summary>
    public class ItemContextMenuView : MonoBehaviour
    {
        public Button equipButton;
        public Button unequipButton;
        public Button rotateButton;
        // Drop / Close 按钮根据需求已移除，菜单的关闭由逻辑控制
    }
}
