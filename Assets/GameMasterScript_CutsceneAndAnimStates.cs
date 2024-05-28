using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{
    public static bool IsGameInCutsceneOrDialog()
    {
        return IsNextTurnPausedByAnimations() || UIManagerScript.dialogBoxOpen;
    }

    public static bool IsNextTurnPausedByAnimations()
    {
        return gmsSingleton.animationPlaying ||
               gmsSingleton.animatingActorsPreventingUpdate.Count != 0 ||
               gmsSingleton.coroutinesPreventingUpdate.Count != 0;
    }

    public static void SetAnimationPlaying(bool play, bool fromCutscene = false)
    {
        gmsSingleton.animationPlaying = play;

        if (!play)
        {
            if (gmsSingleton.animationFromCutscene)
            {
                gmsSingleton.animationFromCutscene = false;
            }
        }
        else if (fromCutscene)
        {
            gmsSingleton.animationFromCutscene = true;
        }
        //Debug.Log("Animation: " + play + " FromCutscene: " + fromCutscene);
    }

    public static bool IsAnimationPlaying()
    {
        return gmsSingleton.animationPlaying;
    }

    public static bool IsAnimationPlayingFromCutscene()
    {
        return gmsSingleton.animationPlaying && gmsSingleton.animationFromCutscene;
    }
    public static void SetLevelChangeState(bool state)
    {
        levelChangeInProgress = state;
    }

    //This adds an actor to a watch list, and as long as it has queued animations or is moving,
    //the game will not update. 
    public static void PauseUpdateForActorAnimation(Actor a)
    {
        gmsSingleton.animatingActorsPreventingUpdate.Add(a);
    }
}