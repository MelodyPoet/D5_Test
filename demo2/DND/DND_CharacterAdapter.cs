using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using DND5E;

public class DND_CharacterAdapter : MonoBehaviour {
    // 角色统计数据
    public CharacterStats characterStats;

    // Spine动画组件
    public SkeletonAnimation skeletonAnimation;

    // 动画名称映射
    [System.Serializable]
    public class AnimationMapping {
        public string idleAnimation = "idle";
        public string walkAnimation = "walk";
        public string moveToIdleAnimation = "m_to_i"; // 移动到待机的过渡动画
        public string attackAnimation = "attack";
        public string hitAnimation = "hit";
        public string deathAnimation = "death";
        public string castAnimation = "cast";
    }

    public AnimationMapping animationMapping = new AnimationMapping();

    // 当前动画状态
    private string currentAnimation;

    // 初始化
    private void Start() {
        // 获取角色统计数据
        if (characterStats == null) {
            characterStats = GetComponent<CharacterStats>();
            if (characterStats == null) {
                characterStats = gameObject.AddComponent<CharacterStats>();
            }
        }

        // 获取Spine动画组件
        if (skeletonAnimation == null) {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        // 播放空闲动画
        PlayAnimation(animationMapping.idleAnimation, true);

        // 注册事件
        // 战斗管理器已迁移至挂机系统
    }

    // 回合开始事件处理
    private void OnTurnStart(CharacterStats character) {
        // 如果是自己的回合且没有死亡，播放空闲动画
        if (character == characterStats && characterStats.currentHitPoints > 0) {
            PlayAnimation(animationMapping.idleAnimation, true);
        }
    }

    // 播放动画
    public void PlayAnimation(string animationName, bool loop) {
        if (skeletonAnimation != null && !string.IsNullOrEmpty(animationName)) {
            skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
            currentAnimation = animationName;
        }
    }

    // 播放攻击动画
    public void PlayAttackAnimation() {
        PlayAnimation(animationMapping.attackAnimation, false);
        StartCoroutine(ReturnToIdle(skeletonAnimation.AnimationState.GetCurrent(0).Animation.Duration));
    }

    // 播放受击动画
    public void PlayHitAnimation() {
        try {
            if (skeletonAnimation == null) {
                Debug.LogError($"PlayHitAnimation: {gameObject.name} 的skeletonAnimation为null");
                skeletonAnimation = GetComponent<SkeletonAnimation>();
                if (skeletonAnimation == null) {
                    Debug.LogError($"PlayHitAnimation: {gameObject.name} 没有SkeletonAnimation组件");
                    return;
                }
            }

            string hitAnimName = animationMapping.hitAnimation;
            if (string.IsNullOrEmpty(hitAnimName)) {
                Debug.LogError($"PlayHitAnimation: {gameObject.name} 的hitAnimation名称为空");
                hitAnimName = "hit"; // 使用默认名称
            }

            // 检查动画是否存在
            bool animExists = false;
            if (skeletonAnimation.skeleton != null && skeletonAnimation.skeleton.Data != null) {
                foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations) {
                    if (anim.Name == hitAnimName) {
                        animExists = true;
                        break;
                    }
                }

                if (!animExists) {
                    Debug.LogError($"PlayHitAnimation: {gameObject.name} 的SkeletonData中不存在名为 {hitAnimName} 的动画");
                    // 列出所有可用的动画
                    string availableAnims = "可用动画: ";
                    foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations) {
                        availableAnims += anim.Name + ", ";
                    }
                    Debug.Log(availableAnims);

                    // 尝试使用其他可能的受击动画名称
                    string[] possibleHitAnims = { "hit", "hurt", "damage", "injured" };
                    foreach (string possibleAnim in possibleHitAnims) {
                        foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations) {
                            if (anim.Name.ToLower().Contains(possibleAnim)) {
                                hitAnimName = anim.Name;
                                animExists = true;
                                Debug.Log($"找到可能的受击动画: {hitAnimName}");
                                break;
                            }
                        }
                        if (animExists) break;
                    }
                }
            }
            else {
                Debug.LogError($"PlayHitAnimation: {gameObject.name} 的skeleton或skeletonData为null");
                return;
            }

            // 停止所有当前动画，确保受击动画能够播放
            skeletonAnimation.AnimationState.ClearTrack(0);

            // 播放动画，使用较高的混合时间确保平滑过渡
            if (skeletonAnimation != null && !string.IsNullOrEmpty(hitAnimName)) {
                // 直接使用AnimationState API，确保动画能够播放
                Spine.TrackEntry trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, hitAnimName, false);

                // 设置混合时间
                trackEntry.MixDuration = 0.1f;

                // 使用正常的时间缩放
                trackEntry.TimeScale = 1.0f;

                // 记录当前动画
                currentAnimation = hitAnimName;

                // 获取动画持续时间
                float duration = trackEntry.Animation.Duration;

                // 使用协程在动画结束后返回空闲状态
                // 增加一点额外时间，确保动画能够完全播放
                StartCoroutine(ReturnToIdle(duration + 0.1f));
            }
            else {
                Debug.LogError($"无法播放受击动画: skeletonAnimation={skeletonAnimation}, hitAnimName={hitAnimName}");
            }
        }
        catch (System.Exception e) {
            Debug.LogError($"PlayHitAnimation出错: {e.Message}\n{e.StackTrace}");
        }
    }

    // 播放死亡动画
    public void PlayDeathAnimation() {
        try {
            if (skeletonAnimation == null) {
                Debug.LogError($"PlayDeathAnimation: {gameObject.name} 的skeletonAnimation为null");
                skeletonAnimation = GetComponent<SkeletonAnimation>();
                if (skeletonAnimation == null) {
                    Debug.LogError($"PlayDeathAnimation: {gameObject.name} 没有SkeletonAnimation组件");
                    return;
                }
            }

            string deathAnimName = animationMapping.deathAnimation;
            if (string.IsNullOrEmpty(deathAnimName)) {
                Debug.LogError($"PlayDeathAnimation: {gameObject.name} 的deathAnimation名称为空");
                deathAnimName = "dead"; // 使用默认名称
            }

            // 检查动画是否存在
            bool animExists = false;
            if (skeletonAnimation.skeleton != null && skeletonAnimation.skeleton.Data != null) {
                foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations) {
                    if (anim.Name == deathAnimName) {
                        animExists = true;
                        break;
                    }
                }

                if (!animExists) {
                    Debug.LogError($"PlayDeathAnimation: {gameObject.name} 的SkeletonData中不存在名为 {deathAnimName} 的动画");
                    // 列出所有可用的动画
                    string availableAnims = "可用动画: ";
                    foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations) {
                        availableAnims += anim.Name + ", ";
                    }
                    Debug.Log(availableAnims);

                    // 尝试使用其他可能的死亡动画名称
                    string[] possibleDeathAnims = { "dead", "death", "die", "defeat" };
                    foreach (string possibleAnim in possibleDeathAnims) {
                        foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations) {
                            if (anim.Name.ToLower().Contains(possibleAnim)) {
                                deathAnimName = anim.Name;
                                animExists = true;
                                Debug.Log($"找到可能的死亡动画: {deathAnimName}");
                                break;
                            }
                        }
                        if (animExists) break;
                    }
                }
            }
            else {
                Debug.LogError($"PlayDeathAnimation: {gameObject.name} 的skeleton或skeletonData为null");
                return;
            }

            // 停止所有当前动画，确保死亡动画能够播放
            skeletonAnimation.AnimationState.ClearTrack(0);

            // 播放动画，使用较高的混合时间确保平滑过渡
            if (skeletonAnimation != null && !string.IsNullOrEmpty(deathAnimName)) {
                // 直接使用AnimationState API，确保动画能够播放
                Spine.TrackEntry trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, deathAnimName, false);

                // 设置混合时间
                trackEntry.MixDuration = 0.1f;

                // 记录当前动画
                currentAnimation = deathAnimName;

            }
            else {
                Debug.LogError($"无法播放死亡动画: skeletonAnimation={skeletonAnimation}, deathAnimName={deathAnimName}");
            }
        }
        catch (System.Exception e) {
            Debug.LogError($"PlayDeathAnimation出错: {e.Message}\n{e.StackTrace}");
        }
    }

    // 播放施法动画
    public void PlayCastAnimation() {
        PlayAnimation(animationMapping.castAnimation, false);
        StartCoroutine(ReturnToIdle(skeletonAnimation.AnimationState.GetCurrent(0).Animation.Duration));
    }

    // 播放移动动画
    public void PlayWalkAnimation() {
        PlayAnimation(animationMapping.walkAnimation, true);
    }

    // 停止行走动画并播放过渡动画
    public void StopWalkWithTransition() {
        // 检查角色是否已死亡
        if (characterStats != null && characterStats.currentHitPoints <= 0) {
            // 如果角色已死亡，播放死亡动画
            Debug.Log($"{gameObject.name} 已死亡，不播放过渡动画");
            PlayAnimation(animationMapping.deathAnimation, false);
        }
        else {
            // 检查过渡动画是否存在
            if (skeletonAnimation != null && skeletonAnimation.Skeleton != null && skeletonAnimation.Skeleton.Data != null) {
                Spine.Animation transitionAnim = skeletonAnimation.Skeleton.Data.FindAnimation(animationMapping.moveToIdleAnimation);
                if (transitionAnim != null) {
                    // 播放过渡动画
                    PlayAnimation(animationMapping.moveToIdleAnimation, false);
                    Debug.Log($"{gameObject.name} 播放移动到待机的过渡动画: {animationMapping.moveToIdleAnimation}");

                    // 使用协程在过渡动画结束后播放待机动画
                    StartCoroutine(ReturnToIdle(transitionAnim.Duration));
                }
                else {
                    // 如果过渡动画不存在，直接播放待机动画
                    Debug.LogWarning($"{gameObject.name} 过渡动画 {animationMapping.moveToIdleAnimation} 不存在，直接切换到待机动画");
                    PlayAnimation(animationMapping.idleAnimation, true);
                }
            }
            else {
                // 如果skeletonAnimation组件有问题，直接播放待机动画
                Debug.LogWarning($"{gameObject.name} skeletonAnimation组件异常，直接切换到待机动画");
                PlayAnimation(animationMapping.idleAnimation, true);
            }
        }
    }

    // 动画播放完毕后返回空闲状态
    private IEnumerator ReturnToIdle(float delay) {
        yield return new WaitForSeconds(delay);

        // 只有在角色没有死亡的情况下才返回空闲状态
        if (characterStats != null && characterStats.currentHitPoints > 0) {
            PlayAnimation(animationMapping.idleAnimation, true);
        }
        else {
            Debug.Log($"{gameObject.name} 已死亡，不返回空闲状态");
        }
    }

    // 移动到目标位置
    public IEnumerator MoveToPosition(Vector3 targetPosition, float speed) {
        // 播放移动动画
        PlayWalkAnimation();

        // 计算方向
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        // 设置朝向
        if (direction.x != 0) {
            transform.localScale = new Vector3(direction.x > 0 ? 1 : -1, 1, 1);
        }

        // 移动到目标位置
        float distance = direction.magnitude;
        float remainingDistance = distance;

        while (remainingDistance > 0.1f) {
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
            remainingDistance = Vector3.Distance(transform.position, targetPosition);
            yield return null;
        }

        // 到达目标位置，如果角色没有死亡，播放过渡动画
        if (characterStats != null && characterStats.currentHitPoints > 0) {
            StopWalkWithTransition();
        }
        else {
            Debug.Log($"{gameObject.name} 已死亡，不返回空闲状态");
        }
    }

    // 受到伤害
    public void TakeDamage() {
        // 播放受击动画
        PlayHitAnimation();

        // 确保UI更新
        // UI更新已转移至挂机系统自动处理

        // 如果生命值为0，播放死亡动画
        if (characterStats != null && characterStats.currentHitPoints <= 0) {
            PlayDeathAnimation();
        }
    }

    // 清理
    private void OnDestroy() {
        // 取消注册事件
        // 事件注销已转移至挂机系统自动处理
    }
}
