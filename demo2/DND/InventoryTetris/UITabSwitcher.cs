// filepath: d:\UnityProject\Archive\Assets\demo2\DND\InventoryTetris\UITabSwitcher.cs
using UnityEngine;
using UnityEngine.UI;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 左侧标签切换：背包/角色属性 面板显隐控制（严格手动挂载）。
    /// 使用方法：
    /// - 将本脚本挂在任意激活的场景对象（例如 UI_Root）。
    /// - 在 Inspector 中手动拖入 btnBackpack、btnCharacter、backpackPanel、characterPanel。
    /// - 启动显示通过 startState 配置（默认 None=全部隐藏）。
    /// - 可选：启用 Esc 关闭。
    /// </summary>
    public class UITabSwitcher : MonoBehaviour
    {
        public enum StartPanelState { None, Backpack, Character }

        [Header("按钮（手动拖入场景里的实例）")]
        public Button btnBackpack;
        public Button btnCharacter;

        [Header("面板（手动拖入场景里的实例）")]
        public GameObject backpackPanel;
        public GameObject characterPanel;

        [Header("启动显示状态")]
        public StartPanelState startState = StartPanelState.None;

        [Header("Esc 关闭（可选）")]
        public bool enableEscClose = true;

        private void OnEnable()
        {
            if (btnBackpack != null) btnBackpack.onClick.AddListener(ToggleBackpack);
            if (btnCharacter != null) btnCharacter.onClick.AddListener(ToggleCharacter);
        }

        private void OnDisable()
        {
            if (btnBackpack != null) btnBackpack.onClick.RemoveListener(ToggleBackpack);
            if (btnCharacter != null) btnCharacter.onClick.RemoveListener(ToggleCharacter);
        }

        private void Start()
        {
            switch (startState)
            {
                case StartPanelState.Backpack:
                    ShowBackpack();
                    break;
                case StartPanelState.Character:
                    ShowCharacter();
                    break;
                default:
                    ShowNone();
                    break;
            }
        }

        private void Update()
        {
            if (!enableEscClose) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowNone();
            }
        }

        public void ShowBackpack()
        {
            if (backpackPanel != null) backpackPanel.SetActive(true);
            if (characterPanel != null) characterPanel.SetActive(false);
        }

        public void ShowCharacter()
        {
            if (backpackPanel != null) backpackPanel.SetActive(false);
            if (characterPanel != null) characterPanel.SetActive(true);
        }

        public void ToggleBackpack()
        {
            bool isActive = backpackPanel != null && backpackPanel.activeSelf;
            if (isActive) ShowNone(); else ShowBackpack();
        }

        public void ToggleCharacter()
        {
            bool isActive = characterPanel != null && characterPanel.activeSelf;
            if (isActive) ShowNone(); else ShowCharacter();
        }

        public void ShowNone()
        {
            if (backpackPanel != null) backpackPanel.SetActive(false);
            if (characterPanel != null) characterPanel.SetActive(false);
        }
    }
}
