using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

// Our "bones" data that stores info about how a character was lost - level, name, job
// Finishing blow / creature etc. We can then store a bunch of these in the MetaProgress file.

public class DefeatData {

    public string charName;
    public CharacterJobs charJob;
    public int xpLevel;
    public int highestFloor;
    public string areaName;
    public string whoKilled;
    public int monstersDefeated;
    public int champsDefeated;
    public int stepsTaken;
    public string dateAndTime;

    StringBuilder reusableStringBuilder;
    bool sbInitialized;


    public void InitializeFromHeroData()
    {
        string regex = "(\\<.*\\>)";

        if (GameMasterScript.heroPCActor.whoKilledMe != null)
        {
            whoKilled = GameMasterScript.heroPCActor.whoKilledMe.displayName;
            whoKilled = whoKilled.Replace("</color>", "");
            whoKilled = Regex.Replace(whoKilled, regex, "");
        }
        else
        {
            whoKilled = "???";
        }

        charName = GameMasterScript.heroPCActor.displayName;
        areaName = UIManagerScript.uiDungeonName.text;
        areaName = areaName.Replace("</color>", "");
        areaName = Regex.Replace(areaName, regex, "");

        charJob = GameMasterScript.heroPCActor.myJob.jobEnum;
        xpLevel = GameMasterScript.heroPCActor.myStats.GetLevel();
        stepsTaken = GameMasterScript.heroPCActor.stepsTaken;
        monstersDefeated = GameMasterScript.heroPCActor.monstersKilled;
        champsDefeated = GameMasterScript.heroPCActor.championsKilled;
        highestFloor = GameMasterScript.heroPCActor.lowestFloorExplored + 1;

        //CultureInfo culture = new CultureInfo("en-US");
        CultureInfo culture = StringManager.GetCurrentCulture();
        DateTime localDate = DateTime.Now;
        dateAndTime = localDate.ToString(culture);
    }

    public void WriteToXml(XmlWriter writer)
    {        
        if (!sbInitialized)
        {
            reusableStringBuilder = new StringBuilder();
            sbInitialized = true;
        }
        reusableStringBuilder.Length = 0;
        reusableStringBuilder.Append(charName + "|");
        reusableStringBuilder.Append(areaName + "|");
        reusableStringBuilder.Append(whoKilled + "|");
        reusableStringBuilder.Append((int)charJob + "|");
        reusableStringBuilder.Append(xpLevel + "|");
        reusableStringBuilder.Append(highestFloor + "|");
        reusableStringBuilder.Append(stepsTaken + "|");
        reusableStringBuilder.Append(monstersDefeated + "|");
        reusableStringBuilder.Append(champsDefeated + "|");
        reusableStringBuilder.Append(dateAndTime);
        writer.WriteElementString("defeat", reusableStringBuilder.ToString());
    }

    public void ReadFromXml(XmlReader reader)
    {
        string unparsed = reader.ReadElementContentAsString();
        string[] parsed = unparsed.Split('|');
        if (parsed.Length != 10)
        {
            // Mismatch with what was expected from WriteToXml method above.
            return;
        }
        charName = parsed[0];
        areaName = parsed[1];
        whoKilled = parsed[2];
        int iParse;
        Int32.TryParse(parsed[3], out iParse);
        charJob = (CharacterJobs)iParse;
        Int32.TryParse(parsed[4], out xpLevel);
        Int32.TryParse(parsed[5], out highestFloor);
        Int32.TryParse(parsed[6], out stepsTaken);
        Int32.TryParse(parsed[7], out monstersDefeated);
        Int32.TryParse(parsed[8], out champsDefeated);
        dateAndTime = parsed[9];
    }

    public static void WriteAllDataToSave(XmlWriter writer)
    {
        foreach(DefeatData dd in MetaProgressScript.defeatHistory)
        {
            dd.WriteToXml(writer);
        }
    }

    public string GetPrintableString()
    {
        if (!sbInitialized)
        {
            reusableStringBuilder = new StringBuilder();
            sbInitialized = true;
        }

        reusableStringBuilder.Length = 0;

        reusableStringBuilder.Append("<size=46>");
        reusableStringBuilder.Append(charName);
        reusableStringBuilder.Append("</size>\n\n");

        StringManager.SetTag(0, whoKilled);
        StringManager.SetTag(1, areaName);

        reusableStringBuilder.Append(StringManager.GetString("desc_defeated_actor"));
        reusableStringBuilder.Append("\n");

        StringManager.SetTag(0, CharacterJobData.GetJobDataByEnum((int)charJob).DisplayName);
        StringManager.SetTag(1, xpLevel.ToString());

        reusableStringBuilder.Append(StringManager.GetString("desc_died_joblevel"));
        reusableStringBuilder.Append("\n");

        StringManager.SetTag(0, highestFloor.ToString());

        reusableStringBuilder.Append(StringManager.GetString("desc_died_highestfloor"));
        reusableStringBuilder.Append("\n");

        StringManager.SetTag(0, monstersDefeated.ToString());

        reusableStringBuilder.Append(StringManager.GetString("desc_died_monstersdefeated"));
        reusableStringBuilder.Append("\n");

        StringManager.SetTag(0, champsDefeated.ToString());
        reusableStringBuilder.Append(StringManager.GetString("desc_died_championsdefeated"));
        reusableStringBuilder.Append("\n\n");

        reusableStringBuilder.Append(UIManagerScript.greenHexColor);
        reusableStringBuilder.Append(dateAndTime);
        reusableStringBuilder.Append("</color>\n\n");

        return reusableStringBuilder.ToString();
    }

}
