using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogDataPackages  {

    static Stack<LoseHPPackage> loseHPStack;
    static Stack<ChangeCoreStatPackage> changeCoreStatStack;
    static Stack<GainStatusPackage> gainStatusStack;

    static bool initialized;

    public static void Initialize()
    {
        if (initialized) return;

        loseHPStack = new Stack<LoseHPPackage>();
        changeCoreStatStack = new Stack<ChangeCoreStatPackage>();
        gainStatusStack = new Stack<GainStatusPackage>();

        for (int i = 0; i < 5; i++)
        {
            LoseHPPackage lhp = new LoseHPPackage();
            loseHPStack.Push(lhp);
            GainStatusPackage gsp = new GainStatusPackage();
            gainStatusStack.Push(gsp);
            ChangeCoreStatPackage ccsp = new ChangeCoreStatPackage();
            changeCoreStatStack.Push(ccsp);
        }

        initialized = true;
    }

    public static LoseHPPackage GetLoseHPPackage()
    {
        if (loseHPStack.Count == 0)
        {
            LoseHPPackage lhp = new LoseHPPackage();
            loseHPStack.Push(lhp);
        }

        LoseHPPackage val = loseHPStack.Pop();
        val.Initialize();

        return val;
    }

    public static GainStatusPackage GetGainStatusPackage()
    {
        if (gainStatusStack.Count == 0)
        {
            GainStatusPackage gsp = new GainStatusPackage();
            gainStatusStack.Push(gsp);
        }

        GainStatusPackage val = gainStatusStack.Pop();
        val.Initialize();

        return val;
    }

    public static ChangeCoreStatPackage GetChangeCoreStatPackage()
    {
        if (changeCoreStatStack.Count == 0)
        {
            ChangeCoreStatPackage ccsp = new ChangeCoreStatPackage();
            changeCoreStatStack.Push(ccsp);
        }

        ChangeCoreStatPackage val = changeCoreStatStack.Pop();
        val.Initialize();

        return val;
    }

    public static void ReturnToStack(LogDataPackage ldp)
    {
        switch(ldp.type)
        {
            case LogDataTypes.CHANGECORESTAT:
                changeCoreStatStack.Push(ldp as ChangeCoreStatPackage);
                break;
            case LogDataTypes.LOSEHP:
                loseHPStack.Push(ldp as LoseHPPackage);
                break;
            case LogDataTypes.GAINSTATUS:
                gainStatusStack.Push(ldp as GainStatusPackage);
                break;
        }                            
    }
}

public class LoseHPPackage : LogDataPackage
{
    public string damageEffectSource;
    public DamageTypes dType;
    public float damageAmount;
    public string abilityUser;
    public string damageSpriteString;

    public override void Initialize()
    {
        gameActor = null;
        damageEffectSource = "";
        dType = DamageTypes.PHYSICAL;
        damageAmount = 0f;
        abilityUser = "";
        damageSpriteString = "";
    }

    public LoseHPPackage()
    {
        type = LogDataTypes.LOSEHP;
    }



    public override bool CompatibleWith(LogDataPackage comparePackage)
    {
        bool baseCompatible = base.CompatibleWith(comparePackage);
        if (!baseCompatible) return baseCompatible;

        LoseHPPackage lhp = comparePackage as LoseHPPackage;

        if (damageEffectSource != lhp.damageEffectSource) return false;
        if (dType != lhp.dType) return false;
        if (damageSpriteString != lhp.damageSpriteString) return false;

        return true;
    }

    public override void CombineWith(LogDataPackage packageToAbsorb)
    {
        LoseHPPackage lhp = packageToAbsorb as LoseHPPackage;

        damageAmount += lhp.damageAmount;
    }

    public override string GetTextDisplay()
    {
        string constructor = "";
        string color = "";
        if (gameActor == GameMasterScript.heroPCActor)
        {
            color = UIManagerScript.orangeHexColor;
        }
        else
        {
            color = "<#fffb00>";
        }

        StringManager.SetTag(0, gameActor.displayName); // ^tag1^, the target
        StringManager.SetTag(2, damageEffectSource + "</color>"); // ^tag3^, the source, such as ability name
        StringManager.SetTag(1, color + (int)damageAmount + "</color>"); // ^tag2^, the damage number

        // Note: damage icons dont have any padding so we have to pad 'em
        StringManager.SetTag(4, "" + damageSpriteString + "     "); // ^tag5^, little damage icon as needed

        if (!string.IsNullOrEmpty(abilityUser))
        {
            StringManager.SetTag(3, abilityUser); // ^tag4^, the user
            constructor = StringManager.GetString("log_lose_hp_fromeffect_user");
        }
        else
        {
            constructor = StringManager.GetString("log_lose_hp_fromeffect");
        }


        //UIManagerScript.orangeHexColor + gameActor.displayName + "</color> " + StringManager.GetString("misc_loses") + " " + color + (int)damageAmount + "</color>" + " " + StringManager.GetString("misc_hpfrom") + damageEffectSource + "</color>!";
        return constructor;

        //GameLogScript.GameLogWrite(UIManagerScript.orangeHexColor + def.displayName + "</color> " + StringManager.GetString("misc_loses") + " " + color + (int)finalDamage + "</color>" + " " + StringManager.GetString("misc_hpfrom") + critText + " " + colorizedName + "</color>!",def);
    }
}

public class ChangeCoreStatPackage : LogDataPackage
{
    public float[] statChanges;
    public bool percentBased;
    public string abilityUser;
    public string effectSource;
    public List<StatTypes> statsUp;
    public List<StatTypes> statsDown;

    public override void Initialize()
    {
        gameActor = null;
        abilityUser = "";
        type = LogDataTypes.CHANGECORESTAT;
        effectSource = "";
        percentBased = false;
        for (int i = 0; i < statChanges.Length; i++)
        {
            statChanges[i] = 0f;
        }
        statsUp.Clear();
        statsDown.Clear();
    }

    public ChangeCoreStatPackage()
    {
        statChanges = new float[(int)StatBlock.expandedCoreStats.Length];
        type = LogDataTypes.CHANGECORESTAT;
        abilityUser = "";
        effectSource = "";
        statsUp = new List<StatTypes>();
        statsDown = new List<StatTypes>();
    }

    public override bool CompatibleWith(LogDataPackage comparePackage)
    {
        if (!base.CompatibleWith(comparePackage)) return false;

        ChangeCoreStatPackage ccsp = comparePackage as ChangeCoreStatPackage;

        if (abilityUser != ccsp.abilityUser) return false;
        if (effectSource != ccsp.effectSource) return false;

        return true;
    }

    public override void CombineWith(LogDataPackage packageToAbsorb)
    {
        ChangeCoreStatPackage ccsp = packageToAbsorb as ChangeCoreStatPackage;
        for (int i = 0; i < statChanges.Length; i++)
        {
            statChanges[i] += ccsp.statChanges[i];
        }
    }

    public override string GetTextDisplay()
    {
        string constructor = "";
        // Build the list of stat types affected so we have a specific count.
        statsUp.Clear();
        statsDown.Clear();

        bool onlyHealingEffect = true;
        int healAmount = 0;

        for (int i = 0; i < statChanges.Length; i++)
        {
            if (statChanges[i] > 0)
            {
                statsUp.Add(StatBlock.expandedCoreStats[i]);
                if (StatBlock.expandedCoreStats[i] != StatTypes.HEALTH) onlyHealingEffect = false;
                else
                {
                    healAmount = (int)statChanges[i];
                }
            }
            else if (statChanges[i] < 0)
            {
                statsDown.Add(StatBlock.expandedCoreStats[i]);
                onlyHealingEffect = false;
            }
        }

        StringManager.SetTag(0, gameActor.displayName);

        string extraText = "";

        if (percentBased)
        {
            extraText = StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
        }

        // Now go through that list and contruct the string.

        if (onlyHealingEffect)
        {
            StringManager.SetTag(0, abilityUser);
            StringManager.SetTag(1, gameActor.displayName); // target of healing


            StringManager.SetTag(2, healAmount.ToString());
            StringManager.SetTag(3, effectSource);

            if (string.IsNullOrEmpty(effectSource) && StringManager.gameLanguage == EGameLanguage.en_us)
            {
                // What if you are healing yourself? Don't need to list your whole name right?
                // #todo This only works in english probably?

                if (abilityUser == GameMasterScript.heroPCActor.displayName)
                {
                    StringManager.SetTag(0, "You");
                    if (gameActor == GameMasterScript.heroPCActor)
                    {
                        StringManager.SetTag(1, "yourself");
                    }
                }

                string temp = StringManager.GetString("log_actorheal_noeffect");

                temp = temp.Replace("heals", "heal");

                return temp;
            }
            else
            {
                // English only readability tweaks
                if (StringManager.gameLanguage == EGameLanguage.en_us)
                {
                    if (abilityUser == GameMasterScript.heroPCActor.displayName)
                    {
                        StringManager.SetTag(0, "Your");
                    }
                    else
                    {
                        StringManager.SetTag(0, abilityUser + "'s");
                    }
                    if (gameActor == GameMasterScript.heroPCActor)
                    {
                        StringManager.SetTag(1, "yourself");
                    }
                }

                return StringManager.GetString("log_actorheal_effect");
            }
        }
        else
        {
            if (statsUp.Count > 0)
            {
                constructor = StringManager.GetString("log_actor_gainstat_start");
                for (int i = 0; i < statsUp.Count; i++)
                {
                    string displayVal = ((int)Mathf.Abs(statChanges[(int)statsUp[i]])).ToString();

                    if (i > 0 && i != statsUp.Count - 1)
                    {
                        constructor += ",";
                    }
                    else if (i > 0 && i == statsUp.Count - 1)
                    {
                        if (statsUp.Count > 2)
                        {
                            // oxford comma only with 3+ items
                            constructor += ", " + StringManager.GetString("misc_and");
                        }
                        else
                        {
                            // no oxford comma with 2 items
                            constructor += " " + StringManager.GetString("misc_and");
                        }

                    }
                    constructor += " " + UIManagerScript.greenHexColor + displayVal + extraText + "</color> " + StatBlock.GetCoreStatString(statsUp[i]);
                }
                constructor += "!";
            }
            if (statsDown.Count > 0)
            {
                if (statsUp.Count > 0) constructor += "\n";
                constructor += StringManager.GetString("log_actor_losestat_start");
                for (int i = 0; i < statsDown.Count; i++)
                {
                    string displayVal = ((int)Mathf.Abs(statChanges[(int)statsDown[i]])).ToString();

                    if (i > 0 && i != statsDown.Count - 1)
                    {
                        constructor += ",";
                    }
                    else if (i > 0 && i == statsDown.Count - 1)
                    {
                        if (statsDown.Count > 2)
                        {
                            constructor += ", " + StringManager.GetString("misc_and");
                        }
                        else
                        {
                            constructor += " " + StringManager.GetString("misc_and");
                        }
                    }
                    constructor += " " + UIManagerScript.redHexColor + displayVal + extraText + "</color> " + StatBlock.GetCoreStatString(statsDown[i]);
                }
                constructor += "!";
            }
        }




        return constructor;
    }
}

public class GainStatusPackage : LogDataPackage
{
    public List<string> statusRefNames;

    public override void Initialize()
    {
        gameActor = null;
        statusRefNames.Clear();
        type = LogDataTypes.GAINSTATUS;
    }

    public GainStatusPackage()
    {
        statusRefNames = new List<string>();
        type = LogDataTypes.GAINSTATUS;
    }

    public override bool CompatibleWith(LogDataPackage comparePackage)
    {
        return base.CompatibleWith(comparePackage);
    }

    public override void CombineWith(LogDataPackage packageToAbsorb)
    {
        GainStatusPackage gsp = packageToAbsorb as GainStatusPackage;

        foreach (string se in gsp.statusRefNames)
        {
            if (!statusRefNames.Contains(se))
            {
                // Should we allow multiple statuses to be added at once?
                statusRefNames.Add(se);
            }
        }
    }

    public override string GetTextDisplay()
    {
        StringManager.SetTag(0, gameActor.displayName);

        string constructor = "";

        if (statusRefNames.Count == 1)
        {
            constructor = StringManager.GetString("log_gainstatus_single");
        }
        else if (statusRefNames.Count > 1)
        {
            constructor = StringManager.GetString("log_gainstatus_multiple");
        }

        for (int i = 0; i < statusRefNames.Count; i++)
        {
            if (i > 0)
            {
                if ((i >= 1) && (i == statusRefNames.Count - 1))
                {
                    // Final entry in a list of 2 or more

                    if (statusRefNames.Count > 2)
                    {
                        // Oxford comma for 3 or more
                        constructor += ",";
                    }
                    constructor += " " + StringManager.GetString("misc_and");
                }
                else if ((i >= 1) && (statusRefNames.Count > 2))
                {
                    constructor += ",";
                }
            }

            // First status in list
            constructor += " " + UIManagerScript.cyanHexColor + statusRefNames[i] + "</color>";
        }

        constructor += "!";

        return constructor;
    }
}

public class LogDataPackage
{
    public Actor gameActor;
    public LogDataTypes type;

    public virtual void Initialize()
    {
        gameActor = null;
    }

    public virtual bool CompatibleWith(LogDataPackage comparePackage)
    {
        if (type != comparePackage.type) return false;
        if (gameActor != comparePackage.gameActor) return false;

        return true;
    }

    public virtual void CombineWith(LogDataPackage packageToAbsorb)
    {

    }

    public virtual string GetTextDisplay()
    {
        string constructor = "";

        switch (type)
        {
            case LogDataTypes.CHANGECORESTAT:


                break;
        }

        return constructor;
    }
}
