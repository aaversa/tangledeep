using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostMoveActorCustomFunctions
{
    public static bool FancyTeleportFX(MoveActorEffect effect, Actor actorToProcess, Vector2 oldPos, Vector2 newPos)
    {
        if (actorToProcess.GetActorType() == ActorTypes.HERO)
        {
            actorToProcess.myAnimatable.SetAnim("UseItem");
        }
        
        CombatManagerScript.GenerateSpecificEffectAnimation(oldPos, "TeleportUp", effect, true);
        CombatManagerScript.GenerateSpecificEffectAnimation(newPos, "TeleportDown", effect, true);

        return true;
    }

    public static bool TryAddBrushstrokeFromGlide(MoveActorEffect effect, Actor actorToProcess, Vector2 oldPos, Vector2 newPos)
    {
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_dualwielderemblem_tier1_glide"))
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog("brushstroke_charge", GameMasterScript.heroPCActor, 99);
        }

        return true;
    }

}

public class MoveActorCustomFunctions {

    public static bool ValkyriePushCheck(MoveActorEffect effect)
    {
        foreach (Actor act in effect.targetActors)
        {
            if (MapMasterScript.GetGridDistance(act.GetPos(), effect.originatingActor.GetPos()) > 2)
            {
                //Debug.Log("Do not push " + act.actorRefName);
                effect.skipTargetActors.Add(act);
            }
        }

        return true;
    }
}
