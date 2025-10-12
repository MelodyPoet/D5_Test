using UnityEngine;
using System.Collections.Generic;

namespace demo2.DND
{
    [CreateAssetMenu(menuName = "Config/CharacterAnimationConfig")]
    public class CharacterAnimationConfig : ScriptableObject
    {
        // 角色唯一ID，与CharacterTemplate等保持一致
        public string characterId;

        // 动画名配置（常用动画）
        public string idleAnimation = "idle";
        public string walkAnimation = "walk";
        public string runAnimation = "run";
        public string attackAnimation = "attack";
        public string hitAnimation = "hit";
        public string deathAnimation = "death";
        public string skillAnimation = "skill";
        public string defendAnimation = "defend";
        public string dodgeAnimation = "dodge";
        public string unconsciousAnimation = "unconscious";

        // Spine事件名配置（可选）
        // 注：死亡/昏迷等关键状态请通过动画名称映射来驱动（deathAnimation / unconsciousAnimation），
        //      因为血量<=0 是确定的时间点，不需要依赖 Spine 事件触发。保留下面的事件字段用于攻击命中/脚步/状态改变等场景。
        public string attackHitEvent = "attack_hit";
        public string footstepEvent = "footstep";
        public string stateChangeEvent = "state_change";

        // 表现参数
        public float moveSpeed = 2.0f;
        public float attackDistance = 1.5f;
        public DG.Tweening.Ease moveEase = DG.Tweening.Ease.OutCubic;

        // 扩展动画表（可选，支持批量查表/特殊动画）
        [System.Serializable]
        public class AnimationEntry
        {
            public string stateName; // 动画状态名
            public string clipPath;  // 动画剪辑资源路径
            public float transitionDuration = 0.1f; // 切换时长
            public string[] tags; // 可选：标签
        }
        public List<AnimationEntry> animations = new List<AnimationEntry>();
    }
}
