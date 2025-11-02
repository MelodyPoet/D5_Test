using System;
using System.Collections.Generic;
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
        [Tooltip("跨场景保留该控制器")]
        public bool persistAcrossScenes;

        [Tooltip("开始时是否处于暂停状态")]
        public bool startPaused = false;

        public bool IsPaused { get; private set; }

        public static PauseController Instance { get; private set; }

        // Record the frame in which Pause/Resume was last executed so UI can ignore same-frame toggles
        public static int LastToggleFrame = -1;

        // How many frames after a pause/resume toggle should UI ignore incoming show/toggle requests
        [Tooltip("How many frames after a pause/resume toggle UI components should ignore incoming show/toggle requests to avoid race conditions.")]
        public int suppressionFrames = 1;

        /// <summary>
        /// Returns true if UI should ignore changes because a pause/resume toggle happened very recently.
        /// </summary>
        public bool ShouldIgnoreUIChanges()
        {
            if (LastToggleFrame < 0) return false;
            return Time.frameCount <= LastToggleFrame + suppressionFrames;
        }

        /// <summary>
        /// Static helper so callers don't need a PauseController instance reference.
        /// </summary>
        public static bool StaticShouldIgnoreUIChanges(int suppressionFramesToCheck = 1)
        {
            if (LastToggleFrame < 0) return false;
            return Time.frameCount <= LastToggleFrame + suppressionFramesToCheck;
        }

        // Removed: explicit UI listener subsystem. PauseController will no longer notify UI listeners by default.

        // Add a list of excluded components that should not be affected by pause
        [Header("Excluded Components")]
        [Tooltip("UI components that should not be affected by the pause state.")]
        public List<MonoBehaviour> excludedComponents;

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

        // NOTE: Keyboard toggle removed. Pause is now controlled only via explicit calls (e.g. PauseButtonBinder or other UI).

        public void Pause()
        {
            if (IsPaused) return;
            Time.timeScale = 0f;
            AudioListener.pause = true; // 如不希望静音，可改为 false
            IsPaused = true;

            // Record toggle frame
            LastToggleFrame = Time.frameCount;

            // NO UI notifications by default: PauseController does not change UI states automatically.

            foreach (var component in excludedComponents)
            {
                if (component is IExcludableFromPause excludable)
                {
                    excludable.OnPauseExcluded();
                }
            }
        }

        public void Resume()
        {
            if (!IsPaused) return;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsPaused = false;

            // Record toggle frame
            LastToggleFrame = Time.frameCount;

            // NO UI notifications by default: PauseController does not change UI states automatically.

            foreach (var component in excludedComponents)
            {
                if (component is IExcludableFromPause excludable)
                {
                    excludable.OnPauseExcluded();
                }
            }
        }

        public void Toggle()
        {
            if (IsPaused) Resume(); else Pause();
        }

        public void SetPaused(bool paused)
        {
            if (paused) Pause(); else Resume();
        }

        public interface IExcludableFromPause
        {
            void OnPauseExcluded();
        }
    }
}
