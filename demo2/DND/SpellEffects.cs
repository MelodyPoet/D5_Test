using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 处理法术视觉效果的类
/// </summary>
public class SpellEffects : MonoBehaviour
{
    // 单例实例
    public static SpellEffects Instance { get; private set; }

    [Header("投射法术预制体")]
    public GameObject arcaneBlastPrefab; // 奥术冲击预制体

    [Header("即时效果法术预制体")]
    public GameObject dodgeEffectPrefab; // 闪避效果预制体

    [Header("投射法术设置")]
    public float projectileSpeed = 10f; // 投射法术移动速度
    public float projectileDestroyDelay = 1f; // 投射法术效果销毁延迟

    [Header("即时效果法术设置")]
    public float instantEffectDuration = 6f; // 即时效果持续时间（如闪避）
    public float buffEffectHeight = 1f; // Buff效果相对角色的高度偏移

    // 法术预制体字典，用于动态加载
    private Dictionary<string, GameObject> spellPrefabs = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 添加SpellEffects标签，用于查找
            if (gameObject.tag == "Untagged")
            {
                gameObject.tag = "SpellEffects";
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 初始化法术预制体字典
        InitializeSpellPrefabs();
    }

    /// <summary>
    /// 初始化法术预制体字典
    /// </summary>
    private void InitializeSpellPrefabs()
    {
        // 只使用在Inspector中设置的预制体
        if (arcaneBlastPrefab != null)
        {
            spellPrefabs["ArcaneBlast"] = arcaneBlastPrefab;
            Debug.Log($"使用Inspector中设置的奥术冲击预制体: {arcaneBlastPrefab.name}");
        }
        else
        {
            Debug.LogWarning("Inspector中未设置奥术冲击预制体，法术效果可能无法正常显示");
        }

        // 初始化闪避效果预制体
        if (dodgeEffectPrefab != null)
        {
            spellPrefabs["DodgeEffect"] = dodgeEffectPrefab;
            Debug.Log($"使用Inspector中设置的闪避效果预制体: {dodgeEffectPrefab.name}");
        }
        else
        {
            Debug.LogWarning("Inspector中未设置闪避效果预制体，闪避效果可能无法正常显示");
        }

    }

    /// <summary>
    /// 播放奥术冲击效果
    /// </summary>
    /// <param name="caster">施法者</param>
    /// <param name="target">目标</param>
    /// <param name="damage">伤害值</param>
    /// <param name="damageType">伤害类型</param>
    /// <param name="onHitCallback">命中回调</param>
    public void PlayArcaneBlast(GameObject caster, GameObject target, int damage, DND5E.DamageType damageType, System.Action<GameObject, int, DND5E.DamageType> onHitCallback = null)
    {
        if (caster == null || target == null)
        {
            Debug.LogError("施法者或目标为空，无法播放奥术冲击效果");
            return;
        }

        // 获取奥术冲击预制体
        GameObject prefab = GetSpellPrefab("ArcaneBlast");
        if (prefab == null)
        {
            Debug.LogWarning("奥术冲击预制体不存在，请在Inspector中设置arcaneBlastPrefab");

            // 直接应用伤害，不播放特效（回退机制）
            CharacterStats targetStats = target.GetComponent<CharacterStats>();
            if (targetStats != null)
            {
                targetStats.TakeDamage(damage, damageType);
                Debug.Log($"奥术冲击命中 {targetStats.characterName}，造成 {damage} 点 {damageType} 伤害!");

                // 播放受击动画
                StartCoroutine(PlayFallbackHitEffects(target, targetStats));
            }
            return;
        }

        // 获取施法者和目标的信息用于调试
        string casterName = caster.GetComponent<CharacterStats>()?.GetDisplayName() ?? caster.name;
        string targetName = target.GetComponent<CharacterStats>()?.GetDisplayName() ?? target.name;

        Debug.Log($"【法术特效】{casterName} 对 {targetName} 施放奥术冲击");
        Debug.Log($"【法术特效】施法者位置: {caster.transform.position}");
        Debug.Log($"【法术特效】目标位置: {target.transform.position}");

        // 计算施法者和目标之间的方向
        Vector3 direction = (target.transform.position - caster.transform.position).normalized;

        // 计算施法起始位置（从施法者稍微偏移一点，避免从施法者内部发射）
        Vector3 spawnPosition = caster.transform.position + direction * 1.0f;

        // 调整高度，使法术效果在角色中心位置
        spawnPosition.y = caster.transform.position.y + 1.0f; // 假设角色高度约为2单位，取中点

        Debug.Log($"【法术特效】特效生成位置: {spawnPosition}");

        // 计算旋转角度，使法术效果朝向目标
        Quaternion rotation = Quaternion.LookRotation(direction);

        // 实例化法术效果
        GameObject spellEffect = Instantiate(prefab, spawnPosition, rotation);

        // 启动协程，移动法术效果并处理命中
        StartCoroutine(MoveSpellEffect(spellEffect, target, damage, damageType, onHitCallback));
    }

    /// <summary>
    /// 移动法术效果并处理命中
    /// </summary>
    private IEnumerator MoveSpellEffect(GameObject spellEffect, GameObject target, int damage, DND5E.DamageType damageType, System.Action<GameObject, int, DND5E.DamageType> onHitCallback)
    {
        if (spellEffect == null || target == null)
            yield break;

        // 获取目标位置
        Vector3 targetPosition = target.transform.position;

        // 调整高度，使法术效果命中角色中心位置
        targetPosition.y = target.transform.position.y + 1.0f; // 假设角色高度约为2单位，取中点

        // 计算距离
        float distance = Vector3.Distance(spellEffect.transform.position, targetPosition);

        // 计算移动时间
        float moveTime = distance / projectileSpeed;

        // 记录开始时间和位置
        float startTime = Time.time;
        Vector3 startPosition = spellEffect.transform.position;

        // 移动法术效果
        while (Time.time - startTime < moveTime)
        {
            // 如果目标或法术效果被销毁，退出循环
            if (spellEffect == null || target == null)
                yield break;

            // 更新目标位置（如果目标在移动）
            targetPosition = target.transform.position;
            targetPosition.y = target.transform.position.y + 1.0f;

            // 计算当前时间比例
            float t = (Time.time - startTime) / moveTime;

            // 使用线性插值移动法术效果
            spellEffect.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            // 更新朝向
            if (targetPosition != spellEffect.transform.position)
            {
                spellEffect.transform.rotation = Quaternion.LookRotation(targetPosition - spellEffect.transform.position);
            }

            yield return null;
        }

        // 确保法术效果到达目标位置
        if (spellEffect != null)
        {
            spellEffect.transform.position = targetPosition;
        }

        // 处理命中效果
        if (target != null)
        {
            // 播放命中粒子效果（如果有）
            ParticleSystem[] particleSystems = spellEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                // 停止发射新粒子
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                // 可以在这里添加命中时的特殊粒子效果
                // 例如：ps.Play();
            }

            // 调用命中回调
            onHitCallback?.Invoke(target, damage, damageType);

            // 获取目标的CharacterStats组件和动画控制器
            CharacterStats targetStats = target.GetComponent<CharacterStats>();
            DND_CharacterAdapter targetAdapter = target.GetComponent<DND_CharacterAdapter>();
            AnimationController targetAnimController = null;

            // 如果没有DND_CharacterAdapter，尝试获取AnimationController
            if (targetAdapter == null)
            {
                targetAnimController = target.GetComponent<AnimationController>();
            }

            // 应用伤害
            if (targetStats != null)
            {
                targetStats.TakeDamage(damage, damageType);
                Debug.Log($"奥术冲击命中 {targetStats.characterName}，造成 {damage} 点 {damageType} 伤害!");
            }

            // 等待一小段时间，确保有时间准备播放受击动画
            yield return new WaitForSeconds(0.1f);

            // 播放目标受击动画
            if (targetAdapter != null)
            {
                targetAdapter.TakeDamage();
                Debug.Log($"播放 {target.name} 的受击动画");
            }
            else if (targetAnimController != null)
            {
                targetAnimController.PlayHit();
                Debug.Log($"使用AnimationController播放目标受击动画: {targetAnimController.hitAnimation}");
            }

            // 确保UI更新
            if (DND_BattleUI.Instance != null && targetStats != null)
            {
                DND_BattleUI.Instance.UpdateCharacterStatusUI(targetStats);
            }

            // 检查目标是否死亡
            if (targetStats != null && targetStats.currentHitPoints <= 0)
            {
                yield return new WaitForSeconds(0.5f); // 给受击动画更多时间

                if (targetAdapter != null)
                {
                    // 使用DND_CharacterAdapter播放死亡动画
                    targetAdapter.PlayDeathAnimation();
                    Debug.Log($"使用DND_CharacterAdapter播放目标死亡动画: {targetAdapter.animationMapping.deathAnimation}");
                }
                else if (targetAnimController != null)
                {
                    // 如果没有DND_CharacterAdapter，使用AnimationController
                    targetAnimController.PlayDeath();
                    Debug.Log($"使用AnimationController播放目标死亡动画: {targetAnimController.deathAnimation}");
                }
            }
        }

        // 延迟销毁投射法术效果
        if (spellEffect != null)
        {
            Destroy(spellEffect, projectileDestroyDelay);
        }
    }

    /// <summary>
    /// 获取法术预制体
    /// </summary>
    /// <param name="spellName">法术名称</param>
    /// <returns>法术预制体</returns>
    private GameObject GetSpellPrefab(string spellName)
    {
        // 检查字典中是否有该法术预制体
        if (spellPrefabs.TryGetValue(spellName, out GameObject prefab) && prefab != null)
        {
            Debug.Log($"从字典中获取到法术 {spellName} 的预制体");
            return prefab;
        }

        // 如果是奥术冲击，直接使用arcaneBlastPrefab
        if (spellName == "ArcaneBlast" && arcaneBlastPrefab != null)
        {
            Debug.Log("使用arcaneBlastPrefab作为奥术冲击预制体");
            spellPrefabs[spellName] = arcaneBlastPrefab;
            return arcaneBlastPrefab;
        }

        // 如果是闪避效果，直接使用dodgeEffectPrefab
        if (spellName == "DodgeEffect" && dodgeEffectPrefab != null)
        {
            Debug.Log("使用dodgeEffectPrefab作为闪避效果预制体");
            spellPrefabs[spellName] = dodgeEffectPrefab;
            return dodgeEffectPrefab;
        }

        Debug.LogWarning($"无法找到法术 {spellName} 的预制体，请确保在Inspector中设置了对应的预制体");
        return null;
    }

    /// <summary>
    /// 处理SendMessage调用的PlayArcaneBlast方法（接收SpellData参数）
    /// </summary>
    /// <param name="spellData">法术数据</param>
    public void PlayArcaneBlastWrapper(SpellData spellData)
    {
        if (spellData == null)
        {
            Debug.LogError("PlayArcaneBlastWrapper: spellData为null");
            return;
        }

        try
        {
            // 调用原始方法
            PlayArcaneBlast(spellData.caster, spellData.target, spellData.damage, spellData.damageType, null);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PlayArcaneBlastWrapper: 调用PlayArcaneBlast方法时出错: {e.Message}");
        }
    }

    /// <summary>
    /// 播放闪避效果（即时效果法术）
    /// </summary>
    /// <param name="character">使用闪避的角色</param>
    /// <param name="duration">效果持续时间（秒），如果为0则使用默认设置</param>
    public void PlayDodgeEffect(GameObject character, float duration = 0f)
    {
        if (character == null)
        {
            Debug.LogError("【闪避特效】角色为空，无法播放闪避效果");
            return;
        }

        // 如果没有指定持续时间，使用默认设置
        if (duration <= 0f)
        {
            duration = instantEffectDuration;
        }

        string characterName = character.GetComponent<CharacterStats>()?.GetDisplayName() ?? character.name;
        Debug.Log($"【防御姿态特效】开始为 {characterName} 播放防御姿态效果，持续时间: {duration}秒");
        Debug.Log($"【防御姿态特效】角色位置: {character.transform.position}");

        // 获取防御姿态效果预制体
        GameObject prefab = GetSpellPrefab("DodgeEffect");
        Debug.Log($"【防御姿态特效】获取DodgeEffect预制体: {(prefab != null ? prefab.name : "null")}");

        if (prefab == null)
        {
            Debug.LogError("【防御姿态特效】防御姿态效果预制体不存在，无法播放效果");
            Debug.LogError("【防御姿态特效】请检查SpellEffects组件的Inspector面板，确保设置了dodgeEffectPrefab");

            // 检查dodgeEffectPrefab字段
            Debug.Log($"【防御姿态特效】dodgeEffectPrefab字段值: {(dodgeEffectPrefab != null ? dodgeEffectPrefab.name : "null")}");

            return;
        }

        // 计算即时效果生成位置（使用配置的高度偏移）
        Vector3 spawnPosition = character.transform.position;
        spawnPosition.y = character.transform.position.y + buffEffectHeight;
        Debug.Log($"【防御姿态特效】特效生成位置: {spawnPosition}");

        // 实例化防御姿态效果
        try
        {
            GameObject dodgeEffect = Instantiate(prefab, spawnPosition, Quaternion.identity);

            // 将效果附加到角色上，跟随角色移动
            dodgeEffect.transform.SetParent(character.transform, true);
            Debug.Log($"【防御姿态特效】特效已附加到角色 {characterName}");

            // 添加防御姿态状态效果
            CharacterStats characterStats = character.GetComponent<CharacterStats>();
            if (characterStats != null)
            {
                characterStats.AddStatusEffect(DND5E.StatusEffectType.Dodging);
                Debug.Log($"【防御姿态特效】{characterStats.GetDisplayName()} 进入防御姿态，AC+2，持续到下回合开始");
            }
            else
            {
                Debug.LogWarning($"【防御姿态特效】角色 {characterName} 没有CharacterStats组件，无法添加状态效果");
            }

            // 设置粒子系统的持续时间
            ParticleSystem[] particleSystems = dodgeEffect.GetComponentsInChildren<ParticleSystem>();
            Debug.Log($"【防御姿态特效】找到 {particleSystems.Length} 个粒子系统");

            foreach (ParticleSystem ps in particleSystems)
            {
                // 设置粒子系统的持续时间
                var main = ps.main;
                main.duration = duration;

                // 播放粒子效果
                ps.Play();
                Debug.Log($"【防御姿态特效】启动粒子系统: {ps.name}");
            }

            // 延迟销毁防御姿态效果
            Destroy(dodgeEffect, duration);
            Debug.Log($"【防御姿态特效】防御姿态效果将在 {duration} 秒后销毁");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"【防御姿态特效】实例化防御姿态效果时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 处理SendMessage调用的PlayDodgeEffect方法
    /// </summary>
    /// <param name="character">使用闪避的角色</param>
    public void PlayDodgeEffectWrapper(GameObject character)
    {
        if (character == null)
        {
            Debug.LogError("PlayDodgeEffectWrapper: character为null");
            return;
        }

        try
        {
            // 调用原始方法，使用默认持续时间设置
            PlayDodgeEffect(character);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PlayDodgeEffectWrapper: 调用PlayDodgeEffect方法时出错: {e.Message}");
        }
    }

    /// <summary>
    /// 播放即时效果法术（通用方法，用于Buff、治疗等）
    /// </summary>
    /// <param name="character">目标角色</param>
    /// <param name="effectName">效果名称（用于查找预制体）</param>
    /// <param name="duration">效果持续时间，如果为0则使用默认设置</param>
    /// <param name="statusEffect">要添加的状态效果（可选）</param>
    public void PlayInstantEffect(GameObject character, string effectName, float duration = 0f, DND5E.StatusEffectType? statusEffect = null)
    {
        if (character == null)
        {
            Debug.LogError($"【即时效果】角色为空，无法播放{effectName}效果");
            return;
        }

        // 如果没有指定持续时间，使用默认设置
        if (duration <= 0f)
        {
            duration = instantEffectDuration;
        }

        string characterName = character.GetComponent<CharacterStats>()?.GetDisplayName() ?? character.name;
        Debug.Log($"【即时效果】开始为 {characterName} 播放{effectName}效果，持续时间: {duration}秒");

        // 获取效果预制体
        GameObject prefab = GetSpellPrefab(effectName);
        if (prefab == null)
        {
            Debug.LogError($"【即时效果】{effectName}效果预制体不存在，无法播放效果");
            return;
        }

        // 计算即时效果生成位置
        Vector3 spawnPosition = character.transform.position;
        spawnPosition.y = character.transform.position.y + buffEffectHeight;

        try
        {
            // 实例化效果
            GameObject effect = Instantiate(prefab, spawnPosition, Quaternion.identity);

            // 将效果附加到角色上
            effect.transform.SetParent(character.transform, true);

            // 添加状态效果（如果指定）
            if (statusEffect.HasValue)
            {
                CharacterStats characterStats = character.GetComponent<CharacterStats>();
                if (characterStats != null)
                {
                    characterStats.AddStatusEffect(statusEffect.Value);
                    Debug.Log($"【即时效果】{characterStats.GetDisplayName()} 获得{statusEffect.Value}状态效果");
                }
            }

            // 设置粒子系统的持续时间
            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                var main = ps.main;
                main.duration = duration;
                ps.Play();
            }

            // 延迟销毁效果
            Destroy(effect, duration);
            Debug.Log($"【即时效果】{effectName}效果将在 {duration} 秒后销毁");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"【即时效果】实例化{effectName}效果时出错: {e.Message}");
        }
    }

    /// <summary>
    /// 回退机制：播放受击动画（当没有特效预制体时）
    /// </summary>
    private IEnumerator PlayFallbackHitEffects(GameObject target, CharacterStats targetStats)
    {
        // 等待一小段时间，确保有时间准备播放受击动画
        yield return new WaitForSeconds(0.1f);

        // 获取动画组件
        DND_CharacterAdapter targetAdapter = target.GetComponent<DND_CharacterAdapter>();
        AnimationController targetAnimController = target.GetComponent<AnimationController>();

        // 播放受击动画
        if (targetAdapter != null)
        {
            targetAdapter.TakeDamage();
            Debug.Log($"播放 {target.name} 的受击动画（回退机制）");
        }
        else if (targetAnimController != null)
        {
            targetAnimController.PlayHit();
            Debug.Log($"使用AnimationController播放目标受击动画（回退机制）: {targetAnimController.hitAnimation}");
        }

        // 确保UI更新
        if (DND_BattleUI.Instance != null && targetStats != null)
        {
            DND_BattleUI.Instance.UpdateCharacterStatusUI(targetStats);
        }
    }
}
