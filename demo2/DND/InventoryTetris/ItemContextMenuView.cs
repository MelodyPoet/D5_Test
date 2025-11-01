using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 右键菜单的视图组件：挂在“右键菜单预制体”的根节点或其子节点上，
    /// 用于将 Overlay/Panel/三个按钮暴露给逻辑层绑定。
    /// 设计师可以自由美化预制体，只需在此脚本中指向对应的 UI 元素即可。
    /// </summary>
    public class ItemContextMenuView : MonoBehaviour
    {
        [Header("根节点（可选，仅用于点击空白关闭）")]
        [Tooltip("全屏遮罩或菜单根节点，用于接收点击以关闭菜单；可为空（若为空则将尝试在运行时为该节点添加一个Button以接收点击)")]
        public RectTransform overlayRoot;

        [Header("菜单面板（用于定位到鼠标位置）")]
        public RectTransform panelRoot;

        [Header("按钮引用")]
        public Button btnEquip;
        public Button btnUnequip;
        public Button btnRotate;

        [Header("（可选）点击遮罩关闭按钮")]
        [Tooltip("如果overlayRoot上已经有一个Button用于点击空白区域关闭菜单，可以在这里指向它；若为空，逻辑层会尝试为overlayRoot自动添加一个Button")]
        public Button overlayCloseButton;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 开发期便捷：若未手动赋值，尝试按常见命名自动寻找
            if (panelRoot == null)
            {
                var t = transform.Find("Panel");
                if (t != null) panelRoot = t as RectTransform;
            }
            if (overlayRoot == null)
            {
                var t = transform.Find("Overlay");
                if (t != null) overlayRoot = t as RectTransform;
            }
            if (btnEquip == null || btnUnequip == null || btnRotate == null)
            {
                AutoMatchButtonsByName();
            }
        }
#endif

        // 运行时兜底：若未拖引用，尝试按通用命名自动匹配
        public void EnsureRuntimeBindings()
        {
            if (overlayRoot == null)
            {
                overlayRoot = GetComponent<RectTransform>();
            }
            if (panelRoot == null)
            {
                var t = transform.Find("Panel");
                if (t == null)
                {
                    // 尝试通过布局特征寻找
                    var rts = GetComponentsInChildren<RectTransform>(true);
                    panelRoot = rts.FirstOrDefault(rt => rt.GetComponent<VerticalLayoutGroup>() != null) ?? GetComponent<RectTransform>();
                }
                else panelRoot = t as RectTransform;
            }
            if (btnEquip == null || btnUnequip == null || btnRotate == null)
            {
                AutoMatchButtonsByName();
            }
            // 若 overlayCloseButton 未设置，后续由控制器在 overlayRoot 上补一个 Button 以接收关闭
        }

        private void AutoMatchButtonsByName()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            if (buttons == null || buttons.Length == 0) return;

            // 中英常见命名关键字
            string[] equipKeys = { "装备", "equip", "Equip" };
            string[] unequipKeys = { "卸下", "unequip", "UnEquip", "UnEquip", "Remove" };
            string[] rotateKeys = { "旋转", "rotate", "Rotate" };

            btnEquip = btnEquip ?? FindByKeys(buttons, equipKeys);
            btnUnequip = btnUnequip ?? FindByKeys(buttons, unequipKeys);
            btnRotate = btnRotate ?? FindByKeys(buttons, rotateKeys);
        }

        private Button FindByKeys(Button[] buttons, string[] keys)
        {
            foreach (var k in keys)
            {
                var b = buttons.FirstOrDefault(x => x != null && x.gameObject.name.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (b != null) return b;
            }
            return null;
        }
    }
}
