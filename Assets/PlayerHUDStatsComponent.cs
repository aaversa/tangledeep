using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUDStatsComponent : MonoBehaviour {

	public Image playerStaminaFill;
    public Image playerStaminaReservedFill;
    public Image playerHealthFill;
    public Image playerEnergyFill;
    public Image playerEnergyReservedFill;

    public Image playerLimitBreakFill;
    public GameObject limitBreakContainer;

    static PlayerHUDStatsComponent singleton;

    private void Start()
    {
        if (singleton != null && singleton != this) return;

        singleton = this;
        playerStaminaReservedFill.fillAmount = 0f;
        playerEnergyReservedFill.fillAmount = 0f;

        limitBreakContainer.SetActive(false);
    }

    public static void ToggleLimitBreak(bool state)
    {
        singleton.limitBreakContainer.SetActive(state);
    }

    public static void SetLimitBreakAmount(float amt)
    {
        float maxFillAmount = HeroPC.PERCENT_OF_HEALTH_LIMITBREAK;

        maxFillAmount -= (GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("xp2_dragons") * 0.05f);

        amt = (amt / maxFillAmount); 

        float prevAmount = singleton.playerLimitBreakFill.fillAmount;

        if (amt > 1f)
        {
            amt = 1f;
        }
        singleton.playerLimitBreakFill.fillAmount = amt;

        if (prevAmount < 1f && amt == 1f)
        {
            GameMasterScript.heroPCActor.OnLimitBreakReached();
        }
    }

    public static void RefreshReservedEnergy(float energyReserved, float maxEnergy)
    {
        float percentOfMaxReserved = energyReserved / maxEnergy;
        singleton.playerEnergyReservedFill.fillAmount = percentOfMaxReserved;
    }

    public static void RefreshReservedStamina(float staminaReserved, float maxStamina)
    {
        float percentOfMaxReserved = staminaReserved / maxStamina;
        singleton.playerStaminaReservedFill.fillAmount = percentOfMaxReserved;
    }
}

public partial class UIManagerScript
{
    public PlayerHUDStatsComponent hudPlayerStats;
}
