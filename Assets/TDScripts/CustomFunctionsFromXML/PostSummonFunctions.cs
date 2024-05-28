using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PostSummonFunctions
{
    public static void AssignMeteorKeyAnimPositions(SummonActorEffect sae, Actor summonedActor)
    {
        int numMeteors = GameMasterScript.gmsSingleton.ReadTempGameData("num_meteors");
        int meteorCounter = GameMasterScript.gmsSingleton.ReadTempGameData("meteor_counter");
     
        if (meteorCounter >= numMeteors)
        {
            // Matched everything for this spellcast.
            return;
        }
           
        for (int i = 0; i < numMeteors; i++)
        {
            float xPos = GameMasterScript.gmsSingleton.ReadTempFloatData("meteor" + i + "x");
            float yPos = GameMasterScript.gmsSingleton.ReadTempFloatData("meteor" + i + "y");

            if (CustomAlgorithms.CompareFloats(summonedActor.GetPos().x, xPos) && CustomAlgorithms.CompareFloats(summonedActor.GetPos().y, yPos))
            {
                summonedActor.SetActorData("meteoranim", 1);
                meteorCounter++;
                GameMasterScript.gmsSingleton.SetTempGameData("meteor_counter", meteorCounter);
                break;
            }
        }
    }

    public static void TryExtendCalligrapherSummon(SummonActorEffect sae, Actor summonedActor)
    {
        if (GameMasterScript.heroPCActor.myStats.CheckHasActiveStatusName("dualwielderbonus2"))
        {
            summonedActor.turnsToDisappear *= 2;
            summonedActor.maxTurnsToDisappear *= 2;
        }
    }

    public static void FillCombatBiography(SummonActorEffect sae, Actor summonedActor)
    {
        int strokesToWrite = 1;
        int statusCount = GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("brushstroke_charge");
        strokesToWrite += statusCount;
        GameMasterScript.heroPCActor.myStats.RemoveAllStatusByRef("brushstroke_charge");
        if (strokesToWrite > 6) strokesToWrite = 6;
        summonedActor.SetActorData("brushstrokes", strokesToWrite);
    }

}
