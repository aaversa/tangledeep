using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{

    IEnumerator PreloadAllStaticResources()
    {
        float timeAtStart = Time.realtimeSinceStartup;
#if UNITY_EDITOR
        //Debug.Log("<color=green>Begin static resource loading: " + Time.realtimeSinceStartup + "</color>");
#endif
        var dictLoadableObjects = new Dictionary<string, string>();

                
        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            yield return TryPreloadResource("TargetingLine", "targetingline/TargetingLine");
        }
        else
        {
            dictLoadableObjects.Add("TargetingLine", "TargetingLine");
        }

        TargetingLineScript.LoadAllMaterials();

        MapMasterScript.InitTileSetNames();

        // Stairs need to be preloaded manually because the names are not stored in XML.
        for (int i = 0; i < MapMasterScript.visualTileSetNames.Length; i++)
        {
            string down = MapMasterScript.visualTileSetNames[i] + "StairsDown";
            string up = MapMasterScript.visualTileSetNames[i] + "StairsUp";

            if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
            {
                yield return TryPreloadResource(down, "MapObjects/" + down);
                yield return TryPreloadResource(up, "MapObjects/" + up);
            }
            else
            {
                if (!dictLoadableObjects.ContainsKey(down))
                {
                    dictLoadableObjects.Add(down, down);
                }
                if (!dictLoadableObjects.ContainsKey(up))
                {
                    dictLoadableObjects.Add(up, up);
                }
            }
        }

        dictLoadableObjects.Add("Portal", "Portal");
        dictLoadableObjects.Add("AltPortal", "AltPortal");

        dictLoadableObjects.Add("TransparentStairs", "MapObjects/TransparentStairs");

        dictLoadableObjects.Add("RedPortal", "MapObjects/RedPortal");
        dictLoadableObjects.Add("MightyVine", "MapObjects/MightyVine");

        dictLoadableObjects.Add("MonsterCompendiumButton", "MonsterCompendiumButton");

        dictLoadableObjects.Add("ChildShara", "Jobs/ChildShara");

        dictLoadableObjects.Add("GenericTossableItemPrefab", "Items/GenericTossableItemPrefab");

        dictLoadableObjects.Add("FervirAltSwordEffect", "SpriteEffects/FervirAltSwordEffect");
        dictLoadableObjects.Add("ShadowReverberateEffect", "SpriteEffects/ShadowReverberateEffect");
        dictLoadableObjects.Add("FlowShatterParticles", "SpriteEffects/FlowShatterParticles");
        dictLoadableObjects.Add("VoidShatterParticles", "SpriteEffects/VoidShatterParticles");
        dictLoadableObjects.Add("HealthBar", "HealthBar");
        dictLoadableObjects.Add("CombatLogDivider", "CombatLogDivider");
        dictLoadableObjects.Add("MediumExplosionEffect", "SpriteEffects/MediumExplosionEffect");
        dictLoadableObjects.Add("ElementalAura", "SpriteEffects/ElementalAura");
        dictLoadableObjects.Add("PlayerWrathBar", "PlayerWrathBar");
        dictLoadableObjects.Add("PokerCard", "PokerCard");
        dictLoadableObjects.Add("ItemPickupBox", "ItemPickupBox");
        dictLoadableObjects.Add("Credits 3 Column", "Credits 3 Column");
        dictLoadableObjects.Add("Credits Single Column", "Credits Single Column");

        dictLoadableObjects.Add("SkirmisherEffect", "SpriteEffects/SkirmisherEffect");
        dictLoadableObjects.Add("ReflectEffect", "SpriteEffects/ReflectEffect");
        dictLoadableObjects.Add("VitalBleedEffect", "SpriteEffects/VitalBleedEffect");
        dictLoadableObjects.Add("VitalPainEffect", "SpriteEffects/VitalPainEffect");
        dictLoadableObjects.Add("VitalExplodeEffect", "SpriteEffects/VitalExplodeEffect");

#if !UNITY_SWITCH
        IncrementLoadingBar(0.0075f);
#endif

        dictLoadableObjects.Add("PlayerRootedEffect", "SpriteEffects/PlayerRootedEffect");
        dictLoadableObjects.Add("MudExplosion", "SpriteEffects/MudExplosion");

        dictLoadableObjects.Add("MusicNoteComboSystem", "SpriteEffects/MusicNoteComboSystem");

        dictLoadableObjects.Add("SmallExplosionEffectSystem", "SpriteEffects/SmallExplosionEffectSystem");
        dictLoadableObjects.Add("SmokeSpewEffect", "SpriteEffects/SmokeSpewEffect");
        dictLoadableObjects.Add("StarshardEffect", "SpriteEffects/StarshardEffect");
        dictLoadableObjects.Add("TeleportDown", "SpriteEffects/TeleportDown");
        dictLoadableObjects.Add("TeleportUp", "SpriteEffects/TeleportUp");
        dictLoadableObjects.Add("TeleportUpSilent", "SpriteEffects/TeleportUpSilent");
        dictLoadableObjects.Add("DirtParticleExplosion", "SpriteEffects/DirtParticleExplosion");

        dictLoadableObjects.Add("MeteorEffect", "SpriteEffects/MeteorEffect");

        dictLoadableObjects.Add("FadeAwayParticles", "SpriteEffects/FadeAwayParticles");

        dictLoadableObjects.Add("AutoBarrierEffect", "SpriteEffects/AutoBarrierEffect");
        dictLoadableObjects.Add("ChargingSkillParticles", "SpriteEffects/ChargingSkillParticles");
        dictLoadableObjects.Add("MassiveShatterParticles", "SpriteEffects/MassiveShatterParticles");
        dictLoadableObjects.Add("DustCloudLanding", "SpriteEffects/DustCloudLanding");
        dictLoadableObjects.Add("ItemUsedPopup", "SpriteEffects/ItemUsedPopup");
        dictLoadableObjects.Add("ItemStaticPopup", "SpriteEffects/ItemStaticPopup");
        dictLoadableObjects.Add("MonsterCorralMonster", "MonsterCorralMonster");
        dictLoadableObjects.Add("Kitchen", "MapObjects/Kitchen");
        dictLoadableObjects.Add("BudokaBuffTile2", "MapObjects/BudokaBuffTile2");
        dictLoadableObjects.Add("Charged Blessed Hammer", "MapObjects/Charged Blessed Hammer");
        dictLoadableObjects.Add("HUDPetStats", "HUDPetStats");
        dictLoadableObjects.Add("PlayerTargeting", "SpriteEffects/PlayerTargeting");
        dictLoadableObjects.Add("PlayerIngameHealthBar", "PlayerIngameHealthBar");
        dictLoadableObjects.Add("GroveTree_Dirt", "NPCs/GroveTree_Dirt");
        dictLoadableObjects.Add("GroveTree_Seed", "NPCs/GroveTree_Seed");
        dictLoadableObjects.Add("GroveTree_Seedling", "NPCs/GroveTree_Seedling");
        dictLoadableObjects.Add("GroveTree_Sapling", "NPCs/GroveTree_Sapling");
        dictLoadableObjects.Add("RareTreeSapling", "NPCs/RareTreeSapling");
        dictLoadableObjects.Add("RareTreeAdult", "NPCs/RareTreeAdult");
        dictLoadableObjects.Add("UncommonTreeAdult", "NPCs/UncommonTreeAdult");
        dictLoadableObjects.Add("TransLayer", "TransLayer");
        dictLoadableObjects.Add("UncommonTreeAdult2", "NPCs/UncommonTreeAdult2");
        dictLoadableObjects.Add("NewItemsDisplay", "SpriteEffects/NewItemsDisplay");
        dictLoadableObjects.Add("FervirFastPunchEffect", "SpriteEffects/FervirFastPunchEffect");
        dictLoadableObjects.Add("PivotHolder", "SpriteEffects/PivotHolder");
        dictLoadableObjects.Add("DodgeEffect", "SpriteEffects/DodgeEffect");
        dictLoadableObjects.Add("FervirBuffSilent", "SpriteEffects/FervirBuffSilent");
        dictLoadableObjects.Add("DustEffect", "SpriteEffects/DustEffect");
        dictLoadableObjects.Add("ParryEffect", "SpriteEffects/ParryEffect");
        dictLoadableObjects.Add("BlockEffect", "SpriteEffects/BlockEffect");
        dictLoadableObjects.Add("BlueBlockEffect", "SpriteEffects/BlueBlockEffect");
        dictLoadableObjects.Add("CriticalEffect", "SpriteEffects/CriticalEffect");
        dictLoadableObjects.Add("AggroEffect", "SpriteEffects/AggroEffect");
        dictLoadableObjects.Add("LevelUpEffect", "SpriteEffects/LevelUpEffect");

#if !UNITY_SWITCH
        IncrementLoadingBar(0.0075f);
#endif

        dictLoadableObjects.Add("DragonAura", "SpriteEffects/DragonAura");

        dictLoadableObjects.Add("AfterImageCreator", "SpriteEffects/AfterImageCreator");
        dictLoadableObjects.Add("SingleAfterImagePrefab", "SpriteEffects/SingleAfterImagePrefab");

        dictLoadableObjects.Add("InkBrushAttack", "SpriteEffects/InkBrushAttack");
        dictLoadableObjects.Add("InkBrushAttackCurve", "SpriteEffects/InkBrushAttackCurve");
        dictLoadableObjects.Add("InkBrushAttackAltCurve", "SpriteEffects/InkBrushAttackAltCurve");
        dictLoadableObjects.Add("InkBrushAttackVert", "SpriteEffects/InkBrushAttackVert");
        dictLoadableObjects.Add("InkBrushAttackHoriz", "SpriteEffects/InkBrushAttackHoriz");

        dictLoadableObjects.Add("FervirRecoveryQuiet", "SpriteEffects/FervirRecoveryQuiet");
        dictLoadableObjects.Add("ChampionSkull1", "SpriteEffects/ChampionSkull1");
        dictLoadableObjects.Add("ChampionSkull2", "SpriteEffects/ChampionSkull2");
        dictLoadableObjects.Add("ChampionSkull3", "SpriteEffects/ChampionSkull3");
        dictLoadableObjects.Add("ChampionSkull4", "SpriteEffects/ChampionSkull4");
        dictLoadableObjects.Add("ChampionSkull5", "SpriteEffects/ChampionSkull5");
        dictLoadableObjects.Add("MagicParticle1", "SpriteEffects/MagicParticle1");
        dictLoadableObjects.Add("MagicParticle2", "SpriteEffects/MagicParticle2");
        dictLoadableObjects.Add("MagicParticle3", "SpriteEffects/MagicParticle3");
        dictLoadableObjects.Add("MagicParticleSystem", "SpriteEffects/MagicParticleSystem");
        dictLoadableObjects.Add("FervirClawGrowEffect", "SpriteEffects/FervirClawGrowEffect");
        dictLoadableObjects.Add("CharmEffectSystem", "SpriteEffects/CharmEffectSystem");
        dictLoadableObjects.Add("SplashEffectSystem", "SpriteEffects/SplashEffectSystem");
        dictLoadableObjects.Add("TempCharmEffect", "SpriteEffects/TempCharmEffect");
        dictLoadableObjects.Add("OneshotSparkles", "SpriteEffects/OneshotSparkles");
        dictLoadableObjects.Add("RiverTileN", "MapObjects/Water/RiverTileN");
        dictLoadableObjects.Add("RiverTileE", "MapObjects/Water/RiverTileE");
        dictLoadableObjects.Add("RiverTileS", "MapObjects/Water/RiverTileS");
        dictLoadableObjects.Add("RiverTileW", "MapObjects/Water/RiverTileW");
        dictLoadableObjects.Add("RiverTileN_E", "MapObjects/Water/RiverTileN_E");
        dictLoadableObjects.Add("RiverTileN_S", "MapObjects/Water/RiverTileN_S");
        dictLoadableObjects.Add("RiverTileN_W", "MapObjects/Water/RiverTileN_W");
        dictLoadableObjects.Add("RiverTileE_S", "MapObjects/Water/RiverTileE_S");
        dictLoadableObjects.Add("RiverTileE_W", "MapObjects/Water/RiverTileE_W");
        dictLoadableObjects.Add("RiverTileE_S_W", "MapObjects/Water/RiverTileE_S_W");
        dictLoadableObjects.Add("RiverTileS_W", "MapObjects/Water/RiverTileS_W");
        dictLoadableObjects.Add("RiverTileN_E_S", "MapObjects/Water/RiverTileN_E_S");
        dictLoadableObjects.Add("RiverTileN_E_W", "MapObjects/Water/RiverTileN_E_W");
        dictLoadableObjects.Add("RiverTileN_S_W", "MapObjects/Water/RiverTileN_S_W");
        dictLoadableObjects.Add("RiverTileN_E_S_W", "MapObjects/Water/RiverTileN_E_S_W");
        dictLoadableObjects.Add("RiverTileNone", "MapObjects/Water/RiverTileNone");
        dictLoadableObjects.Add("GreenWindEffect", "SpriteEffects/GreenWindEffect");
        dictLoadableObjects.Add("KunaiImpactEffect", "SpriteEffects/KunaiImpactEffect");
        dictLoadableObjects.Add("BigSlashEffect1", "SpriteEffects/BigSlashEffect1");
        dictLoadableObjects.Add("BigSlashEffect2", "SpriteEffects/BigSlashEffect2");
        dictLoadableObjects.Add("TransparentHoly", "SpriteEffects/TransparentHoly");
        dictLoadableObjects.Add("ItemWorldMachineFixed", "NPCs/ItemWorldMachineFixed");
        dictLoadableObjects.Add("ItemWorldMachineBroken", "NPCs/ItemWorldMachineBroken");

        // For effect systems.
        dictLoadableObjects.Add("Sparkle1", "SpriteEffects/Sparkle1");
        dictLoadableObjects.Add("Sparkle2", "SpriteEffects/Sparkle2");
        dictLoadableObjects.Add("Sparkle3", "SpriteEffects/Sparkle3");
        dictLoadableObjects.Add("Sparkle4", "SpriteEffects/Sparkle4");
        dictLoadableObjects.Add("Sparkle5", "SpriteEffects/Sparkle5");
        dictLoadableObjects.Add("Sparkle6", "SpriteEffects/Sparkle6");
        dictLoadableObjects.Add("BlueSparkle1", "SpriteEffects/BlueSparkle1");
        dictLoadableObjects.Add("BlueSparkle2", "SpriteEffects/BlueSparkle2");
        dictLoadableObjects.Add("BlueSparkle3", "SpriteEffects/BlueSparkle3");
        dictLoadableObjects.Add("BlueSparkle4", "SpriteEffects/BlueSparkle4");
        dictLoadableObjects.Add("BlueSparkle5", "SpriteEffects/BlueSparkle5");
        dictLoadableObjects.Add("BlueSparkle6", "SpriteEffects/BlueSparkle6");
        dictLoadableObjects.Add("SingleSparkle", "SpriteEffects/SingleSparkle");
        dictLoadableObjects.Add("BigHemorrhageEffect", "SpriteEffects/BigHemorrhageEffect");
        dictLoadableObjects.Add("DownArrowEffect", "SpriteEffects/DownArrowEffect");
        dictLoadableObjects.Add("FervirMiniBiteEffect", "SpriteEffects/FervirMiniBiteEffect");
        dictLoadableObjects.Add("ItemSparkleSystem", "SpriteEffects/ItemSparkleSystem");
        dictLoadableObjects.Add("BlueItemSparkleSystem", "SpriteEffects/BlueItemSparkleSystem");

        dictLoadableObjects.Add("RayOfLightEffect", "SpriteEffects/RayOfLightEffect");
        dictLoadableObjects.Add("RobotScanRayEffect", "SpriteEffects/RobotScanRayEffect");

        dictLoadableObjects.Add("Green Spin Blade", "MapObjects/Green Spin Blade");

#if !UNITY_SWITCH
        IncrementLoadingBar(0.0075f);
#endif

        dictLoadableObjects.Add("MonsterDarkChemist_Alt", "Monsters/MonsterDarkChemist_Alt");

        dictLoadableObjects.Add("MonsterJadeBeetle_Alt", "Monsters/MonsterJadeBeetle_Alt");
        dictLoadableObjects.Add("MonsterSlimeRat_Alt", "Monsters/MonsterSlimeRat_Alt");

        dictLoadableObjects.Add("AcidExplosion3x", "SpriteEffects/AcidExplosion3x");

        dictLoadableObjects.Add("Hornet", "Monsters/Hornet");
        dictLoadableObjects.Add("MadderScientist_Alt", "Monsters/MadderScientist_Alt");
        dictLoadableObjects.Add("MonsterFungalFrogAlt", "Monsters/MonsterFungalFrogAlt");
        dictLoadableObjects.Add("MonsterRockViperAlt", "Monsters/MonsterRockViperAlt");
        dictLoadableObjects.Add("MonsterFloatingSnake", "Monsters/MonsterFloatingSnake");
        dictLoadableObjects.Add("MonsterGhostSamurai_Alt", "Monsters/MonsterGhostSamurai_Alt");
        dictLoadableObjects.Add("TreeFrog", "Monsters/TreeFrog");

        dictLoadableObjects.Add("MonsterSpiritMoose_Alt", "Monsters/MonsterSpiritMoose_Alt");
        dictLoadableObjects.Add("SnappingTurtle_Alt", "Monsters/SnappingTurtle_Alt");
        dictLoadableObjects.Add("MonsterAcidElemental_Alt", "Monsters/MonsterAcidElemental_Alt");

        dictLoadableObjects.Add("Bookshelf 1", "MapObjects/Bookshelf 1");
        dictLoadableObjects.Add("Bookshelf 2", "MapObjects/Bookshelf 2");
        dictLoadableObjects.Add("Bookshelf 3", "MapObjects/Bookshelf 3");
        dictLoadableObjects.Add("Bookshelf 4", "MapObjects/Bookshelf 4");
        dictLoadableObjects.Add("Shelves 1", "MapObjects/Shelves 1");
        dictLoadableObjects.Add("Shelves 2", "MapObjects/Shelves 2");
        dictLoadableObjects.Add("Shelves 3", "MapObjects/Shelves 3");
        dictLoadableObjects.Add("Shelves 4", "MapObjects/Shelves 4");
        dictLoadableObjects.Add("Shelves 5", "MapObjects/Shelves 5");
        dictLoadableObjects.Add("BronzePillar1", "Art/DecorPrefabs/BronzePillar1");
        dictLoadableObjects.Add("BronzePillar2", "Art/DecorPrefabs/BronzePillar2");
        dictLoadableObjects.Add("EarthPillar1", "Art/DecorPrefabs/EarthPillar1");
        dictLoadableObjects.Add("EarthPillar2", "Art/DecorPrefabs/EarthPillar2");
        dictLoadableObjects.Add("EarthPillar3", "Art/DecorPrefabs/EarthPillar3");
        dictLoadableObjects.Add("EarthPillar4", "Art/DecorPrefabs/EarthPillar4");

        dictLoadableObjects.Add("NightmarePillar1", "Art/DecorPrefabs/NightmarePillar1");
        dictLoadableObjects.Add("NightmarePillar2", "Art/DecorPrefabs/NightmarePillar2");
        dictLoadableObjects.Add("NightmarePillar3", "Art/DecorPrefabs/NightmarePillar3");
        dictLoadableObjects.Add("NightmarePillar4", "Art/DecorPrefabs/NightmarePillar4");

        dictLoadableObjects.Add("StonePillar1", "Art/DecorPrefabs/StonePillar1");
        dictLoadableObjects.Add("StonePillar2", "Art/DecorPrefabs/StonePillar2");
        dictLoadableObjects.Add("StonePillar3", "Art/DecorPrefabs/StonePillar3");
        dictLoadableObjects.Add("StonePillar4", "Art/DecorPrefabs/StonePillar4");
        dictLoadableObjects.Add("StonePillar5", "Art/DecorPrefabs/StonePillar5");
        dictLoadableObjects.Add("StonePillar6", "Art/DecorPrefabs/StonePillar6");

        dictLoadableObjects.Add("MonsterCoolfrog", "Monsters/MonsterCoolfrog");

        dictLoadableObjects.Add("SnowTree1", "Art/DecorPrefabs/SnowTree1");
        dictLoadableObjects.Add("SnowTree2", "Art/DecorPrefabs/SnowTree2");
        dictLoadableObjects.Add("SnowTree3", "Art/DecorPrefabs/SnowTree3");

#if !UNITY_SWITCH
        IncrementLoadingBar(0.0075f);
#endif

        dictLoadableObjects.Add("FireBall", "SpriteEffects/FireBall");

        dictLoadableObjects.Add("Tree1", "Art/DecorPrefabs/Tree1");
        dictLoadableObjects.Add("Tree2", "Art/DecorPrefabs/Tree2");
        dictLoadableObjects.Add("Tree3", "Art/DecorPrefabs/Tree3");
        dictLoadableObjects.Add("Tree4", "Art/DecorPrefabs/Tree4");
        dictLoadableObjects.Add("Tree5", "Art/DecorPrefabs/Tree5");
        dictLoadableObjects.Add("Tree7", "Art/DecorPrefabs/Tree7");
        dictLoadableObjects.Add("Tree8", "Art/DecorPrefabs/Tree8");
        dictLoadableObjects.Add("Stump1", "Art/DecorPrefabs/Stump1");
        dictLoadableObjects.Add("Stump2", "Art/DecorPrefabs/Stump2");

        dictLoadableObjects.Add("SlateTree1", "Art/DecorPrefabs/SlateTree1");

        dictLoadableObjects.Add("Bush1", "Art/DecorPrefabs/Bush1");
        dictLoadableObjects.Add("Bush2", "Art/DecorPrefabs/Bush2");
        dictLoadableObjects.Add("Bush3", "Art/DecorPrefabs/Bush3");
        dictLoadableObjects.Add("Bush4", "Art/DecorPrefabs/Bush4");
        dictLoadableObjects.Add("Bush5", "Art/DecorPrefabs/Bush5");
        dictLoadableObjects.Add("Bush6", "Art/DecorPrefabs/Bush6");
        dictLoadableObjects.Add("Bush7", "Art/DecorPrefabs/Bush7");
        dictLoadableObjects.Add("Bush8", "Art/DecorPrefabs/Bush8");
        dictLoadableObjects.Add("Bush9", "Art/DecorPrefabs/Bush9");
        dictLoadableObjects.Add("Bush10", "Art/DecorPrefabs/Bush10");

        dictLoadableObjects.Add("TerrainTile", "Art/DecorPrefabs/TerrainTile");
        dictLoadableObjects.Add("ElectricTile", "Art/DecorPrefabs/ElectricTile");
        dictLoadableObjects.Add("MudTile", "Art/DecorPrefabs/MudTile");
        dictLoadableObjects.Add("LaserTile", "Art/DecorPrefabs/LaserTile");
        dictLoadableObjects.Add("VolcanoBush1", "Art/DecorPrefabs/VolcanoBush1");
        dictLoadableObjects.Add("VolcanoBush2", "Art/DecorPrefabs/VolcanoBush2");
        dictLoadableObjects.Add("VolcanoBush3", "Art/DecorPrefabs/VolcanoBush3");
        dictLoadableObjects.Add("VolcanoBush4", "Art/DecorPrefabs/VolcanoBush4");
        dictLoadableObjects.Add("VolcanoBush5", "Art/DecorPrefabs/VolcanoBush5");

        dictLoadableObjects.Add("MagmaPillar1", "Art/DecorPrefabs/MagmaPillar1");
        dictLoadableObjects.Add("MagmaPillar2", "Art/DecorPrefabs/MagmaPillar2");

        dictLoadableObjects.Add("RuinedTree1", "Art/DecorPrefabs/RuinedTree1");
        dictLoadableObjects.Add("RuinedTree2", "Art/DecorPrefabs/RuinedTree2");
        dictLoadableObjects.Add("RuinedTree3", "Art/DecorPrefabs/RuinedTree3");

#if !UNITY_SWITCH
        IncrementLoadingBar(0.0075f);
#endif

        dictLoadableObjects.Add("FutureBush1", "Art/DecorPrefabs/FutureBush1");
        dictLoadableObjects.Add("FutureBush2", "Art/DecorPrefabs/FutureBush2");
        dictLoadableObjects.Add("FutureBush3", "Art/DecorPrefabs/FutureBush3");
        dictLoadableObjects.Add("FutureBush4", "Art/DecorPrefabs/FutureBush4");
        dictLoadableObjects.Add("FutureBush5", "Art/DecorPrefabs/FutureBush5");
        dictLoadableObjects.Add("FutureBush6", "Art/DecorPrefabs/FutureBush6");
        dictLoadableObjects.Add("FutureBush7", "Art/DecorPrefabs/FutureBush7");
        dictLoadableObjects.Add("Decor2x2 1", "Art/DecorPrefabs/Decor2x2 1");
        dictLoadableObjects.Add("Decor2x2 2", "Art/DecorPrefabs/Decor2x2 2");
        dictLoadableObjects.Add("Decor2x2 3", "Art/DecorPrefabs/Decor2x2 3");
        dictLoadableObjects.Add("Decor2x2 4", "Art/DecorPrefabs/Decor2x2 4");
        dictLoadableObjects.Add("Decor2x2 5", "Art/DecorPrefabs/Decor2x2 5");
        dictLoadableObjects.Add("Decor2x2 6", "Art/DecorPrefabs/Decor2x2 6");
        dictLoadableObjects.Add("Decor3x3 1", "Art/DecorPrefabs/Decor3x3 1");
        dictLoadableObjects.Add("Decor3x3 2", "Art/DecorPrefabs/Decor3x3 2");
        dictLoadableObjects.Add("Decor3x3 3", "Art/DecorPrefabs/Decor3x3 3");
        dictLoadableObjects.Add("StoneDecor2x2 1", "Art/DecorPrefabs/StoneDecor2x2 1");
        dictLoadableObjects.Add("StoneDecor2x2 2", "Art/DecorPrefabs/StoneDecor2x2 2");
        dictLoadableObjects.Add("StoneDecor2x2 3", "Art/DecorPrefabs/StoneDecor2x2 3");
        dictLoadableObjects.Add("SlateDecor2x2 1", "Art/DecorPrefabs/SlateDecor2x2 1");
        dictLoadableObjects.Add("SlateDecor2x2 2", "Art/DecorPrefabs/SlateDecor2x2 2");
        dictLoadableObjects.Add("SlateDecor2x2 3", "Art/DecorPrefabs/SlateDecor2x2 3");
        dictLoadableObjects.Add("EnterWaterSplash", "SpriteEffects/EnterWaterSplash");
        dictLoadableObjects.Add("EnterLavaSplash", "SpriteEffects/EnterLavaSplash");
        dictLoadableObjects.Add("EnterMudSplash", "SpriteEffects/EnterMudSplash");
        dictLoadableObjects.Add("WalkWaterSplash", "SpriteEffects/WalkWaterSplash");
        dictLoadableObjects.Add("WalkLavaSplash", "SpriteEffects/WalkLavaSplash");
        dictLoadableObjects.Add("WalkMudSplash", "SpriteEffects/WalkMudSplash");
        dictLoadableObjects.Add("PlayerMouseTargeting", "MapObjects/PlayerMouseTargeting");


        dictLoadableObjects.Add("DamageText", "DamageText");
        dictLoadableObjects.Add("OtherText", "OtherText");
        dictLoadableObjects.Add("OtherSpriteText", "OtherSpriteText");
        dictLoadableObjects.Add("MonsterDeath", "SpriteEffects/MonsterDeath");
        dictLoadableObjects.Add("TargetingMesh", "TargetingMesh");
        dictLoadableObjects.Add("CursorTargetingMesh", "CursorTargetingMesh");
        dictLoadableObjects.Add("CombatLogText", "CombatLogText");
        dictLoadableObjects.Add("StatModifier", "StatModifier");
        dictLoadableObjects.Add("GenericItemPrefab", "Items/GenericItemPrefab");
        dictLoadableObjects.Add("Sparkles", "SpriteEffects/Sparkles");
        dictLoadableObjects.Add("DungeonStuff", "DungeonStuff");
        dictLoadableObjects.Add("GoldSparkles", "SpriteEffects/GoldSparkles");
        dictLoadableObjects.Add("GreenSparkles", "SpriteEffects/GreenSparkles");
        dictLoadableObjects.Add("OrangeSparkles", "SpriteEffects/OrangeSparkles");
        dictLoadableObjects.Add("BlueSparkles", "SpriteEffects/BlueSparkles");
        dictLoadableObjects.Add("YellowSparkles", "SpriteEffects/YellowSparkles");
#if !UNITY_SWITCH
        dictLoadableObjects.Add("prefab_ghostcursor_sparkles", "GhostCursor/prefab_ghostcursor_sparkles");
        dictLoadableObjects.Add("GhostCursorSparkles", "GhostCursor/prefab_ghostcursor_sparkles");
        dictLoadableObjects.Add("TutorialMovementAnimatedSprite", "prefab_tutorial_showstickmoveimage");
#else
        dictLoadableObjects.Add("GhostCursorSparkles", "GhostCursor/prefab_ghostcursor_sparkles");
        dictLoadableObjects.Add("prefab_ghostcursor_sparkles", "GhostCursor/prefab_ghostcursor_sparkles");
        dictLoadableObjects.Add("TutorialMovementAnimatedSprite", "prefab_tutorial_showstickmoveimage");
#endif

        dictLoadableObjects.Add("FlyingWhiteBird", "MapObjects/FlyingWhiteBird");

        dictLoadableObjects.Add("PrototypeHusynStasis", "MapObjects/PrototypeHusynStasis");

        dictLoadableObjects.Add("GenericSwingEffect", "SpriteEffects/GenericSwingEffect");
        dictLoadableObjects.Add("MetalPoofSystem", "SpriteEffects/MetalPoofSystem");

        float timeAtResourceLoadStart = Time.realtimeSinceStartup;
        foreach (string key in dictLoadableObjects.Keys)
        {
            TryPreloadResourceNoBundles(key, dictLoadableObjects[key]);
            if (Time.realtimeSinceStartup - timeAtResourceLoadStart > MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeAtResourceLoadStart = Time.realtimeSinceStartup;
            }
        }

        IncrementLoadingBar(0.0075f);

        float timeAtCreditsLoadStart = Time.realtimeSinceStartup;

        while (UIManagerScript.singletonUIMS == null)
        {
            if (Time.realtimeSinceStartup - timeAtCreditsLoadStart >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeAtCreditsLoadStart = Time.realtimeSinceStartup;
#if UNITY_SWITCH
                IncrementLoadingBar(ELoadingBarIncrementValues.small);
#endif
            }
        }

        // Do this now at load time rather than waiting until the credits start to instantiate everything
        if (UIManagerScript.singletonUIMS != null)
        {
            CreditRollScript crs = UIManagerScript.singletonUIMS.creditsRoll;
            if (crs == null)
            {
                crs = GameObject.Find("Credits Container").GetComponent<CreditRollScript>();
            }

            yield return crs.DoCreditsInstantiation();
#if UNITY_SWITCH
            IncrementLoadingBar(ELoadingBarIncrementValues.medium);
#endif

        }

        allResourcesLoaded = true;
        //if (Debug.isDebugBuild) Debug.Log("<color=green>ALL STATIC RESOURCES LOADED!</color>");
    }

}