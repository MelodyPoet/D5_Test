using UnityEngine;
using Spine.Unity;
using DG.Tweening;
using demo2.DND.HorizontalFormation;

namespace demo2.DND
{
    public class DND_CharacterAdapter : MonoBehaviour
    {
        // 角色统计数据
        public CharacterStats characterStats;

        // Spine动画组件
        public SkeletonAnimation skeletonAnimation;

        // 动画名称映射
        [System.Serializable]
        public class AnimationMapping
        {
            public string idleAnimation = "idle";
            public string walkAnimation = "walk";
            public string moveToIdleAnimation = "m_to_i"; // 移动到待机的过渡动画
            public string attackAnimation = "attack";
            public string hitAnimation = "hit";
            public string deathAnimation = "death";
            public string castAnimation = "cast";
            [Header("昏迷动画（可选，仅玩家和队友使用）")]
            public string unconsciousAnimation = "unconscious"; // 昏迷动画，怪物预制体不填此项
        }

        public AnimationMapping animationMapping = new AnimationMapping();

        // 当前动画状态
        private string currentAnimation;

        // 公开当前动画状态的只读属性
        public string CurrentAnimation => currentAnimation;

        // 事件驱动动画控制
        private Vector3 originalPosition;
        private bool isMovingForAttack;

        // 初始化
        private void Start()
        {
            // 获取角色统计数据
            if (characterStats == null)
            {
                characterStats = GetComponent<CharacterStats>();
                if (characterStats == null)
                {
                    characterStats = gameObject.AddComponent<CharacterStats>();
                }
            }

            // 获取Spine动画组件
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }

            // 订阅Spine动画事件
            if (skeletonAnimation != null)
            {
                skeletonAnimation.AnimationState.Event += OnSpineAnimationEvent;
                skeletonAnimation.AnimationState.Complete += OnAnimationComplete;
            }

            // 记录原始位置
            originalPosition = transform.position;

            // 简化初始化：玩家角色开始就播放走路动画（探索模式）
            if (characterStats != null && characterStats.battleSide == BattleSide.Player)
            {
                PlayWalkAnimation();
            }
        }

        private void OnDestroy()
        {
            // 取消订阅事件
            if (skeletonAnimation != null)
            {
                skeletonAnimation.AnimationState.Event -= OnSpineAnimationEvent;
                skeletonAnimation.AnimationState.Complete -= OnAnimationComplete;
            }

            // 停止所有DOTween动画
            transform.DOKill();
        }

        // Spine动画事件处理
        private void OnSpineAnimationEvent(Spine.TrackEntry trackEntry, Spine.Event e)
        {
            if (e.Data.Name == "deal_damage")
            {
                // 触发伤害计算事件
                OnDealDamage();
            }
        }

        // 动画完成事件处理
        private void OnAnimationComplete(Spine.TrackEntry trackEntry)
        {
            string animName = trackEntry.Animation.Name;

            // 攻击动画完成后的处理
            if (animName == animationMapping.attackAnimation)
            {
                OnAttackAnimationComplete();
            }
            // 受击动画完成后返回待机
            else if (animName == animationMapping.hitAnimation)
            {
                if (characterStats != null && characterStats.currentHitPoints > 0)
                {
                    PlayIdleAnimation();
                }
            }
            // 过渡动画完成后播放待机
            else if (animName == animationMapping.moveToIdleAnimation)
            {
                PlayIdleAnimation();
            }
        }

        // 伤害触发事件（在攻击动画的关键帧触发）
        private void OnDealDamage()
        {
            Debug.Log($"{gameObject.name} 在动画关键帧触发伤害计算");
        }

        // 攻击动画完成事件
        private void OnAttackAnimationComplete()
        {
            if (isMovingForAttack)
            {
                ReturnToOriginalPosition();
            }
            else
            {
                PlayIdleAnimation();
            }
        }

        /// <summary>
        /// 初始化角色动画状态
        /// </summary>
        public void InitializeAnimation()
        {
            if (characterStats != null && characterStats.battleSide == BattleSide.Player)
            {
                PlayAnimation(animationMapping.idleAnimation, true);
            }
            else
            {
                PlayAnimation(animationMapping.idleAnimation, true);
            }
        }

        // 播放动画
        public void PlayAnimation(string animationName, bool loop)
        {
            if (skeletonAnimation != null && !string.IsNullOrEmpty(animationName))
            {
                skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
                currentAnimation = animationName;
            }
        }

        // 播放攻击动画（根据职业类型选择攻击方式）
        public void PlayAttackAnimation()
        {
            // 获取阵型管理器来判断职业类型
            // 注意：使用FindObjectsOfType避免类名冲突问题
            var managers = FindObjectsOfType<MonoBehaviour>();
            object formationManager = null;

            // 查找HorizontalBattleFormationManager类型的对象
            foreach (var manager in managers)
            {
                if (manager.GetType().Name == "HorizontalBattleFormationManager")
                {
                    formationManager = manager;
                    break;
                }
            }

            if (formationManager != null && characterStats != null)
            {
                // 使用反射调用IsMeleeClass方法 - 解决类名冲突问题
                var managerType = formationManager.GetType();
                var isMeleeMethod = managerType.GetMethod("IsMeleeClass");

                if (isMeleeMethod != null)
                {
                    bool isMelee = (bool)isMeleeMethod.Invoke(formationManager, new object[] { characterStats });

                    if (isMelee)
                    {
                        // 前排近战职业：原地播放攻击动画（目标会自动移动到攻击范围）
                        PlayAnimation(animationMapping.attackAnimation, false);
                    }
                    else
                    {
                        // 后排远程职业：原地远程攻击
                        PlayRangedAttack();
                    }
                }
                else
                {
                    // 如果反射调用失败，默认使用原地攻击
                    PlayAnimation(animationMapping.attackAnimation, false);
                }
            }
            else
            {
                // 如果无法判断职业类型，默认使用原地攻击
                PlayAnimation(animationMapping.attackAnimation, false);
            }
        }

        // 播放近战攻击（带位移）- 仅供近战职业使用
        public void PlayMeleeAttack(Vector3 targetPosition)
        {
            isMovingForAttack = true;

            // 使用DOTween移动到目标位置
            transform.DOMove(targetPosition, 0.3f).OnComplete(() => {
                // 到达位置后播放攻击动画
                PlayAnimation(animationMapping.attackAnimation, false);
            });
        }

        // 播放远程攻击（原地）- 供远程职业使用
        public void PlayRangedAttack()
        {
            isMovingForAttack = false;
            PlayAnimation(animationMapping.attackAnimation, false);
        }

        // 返回原位置
        private void ReturnToOriginalPosition()
        {
            transform.DOMove(originalPosition, 0.3f).OnComplete(() => {
                isMovingForAttack = false;
                PlayIdleAnimation();
            });
        }

        // 播放受击动画
        public void PlayHitAnimation()
        {
            try
            {
                if (skeletonAnimation == null)
                {
                    Debug.LogError($"PlayHitAnimation: {gameObject.name} 的skeletonAnimation为null");
                    skeletonAnimation = GetComponent<SkeletonAnimation>();
                    if (skeletonAnimation == null)
                    {
                        Debug.LogError($"PlayHitAnimation: {gameObject.name} 没有SkeletonAnimation组件");
                        return;
                    }
                }

                string hitAnimName = animationMapping.hitAnimation;
                if (string.IsNullOrEmpty(hitAnimName))
                {
                    Debug.LogError($"PlayHitAnimation: {gameObject.name} 的hitAnimation名称为空");
                    hitAnimName = "hit";
                }

                // 检查动画是否存在
                bool animExists = false;
                if (skeletonAnimation.skeleton != null && skeletonAnimation.skeleton.Data != null)
                {
                    foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations)
                    {
                        if (anim.Name == hitAnimName)
                        {
                            animExists = true;
                            break;
                        }
                    }

                    if (!animExists)
                    {
                        Debug.LogError($"PlayHitAnimation: {gameObject.name} 的SkeletonData中不存在名为 {hitAnimName} 的动画");

                        // 尝试使用其他可能的受击动画名称
                        string[] possibleHitAnims = { "hit", "hurt", "damage", "injured" };
                        foreach (string possibleAnim in possibleHitAnims)
                        {
                            foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations)
                            {
                                if (anim.Name.ToLower().Contains(possibleAnim))
                                {
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
                else
                {
                    Debug.LogError($"PlayHitAnimation: {gameObject.name} 的skeleton或skeletonData为null");
                    return;
                }

                // 停止所有当前动画，确保受击动画能够播放
                skeletonAnimation.AnimationState.ClearTrack(0);

                // 播放动画
                if (skeletonAnimation != null && !string.IsNullOrEmpty(hitAnimName))
                {
                    Spine.TrackEntry trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, hitAnimName, false);
                    trackEntry.MixDuration = 0.1f;
                    trackEntry.TimeScale = 1.0f;
                    currentAnimation = hitAnimName;
                }
                else
                {
                    Debug.LogError($"无法播放受击动画: skeletonAnimation={skeletonAnimation}, hitAnimName={hitAnimName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PlayHitAnimation出错: {e.Message}\n{e.StackTrace}");
            }
        }

        // 播放死亡动画
        public void PlayDeathAnimation()
        {
            try
            {
                if (skeletonAnimation == null)
                {
                    Debug.LogError($"PlayDeathAnimation: {gameObject.name} 的skeletonAnimation为null");
                    skeletonAnimation = GetComponent<SkeletonAnimation>();
                    if (skeletonAnimation == null)
                    {
                        Debug.LogError($"PlayDeathAnimation: {gameObject.name} 没有SkeletonAnimation组件");
                        return;
                    }
                }

                string deathAnimName = animationMapping.deathAnimation;
                if (string.IsNullOrEmpty(deathAnimName))
                {
                    Debug.LogError($"PlayDeathAnimation: {gameObject.name} 的deathAnimation名称为空");
                    deathAnimName = "dead";
                }

                // 检查动画是否存在
                bool animExists = false;
                if (skeletonAnimation.skeleton != null && skeletonAnimation.skeleton.Data != null)
                {
                    foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations)
                    {
                        if (anim.Name == deathAnimName)
                        {
                            animExists = true;
                            break;
                        }
                    }

                    if (!animExists)
                    {
                        Debug.LogError($"PlayDeathAnimation: {gameObject.name} 的SkeletonData中不存在名为 {deathAnimName} 的动画");

                        // 尝试使用其他可能的死亡动画名称
                        string[] possibleDeathAnims = { "dead", "death", "die", "defeat" };
                        foreach (string possibleAnim in possibleDeathAnims)
                        {
                            foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations)
                            {
                                if (anim.Name.ToLower().Contains(possibleAnim))
                                {
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
                else
                {
                    Debug.LogError($"PlayDeathAnimation: {gameObject.name} 的skeleton或skeletonData为null");
                    return;
                }

                // 停止所有当前动画，确保死亡动画能够播放
                skeletonAnimation.AnimationState.ClearTrack(0);

                // 播放动画
                if (skeletonAnimation != null && !string.IsNullOrEmpty(deathAnimName))
                {
                    Spine.TrackEntry trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, deathAnimName, false);
                    trackEntry.MixDuration = 0.1f;
                    currentAnimation = deathAnimName;
                }
                else
                {
                    Debug.LogError($"无法播放死亡动画: skeletonAnimation={skeletonAnimation}, deathAnimName={deathAnimName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PlayDeathAnimation出错: {e.Message}\n{e.StackTrace}");
            }
        }

        // 播放施法动画
        public void PlayCastAnimation()
        {
            PlayAnimation(animationMapping.castAnimation, false);
        }

        // 停止行走动画并播放过渡动画
        public void StopWalkWithTransition()
        {
            // 检查角色是否已死亡
            if (characterStats != null && characterStats.currentHitPoints <= 0)
            {
                Debug.Log($"{gameObject.name} 已死亡，不播放过渡动画");
                PlayAnimation(animationMapping.deathAnimation, false);
            }
            else
            {
                // 检查过渡动画是否存在
                if (skeletonAnimation != null && skeletonAnimation.Skeleton != null && skeletonAnimation.Skeleton.Data != null)
                {
                    Spine.Animation transitionAnim = skeletonAnimation.Skeleton.Data.FindAnimation(animationMapping.moveToIdleAnimation);
                    if (transitionAnim != null)
                    {
                        PlayAnimation(animationMapping.moveToIdleAnimation, false);
                        Debug.Log($"{gameObject.name} 播放移动到待机的过渡动画: {animationMapping.moveToIdleAnimation}");
                    }
                    else
                    {
                        Debug.LogWarning($"{gameObject.name} 过渡动画 {animationMapping.moveToIdleAnimation} 不存在，直接切换到待机动画");
                        PlayIdleAnimation();
                    }
                }
                else
                {
                    PlayIdleAnimation();
                }
            }
        }

        /// <summary>
        /// 播放昏迷动画 - 玩家和队友专用
        /// </summary>
        public void PlayUnconsciousAnimation()
        {
            // 检查是否配置了昏迷动画
            if (string.IsNullOrEmpty(animationMapping.unconsciousAnimation))
            {
                Debug.LogWarning($"{gameObject.name} 没有配置昏迷动画，怪物预制体无需此动画");
                // 怪物直接播放死亡动画
                PlayDeathAnimation();
                return;
            }

            if (skeletonAnimation == null)
            {
                Debug.LogWarning($"{gameObject.name} 没有SkeletonAnimation组件，无法播放昏迷动画");
                return;
            }

            // 检查昏迷动画是否存在
            bool animExists = false;
            if (skeletonAnimation.skeleton != null && skeletonAnimation.skeleton.Data != null)
            {
                foreach (Spine.Animation anim in skeletonAnimation.skeleton.Data.Animations)
                {
                    if (anim.Name == animationMapping.unconsciousAnimation)
                    {
                        animExists = true;
                        break;
                    }
                }
            }

            if (animExists)
            {
                PlayAnimation(animationMapping.unconsciousAnimation, true); // 循环播放昏迷动画
                Debug.Log($"{gameObject.name} 播放昏迷动画: {animationMapping.unconsciousAnimation}");
            }
            else
            {
                // 如果没有昏迷动画，使用受击动画作为替代
                Debug.LogWarning($"{gameObject.name} 昏迷动画不存在，使用受击动画替代");
                PlayAnimation(animationMapping.hitAnimation, true);
            }
        }

        /// <summary>
        /// 播放恢复动画 - 从昏迷状态恢复到待机
        /// </summary>
        public void PlayReviveAnimation()
        {
            if (skeletonAnimation == null)
            {
                Debug.LogWarning($"{gameObject.name} 没有SkeletonAnimation组件，无法播放恢复动画");
                return;
            }

            Debug.Log($"{gameObject.name} 从昏迷中恢复，切换到待机动画");

            // 停止当前动画
            skeletonAnimation.AnimationState.ClearTrack(0);

            // 直接切换到待机动画
            PlayIdleAnimation();
        }

        /// <summary>
        /// 播放待机动画
        /// </summary>
        public void PlayIdleAnimation()
        {
            if (skeletonAnimation == null)
            {
                Debug.LogWarning($"{gameObject.name} 没有SkeletonAnimation组件，无法播放待机动画");
                return;
            }
            PlayAnimation(animationMapping.idleAnimation, true);
            Debug.Log($"{gameObject.name} 开始播放待机动画: {animationMapping.idleAnimation}");
        }

        /// <summary>
        /// 播放走路动画
        /// </summary>
        public void PlayWalkAnimation()
        {
            if (skeletonAnimation == null)
            {
                Debug.LogWarning($"{gameObject.name} 没有SkeletonAnimation组件，无法播放走路动画");
                return;
            }
            PlayAnimation(animationMapping.walkAnimation, true);
            Debug.Log($"{gameObject.name} 开始播放走路动画: {animationMapping.walkAnimation}");
        }

        /// <summary>
        /// 播放法术动画（施法动画的别名）
        /// </summary>
        public void PlaySpellAnimation()
        {
            PlayCastAnimation();
        }
    }
}
