using System.Collections;
using UnityEngine;
using DND5E;

public class SimpleEnemyAI : MonoBehaviour
{
    // 战斗管理器引用
    private CombatManager combatManager;

    // AI决策延迟（秒）
    public float decisionDelay = 1.0f;
    public float actionDelay = 0.5f;

    private void Start()
    {
        // 获取战斗管理器
        combatManager = FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            // 注册回合开始事件
            combatManager.OnTurnStart += OnTurnStart;
        }
    }

    // 回合开始事件处理
    private void OnTurnStart(CharacterStats character)
    {
        // 如果是敌人角色，执行AI决策
        if (character != null && character.gameObject.CompareTag("Enemy"))
        {
            StartCoroutine(ExecuteAITurn(character));
        }
    }

    // 执行AI回合
    private IEnumerator ExecuteAITurn(CharacterStats enemy)
    {
        // 等待一段时间，让玩家看清楚是谁的回合
        yield return new WaitForSeconds(decisionDelay);

        // 获取行动系统
        if (!enemy.TryGetComponent(out ActionSystem actionSystem))
        {
            Debug.LogWarning($"{enemy.characterName} 没有ActionSystem组件!");
            combatManager.EndCurrentTurn();
            yield break;
        }

        // 决策：寻找最近的玩家角色作为目标
        CharacterStats target = FindNearestPlayerCharacter(enemy);
        if (target == null)
        {
            Debug.LogWarning("找不到有效的玩家目标!");
            combatManager.EndCurrentTurn();
            yield break;
        }

        // 计算与目标的距离
        float distanceToTarget = RangeManager.Instance.GetDistanceInFeet(enemy, target);
        Debug.Log($"{enemy.characterName} 与目标 {target.characterName} 的距离为 {distanceToTarget:F1} 尺");

        // 根据敌人职业确定其战斗策略
        switch (enemy.characterClass)
        {
            case CharacterClass.Fighter:
            case CharacterClass.Barbarian:
            case CharacterClass.Paladin:
                // 近战职业：优先移动接近目标，然后进行近战攻击
                yield return StartCoroutine(ExecuteMeleeStrategy(enemy, target, actionSystem));
                break;

            case CharacterClass.Ranger:
            case CharacterClass.Rogue:
                // 远程/混合职业：根据距离决定是远程攻击还是近战攻击
                yield return StartCoroutine(ExecuteRangedStrategy(enemy, target, actionSystem));
                break;

            case CharacterClass.Wizard:
            case CharacterClass.Sorcerer:
            case CharacterClass.Warlock:
            case CharacterClass.Druid:
            case CharacterClass.Cleric:
            case CharacterClass.Bard:
                // 施法职业：优先使用法术，然后考虑其他选项
                yield return StartCoroutine(ExecuteSpellcasterStrategy(enemy, target, actionSystem));
                break;

            default:
                // 默认使用近战策略
                yield return StartCoroutine(ExecuteMeleeStrategy(enemy, target, actionSystem));
                break;
        }

        // 等待一段时间，让玩家看清楚敌人的行动
        yield return new WaitForSeconds(actionDelay);

        // 结束回合
        combatManager.EndCurrentTurn();
    }

    // 近战职业的战斗策略
    private IEnumerator ExecuteMeleeStrategy(CharacterStats enemy, CharacterStats target, ActionSystem actionSystem)
    {
        // 计算与目标的距离
        float distanceToTarget = RangeManager.Instance.GetDistanceInFeet(enemy, target);

        // 如果不在近战范围内，先移动接近目标
        if (distanceToTarget > RangeManager.Instance.meleeRange && actionSystem.hasMovement)
        {
            Debug.Log($"{enemy.characterName} 决定移动接近 {target.characterName}");

            // 计算移动目标点（考虑Y轴方向）
            Vector3 directionToTarget = (target.transform.position - enemy.transform.position).normalized;

            // 计算理想的移动距离（尺）
            // 目标是移动到近战范围边缘，但不超过可用移动距离
            float idealDistanceInFeet = distanceToTarget - RangeManager.Instance.meleeRange * 0.9f; // 留一点余量

            // 确保不超过可用移动距离
            float actualMoveDistanceInFeet = Mathf.Min(idealDistanceInFeet, actionSystem.movementRemaining);

            // 转换为Unity单位
            float moveDistanceInUnits = actualMoveDistanceInFeet * RangeManager.Instance.unitToFeetRatio;

            // 计算目标位置（保留Y轴方向）
            Vector3 destination = enemy.transform.position + directionToTarget * moveDistanceInUnits;

            // 确保目标位置在允许的行走区域内
            destination = LevelData.GetValidDestination(enemy.transform.position, destination);

            Debug.Log($"{enemy.characterName} 计划移动 {actualMoveDistanceInFeet:F1} 尺接近目标，剩余移动距离: {actionSystem.movementRemaining} 尺");

            // 执行移动
            yield return StartCoroutine(combatManager.ExecuteMovement(enemy, destination));

            // 重新计算距离
            distanceToTarget = RangeManager.Instance.GetDistanceInFeet(enemy, target);
        }

        // 如果在近战范围内且还有动作，进行近战攻击
        if (distanceToTarget <= RangeManager.Instance.meleeRange && actionSystem.hasAction)
        {
            Debug.Log($"{enemy.characterName} 决定近战攻击 {target.characterName}");

            // 执行攻击
            yield return StartCoroutine(combatManager.ExecuteAttack(
                enemy, target, AttackType.Melee, "str", "1d8", DamageType.Slashing));
        }
        // 如果不在近战范围内且还有移动动作，尝试移动
        else if (distanceToTarget > RangeManager.Instance.meleeRange && actionSystem.hasMovement)
        {
            Debug.Log($"{enemy.characterName} 决定移动接近 {target.characterName}，但无法在本回合内到达攻击范围");

            // 计算移动目标点（考虑Y轴方向）
            Vector3 directionToTarget = (target.transform.position - enemy.transform.position).normalized;

            // 使用全部可用移动距离
            float moveDistanceInUnits = actionSystem.movementRemaining * RangeManager.Instance.unitToFeetRatio;

            // 计算目标位置（保留Y轴方向）
            Vector3 destination = enemy.transform.position + directionToTarget * moveDistanceInUnits;

            // 确保目标位置在允许的行走区域内
            destination = LevelData.GetValidDestination(enemy.transform.position, destination);

            Debug.Log($"{enemy.characterName} 计划移动 {actionSystem.movementRemaining:F1} 尺接近目标");

            // 执行移动
            yield return StartCoroutine(combatManager.ExecuteMovement(enemy, destination));
        }
    }

    // 远程/混合职业的战斗策略
    private IEnumerator ExecuteRangedStrategy(CharacterStats enemy, CharacterStats target, ActionSystem actionSystem)
    {
        // 计算与目标的距离
        float distanceToTarget = RangeManager.Instance.GetDistanceInFeet(enemy, target);

        // 如果在远程攻击范围内且不在近战范围内，进行远程攻击
        if (distanceToTarget <= RangeManager.Instance.defaultRangedRange &&
            distanceToTarget > RangeManager.Instance.meleeRange &&
            actionSystem.hasAction)
        {
            Debug.Log($"{enemy.characterName} 决定远程攻击 {target.characterName}");

            // 执行远程攻击
            yield return StartCoroutine(combatManager.ExecuteAttack(
                enemy, target, AttackType.Ranged, "dex", "1d6", DamageType.Piercing));
        }
        // 如果在近战范围内，进行近战攻击
        else if (distanceToTarget <= RangeManager.Instance.meleeRange && actionSystem.hasAction)
        {
            Debug.Log($"{enemy.characterName} 决定近战攻击 {target.characterName}");

            // 执行攻击
            yield return StartCoroutine(combatManager.ExecuteAttack(
                enemy, target, AttackType.Melee, "str", "1d8", DamageType.Slashing));
        }
        // 如果不在任何攻击范围内，移动接近目标
        else if (actionSystem.hasMovement)
        {
            Debug.Log($"{enemy.characterName} 决定移动接近 {target.characterName}");

            // 计算移动目标点（考虑Y轴方向）
            Vector3 directionToTarget = (target.transform.position - enemy.transform.position).normalized;

            // 计算理想的移动距离（尺）
            // 目标是移动到远程攻击的理想距离（比近战范围稍远一点）
            float idealDistance = RangeManager.Instance.meleeRange * 2f;
            float idealDistanceInFeet = distanceToTarget - idealDistance;

            // 确保不超过可用移动距离
            float actualMoveDistanceInFeet = Mathf.Min(idealDistanceInFeet, actionSystem.movementRemaining);

            // 转换为Unity单位
            float moveDistanceInUnits = actualMoveDistanceInFeet * RangeManager.Instance.unitToFeetRatio;

            // 计算目标位置（保留Y轴方向）
            Vector3 destination = enemy.transform.position + directionToTarget * moveDistanceInUnits;

            // 确保目标位置在允许的行走区域内
            destination = LevelData.GetValidDestination(enemy.transform.position, destination);

            Debug.Log($"{enemy.characterName} 计划移动 {actualMoveDistanceInFeet:F1} 尺接近目标，剩余移动距离: {actionSystem.movementRemaining} 尺");

            // 执行移动
            yield return StartCoroutine(combatManager.ExecuteMovement(enemy, destination));

            // 移动后如果在攻击范围内且还有动作，进行攻击
            if (actionSystem.hasAction)
            {
                // 重新计算距离
                distanceToTarget = RangeManager.Instance.GetDistanceInFeet(enemy, target);

                if (distanceToTarget <= RangeManager.Instance.defaultRangedRange)
                {
                    Debug.Log($"{enemy.characterName} 移动后决定远程攻击 {target.characterName}");

                    // 执行远程攻击
                    yield return StartCoroutine(combatManager.ExecuteAttack(
                        enemy, target, AttackType.Ranged, "dex", "1d6", DamageType.Piercing));
                }
            }
        }
    }

    // 施法职业的战斗策略
    private IEnumerator ExecuteSpellcasterStrategy(CharacterStats enemy, CharacterStats target, ActionSystem actionSystem)
    {
        // 获取法术系统
        enemy.TryGetComponent(out SpellSystem spellSystem);

        // 计算与目标的距离
        float distanceToTarget = RangeManager.Instance.GetDistanceInFeet(enemy, target);

        // 如果有法术系统且有法术，尝试施法
        if (spellSystem != null && spellSystem.spellList.knownSpells.Count > 0 && actionSystem.hasAction)
        {
            // 随机选择一个法术
            Spell spell = spellSystem.spellList.knownSpells[Random.Range(0, spellSystem.spellList.knownSpells.Count)];

            // 检查法术范围
            if (distanceToTarget <= RangeManager.Instance.defaultSpellRange)
            {
                Debug.Log($"{enemy.characterName} 决定对 {target.characterName} 施放 {spell.name}");
                spellSystem.CastSpell(spell, target.gameObject);

                // 等待法术效果
                yield return new WaitForSeconds(actionDelay);
            }
            // 如果不在法术范围内，移动接近目标
            else if (actionSystem.hasMovement)
            {
                Debug.Log($"{enemy.characterName} 决定移动接近 {target.characterName} 以施法");

                // 计算移动目标点（考虑Y轴方向）
                Vector3 directionToTarget = (target.transform.position - enemy.transform.position).normalized;

                // 计算理想的移动距离（尺）
                // 目标是移动到法术范围边缘，但不超过可用移动距离
                float idealDistanceInFeet = distanceToTarget - RangeManager.Instance.defaultSpellRange * 0.9f;

                // 确保不超过可用移动距离
                float actualMoveDistanceInFeet = Mathf.Min(idealDistanceInFeet, actionSystem.movementRemaining);

                // 转换为Unity单位
                float moveDistanceInUnits = actualMoveDistanceInFeet * RangeManager.Instance.unitToFeetRatio;

                // 计算目标位置（保留Y轴方向）
                Vector3 destination = enemy.transform.position + directionToTarget * moveDistanceInUnits;

                // 确保目标位置在允许的行走区域内
                destination = LevelData.GetValidDestination(enemy.transform.position, destination);

                Debug.Log($"{enemy.characterName} 计划移动 {actualMoveDistanceInFeet:F1} 尺接近目标，剩余移动距离: {actionSystem.movementRemaining} 尺");

                // 执行移动
                yield return StartCoroutine(combatManager.ExecuteMovement(enemy, destination));

                // 移动后如果在法术范围内且还有动作，施放法术
                if (actionSystem.hasAction)
                {
                    // 重新计算距离
                    distanceToTarget = RangeManager.Instance.GetDistanceInFeet(enemy, target);

                    if (distanceToTarget <= RangeManager.Instance.defaultSpellRange)
                    {
                        Debug.Log($"{enemy.characterName} 移动后决定对 {target.characterName} 施放 {spell.name}");
                        spellSystem.CastSpell(spell, target.gameObject);
                    }
                }
            }
        }

        // 如果没有施法或施法失败，回退到近战策略
        else
        {
            yield return StartCoroutine(ExecuteMeleeStrategy(enemy, target, actionSystem));
        }
    }

    // 寻找最近的玩家角色
    private CharacterStats FindNearestPlayerCharacter(CharacterStats enemy)
    {
        CharacterStats nearestPlayer = null;
        float minDistance = float.MaxValue;

        // 获取所有角色
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();

        foreach (var character in allCharacters)
        {
            // 跳过非玩家控制的角色和已经失去意识的角色
            // 现在包括主角(Player)和队友(Ally)
            if ((!character.gameObject.CompareTag("Player") && !character.gameObject.CompareTag("Ally")) ||
                character.HasStatusEffect(StatusEffectType.Unconscious))
                continue;

            // 计算距离
            float distance = Vector3.Distance(enemy.transform.position, character.transform.position);

            // 更新最近的玩家
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPlayer = character;
            }
        }

        return nearestPlayer;
    }

    // 清理
    private void OnDestroy()
    {
        // 取消注册事件
        if (combatManager != null)
        {
            combatManager.OnTurnStart -= OnTurnStart;
        }
    }
}
