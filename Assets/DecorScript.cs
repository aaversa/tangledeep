using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class DecorScript : MonoBehaviour
{

    private MeshFilter mf;
    private MeshRenderer mr;
    private MeshCollider mc;

    public Texture2D decorTiles;
    public int tileResolution = 32;

    public int size_x;
    public int size_z;

    private float tileSize = 1.0f;
    private Color[] transparentColor;
    Color[] colors;

    void Start()
    {
        mf = gameObject.GetComponent<MeshFilter>();
        mr = gameObject.GetComponent<MeshRenderer>();
        mc = gameObject.GetComponent<MeshCollider>();
        mr.sortingLayerName = "Actor";
        mr.sortingOrder = 0;
        transparentColor = new Color[tileResolution * tileResolution];
        for (int p = 0; p < (tileResolution*tileResolution); p++)
        {
            transparentColor[p] = Color.clear;
        }
    }
    Color[][] ChopUpTiles()
    {
        int columns = decorTiles.width / tileResolution;
        int rows = decorTiles.height / tileResolution;
        Color[][] tiles = new Color[columns * rows][];

        // Y 0, x 0. Tiles[0] is at 0,0. Then y0, x1. Tile 1 is at 1,0.

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                tiles[((rows - 1 - y) * columns) + x] = decorTiles.GetPixels(x * tileResolution, y * tileResolution, tileResolution, tileResolution);
            }
        }

        return tiles;
    }

    public void BuildTexture()
    {
        //terrainSprites[] = Resources.LoadAll<Sprite>(terrainTiles);
        int texWidth = size_x * tileResolution;
        int texHeight = size_z * tileResolution;

        Texture2D texture = new Texture2D(texWidth, texHeight);

        Color[][] tiles = ChopUpTiles();

        for (int y = 0; y < size_z; y++)
        {
            for (int x = 0; x < size_x; x++)
            {
                Color[] p = transparentColor;
                MapTileData mtd = MapMasterScript.GetTile(new Vector2(x, y));
                if (mtd.CheckTag(LocationTags.HASDECOR))
                {
                    p = tiles[mtd.indexOfDecorSpriteInAtlas];
                }                
                texture.SetPixels(x * tileResolution, y * tileResolution, tileResolution, tileResolution, p);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        mf = gameObject.GetComponent<MeshFilter>();
        mr = gameObject.GetComponent<MeshRenderer>();
        mc = gameObject.GetComponent<MeshCollider>();

        if (mr.sharedMaterials[0].mainTexture != null)
        {
            Destroy(mr.sharedMaterials[0].mainTexture);
        }

        mr.sharedMaterials[0].mainTexture = texture;
    }

    public void BuildMesh(int rows, int columns)
    {
        int numTiles = size_x * size_z;
        int numTris = numTiles * 2;

        int vsize_x = size_x + 1;
        int vsize_z = size_z + 1;
        int numVerts = vsize_x * vsize_z;

        // Generate the mesh data
        Vector3[] vertices = new Vector3[numVerts];
        Vector3[] normals = new Vector3[numVerts];
        Vector2[] uv = new Vector2[numVerts];

        colors = new Color[vertices.Length];

        int[] triangles = new int[numTris * 3];

        int x, z;
        for (z = 0; z < vsize_z; z++)
        {
            for (x = 0; x < vsize_x; x++)
            {
                vertices[z * vsize_x + x] = new Vector3(x * tileSize, 0, -z * tileSize);
                normals[z * vsize_x + x] = Vector3.up;
                uv[z * vsize_x + x] = new Vector2((float)x / size_x, 1f - (float)z / size_z);
            }
        }
        //Debug.Log ("Done Verts!");

        for (z = 0; z < size_z; z++)
        {
            for (x = 0; x < size_x; x++)
            {
                int squareIndex = z * size_x + x;
                int triOffset = squareIndex * 6;
                triangles[triOffset + 0] = z * vsize_x + x + 0;
                triangles[triOffset + 2] = z * vsize_x + x + vsize_x + 0;
                triangles[triOffset + 1] = z * vsize_x + x + vsize_x + 1;

                triangles[triOffset + 3] = z * vsize_x + x + 0;
                triangles[triOffset + 5] = z * vsize_x + x + vsize_x + 1;
                triangles[triOffset + 4] = z * vsize_x + x + 1;
            }
        }

        //Debug.Log ("Done Triangles!");

        // Create a new Mesh and populate with the data

        Mesh mesh = new Mesh();
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
            Destroy(mf.mesh);
        }

        mf.mesh = mesh;
        mc.sharedMesh = mesh;
        //Debug.Log ("Done Mesh!");

        BuildTexture();
        transform.position = new Vector3(-0.5f, size_z + -0.5f, transform.position.z);
        //UpdateColors();
    
    }

    /*public void UpdateColors()
    {
        Vector2 checkVector = new Vector2(0, 0);
        Debug.Log(mapColumns + " " + mapRows);
        for (int x = 0; x < mapColumns; x++)
        {
            for (int y = 0; y < mapRows; y++)
            {
                int i = ((mapRows - 1 - y) * mapColumns) + x;
                Debug.Log("index " + i + " vs " + colors.Length);
                Color colorToUse = Color.black;
                checkVector.x = x;
                checkVector.y = y;
                if (MapMasterScript.GetTile(checkVector).decor == null)
                {
                    colorToUse = Color.clear;
                }
                colors[(i * 4)] = colorToUse;
                colors[(i * 4) + 1] = colorToUse;
                colors[(i * 4) + 2] = colorToUse;
                colors[(i * 4) + 3] = colorToUse;
            }
        }
        //Debug.Log(mf.mesh.vertices.Length + " " + colors.Length + " " + vertices.Length);
        mf.mesh.colors = colors;
    } */
}
