using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class is home to any sort of cool scripted events and animations that can be triggered by various powers, cutscenes, etc.

public class TDAnimationScripts  
{
	public static void TossItemSprite(Sprite spriteToToss, Actor origActor, Actor destinationActor, float time) 
	{
        GameMasterScript.tossProjectileDummy.animLength = time + 0.05f;

        // Put the item prefab on the player
        GameObject go = GameMasterScript.TDInstantiate("GenericTossableItemPrefab");
        go.GetComponent<SpriteRenderer>().sprite = spriteToToss;
        go.transform.position = GameMasterScript.heroPCActor.GetPos();

        CombatManagerScript.FireProjectile(origActor.GetPos(), destinationActor.GetPos(),
            go, time, false, destinationActor.GetObject(), MovementTypes.TOSS,
            GameMasterScript.tossProjectileDummy, 360f, true, true);

		// Reset timing afterward.
        GameMasterScript.tossProjectileDummy.animLength = 0.25f;		
	}

    public static void MakeActorJumpUpAndDown(Actor act, float time, float spin, bool afterImages)
    {
        JumpActorToTargetPoint(act, act.GetPos(), time, spin, afterImages);
    }

    /// <summary>
    /// Moves actor (visually) to the target position. This does not change their game data position. It is strictly visual.
    /// </summary>
    /// <param name="act"></param>
    /// <param name="time"></param>
    /// <param name="spin"></param>
    /// <param name="afterImages"></param>
    public static void JumpActorToTargetPoint(Actor act, Vector2 targetPosition, float time, float spin, bool afterImages)
    {
        // Create a target for our jump (can't be ourselves, must be a dummy)
        GameObject invisibleTarget = GameMasterScript.TDInstantiate("TransparentStairs");
        invisibleTarget.transform.position = targetPosition;

        // Delete the target later
        GameMasterScript.gmsSingleton.WaitThenDestroyObject(invisibleTarget, time + 0.01f);

        // Do the jump effect
        CombatManagerScript.FireProjectile(act.GetPos(),
            targetPosition,
            act.GetObject(),
            time,
            false,
            invisibleTarget,
            MovementTypes.TOSS,
            GameMasterScript.tossProjectileDummy,
            spin,
            false,
            true);

        if (afterImages)
        {
            // Make some images if necessary
            // SMOOTH movement type will just follow the parent.
            GameObject afterImageCreator = GameMasterScript.TDInstantiate("AfterImageCreator");
            afterImageCreator.transform.SetParent(act.GetObject().transform);
            afterImageCreator.transform.localPosition = Vector3.zero;
            afterImageCreator.GetComponent<AfterImageCreatorScript>().Initialize(act.GetPos(), time, Vector2.Distance(act.GetPos(), targetPosition), act.mySpriteRenderer,
                true,
                -1f,
                MovementTypes.SMOOTH,
                6); // number of images lol
        }
    }

}
