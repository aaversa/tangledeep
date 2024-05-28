using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class DestroyTileEffect : EffectScript
{
    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        DestroyTileEffect temp = template as DestroyTileEffect;
        tActorType = temp.tActorType;
    }

    public override float DoEffect(int indexOfEffect = 0)
    {
        affectedActors.Clear();
        results.Clear();
        Fighter origFighter = (Fighter)originatingActor as Fighter;
        StatBlock origStats = origFighter.myStats;
        EquipmentBlock origEquipment = origFighter.myEquipment;

        if (UnityEngine.Random.Range(0, 1.0f) > procChance)
        {
            return 0.0f;
        }

        List<MapTileData> tilesToProcess = new List<MapTileData>();
        MapTileData chkMtd;
        foreach (Vector2 pos in positions)
        {
            chkMtd = MapMasterScript.GetTile(pos);
            if (chkMtd.tileType == TileTypes.WALL)
            {
                if (!MapMasterScript.activeMap.IsMapEdge(chkMtd))
                {
                    tilesToProcess.Add(chkMtd);
                }
                else
                {
                    GameLogScript.LogWriteStringRef("log_error_wallbreaker_mapedge");
                }
            }
        }

        if (tilesToProcess.Count == 0)
        {
            return 0.0f;
        }

        bool perTargetAnim = parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM);

        if (playAnimation && !parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM))
        {
            perTargetAnim = false;
            // Just play ONE animation for the entire thing.
            CombatManagerScript.GenerateEffectAnimation(originatingActor.GetPos(), centerPosition, this, originatingActor.GetObject());
        }
        else if (!playAnimation)
        {
            perTargetAnim = false;
        }

        List<MapTileData> adjTiles = new List<MapTileData>();

        foreach (MapTileData mtd in tilesToProcess)
        {
            if (mtd.CheckTag(LocationTags.SOLIDTERRAIN) || mtd.CheckTag(LocationTags.TREE) || mtd.wallReplacementIndex >= 0)
            {
                Debug.Log("Tile at " + mtd.pos + " has solid terrain on it");
                GameObject objToDestroy = null;
                foreach (GameObject go in MapMasterScript.singletonMMS.activeNonTileGameObjects)
                {
                    if (CustomAlgorithms.CompareFloats(go.transform.position.x, mtd.pos.x) && CustomAlgorithms.CompareFloats(go.transform.position.y, mtd.pos.y))
                    {
                        objToDestroy = go;
                        Debug.Log("destroy solid terrain " + objToDestroy.name + " at " + objToDestroy.transform.position);
                        break;
                    }
                }
                MapMasterScript.singletonMMS.activeNonTileGameObjects.Remove(objToDestroy);
                GameMasterScript.Destroy(objToDestroy);
            }
            mtd.ChangeTileType(TileTypes.GROUND, MapMasterScript.activeMap.mgd);
            mtd.SetTileVisualType(VisualTileType.GROUND);
            adjTiles = MapMasterScript.activeMap.GetListOfTilesAroundPoint(mtd.pos, 1);
            mtd.UpdateCollidableState();
            mtd.UpdateVisionBlockingState();

            // don't change visuals of surrounding tiles.
            /* for (int x = 0; x < adjTiles.Count; x++)
            {
                MapMasterScript.activeMap.BeautifyTile(adjTiles[x]);
            } */
        }

        MapMasterScript.RebuildMapMesh();

        float returnVal = 0.0f;
        if (!parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
        {
            returnVal = animLength * tilesToProcess.Count;
        }
        else
        {
            returnVal = animLength;
        }

        if (playAnimation == false)
        {
            returnVal = 0.0f;
        }

        if (MinimapUIScript.GetOverlay())
        {
            MinimapUIScript.StopOverlay();
            MinimapUIScript.GenerateOverlay();
        }

        return returnVal;
    }
}