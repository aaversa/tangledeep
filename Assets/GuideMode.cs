using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   

public class GuideMode : MonoBehaviour
{
    public GameObject snackBagUIPulse;
    public ImagePulse snackPulseSettings;

    public GameObject flaskUIPulse;
    public ImagePulse flaskPulseSettings;

    public static GuideMode singleton;

    public const float HEALTH_THRESHOLD_FOR_UI_PULSE = 0.6f;

    public const float LOWEST_HEALTH_THRESHOLD_FOR_UI_PULSE = 0.33f;

    const float CYCLE_TIME_REGULAR = 0.6f;
    const float CYCLE_TIME_URGENT = 0.3f;

    static bool foodPulseEnabled;
    static bool flaskPulseEnabled;

    private void Start()
    {
        singleton = this;
    }

    public static void OnFullScreenUIOpened(bool heroMightNotExist = false)
    {
        if (!PlayerOptions.showUIPulses || PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) return;
        if (GameMasterScript.gmsSingleton.titleScreenGMS) return;
        if (singleton == null) return;
        singleton.snackBagUIPulse.SetActive(false);
        singleton.flaskUIPulse.SetActive(false);

        //Debug.Log("Pulses disabled");
    }

    public static void WaitThenOnFullScreenUIClosed(float time)
    {
        GameMasterScript.gmsSingleton.StartCoroutine(_WaitThenOnFullScreenUIClosed(time));
    }

    static IEnumerator _WaitThenOnFullScreenUIClosed(float time)
    {
        yield return time;
        OnFullScreenUIClosed();
    }

    public static void OnFullScreenUIClosed()
    {
        if (!PlayerOptions.showUIPulses || PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) return;
        if (GameMasterScript.gmsSingleton.titleScreenGMS) return;
        if (singleton == null) return;
        singleton.snackBagUIPulse.SetActive(foodPulseEnabled);
        singleton.flaskUIPulse.SetActive(flaskPulseEnabled);

        //Debug.Log("Pulses enabled");
    }

    public static void ToggleFlaskUIPulse(bool state)
    {
        if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) return;

        if (SharaModeStuff.IsSharaModeActive()) state = false;

        singleton.flaskUIPulse.SetActive(state);

        flaskPulseEnabled = state;
    }

    public static void ToggleSnackBagUIPulse(bool state)
    {
        if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) return;

        if (SharaModeStuff.IsSharaModeActive()) state = false;

        singleton.snackBagUIPulse.SetActive(state);

        foodPulseEnabled = state;
    }

    public static void CheckIfFoodAndFlaskShouldBeConsumedAndToggleIndicator()
    {
        if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) return;

        if (SharaModeStuff.IsSharaModeActive()) return;

        if (!PlayerOptions.showUIPulses)
        {
            singleton.flaskUIPulse.SetActive(false);
            singleton.snackBagUIPulse.SetActive(false);
            return;
        }

        float percent = GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH);
        bool needHealing = CheckIfHealingNeeded();

        CheckFoodPulseIndicator(needHealing, percent);

        CheckFlaskPulseIndicator(needHealing, percent);
    }

    public static void CalculateThenCheckFoodPulse()
    {
        if (!GameMasterScript.actualGameStarted || PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) return;

        if (!PlayerOptions.showUIPulses || SharaModeStuff.IsSharaModeActive())
        {            
            singleton.snackBagUIPulse.SetActive(false);
            return;
        }

        float percent = GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH);
        bool needHealing = CheckIfHealingNeeded();

        CheckFoodPulseIndicator(needHealing, percent);
    }

    public static void CalculateThenCheckFlaskPulse()
    {        
        if (!PlayerOptions.showUIPulses || PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE || SharaModeStuff.IsSharaModeActive())
        {
            singleton.flaskUIPulse.SetActive(false);
            return;
        }

        float percent = GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH);
        bool needHealing = CheckIfHealingNeeded();

        CheckFlaskPulseIndicator(needHealing, percent);
    }

    static void CheckFlaskPulseIndicator(bool needHealing, float percent)
    {
        bool canUseFlask = GameMasterScript.heroPCActor.regenFlaskUses > 0 && !GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_regenflask");
        if (needHealing)
        {
            if (canUseFlask)
            {
                if (!flaskPulseEnabled)
                {
                    flaskPulseEnabled = true;
                    singleton.flaskUIPulse.SetActive(true);
                }

                if (percent > LOWEST_HEALTH_THRESHOLD_FOR_UI_PULSE) singleton.flaskPulseSettings.cycleTime = CYCLE_TIME_REGULAR;
                else singleton.flaskPulseSettings.cycleTime = CYCLE_TIME_URGENT;
            }
            else
            {
                flaskPulseEnabled = false;
                singleton.flaskUIPulse.SetActive(false);
            }
        }
        else if (!needHealing && flaskPulseEnabled)
        {
            singleton.flaskUIPulse.SetActive(false);
            flaskPulseEnabled = false;
        }
    }

    static void CheckFoodPulseIndicator(bool needHealing, float percent)
    {
        if (needHealing)
        {
            if (CanFoodOrPotionsBeConsumed())
            {
                if (!foodPulseEnabled)
                {
                    foodPulseEnabled = true;
                    singleton.snackBagUIPulse.SetActive(true);
                }

                if (percent > LOWEST_HEALTH_THRESHOLD_FOR_UI_PULSE) singleton.snackPulseSettings.cycleTime = CYCLE_TIME_REGULAR;
                else singleton.snackPulseSettings.cycleTime = CYCLE_TIME_URGENT;
            }
            else
            {
                foodPulseEnabled = false;
                singleton.snackBagUIPulse.SetActive(false);
            }
        }
        else if (!needHealing && foodPulseEnabled)
        {
            singleton.snackBagUIPulse.SetActive(false);
            foodPulseEnabled = false;
        }
    }

    public static bool CheckIfHealingNeeded()
    {
        if (GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) > HEALTH_THRESHOLD_FOR_UI_PULSE) return false;
        
        return true;
    }

    static bool anyPotionsOrFood = false;

    public static bool CanFoodOrPotionsBeConsumed()
    {
        List<Item> heroItems = GameMasterScript.heroPCActor.myInventory.GetInventory();

        bool isFull = GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_foodfull");

        if (!GameMasterScript.heroPCActor.myInventory.consumableItemListDirty) return anyPotionsOrFood;

        GameMasterScript.heroPCActor.myInventory.consumableItemListDirty = false;

        foreach (Item itm in heroItems)
        {
            // Can't eat if we can't eat...
            if (isFull && itm.IsItemFood()) continue;

            if (itm.IsCurative(StatTypes.HEALTH))
            {
                anyPotionsOrFood = true;
                return true;
            }
        }

        anyPotionsOrFood = false;

        return false;
    }
}
