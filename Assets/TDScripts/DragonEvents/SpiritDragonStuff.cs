using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class DialogEventsScript
{
    public static bool BeginSpiritDragonBossFight(string value)
    {
        //MusicManagerScript.singleton.LoadMusicByName("bosstheme2", true, true);
        StartEncounterWithBoss("mon_xp_spiritdragon", TDProgress.DRAGON_SPIRIT, 1);
        return false;
    }

    public static bool FinishSpiritDragonDeath(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.gmsSingleton.StartCoroutine(SpiritDragonStuff.FinalSpiritDragonDeathFade());

        return false;
    }
}

public class SpiritDragonStuff
{
    public static void ChangeDragonAnimationByForm(Fighter ft, bool newFormIsPhysical)
    {
        zirconAnim newIdle = null;
        zirconAnim newTakeDamage = null;
        zirconAnim idle = null;
        zirconAnim takeDamage = null;

        //idle = ft.myAnimatable.FindAnim("Idle");
        //takeDamage = ft.myAnimatable.FindAnim("TakeDamage");

        if (newFormIsPhysical)
        {
            ft.myAnimatable.defaultIdleAnimationName = "IdlePhysical";
            ft.myAnimatable.defaultTakeDamageAnimationName = "TakeDamagePhysical";
            ft.myAnimatable.SetAnim("IdlePhysical");
            //newIdle = ft.myAnimatable.FindAnim("IdlePhysical");
            //newTakeDamage = ft.myAnimatable.FindAnim("TakeDamagePhysical");
        }
        else
        {
            ft.myAnimatable.defaultIdleAnimationName = "IdleEthereal";
            ft.myAnimatable.defaultTakeDamageAnimationName = "TakeDamageEthereal";

            ft.myAnimatable.SetAnim("IdleEthereal");

            //newIdle = ft.myAnimatable.FindAnim("IdleEthereal");
            //newTakeDamage = ft.myAnimatable.FindAnim("TakeDamageEthereal");
        }

        ft.checkForCustomAnimations = true;
        

        return;
        Debug.Log(newIdle.animName + " " + newTakeDamage.animName);

        List<zirconAnim.AnimationFrameData> afdList = new List<zirconAnim.AnimationFrameData>();

        for (int i = 0; i < idle.mySprites.Count; i++)
        {
            afdList.Add(newIdle.mySprites[i]);
            // idle.mySprites[i].mySprite = newIdle.mySprites[i].mySprite;                
            if (ft.myAnimatable.animPlaying.animName.Contains("Idle"))
            {
                ft.myAnimatable.animPlaying.mySprites[i].mySprite = newIdle.mySprites[i].mySprite;
            } 

        }

        idle.setSprite(afdList);

        for (int i = 0; i < newTakeDamage.mySprites.Count; i++)
        {
            takeDamage.mySprites[i].mySprite = newTakeDamage.mySprites[i].mySprite;
            if (ft.myAnimatable.animPlaying.animName.Contains("TakeDamage"))
            {
                ft.myAnimatable.animPlaying.mySprites[i].mySprite = newTakeDamage.mySprites[i].mySprite;
            }
        }

        ft.myAnimatable.SetAnim("IdleEthereal");

    }

    /// <summary>
    /// Runs in boss2 room to see if we can unlock bandit dragon.
    /// </summary>
    /// <param name="args"></param>
    public static void EvaluateSpiritDragonQuestLineInItemDream()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2)) return;

        // have not unlocked the dragon stuff yet
        if (ProgressTracker.CheckProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META) < 1)
        {
            return;
        }

        // already revealed spirit dungeon
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_SPIRIT_DUNGEON, ProgressLocations.HERO) >= 1 &&
            ProgressTracker.CheckProgress(TDProgress.DRAGON_SPIRIT_DUNGEON_ACCESSIBLE, ProgressLocations.HERO) >= 1)
        {
            return;
        }

        SpiritDragonStuff.DoSpiritDungeonUnlockIntro();
        ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT_DUNGEON, ProgressLocations.HERO, 1);
        ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT_DUNGEON_ACCESSIBLE, ProgressLocations.HERO, 1);
    }

    public static void DoSpiritDungeonUnlockIntro()
    {
        GameMasterScript.gmsSingleton.StartCoroutine(SpiritDragonStuff.SpiritDungeonUnlockIntro());
    }

    static IEnumerator SpiritDungeonUnlockIntro()
    {
        bool doWait = true;

        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_SPIRIT, ProgressLocations.HERO) >= 2)
        {
            doWait = false;
        }

        if (doWait) GameMasterScript.SetAnimationPlaying(true, true);

        Stairs spiritDungeonPortal = MapMasterScript.activeMap.SpawnStairs(false, MapMasterScript.SPIRIT_DRAGON_DUNGEONSTART_FLOOR);
        spiritDungeonPortal.prefab = "RedPortal";
        spiritDungeonPortal.autoMove = true;
        MapMasterScript.singletonMMS.SpawnStairs(spiritDungeonPortal);

        CustomAlgorithms.RevealTilesAroundPoint(spiritDungeonPortal.GetPos(), 1, true);

        if (doWait)
        {
            yield return new WaitForSeconds(0.5f);
            UIManagerScript.PlayCursorSound("Mirage");
            CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);
            GameMasterScript.cameraScript.AddScreenshake(0.4f);

            yield return new WaitForSeconds(0.75f);
        }

        /* foreach (Stairs st in MapMasterScript.theDungeon.FindFloor(MapMasterScript.SPIRIT_DRAGON_DUNGEONSTART_FLOOR).mapStairs)
        {
            if (st.pointsToFloor == 0 || st.stairsUp)
            {
                st.SetDestination(MapMasterScript.singletonMMS.ProcessActorAnchorMove);
            }
        } */

        if (doWait)
        {
            GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(spiritDungeonPortal.GetPos(), 1.0f, false);
            GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

            yield return new WaitForSeconds(2f);
        }

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;

        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_SPIRIT_DUNGEON, ProgressLocations.META) < 1)
        {
            UIManagerScript.StartConversationByRef("spirit_dungeon_unlocked", DialogType.STANDARD, null);
        }
        else
        {
            //GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
            if (doWait)
            {
                GameMasterScript.cameraScript.WaitThenSetCustomCameraMovement(GameMasterScript.heroPCActor.GetPos(), 1.0f, 0f, true);
            }                        
        }

        ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT_DUNGEON, ProgressLocations.META, 1);

        yield return new WaitForSeconds(1f);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
    }

    public static IEnumerator SpiritDragonIntro()
    {
        BossHealthBarScript.DisableBoss();
        Monster spiritDragon = MapMasterScript.activeMap.FindActor("mon_xp_spiritdragon") as Monster;
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(1.2f);

        // Pan to Big Boy

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(spiritDragon.GetPos(), 1.0f, false);

        yield return new WaitForSeconds(1.0f);

        // Linger

        ///CombatManagerScript.GenerateSpecificEffectAnimation(spiritDragon.GetPos(), "SndMirageEffect", null, true);
        UIManagerScript.PlayCursorSound("Mirage");

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.25f);

        UIManagerScript.StartConversationByRef("spirit_dragon_intro", DialogType.STANDARD, null);
        ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT, ProgressLocations.HERO, 1);
    }

    public static IEnumerator SpiritDragonDefeated()
    {
        /* MusicManagerScript.singleton.LoadMusicByName("BossVictory", true);
        MusicManagerScript.singleton.Play(true, true);
        MapMasterScript.activeMap.musicCurrentlyPlaying = "postboss"; */

        MusicManagerScript.singleton.Fadeout(1f);

        ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT, ProgressLocations.HERO, 2);

        Monster spiritDragon = MapMasterScript.activeMap.FindActor("mon_xp_spiritdragon") as Monster;

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(spiritDragon.GetPos(), 1f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.1f);

        UIManagerScript.FlashWhite(1f);

        UIManagerScript.PlayCursorSound("Mirage");
        GameMasterScript.cameraScript.AddScreenshake(0.7f);

        //beastDragon.myMovable.FadeOutThenDie();
        //GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitToDestroyActorObject(beastDragon, beastDragon.GetObject(), 0.1f));        

        yield return new WaitForSeconds(1f);

        UIManagerScript.FlashWhite(0.6f);
        GameMasterScript.cameraScript.AddScreenshake(0.4f);
        CombatManagerScript.GenerateSpecificEffectAnimation(spiritDragon.GetPos(), "HolyBolt", null, true);


        GameMasterScript.gmsSingleton.statsAndAchievements.DLC2_Beat_Spirit_Dragon();


        yield return new WaitForSeconds(2f);

        GameMasterScript.gmsSingleton.SetTempGameObject("spiritdragon", spiritDragon.GetObject());

        UIManagerScript.StartConversationByRef("spirit_dragon_defeated", DialogType.KEYSTORY, null);

    }

    public static IEnumerator FinalSpiritDragonDeathFade()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        UIManagerScript.FlashWhite(0.5f);

        UIManagerScript.PlayCursorSound("Whirlwind");

        GameObject dragon = GameMasterScript.gmsSingleton.ReadTempGameObject("spiritdragon");
        Movable mv = dragon.GetComponent<Movable>();
        mv.SetBShouldBeVisible(false);
        dragon.GetComponent<Animatable>().enabled = false;
        dragon.GetComponent<Movable>().FadeOut(2f);

        yield return new WaitForSeconds(1.25f);

        UIManagerScript.PlayCursorSound("Mirage");

        yield return new WaitForSeconds(1.25f);

        UIManagerScript.PlayCursorSound("TimePassing");

        UIManagerScript.FlashWhite(0.6f);
        GameMasterScript.ReturnToStack(dragon, "MonsterEchoDragon", new string[] { "MonsterEchoDragonWhite" }, true);

		MusicManagerScript.RequestPlayMusic("BossVictory",true);

        MapMasterScript.activeMap.musicCurrentlyPlaying = "postboss";

        BattleTextManager.NewText(StringManager.GetString("exp_popup_dragondefeated"), GameMasterScript.heroPCActor.GetObject(), Color.green, 0.5f);

        DialogEventsScript.RecenterCameraOnPlayer("");

        GameMasterScript.SetAnimationPlaying(false, false);
    }
}
