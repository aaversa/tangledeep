using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SpecialEffectFunctions 
{
    public static EffectResultPayload SummonRandomFriendlyMonster(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.waitTime = 0f;

        string monsterRef = MapMasterScript.activeMap.dungeonLevelData.spawnTable.GetRandomActorRef();

        Monster newMon = MonsterManagerScript.CreateMonster(monsterRef, false, false, false, 0f, true);
        newMon.myStats.AddStatusByRef("status_permacharmed", GameMasterScript.heroPCActor, 99);

        // These monsters will always be helpful and highly aggressive.
        newMon.aggroRange = 99;
        newMon.RemoveAttribute(MonsterAttributes.PREDATOR);
        newMon.RemoveAttribute(MonsterAttributes.STALKER);
        newMon.RemoveAttribute(MonsterAttributes.GANGSUP);
        newMon.RemoveAttribute(MonsterAttributes.TIMID);
        newMon.actorfaction = Faction.PLAYER;
        newMon.bufferedFaction = Faction.PLAYER;

        newMon.myTargetTile = new Vector2(MapMasterScript.activeMap.columns / 2, MapMasterScript.activeMap.rows / 2);
       
        //MapTileData position = MapMasterScript.activeMap.GetRandomEmptyTile(effect.destructibleOwnerOfEffect.GetPos(), 1, true, true, true, true);
        MapTileData position = MapMasterScript.GetTile(effect.destructibleOwnerOfEffect.GetPos());
        newMon.SetSpawnPos(position.pos);
        MapMasterScript.activeMap.PlaceActor(newMon, position);

        newMon.scriptTakeAction = "AggroAnyEnemyMonster";

        // No need for monster creation FX, we'll do shatter particles instead.
        //CombatManagerScript.GenerateSpecificEffectAnimation(position.pos, "SoundEmanation", null, true);
        MapMasterScript.singletonMMS.SpawnMonster(newMon, true);

        int monSealChance = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("monsealchance");

        if (MapMasterScript.activeMap.ScaleUpToPlayerLevel())
        {
            int lvl = GameMasterScript.heroPCActor.myStats.GetLevel();
            lvl -= 2;
            if (lvl < 1) lvl = 1;
            newMon.ScaleToSpecificLevel(lvl, false, scaleToPlayerLevel: true);
        }

        if (!newMon.isChampion && UnityEngine.Random.Range(0, 101) <= monSealChance)
        {
            newMon.myStats.AddStatusByRef("status_silentsealed", GameMasterScript.heroPCActor, 999);
        }

        return erp; 
    }

    public static EffectResultPayload UnlockGatesBySwitch(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.waitTime = 0f;

        bool unlockAnything = true;
        bool storyGateUnlocked = false;

        // For the bandit hub map, once we press 4 Story Switches, the main gate unlocks in the center.
        if (effect.destructibleOwnerOfEffect.ReadActorData("bandithub_storyswitch") == 1)
        {
            string strcheckFloorGatekeepers = "bandit_gatekeepers" + MapMasterScript.activeMap.floor;
            int numGatekeepersDefeated = GameMasterScript.heroPCActor.ReadActorData(strcheckFloorGatekeepers);
            if (numGatekeepersDefeated < 0) numGatekeepersDefeated = 0;

            numGatekeepersDefeated++;

            // Unlock access to bandit leader
            if (numGatekeepersDefeated >= 4)
            {
                BanditDragonStuff.UnlockGatesOfIndex(999, MapMasterScript.activeMap, false);
                Conversation convo = GameMasterScript.FindConversation("banditdungeon_hub_unlock");
                UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(convo, DialogType.STANDARD, null, 2.2f));
                storyGateUnlocked = true;
            }

            GameMasterScript.heroPCActor.SetActorData(strcheckFloorGatekeepers, numGatekeepersDefeated);

            // If this is a story gate, also unlock a treasure gate that wasn't already unlocked.
            // Do this by looking through all gate indices <999 and pick one that isn't unlocked
            // And change the floorswitch index of this switch.
            int switchToIndex = -1;

            foreach(int gateIndex in MapMasterScript.activeMap.linkSwitchesToGates.Keys)
            {
                if (gateIndex >= 100) continue; // skip story-related gates
                List<Destructible> gates = MapMasterScript.activeMap.linkSwitchesToGates[gateIndex];
                if (gates.Count > 0)
                {
                    if (!gates[0].isDestroyed && !gates[0].destroyed)
                    {
                        switchToIndex = gateIndex;
                        break;
                    }
                }
            }
            if (switchToIndex > -1)
            {
                effect.destructibleOwnerOfEffect.SetActorData("floorswitch_index", switchToIndex);
            }
        }

        GameMasterScript.gmsSingleton.StartCoroutine(TileInteractions.WaitThenBreakDestructible(0.15f, GameMasterScript.heroPCActor, effect.destructibleOwnerOfEffect as Destructible, animation: false));

        int indexOfLinkedGates = effect.destructibleOwnerOfEffect.ReadActorData("floorswitch_index");

        effect.destructibleOwnerOfEffect.SetActorData("runeffect", 1);

        //Debug.Log("Stepped on switch. Attempt to unlock " + indexOfLinkedGates);

        if (indexOfLinkedGates == -1)
        {
            Debug.Log("Actor ID " + effect.destructibleOwnerOfEffect.actorUniqueID + " has no switch index?");
            return erp;
        }        

        unlockAnything = BanditDragonStuff.UnlockGatesOfIndex(indexOfLinkedGates, MapMasterScript.activeMap, true);
        if (unlockAnything)
        {
            GameLogScript.LogWriteStringRef("exp_log_gateopened");
        }
        else
        {
            if (!storyGateUnlocked)
            {
                GameLogScript.LogWriteStringRef("exp_switch_nothing");
            }
            else
            {
                GameMasterScript.cameraScript.AddScreenshake(0.4f);
                UIManagerScript.PlayCursorSound("StoneMovement");
            }
        }
        

        return erp;
    }

    public static IEnumerator PressFloorSwitchWithJuice(List<Destructible> linkedGates)
    {
        //UIManagerScript.PlayCursorSound("PickupItem");
        yield return new WaitForSeconds(0.6f);
        GameMasterScript.cameraScript.AddScreenshake(0.33f);
        UIManagerScript.PlayCursorSound("StoneMovement");
        // Change graphics for each linked gate, "destroy" 'em so they're not collidable
        foreach (Destructible dt in linkedGates)
        {
            TileInteractions.SetDestructibleStateToDestroyed(dt);
        }
        yield return new WaitForSeconds(0.33f);
        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;
    }

    public static EffectResultPayload RevealHiddenMonsterInParentDestructible(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        if (actorsToProcess.Contains(GameMasterScript.heroPCActor))
        {
            Debug.Log("TRIGGER MONSTER!");
        }

        return erp;
    }
}
