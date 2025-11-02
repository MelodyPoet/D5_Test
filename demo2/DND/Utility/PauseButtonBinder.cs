using UnityEngine;
using UnityEngine.UI;

namespace demo2.DND.Utility
{
    /// <summary>
    /// 将一个 UI Button 与 PauseController 绑定：
    /// - 点击按钮 => 切换暂停/继续；
    /// - 按钮文字随暂停状态自动显示（未暂停显示 "暂停"，已暂停显示 "继续"）。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PauseButtonBinder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Text label;

        [Header("Texts")]
        [Tooltip("游戏未暂停时按钮上显示的文字（点击后会进入暂停）")]
        [SerializeField] private string runningText = "暂停";
        [Tooltip("游戏已暂停时按钮上显示的文字（点击后会恢复运行）")]
        [SerializeField] private string pausedText = "继续";

        [Tooltip("在脚本启用时立即刷新一次按钮文字")]
        [SerializeField] private bool reflectStateOnEnable = true;

        private bool lastKnownPaused;

        private void Reset()
        {
            AutoFindRefs();
        }

        private void Awake()
        {
            AutoFindRefs();
            if (button != null)
            {
                // Disable keyboard/gamepad navigation for the Pause button to avoid Submit via Space triggering it.
                try
                {
                    var nav = button.navigation;
                    nav.mode = Navigation.Mode.None;
                    button.navigation = nav;
                }
                catch { }

                button.onClick.AddListener(OnClick);
            }
        }

        private void OnEnable()
        {
            if (reflectStateOnEnable)
            {
                bool isPaused = PauseController.Instance != null && PauseController.Instance.IsPaused;
                lastKnownPaused = isPaused;
                UpdateLabel(isPaused);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }

        private void Update()
        {
            // Poll for pause state changes so the label stays in sync even if Pause is toggled elsewhere.
            if (PauseController.Instance != null)
            {
                bool isPaused = PauseController.Instance.IsPaused;
                if (isPaused != lastKnownPaused)
                {
                    lastKnownPaused = isPaused;
                    UpdateLabel(isPaused);
                }
            }
        }

        private void OnClick()
        {
            if (PauseController.Instance == null)
            {
                Debug.LogWarning("[PauseButtonBinder] 未找到 PauseController.Instance，无法切换暂停。");
                return;
            }
            PauseController.Instance.Toggle();
            // Update label immediately to reflect the action we just triggered
            bool isPaused = PauseController.Instance != null && PauseController.Instance.IsPaused;
            lastKnownPaused = isPaused;
            UpdateLabel(isPaused);
        }

        private void UpdateLabel(bool isPaused)
        {
            if (label != null)
            {
                label.text = isPaused ? pausedText : runningText;
            }
        }

        private void AutoFindRefs()
        {
            if (button == null) button = GetComponent<Button>();
            if (label == null) label = GetComponentInChildren<Text>(true);
        }
    }
}
