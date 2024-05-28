using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChampionMod
{
    public string displayName;
    public string refName;
    public string accessoryRef;
    public float challengeValue;
    public float maxChallengeValue;
    public int exclusionGroup;
    public bool newGamePlusOnly;
    public bool displayNameOnHover;
    public List<MonsterPowerData> modPowers;

    public DamageTypes elementalAura;

    public bool shadowKingOnly;
    public bool memoryKingOnly;

    public Dictionary<string, int> metaData;

    public static List<string> modsForGodRealm = new List<string>()
    {
        "monmod_hauler",
        "monmod_banisher", // stronger ver?
        "monmod_steeltoe", // stronger ver?
        "monmod_heavy2",
        "monmod_hurricane",
        "monmod_barrier",
        "monmod_illusionist", // stronger ver?
        "monmod_harrier",
        "monmod_blinking",
        "monmod_leadtouched",
        "monmod_regenerating2",
        "monmod_resbreaker",
        "monmod_frogmaster" // stronger ver?
    };

    // you can only have one of these
    public static List<string> godMods = new List<string>()
    {
        "monmod_god_shadow",
        "monmod_god_fire",
        "monmod_god_water",
        "monmod_god_physical",
        "monmod_god_poison",
        "monmod_god_lightning"
    };

 
    public ChampionMod()
    {
        metaData = new Dictionary<string, int>();
        modPowers = new List<MonsterPowerData>();
        accessoryRef = null;
        maxChallengeValue = 99f;
        shadowKingOnly = false;
        displayNameOnHover = true;
        newGamePlusOnly = false;
        memoryKingOnly = false;
        elementalAura = DamageTypes.COUNT; // no aura by default
    }

    /// <summary>
    /// Returns TRUE if this mod is for 'god' monsters only
    /// </summary>
    /// <returns></returns>
    public bool CheckGodsOnly()
    {
        int value;
        if (metaData.TryGetValue("godsonly", out value))
        {
            return value == 1;
        }

        return false;
    }
}