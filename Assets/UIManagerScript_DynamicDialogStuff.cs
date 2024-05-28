using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIManagerScript : MonoBehaviour {

    /// <summary>
    /// Handles weird/bad hardcoded stuff related to specific dialogs
    /// </summary>
    public static void OnDialogBoxUpdate_SpecialConversationCases()
    {
        if (currentConversation.refName == "grovetreeharvest")
        {
            int numFruits = MetaProgressScript.CountAllFoodInTrees();
            StringManager.SetTag(0, numFruits.ToString());
            currentTextBranch.text = StringManager.GetString("dialog_grove_harvest");
        }
        else if (currentConversation.refName == "dreamcaster_modify" && currentTextBranch.branchRefName != "main")
        {
            currentTextBranch.responses.Clear();
            int itemID = GameMasterScript.gmsSingleton.ReadTempGameData("removemoditemid");
            Actor itemToModify = GameMasterScript.gmsSingleton.TryLinkActorFromDict(itemID);
            if (itemToModify == null)
            {
                Debug.Log("WARNING: Could not find item in player inventory / equipment " + itemID);
            }
            else
            {
                Item eqToModify = itemToModify as Equipment;
                foreach (MagicMod mm in eqToModify.mods)
                {
                    if (eqToModify.autoModRef != null)
                    {
                        if (eqToModify.autoModRef.Contains(mm.refName)) continue;
                    }

                    if (mm.noNameChange) continue;

                    string textToUse = "";

                    textToUse = mm.GetDescription();

                    int orbCost = ItemWorldUIScript.GetOrbCostByCV(mm.challengeValue, eqToModify.challengeValue, eqToModify.legendary, mm.IsSpecialMod());

                    StringManager.SetTag(4, orbCost.ToString());
                    string addOrbCost = StringManager.GetString("misc_itemworld_orbcost_definite");

                    StringManager.SetTag(0, itemToModify.displayName);
                    StringManager.SetTag(1, GameMasterScript.gmsSingleton.ReadTempGameData("removemodcost").ToString());

                    textToUse += " (" + addOrbCost + ")";

                    ButtonCombo modButton = new ButtonCombo();
                    modButton.buttonText = textToUse;
                    modButton.dbr = DialogButtonResponse.CONTINUE;
                    modButton.actionRef = mm.refName;
                    currentTextBranch.responses.Add(modButton);
                }
            }

            ButtonCombo bc = new ButtonCombo();
            bc.buttonText = StringManager.GetString("misc_button_exit_normalcase");
            bc.dbr = DialogButtonResponse.EXIT;
            bc.actionRef = "exit";
            currentTextBranch.responses.Add(bc);
        }
        else if (currentConversation.refName == "grovetree")
        {
            int slot = currentConversation.whichNPC.GetTreeSlot();
            if (slot == -1)
            {
                Debug.Log("Null slot?");
                slot = 0;
            }
            MagicTree mt = currentConversation.whichNPC.treeComponent;

            if (mt.alive)
            {
                currentConversation.RemoveBranchByRef("chopbranch");
                TextBranch newTB = new TextBranch();
                newTB.branchRefName = "chopbranch";

                newTB.responses.Clear();

                ButtonCombo exit = new ButtonCombo();
                exit.actionRef = "exit";
                exit.dbr = DialogButtonResponse.EXIT;
                exit.buttonText = StringManager.GetString("misc_button_exit_normalcase");
                newTB.responses.Add(exit);
                SwitchConversationBranch(newTB);

                StringManager.SetTag(0, mt.whoPlanted);
                StringManager.SetTag(1, mt.CalcAge().ToString());

                newTB.text = mt.GetTreeAgeString() + " " + mt.GetSpeciesName() + " (" + mt.GetRarity() + ").\n" + StringManager.GetString("grove_tree_plantinfo") + "\n";
                int xpReward = (int)mt.GetXPReward();
                int jpReward = (int)mt.GetJPReward();
                ButtonCombo bc = new ButtonCombo();
                bc.actionRef = "choptree";
                bc.buttonText = StringManager.GetString("misc_chop_down") + " (" + xpReward + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.XP) + ", " + jpReward + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.JP) + ")";
                newTB.responses.Add(bc);

            }
            else
            {
                // Don't overwrite text at all.
            }
        }
    }

}
