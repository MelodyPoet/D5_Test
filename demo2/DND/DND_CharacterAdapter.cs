using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Spine;
using DG.Tweening;

namespace demo2.DND
{
    /// <summary>
    /// DND角色动画适配器 - 统一管理角色动画播放和状态切换
    /// 使用DOTween处理位移 + SpineEvent处理状态切换
    /// </summary>
    public class DND_CharacterAdapter : MonoBehaviour
    {
        [Header("组件引用")]
        public CharacterStats characterStats;
        public SkeletonAnimation skeletonAnimation;

        [Header("动画表现配置（ScriptableObject驱动）")]
        public CharacterAnimationConfig animationConfig;

        [Header("位移设置")]
        // public float moveSpeed = 2.0f; // 已移除，统一由SO配置
        // public float attackDistance = 1.5f; // 已移除，统一由SO配置

        private Vector3 originalPosition;
        private bool isAnimating;
        private Tween currentMoveTween;

        // Spine事件委托
        public System.Action OnAttackHit;
        public System.Action OnAnimationComplete;
        public System.Action<string> OnStateChanged;

        private bool isForceAttackAnimation = false;

        public enum CharacterState
        {
            Idle,
            Walk,
            Run,
            Attack,
            Hit,
            Death,
            Skill,
            Defend,
            Dodge,
            Unconscious
            // ...可扩展
        }

        public CharacterState CurrentState { get; private set; } = CharacterState.Idle;

        void Awake()
        {
            InitializeComponents();
            SetupSpineEvents();
            originalPosition = transform.position;
            // 不再需要赋值moveSpeed和attackDistance，全部通过animationConfig访问
        }

        void OnEnable()
        {
            if (skeletonAnimation != null)
            {
                skeletonAnimation.AnimationState.Event += OnSpineEvent;
                skeletonAnimation.AnimationState.Complete += OnSpineAnimationComplete;
            }
        }

        void OnDisable()
        {
            if (skeletonAnimation != null)
            {
                skeletonAnimation.AnimationState.Event -= OnSpineEvent;
                skeletonAnimation.AnimationState.Complete -= OnSpineAnimationComplete;
            }
            if (currentMoveTween != null && currentMoveTween.IsActive())
            {
                currentMoveTween.Kill();
                currentMoveTween = null;
            }
        }

        void InitializeComponents()
        {
            if (characterStats == null)
                characterStats = GetComponent<CharacterStats>();

            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();

            if (characterStats == null || skeletonAnimation == null)
            {
                Debug.LogError($"DND_CharacterAdapter: 缺少必需组件！角色: {gameObject.name}");
            }
        }

        void SetupSpineEvents()
        {
            if (skeletonAnimation == null) return;

            skeletonAnimation.AnimationState.Event += OnSpineEvent;
            skeletonAnimation.AnimationState.Complete += OnSpineAnimationComplete;
        }

        void OnSpineEvent(TrackEntry trackEntry, Spine.Event e)
        {
            string eventName = e.Data.Name;
            Debug.Log($"[{gameObject.name}] 接收到Spine事件: {eventName}");

            // 更智能的攻击事件识别 - 支持多种命名规范
            bool isAttackEvent = false;
            string lowerEventName = eventName.ToLower();
            if (lowerEventName.Contains("atk") ||
                lowerEventName.Contains("attack") ||
                lowerEventName.Contains("damage") ||
                lowerEventName.Contains("hit") ||
                lowerEventName.Contains("strike"))
            {
                isAttackEvent = true;
            }
            if (!isAttackEvent)
            {
                var attackPatterns = new string[]
                {
                    "atk\\d*_e",
                    "attack\\d*_e",
                    "hit\\d*_e",
                    "strike\\d*_e"
                };
                foreach (var pattern in attackPatterns)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(lowerEventName, pattern))
                    {
                        isAttackEvent = true;
                        Debug.Log($"[{gameObject.name}] 通过模式匹配识别攻击事件: {eventName} (模式: {pattern})");
                        break;
                    }
                }
            }
            if (isAttackEvent)
            {
                Debug.Log($"[{gameObject.name}] 识别为攻击命中事件: {eventName}");
                OnAttackHit?.Invoke();
                return;
            }

            // 使用SO配置的事件名进行精确匹配
            if (animationConfig != null)
            {
                if (eventName == animationConfig.attackHitEvent)
                {
                    Debug.Log($"[{gameObject.name}] 攻击命中事件触发（映射匹配）: {eventName}");
                    OnAttackHit?.Invoke();
                }
                else if (eventName == animationConfig.deathCompleteEvent)
                {
                    Debug.Log($"[{gameObject.name}] 死亡完成事件触发: {eventName}");
                    OnAnimationComplete?.Invoke();
                }
                else if (eventName == animationConfig.unconsciousStartEvent)
                {
                    Debug.Log($"[{gameObject.name}] 昏迷开始事件触发: {eventName}");
                    OnStateChanged?.Invoke("unconscious");
                }
                else if (eventName == animationConfig.stateChangeEvent)
                {
                    OnStateChanged?.Invoke(e.String ?? "");
                }
                else if (eventName == animationConfig.footstepEvent)
                {
                    OnStateChanged?.Invoke("footstep");
                }
                else
                {
                    Debug.Log($"[{gameObject.name}] 未映射的Spine事件: {eventName}");
                }
            }
            else
            {
                Debug.Log($"[{gameObject.name}] animationConfig为空，事件: {eventName} 未被处理");
            }
        }

        void OnSpineAnimationComplete(TrackEntry trackEntry)
        {
            string completedAnimationName = trackEntry.Animation.Name;
            Debug.Log($"[{gameObject.name}] Spine动画完成: {completedAnimationName}");

            OnAnimationComplete?.Invoke();

            string expectedAttackAnimName = animationConfig != null ? animationConfig.attackAnimation : "attack";

            if (completedAnimationName == expectedAttackAnimName ||
                completedAnimationName == (animationConfig != null ? animationConfig.hitAnimation : "hit") ||
                completedAnimationName == (animationConfig != null ? animationConfig.dodgeAnimation : "dodge"))
            {
                if (isForceAttackAnimation)
                {
                    Debug.Log($"[{gameObject.name}] 攻击动画完成，解除动画锁");
                    isForceAttackAnimation = false;
                }
                if (isAnimating)
                {
                    Debug.Log($"[{gameObject.name}] 战斗动画 '{completedAnimationName}' 完成，但角色仍在战斗状态中，由战斗系统控制后续动画");
                }
                else
                {
                    Debug.Log($"[{gameObject.name}] 自动切换到idle状态 (来源: {completedAnimationName})");
                    PlayIdleAnimation();
                }
            }
            else
            {
                Debug.Log($"[{gameObject.name}] 其他动画 '{completedAnimationName}' 完成，无需特殊处理");
            }
        }

        #region 基础与战斗动画播放

        // 基础动画：在攻击锁定期间会被阻止切换
        public void PlayIdleAnimation()
        {
            if (isForceAttackAnimation)
            {
                Debug.Log($"[{gameObject.name}] [锁] 攻击动画期间禁止切换到Idle");
                return;
            }
            PlayAnimation(animationConfig.idleAnimation, true);
        }

        public void PlayWalkAnimation()
        {
            if (isForceAttackAnimation)
            {
                Debug.Log($"[{gameObject.name}] [锁] 攻击动画期间禁止切换到Walk");
                return;
            }
            PlayAnimation(animationConfig.walkAnimation, true);
        }

        public void PlayRunAnimation()
        {
            if (isForceAttackAnimation)
            {
                Debug.Log($"[{gameObject.name}] [锁] 攻击动画期间禁止切换到Run");
                return;
            }
            PlayAnimation(animationConfig.runAnimation, true);
        }

        // 战斗动画
        public void PlayAttackAnimation()
        {
            Debug.Log($"[{gameObject.name}] ========== PlayAttackAnimation 开始 ==========");
            Debug.Log($"[{gameObject.name}] 当前isAnimating状态: {isAnimating}");
            Debug.Log($"[{gameObject.name}] skeletonAnimation是否为空: {skeletonAnimation == null}");
            Debug.Log($"[{gameObject.name}] animationConfig是否为空: {animationConfig == null}");

            if (animationConfig != null)
            {
                Debug.Log($"[{gameObject.name}] 配置的攻击动画名称: '{animationConfig.attackAnimation}'");
            }

            string attackAnimName = FindBestAttackAnimationName();
            if (!string.IsNullOrEmpty(attackAnimName))
            {
                Debug.Log($"[{gameObject.name}] ✓ 找到攻击动画: {attackAnimName}，开始播放");
                Debug.Log($"[{gameObject.name}] ===== 即将调用PlayAnimation =====");
                Debug.Log($"[{gameObject.name}] 参数: animationName='{attackAnimName}', loop=false");
                isForceAttackAnimation = true;
                PlayAnimation(attackAnimName, false);
                Debug.Log($"[{gameObject.name}] ===== PlayAnimation调用完成 =====");
                Debug.Log($"[{gameObject.name}] ✓ PlayAnimation调用完成");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] ✗ 未找到可用的攻击动画！");
                if (skeletonAnimation != null && skeletonAnimation.Skeleton?.Data != null)
                {
                    Debug.LogError($"[{gameObject.name}] 当前Spine角色的所有动画:");
                    var skeletonData = skeletonAnimation.Skeleton.Data;
                    for (int i = 0; i < skeletonData.Animations.Count; i++)
                    {
                        Debug.LogError($"  {i}: {skeletonData.Animations.Items[i].Name}");
                    }
                }
            }
            Debug.Log($"[{gameObject.name}] ========== PlayAttackAnimation 结束 ==========");
        }

        public void PlayHitAnimation()
        {
            PlayAnimation(animationConfig.hitAnimation, false);
        }

        public void PlayDeathAnimation()
        {
            PlayAnimation(animationConfig.deathAnimation, false);
        }

        public void PlayUnconsciousAnimation()
        {
            PlayAnimation(animationConfig.unconsciousAnimation, true);
        }

        public void ForcePlayAttackAnimation()
        {
            // 强制播放，仍设置锁以防止覆盖
            isForceAttackAnimation = true;
            PlayAnimation(animationConfig.attackAnimation, false);
        }

        #endregion

        #region 技能动画播放

        public void PlayCastSpellAnimation()
        {
            if (isAnimating) return;

            PlayAnimation(animationConfig.skillAnimation, false);
        }

        public void PlayDefendAnimation()
        {
            PlayAnimation(animationConfig.defendAnimation, false);
        }

        public void PlayDodgeAnimation()
        {
            PlayAnimation(animationConfig.dodgeAnimation, false);
        }

        #endregion

        #region 位移动画 - 使用DOTween

        /// <summary>
        /// 执行近战攻击 - 移动到目标面前攻击后返回
        /// 修复版本：解决动画播放时序问题
        /// </summary>
        public void ExecuteMeleeAttack(Transform target, System.Action onAttackHit = null, System.Action onComplete = null)
        {
            if (target == null)
            {
                Debug.LogError($"[{gameObject.name}] ExecuteMeleeAttack: target为空！");
                return;
            }

            if (isAnimating)
            {
                Debug.LogWarning($"[{gameObject.name}] 角色正在执行动画，跳过近战攻击");
                return;
            }

            isAnimating = true;
            Debug.Log($"[{gameObject.name}] ========== 开始执行近战攻击（修复版本） ==========");

            // 计算攻击位置
            Vector3 attackPosition = CalculateAttackPosition(target);
            Debug.Log($"{gameObject.name} 近战攻击 - 当前位置: {transform.position} → 攻击位置: {attackPosition}");

            // 阶段1：移动到攻击位置
            Debug.Log($"[{gameObject.name}] 阶段1：移动到攻击位置");
            PlayWalkAnimation();

            float moveDistance = Vector3.Distance(transform.position, attackPosition);
            float moveDuration = moveDistance / animationConfig.moveSpeed;

            // 新Tween前Kill旧Tween
            if (currentMoveTween != null && currentMoveTween.IsActive())
            {
                currentMoveTween.Kill();
                currentMoveTween = null;
            }

            currentMoveTween = transform.DOMove(attackPosition, moveDuration)
                .SetEase(animationConfig.moveEase)
                .OnComplete(() => {
                    try
                    {
                        Debug.Log($"[{gameObject.name}] 阶段2：到达攻击位置，执行攻击");
                        ExecuteAttackAtPosition(target, onAttackHit, () => {
                            Debug.Log($"[{gameObject.name}] 阶段3：攻击完成，返回原位");
                            // 返回原位
                            if (currentMoveTween != null && currentMoveTween.IsActive())
                            {
                                currentMoveTween.Kill();
                                currentMoveTween = null;
                            }
                            currentMoveTween = transform.DOMove(originalPosition, moveDuration)
                                .SetEase(animationConfig.moveEase)
                                .OnComplete(() => {
                                    isAnimating = false;
                                    PlayIdleAnimation();
                                    onComplete?.Invoke();
                                });
                        });
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[{gameObject.name}] ExecuteMeleeAttack异常: {ex.Message}");
                        isAnimating = false;
                        PlayIdleAnimation();
                        onComplete?.Invoke();
                    }
                });
        }

        /// <summary>
        /// 在当前位置执行攻击 - 统一的攻击执行逻辑
        /// </summary>
        private void ExecuteAttackAtPosition(Transform target, System.Action onAttackHit = null, System.Action onComplete = null)
        {
            Debug.Log($"[{gameObject.name}] ========== ExecuteAttackAtPosition 开始 ==========");

            // 创建临时回调，避免事件重复注册
            System.Action tempAttackHitCallback = null;
            System.Action tempAnimCompleteCallback = null;

            // 备用计时器引用，用于在Spine事件触发时取消
            Tween attackHitBackupTimer = null;
            Tween attackCompleteBackupTimer = null;

            // 标记是否已经触发过，防止重复执行
            bool hasAttackHitTriggered = false;
            bool hasAttackCompleteTriggered = false;

            // 设置攻击命中回调
            if (onAttackHit != null)
            {
                tempAttackHitCallback = () => {
                    try
                    {
                        if (hasAttackHitTriggered)
                        {
                            Debug.Log($"{gameObject.name} 攻击命中已执行，跳过重复触发");
                            return;
                        }
                        hasAttackHitTriggered = true;

                        Debug.Log($"{gameObject.name} 攻击命中事件触发（Spine事件）");
                        attackHitBackupTimer?.Kill();
                        onAttackHit.Invoke();
                        OnAttackHit -= tempAttackHitCallback;
                        tempAttackHitCallback = null;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[{gameObject.name}] 攻击命中回调错误: {ex.Message}");
                    }
                };
                OnAttackHit += tempAttackHitCallback;
            }

            // 播放攻击动画 - 使用和远程攻击相同的逻辑
            Debug.Log($"[{gameObject.name}] 开始播放攻击动画");

            // 清理第0轨道以避免残留的walk/idle覆盖
            try
            {
                skeletonAnimation.AnimationState.ClearTrack(0);
            }
            catch { }

            PlayAttackAnimation();

            // 获取实际播放的攻击动画名称和时长
            string actualAttackAnimName = FindBestAttackAnimationName();
            float attackAnimationDuration = GetAnimationDuration(actualAttackAnimName);
            if (attackAnimationDuration <= 0) attackAnimationDuration = 1.0f;

            Debug.Log($"[{gameObject.name}] 攻击动画时长: {attackAnimationDuration}秒 (动画: {actualAttackAnimName})");

            // 为了保证攻击动画能在画面上显示，先等一小段时间（让Spine刷新并渲染第一帧），再设置备用计时器和完成回调
            float visualDelay = 0.12f; // 增加延迟，确保动画帧已渲染
            DOVirtual.DelayedCall(visualDelay, () => {
                Debug.Log($"[{gameObject.name}] 延迟 {visualDelay}s 后开始注册攻击备份计时器和完成回调");

                TrackEntry attackEntry = null;
                try
                {
                    attackEntry = skeletonAnimation.AnimationState.GetCurrent(0);
                }
                catch { }

                // 如果当前轨道不是目标动画，再强制设置一次并立即应用
                try
                {
                    if (attackEntry == null || attackEntry.Animation == null || attackEntry.Animation.Name != actualAttackAnimName)
                    {
                        Debug.Log($"[{gameObject.name}] 再次SetAnimation以确保动画在播放: {actualAttackAnimName}");
                        attackEntry = skeletonAnimation.AnimationState.SetAnimation(0, actualAttackAnimName, false);
                        try
                        {
                            skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
                            skeletonAnimation.Skeleton.UpdateWorldTransform();
                            try { skeletonAnimation.Update(0f); } catch { }
                        }
                        catch { }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[{gameObject.name}] 确保攻击动画播放时出错: {ex.Message}");
                }

                // 设置攻击命中的备用触发器
                attackHitBackupTimer = DOVirtual.DelayedCall(attackAnimationDuration * 0.5f, () => {
                    try
                    {
                        if (tempAttackHitCallback != null && !hasAttackHitTriggered)
                        {
                            Debug.Log($"{gameObject.name} 备用攻击命中触发（Spine事件未响应）");
                            tempAttackHitCallback.Invoke();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[{gameObject.name}] 备用攻击命中触发错误: {ex.Message}");
                    }
                });

                // 设置攻击完成回调（备用触发也已在下面设置）
                tempAnimCompleteCallback = () => {
                    try
                    {
                        if (hasAttackCompleteTriggered)
                        {
                            Debug.Log($"{gameObject.name} 攻击完成已执行，跳过重复触发");
                            return;
                        }
                        hasAttackCompleteTriggered = true;

                        Debug.Log($"{gameObject.name} 攻击完成事件触发（Spine事件）");
                        attackCompleteBackupTimer?.Kill();

                        // 解除攻击锁，确保后续可以切换到Walk/Idle
                        if (isForceAttackAnimation)
                        {
                            Debug.Log($"[{gameObject.name}] 备份/完成回调：解除攻击动画锁");
                            isForceAttackAnimation = false;
                        }

                        OnAnimationComplete -= tempAnimCompleteCallback;
                        tempAnimCompleteCallback = null;
                        onComplete?.Invoke();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[{gameObject.name}] 攻击完成回调错误: {ex.Message}");
                    }
                };
                OnAnimationComplete += tempAnimCompleteCallback;

                // 添加备用完成触发器
                attackCompleteBackupTimer = DOVirtual.DelayedCall(attackAnimationDuration + 0.1f, () => {
                    try
                    {
                        if (tempAnimCompleteCallback != null && !hasAttackCompleteTriggered)
                        {
                            Debug.Log($"{gameObject.name} 备用攻击完成触发（Spine事件未响应）");
                            tempAnimCompleteCallback.Invoke();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[{gameObject.name}] 备用攻击完成触发错误: {ex.Message}");
                    }
                });
            });

            Debug.Log($"[{gameObject.name}] ========== ExecuteAttackAtPosition 设置完成 ==========");
        }

        /// <summary>
        /// 执行远程攻击 - 原地攻击
        /// 修复版本：使用统一的攻击执行逻辑
        /// </summary>
        public void ExecuteRangedAttack(Transform target, System.Action onAttackHit = null, System.Action onComplete = null)
        {
            Debug.Log($"[RANGED] ========== ExecuteRangedAttack 被调用 ==========");
            Debug.Log($"[RANGED] 角色: {gameObject.name}, 目标: {target?.name}");

            if (target == null)
            {
                Debug.LogError($"[RANGED] {gameObject.name} ExecuteRangedAttack: target为空！");
                return;
            }

            if (isAnimating)
            {
                Debug.LogWarning($"[RANGED] {gameObject.name} 角色正在执行动画，跳过远程攻击");
                return;
            }

            isAnimating = true;
            Debug.Log($"[RANGED] {gameObject.name} 开始执行远程攻击（修复版本）");

            // 直接在当前位置执行攻击，使用统一的攻击执行逻辑
            ExecuteAttackAtPosition(target, onAttackHit, () => {
                // 远程攻击完成，直接结束（不需要返回原位）
                Debug.Log($"[{gameObject.name}] 远程攻击完成");
                isAnimating = false;
                onComplete?.Invoke();
            });
        }

        private Vector3 CalculateAttackPosition(Transform target)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Vector3 attackPos = target.position - direction * animationConfig.attackDistance;

            Debug.Log($"计算攻击位置 - 目标: {target.position}, 攻击位置: {attackPos}, 距离: {animationConfig.attackDistance}");
            return attackPos;
        }

        private void ReturnToOriginalPosition(System.Action onComplete = null)
        {
            Debug.Log($"{gameObject.name} 返回原位 - 当前位置: {transform.position} → 原位: {originalPosition}");

            if (isForceAttackAnimation)
            {
                Debug.Log($"[{gameObject.name}] ReturnToOriginalPosition: 解除攻击动画锁以便播放返回动画");
                isForceAttackAnimation = false;
            }
            PlayWalkAnimation();
            float distance = Vector3.Distance(transform.position, originalPosition);
            if (distance < 0.1f)
            {
                PlayIdleAnimation();
                isAnimating = false;
                onComplete?.Invoke();
                return;
            }
            currentMoveTween?.Kill();
            float moveDelay = 0.06f;
            DOVirtual.DelayedCall(moveDelay, () => {
                currentMoveTween = transform.DOMove(originalPosition, distance / animationConfig.moveSpeed)
                    .SetEase(animationConfig.moveEase)
                    .OnComplete(() => {
                        PlayIdleAnimation();
                        isAnimating = false;
                        onComplete?.Invoke();
                    });
            });
        }

        /// <summary>
        /// 更新原始位置 - 用于敌人进场后重新设置
        /// </summary>
        public void UpdateOriginalPosition()
        {
            originalPosition = transform.position;
            Debug.Log($"{gameObject.name} 原始位置已更新为: {originalPosition}");
        }

        #endregion

        #region 动画核心方法

        private void PlayAnimation(string animationName, bool loop)
        {
            Debug.Log($"[{gameObject.name}] ========== PlayAnimation 调用 ==========");
            Debug.Log($"[{gameObject.name}] 动画名称: '{animationName}', 循环: {loop}");
            Debug.Log($"[{gameObject.name}] >>> 开始组件状态检查 <<<");

            if (skeletonAnimation == null)
            {
                Debug.LogError($"[{gameObject.name}] [RETURN1] PlayAnimation失败: skeletonAnimation为空");
                return;
            }

            if (string.IsNullOrEmpty(animationName))
            {
                Debug.LogError($"[{gameObject.name}] [RETURN2] PlayAnimation失败: animationName为空或null");
                return;
            }

            Debug.Log($"[{gameObject.name}] >>> 步骤1: 基础检查完成 <<<");

            if (skeletonAnimation.AnimationState == null)
            {
                Debug.LogError($"[{gameObject.name}] [RETURN3] AnimationState为空，无法播放动画");
                return;
            }
            Debug.Log($"[{gameObject.name}] >>> 步骤2: AnimationState检查通过 <<<");

            if (skeletonAnimation.Skeleton == null)
            {
                Debug.LogError($"[{gameObject.name}] [RETURN4] Skeleton为空，无法播放动画");
                return;
            }
            Debug.Log($"[{gameObject.name}] >>> 步骤3: Skeleton检查通过 <<<");

            if (skeletonAnimation.Skeleton.Data == null)
            {
                Debug.LogError($"[{gameObject.name}] [RETURN5] SkeletonData为空，无法播放动画");
                return;
            }
            Debug.Log($"[{gameObject.name}] >>> 步骤4: SkeletonData检查通过 <<<");

            var targetAnim = skeletonAnimation.Skeleton.Data.FindAnimation(animationName);
            if (targetAnim == null)
            {
                Debug.LogError($"[{gameObject.name}] [RETURN6] ✗ 动画不存在: '{animationName}'");
                return;
            }
            Debug.Log($"[{gameObject.name}] >>> 步骤5: 动画存在验证通过: '{animationName}' <<<");

            Debug.Log($"[{gameObject.name}] >>> 步骤6: 即将调用SetAnimation <<<");
            try
            {
                var trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
                if (trackEntry == null)
                {
                    Debug.LogError($"[{gameObject.name}] ✗ SetAnimation返回null - 动画设置失败: '{animationName}'");
                }
                else
                {
                    Debug.Log($"[{gameObject.name}] ✓ 成功设置动画: '{animationName}', 时长: {trackEntry.Animation.Duration}秒, 循环: {loop}");
                    var currentTrack = skeletonAnimation.AnimationState.GetCurrent(0);
                    if (currentTrack != null)
                    {
                        Debug.Log($"[{gameObject.name}] ✓ 验证成功，当前播放: {currentTrack.Animation.Name}");
                    }
                    else
                    {
                        Debug.LogError($"[{gameObject.name}] ✗ 验证失败，当前没有播放动画");

                    // 强制立即将AnimationState应用到Skeleton并更新世界变换，确保画面及时刷新
                    try
                    {
                        skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
                        skeletonAnimation.Skeleton.UpdateWorldTransform();
                        // 如果SkeletonAnimation提供Update接口，调用以便立即刷新（兼容不同版本）
                        try { skeletonAnimation.Update(0f); } catch { }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[{gameObject.name}] 强制刷新Spine显示失败: {ex.Message}");
                    }

                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{gameObject.name}] SetAnimation异常: {ex.Message}");
            }

            Debug.Log($"[{gameObject.name}] ========== PlayAnimation 结束 ==========");
        }

        /// <summary>
        /// 获取动画时长 - 用于备用计时器
        /// </summary>
        private float GetAnimationDuration(string animationName)
        {
            if (skeletonAnimation == null || string.IsNullOrEmpty(animationName)) return 0f;

            var skeletonData = skeletonAnimation.Skeleton.Data;
            var animation = skeletonData.FindAnimation(animationName);

            if (animation != null)
            {
                return animation.Duration;
            }

            Debug.LogWarning($"无法获取动画时长: {animationName} - 角色: {gameObject.name}");
            return 0f;
        }

        #endregion

        #region 状态查询

        public bool IsAnimating => isAnimating;

        public string CurrentAnimationName
        {
            get
            {
                if (skeletonAnimation?.AnimationState?.GetCurrent(0) != null)
                {
                    return skeletonAnimation.AnimationState.GetCurrent(0).Animation.Name;
                }
                return "";
            }
        }

        #endregion

        #region 兼容性方法 - 为HorizontalBattleFormationManager提供支持

        /// <summary>
        /// 停止行走动画并切换到待机 - 兼容旧接口
        /// </summary>
        public void StopWalkWithTransition()
        {
            PlayIdleAnimation();
        }

        /// <summary>
        /// 当前动画名称 - 兼容旧接口
        /// </summary>
        public string CurrentAnimation => CurrentAnimationName;

        #endregion

        #region 清理

        void OnDestroy()
        {
            // 停止所有DOTween动画
            currentMoveTween?.Kill();

            // 清理Spine事件
            if (skeletonAnimation != null)
            {
                skeletonAnimation.AnimationState.Event -= OnSpineEvent;
                skeletonAnimation.AnimationState.Complete -= OnSpineAnimationComplete;
            }
        }

        #endregion

        #region 调试工具

        /// <summary>
        /// 调试方法：列出所有可用的动画
        /// </summary>
        [ContextMenu("列出所有可用动画")]
        public void ListAllAvailableAnimations()
        {
            if (skeletonAnimation == null)
            {
                Debug.LogError($"[{gameObject.name}] SkeletonAnimation组件为空！");
                return;
            }

            var skeletonData = skeletonAnimation.Skeleton.Data;
            if (skeletonData == null)
            {
                Debug.LogError($"[{gameObject.name}] SkeletonData为空！");
                return;
            }

            Debug.Log($"=== [{gameObject.name}] 所有可用动画列表 ===");
            for (int i = 0; i < skeletonData.Animations.Count; i++)
            {
                var animation = skeletonData.Animations.Items[i];
                Debug.Log($"{i}: {animation.Name} (时长: {animation.Duration}秒)");
            }

            Debug.Log($"=== [{gameObject.name}] 当前动画映射配置 ===");
            Debug.Log($"idle: {animationConfig.idleAnimation}");
            Debug.Log($"walk: {animationConfig.walkAnimation}");
            Debug.Log($"attack: {animationConfig.attackAnimation}");
            Debug.Log($"hit: {animationConfig.hitAnimation}");

            // 验证攻击动画是否存在
            var attackAnim = skeletonData.FindAnimation(animationConfig.attackAnimation);
            if (attackAnim != null)
            {
                Debug.Log($"✓ 攻击动画 '{animationConfig.attackAnimation}' 找到，时长: {attackAnim.Duration}秒");
            }
            else
            {
                Debug.LogError($"✗ 攻击动画 '{animationConfig.attackAnimation}' 未找到！");
            }
        }

        /// <summary>
        /// 调试方法：强制播放攻击动画
        /// </summary>
        [ContextMenu("强制播放攻击动画")]
        public void DebugForcePlayAttack()
        {
            Debug.Log($"[{gameObject.name}] 强制播放攻击动画测试");
            PlayAttackAnimation();
        }

        #endregion

        #region 智能查找攻击动画名称

        /// <summary>
        /// 智能查找最佳攻击动画名称
        /// </summary>
        private string FindBestAttackAnimationName()
        {
            if (skeletonAnimation == null)
            {
                Debug.LogError($"[{gameObject.name}] FindBestAttackAnimationName: skeletonAnimation为空");
                return null;
            }

            var skeletonData = skeletonAnimation.Skeleton.Data;
            if (skeletonData == null)
            {
                Debug.LogError($"[{gameObject.name}] FindBestAttackAnimationName: skeletonData为空");
                return null;
            }

            Debug.Log($"[{gameObject.name}] 智能查找攻击动画 - 配置的攻击动画名称: '{animationConfig.attackAnimation}'");

            // 常见的攻击动画名称列表（按优先级排序）
            string[] possibleAttackNames = {
                animationConfig.attackAnimation, // 优先使用配置的名称
                "Atk01", "Atk02", "Atk03", "attack", "Attack", "ATTACK",
                "atk", "ATK", "hit", "Hit", "strike", "Strike"
            };

            foreach (string animName in possibleAttackNames)
            {
                if (string.IsNullOrEmpty(animName))
                {
                    Debug.Log($"[{gameObject.name}] 跳过空的动画名称");
                    continue;
                }

                Debug.Log($"[{gameObject.name}] 尝试查找动画: '{animName}'");
                var animation = skeletonData.FindAnimation(animName);
                if (animation != null)
                {
                    Debug.Log($"[{gameObject.name}] ✓ 找到攻击动画: '{animName}', 时长: {animation.Duration}秒");
                    return animName;
                }
                else
                {
                    Debug.Log($"[{gameObject.name}] ✗ 未找到动画: '{animName}'");
                }
            }

            Debug.LogError($"[{gameObject.name}] 未找到任何可用的攻击动画！");
            return null;
        }

        #endregion
    }
}
