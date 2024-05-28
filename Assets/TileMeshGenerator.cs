using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Unity.Collections;
using Debug = UnityEngine.Debug;

//[RequireComponent(typeof(MeshFilter))]
//[RequireComponent(typeof(MeshRenderer))]
//[RequireComponent(typeof(MeshCollider))]
public class TileMeshGenerator : MonoBehaviour {

	private MeshFilter mf;
    private MeshRenderer mr;
	private MeshCollider mc;

    public Texture2D terrainTiles;

    public Texture2D decorTiles;
    public Texture2D grassTiles;
    public Texture2D grassDecorTiles;
    public int tileResolution = 32;

    public bool fogOfWar;
    public bool isMiniMap;

	public int size_x;
	public int size_z;

    private float tileSize = 1.0f;

    public bool isGridMap;
    public float opacity;

    Texture2D latestTexture;
    private Texture2D texTranslucentMiniMap;

    int[,] indexOfBaseTileContentsForMinimap;
    int[,] indexOfFullTileContentsForMinimap;

    int[,] indexOfBaseTileContentsForTranslucentMinimap;
    int[,] indexOfFullTileContentsForTranslucentMinimap;

    private HashSet<int> tilesAlreadyRevealed;
    private HashSet<int> tilesAlreadyRevealedTranslucent;
    
    /// <summary>
    /// used for the translucent minimap to store pixels between moving from source to dest,
    /// because we have to futz with them along the way.
    /// </summary>
    private Color32[] blockBucket;
    
    void Awake ()
    {
        
    }

    void Start () {
        if (!isMiniMap)
        {
            mf = gameObject.GetComponent<MeshFilter>();
            mr = gameObject.GetComponent<MeshRenderer>();
            mc = gameObject.GetComponent<MeshCollider>();
        }

        if (fogOfWar)
        {
            mr.sortingLayerName = "FogOfWar";
            mr.sharedMaterials[0].renderQueue = 3500;        
            
            //This value ensures that the renderQueue value is used to determine rendering without being
            //overruled by z-positioning.
            mr.sharedMaterials[0].SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);        
            
        }
        else if (isGridMap)
        {
            mr.sortingLayerName = "GridMap";
        }
        else if (isMiniMap)
        {
            /* mr.sortingLayerName = "Minimap";
            mr.sortingOrder = 999;
            mr.sharedMaterials[0].renderQueue = 4000; */            
        }
        else
        {
            mr.sortingLayerName = "Map";
        }

        if (!isMiniMap)
        {
            mr.sortingOrder = 0;
        }        
	}

    public void SetTileResolution(int newRes)
    {
        tileResolution = newRes;
    }

    /// <summary>
    /// Grab all the pixels from a given tileResolution block on the source and put them in a pre-existing array.
    /// </summary>
    /// <param name="source">An uncompressed terrain texture</param>
    /// <param name="sourceIdx">0 based index starting at the TOP LEFT of the source</param>
    /// <param name="destBlock">An array of size tileResolution^2</param>
    void GetBlockFromTerrainSourceTexture(Texture2D source, int sourceIdx, Color32[] destBlock)
    {
        Vector2Int srcPx = GetPixelCoordinatesFromTileIdx(source, sourceIdx, true);
        var sourceArray = source.GetRawTextureData<Color32>();
        
        //Can't currently move blocks enmasse from native to managed, so roll one at a time.
        var destTileIdx = 0;
        for (int y = 0; y < tileResolution; y++)
        {
            for (int x = 0; x < tileResolution; x++)
            {
                int srcPixel = srcPx.x + x + (srcPx.y + y) * source.width;
                destBlock[destTileIdx] = sourceArray[srcPixel];
                destTileIdx++;
            }
        }
    }

    /// <summary>
    /// Place pixels from an array into raw texture data. This texture is usually what we display on the ground or
    /// minimap, and thus the idx starts at the BOTTOM LEFT.
    /// </summary>
    /// <param name="dest">An uncompressed texture</param>
    /// <param name="destIdx">0 based index starting at the BOTTOM LEFT of the source.</param>
    /// <param name="sourceBlock">An array of size tileResolution^2 full of pretty colors.</param>
    void SetPixelsFromBlockIntoTexture(Texture2D dest, int destIdx, Color32[] sourceBlock)
    {
        Vector2Int dstPx = GetPixelCoordinatesFromTileIdx(dest, destIdx, false);
        var dstArray = dest.GetRawTextureData<Color32>();
        
        //Can't currently move blocks enmasse from native to managed, so roll one at a time.
        var srcIdx = 0;
        for (int y = 0; y < tileResolution; y++)
        {
            for (int x = 0; x < tileResolution; x++)
            {
                int dstPixel = dstPx.x + x + (dstPx.y + y) * dest.width;
                dstArray[dstPixel] = sourceBlock[srcIdx];
                srcIdx++;
            }
        }
    }

    /// <summary>
    /// Do the math that converts 0 based tile index into a pair of pixel coordinates
    /// </summary>
    /// <param name="tex"></param>
    /// <param name="idx"></param>
    /// <param name="flipYValue">Set this to true if the idx is TOP LEFT based.</param>
    /// <returns></returns>
    Vector2Int GetPixelCoordinatesFromTileIdx(Texture2D tex, int idx, bool flipYValue)
    {
        //These values are the number of tiles in each texture, not the number of pixels.
        int tilesWide = tex.width / tileResolution;

        //we know now which tile to start at,
        int srcX = idx % tilesWide;
        int srcY = idx / tilesWide;
        
        //buuut wait!
        
        //Decor/sprite indicies for tiles are stored with 0 being the top left location in the file.
        //however, rawtexturedata starts at the bottom left and moves up. So perhaps our srcY needs to be 
        //flipped upside down. 
        if (flipYValue)
        {
            int srcRowsTall = tex.height / tileResolution;
            srcY = srcRowsTall - 1 - srcY;
        }
        
        //multiply by tileRes to get the pixel coordinates we begin at
        srcX *= tileResolution;
        srcY *= tileResolution;
        
        return new Vector2Int(srcX, srcY);
    }

    /// <summary>
    /// Copies a square of pixels from one tile to another. Remember when you're done doing all the copies
    /// you need to Apply() the texture.
    /// </summary>
    /// <param name="source">The texture we're copying from with tiles of Tangledeep art</param>
    /// <param name="dest">Destination texture, likely displayed on screen.</param>
    /// <param name="sourceIdx">0 based index starting at the TOP LEFT of the source</param>
    /// <param name="destIdx">0 based index starting at the BOTTOM LEFT of the source. What?</param>
    /// <param name="useAlpha">Respect the source alpha to layer things atop the dest gently.</param>
    void CopyBlockFromTextureToTexture(Texture2D source, Texture2D dest, int sourceIdx, int destIdx, 
        bool useAlpha = true)
    {
        UnityEngine.Profiling.Profiler.BeginSample("source/dest GetRawTextureData");
        var sourceArray = source.GetRawTextureData<Color32>();
        var dstArray = dest.GetRawTextureData<Color32>();
        UnityEngine.Profiling.Profiler.EndSample();
        
        //determine where in the source we need to start. We're looking for a 32x32 block of pixels
        //and the sourceIdx determines where we start.
        
        UnityEngine.Profiling.Profiler.BeginSample("Get PX coordinates");
        var srcPx = GetPixelCoordinatesFromTileIdx(source, sourceIdx, true);
        var dstPx = GetPixelCoordinatesFromTileIdx(dest, destIdx, false);
        UnityEngine.Profiling.Profiler.EndSample();

        for (int y = 0; y < tileResolution; y++)
        {
            //if we aren't using alpha, we can just str8 copy a row of pixels from source to dest
            //and skip the X loop
            if (!useAlpha)
            {
                UnityEngine.Profiling.Profiler.BeginSample("Slice, no alpha");
                int dstPixel = dstPx.x + (dstPx.y + y) * dest.width;
                int srcPixel = srcPx.x + (srcPx.y + y) * source.width;

                dstArray.Slice( dstPixel, tileResolution).CopyFrom(sourceArray.Slice(srcPixel,tileResolution));
                UnityEngine.Profiling.Profiler.EndSample();                
                continue;
            }
            
            //from here down, we are using alpha, so pixels may have to blend and not just copy directly.
            for (int x = 0; x < tileResolution; x++)
            {
                //the size of one row indicates how many pixels we need to advance to grab a pixel
                //1 Y higher. 
                int dstPixel = dstPx.x + x + (dstPx.y + y) * dest.width;
                int srcPixel = srcPx.x + x + (srcPx.y + y) * source.width;

                //if (srcPixel > sourceArray.Length ||
                //    dstPixel > dstArray.Length)
                //{
                //    Debugger.Break();
                //}

                var sp = sourceArray[srcPixel];
                UnityEngine.Profiling.Profiler.BeginSample("Color lerp in copy with alpha");
                dstArray[dstPixel] = Color32.Lerp(dstArray[dstPixel], sp, sp.a / 255f);
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }
    }

    public void ForceRebuildTexture(int columns, int rows)
    {
        size_x = columns;
        size_z = rows;
        tilesAlreadyRevealed = new HashSet<int>();
        tilesAlreadyRevealedTranslucent = new HashSet<int>();

        if (isMiniMap)
        {
            indexOfBaseTileContentsForMinimap = new int[columns, rows];
            indexOfFullTileContentsForMinimap = new int[columns, rows];
            indexOfBaseTileContentsForTranslucentMinimap = new int[columns, rows];
            indexOfFullTileContentsForTranslucentMinimap = new int[columns, rows];

            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    indexOfBaseTileContentsForMinimap[x, y] = -1;
                    indexOfFullTileContentsForMinimap[x, y] = -1;
                    indexOfBaseTileContentsForTranslucentMinimap[x, y] = -1;
                    indexOfFullTileContentsForTranslucentMinimap[x, y] = -1;
                }
            }
        }

        DestroyTexturesAndCleanup();
    }

    // These indices are hardcoded based on Fervir data
    public int GetBaseMinimapConvertedIndex(MapTileData mtd)
    {
        if (mtd.tileType == TileTypes.WALL)
        {
            return 14;
        }

        if (mtd.CheckTag(LocationTags.ISLANDSWATER)) return 16;
        if (mtd.CheckTag(LocationTags.ELECTRIC)) return 15;
        if (mtd.CheckTag(LocationTags.WATER)) return 11;
        if (mtd.CheckTag(LocationTags.LAVA)) return 13;
        if (mtd.CheckTag(LocationTags.MUD) || mtd.CheckTag(LocationTags.SUMMONEDMUD)) return 12;

        foreach(Actor act in mtd.GetAllActors())
        {
            if (!act.visibleOnMinimap) continue;
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE &&
                act.playerCollidable && !act.targetable)
            {
                Destructible dt = act as Destructible;
                if (dt.mapObjType != SpecialMapObject.SLIMETOWER)
                {
                return 14; // An indestructible, un-targetable object
                }
                else
                {
                    return 10;
                }
            }
            else if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (dt.mapObjType == SpecialMapObject.LAVA_LIKE_HAZARD)
                {
                    return 13; // something that deals damage like lava, but isn't lava, should still be displayed as lava
                }
            }
        }

        return 10; // Ground
    }

    public int GetOverlayMinimapConvertedIndex(MapTileData mtd)
    {
        if (mtd.GetAllActors().Count == 0) return -1; // Nothin 

        if (GameMasterScript.heroPCActor.GetPos() == mtd.pos)
        {
            return 0;
        }

        // Check non-targetable destructibles first.
        if (mtd.specialMapObjectsInTile[(int)SpecialMapObject.FOUNTAIN])
        {
            return 17;
        }
        else if (mtd.specialMapObjectsInTile[(int)SpecialMapObject.SLIMETOWER])
        {
            Map_SlimeDungeon msd = MapMasterScript.activeMap as Map_SlimeDungeon;
            bool anyActor = false;
            Actor tower = mtd.FindActorByRef("exp2_slime_tower", out anyActor);
            Map_SlimeDungeon.SlimeStatus ss = Map_SlimeDungeon.GetSlimeStatusFromActorData(tower);
            switch(ss)
            {
                case Map_SlimeDungeon.SlimeStatus.Enemy_1:
                    return 23;
                case Map_SlimeDungeon.SlimeStatus.Friendly:
                    return 22;
                case Map_SlimeDungeon.SlimeStatus.Unslimed:
                    return 24;
            }            
        }
        else if (mtd.specialMapObjectsInTile[(int)SpecialMapObject.FLOORSWITCH])
        {
            Destructible fSwitch = mtd.GetActorRef("obj_floorswitch") as Destructible;
            if (fSwitch.destroyed || fSwitch.isDestroyed)
            {
                return 19;
            }
            else
            {
                return 18;
            }
        }
        
        NPC n = mtd.GetInteractableNPC();
        if (n != null)
        {
            if (!string.IsNullOrEmpty(n.shopRef))
            {
                return 20; // merchant!
            }
            else
            {
                return 5; // friendly thing that isn't merchant
            }
        }
        
        
        Actor targ = mtd.GetTargetable();
        if (targ != null)
        {
            if (targ.GetActorType() == ActorTypes.MONSTER)
            {
                Monster m = targ as Monster;
                if (m.actorfaction == Faction.PLAYER)
                {
                    return 5; // friendly thing
                }
                else
                {
                if (m.isChampion || m.isBoss) return 6;
                if (m.actorRefName == "mon_fungalcolumn") return 21;
                return 1;
                }
            }
            else
            {
            Destructible dt = targ as Destructible;
            if (dt.mapObjType == SpecialMapObject.MONSTERSPAWNER || dt.actorRefName == "obj_jobtrial_crystal")
            {
                return 4;
            }
                else if (dt.mapObjType == SpecialMapObject.TREASURESPARKLE)
            {
                return 2; // treasure chest
            }
                else if (dt.mapObjType == SpecialMapObject.SWINGINGVINE)
            {
                return 5;
            }
                else if (dt.mapObjType == SpecialMapObject.FOUNTAIN)
            {
                return 17; 
            }

            return 3; // some kinda destructible?
            }
        }

        if (mtd.AreItemsInTile()) return 7; // item bag

        Stairs st = mtd.GetStairsInTile();
        {
            if (st != null)
            {
                if (st.stairsUp)
                {
                    return 9;
                }
                return 8;
            }
        }

        return -1;
    }

    public Texture2D GetTexture()
    {
        return latestTexture;
    }

    //A secondary texture used just for certain minimaps
    public Texture2D GetTranslucentMinimapTexture()
    {
        return texTranslucentMiniMap;
    }

    public void BuildTexture() 
    {
        if (fogOfWar) return;

        if (tileResolution == 0) return;

        int texWidth = size_x * tileResolution;
		int texHeight = size_z * tileResolution;

        bool latestIsNull = latestTexture == null;

        if (latestIsNull || !isMiniMap)
        {
            if (!latestIsNull)
            {
               DestroyTexturesAndCleanup();
            }

            if (isMiniMap)
            {
                ResetMinimapInfo();
            }
            
            latestTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            var pixels = latestTexture.GetRawTextureData<Color32>();
            for (int t = 0; t < pixels.Length; t++)
            {
                pixels[t] = Color.black;
            }

            if (isMiniMap)
            {
                texTranslucentMiniMap = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
                var translucentPx = texTranslucentMiniMap.GetRawTextureData<byte>();
                for (int t = 0; t < translucentPx.Length; t++)
                {
                    translucentPx[t] = 0;
                }
            }
        }


        //Copy these pixels over, but only do so for the texture we need this frame.
        if (!isMiniMap ||
            (isMiniMap && MinimapUIScript.MinimapState != MinimapStates.TRANSLUCENT))
        {
            UnityEngine.Profiling.Profiler.BeginSample("GenerateTextureForMap - not translucent");
            latestTexture = GenerateTextureForMap(latestTexture, false);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        if (isMiniMap && MinimapUIScript.MinimapState == MinimapStates.TRANSLUCENT )
        {
            UnityEngine.Profiling.Profiler.BeginSample("GenerateTextureForMap - yes translucent");
            //if (!DEBUG_MODE_NO_TRANSLUCENT_MINIMAP) texTranslucentMiniMap = GenerateTextureForMap(texTranslucentMiniMap, true);
            texTranslucentMiniMap = GenerateTextureForMap(texTranslucentMiniMap, true);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        

        latestTexture.filterMode = FilterMode.Point;
        latestTexture.wrapMode = TextureWrapMode.Clamp;
        latestTexture.Apply();

        if (!isMiniMap)
        {
            mf = gameObject.GetComponent<MeshFilter>();
            mr = gameObject.GetComponent<MeshRenderer>();
            mc = gameObject.GetComponent<MeshCollider>();

            if (mr.sharedMaterials[0].mainTexture != null)
            {
                Destroy(mr.sharedMaterials[0].mainTexture);
            }
            mr.sharedMaterials[0].mainTexture = latestTexture;
        }
        //else if (!DEBUG_MODE_NO_TRANSLUCENT_MINIMAP)
        else
        {
            //if this a minimap, seal and apply the secondary translucent texture too
            texTranslucentMiniMap.filterMode = FilterMode.Point;
            texTranslucentMiniMap.wrapMode = TextureWrapMode.Clamp;
            texTranslucentMiniMap.Apply();
        }
        
    }

    private void OnDestroy()
    {
        DestroyTexturesAndCleanup();
        
        if (mf != null &&
            mf.mesh != null)
        {            
            MeshPooler.ReturnMeshToPool(mf.mesh);
            //Destroy(mf.mesh);
        }

        if (mr != null &&
            mr.sharedMaterials.Length > 0 &&
            mr.sharedMaterials[0] != null)
        {
            Destroy(mr.sharedMaterials[0].mainTexture);
        }
        
        //Destroy(terrainTiles);
        //Destroy(decorTiles);
        //Destroy(grassTiles);
        //Destroy(grassDecorTiles);
    }

    public void DestroyTexturesAndCleanup()
    {
        if (latestTexture != null)
        {
            Destroy(latestTexture);
            latestTexture = null;
        }

        if (texTranslucentMiniMap != null)
        {
            Destroy(texTranslucentMiniMap);
            texTranslucentMiniMap = null;
        }
    }

    /// <summary>
    /// Creates a map or minimap texture based on the world map information
    /// </summary>
    /// <param name="destTexture">The texture we are modifying</param>
    /// <param name="bIsTranslucent">true if you would like the tiles drawn in the Translucent map fashion</param>
    /// <returns>sourceTexture but modified</returns>
    Texture2D GenerateTextureForMap(Texture2D destTexture, bool bIsTranslucent)
    {
        var neighbors = new MapTileData[4];

        //gridmaps start blank except for ground tiles.
        if (isGridMap)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Grid Map raw ass texture data.");
            var dstNative = destTexture.GetRawTextureData<byte>();
            for (int t = 0; t < dstNative.Length; t++)
            {
                dstNative[t] = 0;
            }
            destTexture.Apply();
            UnityEngine.Profiling.Profiler.EndSample();
        }
        
        for (int y = 0; y < size_z; y++)
        {
            for (int x = 0; x < size_x; x++)
            {
                //the tile on the source image will we be modifying here
                var dstTileIdx = x + y * size_x;
                                
                MapTileData mtd = MapMasterScript.GetTile( new Vector2(x, y));
                if (mtd == null)
                {
                    continue;
                }

                //grid maps just draw a rock or nothing? 
                if (isGridMap)
                {
                    if (mtd.tileType == TileTypes.GROUND)
                    {
                        CopyBlockFromTextureToTexture(terrainTiles, destTexture, 0, dstTileIdx, false);
                    }
                    
                    continue;
                }

                //Not minimap is straight up -- base ground, then decor. 
                if (!isMiniMap)
                {
                    mtd = MapMasterScript.GetTile(new Vector2(x, y));

                    //copy base terrain
                    var srcIdx = mtd.indexOfSpriteInAtlas;
                    CopyBlockFromTextureToTexture(terrainTiles, destTexture, srcIdx, dstTileIdx, false);
                    
                    //now look for potential grass/decor stuff
                    if (mtd.CheckTag(LocationTags.HASDECOR))
                    {
                        if (!mtd.CheckTag(LocationTags.GRASS))
                        {
                            CopyBlockFromTextureToTexture(decorTiles, destTexture, mtd.indexOfDecorSpriteInAtlas,
                                dstTileIdx );
                        }
                        else
                        {
                            CopyBlockFromTextureToTexture(grassDecorTiles, destTexture, mtd.indexOfDecorSpriteInAtlas,
                                dstTileIdx );
                        }

                    }
                    if (mtd.CheckTag(LocationTags.GRASS) || mtd.CheckTag(LocationTags.GRASS2))
                    {
                        CopyBlockFromTextureToTexture(grassTiles, destTexture, mtd.indexOfGrassSpriteInAtlas,
                            dstTileIdx );
                    }

                    continue;
                }
                
                //The index that represents the terrain we're using
                int idxForMiniMap = 0;
                
                //And the index that represents the icon on the terrain.
                int overlayIndexForMinimap = 0;
                
                //UnityEngine.Profiling.Profiler.BeginSample("Checking if tile is explored or on edge");
                bool tileIsExploredAndShouldBeDrawn = false;
                if ( x == 0 || y == 0 || x == MapMasterScript.activeMap.columns-1 || 
                     y == MapMasterScript.activeMap.rows-1  ||
                     MapMasterScript.activeMap.exploredTiles[x,y] ||
                     UIManagerScript.dbRevealMode)
                {
                    tileIsExploredAndShouldBeDrawn = true;
                    
                    //this is the base terrain we're drawing
                    idxForMiniMap = GetBaseMinimapConvertedIndex(mtd);
                    
                    //and this is the icon on top of it.
                    overlayIndexForMinimap = GetOverlayMinimapConvertedIndex(mtd);

                    //Don't draw the terrain on the minimap more than once.
                    //If we've never drawn it, or if it has changed, go ahead and draw.
                    int tileHash = x + (y << 16);
                    bool thisTileIsDifferentFromLastTime = false;

                    if (bIsTranslucent)
                    {
                        if (idxForMiniMap != indexOfBaseTileContentsForTranslucentMinimap[x, y] ||
                            overlayIndexForMinimap != indexOfFullTileContentsForTranslucentMinimap[x, y] ||
                            !tilesAlreadyRevealedTranslucent.Contains(tileHash))
                        {
                            thisTileIsDifferentFromLastTime = true;
                        }
                    }
                    else
                    {
                        if (idxForMiniMap != indexOfBaseTileContentsForMinimap[x, y] ||
                            overlayIndexForMinimap != indexOfFullTileContentsForMinimap[x, y] ||
                            !tilesAlreadyRevealed.Contains(tileHash))
                        {
                            thisTileIsDifferentFromLastTime = true;
                        }
                    }
                    
                    //If any of the above conditions are met...
                    if( thisTileIsDifferentFromLastTime)
                    {
                        //track this tile so we don't draw it again unless the terrain or contents change.
                        if (bIsTranslucent)
                        {
                            tilesAlreadyRevealedTranslucent.Add(tileHash);
                        }
                        else
                        {
                            tilesAlreadyRevealed.Add(tileHash);
                        }
                        
                        //Shape the edges if we're in a translucent map. This requires grabbing pixels from the source,
                        //mucking with them, then putting them onto the destination. 
                        if (bIsTranslucent)
                        {
                            if (blockBucket == null)
                            {
                                blockBucket = new Color32[ tileResolution * tileResolution];
                            }
                            
                            //here's the default image
                            GetBlockFromTerrainSourceTexture(terrainTiles, idxForMiniMap, blockBucket);
                            
                            //prepare for futzing
                            for (int t = 0; t < 4; t++)
                            {
                                neighbors[t] = MapMasterScript.GetTile(MapMasterScript.directions[t] + mtd.pos);
                            }
    
                            //futz away
                            blockBucket = GenerateColorArrayForTileUsingHellaTranslucentMap(blockBucket, mtd, neighbors);
                            
                            SetPixelsFromBlockIntoTexture(destTexture, dstTileIdx, blockBucket);
                        }
                        //otherwise, just copy it over
                        else
                        {
                            UnityEngine.Profiling.Profiler.BeginSample("CopyBlockFromTextureToTexture called");
                            CopyBlockFromTextureToTexture(terrainTiles, destTexture, idxForMiniMap, dstTileIdx);
                            UnityEngine.Profiling.Profiler.EndSample();
                        }
                    }
                }

                //Now draw symbols over the tile.
                if (tileIsExploredAndShouldBeDrawn)
                {
                    //if overlayIndexForMinimap is 0, then this tile has the hero in it.
                    if (overlayIndexForMinimap == 0)
                    {
                        //find out where we are on the texture
                        float fXPos = mtd.pos.x / MapMasterScript.activeMap.columns;
                        float fYpos = mtd.pos.y / MapMasterScript.activeMap.rows;

                        //setting this value won't move anything if we're not in translucent mode, but we'd like to keep it updated.
                        MinimapUIScript.SetDesiredTransformPositionRelativeToScreen(new Vector2(fXPos, fYpos));
                    }

                    //-1 means an empty tile, we should not draw extra icons in it.
                    bool bShouldDraw = overlayIndexForMinimap >= 0 &&
                        (
                            bIsTranslucent ?
                              idxForMiniMap != indexOfBaseTileContentsForTranslucentMinimap[x, y] ||
                              overlayIndexForMinimap != indexOfFullTileContentsForTranslucentMinimap[x, y] :
                              idxForMiniMap != indexOfBaseTileContentsForMinimap[x, y] ||
                              overlayIndexForMinimap != indexOfFullTileContentsForMinimap[x, y]
                        );

                    //minimap terrain tiles also include the symbols for player, monsters, boxes, etc.
                    //so we'll pull from that texture and make sure to use alpha
                    if (bShouldDraw)
                    {
                        CopyBlockFromTextureToTexture(terrainTiles, destTexture, overlayIndexForMinimap, dstTileIdx);
                    }

                }
                
                //store this for later.
                indexOfBaseTileContentsForTranslucentMinimap[x, y] = idxForMiniMap;
                indexOfBaseTileContentsForMinimap[x, y] = idxForMiniMap;

                indexOfFullTileContentsForTranslucentMinimap[x, y] = overlayIndexForMinimap;
                indexOfFullTileContentsForMinimap[x, y] = overlayIndexForMinimap;

            }
        }
        UnityEngine.Profiling.Profiler.BeginSample("Final Apply");
        destTexture.Apply();
        UnityEngine.Profiling.Profiler.EndSample();

        return destTexture;
    }

    /// <summary>
    /// Creates a tile based on the state of the neighbors, highlighting only edges that are adjacent to different tiles.
    /// </summary>
    /// <param name="baseColorArray"></param>
    /// <param name="neighbors"></param>
    /// <returns></returns>
    Color32[] GenerateColorArrayForTileUsingHellaTranslucentMap(Color32[] baseColorArray, MapTileData selfTile, MapTileData[] neighbors)
    {
        if (selfTile == null)
            return null;

        //make the pixels clear for now
        byte clearValue32 = 0;

        //who we are
        int iMyIdx = GetBaseMinimapConvertedIndex(selfTile);

        //if we aren't ground or wall, don't clear the tile 100%
        //we want to see these to stand out in the map, as these
        //tiles cause different behavior
        //The function above currently returns 10 for ground and 14 for wall.
        if (iMyIdx != 10 && iMyIdx != 14)
        {
            clearValue32 = (byte)(0.4f * 255.0f);
        }

        for (int t= 0; t < baseColorArray.Length; t++)
        {
            baseColorArray[t].a = clearValue32;
        }

        //how alpha is alpha for the edges?
        byte edgeAlpha = (byte)(0.7f * 255.0f);

        //size of one side of the tile
        int iEdgeLength = (int)Math.Sqrt(baseColorArray.Length);


        //0,1,2,3 are N,E,S,W
        for (int iNeighborIndex = 0; iNeighborIndex < 4; iNeighborIndex++)
        {
            //cache me
            var bro = neighbors[iNeighborIndex];

            //draw our own border on this side if we aren't adjacent to one of our own selves
            //note that this considers ground and river to be the same type, which is great,
            //since ground borders 
            if (bro == null ||
                //MapMasterScript.activeMap.exploredTiles[(int)bro.pos.x, (int)bro.pos.y] ||
                selfTile.tileType != bro.tileType)
            {
                //the array comes in starting at the bottom left of the tile. 
                
                /*  56 57 58 59 60 61 62 63 
                 *
                 *  ...
                 *
                 *  16 17 18 19 20 21 22 23
                 *  8  9  10 11 12 13 14 15
                 *  0  1  2  3  4  5  6  7
                 */
                
                switch (iNeighborIndex)
                {
                    //never 
                    case 0: //starting at top right last and moving left.
                        int lastPx = iEdgeLength * iEdgeLength;
                        for (int idx = lastPx-1; idx >= lastPx - iEdgeLength; idx--)
                        {
                            baseColorArray[idx].a = edgeAlpha;
                        }
                        break;
                    //eat
                    case 1: //starting at bottom right last (7) and moving up
                        for (int idx = 0; idx < iEdgeLength; idx++)
                        {
                            baseColorArray[iEdgeLength - 1 + idx * iEdgeLength ].a = edgeAlpha;
                        }
                        break;
                    //soggy
                    case 2: //starting at 0 and moving right.
                        for (int idx = 0; idx < iEdgeLength; idx++)
                        {
                            baseColorArray[idx].a = edgeAlpha;
                        }
                        break;
                    //waffles
                    case 3: //starting at 0 and moving up
                        for (int idx = 0; idx < iEdgeLength; idx++)
                        {
                            baseColorArray[idx * iEdgeLength].a = edgeAlpha;
                        }
                        break;
                }

            }
        }

        return baseColorArray;
    }

	public void BuildMesh()
    {
        if (isMiniMap)
        {
            BuildTexture();
            //UnityEngine.Profiling.Profiler.EndSample();
            return;
        }

		int numTiles = size_x * size_z;
		int numTris = numTiles * 2;
		
		int vsize_x = size_x + 1;
		int vsize_z = size_z + 1;
		int numVerts = vsize_x * vsize_z;

        //Debug.Log("Preparing to build mesh " + size_x + " by " + size_z);

		// Generate the mesh data
		Vector3[] vertices = new Vector3[ numVerts ];
		Vector3[] normals = new Vector3[numVerts];
		Vector2[] uv = new Vector2[numVerts];
		
		int[] triangles = new int[ numTris * 3 ];

		int x, z;
		for(z=0; z < vsize_z; z++) {
			for(x=0; x < vsize_x; x++) {
				vertices[ z * vsize_x + x ] = new Vector3( x*tileSize, 0, -z*tileSize );
				normals[ z * vsize_x + x ] = Vector3.up;
				uv[ z * vsize_x + x ] = new Vector2( (float)x / size_x, 1f - (float)z / size_z );
			}
		}
		//Debug.Log ("Done Verts!");
		
		for(z=0; z < size_z; z++) {
			for(x=0; x < size_x; x++) {
				int squareIndex = z * size_x + x;
				int triOffset = squareIndex * 6;
				triangles[triOffset + 0] = z * vsize_x + x + 		   0;
				triangles[triOffset + 2] = z * vsize_x + x + vsize_x + 0;
				triangles[triOffset + 1] = z * vsize_x + x + vsize_x + 1;
				
				triangles[triOffset + 3] = z * vsize_x + x + 		   0;
				triangles[triOffset + 5] = z * vsize_x + x + vsize_x + 1;
				triangles[triOffset + 4] = z * vsize_x + x + 		   1;
			}
		}

        //Debug.Log ("Done Triangles!");

        // Create a new Mesh and populate with the data

        //Mesh mesh = new Mesh();
        Mesh mesh = MeshPooler.GetMesh();

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.normals = normals;
		mesh.uv = uv;

		mf = gameObject.GetComponent<MeshFilter>();
        mr = gameObject.GetComponent<MeshRenderer>();
		mc = gameObject.GetComponent<MeshCollider>();

        // Assign our mesh to our filter/renderer/collider

        if (mf.mesh != null)
        {
            //Destroy(mf.mesh);
            MeshPooler.ReturnMeshToPool(mf.mesh);
        }
        
        mf.mesh = mesh;
		mc.sharedMesh = mesh;

        if (!fogOfWar)
        {
            BuildTexture();
        }		

        if (!isMiniMap)
        {
            transform.position = new Vector3(-0.5f, size_z + -0.5f, transform.position.z);
        }		
	}

    public void ApplyTexture(Texture2D newText)
    {
        if (isMiniMap) return;
        if (mr.sharedMaterials[0].mainTexture != null && mr.sharedMaterials[0].mainTexture != newText)
        {
            Destroy(mr.sharedMaterials[0].mainTexture);
        }
        mr.sharedMaterials[0].mainTexture = newText;
        transform.position = new Vector3(-0.5f, size_z + -0.5f, transform.position.z);
    }

    /// <summary>
    /// Clear out tracking information for the map so we can draw it anew.
    /// </summary>
    public void ResetMinimapInfo()
    {
        tilesAlreadyRevealed.Clear();
        tilesAlreadyRevealedTranslucent.Clear();
        indexOfBaseTileContentsForMinimap = new int[size_x,size_z];
        indexOfBaseTileContentsForTranslucentMinimap = new int[size_x,size_z];
        indexOfFullTileContentsForMinimap = new int[size_x,size_z];
        indexOfFullTileContentsForTranslucentMinimap = new int[size_x,size_z];
    }
}

