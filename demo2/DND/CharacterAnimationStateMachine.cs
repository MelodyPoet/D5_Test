using UnityEngine;
using Spine.Unity;

/// <summary>
/// 角色动画状态机
/// 统一管理角色动画状态转换，消除复杂协程逻辑
/// </summary>
public class CharacterAnimationStateMachine : MonoBehaviour {
    public enum AnimationState {
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

    [Header("动画设置")]
    [Tooltip("动画混合时间")]
    public float mixDuration = 0.1f;
    [Tooltip("是否自动从临时状态返回idle")]
    public bool autoReturnToIdle = true;

    // 私有字段
    private CharacterStats characterStats;
    private float stateTimer = 0f;
    private AnimationState previousState = AnimationState.Idle;

    // 事件回调
    public System.Action<AnimationState> OnStateChanged;
    public System.Action<AnimationState> OnAnimationCompleted;

    private void Start() {
        if (skeletonAnimation == null) {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        if (characterStats == null) {
            characterStats = GetComponent<CharacterStats>();
        }

        // 获取动画映射
        DND_CharacterAdapter adapter = GetComponent<DND_CharacterAdapter>();
        if (adapter != null) {
            animationMapping = adapter.animationMapping;
        }
    }

    private void Update() {
        stateTimer += Time.deltaTime;
        UpdateState();
    }

    /// <summary>
    /// 切换到指定状态
    /// </summary>
    public void ChangeState(AnimationState newState) {
        if (currentState == newState) return;

        // 检查死亡状态 - 死亡后不能切换到其他状态
        if (currentState == AnimationState.Death && newState != AnimationState.Death) {
            Debug.LogWarning($"{gameObject.name} 已死亡，无法切换到状态: {newState}");
            return;
        }

        // 检查角色血量，强制切换到死亡状态
        if (characterStats != null && characterStats.currentHitPoints <= 0 && newState != AnimationState.Death) {
            newState = AnimationState.Death;
        }

        // 记录状态变化
        previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        // 播放动画
        PlayStateAnimation();

        // 触发事件
        OnStateChanged?.Invoke(currentState);

        Debug.Log($"🎭 {gameObject.name} 状态切换: {previousState} -> {currentState}");
    }

    /// <summary>
    /// 播放当前状态对应的动画
    /// </summary>
    private void PlayStateAnimation() {
        if (skeletonAnimation == null || animationMapping == null) {
            Debug.LogError($"{gameObject.name} 缺少必要的动画组件");
            return;
        }

        string animationName = GetAnimationName(currentState);
        bool loop = IsLoopAnimation(currentState);

        if (!string.IsNullOrEmpty(animationName)) {
            // 使用TrackEntry获取更好的控制
            Spine.TrackEntry trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);

            if (trackEntry != null) {
                // 设置混合时间，让状态转换更平滑
                trackEntry.MixDuration = mixDuration;

                // 为非循环动画设置完成回调
                if (!loop && autoReturnToIdle) {
                    trackEntry.Complete += OnAnimationComplete;
                }
            }

            Debug.Log($"🎭 {gameObject.name} 播放动画: {animationName} (循环: {loop})");
        }
        else {
            Debug.LogWarning($"{gameObject.name} 状态 {currentState} 对应的动画名称为空");
        }
    }

    /// <summary>
    /// 获取状态对应的动画名称
    /// </summary>
    private string GetAnimationName(AnimationState state) {
        switch (state) {
            case AnimationState.Idle: return animationMapping.idleAnimation;
            case AnimationState.Walking: return animationMapping.walkAnimation;
            case AnimationState.Attacking: return animationMapping.attackAnimation;
            case AnimationState.Hit: return animationMapping.hitAnimation;
            case AnimationState.Death: return animationMapping.deathAnimation;
            case AnimationState.Casting: return animationMapping.castAnimation;
            default: return animationMapping.idleAnimation;
        }
    }

    /// <summary>
    /// 判断动画是否循环
    /// </summary>
    private bool IsLoopAnimation(AnimationState state) {
        switch (state) {
            case AnimationState.Idle:
            case AnimationState.Walking:
                return true;
            case AnimationState.Attacking:
            case AnimationState.Hit:
            case AnimationState.Casting:
                return false;
            case AnimationState.Death:
                return false; // 死亡动画通常不循环
            default:
                return true;
        }
    }

    /// <summary>
    /// 动画完成回调
    /// </summary>
    private void OnAnimationComplete(Spine.TrackEntry trackEntry) {
        if (autoReturnToIdle && currentState != AnimationState.Idle && currentState != AnimationState.Death) {
            OnAnimationCompleted?.Invoke(currentState);
            ChangeState(AnimationState.Idle);
        }
    }

    /// <summary>
    /// 更新状态逻辑 - 简化版本，主要依靠事件回调
    /// </summary>
    private void UpdateState() {
        stateTimer += Time.deltaTime;

        // 检查角色死亡状态
        if (characterStats != null && characterStats.currentHitPoints <= 0 && currentState != AnimationState.Death) {
            ChangeState(AnimationState.Death);
        }
    }

    /// <summary>
    /// 检查当前动画是否播放完毕 - 备用方法
    /// </summary>
    private bool IsCurrentAnimationFinished() {
        if (skeletonAnimation == null) return true;

        Spine.TrackEntry trackEntry = skeletonAnimation.AnimationState.GetCurrent(0);
        if (trackEntry == null) return true;

        return trackEntry.IsComplete;
    }

    /// <summary>
    /// 公共接口方法 - 简化的外部调用
    /// </summary>
    public void StartWalking() => ChangeState(AnimationState.Walking);
    public void StopWalking() => ChangeState(AnimationState.Idle);
    public void PlayAttack() => ChangeState(AnimationState.Attacking);
    public void PlayHit() => ChangeState(AnimationState.Hit);
    public void PlayDeath() => ChangeState(AnimationState.Death);
    public void PlayCast() => ChangeState(AnimationState.Casting);
    public void PlayIdle() => ChangeState(AnimationState.Idle);

    /// <summary>
    /// 状态查询方法
    /// </summary>
    public bool IsInState(AnimationState state) => currentState == state;
    public bool IsIdle() => currentState == AnimationState.Idle;
    public bool IsWalking() => currentState == AnimationState.Walking;
    public bool IsDead() => currentState == AnimationState.Death;
    public bool IsInCombatAction() => currentState == AnimationState.Attacking || currentState == AnimationState.Hit || currentState == AnimationState.Casting;

    /// <summary>
    /// 强制设置状态（跳过死亡检查，用于特殊情况）
    /// </summary>
    public void ForceState(AnimationState state) {
        previousState = currentState;
        currentState = state;
        stateTimer = 0f;
        PlayStateAnimation();
        OnStateChanged?.Invoke(currentState);
    }

    /// <summary>
    /// 获取状态持续时间
    /// </summary>
    public float GetStateTime() => stateTimer;

    /// <summary>
    /// 获取上一个状态
    /// </summary>
    public AnimationState GetPreviousState() => previousState;
}
