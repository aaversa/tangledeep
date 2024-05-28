using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SpecialEffectFunctions
{
    public static EffectResultPayload LearnSkillFromItemDataForFree(SpecialEffect eff, List<Actor> actorsToProcess)
    {
        string refOfSkill = GameMasterScript.itemBeingUsed.ReadActorDataString("teachskill");
        
        AbilityScript skillToLearn = GameMasterScript.masterAbilityList[refOfSkill];

        UIManagerScript.FlashWhite(0.9f);
        GameMasterScript.cameraScript.AddScreenshake(0.75f);
        GameMasterScript.heroPCActor.LearnAbility(skillToLearn, true, false, false, false);
        UIManagerScript.PlayCursorSound("Ultra Learn");

        EffectResultPayload erp = new EffectResultPayload();
        erp.actorsToProcess = actorsToProcess;

        Debug.Log(skillToLearn.CheckAbilityTag(AbilityTags.DRAGONSOUL) + " " + skillToLearn.refName + " " + GameMasterScript.tutorialManager.WatchedTutorial("tutorial_dragonsoul_learned"));

        if (skillToLearn.CheckAbilityTag(AbilityTags.DRAGONSOUL) && !GameMasterScript.tutorialManager.WatchedTutorial("tutorial_dragonsoul_learned"))
        {
            Conversation tut = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_dragonsoul_learned");
            GameMasterScript.SetAnimationPlaying(true, true);
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(tut, DialogType.STANDARD, null, 0.33f));
        }        

        return erp;
    }

    public static EffectResultPayload LearnSkillFromItemDataForJP(SpecialEffect eff, List<Actor> actorsToProcess)
    {
        // Pop up a dialogue offering to teach the player a skill for JP
        // What skill and what JP? Read from the actively used item, which is what generates this ability/function

        string refOfSkill = GameMasterScript.itemBeingUsed.ReadActorDataString("teachskill");
        int jpCostOfSkill = GameMasterScript.itemBeingUsed.ReadActorData("jpcost");

        GameMasterScript.gmsSingleton.SetTempStringData("itemsprite_for_dialog", GameMasterScript.itemBeingUsed.spriteRef);

        AbilityScript skillToLearn = GameMasterScript.masterAbilityList[refOfSkill];

        GameMasterScript.gmsSingleton.SetTempStringData("skilltolearn", skillToLearn.abilityName);
        GameMasterScript.gmsSingleton.SetTempStringData("skillreftolearn", skillToLearn.refName);
        GameMasterScript.gmsSingleton.SetTempStringData("itemskilljpcost", jpCostOfSkill.ToString());
        GameMasterScript.gmsSingleton.SetTempGameData("itemskilljpcost", jpCostOfSkill);

        Conversation learnSkill = GameMasterScript.FindConversation("dialog_learnskill_fromitem");
        TextBranch main = learnSkill.FindBranch("main");
        main.responses.Clear();

        ButtonCombo doLearnSkill = new ButtonCombo();
        doLearnSkill.buttonText = StringManager.GetString("exp_misc_promptlearnskill_fromitem");
        doLearnSkill.dbr = DialogButtonResponse.EXIT;
        doLearnSkill.actionRef = skillToLearn.refName;
        doLearnSkill.dialogEventScript = "TryLearnSkillFromRuneStone";
        doLearnSkill.dialogEventScriptValue = skillToLearn.refName;

        ButtonCombo exit = new ButtonCombo();
        exit.actionRef = "exit";
        exit.dbr = DialogButtonResponse.EXIT;
        exit.buttonText = StringManager.GetString("misc_button_exit_normalcase");

        main.responses.Add(doLearnSkill);
        main.responses.Add(exit);

        GameMasterScript.SetAnimationPlaying(true);

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FervirBuff", null, true);
        
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(learnSkill, DialogType.STANDARD, null, 1.2f));        

        EffectResultPayload erp = new EffectResultPayload();
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

}
