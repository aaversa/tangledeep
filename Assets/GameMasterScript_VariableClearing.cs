using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{
    public static void ResetAllVariablesToGameLoadExceptStartData()
    {
        ResetAllVariables();
    }

    public static void ResetVariablesAfterSaveCorruption()
    {
        loadGame_itemWorldPortalOpen = false;
        loadGame_inProgress = false;
        loadGame_creatingResourcePools = false;
        initialGameAwakeComplete = false;
        allResourcesLoaded = false;
        startJob = CharacterJobs.COUNT;

        playerIsScavenger = false;
        actualGameStarted = false;
        gameLoadSequenceCompleted = false;
        heroPC = null;
        heroPCActor = null;
        createdHeroPCActor = false;
        allActorIDs = 1;
        allAbilityIDs = 0;
        ClearActorDict();
        turnNumber = 0;
        UIManagerScript.SetGlobalResponse(DialogButtonResponse.NOTHING);
        TitleScreenScript.CreateStage = CreationStages.TITLESCREEN;
        GameStartData.Initialize();
    }

    static void ResetAllVariables()
    {
        TDInputHandler.initialized = false;
        loadGame_itemWorldPortalOpen = false;
        loadGame_inProgress = false;
        loadGame_creatingResourcePools = false;
        initialGameAwakeComplete = false;
        allResourcesLoaded = false;
        deadActorsToSaveAndLoad.Clear();
        startJob = CharacterJobs.COUNT;
        playerIsScavenger = false;
        actualGameStarted = false;
        gameLoadSequenceCompleted = false;
        playerIsResting = false;
        playerDied = false;
        returningToTownAfterKO = false;
        heroPC = null;
        heroPCActor = null;
        createdHeroPCActor = false;
        allActorIDs = 1;
        allAbilityIDs = 0;
        ClearActorDict();

        turnNumber = 0;

        if (gmsSingleton != null)
        {
            gmsSingleton.animationPlaying = false;
            gmsSingleton.animatingActorsPreventingUpdate = new HashSet<Actor>();
            gmsSingleton.coroutinesPreventingUpdate = new List<ImpactCoroutineWatcher>();
        }

        EffectScript.ResetAllVariablesToGameLoad();

        abilityToTry = null;
        unmodifiedAbility = null;
        bufferedLandingTile = Vector2.zero;
        bufferTargetData.Clear();
        originatingAbility = null;
        processBufferTargetDataIndex = 0;
        playerAttackedThisTurn = false;
        changePlayerTimerThisTurn = false;
        playerMovingAnimation = false;
        deadQueue.Clear();

        endingItemWorld = false;
        checkMoveIndex = 0;
        debug_neverDie = false;
        playerStatsChangedThisTurn = false;
        jobChangeFromNPC = false;

        UIManagerScript.ResetAllVariablesToGameLoad();
        CombatManagerScript.ResetAllVariablesToGameLoad();
        MapMasterScript.ResetAllVariablesToGameLoad();
        BattleTextManager.ResetAllVariablesToGameLoad();
        MonsterCorralScript.ResetAllVariablesToGameLoad();
        CorralBreedScript.ResetAllVariablesToGameLoad();
        ItemWorldUIScript.ResetAllVariablesToGameLoad();
        FoodCartScript.ResetAllVariablesToGameLoad();
        Map.ResetAllVariablesToGameLoad();
        TileInteractions.ResetAllVariablesToGameLoad();

        heroPC = null;
        heroPCActor = null;
        allLoadedNPCs = new List<NPC>();
        dictAllActors = new Dictionary<int, Actor>();
        cameraScript = null;
        mms = null;
        combatManager = null;
        musicManager = null;
        uims = null;
        abilityToTry = null;
        unmodifiedAbility = null;
        itemToUse = null;
        gmsSingleton = null;
        petAttackAbilityDummy = null;
        rangedWeaponAbilityDummy = null;
        tossProjectileDummy = null;
        regenFlaskAbility = null;
        escapeTorchAbility = null;
        kickDummy = null;
        theDungeonActor = null;
        spellshiftMaterializeTemplate = null;
        vitalPointBleedTemplate = null;
        vitalPointExplodeTemplate = null;
        vitalPointPainTemplate = null;
        vitalPointParalyzeTemplate = null;
        brigandBomberTemplate = null;
        spellshaperEvocationEffects = null;
        bufferTargetData = null;
        originatingAbility = null;
        localTurnEffectsFromPlayer = null;

        possibleItems = new List<Item>();

        CreateOrClearStaticContainers();
    }

    public static void ResetAllVariablesToGameLoad()
    {
        ResetAllVariables();
        GameStartData.Initialize();
    }

    static void CreateOrClearStaticContainers()
    {
        if (dictAllActors == null) // Must be first time start, create NEW lists/dicts
        {
            dictAllActors = new Dictionary<int, Actor>(25000);
        }
        else
        {
            dictAllActors.Clear();
        }
        if (bufferTargetData == null)
        {
            bufferTargetData = new List<TargetData>(20);
        }
        else
        {
            bufferTargetData.Clear();
        }

        TDInputHandler.ResetPathfindingVariablesAndLists();


        if (actorListCopy == null)
        {
            actorListCopy = new List<Actor>(2000);
        }

        actorListCopy.Clear();

        if (dtActors == null)
        {
            dtActors = new List<Actor>(2000);
        }

        dtActors.Clear();

        if (actorsThatDoStuff == null)
        {
            actorsThatDoStuff = new List<Fighter>(150);
        }

        actorsThatDoStuff.Clear();

        if (dtTileActors == null)
        {
            dtTileActors = new List<Actor>(2000);
        }

        dtTileActors.Clear();

        if (deadMonstersToRemove == null)
        {
            deadMonstersToRemove = new List<Monster>(25);
        }

        deadMonstersToRemove.Clear();

        if (currentMapActors == null)
        {
            currentMapActors = new List<Actor>(2000);
        }

        currentMapActors.Clear();

        if (dtSpreadPositions == null)
        {
            dtSpreadPositions = new List<Vector2>(50);
        }

        dtSpreadPositions.Clear();

        if (allTargetable == null)
        {
            allTargetable = new List<Actor>(50);
        }

        allTargetable.Clear();

        if (affectedActors == null)
        {
            affectedActors = new List<Actor>(50);
        }

        affectedActors.Clear();

        if (cResults == null)
        {
            cResults = new List<CombatResult>(10);
        }

        cResults.Clear();

        if (pool_monsterList == null)
        {
            pool_monsterList = new List<Monster>(10);
        }

        pool_monsterList.Clear();

        if (pool_removeList == null)
        {
            pool_removeList = new List<Actor>(25);
        }

        pool_removeList.Clear();

        if (pool_targetList == null)
        {
            pool_targetList = new List<Actor>(25);
        }

        pool_targetList.Clear();

        if (pool_MTD == null)
        {
            pool_MTD = new List<MapTileData>(25);
        }

        pool_MTD.Clear();

    }

    public static void ClearTurnVariables()
    {
        gmsSingleton.turnExecuting = false;
        SetAnimationPlaying(false);
        UIManagerScript.RefreshStatuses();
        TDInputHandler.turnTimer = 0.0f;
        playerAttackedThisTurn = false;
        changePlayerTimerThisTurn = false;
        processBufferTargetDataIndex = 0;
    }
}