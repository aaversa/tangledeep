using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EPediaRestrictions { SHARA_ONLY, MIRAI_ONLY, DLC1, DLC2, COUNT }

/// <summary>
/// This class manually sorts the Monsterpedia and flag monsters as Mirai mode, Shara mode, or both
/// </summary>
public class BakedMonsterpedia {

    // Monsters will be displayed in the order they are added to this list.
    public static List<MonsterpediaDef> monsterPedia = new List<MonsterpediaDef>()
    {
        // level 1 
        new MonsterpediaDef("mon_mossjelly", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_salamander", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_grottoflyer", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_scythemantis", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),

        // level 2 
        new MonsterpediaDef("mon_slimerat", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_firejelly", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_electricjelly", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_creepingspider", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_toxicurchin", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_scorpionturtle", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),

        // level 3
        new MonsterpediaDef("mon_fungaltoad", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_pirahnas", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_redcrab", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_bigurchin", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_cannondrone", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),

        // level 4
        new MonsterpediaDef("mon_jadebeetle", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_plunderer", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_cavelion", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_saboteur", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_watertentacle", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),

        // level 5
		// Switch has DLC1 built in.        
#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
        new MonsterpediaDef("mon_alchemist", new List<EPediaRestrictions>()),
#endif
        new MonsterpediaDef("mon_livingvine", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_swamptoad", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_hoverbot", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_xp_pistolshrimp", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),

        // level 6
		// Switch has DLC1 built in.
#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
        new MonsterpediaDef("mon_mottledsandjaw", new List<EPediaRestrictions>()),
#endif
        new MonsterpediaDef("mon_youngwaterelemental", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_rockviper", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_rockviperlava", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_bandithunter", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_fungalcolumn", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_iceviper", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_banditwarlord", new List<EPediaRestrictions>(){ EPediaRestrictions.MIRAI_ONLY }), // 1st boss

        // level 7
        new MonsterpediaDef("mon_youngfireelemental", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_jadesalamander", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_robotsnake", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_guardiansphere", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_banditbrigand", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_vinestalker", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_berserker_panthox", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_xp_chonkfrog", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        
        // level 8
        new MonsterpediaDef("mon_ghostsamurai", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_guardianseeker", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_younglightningelemental", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_komodon", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_guardianspider", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_banditspellshaper", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_banditbuffslinger", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_greenmantis", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_scientist_summoner", new List<EPediaRestrictions>() { EPediaRestrictions.SHARA_ONLY, EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_shadowelementalboss", new List<EPediaRestrictions>(){EPediaRestrictions.MIRAI_ONLY }), // 2nd boss

        // level 9
        new MonsterpediaDef("mon_banditswordmaster", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_rockgolem", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_poisonelemental", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_banebite", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_xp_bouldertoad", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),

        // level 10
        new MonsterpediaDef("mon_mossbeast", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_frostedjelly", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_darkcavelion", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_phasmaturret", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_barriercrystal", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_infernospirit", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_xp_bladefrog", new List<EPediaRestrictions>() {EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_toxicslime", new List<EPediaRestrictions>() {EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_heavygolem", new List<EPediaRestrictions>() { EPediaRestrictions.SHARA_ONLY, EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_ancientsteamgolem", new List<EPediaRestrictions>() { EPediaRestrictions.MIRAI_ONLY }), // 3rd boss? old?
        new MonsterpediaDef("mon_xp_cheftoad", new List<EPediaRestrictions>() {EPediaRestrictions.DLC2 }),
        new MonsterpediaDef("mon_xp_skysnake", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),
        new MonsterpediaDef("mon_xp_spectralcrab", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),
        
        // level 11
        new MonsterpediaDef("mon_destroyer", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_guardianorbiter", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_heavygolem", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_banditsniper", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_neutralizer", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_xp_chameleon", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_verminesper", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),
        new MonsterpediaDef("mon_xp_slimedungeon_enemy_slime_1", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2, EPediaRestrictions.MIRAI_ONLY }),

        // level 12
        new MonsterpediaDef("mon_xp_venomancer", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_axewielder", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_shovelgolem", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_budoka", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_sideareaboss4", new List<EPediaRestrictions>()), // side area boss 4
        new MonsterpediaDef("mon_finalbossai", new List<EPediaRestrictions>() { EPediaRestrictions.MIRAI_ONLY }), // 4th boss phase 1
        new MonsterpediaDef("mon_xp_prismslime", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),
        new MonsterpediaDef("mon_xp_treefrog", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),
        new MonsterpediaDef("mon_hornetnest", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),
        new MonsterpediaDef("mon_warhornet", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),

        // level 13
        new MonsterpediaDef("mon_phasmacannon", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_boltspirit", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_mimic", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_ghostsamurai2", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),
        new MonsterpediaDef("mon_xp_icespirit", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),
        new MonsterpediaDef("mon_xp_acidelemental", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),
        new MonsterpediaDef("mon_xp_banditwrangler", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),
        new MonsterpediaDef("mon_xp_metalslime", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2 }),

        // level 14
        new MonsterpediaDef("mon_xp_robotmantis", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_powerorbiter", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_guardianseeker2", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_staffbandit", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_cubeslime", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_komodon2", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_butterfly", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_finalboss2", new List<EPediaRestrictions>() { EPediaRestrictions.MIRAI_ONLY }), // 4th boss phase 2

        // level 15        
        new MonsterpediaDef("mon_xp_batslime", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_chemist2", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_xp_spiritstag", new List<EPediaRestrictions>() { EPediaRestrictions.DLC1 }),
        new MonsterpediaDef("mon_prototype_husyn", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2, EPediaRestrictions.MIRAI_ONLY}),
        new MonsterpediaDef("mon_dimriftboss", new List<EPediaRestrictions>() { EPediaRestrictions.MIRAI_ONLY }), // dreamcaster boss

        // level 25 / special
        new MonsterpediaDef("mon_goldfrog", new List<EPediaRestrictions>()),
        new MonsterpediaDef("mon_darkfrog", new List<EPediaRestrictions>()),

        new MonsterpediaDef("mon_frogdragon", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2, EPediaRestrictions.MIRAI_ONLY}),
        new MonsterpediaDef("mon_banditdragon", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2, EPediaRestrictions.MIRAI_ONLY}),
        new MonsterpediaDef("mon_beastdragon", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2, EPediaRestrictions.MIRAI_ONLY}),
        new MonsterpediaDef("mon_jellydragon", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2, EPediaRestrictions.MIRAI_ONLY}),
        new MonsterpediaDef("mon_xp_spiritdragon", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2, EPediaRestrictions.MIRAI_ONLY}),
        new MonsterpediaDef("mon_robotdragon", new List<EPediaRestrictions>() { EPediaRestrictions.DLC2, EPediaRestrictions.MIRAI_ONLY}),

        // dragons

    };

    static List<MonsterTemplateData> monstersInPedia;

    /// <summary>
    /// We don't need to rebuild the whole pedia unless this variable does NOT match the current game state (shara vs. mirai, DLC1 etc)
    /// </summary>
    static bool[] requirementsMetAndCached;

    static bool[] checkLocalRequirements;

    /// <summary>
    /// Returns list of all monsters that THIS save file should have access to
    /// </summary>
    /// <returns></returns>
    public static List<MonsterTemplateData> GetAllMonstersInPedia()
    {
        bool gameStateDirty = false;

        if (monstersInPedia == null)
        {
            monstersInPedia = new List<MonsterTemplateData>();
            requirementsMetAndCached = new bool[(int)EPediaRestrictions.COUNT];
            checkLocalRequirements = new bool[(int)EPediaRestrictions.COUNT];
            gameStateDirty = true; // always rebuild pedia on first load
        }

        // Calculate what requirements we have met for the current game state        
        // first clear our pooled array of bools
        for (int i = 0; i < requirementsMetAndCached.Length; i++)
        {
            checkLocalRequirements[i] = false;
        }
        
        if (SharaModeStuff.IsSharaModeActive())
        {
            checkLocalRequirements[(int)EPediaRestrictions.SHARA_ONLY] = true;
            checkLocalRequirements[(int)EPediaRestrictions.DLC1] = true;
        }
        else
        {
            // if we're not in shara mode, we MUST be in mirai mode
            checkLocalRequirements[(int)EPediaRestrictions.MIRAI_ONLY] = true;
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            checkLocalRequirements[(int)EPediaRestrictions.DLC1] = true;
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            checkLocalRequirements[(int)EPediaRestrictions.DLC2] = true;
        }

        // If there are any discrepancies between what we just checked and the cached value, rebuild the pedia
        for (int i = 0; i < requirementsMetAndCached.Length; i++)
        {
            if (requirementsMetAndCached[i] != checkLocalRequirements[i])
            {
                gameStateDirty = true;
                requirementsMetAndCached[i] = checkLocalRequirements[i]; // update the cache
            }
        }

        if (!gameStateDirty)
        {
            return monstersInPedia;
        }


        // if we've made it this far, rebuild the pedia
        monstersInPedia.Clear();

        foreach(MonsterpediaDef mDef in monsterPedia)
        {
            bool monsterValid = true;
            foreach(EPediaRestrictions epr in mDef.allRestrictions)
            {
                if (!requirementsMetAndCached[(int)epr])
                {
                    monsterValid = false;
                    continue; // move on if we don't meet this requirement
                }
            }
            if (!monsterValid)
            {
                continue;
            }
            if (!GameMasterScript.masterMonsterList.ContainsKey(mDef.monsterRef))
            {
                Debug.Log("Can't add monster " + mDef.monsterRef + " to the pedia, as it does not exist");
                continue;
            }
            MonsterTemplateData mtd = GameMasterScript.masterMonsterList[mDef.monsterRef];
            monstersInPedia.Add(mtd);
        }

        return monstersInPedia;
    }


}

public class MonsterpediaDef
{
    public string monsterRef;
    public List<EPediaRestrictions> allRestrictions;

    public MonsterpediaDef(string mRef, List<EPediaRestrictions> restrictions)
    {
        monsterRef = mRef;
        allRestrictions = restrictions;
    }
}