using UnityEngine;
using Spine.Unity;

/// <summary>
/// 角色动画状态机 - 简化版本
/// 用于管理角色的动画状态转换，避免复杂的协程和延迟逻辑
/// </summary>
public class CharacterAnimationStateMachine : MonoBehaviour
{

    public enum AnimationState
    {
        Idle,
        Walking,
        Attacking,
        Hit,
        Death,
        Casting
    }

    [Header("动画组件")]
    public SkeletonAnimation skeletonAnimation;
    public DND_CharacterAdapter.AnimationMapping animationMapping;

    [Header("状态管理")]
    public AnimationState currentState = AnimationState.Idle;

    private CharacterStats characterStats;
    private float stateTimer = 0f;

    private void Start()
    {
        if (skeletonAnimation == null)
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        if (characterStats == null)
        {
            characterStats = GetComponent<CharacterStats>();
        }

        // 获取动画映射
        DND_CharacterAdapter adapter = GetComponent<DND_CharacterAdapter>();
        if (adapter != null)
        {
            animationMapping = adapter.animationMapping;
        }
    }

    private void Update()
    {
        stateTimer += Time.deltaTime;
        UpdateState();
    }

    /// <summary>
    /// 切换到指定状态
    /// </summary>
    public void ChangeState(AnimationState newState)
    {
        if (currentState == newState) return;

        // 检查死亡状态
        if (characterStats != null && characterStats.currentHitPoints <= 0 && newState != AnimationState.Death)
        {
            newState = AnimationState.Death;
        }

        currentState = newState;
        stateTimer = 0f;
        PlayStateAnimation();
    }

    /// <summary>
    /// 播放当前状态对应的动画
    /// </summary>
    private void PlayStateAnimation()
    {
        if (skeletonAnimation == null || animationMapping == null) return;

        string animationName = "";
        bool loop = true;

        switch (currentState)
        {
            case AnimationState.Idle:
                animationName = animationMapping.idleAnimation;
                loop = true;
                break;
            case AnimationState.Walking:
                animationName = animationMapping.walkAnimation;
                loop = true;
                break;
            case AnimationState.Attacking:
                animationName = animationMapping.attackAnimation;
                loop = false;
                break;
            case AnimationState.Hit:
                animationName = animationMapping.hitAnimation;
                loop = false;
                break;
            case AnimationState.Death:
                animationName = animationMapping.deathAnimation;
                loop = false;
                break;
            case AnimationState.Casting:
                animationName = animationMapping.castAnimation;
                loop = false;
                break;
        }

        if (!string.IsNullOrEmpty(animationName))
        {
            skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
            Debug.Log($"🎭 {gameObject.name} 切换到状态: {currentState} -> 动画: {animationName}");
        }
    }

    /// <summary>
    /// 更新状态逻辑
    /// </summary>
    private void UpdateState()
    {
        switch (currentState)
        {
            case AnimationState.Attacking:
            case AnimationState.Hit:
            case AnimationState.Casting:
                // 检查非循环动画是否播放完毕
                if (IsCurrentAnimationFinished())
                {
                    ChangeState(AnimationState.Idle);
                }
                break;
        }
    }

    /// <summary>
    /// 检查当前动画是否播放完毕
    /// </summary>
    private bool IsCurrentAnimationFinished()
    {
        if (skeletonAnimation == null) return true;

        var trackEntry = skeletonAnimation.AnimationState.GetCurrent(0);
        if (trackEntry == null) return true;

        return trackEntry.IsComplete;
    }

    /// <summary>
    /// 公共接口方法
    /// </summary>
    public void StartWalking() => ChangeState(AnimationState.Walking);
    public void StopWalking() => ChangeState(AnimationState.Idle);
    public void PlayAttack() => ChangeState(AnimationState.Attacking);
    public void PlayHit() => ChangeState(AnimationState.Hit);
    public void PlayDeath() => ChangeState(AnimationState.Death);
    public void PlayCast() => ChangeState(AnimationState.Casting);
}
