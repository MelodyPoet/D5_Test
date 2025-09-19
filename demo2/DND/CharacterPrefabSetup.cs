/*
 * 角色预制体配置指南
 *
 * 这个脚本说明了如何在角色预制体上正确配置DND_CharacterAdapter组件
 */

using UnityEngine;
using demo2.DND;

namespace demo2.DND
{
    /// <summary>
    /// 角色预制体配置助手 - 用于快速设置角色动画
    /// </summary>
    public class CharacterPrefabSetup : MonoBehaviour
    {
        [Header("自动配置设置")]
        [SerializeField] private bool autoSetupOnAwake = true;

        void Awake()
        {
            if (autoSetupOnAwake)
            {
                SetupCharacterAdapter();
            }
        }

        /// <summary>
        /// 自动配置角色适配器
        /// </summary>
        [ContextMenu("自动配置角色适配器")]
        public void SetupCharacterAdapter()
        {
            DND_CharacterAdapter adapter = GetComponent<DND_CharacterAdapter>();
            if (adapter == null)
            {
                adapter = gameObject.AddComponent<DND_CharacterAdapter>();
                Debug.Log($"为 {gameObject.name} 添加了 DND_CharacterAdapter 组件");
            }

            // 自动查找并设置组件引用
            if (adapter.characterStats == null)
            {
                adapter.characterStats = GetComponent<CharacterStats>();
            }

            if (adapter.skeletonAnimation == null)
            {
                adapter.skeletonAnimation = GetComponent<Spine.Unity.SkeletonAnimation>();
            }

            // 设置默认动画配置
            SetupDefaultAnimationMapping(adapter);

            Debug.Log($"角色 {gameObject.name} 配置完成！");
        }

        private void SetupDefaultAnimationMapping(DND_CharacterAdapter adapter)
        {
            if (adapter.animationMapping == null)
            {
                adapter.animationMapping = new DND_CharacterAdapter.AnimationMapping();
            }

            // 这些是标准的动画名称，需要在Spine中对应
            var mapping = adapter.animationMapping;

            // 基础动画
            if (string.IsNullOrEmpty(mapping.idleAnimation))
                mapping.idleAnimation = "idle";
            if (string.IsNullOrEmpty(mapping.walkAnimation))
                mapping.walkAnimation = "walk";
            if (string.IsNullOrEmpty(mapping.runAnimation))
                mapping.runAnimation = "run";

            // 战斗动画
            if (string.IsNullOrEmpty(mapping.attackAnimation))
                mapping.attackAnimation = "attack";
            if (string.IsNullOrEmpty(mapping.hitAnimation))
                mapping.hitAnimation = "hit";
            if (string.IsNullOrEmpty(mapping.deathAnimation))
                mapping.deathAnimation = "death";
            if (string.IsNullOrEmpty(mapping.unconsciousAnimation))
                mapping.unconsciousAnimation = "unconscious";

            // 技能动画
            if (string.IsNullOrEmpty(mapping.castSpellAnimation))
                mapping.castSpellAnimation = "cast";
            if (string.IsNullOrEmpty(mapping.defendAnimation))
                mapping.defendAnimation = "defend";
            if (string.IsNullOrEmpty(mapping.dodgeAnimation))
                mapping.dodgeAnimation = "dodge";
        }
    }
}

/*
角色预制体配置步骤：

1. 创建角色GameObject
2. 添加以下组件：
   - SkeletonAnimation (Spine组件)
   - CharacterStats (角色数据)
   - DND_CharacterAdapter (动画管理)
   - CharacterPrefabSetup (配置助手，可选)

3. 设置SkeletonAnimation：
   - 拖入Spine数据资产(.asset文件)
   - 设置Material和Atlas

4. 配置DND_CharacterAdapter：
   - 组件引用会自动设置
   - 调整moveSpeed (移动速度)
   - 调整attackDistance (攻击距离)
   - 设置动画名称映射

5. 在Spine动画中添加事件：
   - attack动画: 在命中帧添加"attack_hit"事件
   - death动画: 在结束帧添加"death_complete"事件
   - unconscious动画: 在开始帧添加"unconscious_start"事件
   - walk动画: 可选添加"footstep"事件

6. 测试动画：
   - 播放模式下通过代码调用adapter.PlayAttackAnimation()等方法
   - 检查事件是否正确触发
*/
