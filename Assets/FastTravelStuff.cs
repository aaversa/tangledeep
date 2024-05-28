using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastTravelStuff
{

    public static bool initialized;

    static List<int> waypointFloorIDsSorted;

    public static List<int> GetWaypointIDList()
    {
        if (!initialized)
        {
            Initialize();
        }
        return waypointFloorIDsSorted;
    }

    static void Initialize()
    {
        if (initialized) return;

        waypointFloorIDsSorted = new List<int>()
        {
            0, // town
            MapMasterScript.BRANCH_PASSAGE_POSTBOSS1,
            MapMasterScript.BRANCH_PASSAGE_POSTBOSS2,
            101, // Katie area
            104, // storeroom
            102, // frog bog
            105, // off the beaten path
            106, // hilde area
            107, // bottles brews
            110, // casino
            118, // pet shoppe
            131, // magic merchant
            207, // beastlake park
            224, // frozen alcove
            225, // desert area
            16, // stone halls
            212, // final hub            
            356, // realm of gods 1
            360, // realm of gods 5            
            380, // frog dungeon 1
            375, // bandit dungeon 1
            385, // beast dungeon 1
            390, // spirit dungeon 1
            394, // jelly dungeon 1
            370, // robot dungeon 1

        };

        initialized = true;
    }

}
