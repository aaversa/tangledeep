using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UIManagerScript : MonoBehaviour {

    public static void ResetCookingUI()
    {
        GameMasterScript.gmsSingleton.SetTempGameData("repeatrecipe", 0);
        if (transparentSprite == null)
        {
            transparentSprite = Resources.Load<Sprite>("Art/transparentpixel");

        }
        panIngredient1.GetComponent<Image>().sprite = transparentSprite;
        panIngredient2.GetComponent<Image>().sprite = transparentSprite;
        panIngredient3.GetComponent<Image>().sprite = transparentSprite;
        panSeasoning.GetComponent<Image>().sprite = transparentSprite;
        cookingResultImage.GetComponent<Image>().sprite = transparentSprite;
        for (int i = 0; i < ingredients.Length; i++)
        {
            ingredients[i].gameObj.GetComponent<Image>().sprite = transparentSprite;
        }
        for (int i = 0; i < seasoning.Length; i++)
        {
            seasoning[i].gameObj.GetComponent<Image>().sprite = transparentSprite;
        }

        cookingIngredientItems[0] = null;
        cookingIngredientItems[1] = null;
        cookingIngredientItems[2] = null;
        cookingResultItem = null;
        cookingSeasoningItem = null;
    }

    public void ResetCookingFromInterface()
    {
        PlayCursorSound("Cancel");
        ResetCookingUI();
        UpdateCookingPlayerLists();
    }

    public void RepeatLastRecipeFromInterface()
    {
        PlayCursorSound("Select");
        bool haveAllIngredients = true;
        Dictionary<string, int> reqs = new Dictionary<string, int>();

        for (int i = 0; i < lastCookedItems.Length; i++)
        {
            if (lastCookedItems[i] != null)
            {
                if (reqs.ContainsKey(lastCookedItems[i].actorRefName))
                {
                    int newReq = reqs[lastCookedItems[i].actorRefName] + 1;
                    reqs[lastCookedItems[i].actorRefName] = newReq;
                }
                else
                {
                    reqs.Add(lastCookedItems[i].actorRefName, 1);
                }
            }
        }

        foreach (string sRef in reqs.Keys)
        {
            int qty = GameMasterScript.heroPCActor.myInventory.GetItemQuantity(sRef);
            if (qty < reqs[sRef])
            {
                haveAllIngredients = false;
                break;
            }
        }

        if (!haveAllIngredients || !haveAllIngredients)
        {
            PlayCursorSound("Error");
            return;
        }

        for (int i = 0; i < cookingIngredientItems.Length; i++)
        {
            cookingIngredientItems[i] = lastCookedItems[i];
        }
        cookingSeasoningItem = lastCookedItems[3];

        UpdatePanGraphics();

        ChangeUIFocusAndAlignCursor(cookButton);

        UpdateCookingPlayerLists();

        GameMasterScript.gmsSingleton.SetTempGameData("repeatrecipe", 1);

    }

    public void UpdatePanGraphics()
    {
        if (cookingSeasoningItem != null)
        {
            panSeasoning.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, cookingSeasoningItem.spriteRef);
        }
        else
        {
            panSeasoning.GetComponent<Image>().sprite = transparentSprite;
        }

        if (cookingIngredientItems[0] != null)
        {
            panIngredient1.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, cookingIngredientItems[0].spriteRef);
        }
        else
        {
            panIngredient1.GetComponent<Image>().sprite = transparentSprite;
        }

        if (cookingIngredientItems[1] != null)
        {
            panIngredient2.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, cookingIngredientItems[1].spriteRef);
        }
        else
        {
            panIngredient2.GetComponent<Image>().sprite = transparentSprite;
        }

        if (cookingIngredientItems[2] != null)
        {
            panIngredient3.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, cookingIngredientItems[2].spriteRef);
        }
        else
        {
            panIngredient3.GetComponent<Image>().sprite = transparentSprite;
        }
    }

    public void ResetCookingFromBtn(int dummy)
    {
        ResetCookingFromInterface();
    }

    public void RepeatLastRecipeFromBtn(int dummy)
    {
        RepeatLastRecipeFromInterface();
    }

    public static void UpdateCookingPlayerLists()
    {
        int ingredientIndex = 0;
        int seasoningIndex = 0;

        for (int i = 0; i < ingredients.Length; i++)
        {
            ingredients[i].gameObj.GetComponent<Image>().sprite = transparentSprite;
            cookingPlayerIngredientList[i] = null;
            ingredientsQuantityText[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < seasoning.Length; i++)
        {
            seasoning[i].gameObj.GetComponent<Image>().sprite = transparentSprite;
            cookingPlayerSeasoningList[i] = null;
            seasoningQuantityText[i].gameObject.SetActive(false);
        }

        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.itemType == ItemTypes.CONSUMABLE)
            {
                Consumable c = itm as Consumable;
                int effectiveQuantity = c.Quantity;
                if (c.cookingIngredient)
                {
                    for (int i = 0; i < cookingIngredientItems.Length; i++)
                    {
                        if (cookingIngredientItems[i] == c)
                        {
                            effectiveQuantity--;
                        }
                    }
                    if (effectiveQuantity > 0)
                    {
                        ingredients[ingredientIndex].gameObj.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, c.spriteRef);
                        ingredientQuantities[ingredientIndex] = effectiveQuantity;
                        cookingPlayerIngredientList[ingredientIndex] = itm;

                        ingredientsQuantityText[ingredientIndex].gameObject.SetActive(true);
                        ingredientsQuantityText[ingredientIndex].text = effectiveQuantity.ToString();

                        ingredientIndex++;

                    }
                }

                if (c.seasoning)
                {
                    if (cookingSeasoningItem == c)
                    {
                        effectiveQuantity--;
                    }

                    if (effectiveQuantity > 0)
                    {
                        seasoning[seasoningIndex].gameObj.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, c.spriteRef);
                        cookingPlayerSeasoningList[seasoningIndex] = itm;

                        seasoningQuantityText[seasoningIndex].gameObject.SetActive(true);
                        seasoningQuantityText[seasoningIndex].text = effectiveQuantity.ToString();

                        seasoningIndex++;

                    }
                }
            }
        }
    }

    public static void OpenCookingInterface()
    {
        PlayCursorSound("OpenDialog");
        MinimapUIScript.StopOverlay();
        singletonUIMS.CloseAllDialogsExcept(UITabs.COOKING);

        ShowDialogMenuCursor();
        ChangeUIFocus(ingredients[0]);
        allUIObjects.Clear();
        singletonUIMS.EnableCursor();
        AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false); // Should this be done in change focus?

        for (int i = 0; i < ingredients.Length; i++)
        {
            allUIObjects.Add(ingredients[i]);
        }
        for (int i = 0; i < seasoning.Length; i++)
        {
            allUIObjects.Add(seasoning[i]);
        }
        allUIObjects.Add(cookingExit);
        allUIObjects.Add(cookButton);
        allUIObjects.Add(cookingReset);

        cookingUI.SetActive(true);
        // Clear the results area.
        cookingResultText.text = StringManager.GetString("ui_cooking_placeholder_text");
        ResetCookingUI();

        SetWindowState(UITabs.COOKING, true);
        
        TDInputHandler.OnDialogOrFullScreenUIOpened();
        GuideMode.OnFullScreenUIOpened();

        for (int i = 0; i < lastCookedItems.Length; i++)
        {
            lastCookedItems[i] = null;
        }

        int ingredientIndex = 0;
        int seasoningIndex = 0;
        ingredientQuantities = new int[ingredients.Length];
        cookingPlayerSeasoningList = new Item[CookingScript.NUM_ALL_SEASONINGS];
        cookingPlayerIngredientList = new Item[NUM_INGREDIENTS];
        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.itemType == ItemTypes.CONSUMABLE)
            {
                Consumable c = itm as Consumable;
                if (c.cookingIngredient)
                {
                    if (ingredientIndex >= ingredients.Length) continue;
                    ingredients[ingredientIndex].gameObj.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, c.spriteRef);
                    ingredientQuantities[ingredientIndex] = c.Quantity;
                    cookingPlayerIngredientList[ingredientIndex] = itm;
                    ingredientIndex++;
                }
                if (c.seasoning)
                {
                    if (seasoningIndex >= seasoning.Length) continue;
                    seasoning[seasoningIndex].gameObj.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, c.spriteRef);
                    cookingPlayerSeasoningList[seasoningIndex] = itm;
                    seasoningIndex++;
                }
            }
        }
        UpdateCookingPlayerLists();
        singletonUIMS.EnableCursor();
        ChangeUIFocusAndAlignCursor(ingredients[0]);
    }

    public void SelectCookingIngredient(int slot)
    {        
        if (slot >= 200) return;

        GameMasterScript.gmsSingleton.SetTempGameData("repeatrecipe", 0);

        if (slot >= 100)
        {
            if (cookingPlayerSeasoningList[slot - 100] != null)
            {
                cookingSeasoningItem = cookingPlayerSeasoningList[slot - 100];
                panSeasoning.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, cookingSeasoningItem.spriteRef);
                GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("Pickup");
                UpdateCookingPlayerLists();
            }
        }
        else
        {
            // Already maxed on ingredients?
            Item theItem = cookingPlayerIngredientList[slot];
            if (theItem == null) return;

            int openSlot = -1;
            for (int i = 0; i < cookingIngredientItems.Length; i++)
            {
                if (cookingIngredientItems[i] == null)
                {
                    openSlot = i;
                    break;
                }
            }
            if (openSlot == -1)
            {
                // No open slots for ingredients?
                PlayCursorSound("Error");
                return;
            }
            cookingIngredientItems[openSlot] = theItem;

            GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("Pickup");

            switch (openSlot)
            {
                case 0:
                    panIngredient1.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, cookingIngredientItems[openSlot].spriteRef);
                    break;
                case 1:
                    panIngredient2.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, cookingIngredientItems[openSlot].spriteRef);
                    break;
                case 2:
                    panIngredient3.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, cookingIngredientItems[openSlot].spriteRef);
                    break;
            }

            UpdateCookingPlayerLists();

            bool allFull = true;
            for (int i = 0; i < cookingIngredientItems.Length; i++)
            {
                if (cookingIngredientItems[i] == null)
                {
                    allFull = false;
                    break;
                }
            }
            if (allFull)
            {
                //ChangeUIFocusAndAlignCursor(cookButton);

            }
        }
    }

    public void DragCookingItem(int slot)
    {
        if (isDraggingItem) return;

        GameMasterScript.gmsSingleton.SetTempGameData("repeatrecipe", 0);

        int actualSlot = slot;
        if (actualSlot >= 100 && actualSlot < 200)
        {
            // Drag seasoning
            actualSlot -= 100;
            if (cookingPlayerSeasoningList[actualSlot] != null)
            {
                draggingItem = cookingPlayerSeasoningList[actualSlot];
            }
            else
            {
                return;
            }

        }
        else if (actualSlot < 100)
        {
            // Drag an ingredient
            if (cookingPlayerIngredientList[slot] != null)
            {
                draggingItem = cookingPlayerIngredientList[slot];
            }
            else
            {
                return;
            }
        }
        else if (actualSlot >= 200)
        {
            // Drag a pan ingredient
            if (actualSlot == 203)
            {
                panSeasoning.GetComponent<Image>().sprite = transparentSprite;
                cookingSeasoningItem = null;
            }
            else
            {
                switch (actualSlot)
                {
                    case 200:
                        cookingIngredientItems[0] = null;
                        panIngredient1.GetComponent<Image>().sprite = transparentSprite;
                        break;
                    case 201:
                        cookingIngredientItems[1] = null;
                        panIngredient2.GetComponent<Image>().sprite = transparentSprite;
                        break;
                    case 202:
                        cookingIngredientItems[2] = null;
                        panIngredient3.GetComponent<Image>().sprite = transparentSprite;
                        break;
                }

            }
            UpdateCookingPlayerLists();
            return;
        }

        draggingItemButtonIndex = slot;
        isDraggingItem = true;
        cookingDragger.SetActive(true);
        cookingDragger.GetComponent<Image>().sprite = draggingItem.GetSpriteForUI();

    }

    public void DropCookingItem(int slot)
    {
        if (!isDraggingItem) return;

        GameMasterScript.gmsSingleton.SetTempGameData("repeatrecipe", 0);

        if (slot >= 0 && slot <= 2)
        {
            // Replace ingredient
            cookingIngredientItems[slot] = draggingItem;
            switch (slot)
            {
                case 0:
                    panIngredient1.GetComponent<Image>().sprite = LoadSpriteFromAtlas(allItemGraphics, draggingItem.spriteRef);
                    break;
                case 1:
                    panIngredient2.GetComponent<Image>().sprite = LoadSpriteFromAtlas(allItemGraphics, draggingItem.spriteRef);
                    break;
                case 2:
                    panIngredient3.GetComponent<Image>().sprite = LoadSpriteFromAtlas(allItemGraphics, draggingItem.spriteRef);
                    break;
            }
        }
        else
        {
            cookingSeasoningItem = draggingItem;
            panSeasoning.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, draggingItem.spriteRef);
            GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("Pickup");
        }

        UpdateCookingPlayerLists();

        ExitDragMode();
    }

    public void CookingIngredientClick(int slot)
    {
        SelectCookingIngredient(slot);
    }

    public void CookingIngredientHover(int slot)
    {
        if (slot == -1)
        {
            cookingResultText.text = "";
        }
        else
        {
            string txtBuilder = "";
            if ((slot >= 100) && (slot < 200))
            {
                // Hover seasoning
                int actual = slot - 100;
                if (cookingPlayerSeasoningList[actual] != null)
                {
                    txtBuilder = "<color=yellow>" + cookingPlayerSeasoningList[actual].displayName + "</color>\n\n" + cookingPlayerSeasoningList[actual].description + "\n\n" + cyanHexColor + cookingPlayerSeasoningList[actual].extraDescription + "</color>";
                    cookingResultText.text = txtBuilder;
                }
                ChangeUIFocusAndAlignCursor(seasoning[actual]);
            }
            else if ((slot >= 200) && (slot != 9999))
            {
                // Hover pan stuff
                int actual = slot - 200;
                if (actual != 3)
                {
                    if (cookingIngredientItems[actual] != null)
                    {
                        txtBuilder = "<color=yellow>" + cookingIngredientItems[actual].displayName + "</color>\n\n" + cookingIngredientItems[actual].description;
                        cookingResultText.text = txtBuilder;
                    }
                }
                else
                {
                    if (cookingSeasoningItem != null)
                    {
                        txtBuilder = "<color=yellow>" + cookingSeasoningItem.displayName + "</color>\n\n" + cookingSeasoningItem.description;
                        cookingResultText.text = txtBuilder;
                    }
                }

            }
            else if (slot == 9999)
            {
                if (cookingResultItem != null)
                {
                    txtBuilder = "<color=yellow>" + cookingResultItem.displayName + "</color>\n" + cookingResultItem.GetItemInformationNoName(true);
                    cookingResultText.text = txtBuilder;
                }
            }
            else
            {
                // Hover ingredient
                if (cookingPlayerIngredientList[slot] != null)
                {
                    txtBuilder = "<color=yellow>" + cookingPlayerIngredientList[slot].displayName + "</color>\n\n" + cookingPlayerIngredientList[slot].description;
                    cookingResultText.text = txtBuilder;
                }
                ChangeUIFocusAndAlignCursor(ingredients[slot]);
            }
        }
    }

    public void CookItemsFromButton(int dummy)
    {
        CookItems();
    }

    public void CookItems()
    {
        List<Item> ingredientsUsed = new List<Item>();
        for (int i = 0; i < cookingIngredientItems.Length; i++)
        {
            if (cookingIngredientItems[i] != null)
            {
                ingredientsUsed.Add(cookingIngredientItems[i]);
            }
        }

        // Deprecated
        if (currentConversation.whichNPC != null && currentConversation.whichNPC.actorRefName == "npc_restfire")
        {
            if (ingredientsUsed.Count == 1) // Single-item special healing recipes
            {
                Consumable createdItem = null;
                if (ingredientsUsed[0].actorRefName == "food_legofturkey" || ingredientsUsed[0].actorRefName == "food_chickendinner")
                {
                    // Create roasted meat
                    createdItem = Item.GetItemTemplateFromRef("food_campfiremeat") as Consumable;
                }
                if (ingredientsUsed[0].actorRefName == "food_finecheese" || ingredientsUsed[0].actorRefName == "food_cheesewheel")
                {
                    // Create roasted cheese
                    createdItem = Item.GetItemTemplateFromRef("food_campfirecheese") as Consumable;
                }
                if (ingredientsUsed[0].actorRefName == "food_chaiqicookies" || ingredientsUsed[0].actorRefName == "food_boxofmints")
                {
                    // Create roasted dessert
                    createdItem = Item.GetItemTemplateFromRef("food_campfiredessert") as Consumable;
                }
                if (ingredientsUsed[0].actorRefName == "food_juicyapple" || ingredientsUsed[0].actorRefName == "food_bananas")
                {
                    // Create roasted fruit
                    createdItem = Item.GetItemTemplateFromRef("food_campfirefruit") as Consumable;
                }

                if (createdItem != null)
                {
                    createdItem.SetUniqueIDAndAddToDict();
                    GameMasterScript.heroPCActor.myInventory.AddItem(createdItem, true);
                    GameMasterScript.heroPCActor.myInventory.ChangeItemQuantityAndRemoveIfEmpty(ingredientsUsed[0], -1);
                    if (GameMasterScript.heroPCActor.myInventory.GetItemQuantity(ingredientsUsed[0].actorRefName) <= 0)
                    {
                        GameMasterScript.heroPCActor.myInventory.RemoveItem(ingredientsUsed[0]);
                    }
                    CloseCookingInterface();
                    currentConversation.whichNPC.SetActorData("fireused", 1);
                    FadeOut(3f);

                    MusicManagerScript.RequestPlayNonLoopingMusicFromScratchWithCrossfade("resttheme");
                    if (GameStartData.gameInSharaMode) SharaModeStuff.WaitThenPlayNormalSharaThemeIfStillInCampfire(9f);
                    MysteryDungeonManager.PlayerRestedAtFire();
                    WaitThenFadeIn(3f, 3f);
                    return;
                }
            }
        }

        if (ingredientsUsed.Count < 2)
        {
            PlayCursorSound("Error");
            Debug.Log("Not enough ingredients.");
            return;
        }

        Item resultItem = CookingScript.EvaluateRecipes(ingredientsUsed);

        if (resultItem == null)
        {
            // Generic stew
            Item template = Item.GetItemTemplateFromRef("food_tangledeepstew");
            resultItem = new Consumable();
            resultItem.CopyFromItem(template);
            resultItem.SetUniqueIDAndAddToDict();
            GameMasterScript.gmsSingleton.statsAndAchievements.IncrementRecipesFailed();
            UIManagerScript.PlayCursorSound("CookingFailure");
        }
        else
        {
            // We good
            UIManagerScript.PlayCursorSound("CookingSuccess");
        }

        //singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("OPSelect");

        for (int i = 0; i < lastCookedItems.Length; i++)
        {
            lastCookedItems[i] = null;
        }

        for (int i = 0; i < ingredientsUsed.Count; i++)
        {
            lastCookedItems[i] = ingredientsUsed[i];
        }

        if (cookingSeasoningItem != null)
        {
            ingredientsUsed.Add(cookingSeasoningItem);
            lastCookedItems[3] = cookingSeasoningItem;
        }

        foreach (Item itm in ingredientsUsed)
        {
            Consumable c = itm as Consumable;
            c.ChangeQuantity(-1);
            if (c.Quantity == 0)
            {
                GameMasterScript.heroPCActor.myInventory.RemoveItem(c);
            }
        }

        Consumable con = resultItem as Consumable;
        if (cookingSeasoningItem != null)
        {
            Consumable cSeason = cookingSeasoningItem as Consumable;
            con.seasoningAttached = cSeason.actorRefName;
            switch (cSeason.actorRefName)
            {
                case "spice_rosepetals":
                    if (GameMasterScript.heroPCActor.ReadActorData("romanticmeal") < 2)
                    {
                        GameMasterScript.heroPCActor.SetActorData("romanticmeal", 1);
                    }                    
                    ProgressTracker.SetProgress(TDProgress.ROMANCEQUEST, ProgressLocations.META, 1);
                    break;
            }
            con.AddSeasoningToName();
        }

        Image imgComponent = cookingResultImage.GetComponent<Image>();
        imgComponent.sprite = LoadSpriteFromDict(dictItemGraphics, resultItem.spriteRef);
        cookingResultText.text = "<color=yellow>" + resultItem.displayName + "</color>\n" + resultItem.GetItemInformationNoName(true);
        cookingResultItem = resultItem;

        //immediately terminate any children that were just made for flashing.
        Image[] kiddieImages = cookingResultImage.gameObject.transform.GetComponentsInChildren<Image>();
        for (int t = 0; t < kiddieImages.Length; t++)
        {
            if (kiddieImages[t].gameObject.tag == "equipflash")
            {
                kiddieImages[t].gameObject.SetActive(false);
                kiddieImages[t].transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        // vfx!
        GameObject flashyFX = Instantiate(cookingResultImage.gameObject, cookingResultImage.gameObject.transform);
        flashyFX.tag = "equipflash";
        Destroy(flashyFX, 1.0f);

        Image flashyClone = flashyFX.GetComponent<Image>();
        flashyClone.rectTransform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        flashyClone.rectTransform.localPosition = new Vector3(0, 0, 0);

        //move to front of draw chain
        flashyClone.rectTransform.SetAsFirstSibling();

        LeanTween.scale(flashyFX, new Vector3(1.2f, 1.2f, 1.2f), 1.0f).setEaseOutElastic().setOvershoot(3.0f * 1.2f);
        

        GameMasterScript.heroPCActor.myInventory.AddItem(resultItem, true);

        for (int i = 0; i < cookingIngredientItems.Length; i++)
        {
            cookingIngredientItems[i] = null;
        }
        cookingSeasoningItem = null;

        UpdateCookingPlayerLists();

        panIngredient1.GetComponent<Image>().sprite = transparentSprite;
        panIngredient2.GetComponent<Image>().sprite = transparentSprite;
        panIngredient3.GetComponent<Image>().sprite = transparentSprite;
        panSeasoning.GetComponent<Image>().sprite = transparentSprite;

        EnableCursor();
        ShowDialogMenuCursor();

        if (GameMasterScript.gmsSingleton.ReadTempGameData("repeatrecipe") == 1)
        {
            ChangeUIFocusAndAlignCursor(cookingRepeat);
        }
        else
        {
            ChangeUIFocusAndAlignCursor(ingredients[0]);
        }

        
    }

    public void HoverRecipe(int index)
    {
        JournalScript.GetRecipeInfo(index);
    }

    public void SelectRecipeFromUI(int index)
    {
        JournalScript.GetRecipeInfoAndTryCook(index);
    }

    public void CloseCookingFromButton(int x)
    {
        singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("Cancel");
        CloseCookingInterface();
    }

    public static void CloseCookingInterface()
    {
        //GetWindowState(UITabs.COOKING) = false;
        //cookingUI.SetActive(false);

        singletonUIMS.DisableCursor();
        CleanupAfterUIClose(UITabs.COOKING);
    }

    public void CloseCookingInterfaceFromButton()
    {
        CloseCookingInterface();
    }
    public static void LocalizeCookingStrings()
    {
        string[] bestStrings =
        {
            "IngredientsHeader", "ui_cooking_ingredients_header",
            "PlaceIngredientsHeader", "ui_cooking_place_header",
            "PlaceSeasoningHeader", "misc_seasoning",
            "SeasoningHeader", "ui_cooking_seasoning_header",
            "CookButton", "ui_cook_button",
            "CookRepeatButton", "ui_cookrepeat_button",
            "CookReset", "ui_cookreset_button",
            "CookingResultText", "ui_cooking_placeholder_text"
        };
        for (int t = 0; t < bestStrings.Length; t += 2)
        {
            var cookingo = GameObject.Find(bestStrings[t]);
            var cookingtxt = cookingo.GetComponentInChildren<TextMeshProUGUI>();
            TDFonts fntType = cookingo.GetComponent<Button>() == null ? TDFonts.WHITE : TDFonts.BLACK;
            FontManager.LocalizeMe(cookingtxt, fntType);
            cookingtxt.text = StringManager.GetLocalizedStringInCurrentLanguage(bestStrings[t + 1]);
        }
    }
}
