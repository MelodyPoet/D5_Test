using UnityEngine;
using demo2.DND.Core.Events.Channels;
using demo2.DND; // 引入 CharacterStats 所在命名空间
using demo2.DND.HorizontalFormation; // 引入 BattleSide 枚举

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 当前角色管理器（单例）：维护“当前选中角色”，并通过事件通道广播变更。
    /// 建议挂载在场景的 IdleGameSystem 节点下。
    /// </summary>
    public class ActiveCharacterManager : MonoBehaviour
    {
        public static ActiveCharacterManager Instance { get; private set; }

        [Header("事件通道（拖入资产）")]
        [SerializeField] private ActiveCharacterChangedChannel_SO activeCharacterChangedChannel;

        [Header("当前选中角色（运行时）")]
        [SerializeField] private CharacterStats current;
        public CharacterStats Current => current;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // 可选：从场景中推断一个默认角色（第一个玩家阵营）
            if (current == null)
            {
                var all = FindObjectsOfType<CharacterStats>();
                foreach (var c in all)
                {
                    if (c != null && c.battleSide == BattleSide.Player)
                    {
                        current = c;
                        break;
                    }
                }
            }

            if (current != null)
            {
                PublishActiveChanged(current);
            }
        }

        public void SetActive(CharacterStats stats)
        {
            if (stats == null || stats == current) return;
            current = stats;
            PublishActiveChanged(stats);
        }

        private void PublishActiveChanged(CharacterStats stats)
        {
            if (activeCharacterChangedChannel != null)
            {
                activeCharacterChangedChannel.RaiseEvent(stats);
            }
            else
            {
                Debug.LogWarning("[ActiveCharacterManager] 未配置 ActiveCharacterChangedChannel 资产，无法广播当前角色切换事件。");
            }
        }
    }
}
