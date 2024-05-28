using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AddStatusCustomFunctions
{    

    public static bool AddBasedOnTurnsStandingStill(AddStatusEffect effect)
    {
        Fighter ft = effect.originatingActor as Fighter;
        if (ft.turnsInSamePosition >= 1)
        {
            effect.procChance = 1f;
            return true;
        }
        effect.procChance = 0f;
        return false;
    }

    public static bool ScaleProcChancePerDominatedCreatures(AddStatusEffect effect)
    {
        int numDominated = SharaModeStuff.GetNumberOfDominatedCreatures();

        effect.procChance = numDominated * 0.04f;

        if (effect.procChance >= 0.16f) effect.procChance = 0.16f;

        return true;
    }

    public static bool DominateWeakMonsters(AddStatusEffect effect)
    {
        float dominateHealthValue = 0.35f;

        Fighter ft = effect.originatingActor as Fighter;
        if (ft.myStats.CheckHasStatusName("exp_status_improveddominate"))
        {
            dominateHealthValue = 0.5f;
        }

        foreach (Actor act in effect.targetActors)
        {
            bool skip = false;
            if (act.GetActorType() != ActorTypes.MONSTER)
            {
                skip = true;
            }
            else
            {
                Monster mn = act as Monster;
                if (mn.isChampion || mn.isBoss)
                {
                    skip = true;
                }
                else if (mn.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) > dominateHealthValue) 
                {
                    skip = true;
                }
                /* else if (UnityEngine.Random.Range(0, 1f) > 0.5f)
                {
                    skip = true;
                } */


            }

                
            if (skip)
            {
                effect.skipTargetActors.Add(act);
            }
            else
            {

            }
            
        }
        return true;
    }

    public static bool PlayBuffEffectOnPlants(AddStatusEffect effect)
    {
        foreach(Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            if (act.GetActorType() != ActorTypes.MONSTER) continue;
            Fighter ft = act as Fighter;
            if (!ft.myStats.IsAlive()) continue;
            if (act.actorRefName == "mon_plantturret" || act.actorRefName.Contains("livingvine"))
            {
                CombatManagerScript.SpawnChildSprite("FervirBuff", act, Directions.TRUENEUTRAL, false);
            }
        }
        return true;
    }       

    public static bool SpellshaperEmblemElemShield(AddStatusEffect effect)
    {
        if (CombatManagerScript.damagePayload.damage.damType != DamageTypes.PHYSICAL && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_spellshaperemblem_tier2_elemreact"))
        {
            effect.procChance = 1.0f;
            switch(CombatManagerScript.damagePayload.damage.damType)
            {
                case DamageTypes.FIRE:
                    effect.statusRef = "resistfire15"; 
                    break;
                case DamageTypes.WATER:
                    effect.statusRef = "resistwater15";
                    break;
                case DamageTypes.SHADOW:
                    effect.statusRef = "resistshadow15";
                    break;
                case DamageTypes.POISON:
                    effect.statusRef = "resistpoison15";
                    break;
                case DamageTypes.LIGHTNING:
                    effect.statusRef = "resistlightning15";
                    break;
            }
        }
        else
        {
            effect.procChance = 0f;
        }

        return true;
    }

    public static bool ValkyrieRootCheck(AddStatusEffect effect)
    {
        foreach(Actor act in effect.targetActors)
        {
            if (MapMasterScript.GetGridDistance(act.GetPos(), effect.originatingActor.GetPos()) == 1)
            {
                //Debug.Log("Do not root " + act.actorRefName);
                effect.skipTargetActors.Add(act);
            }
        }

        return true;
    }

    public static bool SoaringHeavenGrabAllActors(AddStatusEffect effect)
    {
        Vector2 prevPos = effect.originatingActor.previousPosition;

        // Draw line

        CustomAlgorithms.GetPointsOnLineNoGarbage(prevPos, effect.originatingActor.GetPos());
        for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
        {
            MapTileData mtd = MapMasterScript.GetTile(CustomAlgorithms.pointsOnLine[i]);
            CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, MapMasterScript.activeMap);
            for (int x = 0; x < CustomAlgorithms.numTilesInBuffer; x++)
            {
                MapTileData mtd2 = CustomAlgorithms.tileBuffer[x];
                foreach (Actor act in mtd2.GetAllActors())
                {
                    if (act.GetActorType() == ActorTypes.MONSTER)
                    {
                        if (!effect.targetActors.Contains(act))
                        {
                            effect.targetActors.Add(act);
                        }
                    }
                }
            }                

        }

        return true;
    }



    public static bool CheckForLethalFists(AddStatusEffect effect)
    {
        Fighter origFighter = effect.originatingActor as Fighter;

        if (origFighter.myStats.CheckHasStatusName("status_lethalfists"))
        {
            effect.procChance = EffectScript.LETHAL_FISTS_PROC_CHANCE;
        }
        else
        {
            effect.procChance = 0.15f;
        }
        
        return true;
    }

    public static bool CheckForBrigandProcBleed(AddStatusEffect effect)
    {
        if ((CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.NATURAL) || (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.MACE) || (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.BOW) || (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.SLING) || (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.STAFF))
        {
            return false;
        }
        return true;
    }

    public static bool PaladinDivineBlock(AddStatusEffect effect)
    {
        int wrathCount = GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("wrathcharge");

        if (wrathCount == 0) 
        {
            effect.procChance = 0f;
            return false;
        }
        GameMasterScript.heroPCActor.myStats.RemoveAllStatusByRef("wrathcharge");

        for (int i = 0; i < wrathCount; i++)
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRef("status_dblock", GameMasterScript.heroPCActor, 10);
        }

        effect.procChance = 1f;
        return false;
    }

}
