using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DND5E;
using System;

public class RangeManager : MonoBehaviour
{
    // 单例实例
    public static RangeManager Instance { get; private set; }

    [Header("预制体设置")]
    [Tooltip("范围指示器预制件 - 用于显示攻击和移动范围的圆形指示器")]
    public GameObject rangeIndicatorPrefab;
    
    [Tooltip("目标指示器预制件 - 用于高亮显示有效攻击目标")]
    public GameObject targetIndicatorPrefab;

    [Header("范围设置")]
    [Tooltip("Unity单位到英尺的转换比例 (1单位 = 5尺时设为0.2)")]
    public float unitToFeetRatio = 0.2f; // 1单位 = 5尺 (0.2 = 1/5)
    
    [SerializeField, Tooltip("近战攻击范围（英尺）")]
    private float _meleeRange = 15f;       // 近战范围（尺），增加到15尺使游戏体验更好
    
    [SerializeField, Tooltip("默认远程攻击范围（英尺）")]
    private float _defaultRangedRange = 30f; // 默认远程范围（尺），修改为30尺与法术范围一致
    
    [SerializeField, Tooltip("默认法术攻击范围（英尺）")]
    private float _defaultSpellRange = 30f;  // 默认法术范围（尺）

    // 属性访问器
    public float meleeRange { get { return _meleeRange; } private set { _meleeRange = value; } }
    public float defaultRangedRange { get { return _defaultRangedRange; } private set { _defaultRangedRange = value; } }
    public float defaultSpellRange { get { return _defaultSpellRange; } private set { _defaultSpellRange = value; } }

    // 范围变更事件
    public event Action<float> OnMeleeRangeChanged;
    public event Action<float> OnRangedRangeChanged;
    public event Action<float> OnSpellRangeChanged;

    // 当前显示的范围指示器
    private GameObject currentRangeIndicator;
    private List<GameObject> targetIndicators = new List<GameObject>();

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 显示移动范围，返回创建的范围指示器对象
    public GameObject ShowMovementRange(CharacterStats character, bool isDash = false)
    {
        ClearRangeIndicator();

        // 获取ActionSystem组件
        ActionSystem actionSystem = character.GetComponent<ActionSystem>();
        if (actionSystem == null) return null;

        // 获取剩余移动距离（尺）
        float movementRange = actionSystem.movementRemaining;

        // 计算用于显示的范围大小（保持冲刺和普通移动的指示器大小一致）
        float displayRange = isDash ? actionSystem.movementSpeed : movementRange;

        // 转换为Unity单位
        float rangeInUnits = displayRange * unitToFeetRatio;

        // 创建范围指示器
        if (rangeIndicatorPrefab != null)
        {
            currentRangeIndicator = Instantiate(rangeIndicatorPrefab, character.transform.position, Quaternion.identity);

            // 设置范围大小
            RangeIndicator indicator = currentRangeIndicator.GetComponent<RangeIndicator>();
            if (indicator != null)
            {
                // 使用displayRange计算，确保冲刺和普通移动的指示器大小一致
                indicator.SetSize(rangeInUnits*2);
                indicator.SetRangeType(RangeIndicator.RangeType.Movement);
            }

            // 返回创建的指示器对象
            return currentRangeIndicator;
        }

        return null;
    }

    // 显示攻击范围
    public void ShowAttackRange(CharacterStats character, AttackType attackType)
    {
        ClearRangeIndicator();

        // 根据攻击类型确定范围（尺）
        float rangeInFeet = 0;
        switch (attackType)
        {
            case AttackType.Melee:
                rangeInFeet = meleeRange;
                break;
            case AttackType.Ranged:
                rangeInFeet = defaultRangedRange;
                break;
            case AttackType.Spell:
                rangeInFeet = defaultSpellRange;
                break;
        }

        // 转换为Unity单位
        float rangeInUnits = rangeInFeet * unitToFeetRatio;

        // 创建范围指示器
        if (rangeIndicatorPrefab != null)
        {
            currentRangeIndicator = Instantiate(rangeIndicatorPrefab, character.transform.position, Quaternion.identity);

            // 设置范围大小
            RangeIndicator indicator = currentRangeIndicator.GetComponent<RangeIndicator>();
            if (indicator != null)
            {
                // 设置指示器大小为直径（半径的两倍）
                indicator.SetSize(rangeInUnits * 2);
                indicator.SetRangeType(RangeIndicator.RangeType.Attack);
            }
        }

        // 高亮显示在范围内的有效目标
        HighlightValidTargets(character, attackType, rangeInFeet);
    }

    // 高亮显示有效目标
    private void HighlightValidTargets(CharacterStats attacker, AttackType attackType, float rangeInFeet)
    {
        ClearTargetIndicators();

        // 获取所有角色
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();

        // 确定目标标签（玩家攻击敌人，敌人攻击玩家）
        string targetTag = attacker.gameObject.CompareTag("Player") ? "Enemy" : "Player";

        foreach (var character in allCharacters)
        {
            // 跳过自己和不符合目标标签的角色
            if (character == attacker || !character.gameObject.CompareTag(targetTag)) continue;

            // 跳过已经失去意识的角色
            if (character.HasStatusEffect(StatusEffectType.Unconscious)) continue;

            // 计算距离（Unity单位）
            float distance = Vector3.Distance(attacker.transform.position, character.transform.position);

            // 转换为尺
            float distanceInFeet = distance / unitToFeetRatio;

            // 检查是否在范围内
            if (distanceInFeet <= rangeInFeet)
            {
                // 创建目标指示器
                if (targetIndicatorPrefab != null)
                {
                    GameObject indicator = Instantiate(targetIndicatorPrefab, character.transform.position, Quaternion.identity);
                    targetIndicators.Add(indicator);

                    // 设置为有效目标颜色
                    RangeIndicator rangeIndicator = indicator.GetComponent<RangeIndicator>();
                    if (rangeIndicator != null)
                    {
                        rangeIndicator.SetRangeType(RangeIndicator.RangeType.ValidTarget);

                        // 设置大小略大于角色
                        rangeIndicator.SetSize(1.2f);

                        // 设置目标指示器的排序顺序，确保可见但不会挡住角色和怪物的高亮效果
                        SpriteRenderer spriteRenderer = indicator.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null)
                        {
                            // 使用较小的负数，确保在角色/怪物后面但仍然可见
                            spriteRenderer.sortingOrder = -2;
                        }
                    }
                }
            }
        }
    }

    // 高亮显示冲刺范围内的敌人
    public void HighlightEnemiesInDashRange(CharacterStats attacker)
    {
        ClearTargetIndicators();

        // 获取ActionSystem组件
        ActionSystem actionSystem = attacker.GetComponent<ActionSystem>();
        if (actionSystem == null) return;

        // 冲刺范围是移动速度的2倍（实际游戏逻辑范围）
        float dashRangeInFeet = actionSystem.movementSpeed * 2;

        // 获取所有角色
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();

        // 确定目标标签（玩家攻击敌人，敌人攻击玩家）
        string targetTag = attacker.gameObject.CompareTag("Player") ? "Enemy" : "Player";

        foreach (var character in allCharacters)
        {
            // 跳过自己和不符合目标标签的角色
            if (character == attacker || !character.gameObject.CompareTag(targetTag)) continue;

            // 跳过已经失去意识的角色
            if (character.HasStatusEffect(StatusEffectType.Unconscious)) continue;

            // 计算距离（Unity单位）
            float distance = Vector3.Distance(attacker.transform.position, character.transform.position);

            // 转换为尺
            float distanceInFeet = distance / unitToFeetRatio;

            // 检查是否在冲刺范围内但大于武器触及范围
            if (distanceInFeet <= dashRangeInFeet && distanceInFeet > meleeRange)
            {
                // 创建目标指示器
                if (targetIndicatorPrefab != null)
                {
                    GameObject indicator = Instantiate(targetIndicatorPrefab, character.transform.position, Quaternion.identity);
                    targetIndicators.Add(indicator);

                    // 设置为有效目标颜色
                    RangeIndicator rangeIndicator = indicator.GetComponent<RangeIndicator>();
                    if (rangeIndicator != null)
                    {
                        rangeIndicator.SetRangeType(RangeIndicator.RangeType.ValidTarget);

                        // 设置大小略大于角色
                        rangeIndicator.SetSize(1.2f);

                        // 设置目标指示器的排序顺序，确保可见但不会挡住角色和怪物的高亮效果
                        SpriteRenderer spriteRenderer = indicator.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null)
                        {
                            // 使用较小的负数，确保在角色/怪物后面但仍然可见
                            spriteRenderer.sortingOrder = -2;
                        }
                    }
                }
            }
        }
    }

    // 清除范围指示器
    public void ClearRangeIndicator()
    {
        if (currentRangeIndicator != null)
        {
            Destroy(currentRangeIndicator);
            currentRangeIndicator = null;
        }
    }

    // 清除目标指示器
    public void ClearTargetIndicators()
    {
        foreach (var indicator in targetIndicators)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
        targetIndicators.Clear();
    }

    // 清除所有指示器
    public void ClearAllIndicators()
    {
        ClearRangeIndicator();
        ClearTargetIndicators();
    }

    // 检查两个角色之间的距离是否在指定范围内
    public bool IsInRange(CharacterStats character1, CharacterStats character2, float rangeInFeet)
    {
        // 计算距离（Unity单位）
        float distance = Vector3.Distance(character1.transform.position, character2.transform.position);

        // 转换为尺
        float distanceInFeet = distance / unitToFeetRatio;

        return distanceInFeet <= rangeInFeet;
    }

    // 获取两个角色之间的距离（尺）
    public float GetDistanceInFeet(CharacterStats character1, CharacterStats character2)
    {
        // 计算距离（Unity单位）
        float distance = Vector3.Distance(character1.transform.position, character2.transform.position);

        // 转换为尺
        return distance / unitToFeetRatio;
    }

    // 设置近战范围
    public void SetMeleeRange(float newRange)
    {
        if (newRange <= 0) return;

        // 如果值没有变化，不做任何处理
        if (Mathf.Approximately(meleeRange, newRange)) return;

        // 更新值
        meleeRange = newRange;

        // 触发事件
        OnMeleeRangeChanged?.Invoke(newRange);

        // 清除所有范围指示器，确保下次显示时使用新的范围值
        ClearAllIndicators();

        Debug.Log($"近战范围已更新为: {newRange:F1} 尺");

        // 如果战斗正在进行，通知战斗管理器范围已更新
        if (CombatManager.Instance != null && CombatManager.Instance.isCombatActive)
        {
            Debug.Log("战斗正在进行，通知战斗管理器范围已更新");
            // 这里可以添加更多的通知逻辑，例如刷新UI等
        }
    }

    // 设置远程范围
    public void SetRangedRange(float newRange)
    {
        if (newRange <= 0) return;

        // 如果值没有变化，不做任何处理
        if (Mathf.Approximately(defaultRangedRange, newRange)) return;

        // 更新值
        defaultRangedRange = newRange;

        // 触发事件
        OnRangedRangeChanged?.Invoke(newRange);

        // 清除所有范围指示器，确保下次显示时使用新的范围值
        ClearAllIndicators();

        Debug.Log($"远程范围已更新为: {newRange:F1} 尺");
    }

    // 设置法术范围
    public void SetSpellRange(float newRange)
    {
        if (newRange <= 0) return;

        // 如果值没有变化，不做任何处理
        if (Mathf.Approximately(defaultSpellRange, newRange)) return;

        // 更新值
        defaultSpellRange = newRange;

        // 触发事件
        OnSpellRangeChanged?.Invoke(newRange);

        // 清除所有范围指示器，确保下次显示时使用新的范围值
        ClearAllIndicators();

        Debug.Log($"法术范围已更新为: {newRange:F1} 尺");
    }
}
