using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class RandomJobMode
{
    public static List<int> removedBranchFloors;

	public static void RemoveBranchesAtRandom()
    {
        CreateRemovedBranchFloorList();

        RemoveAllStairsPointingToRemovedAreasThroughoutTheDungeon();

        RerouteSideAreasInBranchFloorList();
    }

    static void RemoveAllStairsPointingToRemovedAreasThroughoutTheDungeon()
    {
        foreach(Map m in MapMasterScript.theDungeon.maps)
        {
            RemoveStairsInMapThatPointToBadConnection(m);
        }
    }

    public static void RemoveStairsInMapThatPointToBadConnection(Map mapWithStairsToRemove)
    {
        if (removedBranchFloors == null) removedBranchFloors = new List<int>();

        foreach (Stairs st in mapWithStairsToRemove.mapStairs)
        {
            bool pointsToBadMap = false;
            if (st.NewLocation == null)
            {
                if (st.pointsToFloor == -1) continue; // stairs are obviously no good
                if (removedBranchFloors.Contains(st.pointsToFloor)) pointsToBadMap = true;
            }
            else if (removedBranchFloors.Contains(st.NewLocation.floor))
            {
                pointsToBadMap = true;
            }
            if (!pointsToBadMap) continue;
            // Well, we have to nuke this connection.
            st.NewLocation = null;
            st.DisableActor();
            Debug.Log("Disabling stairs on map " + mapWithStairsToRemove.floor + " " + mapWithStairsToRemove.GetName() + " because they point(ed) to a bad map.");
        }
    }

    static void RerouteSideAreasInBranchFloorList()
    {
        List<Map> sideAreaMapsWeNeedToReconnectToOtherMainAreas = new List<Map>();

        foreach(int floorID in removedBranchFloors)
        {
            Map m = MapMasterScript.theDungeon.FindFloor(floorID);
            foreach(Stairs st in m.mapStairs)
            {
                if (st.NewLocation == null) continue;
                if (st.NewLocation.dungeonLevelData.sideArea)
                {
                    Debug.Log("Side area " + st.NewLocation.floor + " " + st.NewLocation.GetName() + " was linked to a removed floor " + floorID + " so we must find a new connection.");
                    sideAreaMapsWeNeedToReconnectToOtherMainAreas.Add(st.NewLocation);
                }
            }
        }

        //Debug.Log("Total maps to reconnect: " + sideAreaMapsWeNeedToReconnectToOtherMainAreas.Count);

        // Each map "m" is a map that was previously pointing to an area that has been removed
        // Therefore, we need any stairs in "m" that are connected to *nothing*, to point to the new area (connectionMap)
        foreach (Map m in sideAreaMapsWeNeedToReconnectToOtherMainAreas)
        {
            Map connectionMap = null;
            bool success = MapMasterScript.FindConnectionMapForSideArea(m, false, out connectionMap);
            if (!success)
            {
                if (Debug.isDebugBuild) Debug.Log("Failed to find connection map for " + m.floor + " " + m.GetName());
                continue;
            }

            m.effectiveFloor = connectionMap.effectiveFloor;

            bool connectedStairs = false;
            foreach(Stairs st in m.mapStairs)
            {
                if (st.pointsToFloor != -1 || st.NewLocation != null) continue; // we must use blank stairs.
                st.NewLocation = connectionMap;
                connectedStairs = true;
                st.EnableActor();
                break;
            }

            if (!connectedStairs)
            {
                m.SpawnStairs(true, connectionMap.floor);
            }

            // Now reciprocate the stair connection.
            connectionMap.ConnectToMap(m);

            
            Debug.Log("Map " + m.floor + " " + m.GetName() + " now connects to " + connectionMap.floor + " " + connectionMap.GetName() + ", and vice versa.");
        }        
    }

    public static bool IsFloorEnabled(int floor)
    {
        if (removedBranchFloors == null) return true;

        return !removedBranchFloors.Contains(floor);
    }

    static void CreateRemovedBranchFloorList()
    {
        if (removedBranchFloors == null) removedBranchFloors = new List<int>();

        removedBranchFloors.Clear();

        int roll = UnityEngine.Random.Range(0, 2);

        if (roll == 0) // remove fungal caverns.
        {
            //Debug.Log("Removing fungal caverns");
            for (int i = 135; i <= 138; i++)
            {
                removedBranchFloors.Add(i);
            }
        }
        else // Remove old amber station
        {
            //Debug.Log("Removing old amber station");
            for (int i = 6; i <= 9; i++)
            {
                removedBranchFloors.Add(i);
            }
        }

        roll = UnityEngine.Random.Range(0, 2);

        if (roll == 0) // remove stonehewn
        {
            //Debug.Log("Removing stonehewn halls");
            for (int i = 11; i <= 14; i++)
            {
                removedBranchFloors.Add(i);
            }
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("Removing ancient ruins");
            for (int i = 151; i <= 154; i++)
            {
                removedBranchFloors.Add(i);
            }
        }
    }

}

public partial class Map
{
    /// <summary>
    /// This assumes targetMap already points to us.
    /// </summary>
    /// <param name="targetMap"></param>
    public void ConnectToMap(Map targetMap)
    {
        // Check our stairs to see if we have any blank usable ones.
        bool connectedStairs = false;
        foreach (Stairs st in mapStairs)
        {
            if (st.NewLocation != null || st.pointsToFloor != -1) continue;
            st.NewLocation = targetMap;
            connectedStairs = true;
            st.EnableActor();
            break;
        }

        if (connectedStairs) return;

        SpawnStairs(true, targetMap.floor);
    }
}
