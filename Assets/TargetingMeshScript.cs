using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TargetingMeshScript : MonoBehaviour {

    private MeshFilter mf;
    private MeshRenderer mr;
    public List<Vector2> goodTiles = new List<Vector2>();
    public List<Vector2> badTiles = new List<Vector2>();
    public List<Vector2> usedTiles = new List<Vector2>();
    public List<Vector2> reqOpenTiles = new List<Vector2>();
    private float transparency = 0.0f;
    public float maxTransparency;
    public float minTransparency;
    public float timeInterval;
    public float transparencyInterval;
    public bool isMouseTargetingMesh;
    private float curTransparencyInterval;
    private bool transparencyIncreasing = true;
    private float prevTime;
    public Vector2 minBounds;
    public Vector2 maxBounds;
    public Texture2D targetSquare;

    private Material myMaterial;

    public Color goodColor;

    private Color myRed = new Color(0.78431f, 0.11764f, 0.11764f);
    public Color myGreen = new Color(0.235294f, 0.78431f, 0.235294f);
    private Color myBlue = new Color(0.196078f, 0.5098039f, 0.823529f);
    private Color myYellow = Color.yellow;

    static int globalMeshID;
    public int myMeshID;

    public HashSet<Vector2> reusableTileHashSet;

    // Use this for initialization
    public void SetBounds(int minX, int minY, int maxX, int maxY)
    {
        minBounds = new Vector2(minX, minY);
        maxBounds = new Vector2(maxX, maxY);
    }

    void Awake()
    {
        goodColor = myGreen;
        myMeshID = globalMeshID;
        globalMeshID++;
        reusableTileHashSet = new HashSet<Vector2>();
    }

    void Start () {
        if (mf == null)
        {
            mf = gameObject.GetComponent<MeshFilter>();
            mr = gameObject.GetComponent<MeshRenderer>();            
        }

        if (myMaterial == null)
        {
            myMaterial = mr.material;            
        }

        mr.sortingLayerName = "Actor";
        mr.sortingOrder = 0;

	}

    /// <summary>
    /// Start with a (-1,-1,0,0) rect, and expand its boundaries based on the vector we send in.
    /// Lots of vectors get sent here, and the rectangle grows to match the size.
    /// </summary>
    /// <param name="checkVec"></param>
    /// <param name="bounds"></param>
    void CheckVectorAgainstBounds(Vector2 checkVec, ref Rect bounds)
    {
        if (checkVec == Vector2.zero)
        {
            return;
        }

        if (bounds.xMin < 0)
        {
            bounds.xMin = checkVec.x;
        }
        else if (checkVec.x < bounds.xMin)
        {
            bounds.xMin = checkVec.x;
        }
        else if (checkVec.x > bounds.xMax)
        {
            bounds.xMax = checkVec.x;
        }

        if (bounds.yMin < 0)
        {
            bounds.yMin = checkVec.y;
        }
        else if (checkVec.y < bounds.yMin)
        {
            bounds.yMin = checkVec.y;
        }
        else if (checkVec.y > bounds.yMax)
        {
            bounds.yMax = checkVec.y;
        }

    }

    /// <summary>
    /// Since we're making 4 quads per tile for the select mesh, this will save us some code space.
    /// </summary>
    /// <param name="xMin"></param>
    /// <param name="yMin"></param>
    /// <param name="index"></param>
    /// <param name="listOfPoints"></param>
    void CreateQuadHalfATileBig(float xMin, float yMin, int index, Vector3[] listOfPoints)
    {
        listOfPoints[index] = new Vector3(xMin, yMin,0);
        listOfPoints[index + 1] = new Vector3(xMin + 0.5f, yMin,0);
        listOfPoints[index + 2] = new Vector3(xMin, yMin + 0.5f, 0);
        listOfPoints[index + 3] = new Vector3(xMin + 0.5f, yMin + 0.5f, 0);
    }
    
	
    public void BuildMesh()
    {
        // Generate mesh data
        // Let's try using a hashset here for efficiency
        reusableTileHashSet.Clear();

        Rect tileBounds = new Rect(-1,-1,1,1);

        for (int i = 0; i < goodTiles.Count; i++)
        {
            CheckVectorAgainstBounds(goodTiles[i], ref tileBounds);
            reusableTileHashSet.Add(goodTiles[i]);
        }
        for (int i = 0; i < badTiles.Count; i++)
        {
            CheckVectorAgainstBounds(badTiles[i], ref tileBounds);
            reusableTileHashSet.Add(badTiles[i]);
        }
        for (int i = 0; i < reqOpenTiles.Count; i++)
        {
            CheckVectorAgainstBounds(reqOpenTiles[i], ref tileBounds);
            reusableTileHashSet.Add(reqOpenTiles[i]);
        }

        //eight triangles per tile now. Huzzah!
        
        int numTiles = reusableTileHashSet.Count;
        Vector3[] vertices = new Vector3[numTiles*16];
        int[] triangles = new int[numTiles*24];
        Vector3[] normals = new Vector3[numTiles * 16];
        Vector2[] uv = new Vector2[numTiles * 16];

        // Coordinates of the vertices
        float offset = -0.5f;

        int counter = 0; // counter, since we can't iterate over hashset
        foreach(Vector2 t in reusableTileHashSet)
        //for (int i = 0; i < numTiles; i++)
        {
            /*    
             *    A  B E  F
             *
             *    8  9 C  D
             *    2  3 6  7
             * 
             *    0  1 4  5
             */

            //var tileXMin = tiles[i].x + offset;
            //var tileYMin = tiles[i].y + offset;
            var tileXMin = t.x + offset;
            var tileYMin = t.y + offset;

            int index = counter * 16;
            CreateQuadHalfATileBig(tileXMin, tileYMin, index, vertices);
            CreateQuadHalfATileBig(tileXMin + 0.5f, tileYMin, index + 4, vertices);
            CreateQuadHalfATileBig(tileXMin, tileYMin + 0.5f, index + 8, vertices);
            CreateQuadHalfATileBig(tileXMin + 0.5f, tileYMin + 0.5f, index + 12, vertices);
            counter++;
        }

        // Which points go to which triangles

        for (int i = 0; i < numTiles; i++)
        {
            int triangleIndex = i * 24;
            int vertexIndex = i * 16;

            //each tile is now 4 quads
            for (int t = 0; t < 4; t++)
            {
                triangles[triangleIndex]     = vertexIndex; // Triangle 0, point 0 matches with vert 0 in the upper left
                triangles[triangleIndex + 1] = vertexIndex + 3; // Triangle 0, point 1 matches with vert 3 in lower right
                triangles[triangleIndex + 2] = vertexIndex + 2; // T0 P2 match P2 in lower left
                triangles[triangleIndex + 3] = vertexIndex; // T1 P3 is point 0
                triangles[triangleIndex + 4] = vertexIndex + 1; // T1 P4 is point 1
                triangles[triangleIndex + 5] = vertexIndex + 3; // T1 P4 is point 3

                triangleIndex += 6;
                vertexIndex += 4;
            }

        }

        // Direction of the points
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = new Vector3(0, 0, -1);
        }
        
        //UVs are actually numbers between 0 and 1 
        //We know right now that our source material consists of 5 tiles
        //in a horizontal strip. So the V values of our UVs will always be 0-1,
        //but the U values will be some offset of 0.2f
        //M a g i c  n u m b a z 

        float xTileWidth = 0.2f;

        //for (int i = 0; i < numTiles; i++)
        counter = 0;
        foreach (Vector2 tile in reusableTileHashSet)
        {
            //each tile is going to have four quads in it. What we draw in each quad is dependent on the neighboring tiles
            //however, each quad cares about a different trio of neighboring tiles.
            
            //the first quad as listed below is the SW quad. It cares about the tiles to the S, SW, and W.
            //the same goes for the other three quads based on their position in the tile.
            
            /*    
             *    A  B E  F
             *     NW   NE 
             *    8  9 C  D
             *    2  3 6  7
             *     SW   SE
             *    0  1 4  5
             */

            var uvStart = counter * 16;
            //var checkTile = tiles[i];
            var checkTile = tile;
            
            //SW
            AssignUVsToCornerQuadBasedOnNeighbors(uvStart, checkTile, reusableTileHashSet, new Vector2(-1, -1), uv, xTileWidth,
                false, true);

            //SE
            AssignUVsToCornerQuadBasedOnNeighbors(uvStart+4, checkTile, reusableTileHashSet, new Vector2(1, -1), uv, xTileWidth,
                true, true);

            //NW
            AssignUVsToCornerQuadBasedOnNeighbors(uvStart+8, checkTile, reusableTileHashSet, new Vector2(-1, 1), uv, xTileWidth,
                false, false);

            //NE
            AssignUVsToCornerQuadBasedOnNeighbors(uvStart+12, checkTile, reusableTileHashSet, new Vector2(1, 1), uv, xTileWidth,
                true, false);

            counter++;
            
        }

        //Mesh myMesh = null;

        if (mf == null)
        {
            mf = gameObject.GetComponent<MeshFilter>();
        }
        if (mr == null)
        {
            mr = gameObject.GetComponent<MeshRenderer>();
        }
        if (myMaterial == null)
        {
            myMaterial = mr.material;
        }

        Mesh mToEdit = mf.mesh;

        // Create mesh and populate with data
        if (mToEdit == null)
        {
            mToEdit = MeshPooler.GetMesh();
        }
        else
        {
            mToEdit.triangles = null;
            mToEdit.vertices = null;
            mToEdit.normals = null;
            mToEdit.uv = null;
        }

        mToEdit.vertices = vertices;
        mToEdit.triangles = triangles;
        mToEdit.normals = normals;
        mToEdit.uv = uv;

        Color[] colors = new Color[vertices.Length];
        for (int i = 0; i < goodTiles.Count; i++)
        {
            // Color good tiles
            Color colorToUse = goodColor;
            if (isMouseTargetingMesh)
            {
                colorToUse = goodColor;
            }

            if (usedTiles.Contains(goodTiles[i]))
            {
                colorToUse = myBlue;
            }

            for (int x = 0; x < 16; x++)
            {
                colors[(i * 16) + x] = colorToUse;
            }
        }
        for (int i = 0; i < badTiles.Count; i++)
        {
            // Color bad tiles red
            int index = 16 * (goodTiles.Count);
            for (int x = 0; x < 16; x++)
            {
                colors[index + (i * 16)+x] = myRed;
            }
        }

        for (int i = 0; i < reqOpenTiles.Count; i++)
        {
            // Color open tiles yellow
            int index = (16 * goodTiles.Count) + (16 * badTiles.Count);
            for (int x = 0; x < 16; x++)
            {
                colors[index + (i * 16) + x] = myYellow;
            }
        }

        mToEdit.colors = colors;

        Color mc = myMaterial.color;
        mc.a = minTransparency;
        myMaterial.color = mc;

        mr.material.color = mc;

        mf.mesh = mToEdit;

        if (GameMasterScript.heroPC != null)
        {
            var heroSR = GameMasterScript.heroPC.GetComponent<SpriteRenderer>();
            mr.sortingOrder = heroSR.sortingOrder;
        }

        prevTime += Time.deltaTime;
    }

    private void OnDestroy()
    {
        mf = gameObject.GetComponent<MeshFilter>();
        if (mf.mesh != null)
        {
            MeshPooler.ReturnMeshToPool(mf.mesh);
            //Destroy(mf.mesh);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="uvStartIndex"></param>
    /// <param name="checkTile"></param>
    /// <param name="allTiles"></param>
    /// <param name="cornerDelta"></param>
    /// <param name="uv"></param>
    /// <param name="uTileWidth"></param>
    /// <param name="flipHorizontal"></param>
    /// <param name="flipVertical"></param>
    void AssignUVsToCornerQuadBasedOnNeighbors(int uvStartIndex, Vector2 checkTile, HashSet<Vector2> allTiles, // try hashset instead of vector2, better for Contains??
        Vector2 cornerDelta, Vector2[] uv, float uTileWidth,
        bool flipHorizontal, bool flipVertical)
    {
        //this will determine our offset
        var uMinVal = 0f;
            
        bool topEmpty = !allTiles.Contains(checkTile + new Vector2(0, cornerDelta.y));
        bool cornerEmpty = !allTiles.Contains(checkTile + cornerDelta);
        bool sideEmpty = !allTiles.Contains(checkTile + new Vector2(cornerDelta.x, 0));

        //8 bits of data, but only 5 useful results.
        // tile 0: top and side are empty
        if (topEmpty && sideEmpty)
        {
            uMinVal = 0f;
        }    
            
        // tile 1: side is empty, top is not
        if (sideEmpty && !topEmpty)
        {
            uMinVal = uTileWidth * 1f;
        }
            
        // tile 2: top and side are full, but the corner is empty
        if (cornerEmpty && !topEmpty && !sideEmpty)
        {
            uMinVal = uTileWidth * 2f;
        }
            
        //tile 3: top is empty, but side is not
        if (topEmpty && !sideEmpty)
        {
            uMinVal = uTileWidth * 3f;
        }
            
        //tile 4: nothing is empty
        if (!topEmpty && !sideEmpty && !cornerEmpty)
        {
            uMinVal = uTileWidth * 4f;
        }
            
        //NW corner UVs can now be set
        uv[uvStartIndex + 0] = new Vector2(uMinVal, 0.0f);
        uv[uvStartIndex + 1] = new Vector2(uMinVal + uTileWidth, 0.0f);
        uv[uvStartIndex + 2] = new Vector2(uMinVal, 1.0f);
        uv[uvStartIndex + 3] = new Vector2(uMinVal + uTileWidth, 1.0f);

        if (flipHorizontal)
        {
            var swapVec = uv[uvStartIndex + 0];
            uv[uvStartIndex + 0] = uv[uvStartIndex + 1];
            uv[uvStartIndex + 1] = swapVec;
            
            swapVec = uv[uvStartIndex + 2];
            uv[uvStartIndex + 2] = uv[uvStartIndex + 3];
            uv[uvStartIndex + 3] = swapVec;
        }

        if (flipVertical)
        {
            var swapVec = uv[uvStartIndex + 0];
            uv[uvStartIndex + 0] = uv[uvStartIndex + 2];
            uv[uvStartIndex + 2] = swapVec;
            
            swapVec = uv[uvStartIndex + 1];
            uv[uvStartIndex + 1] = uv[uvStartIndex + 3];
            uv[uvStartIndex + 3] = swapVec; 
        }
        
    }
    

	void Update () {
        if (mf.mesh == null)
        {
            return;
        }
	    if (prevTime >= timeInterval)
        {
            if (transparencyIncreasing)
            {
                transparency += curTransparencyInterval;
                if (curTransparencyInterval < transparencyInterval)
                {
                    curTransparencyInterval *= 2; // for sine wave esque movement
                }
            }
            else
            {
                transparency -= curTransparencyInterval;
                if (curTransparencyInterval < transparencyInterval)
                {
                    curTransparencyInterval *= 2; // for sine wave esque movement
                }
            }
            if (transparency >= maxTransparency)
            {
                transparency = maxTransparency;
                transparencyIncreasing = false;
                curTransparencyInterval = transparencyInterval / 8;
            }
            if (transparency <= minTransparency)
            {
                transparency = minTransparency;
                transparencyIncreasing = true;
                curTransparencyInterval = transparencyInterval / 8;
            }
            prevTime = 0;
        }

        prevTime += Time.deltaTime;

        Color mc = myMaterial.color;

        // cycle completely every two seconds
        mc.a = transparency; // 0.3f + 0.1f * Mathf.Sin(Time.realtimeSinceStartup * 3.14f);
        myMaterial.color = mc;
        mr.material = myMaterial;
    }   
}

public class MeshPooler
{
    static Stack<Mesh> meshStack;
    const int INIT_STACK_AMOUNT = 25;
    static bool initialized;

    static void Initialize()
    {
        meshStack = new Stack<Mesh>();
        for (int i = 0; i < INIT_STACK_AMOUNT; i++)
        {
            Mesh m = new Mesh();
            meshStack.Push(m);
        }

        initialized = true;
    }

    public static Mesh GetMesh()
    {
        return new Mesh();
        
        if (!initialized) Initialize();

        Mesh mToReturn = null;

        if (meshStack.Count == 0)
        {
            for (int i = 0; i < 5; i++)
            {
#if UNITY_EDITOR
                Debug.Log("Had to make more meshes.");
#endif
                Mesh m = new Mesh();
                meshStack.Push(m);
            }
        }

        mToReturn = meshStack.Pop();

        mToReturn.triangles = null;
        mToReturn.normals = null;
        mToReturn.vertices = null;
        mToReturn.uv = null;

        return mToReturn;
    }

    public static void ReturnMeshToPool(Mesh m)
    {
        GameObject.Destroy(m);
        return;
        meshStack.Push(m);
    }

}