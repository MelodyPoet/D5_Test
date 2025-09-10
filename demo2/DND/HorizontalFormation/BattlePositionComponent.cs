using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 战斗位置组件
    /// 用于标识角色在战斗中的位置
    /// </summary>
    public class BattlePositionComponent : MonoBehaviour {
        [Header("战斗位置")]
        public HorizontalPosition currentPosition;

        [Header("位置状态")]
        [Tooltip("该位置是否被占用")]
        public bool isOccupied = true;

        [Header("调试信息")]
        [Tooltip("显示位置信息（仅在编辑器中有效）")]
        public bool showDebugInfo; // 移除默认值初始化

        void Start() {
            // 标记该位置为已占用
            isOccupied = true;
        }

        /// <summary>
        /// 获取位置的显示名称
        /// </summary>
        public string GetPositionDisplayName() {
            switch (currentPosition) {
                case HorizontalPosition.PlayerFrontLeft:
                    return "玩家前排左翼";
                case HorizontalPosition.PlayerFrontCenter:
                    return "玩家前排中锋";
                case HorizontalPosition.PlayerFrontRight:
                    return "玩家前排右翼";
                case HorizontalPosition.PlayerBackLeft:
                    return "玩家后排左翼";
                case HorizontalPosition.PlayerBackCenter:
                    return "玩家后排中路";
                case HorizontalPosition.PlayerBackRight:
                    return "玩家后排右翼";
                case HorizontalPosition.EnemyFrontLeft:
                    return "敌人前排左翼";
                case HorizontalPosition.EnemyFrontCenter:
                    return "敌人前排中锋";
                case HorizontalPosition.EnemyFrontRight:
                    return "敌人前排右翼";
                case HorizontalPosition.EnemyBackLeft:
                    return "敌人后排左翼";
                case HorizontalPosition.EnemyBackCenter:
                    return "敌人后排中路";
                case HorizontalPosition.EnemyBackRight:
                    return "敌人后排右翼";
                default:
                    return "未知位置";
            }
        }

        /// <summary>
        /// 检查是否为玩家位置
        /// </summary>
        public bool IsPlayerPosition() {
            return currentPosition <= HorizontalPosition.PlayerBackRight;
        }

        /// <summary>
        /// 检查是否为敌人位置
        /// </summary>
        public bool IsEnemyPosition() {
            return currentPosition >= HorizontalPosition.EnemyFrontLeft;
        }

        /// <summary>
        /// 检查是否为前排位置
        /// </summary>
        public bool IsFrontPosition() {
            return currentPosition == HorizontalPosition.PlayerFrontLeft ||
                   currentPosition == HorizontalPosition.PlayerFrontCenter ||
                   currentPosition == HorizontalPosition.PlayerFrontRight ||
                   currentPosition == HorizontalPosition.EnemyFrontLeft ||
                   currentPosition == HorizontalPosition.EnemyFrontCenter ||
                   currentPosition == HorizontalPosition.EnemyFrontRight;
        }

        /// <summary>
        /// 检查是否为后排位置
        /// </summary>
        public bool IsBackPosition() {
            return !IsFrontPosition();
        }

        void OnDestroy() {
            // 当组件被销毁时，释放位置
            isOccupied = false;
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            if (showDebugInfo) {
                // 在编辑器中显示位置信息
                Gizmos.color = IsPlayerPosition() ? Color.blue : Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.5f);

                // 显示位置名称
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, GetPositionDisplayName());
            }
        }
#endif
    }
}
