using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{
    public static Destructible SummonDestructible(Actor originatingActor, Destructible template, Vector2 summonPosition, int duration, float fDelayBeforeRendering = 0f, bool silent = false)
    {
        //Destructible summoned = new Destructible();
        Destructible summoned = DTPooling.GetDestructible();
        MapTileData mtd = MapMasterScript.GetTile(summonPosition);
        summoned.SetUniqueIDAndAddToDict();
        summoned.CopyFromTemplate(template);

        summoned.turnSummoned = turnNumber;

        if (summoned.dtStatusEffect != null)
        {
            foreach (EffectScript eff in summoned.dtStatusEffect.listEffectScripts)
            {
                eff.originatingActor = originatingActor;
                eff.parentAbility = summoned.dtStatusEffect;
            }
        }

        summoned.SetCurPos(summonPosition);
        summoned.areaID = MapMasterScript.activeMap.CheckMTDArea(mtd);
        summoned.SetSpawnPosXY((int)summonPosition.x, (int)summonPosition.y);
        summoned.SetActorArea(MapMasterScript.activeMap.GetAreaByID(summoned.areaID));
        MapMasterScript.activeMap.AddActorToLocation(summonPosition, summoned);
        MapMasterScript.activeMap.AddActorToMap(summoned);

        if (summoned.actorfaction == Faction.MYFACTION)
        {
            summoned.ChangeMyFaction(originatingActor.actorfaction);
        }

        MapMasterScript.singletonMMS.SpawnDestructible(summoned);
        //MapMasterScript.singletonMMS.WaitThenSummonProp(summoned, 0.25f);        

        summoned.turnsToDisappear = duration;
        summoned.maxTurnsToDisappear = duration;

        summoned.spreadThisTurn = true;
        summoned.summonerID = originatingActor.actorUniqueID;

        Fighter ft = originatingActor as Fighter;
        if (ft != null)
        {
            summoned.summoner = ft;
            ft.AddSummon(summoned);
        }

        summoned.UpdateLastMovedDirection(CombatManagerScript.GetDirection(originatingActor, summoned));
        CheckForSummonDelayAndSpriteEffect(summoned, summonPosition, fDelayBeforeRendering, silent);

        return summoned;
    }

    public static void CheckForSummonDelayAndSpriteEffect(Actor summoned, Vector2 summonPosition, float fDelayBeforeRendering, bool silent = false)
    {
        //Optional hide before rendering
        if (fDelayBeforeRendering > 0f)
        {

            if (PlayerOptions.animSpeedScale != 0f)
            {
                fDelayBeforeRendering *= PlayerOptions.animSpeedScale;
            }

            summoned.SetDelayBeforeRendering(fDelayBeforeRendering);
        }
        //Sometimes play a poofy poof effect when summoned
        else if (!string.IsNullOrEmpty(summoned.spriteRefOnSummon))
        {
            // Don't force play the sound, it might be out of LOS.
            CombatManagerScript.GenerateSpecificEffectAnimation(summonPosition, summoned.spriteRefOnSummon, null, false, 0f, silent);
        }
    }

}