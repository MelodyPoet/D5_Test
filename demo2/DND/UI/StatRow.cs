using UnityEngine;
using UnityEngine.UI;

namespace demo2.DND.UI
{
    /// <summary>
    /// 属性行组件 —— 挂在一个预制体上，一行包含：
    ///   [属性名标签] [值Text] [-] [+] [调整值Text] [种族加成选择按钮]
    /// 
    /// 变体人类规则：玩家可点击种族加成按钮将 +1 分配到该属性（最多2个不同属性）
    /// </summary>
    public class StatRow : MonoBehaviour
    {
        [Header("控件引用（拖入预制体的子节点）")]
        public Text labelText;   // "力量", "敏捷" 等
        public Text valueText;   // 当前属性值
        public Button minusBtn;
        public Button plusBtn;
        public Text modText;     // 调整值如 "+2"、"-1"

        [Header("种族加成选择（变体人类）")]
        [Tooltip("种族加成选择按钮（点击将+1分配到该属性）")]
        public Button racialBonusBtn;
        [Tooltip("种族加成选中标记（如高亮边框/勾选图标）")]
        public GameObject racialBonusSelectedMark;
        [Tooltip("种族加成提示文本（如\"(+1种族)\"或\"点击分配+1\"）")]
        public Text racialBonusHintText;

        [HideInInspector] public StatType statType; // 运行时赋值，标识本行对应哪个属性
    }
}
