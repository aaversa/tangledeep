using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class GameMasterScript
{
    struct NextTurnInfoStruct_Switch_Terrible
    {
        public bool runASAP;
        public TurnData td;
        public bool newTurn;
        public int idxThread;
    }
    private NextTurnInfoStruct_Switch_Terrible terribleNextTurnInfo;

    IEnumerator WaitThenContinueTurn(TurnData td, float time, Actor actorWhoMovedTurn)
    {
        yield return new WaitForSeconds(time);
        td.actorThatInitiatedTurn = actorWhoMovedTurn;
        SetAnimationPlaying(false);
        TryNextTurn(td, false, UnityEngine.Random.Range(0, Int32.MaxValue));
    }

    IEnumerator WaitCheckResultThenContinueTurn(CombatResult result, TurnData tData, float time, Actor initiator)
    {
        yield return new WaitForSeconds(time);
        tData.actorThatInitiatedTurn = initiator;
        SetAnimationPlaying(false);
        CombatResultsScript.CheckCombatResult(result, tData.GetSingleTargetActor(), MapMasterScript.activeMap);
        TryNextTurn(tData, false, UnityEngine.Random.Range(0, Int32.MaxValue));
    }

    public IEnumerator WaitCheckResultsThenContinueTurn(List<CombatResult> results, List<Actor> targets, float time, TurnData td, Actor initiator, int iThreadIndex = 0)
    {
#if UNITY_EDITOR
        //Debug.Log("Waiting " + time + " to continue turn " + turnNumber + " idx " + iThreadIndex);
#endif
        yield return new WaitForSeconds(time);
        SetAnimationPlaying(false);
        td.actorThatInitiatedTurn = initiator;
        for (int i = 0; i < results.Count; i++)
        {
            CombatResultsScript.CheckCombatResult(results[i], targets[i], MapMasterScript.activeMap);
        }
        TryNextTurn(td, false, UnityEngine.Random.Range(0, Int32.MaxValue));
    }

}