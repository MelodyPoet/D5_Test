using System;
using UnityEngine;

namespace demo2.DND.Utility
{
    /// <summary>
    /// 运行时暂停控制器：使用 Time.timeScale 切换暂停，从而在暂停时仍然可以操作 UI（例如滚动日志）。
    /// 将该脚本挂到一个常驻物体（如 GameManager）上，或勾选 "Persist Across Scenes" 以跨场景保留。
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("切换暂停/继续的快捷键（运行时有效）")]
        public KeyCode toggleKey = KeyCode.Space;

        [Tooltip("跨场景保留该控制器")]
        public bool persistAcrossScenes = true;

        [Tooltip("开始时是否处于暂停状态")]
        public bool startPaused = false;

        public bool IsPaused { get; private set; }

        public static PauseController Instance { get; private set; }

        // 新增：暂停状态变化事件（参数为当前是否处于暂停）
        public static event Action<bool> OnPauseStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            if (startPaused)
                Pause();
            else
                Resume();
        }

        private void Update()
        {
            // 注意：当使用 Unity 编辑器的“暂停按钮”时，Update 不会执行；
            // 我们仅在运行时通过快捷键来切换 Time.timeScale，从而保持 UI 可交互。
            if (Input.GetKeyDown(toggleKey))
            {
                if (IsPaused) Resume(); else Pause();
            }
        }

        public void Pause()
        {
            if (IsPaused) return;
            Time.timeScale = 0f;
            AudioListener.pause = true; // 如不希望静音，可改为 false
            IsPaused = true;
            OnPauseStateChanged?.Invoke(IsPaused);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsPaused = false;
            OnPauseStateChanged?.Invoke(IsPaused);
        }

        public void Toggle()
        {
            if (IsPaused) Resume(); else Pause();
        }

        public void SetPaused(bool paused)
        {
            if (paused) Pause(); else Resume();
        }
    }
}
