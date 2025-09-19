using UnityEngine;

namespace demo2.DND
{
    /// <summary>
    /// DND_CharacterAdapter调试测试脚本
    /// 用于测试攻击动画播放问题
    /// </summary>
    public class DND_CharacterAdapter_DebugTest : MonoBehaviour
    {
        [Header("测试目标")]
        public DND_CharacterAdapter targetAdapter;

        [Header("测试按钮")]
        [Space]
        [SerializeField] private bool testPlayAttack;
        [SerializeField] private bool testListAnimations;
        [SerializeField] private bool testCurrentAnimation;
        [SerializeField] private bool testSpineComponents;

        void Update()
        {
            if (targetAdapter == null) return;

            if (testPlayAttack)
            {
                testPlayAttack = false;
                TestPlayAttackAnimation();
            }

            if (testListAnimations)
            {
                testListAnimations = false;
                TestListAnimations();
            }

            if (testCurrentAnimation)
            {
                testCurrentAnimation = false;
                TestCurrentAnimation();
            }

            if (testSpineComponents)
            {
                testSpineComponents = false;
                TestSpineComponents();
            }
        }

        void TestPlayAttackAnimation()
        {
            Debug.Log("=== 开始测试攻击动画播放 ===");
            Debug.Log($"目标角色: {targetAdapter.gameObject.name}");
            Debug.Log($"当前动画状态: {targetAdapter.IsAnimating}");
            Debug.Log($"当前播放动画: {targetAdapter.CurrentAnimationName}");

            targetAdapter.PlayAttackAnimation();

            Debug.Log("=== 攻击动画播放测试完成 ===");
        }

        void TestListAnimations()
        {
            Debug.Log("=== 开始列出所有动画 ===");
            targetAdapter.ListAllAvailableAnimations();
            Debug.Log("=== 动画列表完成 ===");
        }

        void TestCurrentAnimation()
        {
            Debug.Log("=== 当前动画信息 ===");
            Debug.Log($"角色名称: {targetAdapter.gameObject.name}");
            Debug.Log($"当前动画: {targetAdapter.CurrentAnimationName}");
            Debug.Log($"是否正在动画: {targetAdapter.IsAnimating}");

            if (targetAdapter.skeletonAnimation != null)
            {
                var current = targetAdapter.skeletonAnimation.AnimationState.GetCurrent(0);
                if (current != null)
                {
                    Debug.Log($"Spine当前动画: {current.Animation.Name}");
                    Debug.Log($"动画时间: {current.TrackTime}/{current.Animation.Duration}");
                    Debug.Log($"是否循环: {current.Loop}");
                    Debug.Log($"是否完成: {current.IsComplete}");
                }
                else
                {
                    Debug.Log("Spine当前没有播放动画");
                }
            }
        }

        void TestSpineComponents()
        {
            Debug.Log("=== Spine组件状态检查 ===");

            if (targetAdapter.skeletonAnimation == null)
            {
                Debug.LogError("SkeletonAnimation组件为空！");
                return;
            }

            Debug.Log($"SkeletonAnimation: ✓");
            Debug.Log($"AnimationState: {(targetAdapter.skeletonAnimation.AnimationState != null ? "✓" : "✗")}");
            Debug.Log($"Skeleton: {(targetAdapter.skeletonAnimation.Skeleton != null ? "✓" : "✗")}");

            if (targetAdapter.skeletonAnimation.Skeleton != null)
            {
                Debug.Log($"SkeletonData: {(targetAdapter.skeletonAnimation.Skeleton.Data != null ? "✓" : "✗")}");

                if (targetAdapter.skeletonAnimation.Skeleton.Data != null)
                {
                    var data = targetAdapter.skeletonAnimation.Skeleton.Data;
                    Debug.Log($"动画数量: {data.Animations.Count}");
                    Debug.Log($"皮肤数量: {data.Skins.Count}");
                    Debug.Log($"骨骼数量: {data.Bones.Count}");
                }
            }

            // 测试直接调用SetAnimation
            Debug.Log("=== 测试直接调用SetAnimation ===");
            try
            {
                var trackEntry = targetAdapter.skeletonAnimation.AnimationState.SetAnimation(0, "Atk01", false);
                if (trackEntry != null)
                {
                    string animName = trackEntry.Animation != null ? trackEntry.Animation.Name : "<null animation>";
                    Debug.Log($"✓ 直接SetAnimation成功: {animName}");
                }
                else
                {
                    Debug.LogError("✗ 直接SetAnimation失败，返回null");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ 直接SetAnimation异常: {ex.Message}");
            }
        }

        [ContextMenu("强制测试攻击动画")]
        public void ForceTestAttack()
        {
            TestPlayAttackAnimation();
        }

        [ContextMenu("强制测试Spine组件")]
        public void ForceTestSpineComponents()
        {
            TestSpineComponents();
        }
    }
}
