using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// All cutscenes and events related to the Bandit Dragon.

public partial class DialogEventsScript
{
    public static bool BeginBanditDragonBossFight(string value)
    {
		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("dragonboss", true);
        StartEncounterWithBoss("mon_banditdragon", TDProgress.DRAGON_BANDIT, 1);
        return false;
    }
}

public class BanditDragonStuff
{
    public static void DoBanditDungeonUnlockIntro()
    {
        GameMasterScript.gmsSingleton.StartCoroutine(BanditDragonStuff.BanditDungeonUnlockIntro());
    }

    // Runs after first boss has been cleared, and we have unlocked the Dragon quest line.
    // Spawn an entrance to the bandit dungeon with cutscene here.
    static IEnumerator BanditDungeonUnlockIntro()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Stairs banditDungeonStairs = MapMasterScript.activeMap.SpawnStairs(false, MapMasterScript.BANDIT_DRAGON_DUNGEONSTART_FLOOR);
        banditDungeonStairs.prefab = "StoneStairsUp";
        MapMasterScript.singletonMMS.SpawnStairs(banditDungeonStairs);

        CustomAlgorithms.RevealTilesAroundPoint(banditDungeonStairs.GetPos(), 1, true);

        yield return new WaitForSeconds(0.5f);

        UIManagerScript.PlayCursorSound("StoneMovement");
        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);
        GameMasterScript.cameraScript.AddScreenshake(0.4f);

        yield return new WaitForSeconds(0.75f);

        foreach(Stairs st in MapMasterScript.theDungeon.FindFloor(MapMasterScript.BANDIT_DRAGON_DUNGEONSTART_FLOOR).mapStairs)
        {
            if (st.pointsToFloor == 0 || st.stairsUp)
            {
                st.SetDestination(MapMasterScript.BOSS1_MAP_FLOOR);
                //Debug.Log("Redirected a Stairs.");
            }            
        }

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(banditDungeonStairs.GetPos(), 1.0f, false);

        yield return new WaitForSeconds(2f);

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;
        UIManagerScript.StartConversationByRef("bandit_dungeon_unlocked", DialogType.STANDARD, null);
    }


    // Used for Bandit Dragon "Keep" style maps.
    public static bool UnlockGatesOfIndex(int indexOfLinkedGates, Map processMap, bool playAnimation = false)
    {
        //Debug.Log("Unlocking gates of index " + indexOfLinkedGates);

        List<Destructible> linkedGates;
        if (MapMasterScript.activeMap.linkSwitchesToGates.TryGetValue(indexOfLinkedGates, out linkedGates))
        {
            if (linkedGates.Count > 0)
            {
                if (linkedGates[0].destroyed || linkedGates[0].isDestroyed)
                {
                    return false; // Already destroyed/opened for some reason.
                }
            }
            if (playAnimation)
            {
                // Play one time SFX with short animation.
                GameMasterScript.SetAnimationPlaying(true, true);
                GameMasterScript.gmsSingleton.StartCoroutine(SpecialEffectFunctions.PressFloorSwitchWithJuice(linkedGates));
            }
            else
            {
                // Change graphics for each linked gate, "destroy" 'em so they're not collidable
                foreach (Destructible dt in linkedGates)
                {
                    TileInteractions.SetDestructibleStateToDestroyed(dt);
                }
                //GameMasterScript.cameraScript.AddScreenshake(0.33f);
                //UIManagerScript.PlayCursorSound("StoneMovement");
            }

        }
        else
        {
            Debug.Log(indexOfLinkedGates + " couldn't find linked gate objects on map");
        }

        return true;
    }

    public static IEnumerator BanditDragonIntro()
    {

        BossHealthBarScript.DisableBoss();
        Monster banditDragon = MapMasterScript.activeMap.FindActor("mon_banditdragon") as Monster;
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(1.2f);

        // Pan to Big Boy

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(banditDragon.GetPos(), 1.0f, false);

        yield return new WaitForSeconds(1.0f);

        // Linger
        CombatManagerScript.GenerateSpecificEffectAnimation(banditDragon.GetPos(), "DeepSoundEmanation", null, true);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.25f);

        ProgressTracker.SetProgress(TDProgress.DRAGON_BANDIT, ProgressLocations.HERO, 1);
        UIManagerScript.StartConversationByRef("bandit_dragon_intro", DialogType.STANDARD, null);        

    }

    public static IEnumerator BanditDragonDefeated()
    {
        MapMasterScript.activeMap.musicCurrentlyPlaying = "postboss";

        Monster banditDragon = MapMasterScript.activeMap.FindActor("mon_banditdragon") as Monster;

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(banditDragon.GetPos(), 2.0f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        // Play buildup of smoke and FX...

        int numPoofs = 22;
        float totalWaitTime = 3.75f;
        Vector2 centerPos = banditDragon.GetPos();

        GameObject particles = CombatManagerScript.GenerateSpecificEffectAnimation(banditDragon.GetPos(), "ChargingSkillParticles", null, false);
        particles.transform.SetParent(banditDragon.GetObject().transform);
        particles.transform.localPosition = Vector3.zero;

        for (int i = 0; i <= numPoofs; i++)
        {
            centerPos = banditDragon.GetPos();
            centerPos.x += UnityEngine.Random.Range(-0.45f, 0.45f);
            centerPos.y += UnityEngine.Random.Range(-0.2f, 0.4f);
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "SoundEmanation", null, true);
            }
            else
            {
                CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "FervirClawEffect", null, true);
            }

            if (i % 6 == 0)
            {
                GameMasterScript.cameraScript.AddScreenshake(0.3f);
                banditDragon.myAnimatable.SetAnim("Idle");
            }

            if (i % 4 == 0)
            {
                banditDragon.myAnimatable.SetAnim("TakeDamage");
            }

            yield return new WaitForSeconds(totalWaitTime / numPoofs);
        }

        CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "BigExplosionEffect", null, true);

        UIManagerScript.FlashWhite(1f);
        GameMasterScript.cameraScript.AddScreenshake(0.7f);

        banditDragon.myMovable.FadeOutThenDie();
        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitToDestroyActorObject(banditDragon, banditDragon.GetObject(), 0.1f));


        GameMasterScript.gmsSingleton.statsAndAchievements.DLC2_Beat_Bandit_Dragon();


        yield return new WaitForSeconds(1.3f);

        DialogEventsScript.RecenterCameraOnPlayer("");
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);

        yield return new WaitForSeconds(1.05f);

        BattleTextManager.NewText(StringManager.GetString("exp_popup_dragondefeated"), GameMasterScript.heroPCActor.GetObject(), Color.green, 0.5f);
        GameMasterScript.cameraScript.AddScreenshake(0.4f);
        UIManagerScript.PlayCursorSound("StoneMovement");
        BanditDragonStuff.UnlockGatesOfIndex(999, MapMasterScript.activeMap, false);

        yield return new WaitForSeconds(0.45f);

        GameMasterScript.SetAnimationPlaying(false);
        //GameMasterScript.gmsSingleton.turnExecuting = false;

        UIManagerScript.StartConversationByRef("banditdragon_postdefeat", DialogType.STANDARD, null);
    }

    public static IEnumerator BanditDragonThreatenFood(Monster dragon)
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(0.75f);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(dragon.GetPos(), 0.75f, false);

        yield return new WaitForSeconds(1.25f);

        GameMasterScript.SetAnimationPlaying(false);

        UIManagerScript.StartConversationByRef("bandit_dragon_takeitems", DialogType.STANDARD, null);
    }
}

public partial class Cutscenes : MonoBehaviour
{
    static IEnumerator TransferFoodFromPlayerToBandit(Monster bandit)
    {
        List<Item> itemsToTake = new List<Item>();
        List<Item> playerInventory = GameMasterScript.heroPCActor.myInventory.GetInventory();
        float timeAtLastPause = Time.realtimeSinceStartup;
        float minFrameTime = 0.013f;
        foreach (Item itm in playerInventory)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            if (!itm.IsItemFood()) continue;
            itemsToTake.Add(itm);
            if (Time.realtimeSinceStartup - timeAtLastPause > minFrameTime)
            {
                yield return null;
                timeAtLastPause = Time.realtimeSinceStartup;
            }
        }
        foreach (Item itm in itemsToTake)
        {
            bandit.myInventory.AddItemRemoveFromPrevCollection(itm, true);
            if (Time.realtimeSinceStartup - timeAtLastPause > minFrameTime)
            {
                yield return null;
                timeAtLastPause = Time.realtimeSinceStartup;
            }
        }

        yield return null;
    }

    public static IEnumerator BanditDragonStealFood()
    {
        Monster theDragon = MapMasterScript.activeMap.FindActor("mon_banditdragon") as Monster;

        Vector2 originalPosition = theDragon.GetPos();

        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(0.25f);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(GameMasterScript.heroPCActor.GetPos(), 0.75f, false);

        yield return new WaitForSeconds(1.0f);

        yield return TransferFoodFromPlayerToBandit(theDragon);

        yield return new WaitForSeconds(0.2f);

        CombatManagerScript.GenerateSpecificEffectAnimation(theDragon.GetPos(), "DustEffect", null, true);
        UIManagerScript.PlayCursorSound("FastMovement");
        CombatManagerScript.FireProjectile(theDragon.GetPos(), GameMasterScript.heroPCActor.GetPos(), theDragon.GetObject(),
            0.4f, false, GameMasterScript.heroPCActor.GetObject(), MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 0f, false);

        yield return new WaitForSeconds(0.4f);

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "DustCloudLanding", null, true);

        yield return new WaitForSeconds(0.25f);

        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);

        List<string> possibleSFX = new List<string>()
        {
            "PickupItem",
            "GetMoney"
        };
        int sfxIndex = 0;

        for (int i = 0; i < 6; i++)
        {
            UIManagerScript.PlayCursorSound(possibleSFX[sfxIndex]);
            sfxIndex++;
            if (sfxIndex >= possibleSFX.Count)
            {
                sfxIndex = 0;
            }
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.15f);

        // Create a target for our jump (can't be ourselves, must be a dummy)
        GameObject invisibleTarget = GameMasterScript.TDInstantiate("TransparentStairs");
        invisibleTarget.transform.position = originalPosition;

        // Delete the target later
        GameMasterScript.gmsSingleton.WaitThenDestroyObject(invisibleTarget, 0.41f);

        // Do the jump effect
        CombatManagerScript.FireProjectile(GameMasterScript.heroPCActor.GetPos(),
            originalPosition,
            theDragon.GetObject(),
            0.4f,
            false,
            invisibleTarget,
            MovementTypes.TOSS,
            GameMasterScript.tossProjectileDummy,
            0f,
            false);

        UIManagerScript.PlayCursorSound("FastMovement");

        yield return new WaitForSeconds(1.0f);

        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FemaleSoundEmanation", null, false, 0f, true);

        yield return new WaitForSeconds(1.0f);

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;

        UIManagerScript.StartConversationByRef("bandit_dragon_takeitems_part2", DialogType.STANDARD, null);

    }
}
