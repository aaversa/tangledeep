using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class TDCalendarEvents {

	public static bool IsAprilFoolsDay()
    {
        return false; // For now just disable this, it causes problems. 

        if (DateTime.Now.Month == 4 && DateTime.Now.Day == 1)
        {
            return true;
        }

        return false;
    }

    public static string ProcessAprilFoolsText(string txt)
    {
        txt = txt.Replace(" she ", " he ");
        txt = txt.Replace("She ", "He ");
        txt = txt.Replace("She's", "He's");
        txt = txt.Replace("she's", "he's");
        txt = txt.Replace(" her ", " him ");
        txt = txt.Replace(" her.", " him.");
        txt = txt.Replace(" her!", " him!");
        txt = txt.Replace(" girl", " boy");
        txt = txt.Replace("Girl", "Boy");
        txt = txt.Replace("sister", "brother");
        txt = txt.Replace("Sister", "Brother");
        txt = txt.Replace("Lady", "Sir");
        txt = txt.Replace("Heroine", "Hero");
        txt = txt.Replace("heroine", "hero");
        txt = txt.Replace("miss!", "mister!");
        txt = txt.Replace("woman", "man");
        txt = txt.Replace("Katie", "Chris");
        txt = txt.Replace("KATIE", "CHRIS");
        txt = txt.Replace("Shara", "Sharo");
        txt = txt.Replace("SHARA", "SHARO");
        txt = txt.Replace("Alexis", "Alex");
        txt = txt.Replace("ALEXIS", "ALEX");
        txt = txt.Replace("Erin", "Male Erin");
        txt = txt.Replace("ERIN", "MALE ERIN");
        txt = txt.Replace("ALEXIS", "ALEX");
        txt = txt.Replace("Zephiira", "Zephiiro");
        txt = txt.Replace("ZEPHIIRA", "ZEPHIIRO");
        txt = txt.Replace("JESSE", "MALE JESSE");
        txt = txt.Replace("Jesse", "Male Jesse");
        txt = txt.Replace("Queen", "King");
        txt = txt.Replace("QUEEN", "KING");
        txt = txt.Replace("queen", "king");
        txt = txt.Replace("mother", "father");
        return txt;
    }

    public static void ProcessAprilFoolsTextForMenOnly()
    {
        if (TDCalendarEvents.IsAprilFoolsDay() && GameMasterScript.heroPCActor.ReadActorData("malemode") == 1) // A1
        {
            GameLogScript.LogWriteStringRef(UIManagerScript.greenHexColor + "Welcome to Male Mode! All characters in the game are now male.</color>");
        }
    }
}
