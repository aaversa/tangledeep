using System.Collections;
using UnityEngine;

public class CSStatBars : DynamicHeroStatUIElement
{

    [Header("Resource and Health Bars")]
    public UIFilledBarScript healthBar;
    public UIFilledBarScript staminaBar;
    public UIFilledBarScript energyBar;

    [Header("Bar Filling Tweaks")]
    public float BarFillDuration;

    [Tooltip("Health fills first, then stamina, then energy. This is the delay between each bar's starting.")]
    public float DelayBetweenBarFills;

    //This is set to false when we are filling the bars during load time.
    private bool bDisplayLiveInfo;

    protected override void UpdateInfo()
    {
        //if we have no hero, well that's too bad
        if (!bHeroHasBeenSet)
        {
            return;
        }

        if (!bDisplayLiveInfo)
        {
            return;
        }

        float curHealth = myHero.myStats.GetCurStat(StatTypes.HEALTH);
        float maxHealth = myHero.myStats.GetMaxStat(StatTypes.HEALTH);

        healthBar.UpdateFillAmount(curHealth / maxHealth);
        healthBar.SetTextValue((int)curHealth + " / " + (int)maxHealth);

        float curStamina = myHero.myStats.GetCurStat(StatTypes.STAMINA);
        float maxStamina = myHero.myStats.GetMaxStat(StatTypes.STAMINA);

        staminaBar.UpdateFillAmount(curStamina / maxStamina);
        staminaBar.SetTextValue((int)curStamina + " / " + (int)maxStamina);

        float curEnergy = myHero.myStats.GetCurStat(StatTypes.ENERGY);
        float maxEnergy = myHero.myStats.GetMaxStat(StatTypes.ENERGY);

        energyBar.UpdateFillAmount(curEnergy / maxEnergy);
        energyBar.SetTextValue((int)curEnergy + " / " + (int)maxEnergy);
    }

    IEnumerator FillBarOverTime( float fWaitBeforeStart, float fFillDuration, float currentValue, float maxValue, UIFilledBarScript bar )
    {
        //Clear the bar at start
        bar.UpdateFillAmount(0f);
        bar.SetTextValue("0 / " + (int)maxValue);

        yield return new WaitForSeconds(fWaitBeforeStart);
        float fTime = 0f;
        while (fTime < fFillDuration)
        {
            float fRatio = fTime / fFillDuration;
            fRatio = Mathfx.Sinerp(0f, 1.0f, fRatio);
            bar.UpdateFillAmount(fRatio * (currentValue/maxValue));

            int iAmount = (int)(currentValue * fRatio);
            bar.SetTextValue(iAmount + " / " + (int)maxValue);
            fTime += Time.deltaTime;

            yield return null;
        }
    }

    IEnumerator FillAllBarsAtStart()
    {
        bDisplayLiveInfo = false;

        float curHealth = myHero.myStats.GetCurStat(StatTypes.HEALTH);
        float maxHealth = myHero.myStats.GetMaxStat(StatTypes.HEALTH);
        float curStamina = myHero.myStats.GetCurStat(StatTypes.STAMINA);
        float maxStamina = myHero.myStats.GetMaxStat(StatTypes.STAMINA);
        float curEnergy = myHero.myStats.GetCurStat(StatTypes.ENERGY);
        float maxEnergy = myHero.myStats.GetMaxStat(StatTypes.ENERGY);

        StartCoroutine(FillBarOverTime(0f, BarFillDuration, curHealth, maxHealth, healthBar));
        StartCoroutine(FillBarOverTime(DelayBetweenBarFills, BarFillDuration, curStamina, maxStamina, staminaBar));
        StartCoroutine(FillBarOverTime(DelayBetweenBarFills * 2.0f, BarFillDuration, curEnergy, maxEnergy, energyBar));

        //Wait until last bar is full
        yield return new WaitForSeconds(BarFillDuration + DelayBetweenBarFills * 2.0f);

        bDisplayLiveInfo = true;
    }

    public override void TurnOff()
    {
        if (!bIsActive)
        {
            return;
        }
        bIsActive = false;
        gameObject.SetActive(false);
    }

    public override void TurnOn()
    {
        if (bIsActive)
        {
            return;
        }
        base.TurnOn();

        StartCoroutine(FillAllBarsAtStart());
    }

    void Update()
    {
        UpdateInfo();
    }
}
