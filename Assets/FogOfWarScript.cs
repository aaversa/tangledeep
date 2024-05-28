using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class FogOfWarScript : MonoBehaviour {

    private MeshFilter mf;
    private MeshRenderer mr;
    public bool[] exploredArray;
    public GameObject tileMap;

    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals;
    public Vector2[] uv;
    //public Color[] colors;

    public Mesh myMesh;

    Vector2[] tiles;

    int totalMeshSize;
    int mapRows;
    int mapColumns;

    Color light1;
    Color light2;
    Color light3;
    Color light4;

    public bool meshBuilt = false;

    public float transparency = 0.0f;
    public bool fogOfWar;

    // Use this for initialization

    void Start () {
        mf = gameObject.GetComponent<MeshFilter>();
        mr = gameObject.GetComponent<MeshRenderer>();
        tileMap = GameObject.Find("TileMap");
        mr.sortingLayerName = "Foreground";
        mr.sortingOrder = 1;
        light1 = new Color(0, 0, 0, 0.5f);
        light2 = new Color(0, 0, 0, 0.35f);
        light3 = new Color(0, 0, 0, 0.22f);
        light4 = new Color(0, 0, 0, 0.0f);
    }

	public void FOWStart (int rows, int columns) {
        //unexploredTiles = new Vector2[x][y];
        Vector3 newPos = gameObject.transform.position;
        //newPos.x = -0.01f; Weird pixel thing? Fixed now?
        gameObject.transform.position = newPos;
        totalMeshSize = rows * columns;
		mapRows = rows;
		mapColumns = columns;
		vertices = new Vector3[totalMeshSize * 4];
		triangles = new int[totalMeshSize * 6];
		normals = new Vector3[totalMeshSize * 4];
		uv = new Vector2[totalMeshSize * 4];
		tiles = new Vector2[rows*columns];

		for (int x = 0; x < mapColumns; x++) {
        	for (int y = 0; y < mapRows; y++) {
				int i = ((mapRows-1-y)*mapColumns)+x;
				tiles[i] = new Vector2(x,y);
        	}
        }

		//colors = new Color[vertices.Length];
        if (myMesh != null)
        {
            //Destroy(myMesh);
            MeshPooler.ReturnMeshToPool(myMesh);
        }
        //myMesh = new Mesh();
        myMesh = MeshPooler.GetMesh();
    }
	private void OnDestroy()
	{
		if (myMesh != null)
		{
            MeshPooler.ReturnMeshToPool(myMesh);
		}
    }

    public void BuildMesh()
    {
        // Generate mesh data


        // Coordinates of the vertices
        float offset = -0.5f;

        for (int i = 0; i < totalMeshSize; i++)
        {
            // Example: Tile is at 0,0.
            int index = i * 4;
            vertices[index] = new Vector3(tiles[i].x + offset, tiles[i].y + offset,0);
            vertices[index+1] = new Vector3(tiles[i].x+1 + offset, tiles[i].y + offset,0);
            vertices[index + 2] = new Vector3(tiles[i].x + offset, tiles[i].y+1 + offset, 0);
            vertices[index + 3] = new Vector3(tiles[i].x+1 + offset, tiles[i].y + 1 + offset, 0);
        }

        // Which points go to which triangles

        for (int i = 0; i < totalMeshSize; i++)
        {
            int triangleIndex = i * 6;
            int vertexIndex = i * 4;
            triangles[triangleIndex] = vertexIndex; // Triangle 0, point 0 matches with vert 0 in the upper left
            triangles[triangleIndex + 1] = vertexIndex + 3; // Triangle 0, point 1 matches with vert 3 in lower right
            triangles[triangleIndex + 2] = vertexIndex + 2; // T0 P2 match P2 in lower left
            triangles[triangleIndex + 3] = vertexIndex; // T1 P3 is point 0
            triangles[triangleIndex + 4] = vertexIndex + 1; // T1 P4 is point 1
            triangles[triangleIndex + 5] = vertexIndex + 3; // T1 P4 is point 3

            // Triangle 2 Point 0 should be vert 2 point 0, which is at index 4
            // Triangle 2 Point 1 should be vert 2 point 3, which is at index 7


        }

        // Direction of the points
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.up;
        }

        for (int i = 0; i < totalMeshSize; i++)
        {
            // Example: Tile is at 0,0.
            int index = i * 4;
            uv[index] = new Vector2(tiles[i].x + offset, tiles[i].y + offset);
            uv[index + 1] = new Vector2(tiles[i].x + 1 + offset, tiles[i].y+offset);
            uv[index + 2] = new Vector2(tiles[i].x + offset, tiles[i].y + 1+offset);
            uv[index + 3] = new Vector2(tiles[i].x + 1 + offset, tiles[i].y + 1+offset);
        }

        // Create mesh and populate with data
        myMesh.Clear();
        myMesh.vertices = vertices;
        myMesh.triangles = triangles;
        myMesh.normals = normals;
        myMesh.uv = uv;

        //Color[] colors = new Color[vertices.Length];

		UpdateColors();

        mf = gameObject.GetComponent<MeshFilter>();
        mr = gameObject.GetComponent<MeshRenderer>();
        //Debug.Log("Setting mesh to MF with vertices length of " + myMesh.vertices.Length);

        if (mf.mesh != null)
        {
            MeshPooler.ReturnMeshToPool(mf.mesh);
            //Destroy(mf.mesh);
        }

        mf.mesh = myMesh;
        //mf.mesh.colors = colors;
        //Debug.Log("Post build");
        Color mc = mr.material.color;
        mr.material.color = mc;
        //transform.position = new Vector3(tileMap.transform.position.x, tileMap.transform.position.y, transform.position.z);
		meshBuilt = true;
     }

    public void UpdateColors() {
    	if (!meshBuilt) {
    		return;
    	}
        if (!gameObject.activeSelf)
        {
            return;
        }
        //List<Vector2> playerLightTiles = new List<Vector2>(30);

        Vector2 pos = GameMasterScript.heroPCActor.GetPos();
        //playerLightTiles.Add(pos);
        // Write a better algorithm.
        Vector2 check = new Vector2(0, 0);

        for (int x = -3; x <= 3; x++)
        {
            for (int y = -3; y <= 3; y++)
            {
                check.x = pos.x + x;
                check.y = pos.y + y;
                //playerLightTiles.Add(new Vector2(pos.x + x, pos.y + y));
            }
        }
        Color colorToUse;
        for (int x = 0; x < mapColumns; x++) {
        	for (int y = 0; y < mapRows; y++) {
				int i = ((mapRows-1-y)*mapColumns)+x;
        		colorToUse = Color.red;
        		colorToUse.a = transparency;
                check.x = x;
                check.y = y;
                //dist = Vector2.Distance(pos, check);

                bool view = false;
                if (fogOfWar) {
                	view = GameMasterScript.heroPCActor.visibleTilesArray[x,y];
                	if (MapMasterScript.activeMap.mapArray[x,y].tileType != TileTypes.GROUND) {
                		view = true;
                	}
                }
                else {
                	view = MapMasterScript.activeMap.exploredTiles[x,y];
                    if ((MapMasterScript.activeMap.exploredTiles[x, y]) && (GameMasterScript.heroPCActor.visibleTilesArray[x, y]))
                    {
                        colorToUse = Color.red;
                        colorToUse.a = 0.0f;
                    }
                    else if ((MapMasterScript.activeMap.exploredTiles[x, y]) || (GameMasterScript.heroPCActor.visibleTilesArray[x, y]))
                    {
                        colorToUse = Color.red;
                        colorToUse.a = 0.5f;
                    }
                    else
                    {
                        colorToUse = Color.black;
                    }
                    //colorToUse = light4;
                }
		        mf.mesh.colors[(i * 4)] = colorToUse;
		        mf.mesh.colors[(i * 4) + 1] = colorToUse;
		        mf.mesh.colors[(i * 4) + 2] = colorToUse;
		        mf.mesh.colors[(i * 4) + 3] = colorToUse;
        	}
        }
        //Debug.Log(mf.mesh.vertices.Length + " " + colors.Length + " " + vertices.Length);
		//mf.mesh.colors = colors;    	
    }
}
