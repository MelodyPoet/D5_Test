using UnityEngine;
using Spine.Unity; // 需要引用Spine-Unity包
using System.Reflection; // 用于反射

public class AnimationController : MonoBehaviour {
    // Spine骨骼动画组件
    private SkeletonAnimation skeletonAnimation;

    // 动画名称定义
    [Header("动画名称配置")]
    public string idleAnimation = "idle";
    public string walkAnimation = "walk";
    public string moveToIdleAnimation = "m_to_i"; // 移动到待机的过渡动画
    public string attackMeleeAnimation = "attack_melee";
    public string attackRangedAnimation = "attack_ranged";
    public string castSpellAnimation = "cast_spell";
    public string hitAnimation = "hit";
    public string deathAnimation = "death";
    public string dodgeAnimation = "dodge";

    // 当前动画状态
    private string currentAnimation = "idle";

    // 动画状态枚举
    private enum AnimationState {
        Idle,
        Walking,
        TransitionToIdle,
        Attacking,
        Casting,
        Hit,
        Death,
        Dodge
    }

    private AnimationState currentState = AnimationState.Idle;

    // 动画完成回调
    public delegate void AnimationCompleteHandler(string animationName);
    public event AnimationCompleteHandler OnAnimationComplete;

    private void Awake() {
        // 获取SkeletonAnimation组件
        skeletonAnimation = GetComponent<SkeletonAnimation>();

        if (skeletonAnimation == null) {
            Debug.LogError("SkeletonAnimation组件未找到! 请确保已添加Spine骨骼动画组件。", gameObject);
            return;
        }

        // 注册动画完成事件
        skeletonAnimation.AnimationState.Complete += HandleAnimationComplete;

        // 默认播放待机动画
        PlayAnimation(idleAnimation, true);
    }

    // 播放指定动画
    public void PlayAnimation(string animationName, bool loop = false) {
        if (skeletonAnimation == null) return;

        // 检查动画是否存在
        Spine.Animation animation = skeletonAnimation.Skeleton.Data.FindAnimation(animationName);
        if (animation == null) {
            Debug.LogWarning($"动画 {animationName} 不存在! 请检查Spine资产中的动画名称。", gameObject);
            return;
        }

        // 设置动画
        skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
        currentAnimation = animationName;

        Debug.Log($"{gameObject.name} 播放动画: {animationName}, 循环: {loop}");
    }

    // 添加动画到队列
    public void AddAnimation(string animationName, bool loop = false, float delay = 0) {
        if (skeletonAnimation == null) return;

        // 检查动画是否存在
        Spine.Animation animation = skeletonAnimation.Skeleton.Data.FindAnimation(animationName);
        if (animation == null) {
            Debug.LogWarning($"动画 {animationName} 不存在! 请检查Spine资产中的动画名称。", gameObject);
            return;
        }

        // 添加动画到队列
        skeletonAnimation.AnimationState.AddAnimation(0, animationName, loop, delay);
    }

    // 动画完成事件处理
    private void HandleAnimationComplete(Spine.TrackEntry trackEntry) {
        string animName = trackEntry.Animation.Name;

        // 触发完成事件
        OnAnimationComplete?.Invoke(animName);

        // 检查是否是死亡动画
        if (animName == deathAnimation) {
            // 如果是死亡动画，不要自动切换回待机状态
            Debug.Log($"{gameObject.name} 死亡动画播放完成，保持死亡状态");
            currentState = AnimationState.Death;
            return;
        }

        // 检查是否是移动到待机的过渡动画
        if (animName == moveToIdleAnimation) {
            // 过渡动画完成，切换到待机动画
            if (!IsCharacterDead()) {
                PlayAnimation(idleAnimation, true);
                currentState = AnimationState.Idle;
                Debug.Log($"{gameObject.name} 过渡动画完成，切换到待机动画");
            }
            else {
                PlayAnimation(deathAnimation, false);
                currentState = AnimationState.Death;
            }
            return;
        }

        // 如果是非循环动画完成且不是死亡动画，自动恢复到待机状态
        if (animName != idleAnimation && !trackEntry.Loop) {
            // 检查角色是否已死亡
            if (IsCharacterDead()) {
                // 如果角色已死亡，播放死亡动画
                Debug.Log($"{gameObject.name} 已死亡，播放死亡动画而不是待机动画");
                PlayAnimation(deathAnimation, false);
                currentState = AnimationState.Death;
            }
            else {
                // 角色未死亡，恢复待机状态
                PlayAnimation(idleAnimation, true);
                currentState = AnimationState.Idle;
            }
        }
    }

    // 获取当前动画名称
    public string GetCurrentAnimation() {
        return currentAnimation;
    }

    // 检查角色是否已死亡
    private bool IsCharacterDead() {
        // 尝试获取父对象上的CharacterStats组件
        MonoBehaviour charStats = GetComponentInParent<MonoBehaviour>();
        if (charStats != null && charStats.GetType().Name == "CharacterStats") {
            // 使用反射获取currentHitPoints属性
            System.Reflection.PropertyInfo property = charStats.GetType().GetProperty("currentHitPoints");
            if (property != null) {
                object hitPoints = property.GetValue(charStats);
                if (hitPoints != null && hitPoints is int currentHP && currentHP <= 0) {
                    return true;
                }
            }
        }
        return false;
    }

    // 播放待机动画
    public void PlayIdle() {
        // 检查角色是否已死亡
        if (IsCharacterDead()) {
            // 如果角色已死亡，播放死亡动画
            Debug.Log($"{gameObject.name} 已死亡，不播放待机动画");
            PlayAnimation(deathAnimation, false);
            currentState = AnimationState.Death;
        }
        else {
            // 角色未死亡，播放待机动画
            PlayAnimation(idleAnimation, true);
            currentState = AnimationState.Idle;
        }
    }

    // 播放行走动画
    public void PlayWalk() {
        // 检查角色是否已死亡
        if (IsCharacterDead()) {
            // 如果角色已死亡，播放死亡动画
            Debug.Log($"{gameObject.name} 已死亡，不播放行走动画");
            PlayAnimation(deathAnimation, false);
            currentState = AnimationState.Death;
        }
        else {
            // 角色未死亡，播放行走动画
            PlayAnimation(walkAnimation, true);
            currentState = AnimationState.Walking;
        }
    }

    // 停止行走动画并播放过渡动画
    public void StopWalkWithTransition() {
        // 检查角色是否已死亡
        if (IsCharacterDead()) {
            // 如果角色已死亡，播放死亡动画
            Debug.Log($"{gameObject.name} 已死亡，不播放过渡动画");
            PlayAnimation(deathAnimation, false);
            currentState = AnimationState.Death;
        }
        else if (currentState == AnimationState.Walking) {
            // 检查过渡动画是否存在
            Spine.Animation transitionAnim = skeletonAnimation.Skeleton.Data.FindAnimation(moveToIdleAnimation);
            if (transitionAnim != null) {
                // 播放过渡动画
                PlayAnimation(moveToIdleAnimation, false);
                currentState = AnimationState.TransitionToIdle;
                Debug.Log($"{gameObject.name} 播放移动到待机的过渡动画: {moveToIdleAnimation}");
            }
            else {
                // 如果过渡动画不存在，直接播放待机动画
                Debug.LogWarning($"{gameObject.name} 过渡动画 {moveToIdleAnimation} 不存在，直接切换到待机动画");
                PlayAnimation(idleAnimation, true);
                currentState = AnimationState.Idle;
            }
        }
        else {
            // 如果当前不是行走状态，直接播放待机动画
            PlayAnimation(idleAnimation, true);
            currentState = AnimationState.Idle;
        }
    }

    // 播放近战攻击动画
    public void PlayMeleeAttack() {
        // 检查角色是否已死亡
        if (IsCharacterDead()) {
            // 如果角色已死亡，播放死亡动画
            Debug.Log($"{gameObject.name} 已死亡，不播放攻击动画");
            PlayAnimation(deathAnimation, false);
            currentState = AnimationState.Death;
        }
        else {
            // 角色未死亡，播放近战攻击动画
            PlayAnimation(attackMeleeAnimation, false);
            currentState = AnimationState.Attacking;
        }
    }

    // 播放远程攻击动画
    public void PlayRangedAttack() {
        // 检查角色是否已死亡
        if (IsCharacterDead()) {
            // 如果角色已死亡，播放死亡动画
            Debug.Log($"{gameObject.name} 已死亡，不播放攻击动画");
            PlayAnimation(deathAnimation, false);
            currentState = AnimationState.Death;
        }
        else {
            // 角色未死亡，播放远程攻击动画
            PlayAnimation(attackRangedAnimation, false);
            currentState = AnimationState.Attacking;
        }
    }

    // 播放施法动画
    public void PlayCastSpell() {
        // 检查角色是否已死亡
        if (IsCharacterDead()) {
            // 如果角色已死亡，播放死亡动画
            Debug.Log($"{gameObject.name} 已死亡，不播放施法动画");
            PlayAnimation(deathAnimation, false);
            currentState = AnimationState.Death;
        }
        else {
            // 角色未死亡，播放施法动画
            PlayAnimation(castSpellAnimation, false);
            currentState = AnimationState.Casting;
        }
    }

    // 播放受击动画
    public void PlayHit() {
        // 检查角色是否已死亡
        if (IsCharacterDead()) {
            // 如果角色已死亡，播放死亡动画
            Debug.Log($"{gameObject.name} 已死亡，不播放受击动画");
            PlayAnimation(deathAnimation, false);
            currentState = AnimationState.Death;
        }
        else {
            // 角色未死亡，播放受击动画
            PlayAnimation(hitAnimation, false);
            currentState = AnimationState.Hit;
        }
    }

    // 播放死亡动画
    public void PlayDeath() {
        // 直接播放死亡动画，不需要检查
        PlayAnimation(deathAnimation, false);
        currentState = AnimationState.Death;
        Debug.Log($"{gameObject.name} 播放死亡动画");
    }

    // 播放闪避动画
    public void PlayDodge() {
        // 检查角色是否已死亡
        if (IsCharacterDead()) {
            // 如果角色已死亡，播放死亡动画
            Debug.Log($"{gameObject.name} 已死亡，不播放闪避动画");
            PlayAnimation(deathAnimation, false);
            currentState = AnimationState.Death;
        }
        else {
            // 角色未死亡，播放闪避动画
            PlayAnimation(dodgeAnimation, false);
            currentState = AnimationState.Dodge;
        }
    }

    // 获取动画持续时间
    public float GetAnimationDuration(string animationName) {
        if (skeletonAnimation == null) return 0f;

        Spine.Animation animation = skeletonAnimation.Skeleton.Data.FindAnimation(animationName);
        if (animation == null) return 0f;

        return animation.Duration;
    }

    private void OnDestroy() {
        // 取消注册事件
        if (skeletonAnimation != null) {
            skeletonAnimation.AnimationState.Complete -= HandleAnimationComplete;
        }
    }
}
