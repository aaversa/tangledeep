using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AutoEatFoodLogic 
{
    public const int AUTO_EAT_INTERVAL = 10;
    public const float MIN_HEALTH_EAT = 0.5f;

    public static void CheckForAndTryAutoEat()
    {
        if (!PlayerOptions.autoEatFood)
        {
            //Debug.Log("Options off.");
            return;
        }

        if (GameMasterScript.turnNumber - GameMasterScript.heroPCActor.lastTurnDamaged < AUTO_EAT_INTERVAL ||
            GameMasterScript.heroPCActor.TurnsSinceLastCombatAction < AUTO_EAT_INTERVAL)
        {
            //Debug.Log("Too soon. " + GameMasterScript.turnNumber + " dmged " + GameMasterScript.heroPCActor.lastTurnDamaged + " last cmb: " + GameMasterScript.heroPCActor.TurnsSinceLastCombatAction);
            return;
        }

        if (GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) > MIN_HEALTH_EAT)
        {
            //Debug.Log("Health above min");
            return;
        }

        if (!JobTrialScript.CanPlayerUseConsumable())
        {
            //Debug.Log("No job trial");
            return;
        }

        Item toEat = null;
        if (!CanEatFoodThisTurn(out toEat))
        {
            //Debug.Log("Full or nothing to eat");
            return;
        }

        GameMasterScript.gmsSingleton.ActuallyUseConsumable(toEat as Consumable);
    }

    static bool CanEatFoodThisTurn(out Item restorativeToUse)
    {
        restorativeToUse = null;
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_foodfull")) return false;

        if (GameMasterScript.heroPCActor.myInventory.HasAnyNonSpicedNonInstantRestorativeFood(out restorativeToUse))
        {
            Item curry = null;
            if (GameMasterScript.heroPCActor.myInventory.GetCurry(out curry))
            {
                restorativeToUse = curry;
            }
            return true;
        }

        return false;
    }
}
