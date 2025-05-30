using UnityEngine;

/// <summary>
/// 角色类型检查工具类
/// 提供统一的角色类型判断方法，避免标签检查逻辑分散
/// </summary>
public static class CharacterTypeHelper
{
        // 标签常量
        public const string PLAYER_TAG = "Player";
        public const string ALLY_TAG = "Ally";
        public const string ENEMY_TAG = "Enemy";

        /// <summary>
        /// 检查是否是玩家控制的角色（包括主角和队友）
        /// </summary>
        /// <param name="character">角色GameObject</param>
        /// <returns>如果是玩家控制的角色返回true</returns>
        public static bool IsPlayerControlled(GameObject character)
        {
            if (character == null) return false;
            return character.CompareTag(PLAYER_TAG) || character.CompareTag(ALLY_TAG);
        }

        /// <summary>
        /// 检查是否是玩家控制的角色（包括主角和队友）
        /// </summary>
        /// <param name="characterStats">角色CharacterStats组件</param>
        /// <returns>如果是玩家控制的角色返回true</returns>
        public static bool IsPlayerControlled(CharacterStats characterStats)
        {
            if (characterStats == null) return false;
            return IsPlayerControlled(characterStats.gameObject);
        }

        /// <summary>
        /// 检查是否是主角角色
        /// </summary>
        /// <param name="character">角色GameObject</param>
        /// <returns>如果是主角返回true</returns>
        public static bool IsPlayerCharacter(GameObject character)
        {
            if (character == null) return false;
            return character.CompareTag(PLAYER_TAG);
        }

        /// <summary>
        /// 检查是否是主角角色
        /// </summary>
        /// <param name="characterStats">角色CharacterStats组件</param>
        /// <returns>如果是主角返回true</returns>
        public static bool IsPlayerCharacter(CharacterStats characterStats)
        {
            if (characterStats == null) return false;
            return IsPlayerCharacter(characterStats.gameObject);
        }

        /// <summary>
        /// 检查是否是队友角色
        /// </summary>
        /// <param name="character">角色GameObject</param>
        /// <returns>如果是队友返回true</returns>
        public static bool IsAllyCharacter(GameObject character)
        {
            if (character == null) return false;
            return character.CompareTag(ALLY_TAG);
        }

        /// <summary>
        /// 检查是否是队友角色
        /// </summary>
        /// <param name="characterStats">角色CharacterStats组件</param>
        /// <returns>如果是队友返回true</returns>
        public static bool IsAllyCharacter(CharacterStats characterStats)
        {
            if (characterStats == null) return false;
            return IsAllyCharacter(characterStats.gameObject);
        }

        /// <summary>
        /// 检查是否是敌人角色
        /// </summary>
        /// <param name="character">角色GameObject</param>
        /// <returns>如果是敌人返回true</returns>
        public static bool IsEnemyCharacter(GameObject character)
        {
            if (character == null) return false;
            return character.CompareTag(ENEMY_TAG);
        }

        /// <summary>
        /// 检查是否是敌人角色
        /// </summary>
        /// <param name="characterStats">角色CharacterStats组件</param>
        /// <returns>如果是敌人返回true</returns>
        public static bool IsEnemyCharacter(CharacterStats characterStats)
        {
            if (characterStats == null) return false;
            return IsEnemyCharacter(characterStats.gameObject);
        }

        /// <summary>
        /// 获取角色类型的字符串描述
        /// </summary>
        /// <param name="character">角色GameObject</param>
        /// <returns>角色类型描述</returns>
        public static string GetCharacterTypeString(GameObject character)
        {
            if (character == null) return "未知";

            if (IsPlayerCharacter(character)) return "主角";
            if (IsAllyCharacter(character)) return "队友";
            if (IsEnemyCharacter(character)) return "敌人";

            return "未知类型";
        }

        /// <summary>
        /// 获取角色类型的字符串描述
        /// </summary>
        /// <param name="characterStats">角色CharacterStats组件</param>
        /// <returns>角色类型描述</returns>
        public static string GetCharacterTypeString(CharacterStats characterStats)
        {
            if (characterStats == null) return "未知";
            return GetCharacterTypeString(characterStats.gameObject);
        }

        /// <summary>
        /// 检查两个角色是否是同一阵营（都是玩家控制或都是敌人）
        /// </summary>
        /// <param name="character1">角色1</param>
        /// <param name="character2">角色2</param>
        /// <returns>如果是同一阵营返回true</returns>
        public static bool IsSameFaction(GameObject character1, GameObject character2)
        {
            if (character1 == null || character2 == null) return false;

            bool char1IsPlayerControlled = IsPlayerControlled(character1);
            bool char2IsPlayerControlled = IsPlayerControlled(character2);

            // 都是玩家控制的角色，或者都是敌人
            return char1IsPlayerControlled == char2IsPlayerControlled;
        }

        /// <summary>
        /// 检查两个角色是否是同一阵营（都是玩家控制或都是敌人）
        /// </summary>
        /// <param name="characterStats1">角色1</param>
        /// <param name="characterStats2">角色2</param>
        /// <returns>如果是同一阵营返回true</returns>
        public static bool IsSameFaction(CharacterStats characterStats1, CharacterStats characterStats2)
        {
            if (characterStats1 == null || characterStats2 == null) return false;
            return IsSameFaction(characterStats1.gameObject, characterStats2.gameObject);
        }

        /// <summary>
        /// 检查角色是否可以被指定角色攻击（不同阵营）
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="target">目标</param>
        /// <returns>如果可以攻击返回true</returns>
        public static bool CanAttack(GameObject attacker, GameObject target)
        {
            if (attacker == null || target == null) return false;

            // 不能攻击同一阵营的角色
            return !IsSameFaction(attacker, target);
        }

        /// <summary>
        /// 检查角色是否可以被指定角色攻击（不同阵营）
        /// </summary>
        /// <param name="attackerStats">攻击者</param>
        /// <param name="targetStats">目标</param>
        /// <returns>如果可以攻击返回true</returns>
        public static bool CanAttack(CharacterStats attackerStats, CharacterStats targetStats)
        {
            if (attackerStats == null || targetStats == null) return false;
            return CanAttack(attackerStats.gameObject, targetStats.gameObject);
        }
}
