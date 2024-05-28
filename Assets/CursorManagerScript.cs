using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CursorSpriteTypes { NORMAL, TARGET, EXAMINE, ATTACK, RANGED, TALK, COUNT }

public class CursorManagerScript : MonoBehaviour {

    public Texture universalCursor;
    public Texture targetCursor;
    public Texture attackCursor;
    public Texture examineCursor;
    public Texture rangedCursor;
    public Texture talkCursor;

    static Texture2D[] cursorTextures;

    static CursorSpriteTypes currentType;

	// Use this for initialization
	void Start () {
        cursorTextures = new Texture2D[(int)CursorSpriteTypes.COUNT];

if (!PlatformVariables.GAMEPAD_ONLY)
{
        for (int i = 0; i < cursorTextures.Length; i++)
        {
            cursorTextures[i] = new Texture2D(32, 32);
        }
}

        cursorTextures[(int)CursorSpriteTypes.NORMAL] = universalCursor as Texture2D;
        cursorTextures[(int)CursorSpriteTypes.ATTACK] = attackCursor as Texture2D;
        cursorTextures[(int)CursorSpriteTypes.RANGED] = rangedCursor as Texture2D;
        cursorTextures[(int)CursorSpriteTypes.TALK] = talkCursor as Texture2D;
        cursorTextures[(int)CursorSpriteTypes.TARGET] = targetCursor as Texture2D;
        cursorTextures[(int)CursorSpriteTypes.EXAMINE] = examineCursor as Texture2D;

    }

    public static CursorSpriteTypes GetCursorType()
    {
        return currentType;
    }

    public static void ChangeCursorSprite(CursorSpriteTypes cType)
    {
        if (UIManagerScript.AnyInteractableWindowOpen() || UIManagerScript.singletonUIMS.GetCurrentFullScreenUI() != null)
        {
            cType = CursorSpriteTypes.NORMAL;
        }
        else if (UIManagerScript.singletonUIMS.CheckTargeting()) // Always use the wand if we are targeting stuff.
        {
            cType = CursorSpriteTypes.TARGET;
        }
        CursorMode mode = CursorMode.Auto;
        Vector2 hotspot = Vector2.zero;
        Cursor.SetCursor(cursorTextures[(int)cType], hotspot, mode);
        currentType = cType;
        if (PlatformVariables.GAMEPAD_ONLY)
        {
                Cursor.visible = false;
        }
    }

    public static void RevertCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        currentType = CursorSpriteTypes.COUNT;
    }
}

