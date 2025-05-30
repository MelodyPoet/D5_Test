using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DND5E
{
    // 战斗规则静态类
    public static class CombatRules
    {
        // 随机数生成器
        private static System.Random random = new System.Random();
        
        // 掷骰方法
        public static int RollDice(int diceCount, int diceType)
        {
            int result = 0;
            for(int i = 0; i < diceCount; i++)
            {
                result += Random.Range(1, diceType + 1);
            }
            return result;
        }
        
        // 掷骰方法（带优势/劣势）
        public static int RollDice(int diceCount, int diceType, bool advantage, bool disadvantage)
        {
            // 优势和劣势相互抵消
            if(advantage && disadvantage)
            {
                return RollDice(diceCount, diceType);
            }
            
            if(advantage)
            {
                int roll1 = RollDice(diceCount, diceType);
                int roll2 = RollDice(diceCount, diceType);
                return Mathf.Max(roll1, roll2);
            }
            
            if(disadvantage)
            {
                int roll1 = RollDice(diceCount, diceType);
                int roll2 = RollDice(diceCount, diceType);
                return Mathf.Min(roll1, roll2);
            }
            
            return RollDice(diceCount, diceType);
        }
        
        // 解析骰子表达式（如"2d6+3"）
        public static int ParseDiceExpression(string expression)
        {
            // 分离加值部分
            string[] parts = expression.Split('+');
            
            int result = 0;
            
            // 处理骰子部分
            if(parts.Length > 0)
            {
                string dicePart = parts[0];
                string[] diceParts = dicePart.Split('d');
                
                if(diceParts.Length == 2)
                {
                    int diceCount = int.Parse(diceParts[0]);
                    int diceType = int.Parse(diceParts[1]);
                    
                    result += RollDice(diceCount, diceType);
                }
            }
            
            // 处理固定加值部分
            if(parts.Length > 1)
            {
                result += int.Parse(parts[1]);
            }
            
            return result;
        }
        
        // 计算攻击掷骰
        public static int RollAttack(int attackBonus, bool advantage = false, bool disadvantage = false)
        {
            int d20Roll = RollDice(1, 20, advantage, disadvantage);
            return d20Roll + attackBonus;
        }
        
        // 计算伤害
        public static int RollDamage(string damageFormula, bool critical = false)
        {
            // 分离加值部分
            string[] parts = damageFormula.Split('+');
            
            int damage = 0;
            
            // 处理骰子部分
            if(parts.Length > 0)
            {
                string dicePart = parts[0];
                string[] diceParts = dicePart.Split('d');
                
                if(diceParts.Length == 2)
                {
                    int diceCount = int.Parse(diceParts[0]);
                    int diceType = int.Parse(diceParts[1]);
                    
                    // 重击时骰子数量翻倍
                    if(critical)
                    {
                        diceCount *= 2;
                    }
                    
                    damage += RollDice(diceCount, diceType);
                }
            }
            
            // 处理固定加值部分（重击不影响固定加值）
            if(parts.Length > 1)
            {
                damage += int.Parse(parts[1]);
            }
            
            return damage;
        }
        
        // 计算豁免DC
        public static int CalculateSaveDC(int abilityModifier, int proficiencyBonus)
        {
            return 8 + abilityModifier + proficiencyBonus;
        }
        
        // 计算护甲等级（AC）
        public static int CalculateAC(int baseAC, int dexModifier, int maxDexBonus = 999)
        {
            return baseAC + Mathf.Min(dexModifier, maxDexBonus);
        }
        
        // 计算先攻值
        public static int RollInitiative(int dexModifier, bool advantage = false)
        {
            return RollDice(1, 20, advantage, false) + dexModifier;
        }
        
        // 计算生命值
        public static int CalculateHitPoints(int level, int hitDie, int conModifier)
        {
            if(level <= 0) return 0;
            
            // 1级时生命值为生命骰最大值+体质调整值
            int hp = hitDie + conModifier;
            
            // 之后每级生命值为生命骰平均值(向上取整)+体质调整值
            if(level > 1)
            {
                int averageRoll = (hitDie / 2) + 1;
                hp += (level - 1) * (averageRoll + conModifier);
            }
            
            return Mathf.Max(1, hp); // 生命值最小为1
        }
        
        // 计算熟练加值
        public static int CalculateProficiencyBonus(int level)
        {
            return 2 + (level - 1) / 4;
        }
        
        // 计算属性调整值
        public static int CalculateAbilityModifier(int abilityScore)
        {
            return (abilityScore - 10) / 2;
        }
        
        // 计算被动察觉
        public static int CalculatePassivePerception(int wisdomModifier, int proficiencyBonus, bool hasProficiency)
        {
            return 10 + wisdomModifier + (hasProficiency ? proficiencyBonus : 0);
        }
        
        // 计算移动速度（考虑负重和装甲）
        public static int CalculateSpeed(int baseSpeed, bool heavyArmor = false, bool encumbered = false)
        {
            int speed = baseSpeed;
            
            if(heavyArmor || encumbered)
            {
                speed -= 10;
            }
            
            return Mathf.Max(0, speed);
        }
        
        // 计算跳跃距离
        public static int CalculateJumpDistance(int strengthScore, int speed)
        {
            // 跳远距离（尺）= 力量值
            int longJump = strengthScore;
            
            // 如果速度小于30尺，跳远距离按比例减少
            if(speed < 30)
            {
                longJump = longJump * speed / 30;
            }
            
            // 跳高距离（尺）= 3 + 力量调整值
            int highJump = 3 + CalculateAbilityModifier(strengthScore);
            
            return longJump;
        }
        
        // 计算负重能力
        public static int CalculateCarryingCapacity(int strengthScore)
        {
            return strengthScore * 15; // 单位：磅
        }
        
        // 计算法术攻击加值
        public static int CalculateSpellAttackBonus(int spellcastingAbilityModifier, int proficiencyBonus)
        {
            return spellcastingAbilityModifier + proficiencyBonus;
        }
        
        // 计算法术豁免DC
        public static int CalculateSpellSaveDC(int spellcastingAbilityModifier, int proficiencyBonus)
        {
            return 8 + spellcastingAbilityModifier + proficiencyBonus;
        }
        
        // 计算武器攻击加值
        public static int CalculateWeaponAttackBonus(int abilityModifier, int proficiencyBonus, bool isProficient)
        {
            return abilityModifier + (isProficient ? proficiencyBonus : 0);
        }
        
        // 计算武器伤害加值
        public static int CalculateWeaponDamageBonus(int abilityModifier)
        {
            return abilityModifier;
        }
        
        // 计算技能检定加值
        public static int CalculateSkillBonus(int abilityModifier, int proficiencyBonus, bool hasProficiency, bool hasExpertise = false)
        {
            if(hasExpertise)
            {
                return abilityModifier + (proficiencyBonus * 2);
            }
            else if(hasProficiency)
            {
                return abilityModifier + proficiencyBonus;
            }
            else
            {
                return abilityModifier;
            }
        }
    }
}
