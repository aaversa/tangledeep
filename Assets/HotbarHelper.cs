using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class HotbarHelper : MonoBehaviour
{
    const int HOTBAR_OFFSET = 3; // The first few slots are reserved
    public const int SLOTS_PER_HOTBAR = 8;
    const float HOTBAR_ANIM_TIME = 0.3f; // Time to complete the juiced up animation
    const float COOLDOWN_ALPHA = 0.78f; // Maximum alpha when cooldown overlay is active
    const float PX_DISTANCE_BETWEEN_HOTBAR_ROWS = 83f; // Skills 1-4 are one row, 5-8 is another, etc.

    static Image[] hotbarSkillIconObjects;
    static Image[] hotbarCooldownIconObjects;
    static Image[] hotbarNumberIconObjects;
    static RectTransform[] hotbarParentObjectTransforms;
    static Dictionary<Image, Vector2> colorFades;

    bool animationInProgress;
    float timeAtAnimationStart;

    static HotbarHelper singleton;

    void Start()
    {
        if (singleton != null && singleton != this) return;
        singleton = this;
    }
	
	public static int GetHotbarOffset() 
	{
		if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) 
		{
			return 3;
		}
		else return 0;
	}

    /// <summary>
    /// Ensures that all abilities are visible that *should* be visible. Run this on closing a FullScreenUI and on map change.
    /// </summary>
    public static void UpdateHotbarVisibility()
    {
		int offset = GetHotbarOffset();
	
        // This can be either 0 (first hotbar) or 1 (second)
        int indexOfActiveHotbar = UIManagerScript.GetIndexOfActiveHotbar();

        for (int i = offset; i < offset + (SLOTS_PER_HOTBAR*2); i++)
        {
            bool shouldBeVisible = false;
            // This index is for an ability on first hotbar
            if (i >= offset && i < offset+SLOTS_PER_HOTBAR)
            {
                shouldBeVisible = indexOfActiveHotbar == 0 ? true : false;
            }
            // This index is for an ability on second hotbar
            if (i >= offset + SLOTS_PER_HOTBAR && i < offset + (SLOTS_PER_HOTBAR * 2))
            {
                shouldBeVisible = indexOfActiveHotbar == 1 ? true : false;
            }

            int abilityIconIndex = i;
            if (offset > 0) abilityIconIndex -= 3;

            UIManagerScript.abilityIcons[abilityIconIndex].SetActive(shouldBeVisible);            
        }
    }

    /// <summary>
    /// Assigns arrays of components so we don't need to use GetComponent repeatedly.
    /// </summary>
    public static void Initialize()
    {
        if (!PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) return;

        hotbarCooldownIconObjects = new Image[SLOTS_PER_HOTBAR * 2];
        hotbarNumberIconObjects = new Image[SLOTS_PER_HOTBAR * 2];
        hotbarSkillIconObjects = new Image[SLOTS_PER_HOTBAR * 2];

        hotbarParentObjectTransforms = new RectTransform[SLOTS_PER_HOTBAR * 2];
        colorFades = new Dictionary<Image, Vector2>();

        for (int i = 0; i < UIManagerScript.abilityIcons.Length; i++)
        {
            hotbarParentObjectTransforms[i] = UIManagerScript.abilityIcons[i].GetComponent<RectTransform>();

            if (i >= SLOTS_PER_HOTBAR)
            {
                // Shift these objects down for animation juice later.
                Vector2 newPos = hotbarParentObjectTransforms[i].localPosition;
                newPos.y -= (PX_DISTANCE_BETWEEN_HOTBAR_ROWS * 2f);
                hotbarParentObjectTransforms[i].localPosition = newPos;
            }

            // Get all images and sort them out into the right array based on object name.
            Image[] allImages = UIManagerScript.abilityIcons[i].GetComponentsInChildren<Image>();
            for (int x = 0; x < allImages.Length; x++)
            {
                string objectName = allImages[x].gameObject.name;
                if (objectName.Contains("Ability"))
                {
                    hotbarSkillIconObjects[i] = allImages[x];       
                }
                else if (objectName == "Cooldown")
                {
                    hotbarCooldownIconObjects[i] = allImages[x];
                }
                else
                {
                    hotbarNumberIconObjects[i] = allImages[x];
                }

                colorFades.Add(allImages[x], Vector2.zero);
            }
        }        
    }

    /// <summary>
    /// Returns TRUE if player has anything (skill or item) bound in the second hotbar.
    /// </summary>
    /// <returns></returns>
    public static bool AnyHotbarBindablesOnSecondHotbar()
    {
        HotbarBindable[] allAbilities = UIManagerScript.GetHotbarAbilities();

        for (int i = SLOTS_PER_HOTBAR; i < allAbilities.Length; i++) 
        {
            if (allAbilities[i] != null && allAbilities[i].actionType != HotbarBindableActions.NOTHING)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Does color fading for icons, cooldown, etc.
    /// </summary>
    void Update()
    {
        if (!PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) return;

        if (!animationInProgress) return;

        float percentCompletion = (Time.time - timeAtAnimationStart) / HOTBAR_ANIM_TIME;
        if (percentCompletion >= 1f)
        {
            animationInProgress = false;
        }

        foreach (Image fadingImage in colorFades.Keys)
        {
            float alphaValue = Mathf.Lerp(colorFades[fadingImage].x, colorFades[fadingImage].y, percentCompletion);
            fadingImage.color = new Color(1f, 1f, 1f, alphaValue);
        }
    }

    /// <summary>
    /// Cycles the hotbar from first to second or vice versa, with juice & coroutine. Handles shadow objects & cursor too. Does it all!
    /// </summary>
    /// <param name="direction">-1 means we pushed up from the top row, 1 is down from bottom</param>
    /// <returns></returns>
    public static IEnumerator AnimateHotbarSwitch(int direction)
    {
        // Determine current cursor position, we'll need this later.
        int indexOfCurrentSelectedAbility = 0;
        for (int i = 0; i < UIManagerScript.hudHotbarAbilities.Length; i++)
        {
            if (UIManagerScript.uiObjectFocus == UIManagerScript.hudHotbarAbilities[i])
            {
                indexOfCurrentSelectedAbility = i;
                break;
            }
        }

        // SFX juice to initiate the move
        UIManagerScript.PlayCursorSound("StartDrag");

        // This can be either 0 (first hotbar) or 1 (second)
        // At this point, the index has already changed, the animation is playing catch up.
        int indexOfActiveHotbar = UIManagerScript.GetIndexOfActiveHotbar();

        // If we're moving from 0 to 1:
        // First hotbar objects move UP and fade out
        // Second hotbar objects move UP and fade in

        // If we're moving from 1 to 0:
        // First hotbar objects move DOWN and fade in
        // Second hotbar objects move DOWN and fade out

        // Make sure all our game objects are active, and set image/cooldown alphas appropriately
        // colorFades dictionary will be used in Update to change alphas from (start) to (finish) values

        for (int i = 0; i < UIManagerScript.abilityIcons.Length; i++)
        {
            // All objects must be enabled for this to work.
            UIManagerScript.abilityIcons[i].SetActive(true);

            Color colorForImageObjects = Color.white;

            if (indexOfActiveHotbar == 1) // Going from first hotbar to second.
            {
                if (i >= SLOTS_PER_HOTBAR) // Second hotbar starts from nothing, fades in
                {
                    colorForImageObjects.a = 0f;
                }
            }
            else // Going from second hotbar to first
            {
                if (i < SLOTS_PER_HOTBAR) // First hotbar starts from nothing, fades in.
                {
                    colorForImageObjects.a = 0f;
                }
            }

            // Number icons should be hidden on Switch
            colorFades[hotbarNumberIconObjects[i]] = new Vector2(0f, 0f);

            // Hide skill icons if they are unbound.
            if (UIManagerScript.hotbarAbilities[i] == null || UIManagerScript.hotbarAbilities[i].actionType == HotbarBindableActions.NOTHING)
            {
                colorFades[hotbarSkillIconObjects[i]] = new Vector2(0f, 0f);
            }
            else // Otherwise, it's white
            {
                colorFades[hotbarSkillIconObjects[i]] = new Vector2(colorForImageObjects.a, 1f - colorForImageObjects.a);
            }
            
            // But cooldown fade values must be managed carefully, as they aren't always min to max value.
            // It depends on the hotbar bindable cooldown state.
            if (GetCooldownByHotbarIndex(i) > 0)
            {
                float originAlpha = colorForImageObjects.a;
                if (originAlpha != 0f) // Don't divide by zero.
                {
                    originAlpha = COOLDOWN_ALPHA / colorForImageObjects.a;
                }
                float destinationAlpha = 1f - colorForImageObjects.a;
                if (destinationAlpha != 0f) // Don't divide by zero.
                {
                    destinationAlpha = COOLDOWN_ALPHA / destinationAlpha;
                }

                colorFades[hotbarCooldownIconObjects[i]] = new Vector2(originAlpha, destinationAlpha);
            }
            else
            {
                // Bindable is not on cooldown, so keep cooldown alpha at 0.
                colorFades[hotbarCooldownIconObjects[i]] = new Vector2(0f, 0f);
            }
            
        }

        // All objects are enabled with the right color, now to move them around.

        for (int i = 0; i < SLOTS_PER_HOTBAR*2; i++)
        {
            RectTransform rtParentToMove = hotbarParentObjectTransforms[i];
            Vector2 endPos = rtParentToMove.localPosition;

            if (indexOfActiveHotbar == 1) // Switched to second hotbar
            {
                // So everything moves UP
                endPos.y += PX_DISTANCE_BETWEEN_HOTBAR_ROWS * 2f;
            }
            else // Switched to first hotbar
            {
                // So everything moves DOWN
                endPos.y -= PX_DISTANCE_BETWEEN_HOTBAR_ROWS * 2f;
            }

            var tween = LeanTween.move(rtParentToMove, endPos, HOTBAR_ANIM_TIME).setEase(LeanTweenType.easeOutQuint);
        }

        singleton.animationInProgress = true;
        singleton.timeAtAnimationStart = Time.time;

        // Hide the cursor temporarily, since it looks jank otherwise.
        UIManagerScript.singletonUIMS.uiDialogMenuCursor.SetActive(false);

        yield return new WaitForSeconds(HOTBAR_ANIM_TIME);

        UIManagerScript.singletonUIMS.RefreshAbilityCooldowns();

        // Quick SFX now that we've locked in place
        UIManagerScript.PlayCursorSound("UITick");

        // Make sure our shadow objects are linked up and cursor ends up in the right spot.
        yield return SetupPostCycleHotbarNavigation();

        // Now put our cursor in the right place. We move in chunks of 4 because there are 4 abilities per row.
        int newCursorIndex = indexOfCurrentSelectedAbility;
        switch(direction)
        {
            case -1: // We pushed up, so we should end up on the bottom row
                newCursorIndex -= SLOTS_PER_HOTBAR / 2;
                break;
            case 1:
                newCursorIndex += SLOTS_PER_HOTBAR / 2;
                break;
        }

        // Wrapping logic if we exceed bounds.
        if (newCursorIndex < HOTBAR_OFFSET) 
        {
            // This would happen if we pushed UP when in the top row, and we want to wrap to the bottom.
            newCursorIndex = SLOTS_PER_HOTBAR * 2 + newCursorIndex;
        }
        else if (newCursorIndex >= SLOTS_PER_HOTBAR*2 + HOTBAR_OFFSET)
        {
            newCursorIndex -= SLOTS_PER_HOTBAR * 2;
        }

        UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.hudHotbarAbilities[newCursorIndex]);

        // Show cursor in its correct new position.
        UIManagerScript.singletonUIMS.uiDialogMenuCursor.SetActive(true);

        bool clearInfo = true;

        // Refresh tooltip if cursor is on a slot with an ability.
        for (int i = 0; i < UIManagerScript.hudHotbarAbilities.Length; i++)
        {
            if (UIManagerScript.uiObjectFocus == UIManagerScript.hudHotbarAbilities[i])
            {
                UIManagerScript.singletonUIMS.MouseoverFetchAbilityInfo(i - 3);
                clearInfo = false;
                break;
            }
        }


        if (clearInfo)
        {
            // If not, clear the tooltip.
            UIManagerScript.singletonUIMS.MouseoverFetchAbilityInfo(-1); // clear
        }

        // wait a frame, then make sure abilities that shouldn't be shown are completely hidden.
        yield return null;

        UpdateHotbarVisibility();

    }

    /// <summary>
    /// Returns the current cooldown turns of the HotbarBindable in the desired slot (0-15). Returns 0 for non-bound.
    /// </summary>
    /// <param name="index">Hotbar slot to check</param>
    /// <returns></returns>
    static int GetCooldownByHotbarIndex(int index)
    {
        HotbarBindable hb = UIManagerScript.hotbarAbilities[index];
        if (hb == null || hb.actionType == HotbarBindableActions.NOTHING)
        {
            return 0;
        }
        switch (hb.actionType)
        {
            case HotbarBindableActions.ABILITY:
                return hb.ability.GetCurCooldownTurns();              
            case HotbarBindableActions.CONSUMABLE:
            default:
                return 0;
        }
    }

    /// <summary>
    /// Setup shadow object navigation/enabled status, put cursor in the right place, enable tooltip if needed.
    /// </summary>
    /// <returns></returns>
    static IEnumerator SetupPostCycleHotbarNavigation()
    {
        switch (UIManagerScript.GetIndexOfActiveHotbar())
        {
            case 0:
                for (int i = HOTBAR_OFFSET; i < HOTBAR_OFFSET + SLOTS_PER_HOTBAR; i++)
                {
                    UIManagerScript.hudHotbarAbilities[i].enabled = true;
                }
                for (int i = HOTBAR_OFFSET + SLOTS_PER_HOTBAR; i < HOTBAR_OFFSET + (SLOTS_PER_HOTBAR * 2); i++)
                {
                    UIManagerScript.hudHotbarAbilities[i].enabled = false;
                }


            if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE)
            {
                UIManagerScript.hudHotbarAbilities[0].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarSkill8;
            }

                break;
            case 1:
                for (int i = HOTBAR_OFFSET; i < HOTBAR_OFFSET+SLOTS_PER_HOTBAR; i++)
                {
                    UIManagerScript.hudHotbarAbilities[i].enabled = false;
                }
                for (int i = HOTBAR_OFFSET+SLOTS_PER_HOTBAR; i < HOTBAR_OFFSET+(SLOTS_PER_HOTBAR*2); i++)
                {
                    UIManagerScript.hudHotbarAbilities[i].enabled = true;
                }

if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE)
{
                UIManagerScript.hudHotbarAbilities[0].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarSkill16;
}
                break;
        }

        yield break;
    }

    /// <summary>
    /// Check if 0-indexed hotbar slot is visible, or if we are looking at the other hotbar.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public static bool IsHotbarSlotInView(int index)
    {
        int indexOfActiveHotbar = UIManagerScript.GetIndexOfActiveHotbar();
        if ((indexOfActiveHotbar == 0 && index < HotbarHelper.SLOTS_PER_HOTBAR) || (indexOfActiveHotbar == 1 && index >= HotbarHelper.SLOTS_PER_HOTBAR))
        {
            return true;
        }
        return false;
    }
}
