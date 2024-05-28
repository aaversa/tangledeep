using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BossHealthBarScript : MonoBehaviour {

    static GameObject bossHealthBar;
    static Image bossHealthBarFill;
    static TextMeshProUGUI bossHealthBarText;
    public static bool healthBarShouldBeActive;

    static float timeAtAnimStart;
    const float ANIM_FILL_TIME = 1.0f;
    public static bool fillingHealthBarAnimation;
    static Monster lastUsedMonster;

    // Use this for initialization
    void Start () {
        bossHealthBar = GameObject.Find("BossHealthBar");
        bossHealthBarFill = GameObject.Find("BossHealthBarFill").GetComponent<Image>();
        bossHealthBarText = GameObject.Find("BossHealthBarText").GetComponent<TextMeshProUGUI>();

        FontManager.LocalizeMe(bossHealthBarText, TDFonts.WHITE);
        bossHealthBar.SetActive(false);
        lastUsedMonster = null;
    }

    public static void ToggleBossHealthBar(bool state)
    {       
        bossHealthBar.SetActive(state);        
        if (state && lastUsedMonster != null)
        {
            bossHealthBarText.text = lastUsedMonster.displayName;
        }
    }

    public static void DisableBoss()
    {
        healthBarShouldBeActive = false;
        ToggleBossHealthBar(false);
    }

    public static void SetBossHealthText(string text)
    {
        bossHealthBarText.text = text;
    }

    public static void SetBossHealthFill(float amount)
    {
        bossHealthBarFill.fillAmount = amount;
        if (bossHealthBarFill.fillAmount <= 0f) // hide the bar if boss is defeated
        {
            DisableBoss();
        }
    }

    public static void EnableBoss(Monster mn)
    {
        if (mn.sleepUntilSeehero) return;
        if (!mn.myStats.IsAlive())
        {
            DisableBoss();
            return;
        }
        if (mn.actorfaction == Faction.PLAYER) return;

        ToggleBossHealthBar(true);
        
        healthBarShouldBeActive = true;

        bossHealthBarText.text = mn.displayName;
        bossHealthBarFill.fillAmount = mn.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH);
        lastUsedMonster = mn;
    }

    public static void EnableBossWithAnimation(Monster mn)
    {
        fillingHealthBarAnimation = true;
        timeAtAnimStart = Time.time;
        EnableBoss(mn);
        bossHealthBarFill.fillAmount = 0f;
        lastUsedMonster = mn;
    }

    void Update()
    {
        if (fillingHealthBarAnimation)
        {
            float complete = (Time.time - timeAtAnimStart) / ANIM_FILL_TIME;
            if (complete >= 1.0f)
            {
                complete = 1f;
                fillingHealthBarAnimation = false;
            }
            bossHealthBarFill.fillAmount = complete;
        }
    }
}
