using UnityEngine;
using System.Reflection;

namespace DND5E
{
    // 行动类型枚举
    public enum ActionType
    {
        Action,         // 主要动作
        BonusAction,    // 附赠动作
        Movement,       // 移动
        Reaction        // 反应
    }

    // 行动系统组件
    public class ActionSystem : MonoBehaviour
    {
        // 行动状态
        public bool hasAction = true;
        public bool hasBonusAction = true;
        public bool hasMovement = true;
        public bool hasReaction = true;
        public bool hasMoved = false; // 标记角色是否已经移动

        // 移动相关
        public int movementSpeed = 30; // 默认移动速度30尺
        public int movementRemaining; // 剩余移动距离

        // 角色引用
        private string characterName = "角色";

        private void Awake()
        {
            // 初始化移动距离
            movementRemaining = movementSpeed;

            // 尝试获取角色名称
            var charStats = GetComponentInParent<MonoBehaviour>();
            if (charStats != null && charStats.GetType().GetProperty("characterName") != null)
            {
                // 使用反射获取characterName属性的值
                var property = charStats.GetType().GetProperty("characterName");
                var value = property.GetValue(charStats);
                if (value != null)
                {
                    characterName = value.ToString();
                }
            }

            // 如果没有获取到角色名称，使用游戏对象名称
            if (string.IsNullOrEmpty(characterName) || characterName == "角色")
            {
                characterName = gameObject.name;
            }
        }

        // 重置行动状态（回合开始时调用）
        public void ResetActions()
        {
            hasAction = true;
            hasBonusAction = true;
            hasMovement = true;
            hasReaction = true;
            hasMoved = false; // 重置移动标志
            movementRemaining = movementSpeed;

            // 尝试获取角色名称
            string charName = characterName;

            // 尝试从游戏对象名称获取
            if (string.IsNullOrEmpty(charName) || charName == "角色")
            {
                charName = gameObject.name;
            }

            Debug.Log($"【重要】{charName} 的行动已重置 - hasAction={hasAction}, hasMovement={hasMovement}, movementRemaining={movementRemaining}");
        }

        // 使用行动
        public bool UseAction(ActionType actionType)
        {
            switch(actionType)
            {
                case ActionType.Action:
                    if(!hasAction)
                    {
                        Debug.LogWarning($"{characterName} 没有主要动作可用!");
                        return false;
                    }
                    hasAction = false;
                    Debug.Log($"{characterName} 使用了主要动作");
                    return true;

                case ActionType.BonusAction:
                    if(!hasBonusAction)
                    {
                        Debug.LogWarning($"{characterName} 没有附赠动作可用!");
                        return false;
                    }
                    hasBonusAction = false;
                    Debug.Log($"{characterName} 使用了附赠动作");
                    return true;

                case ActionType.Movement:
                    if(!hasMovement)
                    {
                        Debug.LogWarning($"{characterName} 没有移动动作可用!");
                        return false;
                    }
                    hasMovement = false;
                    movementRemaining = 0;
                    Debug.Log($"{characterName} 使用了移动动作");
                    return true;

                case ActionType.Reaction:
                    if(!hasReaction)
                    {
                        Debug.LogWarning($"{characterName} 没有反应动作可用!");
                        return false;
                    }
                    hasReaction = false;
                    Debug.Log($"{characterName} 使用了反应动作");
                    return true;

                default:
                    Debug.LogError($"未知的行动类型: {actionType}");
                    return false;
            }
        }

        // 使用移动
        public bool UseMovement(int distance)
        {
            if (!hasMovement)
            {
                Debug.LogWarning($"{characterName} 没有移动动作可用!");
                return false;
            }

            if (distance > movementRemaining)
            {
                Debug.LogWarning($"{characterName} 没有足够的移动距离! (需要: {distance}, 剩余: {movementRemaining})");
                return false;
            }

            movementRemaining -= distance;
            hasMoved = true; // 标记角色已经移动
            Debug.Log($"{characterName} 移动了 {distance} 尺, 剩余 {movementRemaining} 尺, 已标记为已移动");

            // 如果移动距离用完，标记移动动作为已使用
            if (movementRemaining <= 0)
            {
                hasMovement = false;
            }

            return true;
        }

        // 使用冲刺（消耗主要动作，重置并获得等同于移动速度的移动距离）
        public bool UseDash()
        {
            if (!UseAction(ActionType.Action))
            {
                return false;
            }

            // 冲刺应该重置当前移动距离，然后设置为移动速度的两倍
            // 这样可以确保冲刺只能在回合开始时使用，且不能与普通移动叠加

            // 记录之前的移动距离，用于日志
            int previousMovement = movementRemaining;

            // 重置移动距离为移动速度的两倍
            movementRemaining = movementSpeed * 2;

            Debug.Log($"{characterName} 使用冲刺，重置移动距离（之前剩余: {previousMovement} 尺），获得 {movementRemaining} 尺移动距离");

            return true;
        }

        // 使用撤退（消耗主要动作，移动不会触发借机攻击）
        public bool UseDisengage()
        {
            if (!UseAction(ActionType.Action))
            {
                return false;
            }

            Debug.Log($"{characterName} 使用撤退，移动不会触发借机攻击");

            // 这里可以设置一个标志，表示移动不会触发借机攻击
            // 例如：isDisengaged = true;

            return true;
        }

        // 消耗所有移动力
        public bool UseAllMovement()
        {
            if (!hasMovement)
            {
                Debug.LogWarning($"{characterName} 没有移动动作可用!");
                return false;
            }

            int previousMovement = movementRemaining;
            movementRemaining = 0;
            hasMovement = false;
            hasMoved = true; // 标记为已移动，防御姿态消耗移动力也算作移动

            Debug.Log($"{characterName} 消耗了所有移动力 ({previousMovement} 尺)，标记为已移动");

            return true;
        }

        // 使用闪避（消耗主要动作，进入防御姿态，增加AC）
        public bool UseDodge()
        {
            // 自定义DND规则：防御姿态必须在移动之前使用
            if (hasMoved)
            {
                Debug.LogWarning($"{characterName} 已经移动过，无法进入防御姿态!");
                return false;
            }

            if (!UseAction(ActionType.Action))
            {
                return false;
            }

            Debug.Log($"{characterName} 进入防御姿态，获得+2AC直到下回合开始");

            // 防御姿态：角色专注于防御，获得AC加值但无法移动
            // 这个状态会在CharacterStats中通过StatusEffect管理

            return true;
        }

        // 使用帮助（消耗主要动作，给予盟友优势）
        public bool UseHelp(MonoBehaviour ally)
        {
            if (!UseAction(ActionType.Action))
            {
                return false;
            }

            Debug.Log($"{characterName} 帮助 {ally.name}，使其下一次攻击或技能检定具有优势");

            // 这里可以设置一个标志，表示盟友获得帮助
            // 例如：ally.isHelped = true;

            return true;
        }

        // 使用准备动作（消耗主要动作，准备一个触发条件和反应）
        public bool UseReadyAction(string triggerCondition)
        {
            if (!UseAction(ActionType.Action))
            {
                return false;
            }

            Debug.Log($"{characterName} 准备动作，触发条件: {triggerCondition}");

            // 这里可以存储触发条件和反应
            // 例如：
            // readyActionTrigger = triggerCondition;
            // readyActionReaction = reaction;

            return true;
        }

        // 使用物品（通常消耗主要动作）
        public bool UseItem(string itemName)
        {
            if (!UseAction(ActionType.Action))
            {
                return false;
            }

            Debug.Log($"{characterName} 使用物品: {itemName}");

            // 这里可以实现物品使用的具体效果

            return true;
        }
    }
}
