using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using demo2.DND.HorizontalFormation;
using DG.Tweening;

namespace demo2.DND
{
    /// <summary>
    /// 角色属性组件 - 使用DOTween+事件驱动，摒弃协程
    /// </summary>
    public class CharacterStats : MonoBehaviour {
        [Header("角色模板")]
        public CharacterTemplate template;

        [Header("事件通道")]
        public DamageEventChannel_SO damageEventChannel; // 拖入伤害事件通道资产

        [Header("运行时数据")]
        public string characterName = "角色";
        public CharacterClass characterClass = CharacterClass.Fighter;
        public int characterLevel = 1;
        public BattleSide battleSide = BattleSide.Player;

        [Header("当前状态")]
        public int maxHitPoints = 10;
        public int currentHitPoints = 10;
        public int temporaryHitPoints; // 移除默认值初始化
        public int armorClass = 10;

        [Header("状态效果")]
        public List<StatusEffectType> statusEffects = new List<StatusEffectType>();

        // 从模板初始化时的属性值
        [HideInInspector] public int strength = 10;
        [HideInInspector] public int dexterity = 10;
        [HideInInspector] public int constitution = 10;
        [HideInInspector] public int intelligence = 10;
        [HideInInspector] public int wisdom = 10;
        [HideInInspector] public int charisma = 10;

        // 属性调整值
        public int StrMod => (strength - 10) / 2;
        public int DexMod => (dexterity - 10) / 2;
        public int ConMod => (constitution - 10) / 2;
        public int IntMod => (intelligence - 10) / 2;
        public int WisMod => (wisdom - 10) / 2;
        public int ChaMod => (charisma - 10) / 2;

        // 便捷属性访问 - 修正命名规范
        public int level => characterLevel;
        public int Level => characterLevel;

        // 昏迷恢复系统的私有变量
        private int unconsciousSuccessCount = 0;
        private int unconsciousFailureCount = 0;
        private float nextSavingThrowTime = 0f;
        private bool isProcessingSavingThrows = false;

        void Start() {
            // 如果有模板，从模板初始化
            if (template != null) {
                InitializeFromTemplate();
            }

            // 设置DND_CharacterAdapter事件监听
            SetupAdapterEvents();
        }

        /// <summary>
        /// 设置DND_CharacterAdapter事件监听
        /// </summary>
        private void SetupAdapterEvents() {
            DND_CharacterAdapter adapter = GetComponent<DND_CharacterAdapter>();
            if (adapter != null) {
                // 监听状态变化事件
                adapter.OnStateChanged += HandleAdapterStateChange;

                Debug.Log($"{GetDisplayName()} 已连接动画适配器事件");
            }
        }

        /// <summary>
        /// 处理动画适配器的状态变化事件
        /// </summary>
        private void HandleAdapterStateChange(string state) {
            switch (state) {
                case "unconscious":
                    Debug.Log($"{GetDisplayName()} 动画适配器报告：进入昏迷状态");
                    break;

                case "footstep":
                    // 播放脚步声音效
                    PlayFootstepSound();
                    break;

                default:
                    Debug.Log($"{GetDisplayName()} 动画状态变化: {state}");
                    break;
            }
        }

        /// <summary>
        /// 播放脚步声音效
        /// </summary>
        private void PlayFootstepSound() {
            // 这里可以调用音效管理器播放脚步声
            // AudioManager.Instance?.PlaySound("footstep");
        }

        private void OnEnable() {
            // 订阅伤害事件
            if (damageEventChannel != null) {
                damageEventChannel.OnEventRaised += HandleDamageEvent;
            }
        }

        private void OnDisable() {
            // 取消订阅伤害事件
            if (damageEventChannel != null) {
                damageEventChannel.OnEventRaised -= HandleDamageEvent;
            }
        }

        /// <summary>
        /// 处理伤害事件 - 只有当自己是受伤目标时才处理
        /// </summary>
        private void HandleDamageEvent(CharacterStats recipient, CharacterStats dealer, int damage, bool isCritical) {
            if (recipient != this) return; // 确保是自己受伤的事件

            // 处理伤害逻辑（原TakeDamage方法的核心逻辑）
            ApplyDamageToSelf(damage, isCritical);
        }

        /// <summary>
        /// 应用伤害到自身（从原TakeDamage方法重构）
        /// </summary>
        private void ApplyDamageToSelf(int damage, bool isCritical = false) {
            if (template != null) {
                // 检查免疫（这里简化处理，实际应该从攻击中获取伤害类型）
                DamageType damageType = DamageType.Bludgeoning; // 默认钝击伤害

                if (template.immunities.Contains(damageType)) {
                    Debug.Log($"{GetDisplayName()} 免疫 {damageType} 伤害!");
                    return;
                }

                // 检查抗性和弱点
                if (template.resistances.Contains(damageType)) {
                    damage = Mathf.Max(1, damage / 2);
                    Debug.Log($"{GetDisplayName()} 对 {damageType} 伤害有抗性!");
                }
                else if (template.vulnerabilities.Contains(damageType)) {
                    damage *= 2;
                    Debug.Log($"{GetDisplayName()} 对 {damageType} 伤害有弱点!");
                }
            }

            // 先扣除临时生命值
            if (temporaryHitPoints > 0) {
                if (temporaryHitPoints >= damage) {
                    temporaryHitPoints -= damage;
                    damage = 0;
                }
                else {
                    damage -= temporaryHitPoints;
                    temporaryHitPoints = 0;
                }
            }

            // 扣除实际生命值
            currentHitPoints = Mathf.Max(0, currentHitPoints - damage);

            Debug.Log($"{GetDisplayName()} 受到 {damage} 点伤害{(isCritical ? " (暴击!)" : "")}! 剩余生命值: {currentHitPoints}/{maxHitPoints}");

            // 播放受击动画（关键修复）
            PlayHitAnimation();

            // 显示伤害数字
            ShowDamageNumber(damage);

            // 检查是否死亡
            if (currentHitPoints <= 0) {
                HandleDeath();
            }
        }

        /// <summary>
        /// 处理角色死亡
        /// </summary>
        private void HandleDeath() {
            if (battleSide == BattleSide.Player) {
                // 玩家角色进入昏迷状态
                HandlePlayerUnconsciousness();
            } else {
                // 敌人直接死亡
                HandleEnemyDeath();
            }
        }

        /// <summary>
        /// 处理玩家角色昏迷
        /// </summary>
        private void HandlePlayerUnconsciousness() {
            // 添加昏迷状态
            AddStatusEffect(StatusEffectType.Unconscious);
            Debug.Log($"{GetDisplayName()} 失去意识，进入昏迷状态!");

            // 播放昏迷动画
            PlayUnconsciousAnimation();

            // 启动昏迷恢复机制（使用事件驱动替代协程）
            StartUnconsciousSavingThrows();
        }

        /// <summary>
        /// 处理敌人死亡
        /// </summary>
        private void HandleEnemyDeath() {
            // 添加死亡状态
            AddStatusEffect(StatusEffectType.Unconscious);
            Debug.Log($"{GetDisplayName()} 死亡!");

            // 播放死亡动画
            PlayDeathAnimation();

            // 启动尸体消失逻辑（使用DOTween替代协程）
            StartCorpseDisappearance();
        }

        /// <summary>
        /// 播放受击动画
        /// </summary>
        private void PlayHitAnimation() {
            // 获取角色动画适配器组件
            DND_CharacterAdapter characterAdapter = GetComponent<DND_CharacterAdapter>();
            if (characterAdapter != null) {
                characterAdapter.PlayHitAnimation();
            }
        }

        /// <summary>
        /// 播放昏迷动画
        /// </summary>
        private void PlayUnconsciousAnimation() {
            // 获取角色动画适配器组件
            DND_CharacterAdapter characterAdapter = GetComponent<DND_CharacterAdapter>();
            if (characterAdapter != null) {
                characterAdapter.PlayUnconsciousAnimation();
            }
        }

        /// <summary>
        /// 播放死亡动画
        /// </summary>
        private void PlayDeathAnimation() {
            // 获取角色动画适配器组件
            DND_CharacterAdapter characterAdapter = GetComponent<DND_CharacterAdapter>();
            if (characterAdapter != null) {
                characterAdapter.PlayDeathAnimation();
            }
        }

        /// <summary>
        /// 启动昏迷状态的体质豁免判断 - DOTween版本
        /// </summary>
        private void StartUnconsciousSavingThrows() {
            unconsciousSuccessCount = 0;
            unconsciousFailureCount = 0;
            isProcessingSavingThrows = true;
            nextSavingThrowTime = Time.time + 6f; // 6秒后第一次豁免

            Debug.Log($"{GetDisplayName()} 开始昏迷状态体质豁免判断");
        }

        void Update() {
            // 处理昏迷状态的体质豁免
            if (isProcessingSavingThrows && HasStatusEffect(StatusEffectType.Unconscious)) {
                ProcessUnconsciousSavingThrows();
            }
        }

        /// <summary>
        /// 处理昏迷状态的体质豁免判断 - Update版本
        /// </summary>
        private void ProcessUnconsciousSavingThrows() {
            if (Time.time < nextSavingThrowTime) return;

            int maxAttempts = 3;
            int savingThrowDC = 10;

            // 进行体质豁免检定
            int constitutionSave = Random.Range(1, 21) + ConMod;

            if (constitutionSave >= savingThrowDC) {
                unconsciousSuccessCount++;
                Debug.Log($"{GetDisplayName()} 体质豁免成功 ({constitutionSave} vs DC{savingThrowDC}) - 成功次数: {unconsciousSuccessCount}/{maxAttempts}");
            } else {
                unconsciousFailureCount++;
                Debug.Log($"{GetDisplayName()} 体质豁免失败 ({constitutionSave} vs DC{savingThrowDC}) - 失败次数: {unconsciousFailureCount}/{maxAttempts}");
            }

            // 检查是否达到结束条件
            if (unconsciousSuccessCount >= maxAttempts) {
                // 3次成功：恢复1点血量并脱离昏迷
                isProcessingSavingThrows = false;
                currentHitPoints = 1;
                RemoveStatusEffect(StatusEffectType.Unconscious);
                Debug.Log($"{GetDisplayName()} 体质豁免成功，恢复意识并获得1点血量!");

                // 切换回待机动画
                DND_CharacterAdapter characterAdapter = GetComponent<DND_CharacterAdapter>();
                if (characterAdapter != null) {
                    characterAdapter.PlayIdleAnimation();
                }
            } else if (unconsciousFailureCount >= maxAttempts) {
                // 3次失败：真正死亡
                isProcessingSavingThrows = false;
                Debug.Log($"{GetDisplayName()} 体质豁免失败，真正死亡!");
                HandleTrueDeath();
            } else {
                // 继续下一次豁免
                nextSavingThrowTime = Time.time + 6f;
            }
        }

        /// <summary>
        /// 处理真正的死亡（玩家角色豁免失败后）
        /// </summary>
        private void HandleTrueDeath() {
            Debug.Log($"{GetDisplayName()} 真正死亡!");

            // 播放死亡动画
            PlayDeathAnimation();

            // 启动尸体消失逻辑 - 使用DOTween替代协程
            StartCorpseDisappearance();
        }

        /// <summary>
        /// 启动尸体消失逻辑 - DOTween版本
        /// </summary>
        private void StartCorpseDisappearance() {
            // 使用DOTween序列替代协程
            Sequence corpseSequence = DOTween.Sequence();

            // 等待死亡动画播放完成
            corpseSequence.AppendInterval(3f);

            // 开始淡出效果
            corpseSequence.AppendCallback(() => StartFadeOutCorpse());

            // 等待淡出完成后销毁
            corpseSequence.AppendInterval(2f);

            corpseSequence.OnComplete(() => {
                RemoveFromFormation();
                Debug.Log($"{GetDisplayName()} 尸体已消失");
                Destroy(gameObject);
            });
        }

        /// <summary>
        /// 开始淡出尸体效果 - DOTween版本
        /// </summary>
        private void StartFadeOutCorpse() {
            DND_CharacterAdapter adapter = GetComponent<DND_CharacterAdapter>();
            if (adapter != null && adapter.skeletonAnimation != null) {
                // 使用DOTween对Spine角色进行淡出
                DOTween.To(() => adapter.skeletonAnimation.skeleton.A,
                          x => adapter.skeletonAnimation.skeleton.A = x,
                          0f, 2f);
            } else {
                // 如果没有Spine动画，尝试使用SpriteRenderer
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null) {
                    spriteRenderer.DOFade(0f, 2f);
                }
            }
        }

        /// <summary>
        /// 从阵型管理器中移除角色
        /// </summary>
        private void RemoveFromFormation() {
            // 查找阵型管理器
            HorizontalBattleFormationManager formationManager = FindObjectOfType<HorizontalBattleFormationManager>();
            if (formationManager != null) {
                // 这里可以添加从阵型中移除角色的逻辑
                Debug.Log($"{GetDisplayName()} 已从阵型中移除");
            }
        }

        /// <summary>
        /// 从模板初始化角色数据
        /// </summary>
        public void InitializeFromTemplate() {
            if (template == null) return;

            // 复制基本信息
            characterName = template.characterName;
            characterClass = template.characterClass;
            characterLevel = template.level;
            battleSide = template.defaultSide;

            // 复制属性
            strength = template.strength;
            dexterity = template.dexterity;
            constitution = template.constitution;
            intelligence = template.intelligence;
            wisdom = template.wisdom;
            charisma = template.charisma;

            // 计算战斗属性
            maxHitPoints = template.CalculateHitPoints();
            currentHitPoints = maxHitPoints;
            armorClass = template.baseArmorClass;

            Debug.Log($"{characterName} 从模板初始化完成 - 等级{characterLevel} - 血量{maxHitPoints} - AC{armorClass}");
        }

        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName() {
            return !string.IsNullOrEmpty(characterName) ? characterName : "未命名角色";
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(int damage, DamageType damageType = DamageType.Bludgeoning) {
            if (template != null) {
                // 检查免疫
                if (template.immunities.Contains(damageType)) {
                    Debug.Log($"{GetDisplayName()} 免疫 {damageType} 伤害!");
                    return;
                }

                // 检查抗性和弱点
                if (template.resistances.Contains(damageType)) {
                    damage = Mathf.Max(1, damage / 2);
                    Debug.Log($"{GetDisplayName()} 对 {damageType} 伤害有抗性!");
                }
                else if (template.vulnerabilities.Contains(damageType)) {
                    damage *= 2;
                    Debug.Log($"{GetDisplayName()} 对 {damageType} 伤害有弱点!");
                }
            }

            // 先扣除临时生命值
            if (temporaryHitPoints > 0) {
                if (temporaryHitPoints >= damage) {
                    temporaryHitPoints -= damage;
                    damage = 0;
                }
                else {
                    damage -= temporaryHitPoints;
                    temporaryHitPoints = 0;
                }
            }

            // 扣除实际生命值
            currentHitPoints = Mathf.Max(0, currentHitPoints - damage);

            Debug.Log($"{GetDisplayName()} 受到 {damage} 点 {damageType} 伤害! 剩余生命值: {currentHitPoints}/{maxHitPoints}");

            // 检查是否失去意识
            if (currentHitPoints <= 0) {
                AddStatusEffect(StatusEffectType.Unconscious);
                Debug.Log($"{GetDisplayName()} 失去意识!");
            }
        }

        /// <summary>
        /// 恢复生命值
        /// </summary>
        public void HealDamage(int amount) {
            currentHitPoints = Mathf.Min(maxHitPoints, currentHitPoints + amount);

            Debug.Log($"{GetDisplayName()} 恢复 {amount} 点生命值! 当前生命值: {currentHitPoints}/{maxHitPoints}");

            // 显示治疗数字
            ShowHealNumber(amount);

            // 如果恢复意识
            if (currentHitPoints > 0 && HasStatusEffect(StatusEffectType.Unconscious)) {
                RemoveStatusEffect(StatusEffectType.Unconscious);
                Debug.Log($"{GetDisplayName()} 恢复意识!");
            }
        }

        /// <summary>
        /// 检查是否具有特定状态效果
        /// </summary>
        public bool HasStatusEffect(StatusEffectType type) {
            return statusEffects.Contains(type);
        }

        /// <summary>
        /// 添加状态效果
        /// </summary>
        public void AddStatusEffect(StatusEffectType type) {
            if (!statusEffects.Contains(type)) {
                statusEffects.Add(type);

                // 如果是闪避状态，更新AC
                if (type == StatusEffectType.Dodging) {
                    UpdateArmorClass();
                }
            }
        }

        /// <summary>
        /// 移除状态效果
        /// </summary>
        public void RemoveStatusEffect(StatusEffectType type) {
            if (!statusEffects.Contains(type)) return; // 如果状态不存在，直接返回

            statusEffects.Remove(type);

            // 如果移除了闪避状态，更新AC
            if (type == StatusEffectType.Dodging) {
                UpdateArmorClass();
            }

            // 注意：移除昏迷状态的恢复逻辑应该在别处处理，避免递归
        }

        /// <summary>
        /// 从昏迷中恢复 - 玩家和队友专用
        /// 修复：避免无限递归
        /// </summary>
        private void ReviveFromUnconsciousness() {
            // 直接移除昏迷状态，不调用RemoveStatusEffect避免递归
            if (statusEffects.Contains(StatusEffectType.Unconscious)) {
                statusEffects.Remove(StatusEffectType.Unconscious);
            }

            // 停止昏迷恢复处理
            isProcessingSavingThrows = false;

            Debug.Log($"{GetDisplayName()} 从昏迷中恢复，重新站起来!");

            // 播放恢复动画
            PlayReviveAnimation();
        }

        /// <summary>
        /// 播放恢复动画 - 从昏迷状态恢复到待机
        /// </summary>
        private void PlayReviveAnimation() {
            DND_CharacterAdapter characterAdapter = GetComponent<DND_CharacterAdapter>();
            if (characterAdapter != null) {
                characterAdapter.PlayIdleAnimation();
            }
        }

        /// <summary>
        /// 显示伤害数字
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="isDamage">是否为伤害（true）还是治疗（false）</param>
        private void ShowDamageNumber(int damage, bool isDamage = true) {
            // 使用统一的伤害显示管理器
            if (DamageDisplayManager.Instance != null) {
                DamageDisplayManager.Instance.ShowDamageNumber(transform, damage, isDamage);
            } else {
                Debug.LogWarning($"没有找到伤害显示管理器，无法显示伤害数字");
            }
        }

        /// <summary>
        /// 显示MISS
        /// </summary>
        public void ShowMiss() {
            // 使用统一的伤害显示管理器
            if (DamageDisplayManager.Instance != null) {
                DamageDisplayManager.Instance.ShowMiss(transform);
            } else {
                Debug.LogWarning($"没有找到伤害显示管理器，无法显示MISS");
            }
        }

        /// <summary>
        /// 显示治疗数字
        /// </summary>
        /// <param name="healAmount">治疗量</param>
        private void ShowHealNumber(int healAmount) {
            ShowDamageNumber(healAmount, false); // false表示治疗
        }

        /// <summary>
        /// 检查角色是否已死亡（敌人专用）或昏迷（玩家队友专用）
        /// </summary>
        public bool IsDownOrDead() {
            return currentHitPoints <= 0;
        }

        /// <summary>
        /// 检查角色是否可以行动（未昏迷且血量大于0）
        /// </summary>
        public bool CanAct() {
            return currentHitPoints > 0 && !HasStatusEffect(StatusEffectType.Unconscious);
        }

        /// <summary>
        /// 更新护甲等级
        /// </summary>
        public void UpdateArmorClass() {
            // 从基础AC开始
            int baseAc = template != null ? template.baseArmorClass : 10;
            armorClass = baseAc;

            // 应用状态效果的修正
            if (HasStatusEffect(StatusEffectType.Dodging)) {
                armorClass += 2; // 防御姿态提供+2 AC
                Debug.Log($"{GetDisplayName()} 处于防御姿态，AC+2，当前AC: {armorClass}");
            }
        }

        /// <summary>
        /// 进行技能检定
        /// </summary>
        public int SkillCheck(Skill skill) {
            if (template == null) {
                Debug.LogWarning($"{GetDisplayName()} 没有角色模板，无法进行技能检定");
                return Random.Range(1, 21);
            }

            int bonus = template.GetSkillBonus(skill);
            int roll = Random.Range(1, 21);
            int total = roll + bonus;

            Debug.Log($"{GetDisplayName()} 进行 {skill} 检定: 掷骰 {roll} + 加值 {bonus} = {total}");
            return total;
        }

        /// <summary>
        /// 进行豁免检定
        /// </summary>
        public int SavingThrow(string ability) {
            if (template == null) {
                Debug.LogWarning($"{GetDisplayName()} 没有角色模板，无法进行豁免检定");
                return Random.Range(1, 21);
            }

            int bonus = template.GetSavingThrowBonus(ability);
            int roll = Random.Range(1, 21);
            int total = roll + bonus;

            Debug.Log($"{GetDisplayName()} 进行 {ability} 豁免检定: 掷骰 {roll} + 加值 {bonus} = {total}");
            return total;
        }
    }
}
