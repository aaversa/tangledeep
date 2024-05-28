using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using System.Reflection;

public partial class UIManagerScript
{
    public static void SwitchConversationBranch(TextBranch tb)
    {
        // Special case - intros, cinematics etc.

        CheckForIntroStorySpecialCases(tb);

        bool searchForAltBranch = true;

        TextBranch searchTB = tb;
        Item useItem = null;
        string reqItemInInventory = "";
        AlternateBranch switchToAlternateBranch = null;

        while (searchForAltBranch)
        {
            if (searchTB.altBranches.Count > 0)
            {
                TextBranch newTB = null;
                switchToAlternateBranch = null;
                foreach (AlternateBranch ab in searchTB.altBranches)
                {
                    int value = GameMasterScript.heroPCActor.ReadActorData(ab.branchReqFlag);
                    if (ab.branchReqFlagMeta)
                    {
                        value = MetaProgressScript.ReadMetaProgress(ab.branchReqFlag);
                    }
                    //Debug.Log("Consider value " + value + " for flag " + ab.branchReqFlag + " " + ab.altBranchRef + " vs. req of " + ab.branchReqFlagValue);
                    if (value >= ab.branchReqFlagValue)
                    {
                        bool valid = true;
                        if (!String.IsNullOrEmpty(ab.reqItemInInventory))
                        {
                            useItem = GameMasterScript.heroPCActor.myInventory.GetItemByRef(ab.reqItemInInventory);
                            if (useItem == null)
                            {
                                valid = false;
                            }
                            else
                            {
                                reqItemInInventory = ab.reqItemInInventory;
                            }
                        }
                        else
                        {
                            // No item required.
                        }
                        if (valid)
                        {
                            newTB = currentConversation.FindBranch(ab.altBranchRef);
                            switchToAlternateBranch = ab;
                        }
                    }
                }

                if (newTB != null)
                {
                    // Found a new text branch to switch to
                    searchTB = newTB;
                }
                else
                {
                    searchForAltBranch = false;
                }
            }
            else
            {
                searchForAltBranch = false;
            }
        }

        tb = searchTB; // this was currentTextBranch = searchTB
        currentTextBranch = searchTB;

        if (switchToAlternateBranch != null)
        {
            if (!String.IsNullOrEmpty(switchToAlternateBranch.reqItemInInventory))
            {
                if (switchToAlternateBranch.useReqItem)
                {
                    if (useItem.itemType == ItemTypes.CONSUMABLE)
                    {
                        useItem = GameMasterScript.heroPCActor.myInventory.GetIdeallyUnmodifiedFoodbyRef(useItem.actorRefName);
                        Consumable c = useItem as Consumable;
                        c.ChangeQuantity(-1);
                        if (c.Quantity <= 0)
                        {
                            GameMasterScript.heroPCActor.myInventory.RemoveItem(c);
                        }
                    }
                    else
                    {
                        GameMasterScript.heroPCActor.myInventory.RemoveItem(useItem);
                    }
                }
                StringManager.SetTag(0, useItem.displayName);
                GameLogScript.GameLogWrite(StringManager.GetString("item_used_single"), GameMasterScript.heroPCActor);
            }
        }

        CheckForScriptsOnTextBranch(tb);
        CheckForLearnRecipeSpecialOnTextBranch(tb);        
        CheckForPainterQuestStuffOnTextBranch(tb);
        CheckForAddFlagsAndRecipesOnTextBranch(tb);
        CheckForGrantItemOnTextBranch(tb);
        CheckForDirtbeakConvoBuffOnTextBranch(tb);
        TryWriteConversationInGameLog(tb);

        if (currentConversation.overrideSize)
        {
            myDialogBoxComponent.OverrideConversationSize(currentConversation.windowSize);
        }
        if (currentConversation.overridePos)
        {
            myDialogBoxComponent.OverrideConversationPos(currentConversation.windowPos);
        }

        if (currentTextBranch.branchRefName == "special_cookfire")
        {
            CloseDialogBox();
            OpenCookingInterface();
        }
        //would we like to display an image?
        currentConversation.strSpriteToDisplay = currentTextBranch.strSpriteToDisplay;
        currentConversation.strPrefabToDisplayInFrontOfDialog = currentTextBranch.strPrefabToDisplayInFrontOfDialog;
        currentConversation.vOffsetForPrefabToDisplay = currentTextBranch.vPrefabToDisplayOffset;
    }

    private static void TryWriteConversationInGameLog(TextBranch tb)
    {
        if (currentConversation.writeInCombatLog)
        {
            string copyOfText = string.Copy(tb.text);
            copyOfText = CustomAlgorithms.ParseRichText(copyOfText, true);
            copyOfText = CustomAlgorithms.ParseButtonAssignments(copyOfText);
            copyOfText = CustomAlgorithms.ParseLiveMergeTags(copyOfText);
            if (!copyOfText.Contains("<sprite"))
            {
                copyOfText = copyOfText.Replace("<size=50>", String.Empty);
                copyOfText = copyOfText.Replace("<size=48>", String.Empty);
                copyOfText = copyOfText.Replace("<size=46>", String.Empty);
                copyOfText = copyOfText.Replace("<size=44>", String.Empty);
                copyOfText = copyOfText.Replace("<size=42>", String.Empty);
                copyOfText = copyOfText.Replace("</size>", String.Empty);
                copyOfText = copyOfText.Replace("#big#", String.Empty);
                copyOfText = copyOfText.Replace("#endbig#", String.Empty);
                GameLogScript.GameLogWrite(copyOfText, GameMasterScript.heroPCActor);
            }
        }
    }

    private static void CheckForDirtbeakConvoBuffOnTextBranch(TextBranch tb)
    {
        bool doBuff = false;

        if (tb.branchRefName == "ending_defense")
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRef("status_storydefenseup", GameMasterScript.heroPCActor, 15);
            StringManager.SetTag(0, StringManager.GetString("misc_generic_defense").ToUpperInvariant());
            doBuff = true;
        }
        else if (tb.branchRefName == "ending_spirit")
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRef("status_storyspiritup", GameMasterScript.heroPCActor, 15);
            StringManager.SetTag(0, StringManager.GetString("stat_spiritpower").ToUpperInvariant());
            doBuff = true;
        }
        else if (tb.branchRefName == "ending_offense")
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRef("status_storyattackup", GameMasterScript.heroPCActor, 15);
            StringManager.SetTag(0, StringManager.GetString("misc_generic_damage").ToUpperInvariant());
            doBuff = true;
        }

        if (doBuff)
        {
            GameLogScript.LogWriteStringRef("log_confidence_buff");
            UIManagerScript.RefreshStatuses();
        }
    }

    private static void CheckForLearnRecipeSpecialOnTextBranch(TextBranch tb)
    {
        if (tb.branchRefName == "learnarecipe")
        {
            CookingScript.masterRecipeList.Shuffle();
            Recipe r = null;

            for (int i = 0; i < CookingScript.masterRecipeList.Count; i++)
            {
                if (!MetaProgressScript.recipesKnown.Contains(CookingScript.masterRecipeList[i].refName))
                {
                    r = CookingScript.masterRecipeList[i];
                    break;
                }
            }
            if (r == null)
            {
                SwitchConversationBranch(currentConversation.FindBranch("nothing"));
                return;
            }
            Recipe r2 = null;
            for (int i = 0; i < CookingScript.masterRecipeList.Count; i++)
            {
                if ((!MetaProgressScript.recipesKnown.Contains(CookingScript.masterRecipeList[i].refName)) && (CookingScript.masterRecipeList[i] != r))
                {
                    r2 = CookingScript.masterRecipeList[i];
                    break;
                }
            }

            MetaProgressScript.LearnRecipe(r.refName);
            string rText1 = "<#fffb00>" + r.displayName + "</color>: " + r.description + " <#fffb00>" +
                StringManager.GetString("misc_craft_requires") + ":</color> " + r.ingredientsDescription + "\n\n";
            tb.text += rText1;
            if (r2 != null)
            {
                MetaProgressScript.LearnRecipe(r2.refName);
                string rText2 = "<color=yellow>" + r2.displayName + "</color>: " + r2.description + " <color=yellow>" +
                                StringManager.GetString("misc_craft_requires") + ":</color> " + r2.ingredientsDescription + "\n\n";
                tb.text += rText2;
            }

        }
    }

    private static void CheckForConversationStartScript(Conversation c)
    {
        if (!String.IsNullOrEmpty(c.runScriptOnConversationStart))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(ConversationStartScripts), c.runScriptOnConversationStart);
            object[] paramList = new object[1];
            paramList[0] = "nothing";

            try
            {
                runscript.Invoke(null, paramList);
            }
            catch (Exception e)
            {
                Debug.Log("Error with " + c.runScriptOnConversationStart + ": " + e);
            }
        }
    }

    private static void CheckForScriptsOnTextBranch(TextBranch tb)
    {
        if (!String.IsNullOrEmpty(tb.script_textBranchStart))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(DialogEventsScript), tb.script_textBranchStart);
            object[] paramList = new object[1];
            if (!string.IsNullOrEmpty(tb.script_textBranchStartValue))
            {
                paramList[0] = tb.script_textBranchStartValue;
            }
            else
            {
                paramList[0] = "nothing";
            }

            try
            {
                runscript.Invoke(null, paramList);
            }
            catch (Exception e)
            {
                Debug.Log("Error with " + tb.script_textBranchStart + ": " + e);
            }
        }
    }

    private static void CheckForPainterQuestStuffOnTextBranch(TextBranch tb)
    {
        if (tb.branchRefName == "painterprequest" && GameMasterScript.heroPCActor.ReadActorData("painterquest") == 1)
        {
            Map mToUse = MapMasterScript.theDungeon.FindFloor(GameMasterScript.heroPCActor.ReadActorData("painterquestfloor"));
            Map nearby = MapMasterScript.theDungeon.FindFloor(GameMasterScript.heroPCActor.ReadActorData("painterquestnearby"));
            tb.text = tb.text.Replace("VLOCV", mToUse.GetName());
            tb.text = tb.text.Replace("VNEARBYV", nearby.GetName());
        }
        if (tb.branchRefName == "painterselectmap")
        {
            List<Map> possible = QuestScript.GetUnexploredCombatSideAreas(scaleToLevel: true);
            if (possible.Count == 0)
            {
                Debug.Log("No possible painter quest maps.");
                SwitchConversationBranch(currentConversation.FindBranch("nomaps"));
                return;
            }

            Map mToUse = possible[UnityEngine.Random.Range(0, possible.Count)];
            Debug.Log("Our highest level: " + GameMasterScript.heroPCActor.lowestFloorExplored + " vs map effective floor of " + mToUse.effectiveFloor);
            foreach (Stairs st in mToUse.mapStairs)
            {
                st.EnableActor();
            }
            Map nearbyMap = null;
            Map possibleOption = null;
            foreach (Map m in MapMasterScript.theDungeon.maps)
            {
                foreach (Stairs st in m.mapStairs)
                {
                    if (st.NewLocation == mToUse)
                    {
                        st.EnableActor();
                        possibleOption = m;
                        if (mToUse.dungeonLevelData.stairsDownToLevel != m.floor)
                        {
                            nearbyMap = m;
                        }
                    }
                }
            }

            if (nearbyMap == null)
            {
                nearbyMap = possibleOption;
            }

            string mName = mToUse.GetName();
            tb.text = tb.text.Replace("VLOCV", mName);
            tb.text = tb.text.Replace("VNEARBYV", nearbyMap.GetName());
            //tb.addFlag.flagValue = mToUse.floor;
            GameMasterScript.heroPCActor.SetActorData("painterquestfloor", mToUse.floor);
            GameMasterScript.heroPCActor.SetActorData("painterquestnearby", nearbyMap.floor);

            if (nearbyMap == null)
            {
                Debug.Log("2 No possible painter quest maps.");
                tb.branchRefName = "nomaps";
            }
        }
    }

    private static void CheckForAddFlagsAndRecipesOnTextBranch(TextBranch tb)
    {
        if (tb.addFlag != null)
        {
            if (!tb.addFlag.meta)
            {
                GameMasterScript.heroPCActor.SetActorData(tb.addFlag.flagRef, tb.addFlag.flagValue);
            }
            else
            {
                MetaProgressScript.SetMetaProgress(tb.addFlag.flagRef, tb.addFlag.flagValue);
            }

#if UNITY_EDITOR
            DebugConsole.Log("Flag set: " + tb.addFlag.flagRef + " == " + tb.addFlag.flagValue);
#endif
        }
        if (tb.grantRecipe.Count > 0)
        {
            foreach (string txt in tb.grantRecipe)
            {
                MetaProgressScript.LearnRecipe(txt);
            }
        }
    }

    private static void CheckForGrantItemOnTextBranch(TextBranch tb)
    {
        if (!String.IsNullOrEmpty(tb.grantItemRef))
        {
            Item newItem = LootGeneratorScript.CreateItemFromTemplateRef(tb.grantItemRef, MapMasterScript.activeMap.challengeRating, 0f, true);
            newItem.RebuildDisplayName();
            GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(newItem, true);
            if (newItem.IsEquipment())
            {
                if (newItem.actorRefName.Contains("_ori"))
                {
                    Equipment eq = newItem as Equipment;
                    EquipmentBlock.MakeMagical(eq, eq.challengeValue, true);
                }
                else if (newItem.actorRefName == "weapon_bezocrossbow")
                {
                    Equipment eq = newItem as Equipment;
                    EquipmentBlock.MakeMagical(eq, eq.challengeValue, true);
                }
            }
            newItem.CalculateShopPrice(1.0f);
            newItem.CalculateSalePrice();
            StringManager.SetTag(0, currentConversation.whichNPC.displayName);
            StringManager.SetTag(1, newItem.displayName);
            GameLogScript.LogWriteStringRef("npc_give_item");
        }
    }

    static void CheckForIntroStorySpecialCases(TextBranch tb)
    {
        if (currentConversation.refName == "introstory1")
        {
            animatingDialog = true; // Make sure the dialog box y-scale fades in during intro stuff.
            if (tb.branchRefName != "gameintropart1")
            {
                PlayCursorSound("Select");

                if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                {
                    Image[] imagesInChildren = GameObject.Find("IntroStoryStuff").GetComponentsInChildren<Image>();
                    foreach (var a in imagesInChildren)
                    {
                        a.sprite = TDAssetBundleLoader.GetSpriteFromMemory(a.sprite.name);
                    }
                }
            }

            // Images of NPC characters
            if (tb.branchRefName == "gameintropart2")
            {
                GameObject.Find("IntroStoryStuff").GetComponent<CanvasGroupFader>().FadeIn(1.0f);

                if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                {
                    GameObject.Find("NPCCatBirdMerchant").GetComponent<Animatable>().SetAnim("Default");
                    GameObject.Find("NPCFrogFarmer").GetComponent<Animatable>().SetAnim("Default");
                    GameObject.Find("NPCMoonRabbit").GetComponent<Animatable>().SetAnim("Default");
                }
                else
                {
                    Animatable[] animatablesInChildren = GameObject.Find("IntroStoryStuff").GetComponentsInChildren<Animatable>();
                    foreach (var a in animatablesInChildren)
                    {
                        a.SetAnim("Default");
                    }
                }
            }
            else
            {
                if (currentTextBranch.branchRefName == "gameintropart2")
                {
                    GameObject.Find("IntroStoryStuff").GetComponent<CanvasGroupFader>().FadeOut(0.2f);
                }
            }
            if (tb.branchRefName == "gameintropart3")
            {
                GameObject.Find("IntroStoryBG").GetComponent<CanvasGroupFader>().FadeIn(1.0f);
            }
            else
            {
                if (currentTextBranch.branchRefName == "gameintropart3")
                {
                    GameObject.Find("IntroStoryBG").GetComponent<CanvasGroupFader>().FadeOut(0.2f);
                }
            }
        }
    }
}
