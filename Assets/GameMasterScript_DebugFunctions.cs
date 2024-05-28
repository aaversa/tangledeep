using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public partial class GameMasterScript
{
    //SHEP: Destroy all monsters in the level
    public static void Debug_DetonateAllMonsters()
    {
        for (int i = 0; i < mms.GetAllActors().Count; i++)
        {
            Monster mon = mms.GetAllActors()[i] as Monster;
            if (mon != null && mon.actorfaction == Faction.ENEMY)
            {
                mon.TakeDamage(mon.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 10.0f, DamageTypes.COUNT);
                mon.whoKilledMe = GetHeroActor();
                BattleTextManager.NewText("FOOM!", mon.GetObject(), Color.red, UnityEngine.Random.value * 2.0f);
                GameObject attackSpriteObj = GameMasterScript.TDInstantiate("CriticalEffect");
                if (attackSpriteObj != null)
                {
                    CombatManagerScript.WaitThenGenerateSpecificEffect(mon.GetPos(), "BigExplosionEffect", null, UnityEngine.Random.Range(0, 0.5f));
                    CombatManagerScript.TryPlayEffectSFX(attackSpriteObj, mon.GetPos(), null);
                    Animatable anim = attackSpriteObj.GetComponent<Animatable>();
                    anim.SetAnim("Default");
                }
            }
        }

        GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);

    }

    //This will display all the reasons the main game loop is paused.
    public static object Debug_WhatsTheHoldUp(string[] args)
    {
        StringBuilder strRet = new StringBuilder("What's the hold up?\n=================\n\n");
        bool bShouldBeRunning = !IsNextTurnPausedByAnimations();

        // gmsSingleton.animationPlaying || gmsSingleton.animatingActorsPreventingUpdate.Count != 0;

        //show the bool that stops the game
        strRet.Append("bool animationPlaying == " + gmsSingleton.animationPlaying.ToString().ToUpper() + "\n");
        strRet.Append("bool turnExecuting == " + gmsSingleton.turnExecuting.ToString().ToUpper() + "\n");

        //show any animations that are holding up the game
        int iDelayAnimsCount = gmsSingleton.animatingActorsPreventingUpdate.Count;
        strRet.Append("animatingActorsPreventingUpdate.Count == " + iDelayAnimsCount + "\n");
        if (iDelayAnimsCount > 0)
        {
            foreach (var a in gmsSingleton.animatingActorsPreventingUpdate)
            {
                strRet.Append(" * " + a.actorRefName + " ID " + a.actorUniqueID + "\n");
            }
        }

        //check coroutines that are holding up the game
        int iDelayRoutinesCount = gmsSingleton.coroutinesPreventingUpdate.Count;
        strRet.Append("coroutinesPreventingUpdate.Count == " + iDelayRoutinesCount + "\n");
        if (iDelayRoutinesCount > 0)
        {
            foreach (var a in gmsSingleton.coroutinesPreventingUpdate)
            {
                strRet.Append(" * " + a.GetCoroutineName() + "\n");
            }
        }

        if (bShouldBeRunning)
        {
            strRet.Append("\nThe main loop should be running.");
        }
        else
        {
            strRet.Append("\nThe main loop is delayed.");
        }

        //how long ago did we try to run the game?
        float fDelta = (Time.realtimeSinceStartup - TDInputHandler.fLastTimeUpdateInputFinished);
        strRet.Append("\n\nThe last full UpdateInput happened \n" + fDelta + " seconds ago.");

        if (fDelta > 1.0f && bShouldBeRunning)
        {
            strRet.Append("\n\nUh oh. Game should be running, but maybe input isn't being checked?");
        }

        return strRet.ToString();
    }

    //Test holding up the game with a coroutine.
    public static string Debug_TestAddDelayCoroutine(string[] args)
    {
        float fDelayTime = 1.0f;
        if (args.Length >= 2)
        {
            fDelayTime = float.Parse(args[1]);
        }

        //this should hold up the game
        StartWatchedCoroutine(gmsSingleton.TestDelayGameCoroutine(fDelayTime));

        return "Delaying the game for " + fDelayTime + " second(s).";
    }

    IEnumerator TestDelayGameCoroutine(float fTime)
    {
        yield return new WaitForSeconds(fTime);
    }
}