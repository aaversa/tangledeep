using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class SpriteFontManager : MonoBehaviour {

    public TMP_SpriteAsset[] spriteFontAssets;

    Dictionary<string, TMP_SpriteAsset> dictStringsToAssets;

    static SpriteFontManager singleton;

    // Use this for initialization
    void Awake ()
    {
        if (singleton != null && singleton != this)
        {
            return;
        }
        dictStringsToAssets = new Dictionary<string, TMP_SpriteAsset>();
        for (int i = 0; i < spriteFontAssets.Length; i++)
        {
            dictStringsToAssets.Add(spriteFontAssets[i].name.ToLowerInvariant(), spriteFontAssets[i]);
        }
        DontDestroyOnLoad(this);

        singleton = this;
	}
	
    // Takes a DialogBoxScript and sets its TMPro text object's Sprite Font to the desired asset
    // Assets are assigned to the SpriteFontManager object in the editor
    
    // Possible assets and their sprites (0-index) are:    
    // "HUDIcons" (Default): Timer, health, lightning bolt, Lv, boot/speed, JP, XP, gold coin
    // "type_icons" = Sword, fire, poison/acid, water, lightning, skull/shadow
    // "RingMenuSpritesheet" = flask, portal, frog, clock, nothing, bomb, bread, arrow, nothing, nothing
    public static void SetSpriteFontForDialogBox(DialogBoxScript dbs, string spriteFontAssetName)
    {
        if (singleton == null) return;
        singleton._SetSpriteFontForDialogBox(dbs, spriteFontAssetName);
    }

    void _SetSpriteFontForDialogBox(DialogBoxScript dbs, string spriteFontAssetName)
    {
        TMP_SpriteAsset retrievedAsset;
        if (dictStringsToAssets.TryGetValue(spriteFontAssetName.ToLowerInvariant(), out retrievedAsset))
        {
            dbs.GetDialogText().spriteAsset = retrievedAsset;
        }
        else
        {
            Debug.LogError("WARNING! Sprite font asset " + spriteFontAssetName + " does not exist! Did you add it to the SpriteFontManager object in the editor?");
        }
    }
}
