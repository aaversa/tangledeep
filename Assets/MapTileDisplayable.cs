using UnityEngine;
using System.Collections;

public class MapTileDisplayable : MonoBehaviour {

    public bool alive;
    private Vector2 truePosition;
    private Vector2 drawPosition;
    private Vector2 gridPosition;
    public Vector2 posInArray;
    public bool visible;
    public bool blocking;
    public VisualTileType vttype;
    public TileTypes internalTileType;

    public bool childTileVisible;
    public bool childTileDisplayRadius;
    public bool childTileExplored;

    // Use this for initialization
    void Start () {
	
	}

    void Awake ()
    {
        posInArray = new Vector2();
    }

    public void SetPosInArray(int x, int y)
    {
        posInArray = new Vector2(x, y);
    }

    public void SetBlocking(bool block)
    {
        blocking = block;
    }

    public bool GetBlocking()
    {
        return blocking;
    }
	
    public void SetGridPosition(Vector2 pos)
    {
        gridPosition.x = pos.x;
        gridPosition.y = pos.y;
    }

    public Vector2 GetGridPosition()
    {
        return gridPosition;
    }

    public bool GetVisible()
    {
        return visible;
    }

    public void SetVisible(bool visible)
    {
        this.visible = visible;
    }

    public bool IsAlive()
    {
        return alive;
    }

    public void SetAlive(bool alive)
    {
        this.alive = alive;
    }

    public void SetDrawPosition(Vector2 pos)
    {
        drawPosition = pos;
    }

    public Vector2 GetDrawPosition()
    {
        return drawPosition;
    }

    public void SetTruePosition(Vector2 pos)
    {
        truePosition = pos;
    }

    public Vector2 GetTruePosition()
    {
        return truePosition;
    }
}
