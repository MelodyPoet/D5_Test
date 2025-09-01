using UnityEngine;
using UnityEditor;

/// <summary>
/// 角色模板创建工具
/// </summary>
public class CharacterTemplateCreator : EditorWindow
{
    [MenuItem("DND/Create Character Templates")]
    public static void CreateAllTemplates()
    {
        CreatePlayerTemplates();
        CreateEnemyTemplates();
        AssetDatabase.Refresh();
        Debug.Log("所有角色模板创建完成！");
    }

    private static void CreatePlayerTemplates()
    {
        // 创建玩家战士模板
        var fighterTemplate = ScriptableObject.CreateInstance<CharacterTemplate>();
        fighterTemplate.characterName = "玩家战士";
        fighterTemplate.characterClass = CharacterClass.Fighter;
        fighterTemplate.level = 3;
        fighterTemplate.defaultSide = BattleSide.Player;
        fighterTemplate.strength = 16;
        fighterTemplate.dexterity = 12;
        fighterTemplate.constitution = 14;
        fighterTemplate.intelligence = 10;
        fighterTemplate.wisdom = 12;
        fighterTemplate.charisma = 10;
        fighterTemplate.baseArmorClass = 16;
        fighterTemplate.hitDie = 10;
        fighterTemplate.proficiencyBonus = 2;
        fighterTemplate.proficientSkills.Add(Skill.Athletics);
        fighterTemplate.proficientSkills.Add(Skill.Intimidation);
        fighterTemplate.proficientSaves.Add("strength");
        fighterTemplate.proficientSaves.Add("constitution");

        AssetDatabase.CreateAsset(fighterTemplate, "Assets/demo2/DND/Templates/PlayerFighter_Template.asset");

        // 创建玩家法师模板
        var wizardTemplate = ScriptableObject.CreateInstance<CharacterTemplate>();
        wizardTemplate.characterName = "玩家法师";
        wizardTemplate.characterClass = CharacterClass.Wizard;
        wizardTemplate.level = 3;
        wizardTemplate.defaultSide = BattleSide.Player;
        wizardTemplate.strength = 8;
        wizardTemplate.dexterity = 14;
        wizardTemplate.constitution = 12;
        wizardTemplate.intelligence = 16;
        wizardTemplate.wisdom = 13;
        wizardTemplate.charisma = 10;
        wizardTemplate.baseArmorClass = 12;
        wizardTemplate.hitDie = 6;
        wizardTemplate.proficiencyBonus = 2;
        wizardTemplate.proficientSkills.Add(Skill.Arcana);
        wizardTemplate.proficientSkills.Add(Skill.Investigation);
        wizardTemplate.proficientSaves.Add("intelligence");
        wizardTemplate.proficientSaves.Add("wisdom");

        AssetDatabase.CreateAsset(wizardTemplate, "Assets/demo2/DND/Templates/PlayerWizard_Template.asset");

        // 创建玩家牧师模板
        var clericTemplate = ScriptableObject.CreateInstance<CharacterTemplate>();
        clericTemplate.characterName = "玩家牧师";
        clericTemplate.characterClass = CharacterClass.Cleric;
        clericTemplate.level = 3;
        clericTemplate.defaultSide = BattleSide.Player;
        clericTemplate.strength = 12;
        clericTemplate.dexterity = 10;
        clericTemplate.constitution = 14;
        clericTemplate.intelligence = 10;
        clericTemplate.wisdom = 16;
        clericTemplate.charisma = 13;
        clericTemplate.baseArmorClass = 15;
        clericTemplate.hitDie = 8;
        clericTemplate.proficiencyBonus = 2;
        clericTemplate.proficientSkills.Add(Skill.Medicine);
        clericTemplate.proficientSkills.Add(Skill.Insight);
        clericTemplate.proficientSaves.Add("wisdom");
        clericTemplate.proficientSaves.Add("charisma");

        AssetDatabase.CreateAsset(clericTemplate, "Assets/demo2/DND/Templates/PlayerCleric_Template.asset");
    }

    private static void CreateEnemyTemplates()
    {
        // 创建兽人战士模板
        var orcTemplate = ScriptableObject.CreateInstance<CharacterTemplate>();
        orcTemplate.characterName = "兽人战士";
        orcTemplate.characterClass = CharacterClass.Fighter;
        orcTemplate.level = 2;
        orcTemplate.defaultSide = BattleSide.Enemy;
        orcTemplate.strength = 14;
        orcTemplate.dexterity = 12;
        orcTemplate.constitution = 16;
        orcTemplate.intelligence = 7;
        orcTemplate.wisdom = 11;
        orcTemplate.charisma = 10;
        orcTemplate.baseArmorClass = 13;
        orcTemplate.hitDie = 8;
        orcTemplate.proficiencyBonus = 2;
        orcTemplate.proficientSkills.Add(Skill.Intimidation);

        AssetDatabase.CreateAsset(orcTemplate, "Assets/demo2/DND/Templates/EnemyOrc_Template.asset");

        // 创建哥布林模板
        var goblinTemplate = ScriptableObject.CreateInstance<CharacterTemplate>();
        goblinTemplate.characterName = "哥布林";
        goblinTemplate.characterClass = CharacterClass.Rogue;
        goblinTemplate.level = 1;
        goblinTemplate.defaultSide = BattleSide.Enemy;
        goblinTemplate.strength = 8;
        goblinTemplate.dexterity = 14;
        goblinTemplate.constitution = 10;
        goblinTemplate.intelligence = 10;
        goblinTemplate.wisdom = 8;
        goblinTemplate.charisma = 8;
        goblinTemplate.baseArmorClass = 12;
        goblinTemplate.hitDie = 6;
        goblinTemplate.proficiencyBonus = 2;
        goblinTemplate.proficientSkills.Add(Skill.SleightOfHand);
        goblinTemplate.proficientSkills.Add(Skill.Stealth);

        AssetDatabase.CreateAsset(goblinTemplate, "Assets/demo2/DND/Templates/EnemyGoblin_Template.asset");

        // 创建巨魔模板
        var trollTemplate = ScriptableObject.CreateInstance<CharacterTemplate>();
        trollTemplate.characterName = "巨魔";
        trollTemplate.characterClass = CharacterClass.Barbarian;
        trollTemplate.level = 4;
        trollTemplate.defaultSide = BattleSide.Enemy;
        trollTemplate.strength = 18;
        trollTemplate.dexterity = 8;
        trollTemplate.constitution = 18;
        trollTemplate.intelligence = 6;
        trollTemplate.wisdom = 9;
        trollTemplate.charisma = 7;
        trollTemplate.baseArmorClass = 14;
        trollTemplate.hitDie = 12;
        trollTemplate.proficiencyBonus = 2;
        trollTemplate.proficientSkills.Add(Skill.Intimidation);
        trollTemplate.vulnerabilities.Add(DamageType.Fire);

        AssetDatabase.CreateAsset(trollTemplate, "Assets/demo2/DND/Templates/EnemyTroll_Template.asset");
    }
}
