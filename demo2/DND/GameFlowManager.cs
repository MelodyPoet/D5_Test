using UnityEngine;
using Spine.Unity;
using demo2.DND.UI;

namespace demo2.DND
{
    /// <summary>
    /// 游戏总流程管理器
    /// 负责控制游戏的各个阶段，例如：换装 -> 战斗
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        [Header("核心组件引用")]
        [SerializeField] private CharacterCustomizationPanel customizationPanel;
        [SerializeField] private GameObject rpgGameRoot; // 包含所有RPG战斗业务UI和逻辑的总父对象
        [SerializeField] private SkeletonAnimation playerCharacter; // 游戏中的玩家角色

        private void Start()
        {
            // 游戏开始时，直接进入换装流程
            StartCharacterCustomization();
        }

        /// <summary>
        /// 开始角色换装流程
        /// </summary>
        public void StartCharacterCustomization()
        {
            Debug.Log("[GameFlowManager] 开始角色换装流程...");

            // 确保换装面板已设置
            if (customizationPanel == null)
            {
                Debug.LogError("[GameFlowManager] CharacterCustomizationPanel 未在 Inspector 中设置！");
                return;
            }

            // 确保玩家角色已设置
            if (playerCharacter == null)
            {
                Debug.LogError("[GameFlowManager] Player Character 未在 Inspector 中设置！");
                return;
            }

            // 隐藏RPG游戏内容
            if (rpgGameRoot != null)
            {
                rpgGameRoot.SetActive(false);
            }

            // 监听换装面板的事件
            customizationPanel.OnConfirm -= OnCustomizationConfirmed; // 先移除，防止重复监听
            customizationPanel.OnConfirm += OnCustomizationConfirmed;

            customizationPanel.OnCancel -= OnCustomizationCancelled;
            customizationPanel.OnCancel += OnCustomizationCancelled;

            // 显示换装面板
            customizationPanel.gameObject.SetActive(true);

            // 将游戏角色传递给换装面板
            // 注意：CharacterCustomizationPanel 内部会处理 gameCharacter 的引用
        }

        /// <summary>
        /// 当换装完成并点击确认时调用
        /// </summary>
        private void OnCustomizationConfirmed()
        {
            Debug.Log("[GameFlowManager] 换装已确认，准备进入游戏...");

            // 隐藏换装面板
            if (customizationPanel != null)
            {
                customizationPanel.gameObject.SetActive(false);
            }

            // 开始RPG游戏
            StartRpgGame();
        }

        /// <summary>
        /// 当换装被取消时调用
        /// </summary>
        private void OnCustomizationCancelled()
        {
            Debug.Log("[GameFlowManager] 换装已取消。");
            // 根据设计，可以退回主菜单或执行其他操作
            // 此处暂时只隐藏面板
            if (customizationPanel != null)
            {
                customizationPanel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 开始RPG战斗业务
        /// </summary>
        public void StartRpgGame()
        {
            Debug.Log("[GameFlowManager] 启动RPG游戏业务...");

            // 显示RPG游戏内容
            if (rpgGameRoot != null)
            {
                rpgGameRoot.SetActive(true);
            }
            else
            {
                Debug.LogError("[GameFlowManager] RpgGameRoot 未设置，无法启动游戏！");
            }
        }

        private void OnDestroy()
        {
            // 清理事件，防止内存泄漏
            if (customizationPanel != null)
            {
                customizationPanel.OnConfirm -= OnCustomizationConfirmed;
                customizationPanel.OnCancel -= OnCustomizationCancelled;
            }
        }
    }
}
