using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public enum SpellcastIcons { NORMAL, LINE, SQUARE, RAY, PENETRATE, MATERIALIZE, AURA, COUNT }

[System.Serializable]
public class TDVisualEffects : MonoBehaviour {

    static TDVisualEffects singleton;

    public static Sprite[] elementCastIcons;
    public static Sprite[] spellcastIcons;

    public static Sprite[][] elementalAuras;

    void Start()
    {
        singleton = this;
        elementCastIcons = Resources.LoadAll<Sprite>("Spritesheets/ElementCastIcons");
        spellcastIcons = Resources.LoadAll<Sprite>("Spritesheets/Spellshapes");

        elementalAuras = new Sprite[(int)DamageTypes.COUNT * 2][];

        //public enum DamageTypes { PHYSICAL, FIRE, POISON, WATER, LIGHTNING, SHADOW, COUNT };

        // Fire index would be 1*2 = 2 for front, 3 for back

        // Poison would be 2*2 = 4 for front, 5 for back
        elementalAuras[(int)DamageTypes.FIRE * 2] = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Aura_FireFront");
        elementalAuras[(int)DamageTypes.FIRE * 2 + 1] = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Aura_FireBack");
        elementalAuras[(int)DamageTypes.WATER * 2] = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Aura_WaterFront");
        elementalAuras[(int)DamageTypes.WATER * 2 + 1] = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Aura_WaterBack");
        elementalAuras[(int)DamageTypes.POISON * 2] = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Aura_PoisonFront");
        elementalAuras[(int)DamageTypes.POISON * 2 + 1] = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Aura_PoisonBack");
        elementalAuras[(int)DamageTypes.SHADOW * 2] = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Aura_ShadowFront");
        elementalAuras[(int)DamageTypes.SHADOW * 2 + 1] = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Aura_ShadowBack");
        elementalAuras[(int)DamageTypes.LIGHTNING * 2] = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Aura_LightningFront");
        elementalAuras[(int)DamageTypes.LIGHTNING * 2 + 1] = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Aura_LightningBack");
    }

    public static Sprite GetElementalAuraSprite(DamageTypes dType, bool front, int frame)
    {
        string frontBackString = "";
        int searchIndex = (int)dType * 2;
        if (front)
        {
            frontBackString = "Front";
        }
        else
        {
            frontBackString = "Back";
            searchIndex++;
        }

        string elemName = "";
        switch(dType)
        {
            case DamageTypes.FIRE:
                elemName = "Fire";
                break;
            case DamageTypes.WATER:
                elemName = "Water";
                break;
            case DamageTypes.LIGHTNING:
                elemName = "Lightning";
                break;
            case DamageTypes.SHADOW:
                elemName = "Shadow";
                break;
            case DamageTypes.POISON:
                elemName = "Poison";
                break;
        }

        string nameRef = "Aura_" + elemName + frontBackString + "_" + frame;
        return UIManagerScript.LoadSpriteFromAtlas(elementalAuras[searchIndex], nameRef);
    }

    public static Sprite GetSpellcastSprite(SpellcastIcons iconType)
    {
        string spriteName = "Spellshapes_" + (int)iconType;
        return UIManagerScript.LoadSpriteFromAtlas(spellcastIcons, spriteName);
    }

    public static Sprite GetElementSprite(DamageTypes dType, int level)
    {
        if (level < 0) level = 0;
        if (level > 2) level = 2;
        // Element order: Fire, Lightning, Water, Poison, Shadow
        int baseIndex = 0;
        switch (dType)
        {
            case DamageTypes.FIRE:
                baseIndex = 0;
                break;
            case DamageTypes.LIGHTNING:
                baseIndex = 1;
                break;
            case DamageTypes.WATER:
                baseIndex = 2;
                break;
            case DamageTypes.POISON:
                baseIndex = 3;
                break;
            case DamageTypes.SHADOW:
                baseIndex = 4;
                break;
        }

        baseIndex = (baseIndex + (level * 5));

        string spriteName = "ElementCastIcons_" + baseIndex;
        //Debug.Log("Search for element sprite: " + spriteName);

        return UIManagerScript.LoadSpriteFromAtlas(elementCastIcons, spriteName);
    }

    public static void ParseAndExecuteSpritePop(string unparsed, Transform followTransform)
    {
        string[] parsed = unparsed.Split('|');
        DamageTypes dType = DamageTypes.COUNT;
        SpellcastIcons sIcon = SpellcastIcons.COUNT;
        int spellLevel = 0;
        bool playWithNoElementalIcon = false;

        for (int i = 0; i < parsed.Length; i++)
        {
            if (parsed[i] == "empty")
            {
                playWithNoElementalIcon = true;
                continue;
            }
            // Did we feed in a damagetype?
            if (i == 0)
            {
                string tryDTEnum = parsed[i].ToUpperInvariant();
                try { dType = (DamageTypes)Enum.Parse(typeof(DamageTypes), tryDTEnum);  }
                catch(Exception e)
                {
                    
                }
            }
            else
            {
                bool foundSpellshape = true;
                if (sIcon == SpellcastIcons.COUNT)
                {
                    string trySpellshapeEnum = parsed[i].ToUpperInvariant();
                    try { sIcon = (SpellcastIcons)Enum.Parse(typeof(SpellcastIcons), trySpellshapeEnum); }
                    catch (Exception e)
                    {
                        foundSpellshape = false;
                    }                    
                }

                if (!foundSpellshape)
                {
                    // OK, maybe spell level?
                    int checkSpellLevel;
                    if (Int32.TryParse(parsed[i], out checkSpellLevel))
                    {
                        spellLevel = checkSpellLevel;
                    }
                }
            }
        }

        if ((dType == DamageTypes.COUNT || dType == DamageTypes.PHYSICAL) && !playWithNoElementalIcon) return;
        if (sIcon == SpellcastIcons.COUNT) sIcon = SpellcastIcons.NORMAL;        
        Sprite castSprite = GetSpellcastSprite(sIcon);
        PopupSprite("", followTransform, true, castSprite);

        if (!playWithNoElementalIcon)
        {
            Sprite damSprite = GetElementSprite(dType, spellLevel);
            PopupSprite("", followTransform, true, damSprite);
        }
    }

    IEnumerator WaitToPopupSprite(string spriteRef, Transform followTransform, float time, Sprite spriteToPop = null)
    {
        yield return new WaitForSeconds(time);
        PopupSprite(spriteRef, followTransform, true, spriteToPop);
    }

    public static void WaitThenPopupSprite(string spriteRef, Transform followTransform, float time, Sprite spriteToPop = null)
    {
        singleton.StartCoroutine(singleton.WaitToPopupSprite(spriteRef, followTransform, time, spriteToPop));
    }

	public static void PopupSprite(string spriteRef, Transform followTransform, bool alsoGrowItemSprite, Sprite spriteToPop = null)
    {
        string prefab = "ItemStaticPopup";
        if (alsoGrowItemSprite)
        {
            prefab = "ItemUsedPopup";
        }

        GameObject popup = GameMasterScript.TDInstantiate(prefab);

        popup.transform.SetParent(followTransform);
        popup.transform.localPosition = new Vector3(0f, 0.6f, 1f);
        Animatable popupAnimatable = popup.GetComponent<Animatable>();

        Sprite itemSprite;

        if (spriteToPop != null)
        {
            itemSprite = spriteToPop;
        }
        else
        {
            itemSprite = UIManagerScript.GetItemSprite(spriteRef);
            spriteToPop = itemSprite;
        }

       
        foreach (zirconAnim.AnimationFrameData afd in popupAnimatable.myAnimations[0].mySprites) // only one anim here
        {
            afd.mySprite = spriteToPop;
        }
        if (popupAnimatable != null)
        {
            popupAnimatable.SetAnim("Default");
        }
        popup.GetComponent<SpriteRenderer>().sprite = spriteToPop;
        
    }

    // #todo - Camera is not always updating properly
    public static void FancyPullAnimation(Actor actorToProcess, Vector2 oldPos, Vector2 newPos, bool spin, float localAnimLength, float arcMult)
    {
        //Debug.Log(actorToProcess.actorRefName + " is being pulled from " + oldPos + " to " + newPos);
        CombatManagerScript.SpawnChildSprite("AggroEffect", actorToProcess, Directions.NORTHEAST, false);
        BattleTextManager.NewText(StringManager.GetString("misc_pulled"), actorToProcess.GetObject(), Color.yellow, 1.5f);

        //clear the existing movement queue
        Movable heroMov = actorToProcess.GetObject().GetComponent<Movable>();
        heroMov.ClearMovementQueue();

        //make sure the map doesn't leave a bogus copy of us in the tile we were actually moving to
        MapMasterScript.activeMap.RemoveActorFromLocation(actorToProcess.GetPos(), actorToProcess);

        heroMov.gameObject.transform.localEulerAngles = Vector3.zero;
        heroMov.gameObject.transform.eulerAngles = Vector3.zero;

        //Play the anim from our actual transform position, since this is likely happening at the start of a move
        Vector2 vTruePosition = (Vector2)heroMov.GetTruePosition();
        Vector2 vTowardPull = newPos - vTruePosition;
        vTowardPull.Normalize();

        //tell our mover that we're actually where our transform is, thus when animations start we won't teleport
        heroMov.position = vTruePosition;

        float animLength = 0.4f;


        if (PlayerOptions.animSpeedScale != 0f)
        {
            animLength *= PlayerOptions.animSpeedScale;
        }

        //set a brief movement away from the pull source
        heroMov.AnimateSetPositionNoChange(vTruePosition + vTowardPull * -0.3f, animLength, false, 0f, 0f, MovementTypes.SLERP);

        //if we are being pulled back to the tile we started from, the AnimateSetPosition will not actually remove us from the tile were moving to.
        //Recall that as soon as movement input is entered, the game places us in the destination tile, even if the sprite hasn't moved yet.
        //So in this case, we should make sure to set ourselves in the current tile.
        if (vTowardPull == Vector2.zero)
        {
            GameMasterScript.mms.MoveAndProcessActor(oldPos, newPos, actorToProcess);
        }
        else
        {
            //then move towards the pull source afterwards
            Movable.lerpMoveData assignedMove = heroMov.AnimateSetPosition(newPos, localAnimLength, false, spin ? 360.0f : 0f, arcMult, MovementTypes.SMOOTH);

            Directions escapeDirection = CombatManagerScript.GetDirection(vTruePosition, vTruePosition + vTowardPull * -1);

            //play a "fun" animation on the player
            actorToProcess.myAnimatable.SetAnimDirectional("Walk", escapeDirection, Directions.NEUTRAL);
            actorToProcess.myAnimatable.speedMultiplier = 4.0f;
           
            MapMasterScript.singletonMMS.ProcessActorAnchorMove(oldPos, newPos, actorToProcess);

            //and ensure that happens during the pull
            if (assignedMove != null)
            {
                assignedMove.strAnimDuringMove = "Walk";
                assignedMove.dirAnimFacingDuringMove = escapeDirection;
                assignedMove.fAnimSpeedMultiplierDuringMove = 4.0f;
            }
        }
        //Don't update the game until we're done moving
        GameMasterScript.PauseUpdateForActorAnimation(actorToProcess);
        actorToProcess.mySpriteRenderer.enabled = true;
        //actorToProcess.UpdateSpriteOrder(newPos);
        CameraController.UpdateCameraPosition(newPos, true);
    }
}
