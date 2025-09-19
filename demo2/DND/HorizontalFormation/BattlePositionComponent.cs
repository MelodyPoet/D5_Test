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

        [Header("排位信息")]
        public RowPosition rowPosition = RowPosition.Front;

        [Header("位置状态")]
        [Tooltip("该位置是否被占用")]
        public bool isOccupied = true;

        [Header("调试信息")]
        [Tooltip("显示位置信息（仅在编辑器中有效）")]
        public bool showDebugInfo; // 移除默认值初始化

        void Start() {
            // 标记该位置为已占用
            isOccupied = true;

            // 根据当前位置自动设置排位信息
            UpdateRowPosition();
        }

        /// <summary>
        /// 根据当前位置更新排位信息
        /// </summary>
        void UpdateRowPosition()
        {
            rowPosition = HorizontalFormationAI.IsFrontRow(currentPosition) ? RowPosition.Front : RowPosition.Back;
        }

        /// <summary>
        /// 设置新的战斗位置
        /// </summary>
        public void SetPosition(HorizontalPosition newPosition)
        {
            currentPosition = newPosition;
            UpdateRowPosition();
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
        /// 检查是否为前排位置
        /// </summary>
        public bool IsFrontRow()
        {
            return rowPosition == RowPosition.Front;
        }

        /// <summary>
        /// 检查是否为后排位置
        /// </summary>
        public bool IsBackRow()
        {
            return rowPosition == RowPosition.Back;
        }

        /// <summary>
        /// 获取所属阵营
        /// </summary>
        public BattleSide GetBattleSide()
        {
            return HorizontalFormationAI.GetPositionSide(currentPosition);
        }

        void OnDrawGizmosSelected()
        {
            if (showDebugInfo)
            {
                Gizmos.color = IsFrontRow() ? Color.red : Color.blue;
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            }
        }
    }
}
