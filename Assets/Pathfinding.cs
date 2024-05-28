using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class Pathfinding
{
    public static PFNode TileToNode(MapTileData mtd, Actor act)
    {
        PFNode pf = new PFNode();
        pf.x = (int)mtd.pos.x;
        pf.y = (int)mtd.pos.y;
        if (mtd.tileType == TileTypes.WALL) pf.wall = true;
        else if (mtd.IsUnbreakableCollidable(act))
        {
            pf.wall = true;
        }
        pf.lava = mtd.CheckTag(LocationTags.LAVA);
        pf.water = mtd.CheckTag(LocationTags.WATER);
        pf.mud = mtd.CheckTag(LocationTags.MUD);
        pf.elec = mtd.CheckTag(LocationTags.ELECTRIC);

        return pf;
    }

    public static PFNode GetNeighbor(Directions dir)
    {
        switch(dir)
        {
            case Directions.NORTH:

                break;
        }
        return null;
    }
}

public class PFNode
{
    public int x;
    public int y;
    public int g;
    public int f;
    public int h;
    public bool wall;
    public bool open;
    public bool closed;
    public bool lava;
    public bool mud;
    public bool elec;
    public bool water;
    public PFNode parent;
    public PFNode child;
}

