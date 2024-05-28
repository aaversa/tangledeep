using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Rewired;

public partial class UIManagerScript : MonoBehaviour
{
    public static readonly List<TargetShapes> snappableTargetShapes = new List<TargetShapes>()
    {
        TargetShapes.FLEXCONE,
        TargetShapes.POINT,
        TargetShapes.CLAW,
        TargetShapes.FLEXRECT,
        TargetShapes.SEMICIRCLE
    };

    // Not sure why this was treated as a separate list
    public static readonly List<TargetShapes> rotatableTargetShapes = new List<TargetShapes>()
    {
        TargetShapes.FLEXCONE,
        TargetShapes.CONE,
        TargetShapes.BIGDIPPER,
        TargetShapes.CLAW,
        TargetShapes.FLEXRECT,
        TargetShapes.SEMICIRCLE
    };
  
    public static bool IsTileUsedAlready(Vector2 checkTile)
    {
        if (groundTargetingMesh != null)
        {
            if (groundTargetingMesh.GetComponent<TargetingMeshScript>().usedTiles.Contains(checkTile))
            {
                return true;
            }
        }
        if (cursorTargetingMesh != null)
        {
            if (cursorTargetingMesh.GetComponent<TargetingMeshScript>().usedTiles.Contains(checkTile))
            {
                return true;
            }
        }

        return false;
    }

    public List<Vector2> CreateShapeTileList(TargetShapes shape, AbilityScript abil, Vector2 baseTile, Directions localLineDir, int range, bool playerUser)
    {
        //int range = abil.range;
        int min = -1 * range;
        int max = range;

        bool makeNull = false;

        if (!playerUser)
        {
            abilityInTargeting = abil;
            Vector2 check = TrySnapOffset(localLineDir, abil);
            localTargetOffsetX = (int)check.x + abil.targetOffsetX;
            localTargetOffsetY = (int)check.y + abil.targetOffsetY;
            if (shape == TargetShapes.FLEXLINE)
            {
                switch (localLineDir)
                {
                    case Directions.EAST:
                    case Directions.WEST:
                        shape = TargetShapes.HLINE;
                        break;
                    case Directions.NORTH:
                    case Directions.SOUTH:
                        shape = TargetShapes.VLINE;
                        break;
                    case Directions.NORTHEAST:
                    case Directions.SOUTHWEST:
                        shape = TargetShapes.DLINE_NE;
                        break;
                    case Directions.SOUTHEAST:
                    case Directions.NORTHWEST:
                        shape = TargetShapes.DLINE_SE;
                        break;
                }
            }
            else if (shape == TargetShapes.FLEXCROSS)
            {
                switch (localLineDir)
                {
                    case Directions.EAST:
                    case Directions.WEST:
                        shape = TargetShapes.CROSS;
                        break;
                    case Directions.NORTH:
                    case Directions.SOUTH:
                        shape = TargetShapes.CROSS;
                        break;
                    case Directions.NORTHEAST:
                    case Directions.SOUTHWEST:
                        shape = TargetShapes.XCROSS;
                        break;
                    case Directions.SOUTHEAST:
                    case Directions.NORTHWEST:
                        shape = TargetShapes.XCROSS;
                        break;
                }
            }
            makeNull = true;
        }

        if (!abilityInTargeting.CheckAbilityTag(AbilityTags.CENTERED))
        {
            min = 0;
            max = (range * 2) + 1;
        }


        returnTileList.Clear();

        int numShapes = 1;
        Vector2 secondary = Vector2.zero;
        if (abil.refName == "skill_photoncannon")
        {
            Actor act = GameMasterScript.heroPCActor.GetSummonByRef("mon_runiccrystal");
            if (act != null)
            {
                secondary = act.GetPos();
                numShapes = 2;
            }
        }
        else if (abil.refName == "skill_gravitysurge")
        {
            Actor act = GameMasterScript.heroPCActor.GetSummonByRef("mon_runiccrystal");
            if (act != null)
            {
                baseTile = act.GetPos();
            }
        }

        for (int n = 0; n < numShapes; n++)
        {
            if (n == 1)
            {
                baseTile = secondary;
            }
            switch (shape)
            {
                case TargetShapes.BIGDIPPER:

                    returnTileList.Add(baseTile);

                    // # # x x #
                    // # # # x #
                    // # # c x #
                    // # # x # #
                    // # # x # #
                    // # # x # #


                    // Hardcoded, sorry lol
                    Vector2 checkTile = baseTile;
                    Vector2 coords = Vector2.zero;
                    Vector2 holder = Vector2.zero;
                    Vector2 processTile = Vector2.zero;

                    Vector2[] dipperPoints = new Vector2[7];
                    dipperPoints[0] = new Vector2(0f, -1f);
                    dipperPoints[1] = new Vector2(0f, -2f);
                    dipperPoints[2] = new Vector2(0f, -3f);
                    dipperPoints[3] = new Vector2(1f, 0f);
                    dipperPoints[4] = new Vector2(1f, 1f);
                    dipperPoints[5] = new Vector2(1f, 2f);
                    dipperPoints[6] = new Vector2(0f, 2f);

                    switch (lineDir)
                    {
                        case Directions.NORTH:
                            for (int i = 0; i < dipperPoints.Length; i++)
                            {
                                checkTile = baseTile;
                                checkTile.x += dipperPoints[i].x;
                                checkTile.y += dipperPoints[i].y;
                                returnTileList.Add(checkTile);
                            }
                            break;
                        case Directions.SOUTH:
                            for (int i = 0; i < dipperPoints.Length; i++)
                            {
                                checkTile = baseTile;
                                checkTile.x += (dipperPoints[i].x * -1f);
                                checkTile.y += (dipperPoints[i].y * -1f);
                                returnTileList.Add(checkTile);
                            }
                            break;
                        case Directions.WEST:
                            for (int i = 0; i < dipperPoints.Length; i++)
                            {
                                checkTile = baseTile;
                                checkTile.x += dipperPoints[i].y * -1f;
                                checkTile.y += dipperPoints[i].x;
                                returnTileList.Add(checkTile);
                            }
                            break;
                        case Directions.EAST:
                            for (int i = 0; i < dipperPoints.Length; i++)
                            {
                                checkTile = baseTile;
                                checkTile.x += dipperPoints[i].y;
                                checkTile.y += (dipperPoints[i].x * -1f);
                                returnTileList.Add(checkTile);
                            }
                            break;
                    }
                    break;
                case TargetShapes.POINT:
                    Vector2 check = new Vector2(baseTile.x + localTargetOffsetX, baseTile.y + localTargetOffsetY);
                    //Debug.Log(baseTile + " " + localTargetOffsetX + " " + localTargetOffsetY + " " + abil.targetOffsetX + " " + abil.targetOffsetY);
                    //if (!returnTileList.Contains(check))
                    //{
                    returnTileList.Add(check);
                    //}
                    if (abil.CheckAbilityTag(AbilityTags.FILLTOPOINT))
                    {
                        CustomAlgorithms.GetPointsOnLineNoGarbage(baseTile, check);
                        for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
                        {
                            if ((CustomAlgorithms.pointsOnLine[i] != check) && (CustomAlgorithms.pointsOnLine[i] != baseTile))
                            {
                                returnTileList.Add(CustomAlgorithms.pointsOnLine[i]);
                            }
                        }
                    }
                    break;
                case TargetShapes.FLEXRECT:
                    if (!abilityInTargeting.CheckAbilityTag(AbilityTags.CENTERED))
                    {
                        min = 1;
                    }
                    else
                    {
                        min = 0;
                    }

                    int width = 1;
                    if (range == 4) width = 2;
                    if (range == 5) width = 3;

                    for (int i = 0; i < range; i++) // Number of rows in the rect
                    {
                        //for (int v = -1 * i; v <= i; v++)
                        for (int v = width * -1; v <= width; v++)
                        {
                            coords = new Vector2(baseTile.x + v + localTargetOffsetX, baseTile.y + i + min + localTargetOffsetY); // Faces north.
                            switch (localLineDir)
                            {
                                case Directions.NORTH:
                                    coords = new Vector2(baseTile.x + v + localTargetOffsetX, baseTile.y + i + min + localTargetOffsetY); // Faces north.
                                    break;
                                case Directions.EAST:
                                    coords = new Vector2(baseTile.x + i + min + localTargetOffsetX, baseTile.y + v + localTargetOffsetY);
                                    break;
                                case Directions.SOUTH:
                                    coords = new Vector2(baseTile.x + v + localTargetOffsetX, baseTile.y - i - min + localTargetOffsetY);
                                    break;
                                case Directions.WEST:
                                    coords = new Vector2(baseTile.x - i - min + localTargetOffsetX, baseTile.y + v + localTargetOffsetY);
                                    break;
                            }

                            returnTileList.Add(coords);
                        }
                    }
                    break;
                case TargetShapes.RECT:
                    for (int row = min; row <= max; row++)
                    {
                        for (int column = min; column <= max; column++)
                        {
                            coords = new Vector2(baseTile.x + row + localTargetOffsetX, baseTile.y + column + localTargetOffsetY);
                            returnTileList.Add(coords);
                        }
                    }
                    break;
                case TargetShapes.RANDOM:
                    for (int row = min; row <= max; row++)
                    {
                        for (int column = min; column <= max; column++)
                        {
                            coords = new Vector2(baseTile.x + row + localTargetOffsetX, baseTile.y + column + localTargetOffsetY);
                            if (UnityEngine.Random.Range(0, 1f) <= abilityInTargeting.randomChance)
                            {
                                returnTileList.Add(coords);
                            }
                        }
                    }
                    break;
                case TargetShapes.CHECKERBOARD:
                    bool even = false;
                    for (int row = min; row <= max; row++)
                    {
                        even = !even;
                        for (int column = min; column <= max; column++)
                        {
                            if (((even) && (((baseTile.y + column + localTargetOffsetY) % 2) != 0)) || ((!even) && (((baseTile.y + column + localTargetOffsetY) % 2) == 0)))
                            {
                                continue;
                            }
                            coords = new Vector2(baseTile.x + row + localTargetOffsetX, baseTile.y + column + localTargetOffsetY);
                            returnTileList.Add(coords);
                        }
                    }
                    break;
                case TargetShapes.CONE:
                case TargetShapes.FLEXCONE:
                case TargetShapes.CLAW:
                    // Default is that cone faces upward.
                    if (!abilityInTargeting.CheckAbilityTag(AbilityTags.CENTERED))
                    {
                        min = 1;
                    }
                    else
                    {
                        min = 0;
                    }
                    for (int i = 0; i < range; i++) // Number of rows in the cone, expands outward
                    {
                        for (int v = -1 * i; v <= i; v++)
                        {
                            if (shape == TargetShapes.CLAW)
                            {
                                if ((v != -1 * i) && (v != i) && (v != 0))
                                {
                                    continue;
                                }
                            }
                            coords = new Vector2(baseTile.x + v + localTargetOffsetX, baseTile.y + i + min + localTargetOffsetY); // Faces north.
                            switch (localLineDir)
                            {
                                case Directions.NORTH:
                                    coords = new Vector2(baseTile.x + v + localTargetOffsetX, baseTile.y + i + min + localTargetOffsetY); // Faces north.
                                    break;
                                case Directions.EAST:
                                    coords = new Vector2(baseTile.x + i + min + localTargetOffsetX, baseTile.y + v + localTargetOffsetY);
                                    break;
                                case Directions.SOUTH:
                                    coords = new Vector2(baseTile.x + v + localTargetOffsetX, baseTile.y - i - min + localTargetOffsetY);
                                    break;
                                case Directions.WEST:
                                    coords = new Vector2(baseTile.x - i - min + localTargetOffsetX, baseTile.y + v + localTargetOffsetY);
                                    break;
                            }

                            returnTileList.Add(coords);
                        }
                    }

                    break;
                case TargetShapes.CROSS:
                    // Row first
                    for (int i = min; i <= max; i++)
                    {
                        processTile = new Vector2(baseTile.x + i + localTargetOffsetX, baseTile.y + localTargetOffsetY);
                        returnTileList.Add(processTile);
                    }
                    // Now column
                    // Vertical column
                    for (int i = min; i <= max; i++)
                    {
                        processTile = new Vector2(baseTile.x + localTargetOffsetX, baseTile.y + i + localTargetOffsetY);
                        returnTileList.Add(processTile);
                    }
                    break;
                case TargetShapes.BURST:
                    // Row first
                    for (int i = min; i <= max; i++)
                    {
                        processTile = new Vector2(baseTile.x + i + localTargetOffsetX, baseTile.y + localTargetOffsetY);
                        returnTileList.Add(processTile);
                    }
                    // Vertical column
                    for (int i = min; i <= max; i++)
                    {
                        processTile = new Vector2(baseTile.x + localTargetOffsetX, baseTile.y + i + localTargetOffsetY);
                        returnTileList.Add(processTile);
                    }
                    for (int row = -1 * (range - 1); row <= (range - 1); row++) // Special case for bursts, no need to worry about line stuff.
                    {
                        for (int column = -1 * (range - 1); column <= (range - 1); column++)
                        {
                            coords = new Vector2(baseTile.x + row + localTargetOffsetX, baseTile.y + column + localTargetOffsetY);
                            returnTileList.Add(coords);
                        }
                    }
                    break;
                case TargetShapes.XCROSS:
                    for (int i = min; i <= max; i++)
                    {
                        processTile = new Vector2(baseTile.x + i + localTargetOffsetX, baseTile.y + i + localTargetOffsetY);
                        returnTileList.Add(processTile);
                    }
                    for (int i = min; i <= max; i++)
                    {
                        processTile = new Vector2(baseTile.x + i + localTargetOffsetX, baseTile.y - i - localTargetOffsetY);
                        returnTileList.Add(processTile);
                    }
                    break;
                case TargetShapes.HLINE:
                    for (int i = min; i <= max; i++)
                    {
                        int mod = i;
                        if (localLineDir == Directions.WEST)
                        {
                            mod *= -1;
                        }
                        processTile = new Vector2(baseTile.x + mod + localTargetOffsetX, baseTile.y + localTargetOffsetY);
                        returnTileList.Add(processTile);
                    }
                    break;
                case TargetShapes.VLINE:
                    for (int i = min; i <= max; i++)
                    {
                        int mod = i;
                        if (localLineDir == Directions.SOUTH)
                        {
                            mod *= -1;
                        }
                        processTile = new Vector2(baseTile.x + localTargetOffsetX, baseTile.y + mod + localTargetOffsetY);
                        returnTileList.Add(processTile);
                    }
                    break;
                case TargetShapes.DLINE_NE: // Diagonal line going from southwest to northeast
                    for (int i = min; i <= max; i++)
                    {
                        int mod = i;
                        if (localLineDir == Directions.SOUTHWEST)
                        {
                            mod *= -1;
                        }
                        processTile = new Vector2(baseTile.x + mod + localTargetOffsetX, baseTile.y + mod + localTargetOffsetY);
                        returnTileList.Add(processTile);
                    }
                    break;
                case TargetShapes.DLINE_SE: // Diagonal line going from northwest to southeast
                    for (int i = min; i <= max; i++)
                    {
                        int mod = i;
                        if (localLineDir == Directions.NORTHWEST)
                        {
                            mod *= -1;
                        }
                        processTile = new Vector2(baseTile.x + mod + localTargetOffsetX, baseTile.y - mod - localTargetOffsetY);
                        returnTileList.Add(processTile);
                    }
                    break;
                case TargetShapes.SEMICIRCLE:
                    if (!abilityInTargeting.CheckAbilityTag(AbilityTags.CENTERED))
                    {
                        min = 1;
                    }
                    else
                    {
                        min = 0;
                    }

                    width = 1;
                    if (range == 4) width = 2;
                    if (range == 5) width = 3;

                    for (int i = 0; i < range-1; i++) // Number of rows in the rect
                    {
                        for (int v = width * -1; v <= width; v++)
                        {
                            coords = new Vector2(baseTile.x + v + localTargetOffsetX, baseTile.y + i + min + localTargetOffsetY); // Faces north.
                            switch (localLineDir)
                            {
                                case Directions.NORTH:
                                    coords = new Vector2(baseTile.x + v + localTargetOffsetX, baseTile.y + i + min + localTargetOffsetY); // Faces north.
                                    break;
                                case Directions.EAST:
                                    coords = new Vector2(baseTile.x + i + min + localTargetOffsetX, baseTile.y + v + localTargetOffsetY);
                                    break;
                                case Directions.SOUTH:
                                    coords = new Vector2(baseTile.x + v + localTargetOffsetX, baseTile.y - i - min + localTargetOffsetY);
                                    break;
                                case Directions.WEST:
                                    coords = new Vector2(baseTile.x - i - min + localTargetOffsetX, baseTile.y + v + localTargetOffsetY);
                                    break;
                            }

                            returnTileList.Add(coords);
                        }
                    }
                    break;
                case TargetShapes.CIRCLE:
                    for (int row = min; row <= max; row++)
                    {
                        for (int column = min; column <= max; column++)
                        {
                            processTile = new Vector2(baseTile.x + row + localTargetOffsetX, baseTile.y + column + localTargetOffsetY);
                            if (row == min || row == max)
                            {
                                returnTileList.Add(processTile);
                            }
                            else if (column == min || column == max)
                            {
                                returnTileList.Add(processTile);
                            }
                        }
                    }
                    break;
                case TargetShapes.CIRCLECORNERS:
                    for (int row = min; row <= max; row++)
                    {
                        for (int column = min; column <= max; column++)
                        {
                            processTile = new Vector2(baseTile.x + row + localTargetOffsetX, baseTile.y + column + localTargetOffsetY);
                            if (((row == min) || (row == max)) && ((column == min) || (column == max)))
                            {
                                returnTileList.Add(processTile);
                            }
                        }
                    }
                    break;
                case TargetShapes.DIRECTLINE:
                case TargetShapes.DIRECTLINE_THICK:
                    // Must have an actor specified.
                    int actorID = GameMasterScript.gmsSingleton.ReadTempGameData("directline_actor_target");
                    Actor target = GameMasterScript.gmsSingleton.TryLinkActorFromDict(actorID);

                    // Go X spaces past the target position.
                    
                    int maxDist = abilityInTargeting.range;
                    int distCounter = 0;

                    Vector2 pos1 = abilityUser.GetPos();
                    Vector2 pos2 = target.GetPos();
                    Vector2 sum = pos2 - pos1;
                    sum.Normalize();
                    bool checkValid = true;
                    Vector2 marker = pos1;
                    bool pastOriginalPoint = false;

                    //Larger values make for faster checks but the possibility of skipping tile corners
                    float fStepSize = 0.1f;

                    Vector2 step = new Vector2(sum.x * fStepSize, sum.y * fStepSize);
                    MapTileData evaluateTile = MapMasterScript.GetTile(pos1);
                    Vector2 floorPos2 = new Vector2(Mathf.Floor(pos2.x), Mathf.Floor(pos2.y));

                    if (evaluateTile == null)
                    {
                        //Debug.Log("Direct line error occurred.");
                        break;
                    }

                    List<Vector2> preReturnTileList = new List<Vector2>();

                    // Take small steps from start to finish along the normalized V2 Sum, adding all tiles along the way
                    int counter = 0;
                    while (checkValid)
                    {
                        marker += step;

                        Vector2 dirToEnd = (pos2 - marker);
                        float dotProduct = Vector2.Dot(sum, dirToEnd);
                        if (dotProduct < 0)
                        {
                            pastOriginalPoint = true;
                        }

                        Vector2 checkVector = new Vector2(Mathf.Floor(marker.x), Mathf.Floor(marker.y));

                        counter++;
                        if (counter >= 5000)
                        {
                            Debug.Log("<color=red>LOS FAILURE</color>");
                            Debug.Log("From " + pos1 + " to " + pos2 + ", check vector is " + checkVector + " and check tile is " + evaluateTile.pos + " and step is " + step + " and marker is " + marker);
                            break;
                        }

                        if (checkVector != evaluateTile.pos)
                        {
                            if (!MapMasterScript.InBounds(checkVector))
                            {
                                break;
                            }
                            if (pastOriginalPoint)
                            {
                                distCounter++;
                            }

                            preReturnTileList.Add(checkVector);

                            evaluateTile = MapMasterScript.GetTile(checkVector);


                            if (distCounter >= maxDist)
                            {
                                abilityUser.SetActorDataString("line_final_point", checkVector.x.ToString() +"|" + checkVector.y.ToString());
                                break;
                            }
                        }
                    }

                    if (shape == TargetShapes.DIRECTLINE_THICK)
                    {
                        for (int i = 0; i < preReturnTileList.Count; i++)
                        {
                            for (int x = 0; x < MapMasterScript.xDirections.Length; x++)
                            {
                                Vector2 addPos = MapMasterScript.xDirections[x] + preReturnTileList[i];
                                returnTileList.Add(addPos);
                            }
                        }
                    }

                    foreach(Vector2 v2 in preReturnTileList)
                    {
                        returnTileList.Add(v2);
                    }

                    /* CustomAlgorithms.GetPointsOnLineNoGarbage(abilityUser.GetPos(), target.GetPos());
                    for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
                    {
                        returnTileList.Add(CustomAlgorithms.pointsOnLine[i]);
                        if (shape == TargetShapes.DIRECTLINE_THICK)
                        {
                            for (int x = 0; x < MapMasterScript.xDirections.Length; x++)
                            {
                                Vector2 addPos = MapMasterScript.xDirections[x] + CustomAlgorithms.pointsOnLine[i];
                                returnTileList.Add(addPos);
                            }
                        }
                    } */

                    break;
            }
        }

        if (makeNull)
        {
            abilityInTargeting = null;
        }

        return returnTileList.ToList();
    }

    private void UpdateGroundTargetingTiles(TargetingMeshScript tms, Vector2 baseTile, AbilityScript abil, bool buildVisibleMesh)
    {
        if (!string.IsNullOrEmpty(abil.script_SpecialTargeting))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(GameplayScripts), abil.script_SpecialTargeting);
            if (runscript != null)
            {
                object[] paramList = new object[4];
                paramList[0] = tms;
                paramList[1] = baseTile;
                paramList[2] = lineDir;
                paramList[3] = abil;
                tms = runscript.Invoke(null, paramList) as TargetingMeshScript;
                tms.BuildMesh();
                return;
            }
        }

        bool isPlayerRangedWeaponAttack = abilityUser.GetActorType() == ActorTypes.HERO && abil == GameMasterScript.rangedWeaponAbilityDummy;

        List<Vector2> allTiles = CreateShapeTileList(GetFlexShape(abilityInTargeting.boundsShape), abilityInTargeting, baseTile, lineDir, abil.range, true);
        List<Vector2> allTilesProcess = new List<Vector2>();
        tms.goodTiles.Clear();
        tms.badTiles.Clear();
        tms.reqOpenTiles.Clear();
        foreach (Vector2 vec in allTiles)
        {
            allTilesProcess.Add(vec);
        }

        // Now remove stuff outside of map bounds, completely.
        foreach (Vector2 coords in allTiles)
        {
            if ((coords.x < 0) || (coords.x >= MapMasterScript.activeMap.columns) || (coords.y < 0) || (coords.y >= MapMasterScript.activeMap.rows))
            {
                allTilesProcess.Remove(coords);
            }
        }

        switch (abil.landingTile)
        {
            case LandingTileTypes.ENDOFLINE:
                if ((abilityInTargeting.boundsShape == TargetShapes.FLEXLINE) || (abilityInTargeting.boundsShape == TargetShapes.HLINE) || (abilityInTargeting.boundsShape == TargetShapes.VLINE))
                {
                    Vector2 endOfLine = Vector2.zero;
                    if (abil.CheckAbilityTag(AbilityTags.CENTERED))
                    {
                        endOfLine = baseTile + (MapMasterScript.xDirections[(int)lineDir] * (abil.range + 1));
                    }
                    else
                    {
                        endOfLine = baseTile + (MapMasterScript.xDirections[(int)lineDir] * ((abil.range * 2) + 2));
                    }
                    //Vector2 endOfLine = baseTile + (MapMasterScript.xDirections[(int)lineDir] * (abil.range+1));
                    tms.reqOpenTiles.Add(endOfLine);
                    GameMasterScript.bufferedLandingTile = endOfLine;
                }
                break;
        }

        // Now evaluate based on tags to create Good and Badtiles lists.

        bool summonOnCollidableAbility = abil.DoesAbilityHaveSummonOnCollidableEffects();

        Actor checkForActor;

        TargetingMeshScript groundTMS = groundTargetingMesh.GetComponent<TargetingMeshScript>();

        bool friendlyFirePossibleForHero = false;
        if (abilityUser.GetActorType() == ActorTypes.HERO)
        {
            friendlyFirePossibleForHero = GameMasterScript.heroPCActor.IsFriendlyFirePossible();
        }

        foreach (Vector2 coords in allTilesProcess)
        {
            //Debug.Log(coords);
            bool possible = false;
            if (abil.CheckAbilityTag(AbilityTags.TVISIBLEONLY))
            {
                Fighter fAbilityUser = abilityUser as Fighter;

                // This was using the visible tile array before
                //Debug.Log(MapMasterScript.CheckTileToTileLOS(coords, abilityUser.GetPos(), abilityUser) + " " + coords);

                bool tileVisibleToHero = true;
                bool userIsHero = false;

                if (abilityUser.GetActorType() == ActorTypes.HERO)
                {
                    userIsHero = true;

                    bool forcefields = false;
                    // Some maps may have forcefields which mess with visibility. We can SEE through forcefields but shouldn't be able to jump through them.
                    int ffValue = 0;
                    if (MapMasterScript.activeMap.dungeonLevelData.GetMetaData("forcefields") == 1)
                    {
                        // let's make absolutely sure we can see this tile and that there are no forcefields blocking us
                        forcefields = true;                      
                    }

                    tileVisibleToHero = GameMasterScript.heroPCActor.CheckIfTileIsTrulyVisible(coords, viewerIsHero:true, treatForceFieldsAsBlocking:forcefields);
                }

                if (tileVisibleToHero && 
                    (coords == abilityUser.GetPos() || userIsHero || MapMasterScript.CheckTileToTileLOS(coords, abilityUser.GetPos(), abilityUser, MapMasterScript.activeMap))) 
                {
                    if (!abil.CheckAbilityTag(AbilityTags.GROUNDTARGET))
                    {
                        checkForActor = MapMasterScript.GetTargetableAtLocation(coords);
                        if (checkForActor != null)
                        {
                            possible = true;

                            // Some kind of offense
                            if ((abilityInTargeting.targetForMonster == AbilityTarget.ENEMY || abilityInTargeting.targetForMonster == AbilityTarget.SUMMONHAZARD) 
                                && abilityInTargeting.CheckAbilityTag(AbilityTags.MONSTERAFFECTED))
                            {                                
                                if (checkForActor.actorfaction == abilityUser.actorfaction && !friendlyFirePossibleForHero)
                                {
                                    groundTMS.badTiles.Add(coords);
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            // Ability requires a monster target, but no target was found.
                            groundTMS.badTiles.Add(coords);
                        }
                    }
                    else
                    {
                        // Ability requires visibility to tile, and the tile is in line of sight. ground targeted.
                        possible = true;
                        checkForActor = MapMasterScript.GetTargetableAtLocation(coords);
                        if (checkForActor != null)                            
                        {
                            if ((abilityInTargeting.targetForMonster == AbilityTarget.ENEMY 
                                || abilityInTargeting.targetForMonster == AbilityTarget.SUMMONHAZARD)
                                && abilityInTargeting.CheckAbilityTag(AbilityTags.MONSTERAFFECTED))
                            {
                                if (checkForActor.actorfaction == abilityUser.actorfaction && !friendlyFirePossibleForHero)
                                {
                                    //possible = false;
                                    //Debug.Log("Targeting: " + coords + " added to the <color=red>Bad Place</color> because a friendly is on it.");
                                    groundTMS.badTiles.Add(coords);
                                    continue;
                                }
                            }
                        }

                    }
                }
                else
                {
                    // Ability requries visibility to tile, but tile is NOT in line of sight.
                    //Debug.Log("Targeting: " + coords + " added to the <color=red>Bad Place</color> because it is out of LOS");
                    groundTMS.badTiles.Add(coords);
                    //Debug.Log("Not in los");
                }
            }
            else
            {
                // Doesn't need to be visible only.
                possible = true;
            }
            if (possible)
            {

                MapTileData checkTile = MapMasterScript.GetTile(coords);
                if (isPlayerRangedWeaponAttack)
                {
                    // Destructibles SHOULD be targetable with ranged weapons.
                    if (checkTile.tileType == TileTypes.GROUND && checkTile.HasBreakableCollidable(GameMasterScript.heroPCActor))
                    {
                        possible = true;
                        tms.goodTiles.Add(coords);
                        continue;
                    }
                }
                if (abil.CheckAbilityTag(AbilityTags.CLEARGROUNDONLY))
                {
                    possible = true;
                    if ((MapMasterScript.CheckAdjacentTileType(coords, TileTypes.WALL, true)) || (MapMasterScript.CheckAdjacentTileType(coords, TileTypes.NOTHING, true)) || (MapMasterScript.CheckAdjacentTileType(coords, TileTypes.MAPEDGE, true)))
                    {
                        possible = false;
                    }

                }

                //Debug.Log("Does " + abil.refName + " have AdjWallOnly? " + abil.CheckAbilityTag(AbilityTags.ADJACENTWALLONLY));

                if (abil.CheckAbilityTag(AbilityTags.ADJACENTWALLONLY) && possible)
                {
                    possible = false;
                    if ((MapMasterScript.CheckAdjacentTileType(coords, TileTypes.WALL, true)) || (MapMasterScript.CheckAdjacentTileType(coords, TileTypes.NOTHING, true)) || (MapMasterScript.CheckAdjacentTileType(coords, TileTypes.MAPEDGE, true)))
                    {
                        possible = true;
                    }

                    //Added the ability to hookshot / jump off of certain solid objects
                    if (!possible)
                    {
                        foreach (var targetable in MapMasterScript.GetAllTargetablePlusDestructibles( MapMasterScript.activeMap.GetListOfTilesAroundPoint(coords,1) ))
                        {
                            if (targetable.ReadActorData("pretendadjacentwall") == 1)
                            {
                                possible = true;
                            }
                                
                        }
                    }
                }
                if (abil.CheckAbilityTag(AbilityTags.GROUNDONLY) && possible)
                {
                    if (MapMasterScript.GetTile(coords).tileType == TileTypes.GROUND)
                    {
                        possible = true;
                    }
                    else
                    {
                        possible = false;
                    }
                    //Debug.Log("Ground only " + possible);
                }

                if (!abil.CheckAbilityTag(AbilityTags.EMPTYONLY) && abil.targetForMonster == AbilityTarget.ENEMY && possible && !summonOnCollidableAbility)
                {
                    if (MapMasterScript.GetTile(coords).HasPlayerTargetableDestructibleButNoMonsters())
                    {
                        possible = false;
                    }
                }

                if (abil.CheckAbilityTag(AbilityTags.WALLONLY) && possible)
                {
                    if ((MapMasterScript.GetTile(coords).tileType == TileTypes.WALL) || (MapMasterScript.GetTile(coords).tileType == TileTypes.MAPEDGE) || (MapMasterScript.GetTile(coords).tileType == TileTypes.NOTHING))
                    {
                        possible = true;
                    }
                    else
                    {
                        possible = false;
                    }
                }

                if (abil.CheckAbilityTag(AbilityTags.REQELEMENTALAFFINITY) && possible)
                {
                    MapTileData mtd = MapMasterScript.GetTile(coords);
                    possible = false;
                    for (int i = 0; i < (int)ObjectFlags.COUNT; i++)
                    {
                        if (mtd.GetObjectFlagAmount((ObjectFlags)i) > 0)
                        {
                            possible = true;
                            break;
                        }
                    }
                }

                if (abil.CheckAbilityTag(AbilityTags.ADJACENTGROUNDONLY) && possible)
                {
                    possible = MapMasterScript.CheckAdjacentTileType(coords, TileTypes.GROUND, true);
                }
                if (abil.requireTargetRef != null && abil.requireTargetRef.Count > 0)
                {
                    checkTile = MapMasterScript.GetTile(coords);

                    bool anyTarget = false;
                    possible = false;

                    foreach (string act in abil.requireTargetRef)
                    {
                        anyTarget = checkTile.CheckActorRef(act);
                        if (anyTarget)
                        {
                            possible = true;
                            break;
                        }
                    }

                }

                if (abil.CheckAbilityTag(AbilityTags.WATERONLY) && possible)
                {
                    if (checkTile.CheckTag(LocationTags.WATER) || checkTile.CheckTag(LocationTags.ISLANDSWATER))
                    {
                        possible = true;
                    }
                    else
                    {
                        possible = false;
                    }
                }

                if (abil.CheckAbilityTag(AbilityTags.EMPTYONLY) && possible)
                {
                    MapTileData mtd = MapMasterScript.GetTile(coords);
                    if (mtd.HasImpassableDestructible(abilityUser) || mtd.CheckTag(LocationTags.TREE) || mtd.CheckTag(LocationTags.SOLIDTERRAIN))
                    {
                        possible = false;
                    }
                    else if ((mtd.GetInteractableNPC() == null) && (((MapMasterScript.GetAllTargetableAtLocation(coords) == null) || (MapMasterScript.GetAllTargetableAtLocation(coords).Count == 0)) && (mtd.GetBreakableCollidable(abilityUser) == null)))
                    {
                        possible = true;
                    }
                    else
                    {
                        possible = false;
                    }
                    //Debug.Log("Empty only " + possible);
                }
              

                if (abil.CheckAbilityTag(AbilityTags.NOCHAMPIONS) && possible)
                {
                    Actor act = MapMasterScript.GetTargetableAtLocation(coords);
                    if (act != null)
                    {
                        if (act.GetActorType() == ActorTypes.MONSTER)
                        {
                            Monster mn = act as Monster;
                            if (mn.isBoss || mn.isChampion)
                            {
                                possible = false;
                            }
                            else
                            {
                                possible = true;
                            }
                        }
                        else
                        {
                            possible = false;
                        }

                    }

                    //Debug.Log("Empty only " + possible);
                }

                //Debug.Log("Final verdict" + possible);
                if (possible)
                {
                    //Debug.Log("Targeting: " + coords + " added to the <color=green>Good Place</color> because possible == true");
                    tms.goodTiles.Add(coords);
                }
                else
                {
                    //Debug.Log("Targeting: " + coords + " added to the <color=red>Bad Place</color> because possible == false");
                    tms.badTiles.Add(coords);
                }
            }
            else
            {
                // Ability has no visibility restrictions for targeting, so the tile is good.
                possible = true;
            }
        }
        if (buildVisibleMesh)
        {
            tms.BuildMesh();
        }
    }

    public static void UpdateTargetingMeshes()
    {
        List<Vector2> usedTiles = GameMasterScript.GetAllBufferedTargetTiles();
        if (singletonUIMS.abilityInTargeting.CheckAbilityTag(AbilityTags.TARGETUSEDTILES))
        {
            usedTiles.Clear();
        }
        if (groundTargetingMesh != null)
        {
            TargetingMeshScript gtms = groundTargetingMesh.GetComponent<TargetingMeshScript>();
            gtms.usedTiles = usedTiles;
            gtms.BuildMesh();
        }
        if (cursorTargetingMesh != null)
        {
            TargetingMeshScript ctms = cursorTargetingMesh.GetComponent<TargetingMeshScript>();
            ctms.usedTiles = usedTiles;
            ctms.BuildMesh();
        }
    }

    public void EnterTargeting(AbilityScript abil, Directions prevDir)
    {
        singletonUIMS.CloseExamineMode();
        if (abilityTargeting)
        {
            return;
        }


        if (groundTargetingMesh != null)
        {
            //Destroy(groundTargetingMesh);
            GameMasterScript.ReturnToStack(groundTargetingMesh, "TargetingMesh");
        }
        if (cursorTargetingMesh != null)
        {
            //Destroy(cursorTargetingMesh);
            GameMasterScript.ReturnToStack(cursorTargetingMesh, "CursorTargetingMesh");
        }
        abilityInTargeting = abil;
        //Debug.Log("Abil in targeting is now " + abil.myID + " " + abil.CheckAbilityTag(AbilityTags.MONSTERAFFECTED));

        bool tryTargetToNearbyMonsters = false;
        if (abil.CheckAbilityTag(AbilityTags.TARGETED) && !abil.CheckAbilityTag(AbilityTags.CURSORTARGET) && abil.CheckAbilityTag(AbilityTags.CANROTATE))
        {
            if (abil.targetForMonster == AbilityTarget.ENEMY)
            {
                tryTargetToNearbyMonsters = true;
            }
        }

        if (prevDir != Directions.NEUTRAL)
        {
            lineDir = prevDir;
            if (abilityInTargeting.direction == Directions.INHERIT)
            {
                abilityInTargeting.direction = lineDir;
                abilityInTargeting.lineDir = lineDir;
            }
        }

        localTargetOffsetX = abil.targetOffsetX;
        localTargetOffsetY = abil.targetOffsetY;

        // #todo - this may be causing a memory leak
        // Change the cursor

        /* Texture2D newCursorTexture = new Texture2D(32, 32);
        CursorMode mode = CursorMode.Auto;
        Vector2 hotspot = Vector2.zero;        
        newCursorTexture.LoadImage(mouseCursorImageAsset.bytes);
        Cursor.SetCursor(newCursorTexture, hotspot, mode); */
        CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.TARGET);


        lastPhysicalMousePosition = Input.mousePosition;

        abilityTargeting = true;

        // Set helpful description

        string descToUse = abil.shortDescription;
        if (string.IsNullOrEmpty(abil.shortDescription))
        {
            if (GameMasterScript.itemBeingUsed != null)
            {
                Consumable c = GameMasterScript.itemBeingUsed as Consumable;
                if (c != null)
                {
                    descToUse = c.effectDescription;
                }
            }
        }

        descToUse = CustomAlgorithms.ReplaceVariousPoundDelimitedVariables(descToUse);

        string buildText = "<color=yellow>" + abil.abilityName + "</color>: " + descToUse;
        string dispCosts = abil.GetDisplayCosts();
        if (dispCosts != "")
        {
            buildText += " (" + dispCosts + ")";
        }
        

        if (abil.refName == "skill_wildcards")
        {
            buildText = ReturnGamblerHandText();
        }

        if (abil.CheckAbilityTag(AbilityTags.CANROTATE))
        {
            if (abil.CheckAbilityTag(AbilityTags.CURSORTARGET))
            {
                string txt = CustomAlgorithms.GetButtonAssignment("Rotate Targeting Shape");
                
                if (!PlatformVariables.GAMEPAD_ONLY)
                {
                    txt += ", " + StringManager.GetString("ui_mousewheel");
                }

                StringManager.SetTag(0, txt);

                buildText += " <color=yellow>" + StringManager.GetString("ui_rotate_info") + "</color>";
            }
            else
            {
                buildText += " <color=yellow>" + StringManager.GetString("ui_rotate_direction_info") + "</color>";
            }
        }

        ShowGenericInfoBar();
        SetInfoText(buildText);
        bufferInfoBarText = buildText;

        //groundTargetingMesh = Instantiate(GameMasterScript.GetResourceByRef("TargetingMesh"));
        groundTargetingMesh = GameMasterScript.TDInstantiate("TargetingMesh");

        SetVirtualCursorPosition_Internal(GameMasterScript.heroPCActor.GetPos());
        Vector2 baseTile = GameMasterScript.heroPCActor.GetPos();
        abilityOrigin = baseTile;
        abilityUser = GameMasterScript.heroPCActor;

        if (abil.range > 0)
        {
            TargetingMeshScript workTMS = groundTargetingMesh.GetComponent<TargetingMeshScript>();
            workTMS.usedTiles.Clear();
            if (tryTargetToNearbyMonsters)
            {
                Directions bestDir = lineDir;
                int mostValidTargets = 0;
                for (int i = 0; i < MapMasterScript.cardinalDirections.Length; i++)
                {
                    lineDir = MapMasterScript.cardinalDirections[i];
                    UpdateGroundTargetingTiles(workTMS, baseTile, abilityInTargeting, true);
                    int localValidTargets = 0;
                    foreach (Vector2 v2 in workTMS.goodTiles)
                    {
                        MapTileData tile = MapMasterScript.GetTile(v2);
                        localValidTargets += tile.GetAllTargetable().Count;
                    }
                    if (localValidTargets > mostValidTargets)
                    {
                        bestDir = lineDir;
                        mostValidTargets = localValidTargets;
                    }
                }
                lineDir = bestDir;
                UpdateGroundTargetingTiles(workTMS, baseTile, abilityInTargeting, true);
            }
            else
            {
                UpdateGroundTargetingTiles(workTMS, baseTile, abilityInTargeting, true);
            }            
        }

        TargetingMeshScript tms = groundTargetingMesh.GetComponent<TargetingMeshScript>();
        tms.goodColor = tms.myGreen;

        int minX = (int)baseTile.x - abilityInTargeting.range;
        if (minX < 0)
        {
            minX = 0;
        }
        int minY = (int)baseTile.y - abilityInTargeting.range;
        if (minY < 0)
        {
            minY = 0;
        }
        tms.SetBounds(minX, minY, (int)baseTile.x + abilityInTargeting.range, (int)baseTile.y + abilityInTargeting.range);

        bool foundSomething = false;

        bool isRangedWeaponAttack = abilityUser.GetActorType() == ActorTypes.HERO && abil == GameMasterScript.rangedWeaponAbilityDummy;

        // Auto targeting code.
        var shouldAutoTarget = abilityInTargeting.CheckAbilityTag(AbilityTags.CURSORTARGET) &&
                               abilityInTargeting.CheckAbilityTag(AbilityTags.TARGETED);

        //if we are on the second or greater click of a multi-click ability, don't try and autotarget something.
        //Leave the cursor where it was. 
        //...just make sure this *is* in fact a multi target ability :D (>1 multitargets)
        if (!isRangedWeaponAttack &&
            TDInputHandler.targetClicksRemaining != abilityInTargeting.numMultiTargets && abilityInTargeting.numMultiTargets > 1)
        {
            shouldAutoTarget = false;
            
            //set this to true to ensure the cursor stays just where we left it.
            foundSomething = true;
        }
        
        if (shouldAutoTarget)
        {
            //Grab every target in range
            List<Actor> allActorsRenamedJustInCase = MapMasterScript.GetAllTargetableInV2Tiles(tms.goodTiles);

            //this is the last actor the hero attacked. If it already died, don't pick it again.
            Fighter lastAttackedByHero = GameMasterScript.heroPCActor.lastActorAttacked;
            if (lastAttackedByHero != null && 
                !lastAttackedByHero.myStats.IsAlive())
            {
                lastAttackedByHero = null;
            }
            
            //if the player is pressing in a given direction, look that way first.
            if (prevDir != Directions.NEUTRAL)
            {
                //this is the vector the hero is pointing at
                var lookDirection = MapMasterScript.xDirections[(int) prevDir];

                float bestDot = 0f;
                float closestTargetDist = 10000f;
                Actor bestTarget = null;
                
                //if that direction has an enemy in it, pick that one.
                foreach (var mon in allActorsRenamedJustInCase)
                {
                    var deltaToMonster = mon.GetPos() - GameMasterScript.heroPCActor.GetPos();
                    var distToMonster = deltaToMonster.sqrMagnitude;
                    var dotVal = Vector2.Dot(lookDirection, deltaToMonster.normalized);

                    //if this target is closest to where we're pointing, we could pick them.
                    if (dotVal > bestDot && distToMonster < closestTargetDist)
                    {
                        bestDot = dotVal;
                        bestTarget = mon;
                        closestTargetDist = distToMonster;
                    }
                    
                    //but wait -- if this target is the lastAttackedByHero target, 
                    //and we're pointing right at them, take them even if they aren't the
                    //closest target.
                    if (mon == lastAttackedByHero && dotVal >= 1.0f )
                    {
                        bestTarget = mon;
                        break;
                    }
                }
                
                //if we found someone, cool.
                if (bestTarget != null)
                {
                    SetVirtualCursorPosition(bestTarget.GetPos());
                    foundSomething = true;
                }
            }
            
            //If we don't have a target yet, look for what we hit last time, if it is still alive.
            if (!foundSomething)
            {
	            if (allActorsRenamedJustInCase.Contains(lastAttackedByHero) && lastAttackedByHero.myStats.IsAlive())
                {
                    SetVirtualCursorPosition(lastAttackedByHero.GetPos());
                    lastCursorPosition = lastAttackedByHero.GetPos();
                    foundSomething = true;
                }
                else
                {
                    if (GameMasterScript.heroPCActor.lastActorAttackedBy != null
                        && GameMasterScript.heroPCActor.lastActorAttackedBy.myStats.IsAlive()
                    && GameMasterScript.heroPCActor.lastActorAttackedBy.dungeonFloor == GameMasterScript.heroPCActor.dungeonFloor)
                    {
                        SetVirtualCursorPosition(GameMasterScript.heroPCActor.lastActorAttackedBy.GetPos());
                        lastCursorPosition = GameMasterScript.heroPCActor.lastActorAttackedBy.GetPos();
                        foundSomething = true;
                    }
                    else
                    {
                        //look for the closest thing to hit
                        float bestDist = 1000.0f;
                        Actor bestTarget = null;
                        
                        for (int i = 0; i < GameMasterScript.heroPCActor.GetNumCombatTargets(); i++)
                        {
                            Fighter ft = GameMasterScript.heroPCActor.combatTargets[i].combatant;
                            if (ft == null) continue;
                            
                            var dist = (ft.GetPos() - GameMasterScript.heroPCActor.GetPos()).sqrMagnitude;
                            if (allActorsRenamedJustInCase.Contains(ft) && dist < bestDist)
                            {
                                if (ft.actorfaction == Faction.DUNGEON) continue;
                                bestDist = dist;
                                bestTarget = ft;
                            }
                        }

                        if (bestTarget != null)
                        {
                            SetVirtualCursorPosition(bestTarget.GetPos());
                            foundSomething = true;
                        }
                    }

                }
            }

            //still no?
            if (!foundSomething) 
            {
                Actor dtTarget = null;
                bool foundTarget = false;

                //look for the closest thing to hit again! yay
                float bestDist = 1000.0f;
                Actor bestTarget = null;
                foreach (Vector2 tile in tms.goodTiles)
                {
                    Actor act = MapMasterScript.GetTargetableAtLocation(tile);
                    
                    if (act != null)
                    {
                        var dist = (act.GetPos() - GameMasterScript.heroPCActor.GetPos()).sqrMagnitude;
                        if (dist > bestDist) continue;
                        
                        //This will continue to hone in on the closest monster.
                        if (act.GetActorType() == ActorTypes.MONSTER && abilityInTargeting.CheckAbilityTag(AbilityTags.MONSTERAFFECTED) && act.actorfaction != abilityUser.actorfaction && act.actorfaction != Faction.DUNGEON)
                        {
                            bestDist = dist;
                            bestTarget = act;
                            continue;
                        }
                        if (act.GetActorType() == ActorTypes.HERO && abilityInTargeting.CheckAbilityTag(AbilityTags.HEROAFFECTED))
                        {
                            SetVirtualCursorPosition(tile);
                            lastCursorPosition = tile;
                            foundTarget = true;
                            foundSomething = true;
                            break;
                        }
                        if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
                        {
                            Destructible dt = act as Destructible;
                            // Don't auto target destructibles with your ranged weapon
                            if (isRangedWeaponAttack)
                            {
                                continue;
                            }
                            if (dt.mapObjType != SpecialMapObject.MONSTERSPAWNER)
                            {
                                dtTarget = act;
                            }
                            
                        }
                    }
                }

                //if we are looking for monsters and found a best-and-closest one, hit them.
                if (bestTarget != null)
                {
                    SetVirtualCursorPosition(bestTarget.GetPos());
                    foundTarget = true;
                    foundSomething = true;
                }
                
                if (!foundTarget && dtTarget != null && dtTarget.actorRefName != "obj_monsterspawner")
                {
                    SetVirtualCursorPosition(dtTarget.GetPos());
                    lastCursorPosition = dtTarget.GetPos();
                    foundTarget = true;
                    foundSomething = true;
                }
            }

        }

        if (!foundSomething && shouldAutoTarget)
        {
            if (tms != null && tms.goodTiles != null && tms.goodTiles.Count > 0)
            {
                MapTileData mtd = MapMasterScript.GetTile(tms.goodTiles[UnityEngine.Random.Range(0, tms.goodTiles.Count)]);
                
                // Try start near player tile?
                CustomAlgorithms.GetTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 1, MapMasterScript.activeMap);
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    MapTileData checkmtd = CustomAlgorithms.tileBuffer[i];
                    if (tms.goodTiles.Contains(checkmtd.pos))
                    {
                        mtd = checkmtd;
                        break;
                    }
                }
                
                bool setPos = true;
                Destructible checkDT = mtd.GetTargetable() as Destructible;
                if (checkDT != null && (checkDT.mapObjType == SpecialMapObject.MONSTERSPAWNER || isRangedWeaponAttack))
                {
                    setPos = false;
                } 
                if (setPos) SetVirtualCursorPosition_Internal(mtd.pos);
            }
        }
        
        if (shouldAutoTarget)
        {
            PlayerInputTargetingManager.TurnOn(GetVirtualCursorPosition());
            //face the new direction, if we have one.
            var hero = GameMasterScript.heroPCActor;
            var heroPos = hero.GetPos();
            if (heroPos != virtualCursorPosition)
            {
                hero.UpdateLastMovedDirection(MapMasterScript.GetDirectionFromAngle(
                    CombatManagerScript.GetAngleBetweenPoints(heroPos, virtualCursorPosition)));
                hero.myStats.UpdateStatusDirections();
            }
        }
        
       
        //groundTargetingMesh.transform.localPosition = Vector2.zero;
            
        if (abil.CheckAbilityTag(AbilityTags.CURSORTARGET))
        {
            //cursorTargetingMesh = Instantiate(GameMasterScript.GetResourceByRef("CursorTargetingMesh"));
            cursorTargetingMesh = GameMasterScript.TDInstantiate("CursorTargetingMesh");
            TargetingMeshScript ctms = cursorTargetingMesh.GetComponent<TargetingMeshScript>();
            ctms.usedTiles.Clear();
            ctms.goodColor = tms.myGreen;
            // Find the base tile.
            UpdateCursorTargetingTiles();
        }                
    }

    private TargetShapes GetFlexShape(TargetShapes orig)
    {
        Directions dirToUse = lineDir;
        if (!abilityInTargeting.CheckAbilityTag(AbilityTags.CANROTATE))
        {
            // Not player rotatable, so use the built in direction.
            dirToUse = abilityInTargeting.lineDir;
        }

        TargetShapes myShape = orig;
        if (myShape == TargetShapes.FLEXLINE)
        {
            switch (dirToUse)
            {
                case Directions.EAST:
                case Directions.WEST:
                    myShape = TargetShapes.HLINE;
                    break;
                case Directions.NORTH:
                case Directions.SOUTH:
                    myShape = TargetShapes.VLINE;
                    break;
                case Directions.NORTHEAST:
                case Directions.SOUTHWEST:
                    myShape = TargetShapes.DLINE_NE;
                    break;
                case Directions.SOUTHEAST:
                case Directions.NORTHWEST:
                    myShape = TargetShapes.DLINE_SE;
                    break;
            }
        }
        if (myShape == TargetShapes.FLEXCROSS)
        {
            switch (dirToUse)
            {
                case Directions.NORTH:
                case Directions.EAST:
                case Directions.SOUTH:
                case Directions.WEST:
                    myShape = TargetShapes.CROSS;
                    break;
                case Directions.NORTHEAST:
                case Directions.NORTHWEST:
                case Directions.SOUTHEAST:
                case Directions.SOUTHWEST:
                    myShape = TargetShapes.XCROSS;
                    break;
            }
        }
        return myShape;
    }

    private void UpdateCursorTargetingTiles()
    {
        Vector2 baseTile = virtualCursorPosition;
        TargetingMeshScript ctms = cursorTargetingMesh.GetComponent<TargetingMeshScript>();
        TargetingMeshScript gtms = groundTargetingMesh.GetComponent<TargetingMeshScript>();

        if (ctms.goodTiles == null)
        {
            ctms.goodTiles = new List<Vector2>();
        }
        if (ctms.badTiles == null)
        {
            ctms.badTiles = new List<Vector2>();
        }
        ctms.goodTiles.Clear();
        ctms.badTiles.Clear();

        // Get a list of all possible tiles in the shape.

        if (abilityInTargeting == null)
        {
            Debug.Log("No ability is being targeted.");
            return;
        }

        TargetShapes myShape = GetFlexShape(abilityInTargeting.targetShape);

        List<Vector2> possibleTiles = CreateShapeTileList(myShape, abilityInTargeting, baseTile, lineDir, abilityInTargeting.targetRange, true);

        foreach (Vector2 vec in possibleTiles)
        {
            if (gtms.goodTiles.Contains(vec) && !ctms.goodTiles.Contains(vec))
            {
                ctms.goodTiles.Add(vec);
            }
            if (gtms.badTiles.Contains(vec) && !ctms.badTiles.Contains(vec))
            {
                ctms.badTiles.Add(vec);
            }
        }

        var isTargetingValidTile = gtms.goodTiles.Contains(baseTile);
        PlayerInputTargetingManager.UpdateCurrentTargetingInformation(baseTile, isTargetingValidTile);
        ctms.BuildMesh();
    }

    public static Directions SnapLine(Directions dir, bool clockwise)
    {
        Directions returnDir = dir;
        if (dir == Directions.NORTHEAST)
        {
            if (clockwise)
            {
                returnDir = Directions.EAST;
            }
            else
            {
                returnDir = Directions.NORTH;
            }
        }
        if (dir == Directions.SOUTHEAST)
        {
            if (clockwise)
            {
                returnDir = Directions.SOUTH;
            }
            else
            {
                returnDir = Directions.EAST;
            }
        }
        if (dir == Directions.SOUTHWEST)
        {
            if (clockwise)
            {
                returnDir = Directions.WEST;
            }
            else
            {
                returnDir = Directions.SOUTH;
            }
        }
        if (dir == Directions.NORTHWEST)
        {
            if (clockwise)
            {
                returnDir = Directions.NORTH;
            }
            else
            {
                returnDir = Directions.WEST;
            }
        }
        return returnDir;
    }

    private void SnapNEWS(bool clockwise)
    {
        bool cursorTargetToSnap = false;
        bool rotatableGroundTarget = false;

        if (abilityInTargeting.CheckAbilityTag(AbilityTags.CURSORTARGET))
        {
            if (snappableTargetShapes.Contains(abilityInTargeting.targetShape))
            {
                cursorTargetToSnap = true;
            }
        }
        else
        {
            if (rotatableTargetShapes.Contains(abilityInTargeting.boundsShape))
            {
                rotatableGroundTarget = true;
            }
        }

        if (cursorTargetToSnap || rotatableGroundTarget || rotatableTargetShapes.Contains(abilityInTargeting.targetShape))
        {
            if (lineDir == Directions.NORTHEAST)
            {
                if (clockwise)
                {
                    lineDir = Directions.EAST;
                }
                else
                {
                    lineDir = Directions.NORTH;
                }
            }
            if (lineDir == Directions.SOUTHEAST)
            {
                if (clockwise)
                {
                    lineDir = Directions.SOUTH;
                }
                else
                {
                    lineDir = Directions.EAST;
                }
            }
            if (lineDir == Directions.SOUTHWEST)
            {
                if (clockwise)
                {
                    lineDir = Directions.WEST;
                }
                else
                {
                    lineDir = Directions.SOUTH;
                }
            }
            if (lineDir == Directions.NORTHWEST)
            {
                if (clockwise)
                {
                    lineDir = Directions.NORTH;
                }
                else
                {
                    lineDir = Directions.WEST;
                }
            }
        }
    }

    private Vector2 TrySnapOffset(Directions dir, AbilityScript abil)
    {
        Vector2 ret = Vector2.zero;
        if (dir == Directions.NORTH)
        {
            ret.x = abil.targetOffsetX;
            ret.y = abil.targetOffsetY;
        }
        if (dir == Directions.EAST)
        {
            ret.x = abil.targetOffsetY;
            ret.y = abil.targetOffsetX * -1;
        }
        if (dir == Directions.SOUTH)
        {
            ret.x = abil.targetOffsetX * -1;
            ret.y = abil.targetOffsetY * -1;
        }
        if (dir == Directions.WEST)
        {
            ret.x = abil.targetOffsetY * -1;
            ret.y = abil.targetOffsetX;
        }
        return ret;
    }

    private void TrySnapPoint()
    {
        if (abilityInTargeting.boundsShape == TargetShapes.POINT)
        {
            // Say x is -1, y is 1. One square above, and to the left
            if (lineDir == Directions.NORTH)
            {
                localTargetOffsetX = abilityInTargeting.targetOffsetX;
                localTargetOffsetY = abilityInTargeting.targetOffsetY;
            }
            if (lineDir == Directions.NORTHEAST)
            {
                localTargetOffsetX = abilityInTargeting.targetOffsetY;
                localTargetOffsetY = abilityInTargeting.targetOffsetY;
            }
            if (lineDir == Directions.EAST)
            {
                localTargetOffsetX = abilityInTargeting.targetOffsetY;
                localTargetOffsetY = abilityInTargeting.targetOffsetX * -1;
            }
            if (lineDir == Directions.SOUTHEAST)
            {
                localTargetOffsetX = abilityInTargeting.targetOffsetY;
                localTargetOffsetY = abilityInTargeting.targetOffsetY * -1;
            }
            if (lineDir == Directions.SOUTH)
            {
                localTargetOffsetX = abilityInTargeting.targetOffsetX * -1;
                localTargetOffsetY = abilityInTargeting.targetOffsetY * -1;
            }
            if (lineDir == Directions.SOUTHWEST)
            {
                localTargetOffsetX = abilityInTargeting.targetOffsetY * -1;
                localTargetOffsetY = abilityInTargeting.targetOffsetY * -1;
            }
            if (lineDir == Directions.WEST)
            {
                localTargetOffsetX = abilityInTargeting.targetOffsetY * -1;
                localTargetOffsetY = abilityInTargeting.targetOffsetX;
            }
            if (lineDir == Directions.NORTHWEST)
            {
                localTargetOffsetX = abilityInTargeting.targetOffsetY * -1;
                localTargetOffsetY = abilityInTargeting.targetOffsetY;
            }
        }
    }

    public void TryRotateTargetingShape(Directions dir)
    {
        if (abilityInTargeting == null) return;

        Directions startDir = lineDir;

        if (!abilityInTargeting.CheckAbilityTag(AbilityTags.CANROTATE))
        {
            return;
        }

        //if (dir == lineDir) return;


        lineDir = dir;

        SnapNEWS(true);

        TrySnapPoint();

        if (abilityInTargeting.CheckAbilityTag(AbilityTags.CURSORTARGET))
        {
            UpdateCursorTargetingTiles();
        }
        else
        {
            UpdateGroundTargetingTiles(groundTargetingMesh.GetComponent<TargetingMeshScript>(), abilityOrigin, abilityInTargeting, true);
        }
    }

    public void TryRotateTargetingShape(bool clockwise)
    {
        if (abilityInTargeting == null) return;

        if (!abilityInTargeting.CheckAbilityTag(AbilityTags.CANROTATE))
        {
            return;
        }
        uiHotbarCursor.GetComponent<AudioStuff>().PlayCue("Tick");

        if (abilityInTargeting.targetShape == TargetShapes.FLEXLINE
            || abilityInTargeting.targetShape == TargetShapes.POINT
            || abilityInTargeting.targetShape == TargetShapes.FLEXCROSS
            || abilityInTargeting.targetShape == TargetShapes.FLEXCONE

            || abilityInTargeting.boundsShape == TargetShapes.FLEXLINE
            || abilityInTargeting.boundsShape == TargetShapes.FLEXCROSS
            || abilityInTargeting.boundsShape == TargetShapes.FLEXCONE
            || abilityInTargeting.boundsShape == TargetShapes.FLEXRECT
            || abilityInTargeting.boundsShape == TargetShapes.SEMICIRCLE
            || abilityInTargeting.boundsShape == TargetShapes.CLAW)
        {
            int current = (int)lineDir;

            //bool clockwise;

            /* if (Input.GetAxisRaw("RotateTargeting") < 0)
            {
                current--;
                clockwise = false;
                Debug.Log("Counterclockwise");
            }
            else {
                current++;
                clockwise = true;
                Debug.Log("Clockwise");
            } */

            if (clockwise)
            {
                current++;
            }
            else
            {
                current--;
            }

            if (current >= 8)
            {
                current = 0;
            }
            if (current < 0)
            {
                current = 7;
            }
            lineDir = (Directions)current;

            SnapNEWS(clockwise);

            TrySnapPoint();

            if (abilityInTargeting.CheckAbilityTag(AbilityTags.CURSORTARGET))
            {
                UpdateCursorTargetingTiles();
            }
            else
            {
                UpdateGroundTargetingTiles(groundTargetingMesh.GetComponent<TargetingMeshScript>(), abilityOrigin, abilityInTargeting, true);
            }

        }
    }

    public List<Vector2> GetAllValidTargetTiles()
    {
        if (abilityInTargeting == null) return null;
        /* if ((cursorTargetingMesh == null) || (groundTargetingMesh == null))
        {
            
        } */
        if (abilityInTargeting.landingTile != LandingTileTypes.NONE)
        {
            if (groundTargetingMesh == null) return null;
            Vector2 reqTile = groundTargetingMesh.GetComponent<TargetingMeshScript>().reqOpenTiles[0];
            if ((reqTile.x <= 0) || (reqTile.x >= MapMasterScript.activeMap.columns - 1) || (reqTile.y <= 0) || (reqTile.y >= MapMasterScript.activeMap.rows - 1))
            {
                return null;
            }
            MapTileData reqTileMTD = MapMasterScript.GetTile(reqTile);
            if (reqTileMTD.IsCollidable(abilityUser))
            {
                return null;
            }

        }
        if (abilityInTargeting.CheckAbilityTag(AbilityTags.CURSORTARGET))
        {
            if (cursorTargetingMesh == null)
            {
                return null;
            }
            return cursorTargetingMesh.GetComponent<TargetingMeshScript>().goodTiles;
        }
        else
        {
            if (groundTargetingMesh == null)
            {
                Debug.Log("Expected GTM?");
                return null;
            }
            return groundTargetingMesh.GetComponent<TargetingMeshScript>().goodTiles;
        }
    }

    public bool CheckValidTarget(Vector2 pos)
    {
        TargetingMeshScript tms = groundTargetingMesh.GetComponent<TargetingMeshScript>();
        for (int i = 0; i < tms.goodTiles.Count; i++)
        {
            if ((pos.x == tms.goodTiles[i].x) && (pos.y == tms.goodTiles[i].y))
            {
                return true;
            }
        }
        return false;
    }

    public void ExitTargeting()
    {
        //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.NORMAL);

        abilityTargeting = false;
        if (groundTargetingMesh != null)
        {
            GameMasterScript.ReturnToStack(groundTargetingMesh, "TargetingMesh");
            //Destroy(groundTargetingMesh);
            groundTargetingMesh = null;
        }
        if (cursorTargetingMesh != null)
        {
            GameMasterScript.ReturnToStack(cursorTargetingMesh, "CursorTargetingMesh");
            //Destroy(cursorTargetingMesh);
            cursorTargetingMesh = null;
        }
        HideGenericInfoBar();
        TDInputHandler.ClearMousePathfinding();
		        PlayerInputTargetingManager.TurnOff();
        lastHoverPosition = new Vector2(-1, -1);
    }

    public void UpdateAbilityToTry(AbilityScript newAbil)
    {
        abilityInTargeting = newAbil;
        //Debug.Log("Abil in targeting is now " + newAbil.myID + " " + newAbil.CheckAbilityTag(AbilityTags.MONSTERAFFECTED));
        List<Vector2> bufferUsedGroundTiles = new List<Vector2>();
        List<Vector2> bufferUsedCursorTiles = new List<Vector2>();

        if (groundTargetingMesh != null && !newAbil.CheckAbilityTag(AbilityTags.TARGETUSEDTILES))
        {
            //Debug.Log("Add used tiles");
            for (int i = 0; i < groundTargetingMesh.GetComponent<TargetingMeshScript>().usedTiles.Count; i++)
            {
                Vector2 tile = groundTargetingMesh.GetComponent<TargetingMeshScript>().usedTiles[i];
                bufferUsedGroundTiles.Add(tile);
            }
        }
        if ((cursorTargetingMesh != null) && (!newAbil.CheckAbilityTag(AbilityTags.TARGETUSEDTILES)))
        {
            //Debug.Log("Add used tiles");
            for (int i = 0; i < cursorTargetingMesh.GetComponent<TargetingMeshScript>().usedTiles.Count; i++)
            {
                Vector2 tile = groundTargetingMesh.GetComponent<TargetingMeshScript>().usedTiles[i];
                bufferUsedGroundTiles.Add(tile);
            }
        }

        ExitTargeting();
        EnterTargeting(newAbil, lineDir);
        //lineDir = bufferedLineDir;

        if (groundTargetingMesh != null)
        {
            groundTargetingMesh.GetComponent<TargetingMeshScript>().usedTiles = bufferUsedGroundTiles;
            groundTargetingMesh.GetComponent<TargetingMeshScript>().BuildMesh();
        }
        if (cursorTargetingMesh != null)
        {
            cursorTargetingMesh.GetComponent<TargetingMeshScript>().usedTiles = bufferUsedCursorTiles;
            cursorTargetingMesh.GetComponent<TargetingMeshScript>().BuildMesh();
        }
    }

    public Directions GetLineDir()
    {
        return lineDir;
    }

    public static void EnterMonsterTargeting()
    {

    }

    IEnumerator WaitThenExitTargeting(float time)
    {
        yield return new WaitForSeconds(time);
        ExitTargeting();
    }
}
