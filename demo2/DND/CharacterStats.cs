using System.Collections.Generic;
using UnityEngine;
using demo2.DND.HorizontalFormation;
using DG.Tweening;
using UnityEngine.UI;
using System;

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

        // 标记：是否已经进入死亡表现（用于阵型清理与诊断）
        public bool hasPlayedDeath;

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

        // 新增：昏迷/豁免相关私有字段（之前被误删）
        private int unconsciousSuccessCount;
        private int unconsciousFailureCount;
        private float nextSavingThrowTime; // 仍保留字段以兼容旧逻辑，但不会用于定时触发
        private bool isProcessingSavingThrows; // 仍保留以便调用 StartUnconsciousSavingThrows 初始化状态

        // 便捷属性访问 - 修正命名规范（移除重复的小写 level）
        public int Level => characterLevel;

        private void Awake() {
            // 从模板初始化角色数据
            InitializeFromTemplate();

            // 连接动画适配器事件
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
        private void ApplyDamageToSelf(int damage, bool isCritical = false, DamageType damageType = DamageType.Bludgeoning) {
            if (template != null) {
                // 检查免疫（这里简化处理，实际应该从攻击中获取伤害类型）
                // 使用 template 中的免疫/抗性/弱点信息来调整 damage

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

            // 处理暴击（如果传入 isCritical，则加倍伤害，或按需更改）
            if (isCritical) {
                damage *= 2;
                Debug.Log($"{GetDisplayName()} 受到暴击! 伤害翻倍: {damage}");
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

            // 如果已经处于昏迷/倒地状态：不再改变HP，记录死豁失败，并累计倒地期间受到的伤害用于直死判定
            if (HasStatusEffect(StatusEffectType.Unconscious)) {
                Debug.Log($"{GetDisplayName()} 在倒地状态受到攻击，记录死豁失败 (isCritical={isCritical})");
                RegisterUnconsciousHit(isCritical);

                // 显示伤害数字与刷新UI（保持视觉反馈）
                try { ShowDamageNumber(damage, true); } catch (Exception) { }
                HealthBarUIManager.Instance?.RefreshBar(this);
                NotifyHealthChanged();
                return;
            }

            // 记录修改前生命值以便计算溢出（overflow）
            int prevHp = currentHitPoints;

            // 扣除实际生命值（允许先计算溢出，再将hp夹住为0）
            int afterHp = currentHitPoints - damage;

            int overflow = 0;
            if (afterHp < 0) {
                overflow = -afterHp; // 溢出为正数
            }

            currentHitPoints = Mathf.Max(0, afterHp);
            Debug.Log($"{GetDisplayName()} 受到 {damage} 点 {damageType} 伤害! 剩余生命值: {currentHitPoints}/{maxHitPoints}");

            // 如果发生溢出伤害，记录为倒地负值基数
            if (overflow > 0) {
                // 文档规则取消基于体质的即时死亡判定，这里不再累计倒地伤害，仅做日志记录
                Debug.Log($"{GetDisplayName()} 溢出伤害: {overflow}（按文档规则不触发体质阈值直死）");
            }

            // 显示伤害数字（优先使用 DamageDisplayManager）
            try {
                ShowDamageNumber(damage, true);
            }
            catch (System.Exception ex) {
                Debug.LogWarning($"ApplyDamageToSelf: ShowDamageNumber 触发异常 - {ex}");
            }

            // 通知UI直接刷新
            HealthBarUIManager.Instance?.RefreshBar(this);

            // 新增：触发本地血量变化事件，供直接绑定的UI使用
            NotifyHealthChanged();

            // 检查是否失去意识或死亡
            if (currentHitPoints <= 0) {
                // 玩家方：进入昏迷并按回合死豁；不再进行体质阈值直死判定
                if (battleSide == BattleSide.Player) {
                    HandleDeath(); // 内部会分派到 HandlePlayerUnconsciousness
                }
                else {
                    // 敌人：直接死亡，不进入昏迷
                    HandleEnemyDeath();
                }
            }
        }

        /// <summary>
        /// 在倒地（Unconscious）状态下记录受到攻击导致的死豁失败次数
        /// 普通攻击计1次失败，暴击计2次失败。达到3次失败触发真正死亡。
        /// </summary>
        public void RegisterUnconsciousHit(bool isCritical)
        {
            if (!HasStatusEffect(StatusEffectType.Unconscious)) return;

            int add = isCritical ? 2 : 1;
            unconsciousFailureCount += add;
            Debug.Log($"{GetDisplayName()} 倒地时受到攻击，记录死豁失败 +{add} -> now failures={unconsciousFailureCount}/3");

            // 检查是否达到真正死亡
            if (unconsciousFailureCount >= 3)
            {
                Debug.Log($"{GetDisplayName()} 倒地死豁失败达到3次，触发真正死亡");
                HandleTrueDeath();
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

            // 播放昏迷动画（使用SO映射名/兜底名，直接驱动动画，不依赖Spine事件）
            PlayUnconsciousAnimation();

            // 启动昏迷恢复机制（按回合触发死豁）
            StartUnconsciousSavingThrows();
        }

        /// <summary>
        /// 处理敌人死亡
        /// </summary>
        private void HandleEnemyDeath() {
            // 敌人直接死亡，不再添加昏迷状态
            Debug.Log($"{GetDisplayName()} 死亡!");

            // 播放死亡动画
            PlayDeathAnimation();

            // 启动尸体消失逻辑（3秒后消失）
            StartCorpseDisappearance();
        }

        /// <summary>
        /// 播放受击动画
        /// </summary>
        private void PlayHitAnimation() {
            DND_CharacterAdapter characterAdapter = GetAdapter();
            if (characterAdapter != null) {
                characterAdapter.PlayHitAnimation();
            }
        }

        /// <summary>
        /// 播放昏迷动画
        /// </summary>
        private void PlayUnconsciousAnimation() {
            DND_CharacterAdapter characterAdapter = GetAdapter();
            if (characterAdapter != null) {
                characterAdapter.PlayUnconsciousAnimation();
            }
        }

        /// <summary>
        /// 播放死亡动画
        /// </summary>
        private void PlayDeathAnimation() {
            DND_CharacterAdapter characterAdapter = GetAdapter();
            if (characterAdapter != null) {
                characterAdapter.PlayDeathAnimation();
                hasPlayedDeath = true;
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
            // 不再使用基于 Time 的自动豁免触发——豁免现在由 AutoBattleAI 按回合调用 PerformDeathSaveTick()
        }

        /// <summary>
        /// 处理昏迷状态的体质豁免判断 - Update版本
        /// </summary>
        private void ProcessUnconsciousSavingThrows() {
            // 保留该方法以兼容旧代码，但不主动触发——使用 PerformDeathSave 在回合中进行一次豁免判定
         }

        /// <summary>
        /// 在角色回合时调用：执行一次体质豁免（D20 + ConMod vs DC10）。
        /// 成功计为一次成功，失败计为一次失败。达到3次成功 => 恢复1HP并脱离昏迷；达到3次失败 => 真正死亡。
        /// 这个方法用于将死豁从基于时间改为严格按回合触发。
        /// </summary>
        public void PerformDeathSaveTick()
        {
            if (!HasStatusEffect(StatusEffectType.Unconscious)) return;

            int savingThrowDc = 10;
            int constitutionSave = UnityEngine.Random.Range(1, 21) + ConMod;

            if (constitutionSave >= savingThrowDc)
            {
                unconsciousSuccessCount++;
                Debug.Log($"{GetDisplayName()} 回合体质豁免成功 ({constitutionSave} vs DC{savingThrowDc}) - 成功次数: {unconsciousSuccessCount}/3");
            }
            else
            {
                unconsciousFailureCount++;
                Debug.Log($"{GetDisplayName()} 回合体质豁免失败 ({constitutionSave} vs DC{savingThrowDc}) - 失败次数: {unconsciousFailureCount}/3");
            }

            // 检查是否达到结束条件
            if (unconsciousSuccessCount >= 3)
            {
                // 恢复
                currentHitPoints = 1;
                RemoveStatusEffect(StatusEffectType.Unconscious);
                unconsciousFailureCount = 0;
                unconsciousSuccessCount = 0;

                Debug.Log($"{GetDisplayName()} 回合豁免: 三次成功，恢复意识并获得1点血量!");
                DND_CharacterAdapter characterAdapter = GetComponent<DND_CharacterAdapter>();
                if (characterAdapter != null)
                {
                    characterAdapter.PlayIdleAnimation();
                }
                return;
            }

            if (unconsciousFailureCount >= 3)
            {
                Debug.Log($"{GetDisplayName()} 回合豁免: 三次失败，触发真正死亡");
                HandleTrueDeath();
            }
        }

        /// <summary>
        /// 处理真正的死亡（玩家角色豁免失败后）
        /// </summary>
        private void HandleTrueDeath() {
            Debug.Log($"{GetDisplayName()} 真正死亡!");

            // 播放死亡动画
            PlayDeathAnimation();

            // 启动尸体消失逻辑 - 3秒后消失
            StartCorpseDisappearance();
        }

        /// <summary>
        /// 启动尸体消失逻辑 - DOTween版本
        /// </summary>
        private void StartCorpseDisappearance() {
            // 统一为: 播放死亡动画后 3 秒销毁
            Sequence corpseSequence = DOTween.Sequence();

            // 等待 3 秒
            corpseSequence.AppendInterval(3f);

            // 回调移除并销毁
            corpseSequence.AppendCallback(() => {
                // 从战斗AI的先攻列表中移除自己
                var autoBattleAI = FindObjectOfType<HorizontalFormation.AutoBattleAI>();
                if (autoBattleAI != null)
                {
                    autoBattleAI.RemoveCharacterFromInitiative(this);
                }

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
        /// 受到伤害（公开方法）
        /// </summary>
        public void TakeDamage(int damage, DamageType damageType = DamageType.Bludgeoning) {
            // 将受伤逻辑统一到 ApplyDamageToSelf，默认非暴击
            ApplyDamageToSelf(damage, false, damageType);
        }

        /// <summary>
        /// 重载：允许传入是否为暴击
        /// </summary>
        public void TakeDamage(int damage, DamageType damageType, bool isCritical)
        {
            ApplyDamageToSelf(damage, isCritical, damageType);
        }

        /// <summary>
        /// 恢复生命值
        /// </summary>
        public void HealDamage(int amount) {
            currentHitPoints = Mathf.Min(maxHitPoints, currentHitPoints + amount);

            Debug.Log($"{GetDisplayName()} 恢复 {amount} 点生命值! 当前生命值: {currentHitPoints}/{maxHitPoints}");

            // 显示治疗数字
            ShowHealNumber(amount);

            // 新增：触发本地血量变化事件
            NotifyHealthChanged();

            // 如果恢复了意识
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

            // 如果移除昏迷，重置相关倒地数据与豁免流程
            if (type == StatusEffectType.Unconscious) {
                isProcessingSavingThrows = false;
                unconsciousFailureCount = 0;
                unconsciousSuccessCount = 0;
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
            DND_CharacterAdapter characterAdapter = GetAdapter();
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
            if (DamageDisplayManager.Instance != null)
            {
                DamageDisplayManager.Instance.ShowDamageNumber(transform, damage, isDamage);
                return;
            }

            Debug.LogWarning("CharacterStats.ShowDamageNumber: DamageDisplayManager 未找到，使用本地回退显示");
            // 回退显示（在场景 Canvas 上创建临时文本）
            LocalShowFloatingText(damage.ToString(), isDamage ? Color.red : Color.green);
        }

        /// <summary>
        /// 显示MISS
        /// </summary>
        public void ShowMiss() {
            // 使用统一的伤害显示管理器
            if (DamageDisplayManager.Instance != null)
            {
                DamageDisplayManager.Instance.ShowMiss(transform);
                return;
            }

            Debug.LogWarning("CharacterStats.ShowMiss: DamageDisplayManager 未找到，使用本地回退显示 MISS");
            LocalShowFloatingText("MISS", Color.yellow);
        }

        /// <summary>
        /// 显示治疗数字
        /// </summary>
        /// <param name="healAmount">治疗量</param>
        private void ShowHealNumber(int healAmount) {
            // 复用伤害数字显示逻辑，但以绿色显示表示治疗
            ShowDamageNumber(healAmount, false);
        }

        // 本地回退：在 Canvas 上创建临时 UI 文本，支持 Text 和 TMP_Text（若存在）
        private void LocalShowFloatingText(string text, Color color)
        {
            // 查找或回退到场景中的 Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("LocalShowFloatingText: 未找到 Canvas，无法显示伤害数字");
                return;
            }

            // 创建 UI 对象
            GameObject go = new GameObject("FloatingDamageText");
            go.transform.SetParent(canvas.transform, false);

            // 尝试使用 UnityEngine.UI.Text
            Text uiText = null;
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            uiText = textGO.AddComponent<Text>();
            uiText.text = text;
            uiText.color = color;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            uiText.raycastTarget = false;

            RectTransform rt = go.AddComponent<RectTransform>();
            RectTransform childRt = textGO.GetComponent<RectTransform>();
            childRt.sizeDelta = new Vector2(200, 50);

            // 计算屏幕坐标并设置 anchoredPosition
            Camera worldCamera = Camera.main ?? FindObjectOfType<Camera>();
            if (worldCamera == null)
            {
                Debug.LogWarning("LocalShowFloatingText: 未找到摄像机，无法计算屏幕坐标");
                Destroy(go);
                return;
            }

            Vector3 worldPos = transform.position + new Vector3(0, 2f, 0);
            Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0)
            {
                // 在摄像机后方，直接销毁
                Destroy(go);
                return;
            }

            Camera canvasCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (canvas.worldCamera ?? worldCamera);
            Vector2 uiPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPos, canvasCamera, out uiPos))
            {
                rt.anchoredPosition = uiPos;
            }

            // 使用 DOTween 做上升和淡出动画
            Sequence seq = DOTween.Sequence();
            // 确保 CanvasRenderer 的 Text 支持 DOFade (必要时可获取，但这里不直接使用变量)
             // 动画：上升 1.2 秒 并在 1.2s 后淡出
             float duration = 1.2f;
             seq.Append(childRt.DOAnchorPos(childRt.anchoredPosition + Vector2.up * 60f, duration).SetEase(Ease.OutCubic));
             seq.Insert(duration * 0.5f, uiText.DOFade(0f, duration * 0.5f));
             seq.OnComplete(() => { if (go != null) Destroy(go); });
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
                return UnityEngine.Random.Range(1, 21);
            }

            int bonus = template.GetSkillBonus(skill);
            int roll = UnityEngine.Random.Range(1, 21);
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
                return UnityEngine.Random.Range(1, 21);
            }

            int bonus = template.GetSavingThrowBonus(ability);
            int roll = UnityEngine.Random.Range(1, 21);
            int total = roll + bonus;

            Debug.Log($"{GetDisplayName()} 进行 {ability} 豁免检定: 掷骰 {roll} + 加值 {bonus} = {total}");
            return total;
        }

        /// <summary>
        /// 新增：当血量发生变化时触发的本地事件（直接订阅CharacterStats更可靠）
        /// 参数: currentHp, maxHp
        /// </summary>
        public event Action<int, int> OnHealthChanged;

        /// <summary>
        /// 触发血量变更通知
        /// </summary>
        private void NotifyHealthChanged()
        {
            try
            {
                OnHealthChanged?.Invoke(currentHitPoints, maxHitPoints);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"NotifyHealthChanged 触发异常: {ex}");
            }
        }

        /// <summary>
        /// 获取或缓存动画适配器（自节点→子节点→父节点）
        /// </summary>
        private DND_CharacterAdapter GetAdapter()
        {
            var adapter = GetComponent<DND_CharacterAdapter>();
            if (adapter == null)
            {
                adapter = GetComponentInChildren<DND_CharacterAdapter>(true);
            }
            if (adapter == null)
            {
                adapter = GetComponentInParent<DND_CharacterAdapter>();
            }
            return adapter;
        }
    }
}
