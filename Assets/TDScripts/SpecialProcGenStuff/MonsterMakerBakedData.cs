using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MonsterMaker
{
    static void ReadMonsterNameElements()
    {
        Dictionary<EMonsterNameElements, int> numNamesPerElementList = new Dictionary<EMonsterNameElements, int>()
        {
            { EMonsterNameElements.SLIMESUFFIX, 4 },
            { EMonsterNameElements.BANDITPREFIX, 7 },
            { EMonsterNameElements.INSECTSUFFIX, 4 },
            { EMonsterNameElements.ROBOTSUFFIX, 5 },
            { EMonsterNameElements.SPIRITSUFFIX, 9 },
            { EMonsterNameElements.SPIRITPREFIX, 7 },
            { EMonsterNameElements.HYBRIDSUFFIX, 3 },
            { EMonsterNameElements.FROGSUFFIX, 5 },
            { EMonsterNameElements.BEASTPREFIX, 11 },
            { EMonsterNameElements.ROBOTPREFIX, 6 },
            { EMonsterNameElements.COLOREDPREFIX, 6 },
            { EMonsterNameElements.SLIMEPREFIX, 10 },
            { EMonsterNameElements.WEAKFIREPREFIX, 4 },
            { EMonsterNameElements.STRONGFIREPREFIX, 3 },
            { EMonsterNameElements.WEAKWATERPREFIX, 6 },
            { EMonsterNameElements.STRONGWATERPREFIX, 4 },
            { EMonsterNameElements.WEAKPOISONPREFIX, 4 },
            { EMonsterNameElements.STRONGPOISONPREFIX, 3 },
            { EMonsterNameElements.WEAKSHADOWPREFIX, 4 },
            { EMonsterNameElements.STRONGSHADOWPREFIX, 4 },
            { EMonsterNameElements.WEAKLIGHTNINGPREFIX, 3 },
            { EMonsterNameElements.STRONGLIGHTNINGPREFIX, 3 },
            { EMonsterNameElements.SUPPORTERWORD, 4 },
            { EMonsterNameElements.SUMMONERWORD, 3 },
            { EMonsterNameElements.RANGEDWORD, 5 },
            { EMonsterNameElements.STATUSUSERWORD, 3 },
            { EMonsterNameElements.MELEEWORD, 4 },
            { EMonsterNameElements.BEASTSUFFIX, 6 },
            { EMonsterNameElements.SNAKESUFFIX, 7 },
            { EMonsterNameElements.GRASSTREEWORD, 6 },
            { EMonsterNameElements.FROGPREFIX, 9 },
            { EMonsterNameElements.HYBRIDPREFIX, 3 },
            { EMonsterNameElements.INSECTPREFIX, 5 },
            { EMonsterNameElements.BANDITSUFFIX, 11 },
            { EMonsterNameElements.FROGCONCATPREFIX, 9 },
            { EMonsterNameElements.FROGCONCATSUFFIX, 7 },
            { EMonsterNameElements.BEASTCONCATPREFIX, 6 },
            { EMonsterNameElements.BEASTCONCATSUFFIX, 8 },
            { EMonsterNameElements.SLIMECONCATPREFIX, 8 },
            { EMonsterNameElements.SLIMECONCATSUFFIX, 5 },
            { EMonsterNameElements.ROBOTCONCATPREFIX, 7 },
            { EMonsterNameElements.ROBOTCONCATSUFFIX, 3 },
            { EMonsterNameElements.NATURALGROWTHPREFIX, 6 },
            { EMonsterNameElements.NATURALGROWTHSUFFIX, 6 },
            { EMonsterNameElements.SPIRITCONCATPREFIX, 9 },
            { EMonsterNameElements.SPIRITCONCATSUFFIX, 8 },
            { EMonsterNameElements.DISRUPTORWORD, 4 }
        };

        dictMonsterNameElements = new Dictionary<EMonsterNameElements, List<string>>();

        for (int i = 0; i < (int)EMonsterNameElements.COUNT; i++)
        {
            EMonsterNameElements nameElem = (EMonsterNameElements)i;
            string strVersion = nameElem.ToString().ToLowerInvariant();
            
            dictMonsterNameElements.Add(nameElem, new List<string>());

            for (int x = 1; x <= numNamesPerElementList[nameElem]; x++)
            {                
                dictMonsterNameElements[nameElem].Add(StringManager.GetString("exp_monmaker_" + strVersion + x));
            }
        }
    }

    static void CreateMonsterArchetypes()
    {
        dictMonsterArchetypes = new Dictionary<string, MonsterArchetypes>();

        MonsterArchetypes none = new MonsterArchetypes("none");
        dictMonsterArchetypes.Add("none", none);

        MonsterArchetypes fire = new MonsterArchetypes("fire");
        fire.forceWeaponElement = DamageTypes.FIRE;
        fire.minPowersRequiredByTag[(int)EMonsterPowerTags.FIRE] = 1;
        fire.monAttributes[(int)MonsterAttributes.LOVESLAVA] = 100;
        fire.armorID = "armor_firearmor";
        fire.weakNameElement = EMonsterNameElements.WEAKFIREPREFIX;
        fire.strongNameElement = EMonsterNameElements.STRONGFIREPREFIX;
        fire.namingElementIsPrefix = true;
        fire.elementalPrefabTag = EPrefabExtraTags.FIRE;
        dictMonsterArchetypes.Add("fire", fire);

        MonsterArchetypes water = new MonsterArchetypes("water");
        water.forceWeaponElement = DamageTypes.WATER;
        water.minPowersRequiredByTag[(int)EMonsterPowerTags.WATER] = 1;
        water.armorID = "armor_waterarmor";
        water.weakNameElement = EMonsterNameElements.WEAKWATERPREFIX;
        water.strongNameElement = EMonsterNameElements.STRONGWATERPREFIX;
        water.namingElementIsPrefix = true;
        water.elementalPrefabTag = EPrefabExtraTags.WATER;
        dictMonsterArchetypes.Add("water", water);

        MonsterArchetypes lightning = new MonsterArchetypes("lightning");
        lightning.forceWeaponElement = DamageTypes.LIGHTNING;
        lightning.minPowersRequiredByTag[(int)EMonsterPowerTags.LIGHTNING] = 1;
        lightning.armorID = "armor_shockarmor";
        lightning.weakNameElement = EMonsterNameElements.WEAKLIGHTNINGPREFIX;
        lightning.strongNameElement = EMonsterNameElements.STRONGLIGHTNINGPREFIX;
        lightning.namingElementIsPrefix = true;
        lightning.elementalPrefabTag = EPrefabExtraTags.LIGHTNING;
        dictMonsterArchetypes.Add("lightning", lightning);

        MonsterArchetypes poison = new MonsterArchetypes("poison");
        poison.forceWeaponElement = DamageTypes.POISON;
        poison.minPowersRequiredByTag[(int)EMonsterPowerTags.POISON] = 1;
        poison.monAttributes[(int)MonsterAttributes.LOVESMUD] = 100;
        poison.armorID = "exp_armor_poisonarmor";
        poison.weakNameElement = EMonsterNameElements.WEAKPOISONPREFIX;
        poison.strongNameElement = EMonsterNameElements.STRONGPOISONPREFIX;
        poison.namingElementIsPrefix = true;
        poison.elementalPrefabTag = EPrefabExtraTags.POISON;
        dictMonsterArchetypes.Add("poison", poison);

        MonsterArchetypes shadow = new MonsterArchetypes("shadow");
        shadow.forceWeaponElement = DamageTypes.SHADOW;
        shadow.minPowersRequiredByTag[(int)EMonsterPowerTags.SHADOW] = 1;
        shadow.armorID = "armor_shadowarmor";
        shadow.weakNameElement = EMonsterNameElements.WEAKSHADOWPREFIX;
        shadow.strongNameElement = EMonsterNameElements.STRONGSHADOWPREFIX;
        shadow.namingElementIsPrefix = true;
        shadow.elementalPrefabTag = EPrefabExtraTags.SHADOW;
        dictMonsterArchetypes.Add("shadow", shadow);

        MonsterArchetypes healer = new MonsterArchetypes("healer");
        healer.weaponPowerMod = 0.9f;
        healer.minPowersRequiredByTag[(int)EMonsterPowerTags.HEALING] = 1;
        healer.monAttributes[(int)MonsterAttributes.HEALER] = 90;
        healer.monAttributes[(int)MonsterAttributes.SUPPORTER] = 3;
        healer.statMods[(int)StatTypes.HEALTH] = 0.9f;
        healer.statMods[(int)StatTypes.STRENGTH] = 0.85f;
        healer.weakNameElement = EMonsterNameElements.SUPPORTERWORD;
        healer.strongNameElement = EMonsterNameElements.SUPPORTERWORD;
        dictMonsterArchetypes.Add("healer", healer);

        MonsterArchetypes disruptor = new MonsterArchetypes("disruptor");
        disruptor.forceRangedWeapon = true;
        disruptor.minPowersRequiredByType[(int)EMonsterPowerType.DEBUFF] = 1;
        disruptor.minPowersRequiredByType[(int)EMonsterPowerType.PULL] = 1;
        disruptor.statMods[(int)StatTypes.HEALTH] = 0.9f;
        disruptor.statMods[(int)StatTypes.STRENGTH] = 0.85f;
        disruptor.statMods[(int)StatTypes.SWIFTNESS] = 0.85f;
        disruptor.weakNameElement = EMonsterNameElements.DISRUPTORWORD;
        disruptor.strongNameElement = EMonsterNameElements.DISRUPTORWORD;
        dictMonsterArchetypes.Add("disruptor", disruptor);

        MonsterArchetypes buffer = new MonsterArchetypes("buffer");
        buffer.weaponPowerMod = 0.85f;
        buffer.statMods[(int)StatTypes.STRENGTH] = 0.85f;
        buffer.minPowersRequiredByType[(int)EMonsterPowerType.HEALBUFF] = 1;
        buffer.minPowersRequiredByType[(int)EMonsterPowerType.DEBUFF] = 1;
        buffer.monAttributes[(int)MonsterAttributes.HEALER] = 90;
        buffer.monAttributes[(int)MonsterAttributes.SUPPORTER] = 3;
        buffer.statMods[(int)StatTypes.HEALTH] = 0.85f;
        buffer.weakNameElement = EMonsterNameElements.STATUSUSERWORD;
        buffer.strongNameElement = EMonsterNameElements.STATUSUSERWORD;
        dictMonsterArchetypes.Add("buffer", buffer);

        MonsterArchetypes bruiser = new MonsterArchetypes("bruiser");
        bruiser.weaponPowerMod = 1.15f;
        bruiser.forceMeleeWeapon = true;
        bruiser.statMods[(int)StatTypes.STRENGTH] = 1.1f;
        bruiser.statMods[(int)StatTypes.HEALTH] = 1.1f;
        bruiser.statMods[(int)StatTypes.GUILE] = 0.9f;
        bruiser.statMods[(int)StatTypes.SWIFTNESS] = 0.9f;
        bruiser.minPowersRequiredByType[(int)EMonsterPowerType.DAMAGE] = 1;
        bruiser.minPowersRequiredByType[(int)EMonsterPowerType.PASSIVE] = 1;
        bruiser.powerPool.Add("skill_bullrush");
        bruiser.powerPool.Add("skill_weakbullrush");
        bruiser.powerPool.Add("skill_leap");
        bruiser.powerPool.Add("skill_froghop");
        bruiser.powerPool.Add("skill_bullrush");
        bruiser.powerPool.Add("skill_jadebeetlerush");
        bruiser.minPowersRequiredFromPool = 1;
        bruiser.weakNameElement = EMonsterNameElements.MELEEWORD;
        bruiser.strongNameElement = EMonsterNameElements.MELEEWORD;
        dictMonsterArchetypes.Add("bruiser", bruiser);

        MonsterArchetypes sniper = new MonsterArchetypes("sniper");
        sniper.forceRangedWeapon = true;
        sniper.weaponPowerMod = 0.9f;
        sniper.statMods[(int)StatTypes.HEALTH] = 0.9f;
        sniper.statMods[(int)StatTypes.STRENGTH] = 0.85f;
        sniper.statMods[(int)StatTypes.SWIFTNESS] = 1.1f;
        sniper.statMods[(int)StatTypes.GUILE] = 1.1f;
        sniper.monAttributes[(int)MonsterAttributes.SNIPER] = 100;
        sniper.minPowersRequiredByTag[(int)EMonsterPowerTags.RANGEDDAMAGE] = 1;
        sniper.weakNameElement = EMonsterNameElements.RANGEDWORD;
        sniper.strongNameElement = EMonsterNameElements.RANGEDWORD;
        dictMonsterArchetypes.Add("sniper", sniper);

        MonsterArchetypes summoner = new MonsterArchetypes("summoner");
        summoner.minPowersRequiredByTag[(int)EMonsterPowerTags.ANYSUMMON] = 1;
        //summoner.minPowersRequiredByType[(int)EMonsterPowerType.SUMMONHAZARD] = 1;
        summoner.monAttributes[(int)MonsterAttributes.HEALER] = 90;
        summoner.monAttributes[(int)MonsterAttributes.SUPPORTER] = 50;
        summoner.weakNameElement = EMonsterNameElements.SUMMONERWORD;
        summoner.strongNameElement = EMonsterNameElements.SUMMONERWORD;
        dictMonsterArchetypes.Add("summoner", summoner);

        monsterArchetypes = new ActorTable();
        monsterArchetypes.AddToTable("none", 120);
        monsterArchetypes.AddToTable("fire", 10);
        monsterArchetypes.AddToTable("water", 10);
        monsterArchetypes.AddToTable("poison", 10);
        monsterArchetypes.AddToTable("shadow", 10);
        monsterArchetypes.AddToTable("lightning", 10);
        monsterArchetypes.AddToTable("summoner", 15);
        monsterArchetypes.AddToTable("sniper", 15);
        monsterArchetypes.AddToTable("bruiser", 25);
        monsterArchetypes.AddToTable("disruptor", 15);
        monsterArchetypes.AddToTable("healer", 15);
        monsterArchetypes.AddToTable("buffer", 15);
    }

    static void CreateMonsterPowerData()
    {
        // Start by adding basic power info
        monsterPowerMasterList = new Dictionary<string, MonsterPowerDataTemplate>()
        {
            { "skill_slimehop", new MonsterPowerDataTemplate("skill_slimehop", EMonsterPowerType.MOVESELF, 2) },
            { "skill_slimesummonreaction", new MonsterPowerDataTemplate("skill_slimesummonreaction", EMonsterPowerType.PASSIVE, 2) },
            { "skill_firebreath1", new MonsterPowerDataTemplate("skill_firebreath1", EMonsterPowerType.DAMAGE, true, 1, 4, 1, 10) },
            { "skill_grottosting", new MonsterPowerDataTemplate("skill_grottosting", EMonsterPowerType.DAMAGE, 1, 4) },
            { "skill_stealfood", new MonsterPowerDataTemplate("skill_stealfood", EMonsterPowerType.DEBUFF, 1, 1) },
            { "skill_fireclaw", new MonsterPowerDataTemplate("skill_fireclaw", EMonsterPowerType.SUMMONHAZARD, 1, 3) },
            { "skill_smalllightningcircle", new MonsterPowerDataTemplate("skill_smalllightningcircle", EMonsterPowerType.DAMAGE, true, 1, 1, 1, 6) },
            { "skill_moldmindcontrol", new MonsterPowerDataTemplate("skill_moldmindcontrol", EMonsterPowerType.PULL, 2, 4, 1, 10) },
            { "skill_venombite", new MonsterPowerDataTemplate("skill_venombite", EMonsterPowerType.DAMAGE, 1, 1, 3) },
            { "skill_froghop", new MonsterPowerDataTemplate("skill_froghop", EMonsterPowerType.MOVESELF, 2) },
            { "skill_fungalregen", new MonsterPowerDataTemplate("skill_fungalregen", EMonsterPowerType.HEALBUFF) },
            { "skill_fishrush", new MonsterPowerDataTemplate("skill_fishrush", EMonsterPowerType.MOVESELF, 2) },
            { "skill_bloodlust", new MonsterPowerDataTemplate("skill_bloodlust", EMonsterPowerType.PASSIVE) },
            { "skill_fishbleedproc", new MonsterPowerDataTemplate("skill_fishbleedproc", EMonsterPowerType.PASSIVE) },
            { "skill_crabgrab", new MonsterPowerDataTemplate("skill_crabgrab", EMonsterPowerType.DEBUFF, 1, 1, 3) },
            { "skill_jadebeetlerush", new MonsterPowerDataTemplate("skill_jadebeetlerush", EMonsterPowerType.MOVESELF, true, 2, 99, 5) },
            { "skill_vinepull", new MonsterPowerDataTemplate("skill_vinepull", EMonsterPowerType.PULL, 2, 5, 5) },
            { "skill_weakbullrush", new MonsterPowerDataTemplate("skill_weakbullrush", EMonsterPowerType.PUSH, 1, 1, 3) },
            { "skill_lightningline", new MonsterPowerDataTemplate("skill_lightningline", EMonsterPowerType.DAMAGE, true, 1, 4, 4) },
            { "skill_alchemygas", new MonsterPowerDataTemplate("skill_alchemygas", EMonsterPowerType.SUMMONHAZARD, 1, 2, 5) },
            { "skill_chemistheal", new MonsterPowerDataTemplate("skill_chemistheal", EMonsterPowerType.HEALBUFF, 1, 4) },
            { "skill_dodgebuff", new MonsterPowerDataTemplate("skill_dodgebuff", EMonsterPowerType.HEALBUFF) },
            { "skill_poisonproc", new MonsterPowerDataTemplate("skill_poisonproc", EMonsterPowerType.PASSIVE) },
            { "skill_kunaitoss", new MonsterPowerDataTemplate("skill_kunaitoss", EMonsterPowerType.DAMAGE, 2, 5, 5) },
            { "skill_bullrush", new MonsterPowerDataTemplate("skill_bullrush", EMonsterPowerType.PUSH, 1, 1, 5) },
            { "skill_clawrake", new MonsterPowerDataTemplate("skill_clawrake", EMonsterPowerType.DAMAGE, true, 1, 1, 1, 8) },
            { "skill_wateryheal", new MonsterPowerDataTemplate("skill_wateryheal", EMonsterPowerType.HEALBUFF, 1, 99) },
            { "skill_watercross", new MonsterPowerDataTemplate("skill_watercross", EMonsterPowerType.DAMAGE, true, 1, 3, 1, 12) },
            { "skill_watershot", new MonsterPowerDataTemplate("skill_watershot", EMonsterPowerType.DAMAGE, 2, 3, 5) },
            { "skill_rockbite", new MonsterPowerDataTemplate("skill_rockbite", EMonsterPowerType.DAMAGE, 1, 1) },
            { "skill_lavabite", new MonsterPowerDataTemplate("skill_lavabite", EMonsterPowerType.DAMAGE, 1, 1) },
            { "skill_sharkroar", new MonsterPowerDataTemplate("skill_sharkroar", EMonsterPowerType.DEBUFF, 2, 5) },
            { "skill_leap", new MonsterPowerDataTemplate("skill_leap", EMonsterPowerType.MOVESELF, 2) },
            { "skill_bullseye", new MonsterPowerDataTemplate("skill_bullseye", EMonsterPowerType.DEBUFF, 2) },
            { "skill_shadowsnipe", new MonsterPowerDataTemplate("skill_shadowsnipe", EMonsterPowerType.DAMAGE, true, 2, 5, 6) },
            { "skill_bedofflames", new MonsterPowerDataTemplate("skill_bedofflames", EMonsterPowerType.SUMMONHAZARD, 1, 3, 7) },
            { "skill_flameskin", new MonsterPowerDataTemplate("skill_flameskin", EMonsterPowerType.PASSIVE, 1, 1, 5) },
            { "skill_acidspit", new MonsterPowerDataTemplate("skill_acidspit", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange: 2, maxRange: 99, minLevel: 4) },
            { "skill_acidcloud", new MonsterPowerDataTemplate("skill_acidcloud", EMonsterPowerType.SUMMONHAZARD, 1, 4, 6) },
            { "skill_thornhide1", new MonsterPowerDataTemplate("skill_thornhide1", EMonsterPowerType.PASSIVE, 1, 1, 1, 3) },
            { "skill_thornhide2", new MonsterPowerDataTemplate("skill_thornhide2", EMonsterPowerType.PASSIVE, 1, 1, 3, 6) },
            { "skill_poisonthorn", new MonsterPowerDataTemplate("skill_poisonthorn", EMonsterPowerType.DAMAGE, 1, 5, 8) },
            { "skill_summonsentrybot", new MonsterPowerDataTemplate("skill_summonsentrybot", EMonsterPowerType.SUMMONPET, 1, 4, 10) },
            { "skill_fireburst", new MonsterPowerDataTemplate("skill_fireburst", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange: 1, maxRange: 3, minLevel: 8) },
            { "skill_phasmabolt", new MonsterPowerDataTemplate("skill_phasmabolt", EMonsterPowerType.DAMAGE, 2, 5, 10) },
            { "skill_fireburstranged", new MonsterPowerDataTemplate("skill_fireburstranged", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange: 2, maxRange: 99, minLevel: 10) },
            { "skill_powershield", new MonsterPowerDataTemplate("skill_powershield", EMonsterPowerType.HEALBUFF, 1, 99, 6) },
            { "skill_vinebullrush", new MonsterPowerDataTemplate("skill_vinebullrush", EMonsterPowerType.PUSH, 1, 1, 5) },
            { "skill_roborepair", new MonsterPowerDataTemplate("skill_roborepair", EMonsterPowerType.HEALBUFF, 1, 1, 6) },
            { "skill_lasershot", new MonsterPowerDataTemplate("skill_lasershot", EMonsterPowerType.DAMAGE, 1, 5, 6) },
            { "skill_haunt", new MonsterPowerDataTemplate("skill_haunt", EMonsterPowerType.DEBUFF, 1, 4, 7) },
            { "skill_lasershot2", new MonsterPowerDataTemplate("skill_lasershot2", EMonsterPowerType.DAMAGE, 1, 4, 8) },
            { "skill_elemdebuff1", new MonsterPowerDataTemplate("skill_elemdebuff1", EMonsterPowerType.DEBUFF, 1, 4, 5) },
            { "skill_vortex", new MonsterPowerDataTemplate("skill_vortex", EMonsterPowerType.PULL, 2, 5, 6) },
            { "skill_runthrough", new MonsterPowerDataTemplate("skill_runthrough", EMonsterPowerType.DAMAGE, 1, 1, 6) },
            { "skill_smokebomb", new MonsterPowerDataTemplate("skill_smokebomb", EMonsterPowerType.MOVESELF, 1, 4) },
            { "skill_thornedskin", new MonsterPowerDataTemplate("skill_thornedskin", EMonsterPowerType.PASSIVE, 1, 1, 7) },
            { "skill_webshot", new MonsterPowerDataTemplate("skill_webshot", EMonsterPowerType.DEBUFF, 2, 5, 3) },
            { "skill_venombite2", new MonsterPowerDataTemplate("skill_venombite2", EMonsterPowerType.DAMAGE, 1, 1, 4) },
            { "skill_laserclaw", new MonsterPowerDataTemplate("skill_laserclaw", EMonsterPowerType.DAMAGE, true, 1, 4) },
            { "skill_vault", new MonsterPowerDataTemplate("skill_vault", EMonsterPowerType.MOVESELF, 2, 99, 5) },
            { "skill_vanishing25", new MonsterPowerDataTemplate("skill_vanishing25", EMonsterPowerType.PASSIVE, 1, 1, 5) },
            { "skill_shadowbolt", new MonsterPowerDataTemplate("skill_shadowbolt", EMonsterPowerType.DAMAGE, 2, 5, 5) },
            { "skill_bladewave", new MonsterPowerDataTemplate("skill_bladewave", EMonsterPowerType.SUMMONHAZARD, 1, 3, 9) },
            { "skill_banditshrapnelbomb", new MonsterPowerDataTemplate("skill_banditshrapnelbomb", EMonsterPowerType.SUMMONHAZARD, 1, 3, 7) },
            { "skill_iaijutsu", new MonsterPowerDataTemplate("skill_iaijutsu", EMonsterPowerType.DAMAGE, isChargeAbility:true, minRange:1, maxRange: 2, minLevel: 6) },
            { "skill_flameslash", new MonsterPowerDataTemplate("skill_flameslash", EMonsterPowerType.SUMMONHAZARD, 2, 4, 8) },
            { "skill_parry20", new MonsterPowerDataTemplate("skill_parry20", EMonsterPowerType.PASSIVE, 1, 1, 7) },
            { "skill_fly", new MonsterPowerDataTemplate("skill_fly", EMonsterPowerType.MOVESELF, 2, 99, 6) },
            { "skill_removedebuffs", new MonsterPowerDataTemplate("skill_removedebuffs", EMonsterPowerType.HEALBUFF, 1, 5, 3) },
            { "skill_simplewarp", new MonsterPowerDataTemplate("skill_simplewarp", EMonsterPowerType.MOVESELF, 1, 99, 8) },
            { "skill_lightningcircle", new MonsterPowerDataTemplate("skill_lightningcircle", EMonsterPowerType.DAMAGE, 1, 3, 4) },
            { "skill_staticcharge", new MonsterPowerDataTemplate("skill_staticcharge", EMonsterPowerType.HEALBUFF, 1, 6, 6) },
            { "skill_thickhide", new MonsterPowerDataTemplate("skill_thickhide", EMonsterPowerType.PASSIVE, 1, 1, 8) },
            { "skill_sandbreath", new MonsterPowerDataTemplate("skill_sandbreath", EMonsterPowerType.DEBUFF, 1, 4) },
            { "skill_linecharge", new MonsterPowerDataTemplate("skill_linecharge", EMonsterPowerType.DAMAGE, isChargeAbility:true, minRange:1, maxRange: 7, minLevel:8) },
            { "skill_monshadowevocation", new MonsterPowerDataTemplate("skill_monshadowevocation", EMonsterPowerType.DAMAGE, 2, 4, 5) },
            { "skill_icecone", new MonsterPowerDataTemplate("skill_icecone", EMonsterPowerType.DAMAGE, 1, 2, 5) },
            { "skill_materializeacid", new MonsterPowerDataTemplate("skill_materializeacid", EMonsterPowerType.SUMMONHAZARD, 1, 4, 7) },
            { "skill_randomresist", new MonsterPowerDataTemplate("skill_randomresist", EMonsterPowerType.HEALBUFF, 1, 4) },
            { "skill_icetrapprison", new MonsterPowerDataTemplate("skill_icetrapprison", EMonsterPowerType.SUMMONHAZARD, 1, 4, 7) },
            { "skill_rockslam", new MonsterPowerDataTemplate("skill_rockslam", EMonsterPowerType.DAMAGE, 1, 1, 4) },
            { "skill_rocktoss", new MonsterPowerDataTemplate("skill_rocktoss", EMonsterPowerType.DAMAGE, 3, 5, 8) },
            { "skill_mossbeastvine", new MonsterPowerDataTemplate("skill_mossbeastvine", EMonsterPowerType.SUMMONPET, 1, 99, 8) },
            { "skill_summonthornsreaction", new MonsterPowerDataTemplate("skill_summonthornsreaction", EMonsterPowerType.PASSIVE, 1, 1, 4) },
            { "skill_mortarfire", new MonsterPowerDataTemplate("skill_mortarfire", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange: 2, maxRange: 5, minLevel: 7) },
            { "skill_xvortex", new MonsterPowerDataTemplate("skill_xvortex", EMonsterPowerType.PULL, 1, 3, 8) },
            { "skill_nopushpull", new MonsterPowerDataTemplate("skill_nopushpull", EMonsterPowerType.PASSIVE, 1, 3, 12) },
            { "skill_iceslimehop", new MonsterPowerDataTemplate("skill_iceslimehop", EMonsterPowerType.MOVESELF, 2, 99, 7) },
            { "skill_dropice", new MonsterPowerDataTemplate("skill_dropice", EMonsterPowerType.PASSIVE, 1, 1, 8) },
            { "skill_iceslimesummonreaction", new MonsterPowerDataTemplate("skill_iceslimesummonreaction", EMonsterPowerType.PASSIVE, 1, 1, 9) },
            { "skill_sludgehit", new MonsterPowerDataTemplate("skill_sludgehit", EMonsterPowerType.PASSIVE, 1, 1, 5) },
            { "skill_sludgeball", new MonsterPowerDataTemplate("skill_sludgeball", EMonsterPowerType.DAMAGE, 2, 4, 8) },
            { "skill_sludgedeath", new MonsterPowerDataTemplate("skill_sludgedeath", EMonsterPowerType.PASSIVE, 1, 1, 9) },
            { "skill_shadowclawrake", new MonsterPowerDataTemplate("skill_shadowclawrake", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange: 1, maxRange: 5, minLevel: 8) },
            { "skill_shadowstep", new MonsterPowerDataTemplate("skill_shadowstep", EMonsterPowerType.MOVESELF, 1, 5, 8) },
            { "skill_rangedvanishing100", new MonsterPowerDataTemplate("skill_rangedvanishing100", EMonsterPowerType.PASSIVE) },
            { "skill_immuneroot", new MonsterPowerDataTemplate("skill_immuneroot", EMonsterPowerType.PASSIVE) },
            { "skill_aetherbolt", new MonsterPowerDataTemplate("skill_aetherbolt", EMonsterPowerType.DAMAGE, 1, 4, 6) },
            { "skill_waveshot", new MonsterPowerDataTemplate("skill_waveshot", EMonsterPowerType.DAMAGE, 1, 3, 6) },
            { "skill_sludgesplash", new MonsterPowerDataTemplate("skill_sludgesplash", EMonsterPowerType.DAMAGE, 1, 2) },
            { "skill_friendlysludgehit", new MonsterPowerDataTemplate("skill_friendlysludgehit", EMonsterPowerType.PASSIVE, 1, 1, 9) },
            { "skill_lightningdodgebuff", new MonsterPowerDataTemplate("skill_lightningdodgebuff", EMonsterPowerType.HEALBUFF, 1, 2, 4) },
            { "skill_shocktouch", new MonsterPowerDataTemplate("skill_shocktouch", EMonsterPowerType.PASSIVE, 1, 1, 8) },
            { "skill_firebuff", new MonsterPowerDataTemplate("skill_firebuff", EMonsterPowerType.HEALBUFF, 1, 1, 5) },
            { "skill_firearmor", new MonsterPowerDataTemplate("skill_firearmor", EMonsterPowerType.HEALBUFF, 1, 1, 6) },
            { "skill_phasmashot", new MonsterPowerDataTemplate("skill_phasmashot", EMonsterPowerType.SUMMONHAZARD, 1, 99, 8) },
            { "skill_destroyerlaserwall", new MonsterPowerDataTemplate("skill_destroyerlaserwall", EMonsterPowerType.SUMMONHAZARD, 1, 5, 11) },
            { "skill_mutagenself", new MonsterPowerDataTemplate("skill_mutagenself", EMonsterPowerType.HEALBUFF, 1, 4, 9) },
            { "skill_randomdebuffplayer", new MonsterPowerDataTemplate("skill_randomdebuffplayer", EMonsterPowerType.DEBUFF, 1, 5, 7) },
            { "skill_summonsentrybot2", new MonsterPowerDataTemplate("skill_summonsentrybot2", EMonsterPowerType.SUMMONPET, 1, 99, 9) },
            { "skill_vortex2", new MonsterPowerDataTemplate("skill_vortex2", EMonsterPowerType.PULL, 1, 4, 10) },
            { "skill_passiveshadowhit", new MonsterPowerDataTemplate("skill_passiveshadowhit", EMonsterPowerType.PASSIVE, 1, 1, 7) },
            { "skill_shadowslash", new MonsterPowerDataTemplate("skill_shadowslash", EMonsterPowerType.PASSIVE, 2, 4, 7) },
            { "skill_freezeshot", new MonsterPowerDataTemplate("skill_freezeshot", EMonsterPowerType.PASSIVE, isChargeAbility: true, minRange: 2, maxRange: 6, minLevel: 9) },
            { "skill_burnshot", new MonsterPowerDataTemplate("skill_burnshot", EMonsterPowerType.PASSIVE, isChargeAbility: true, minRange: 2, maxRange: 6, minLevel: 9) },
            { "skill_neutralize", new MonsterPowerDataTemplate("skill_neutralize", EMonsterPowerType.DEBUFF, 1, 4, 8) },
            { "skill_removedebuffs2", new MonsterPowerDataTemplate("skill_removedebuffs2", EMonsterPowerType.HEALBUFF, 1, 4, 9) },
            { "skill_monsterriposte", new MonsterPowerDataTemplate("skill_monsterriposte", EMonsterPowerType.PASSIVE, 1, 1, 11) },
            { "skill_rushdownbite", new MonsterPowerDataTemplate("skill_rushdownbite", EMonsterPowerType.DAMAGE, 2, 4, 12) },
            { "skill_panthroxroar", new MonsterPowerDataTemplate("skill_panthroxroar", EMonsterPowerType.DAMAGE, 1, 5, 11) },
            { "skill_expmon_clawrake2", new MonsterPowerDataTemplate("skill_expmon_clawrake2", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange: 2, maxRange: 4, minLevel: 7) },
            { "skill_expmon_dmgmonsters", new MonsterPowerDataTemplate("skill_expmon_dmgmonsters", EMonsterPowerType.PASSIVE, 1, 1, 7) },
            { "skill_expmon_empowerkill", new MonsterPowerDataTemplate("skill_expmon_empowerkill", EMonsterPowerType.PASSIVE, 1, 1, 7) },
            { "skill_expmon_stackpoison", new MonsterPowerDataTemplate("skill_expmon_stackpoison", EMonsterPowerType.PASSIVE, 1, 1, 9) },
            { "skill_expmon_movingflameskin", new MonsterPowerDataTemplate("skill_expmon_movingflameskin", EMonsterPowerType.PASSIVE, 1, 1, 10) },
            { "skill_expmon_infernocircle", new MonsterPowerDataTemplate("skill_expmon_infernocircle", EMonsterPowerType.SUMMONHAZARD, 1, 7, 9) },
            { "skill_expmon_spikeshell", new MonsterPowerDataTemplate("skill_expmon_spikeshell", EMonsterPowerType.PASSIVE) },
            { "skill_expmon_toughshell", new MonsterPowerDataTemplate("skill_expmon_toughshell", EMonsterPowerType.PASSIVE) },
            { "skill_expmon_scorpionsting", new MonsterPowerDataTemplate("skill_expmon_scorpionsting", EMonsterPowerType.PASSIVE) },
            { "skill_expmon_spitice", new MonsterPowerDataTemplate("skill_expmon_spitice", EMonsterPowerType.DAMAGE, 2, 4, 7) },
            { "skill_expmon_freezebite", new MonsterPowerDataTemplate("skill_expmon_freezebite", EMonsterPowerType.PASSIVE, 1, 1, 5) },
            { "skill_expmon_lasercannon", new MonsterPowerDataTemplate("skill_expmon_lasercannon", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange: 2, maxRange: 4) },
            { "skill_expmon_laserpush", new MonsterPowerDataTemplate("skill_expmon_laserpush", EMonsterPowerType.MOVESELF, 1, 1) },
            { "skill_expmon_armorbreak", new MonsterPowerDataTemplate("skill_expmon_armorbreak", EMonsterPowerType.DEBUFF, 1, 1, 4) },
            { "skill_expmon_weaponbreak", new MonsterPowerDataTemplate("skill_expmon_weaponbreak", EMonsterPowerType.DEBUFF, 1, 1, 4) },
            { "skill_expmon_whirlslash", new MonsterPowerDataTemplate("skill_expmon_whirlslash", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange: 1, maxRange: 2, minLevel: 7) },
            { "skill_xp_bladefroghop", new MonsterPowerDataTemplate("skill_xp_bladefroghop", EMonsterPowerType.MOVESELF, 3, 4, 9) },
            { "skill_xp_singlebladesummon", new MonsterPowerDataTemplate("skill_xp_singlebladesummon", EMonsterPowerType.SUMMONHAZARD, 1, 1, 5) },
            { "skill_noroot_nostun", new MonsterPowerDataTemplate("skill_noroot_nostun", EMonsterPowerType.PASSIVE, 1, 1, 8) },
            { "skill_expmon_boulderslam", new MonsterPowerDataTemplate("skill_expmon_boulderslam", EMonsterPowerType.DAMAGE, 1, 1, 7) },
            { "skill_rockcircle", new MonsterPowerDataTemplate("skill_rockcircle", EMonsterPowerType.SUMMONHAZARD, 1, 1, 7) },
            { "skill_createrandomrock", new MonsterPowerDataTemplate("skill_createrandomrock", EMonsterPowerType.SUMMONHAZARD, 1, 6, 7) },
            { "skill_expmon_overdrive", new MonsterPowerDataTemplate("skill_expmon_overdrive", EMonsterPowerType.HEALBUFF, 1, 99, 9) },
            { "skill_expmon_shielding", new MonsterPowerDataTemplate("skill_expmon_shielding", EMonsterPowerType.HEALBUFF, 1, 5, 11) },
            { "skill_expmon_blackhole", new MonsterPowerDataTemplate("skill_expmon_blackhole", EMonsterPowerType.SUMMONHAZARD, 1, 5, 11) },
            { "skill_axeiaijutsu", new MonsterPowerDataTemplate("skill_axeiaijutsu", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange: 1, maxRange: 2, minLevel: 11) },
            { "skill_axeflamecircle", new MonsterPowerDataTemplate("skill_axeflamecircle", EMonsterPowerType.SUMMONHAZARD, 2, 2, minLevel: 8) },
            { "skill_summontoxicslime", new MonsterPowerDataTemplate("skill_summontoxicslime", EMonsterPowerType.SUMMONPET, 1, 5, minLevel: 11) },
            { "exp_skill_venomancerpoison", new MonsterPowerDataTemplate("exp_skill_venomancerpoison", EMonsterPowerType.PASSIVE, 1, 1, 10) },
            { "skill_arrowcatch", new MonsterPowerDataTemplate("skill_arrowcatch", EMonsterPowerType.PASSIVE, 1, 1, 8) },
            { "skill_mon_tornadostance", new MonsterPowerDataTemplate("skill_mon_tornadostance", EMonsterPowerType.HEALBUFF, 1, 1, 9) },
            { "skill_mon_rangedswap", new MonsterPowerDataTemplate("skill_mon_rangedswap", EMonsterPowerType.PULL, 1, 4, 9) },
            { "skill_monrocketcharge", new MonsterPowerDataTemplate("skill_monrocketcharge", EMonsterPowerType.MOVESELF, 2, 4, 5) },
            { "skill_monhailofarrows", new MonsterPowerDataTemplate("skill_monhailofarrows", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange:1, maxRange:4, minLevel: 8) },
            { "skill_monwildhorse", new MonsterPowerDataTemplate("skill_monwildhorse", EMonsterPowerType.MOVESELF, 2, 99, 6) },
            { "skill_monhotstreak", new MonsterPowerDataTemplate("skill_monhotstreak", EMonsterPowerType.MOVESELF, 2, 99) },
            { "skill_monfireevocation", new MonsterPowerDataTemplate("skill_monfireevocation", EMonsterPowerType.DAMAGE, 1, 4, 5) },
            { "skill_mondivineretribution", new MonsterPowerDataTemplate("skill_mondivineretribution", EMonsterPowerType.DAMAGE, 1, 4, 9) },
            { "skill_flameserpent", new MonsterPowerDataTemplate("skill_flameserpent", EMonsterPowerType.SUMMONHAZARD, 1, 3, 5) },
            { "skill_gravitysurge", new MonsterPowerDataTemplate("skill_gravitysurge", EMonsterPowerType.PULL, 1, 3, 5) },
            { "skill_ppextradamage", new MonsterPowerDataTemplate("skill_ppextradamage", EMonsterPowerType.PASSIVE) },
            { "skill_dustinthewind", new MonsterPowerDataTemplate("skill_dustinthewind", EMonsterPowerType.HEALBUFF, 1, 1, 5) },
            { "skill_expmon_resilient", new MonsterPowerDataTemplate("skill_expmon_resilient", EMonsterPowerType.PASSIVE, 1, 1, 7) },
            { "skill_elemabsorb", new MonsterPowerDataTemplate("skill_elemabsorb", EMonsterPowerType.HEALBUFF, 1, 5, 9) },
            { "skill_expmon_staffcharge", new MonsterPowerDataTemplate("skill_expmon_staffcharge", EMonsterPowerType.DAMAGE, 1, 2, 10) },
            { "skill_expmon_batterup", new MonsterPowerDataTemplate("skill_expmon_batterup", EMonsterPowerType.HEALBUFF, 3, 99, 19) },
            { "skill_expmon_absorbpowerups", new MonsterPowerDataTemplate("skill_expmon_absorbpowerups", EMonsterPowerType.PASSIVE) },
            { "skill_expmon_cubeslimesummonreaction", new MonsterPowerDataTemplate("skill_expmon_cubeslimesummonreaction", EMonsterPowerType.PASSIVE, 1, 1, 13) },
            { "skill_expmon_slimehex", new MonsterPowerDataTemplate("skill_expmon_slimehex", EMonsterPowerType.DEBUFF, 1, 4, 11) },
            { "skill_expmon_megabolt", new MonsterPowerDataTemplate("skill_expmon_megabolt", EMonsterPowerType.DAMAGE, isChargeAbility: true, minRange: 1, maxRange: 4, minLevel: 11) },
            { "skill_expmon_icepotion", new MonsterPowerDataTemplate("skill_expmon_icepotion", EMonsterPowerType.DAMAGE, minRange: 1, maxRange: 3, minLevel: 10) },
            { "skill_statusresist50", new MonsterPowerDataTemplate("skill_statusresist50", EMonsterPowerType.PASSIVE, 1, 1, 10) },
            { "skill_expmon_burstgas", new MonsterPowerDataTemplate("skill_expmon_burstgas", EMonsterPowerType.SUMMONHAZARD, 1, 1, 6) },
            { "skill_expmon_glitterskin", new MonsterPowerDataTemplate("skill_expmon_glitterskin", EMonsterPowerType.HEALBUFF, 1, 99, 8) },
            { "skill_expmon_passive_dodge25", new MonsterPowerDataTemplate("skill_expmon_passive_dodge25", EMonsterPowerType.PASSIVE, 1, 99, 5) },
            { "skill_expmon_eatweapon", new MonsterPowerDataTemplate("skill_expmon_eatweapon", EMonsterPowerType.DAMAGE, 1, 1, 13) },
            { "skill_expmon_adhesivetongue", new MonsterPowerDataTemplate("skill_expmon_adhesivetongue", EMonsterPowerType.PASSIVE, 1, 1, 7) },
            { "skill_expmon_wingslash", new MonsterPowerDataTemplate("skill_expmon_wingslash", EMonsterPowerType.DAMAGE, 1, 1, 10) },
            { "skill_expmon_fly2", new MonsterPowerDataTemplate("skill_expmon_fly2", EMonsterPowerType.MOVESELF, 2, 99, 11) },
            { "skill_expmon_stickyshot", new MonsterPowerDataTemplate("skill_expmon_stickyshot", EMonsterPowerType.DEBUFF, 1, 4, 1, 8) },
            { "skill_expmon_passivescare", new MonsterPowerDataTemplate("skill_expmon_passivescare", EMonsterPowerType.PASSIVE) },
            { "skill_expmon_stealandeatfood", new MonsterPowerDataTemplate("skill_expmon_stealandeatfood", EMonsterPowerType.DAMAGE, minRange: 1, maxRange: 1, minLevel: 9) },
            { "skill_expmon_cannonstunner", new MonsterPowerDataTemplate("skill_expmon_cannonstunner", EMonsterPowerType.DAMAGE, minRange: 1, maxRange: 1, minLevel: 5) },
            { "skill_expmon_healovertime", new MonsterPowerDataTemplate("skill_expmon_healovertime", EMonsterPowerType.PASSIVE, minRange: 1, maxRange: 1, minLevel: 7) },
            { "skill_expmon_holystorm", new MonsterPowerDataTemplate("skill_expmon_holystorm", EMonsterPowerType.DAMAGE, minRange: 1, maxRange: 2, minLevel: 9) },
            { "skill_expmon_stagrush", new MonsterPowerDataTemplate("skill_expmon_stagrush", EMonsterPowerType.MOVESELF, minRange: 1, maxRange: 5, minLevel: 12) },
            { "skill_expmon_divinepower", new MonsterPowerDataTemplate("skill_expmon_divinepower", EMonsterPowerType.PASSIVE, minRange: 1, maxRange: 1, minLevel: 7) },
            { "skill_expmon_multibite", new MonsterPowerDataTemplate("skill_expmon_multibite", EMonsterPowerType.DAMAGE, minRange: 1, maxRange: 1, minLevel: 10) },
            { "skill_expmon_transferhealth", new MonsterPowerDataTemplate("skill_expmon_transferhealth", EMonsterPowerType.HEALBUFF, 1, 3, 1) },

        };

        // Now do per-skill extra data.
        monsterPowerMasterList["skill_smokebomb"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_firearmor"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_expmon_overdrive"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_expmon_transferhealth"].mpd.useState = BehaviorState.FIGHT;


        monsterPowerMasterList["skill_smokebomb"].mpd.ignoreCosts = true;

        monsterPowerMasterList["skill_expmon_burstgas"].mpd.alwaysUseIfInRange = true;
        monsterPowerMasterList["skill_expmon_stealandeatfood"].mpd.alwaysUseIfInRange = true;
        monsterPowerMasterList["skill_expmon_cannonstunner"].mpd.alwaysUseIfInRange = true;

        monsterPowerMasterList["skill_expmon_transferhealth"].AddTag(EMonsterPowerTags.HEALING);
        monsterPowerMasterList["skill_expmon_healovertime"].AddTag(EMonsterPowerTags.HEALING);
        monsterPowerMasterList["skill_expmon_stealandeatfood"].AddTag(EMonsterPowerTags.HEALING);
        monsterPowerMasterList["skill_expmon_divinepower"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_expmon_stagrush"].AddTag(EMonsterPowerTags.ANYMOVEMENT);
        monsterPowerMasterList["skill_expmon_stagrush"].AddTag(EMonsterPowerTags.LIGHTNING);

        monsterPowerMasterList["skill_expmon_batterup"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_elemabsorb"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_expmon_glitterskin"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_randomresist"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_mon_tornadostance"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_summontoxicslime"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_expmon_shielding"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_expmon_overdrive"].mpd.healthThreshold = 0.75f;
        monsterPowerMasterList["skill_fungalregen"].mpd.healthThreshold = 0.8f;
        monsterPowerMasterList["skill_chemistheal"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_dodgebuff"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_monrocketcharge"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_removedebuffs2"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_wateryheal"].mpd.healthThreshold = 0.5f;
        monsterPowerMasterList["skill_wateryheal"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_watercross"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_leap"].mpd.healthThreshold = 0.5f;
        monsterPowerMasterList["skill_powershield"].mpd.ignoreCosts = true;
        monsterPowerMasterList["skill_roborepair"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_removedebuffs"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_staticcharge"].mpd.useState = BehaviorState.FIGHT;
        monsterPowerMasterList["skill_mortarfire"].mpd.ignoreCosts = true;
        monsterPowerMasterList["skill_mutagenself"].mpd.healthThreshold = 0.5f;

        // Limit some powers by family.
        monsterPowerMasterList["skill_slimesummonreaction"].limitToFamily = "jelly";
        monsterPowerMasterList["skill_iceslimesummonreaction"].limitToFamily = "jelly";
        monsterPowerMasterList["skill_expmon_cubeslimesummonreaction"].limitToFamily = "jelly";

        monsterPowerMasterList["skill_lasershot"].limitToFamily = "robots";
        monsterPowerMasterList["skill_lasershot2"].limitToFamily = "robots";
        monsterPowerMasterList["skill_laserclaw"].limitToFamily = "robots";
        monsterPowerMasterList["skill_destroyerlaserwall"].limitToFamily = "robots";
        monsterPowerMasterList["skill_summonsentrybot"].limitToFamily = "robots";
        monsterPowerMasterList["skill_summonsentrybot2"].limitToFamily = "robots";
        monsterPowerMasterList["skill_expmon_lasercannon"].limitToFamily = "robots";
        monsterPowerMasterList["skill_expmon_laserpush"].limitToFamily = "robots";
        monsterPowerMasterList["skill_phasmabolt"].limitToFamily = "robots";
        monsterPowerMasterList["skill_phasmashot"].limitToFamily = "robots";
        monsterPowerMasterList["skill_roborepair"].limitToFamily = "robots";

        // Add tags for things like elemental abilities, actual healing powers, offense vs. defensive passives
        monsterPowerMasterList["skill_firebreath1"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_fireclaw"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_smalllightningcircle"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_venombite"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_grottosting"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_lightningline"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_alchemygas"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_chemistheal"].AddTag(EMonsterPowerTags.HEALING);
        monsterPowerMasterList["skill_poisonproc"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_watercross"].AddTag(EMonsterPowerTags.WATER);
        monsterPowerMasterList["skill_watershot"].AddTag(EMonsterPowerTags.WATER);
        monsterPowerMasterList["skill_lavabite"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_shadowsnipe"].AddTag(EMonsterPowerTags.SHADOW);
        monsterPowerMasterList["skill_bedofflames"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_flameskin"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_acidspit"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_acidcloud"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_poisonthorn"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_fireburst"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_phasmabolt"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_fireburstranged"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_roborepair"].AddTag(EMonsterPowerTags.HEALING);
        monsterPowerMasterList["skill_lasershot"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_lasershot2"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_venombite2"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_laserclaw"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_shadowbolt"].AddTag(EMonsterPowerTags.SHADOW);
        monsterPowerMasterList["skill_banditshrapnelbomb"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_flameslash"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_lightningcircle"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_staticcharge"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_monshadowevocation"].AddTag(EMonsterPowerTags.SHADOW);
        monsterPowerMasterList["skill_icecone"].AddTag(EMonsterPowerTags.WATER);
        monsterPowerMasterList["skill_materializeacid"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_icetrapprison"].AddTag(EMonsterPowerTags.WATER);
        monsterPowerMasterList["skill_mortarfire"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_dropice"].AddTag(EMonsterPowerTags.WATER);
        monsterPowerMasterList["skill_sludgedeath"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_shadowclawrake"].AddTag(EMonsterPowerTags.SHADOW);
        monsterPowerMasterList["skill_shadowstep"].AddTag(EMonsterPowerTags.SHADOW);
        monsterPowerMasterList["skill_aetherbolt"].AddTag(EMonsterPowerTags.SHADOW);
        monsterPowerMasterList["skill_waveshot"].AddTag(EMonsterPowerTags.WATER);
        monsterPowerMasterList["skill_sludgesplash"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_shocktouch"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_firebuff"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_firearmor"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_phasmashot"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_destroyerlaserwall"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_passiveshadowhit"].AddTag(EMonsterPowerTags.SHADOW);
        monsterPowerMasterList["skill_shadowslash"].AddTag(EMonsterPowerTags.SHADOW);
        monsterPowerMasterList["skill_freezeshot"].AddTag(EMonsterPowerTags.WATER);
        monsterPowerMasterList["skill_burnshot"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_expmon_stackpoison"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_expmon_movingflameskin"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_expmon_infernocircle"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_expmon_spitice"].AddTag(EMonsterPowerTags.WATER);
        monsterPowerMasterList["skill_expmon_freezebite"].AddTag(EMonsterPowerTags.WATER);
        monsterPowerMasterList["skill_expmon_lasercannon"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_axeflamecircle"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_summontoxicslime"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["exp_skill_venomancerpoison"].AddTag(EMonsterPowerTags.POISON);
        monsterPowerMasterList["skill_mon_rangedswap"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_wateryheal"].AddTag(EMonsterPowerTags.HEALING);
        monsterPowerMasterList["skill_removedebuffs"].AddTag(EMonsterPowerTags.HEALING);
        monsterPowerMasterList["skill_removedebuffs2"].AddTag(EMonsterPowerTags.HEALING);
        monsterPowerMasterList["skill_monfireevocation"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_flameserpent"].AddTag(EMonsterPowerTags.FIRE);
        monsterPowerMasterList["skill_mondivineretribution"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_expmon_megabolt"].AddTag(EMonsterPowerTags.LIGHTNING);
        monsterPowerMasterList["skill_expmon_icepotion"].AddTag(EMonsterPowerTags.WATER);
        monsterPowerMasterList["skill_expmon_burstgas"].AddTag(EMonsterPowerTags.POISON);

        // Prepare to sort everything by type as well.
        for (int i = 0; i < (int)EMonsterPowerTags.COUNT; i++)
        {
            EMonsterPowerTags eTag = (EMonsterPowerTags)i;
            monsterPowersByTag.Add(eTag, new List<MonsterPowerDataTemplate>());
        }
        for (int i = 0; i < (int)EMonsterPowerType.COUNT; i++)
        {
            EMonsterPowerType eType = (EMonsterPowerType)i;
            monsterPowersByType.Add(eType, new List<MonsterPowerDataTemplate>());
        }

        // Figure out ranged damage and summon abilities
        foreach (MonsterPowerDataTemplate mpdt in monsterPowerMasterList.Values)
        {
            if (mpdt.powerType == EMonsterPowerType.SUMMONHAZARD || mpdt.powerType == EMonsterPowerType.SUMMONPET)
            {
                mpdt.AddTag(EMonsterPowerTags.ANYSUMMON);
            }
            if (mpdt.powerType == EMonsterPowerType.MOVESELF || mpdt.powerType == EMonsterPowerType.PULL || mpdt.powerType == EMonsterPowerType.PULL)
            {
                mpdt.AddTag(EMonsterPowerTags.ANYMOVEMENT);
            }
            if (mpdt.CheckTag(EMonsterPowerTags.FIRE) || mpdt.CheckTag(EMonsterPowerTags.WATER) || mpdt.CheckTag(EMonsterPowerTags.SHADOW) ||
                mpdt.CheckTag(EMonsterPowerTags.POISON) || mpdt.CheckTag(EMonsterPowerTags.LIGHTNING))
            {
                mpdt.AddTag(EMonsterPowerTags.ANYELEMENT);
            }
            if (mpdt.mpd.maxRange > 1 && mpdt.powerType == EMonsterPowerType.DAMAGE)
            {
                AbilityScript abilRef = mpdt.mpd.abilityRef;
                if (mpdt.mpd.maxRange >= 2)
                {
                    mpdt.AddTag(EMonsterPowerTags.RANGEDDAMAGE);
                }
            }
            monsterPowersByType[mpdt.powerType].Add(mpdt);
            for (int x = 0; x < mpdt.extraTags.Length; x++)
            {
                if (mpdt.extraTags[x])
                {
                    EMonsterPowerTags eTag = (EMonsterPowerTags)x;
                    monsterPowersByTag[eTag].Add(mpdt);
                }
            }
        }

    }

    /// <summary>
    /// Some prefabs have specific elements we might be aware of, so we don't have a fire-looking thing with Water elements.
    /// </summary>
    static void AddTagsToMonsterPrefabs()
    {
        dictPrefabTags["FisticuffsPlunderer"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterAncientSalamander"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterAxeWielder"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterBanditBrigand"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterBanditBrigand_Alt"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterBanditLady"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterBanditLady_Alt"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterCaveLion"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterCaveLion_Alt"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterCubeSlime"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterGhostSamurai"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterJadeBeetle"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterLandShark"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterLandShark_Alt"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterLizard"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterRedCrab"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterRockGolem"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterRockGolemShovel"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterRockViper"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterRockViperAlt"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterShadowPanther"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterSlimeRat"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["MonsterStaffBandit"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["RobotMantis"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["ScytheMantis"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["ScytheMantis_Alt"].Add(EPrefabExtraTags.MELEE_ONLY);
        dictPrefabTags["SpiritWolf"].Add(EPrefabExtraTags.MELEE_ONLY);

        dictPrefabTags["BanditSniper"].Add(EPrefabExtraTags.RANGEDWEAPON);
        dictPrefabTags["FireSlime"].Add(EPrefabExtraTags.FIRE);
        dictPrefabTags["IceSnake"].Add(EPrefabExtraTags.WATER);
        dictPrefabTags["MonsterBanditBowman"].Add(EPrefabExtraTags.RANGEDWEAPON);
        dictPrefabTags["MonsterBlueLightningElemental"].Add(EPrefabExtraTags.LIGHTNING);
        dictPrefabTags["MonsterAncientSalamander"].Add(EPrefabExtraTags.MULTICOLOR);
        dictPrefabTags["MonsterFlameBird"].Add(EPrefabExtraTags.FIRE);
        dictPrefabTags["MonsterFlameBird_Alt"].Add(EPrefabExtraTags.FIRE);
        dictPrefabTags["MonsterIceSlime"].Add(EPrefabExtraTags.WATER);
        dictPrefabTags["MonsterIceSlime_Alt"].Add(EPrefabExtraTags.WATER);
        dictPrefabTags["MonsterLandShark"].Add(EPrefabExtraTags.MULTICOLOR);
        dictPrefabTags["MonsterLandShark_Alt"].Add(EPrefabExtraTags.MULTICOLOR);
        dictPrefabTags["MonsterLavaViper"].Add(EPrefabExtraTags.FIRE);
        dictPrefabTags["MonsterLightningElemental"].Add(EPrefabExtraTags.LIGHTNING);
        dictPrefabTags["MonsterLivingVine"].Add(EPrefabExtraTags.GRASS_OR_TREE);
        dictPrefabTags["MonsterMiniIceSlime"].Add(EPrefabExtraTags.WATER);
        dictPrefabTags["MonsterMossBeast"].Add(EPrefabExtraTags.GRASS_OR_TREE);
        dictPrefabTags["MonsterPoisonElemental"].Add(EPrefabExtraTags.POISON);
        dictPrefabTags["MonsterShadowElemental"].Add(EPrefabExtraTags.SHADOW);
        dictPrefabTags["SpittingPlant"].Add(EPrefabExtraTags.GRASS_OR_TREE);
        dictPrefabTags["MonsterVineStalker"].Add(EPrefabExtraTags.MULTICOLOR);
        dictPrefabTags["MonsterWaterElementalA"].Add(EPrefabExtraTags.WATER);
        dictPrefabTags["MonsterWaterElementalA_Alt"].Add(EPrefabExtraTags.WATER);
        dictPrefabTags["ThunderSlime"].Add(EPrefabExtraTags.LIGHTNING);
        dictPrefabTags["SnappingTurtle"].Add(EPrefabExtraTags.MULTICOLOR);
        dictPrefabTags["BigUrchin"].Add(EPrefabExtraTags.NATURALGROWTH);
        dictPrefabTags["PoisonUrchin"].Add(EPrefabExtraTags.NATURALGROWTH);
        dictPrefabTags["FungalColumn"].Add(EPrefabExtraTags.NATURALGROWTH);
        dictPrefabTags["SpittingPlant"].Add(EPrefabExtraTags.NATURALGROWTH);

        dictPrefabTags["MonsterCaveFlyer"].Add(EPrefabExtraTags.FLYING);
        dictPrefabTags["MonsterFlameBird"].Add(EPrefabExtraTags.FLYING);
        dictPrefabTags["MonsterFlameBird_Alt"].Add(EPrefabExtraTags.FLYING);
        dictPrefabTags["MonsterNaturalRobot"].Add(EPrefabExtraTags.FLYING);
        dictPrefabTags["Neutralizer"].Add(EPrefabExtraTags.FLYING);
        dictPrefabTags["MonsterGhostSamurai"].Add(EPrefabExtraTags.FLYING);
        dictPrefabTags["GuardianDisc"].Add(EPrefabExtraTags.FLYING);
        dictPrefabTags["Hornet"].Add(EPrefabExtraTags.FLYING);

        dictPrefabTags.Add("LaserTurret", new List<EPrefabExtraTags>());

        dictPrefabTags["LaserTurret"].Add(EPrefabExtraTags.RANGEDWEAPON);
        dictPrefabTags["LaserTurretRed"].Add(EPrefabExtraTags.RANGEDWEAPON);
        dictPrefabTags["MonsterRoboTank"].Add(EPrefabExtraTags.RANGEDWEAPON);
        dictPrefabTags["SpittingPlant"].Add(EPrefabExtraTags.RANGEDWEAPON);
    }

    static void CreateFamilyAttributeData()
    {
        dictAttributeChancesByFamily = new Dictionary<string, FamilyAttributeTemplate>();
        dictAttributeChancesByFamily.Add("jelly", new FamilyAttributeTemplate());
        dictAttributeChancesByFamily.Add("bandits", new FamilyAttributeTemplate());
        dictAttributeChancesByFamily.Add("beasts", new FamilyAttributeTemplate());
        dictAttributeChancesByFamily.Add("snakes", new FamilyAttributeTemplate());
        dictAttributeChancesByFamily.Add("insects", new FamilyAttributeTemplate());
        dictAttributeChancesByFamily.Add("frogs", new FamilyAttributeTemplate());
        dictAttributeChancesByFamily.Add("spirits", new FamilyAttributeTemplate());
        dictAttributeChancesByFamily.Add("hybrids", new FamilyAttributeTemplate());
        dictAttributeChancesByFamily.Add("robots", new FamilyAttributeTemplate());

        dictAttributeChancesByFamily["jelly"].dictChanceOfAttributes = new Dictionary<MonsterAttributes, float>()
        {
            {  MonsterAttributes.BERSERKER, 0.1f },
            {  MonsterAttributes.COMBINABLE, 1.0f },
            {  MonsterAttributes.LOVESBATTLES, 1.0f },
            {  MonsterAttributes.GREEDY, 1.0f },
            {  MonsterAttributes.RONIN, 0.2f },
        };

        dictAttributeChancesByFamily["jelly"].dictAverageAttribute = new Dictionary<MonsterAttributes, int>()
        {
            {  MonsterAttributes.BERSERKER, 15 },
            {  MonsterAttributes.COMBINABLE, 100 },
            {  MonsterAttributes.LOVESBATTLES, 60 },
            {  MonsterAttributes.GREEDY, 35 },
            {  MonsterAttributes.RONIN, 50 }
        };

        dictAttributeChancesByFamily["bandits"].dictChanceOfAttributes = new Dictionary<MonsterAttributes, float>()
        {
            {  MonsterAttributes.BERSERKER, 0.1f },
            {  MonsterAttributes.CALLFORHELP, 1.0f },
            {  MonsterAttributes.LOVESBATTLES, 1.0f },
            {  MonsterAttributes.GREEDY, 0.8f },
            {  MonsterAttributes.TIMID, 0.5f },
            {  MonsterAttributes.GANGSUP, 0.5f },
            {  MonsterAttributes.PREDATOR, 0.4f },
            {  MonsterAttributes.STALKER, 0.4f },
            {  MonsterAttributes.RONIN, 0.33f }
        };

        dictAttributeChancesByFamily["bandits"].dictAverageAttribute = new Dictionary<MonsterAttributes, int>()
        {
            {  MonsterAttributes.BERSERKER,20 },
            {  MonsterAttributes.CALLFORHELP, 70 },
            {  MonsterAttributes.LOVESBATTLES, 60 },
            {  MonsterAttributes.GREEDY, 25 },
            {  MonsterAttributes.TIMID, 20 },
            {  MonsterAttributes.GANGSUP, 20 },
            {  MonsterAttributes.PREDATOR, 80 },
            {  MonsterAttributes.STALKER, 60 },
            {  MonsterAttributes.RONIN, 75 }
        };

        dictAttributeChancesByFamily["frogs"].dictChanceOfAttributes = new Dictionary<MonsterAttributes, float>()
        {
            {  MonsterAttributes.CALLFORHELP, 1.0f },
            {  MonsterAttributes.LOVESMUD, 1.0f },
            {  MonsterAttributes.TIMID, 0.25f },
            {  MonsterAttributes.RONIN, 0.9f }
        };

        dictAttributeChancesByFamily["frogs"].dictAverageAttribute = new Dictionary<MonsterAttributes, int>()
        {
            {  MonsterAttributes.CALLFORHELP, 90 },
            {  MonsterAttributes.LOVESMUD, 100 },
            {  MonsterAttributes.TIMID, 15 },
            {  MonsterAttributes.RONIN, 50 }
        };

        dictAttributeChancesByFamily["beasts"].dictChanceOfAttributes = new Dictionary<MonsterAttributes, float>()
        {
            {  MonsterAttributes.BERSERKER, 0.3f },
            {  MonsterAttributes.LAZY, 0.2f },
            {  MonsterAttributes.LOVESBATTLES, 0.35f },
            {  MonsterAttributes.TIMID, 0.1f },
            {  MonsterAttributes.GANGSUP, 0.15f },
            {  MonsterAttributes.PREDATOR, 0.2f },
            {  MonsterAttributes.STALKER, 0.2f },
            {  MonsterAttributes.RONIN, 0.5f }
        };

        dictAttributeChancesByFamily["beasts"].dictAverageAttribute = new Dictionary<MonsterAttributes, int>()
        {
            {  MonsterAttributes.BERSERKER, 30 },
            {  MonsterAttributes.LAZY, 40 },
            {  MonsterAttributes.LOVESBATTLES, 40 },
            {  MonsterAttributes.TIMID, 20 },
            {  MonsterAttributes.GANGSUP, 20 },
            {  MonsterAttributes.PREDATOR, 80 },
            {  MonsterAttributes.STALKER, 80 },
            {  MonsterAttributes.RONIN, 100 }
        };

        dictAttributeChancesByFamily["snakes"].dictAverageAttribute = new Dictionary<MonsterAttributes, int>();
        dictAttributeChancesByFamily["snakes"].dictChanceOfAttributes = new Dictionary<MonsterAttributes, float>();

        // Snakes can just be a copy of beasts.
        foreach(MonsterAttributes key in dictAttributeChancesByFamily["beasts"].dictAverageAttribute.Keys)
        {
            dictAttributeChancesByFamily["snakes"].dictChanceOfAttributes.Add(key, dictAttributeChancesByFamily["beasts"].dictChanceOfAttributes[key]);
            dictAttributeChancesByFamily["snakes"].dictAverageAttribute.Add(key, dictAttributeChancesByFamily["beasts"].dictAverageAttribute[key]);
        }

        dictAttributeChancesByFamily["robots"].dictChanceOfAttributes = new Dictionary<MonsterAttributes, float>()
        {
            {  MonsterAttributes.LOVESBATTLES, 1.0f },
            {  MonsterAttributes.CALLFORHELP, 0.4f },
            {  MonsterAttributes.RONIN, 1.0f }
        };

        dictAttributeChancesByFamily["robots"].dictAverageAttribute = new Dictionary<MonsterAttributes, int>()
        {
            {  MonsterAttributes.LOVESBATTLES, 80 },
            {  MonsterAttributes.CALLFORHELP, 80 },
            {  MonsterAttributes.RONIN, 100 }
        };

        dictAttributeChancesByFamily["spirits"].dictChanceOfAttributes = new Dictionary<MonsterAttributes, float>()
        {
            {  MonsterAttributes.COMBINABLE, 0.3f },
            {  MonsterAttributes.BERSERKER, 0.2f },
            {  MonsterAttributes.RONIN, 0.5f },
            {  MonsterAttributes.LOVESBATTLES, 0.75f },
            {  MonsterAttributes.STALKER, 0.75f },
            {  MonsterAttributes.GREEDY, 0.25f },
        };

        dictAttributeChancesByFamily["spirits"].dictAverageAttribute = new Dictionary<MonsterAttributes, int>()
        {
            {  MonsterAttributes.COMBINABLE, 75 },
            {  MonsterAttributes.BERSERKER, 25 },
            {  MonsterAttributes.RONIN, 80 },
            {  MonsterAttributes.LOVESBATTLES, 70 },
            {  MonsterAttributes.STALKER, 80 },
            {  MonsterAttributes.GREEDY, 25 },
        };

        dictAttributeChancesByFamily["insects"].dictChanceOfAttributes = new Dictionary<MonsterAttributes, float>()
        {
            {  MonsterAttributes.GANGSUP, 0.8f },
            {  MonsterAttributes.TIMID, 0.8f },
            {  MonsterAttributes.RONIN, 0.5f },
            {  MonsterAttributes.STALKER, 0.75f },
            {  MonsterAttributes.GREEDY, 0.75f }
        };

        dictAttributeChancesByFamily["insects"].dictAverageAttribute = new Dictionary<MonsterAttributes, int>()
        {
            {  MonsterAttributes.GANGSUP, 30 },
            {  MonsterAttributes.TIMID, 25 },
            {  MonsterAttributes.RONIN, 75 },
            {  MonsterAttributes.STALKER, 80 },
            {  MonsterAttributes.GREEDY, 40 }
        };

        dictAttributeChancesByFamily["hybrids"].dictChanceOfAttributes = new Dictionary<MonsterAttributes, float>()
        {
            {  MonsterAttributes.LOVESBATTLES, 0.4f },
            {  MonsterAttributes.STALKER, 0.4f },
            {  MonsterAttributes.BERSERKER, 0.2f },
            {  MonsterAttributes.RONIN, 0.75f }
        };

        dictAttributeChancesByFamily["hybrids"].dictAverageAttribute = new Dictionary<MonsterAttributes, int>()
        {
            {  MonsterAttributes.LOVESBATTLES, 60 },
            {  MonsterAttributes.STALKER, 50 },
            {  MonsterAttributes.BERSERKER, 20 },
            {  MonsterAttributes.RONIN, 80 }
        };
    }
}
