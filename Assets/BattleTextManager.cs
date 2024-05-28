using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;

public enum NumberTextTypes { RED, WHITE, GREEN, COUNT }
public enum BounceTypes { STANDARD, DOUBLE, COUNT }

public class BattleTextData
{
    public string text;
    public Vector3 pos;
    public GameObject btdObj;
    public Color color;
    public float sizeMod;
    public float lengthMod;

    public BattleTextData(string theText, GameObject obj, Color theColor, bool crit)
    {
        text = theText;
        //pos = position;
        btdObj = obj;
        color = theColor;
        sizeMod = 1.0f;
        lengthMod = 0.0f;
        if (crit)
        {
            lengthMod += 0.33f;
            sizeMod = 2.0f;
        }        
    }
}

public class BattleTextObjectTracker
{
    public GameObject obj;
    public float timeOfLastStart;
    public Queue<BattleTextScript> textsReady;
    bool processingQueue = false;

    public BattleTextObjectTracker()
    {
        textsReady = new Queue<BattleTextScript>();
        timeOfLastStart = 0.0f;
    }

    public void AddAnim(BattleTextScript bts)
    {
        textsReady.Enqueue(bts);

        if (!processingQueue)
        {
            ProcessQueue();
        }
        
    }

    public void ProcessQueue()
    {
        if (obj == null || !obj.activeSelf)
        {
            BattleTextManager.RemoveTracker(this);
            //Debug.Log("No object for this tracker");
            return;
        }
        processingQueue = true;
        if (textsReady.Count == 0)
        {
            processingQueue = false;
            return;
        }

        float tDiff = Time.fixedTime - timeOfLastStart;
        if (tDiff > BattleTextManager.baseTextDelay)
        {
            textsReady.Dequeue().StartAnim();
            timeOfLastStart = Time.fixedTime;
            BattleTextManager.WaitThenPlayForMe(this, BattleTextManager.baseTextDelay);
            //StartCoroutine(WaitThenPlayNextAnim(0.33f));
        }
        else
        {
            BattleTextManager.WaitThenPlayForMe(this, tDiff + 0.01f);
            //StartCoroutine(WaitThenPlayNextAnim(tDiff + 0.01f));
        }
    }

}

public class BattleTextManager : MonoBehaviour {

    public float animationDistance;
    public float animationTime;
    public float strHeightOffset;
    public static float animDistance;// = 1.05f;
    public static float startHeightOffset;// = 1.05f;
    public static float animTime;// = 0.7f;
    public static Color playerDamageColor;
    public static Color skillDamageColor;
    public static Color genericDamageColor;
    public static BattleTextManager btmSingleton;
    public static float baseTextDelay = 0.17f;

    static bool initialized = false;

    //public static Queue[,] allTextAreas;

    public static Dictionary<GameObject, BattleTextObjectTracker> activeGameObjectsForText;
    public static Dictionary<char, int> dictSpriteTextIndices;

    public static Stack<GameObject> damageTextPool;
    public static Stack<GameObject> otherTextPool;

    // Use this for initialization
    void Start () {
        //InitializeBTM();
	}

    void Update()
    {
        if (!initialized && GameMasterScript.initialGameAwakeComplete && GameMasterScript.allResourcesLoaded)
        {
            InitializeBTM();
        }
    }

    public static void ResetAllVariablesToGameLoad()
    {
        initialized = false;
        btmSingleton = null;
        activeGameObjectsForText.Clear();
    }

    public static void DeInitialize()
    {
        initialized = false;
    }

    public void InitializeBTM()
    {
        if (initialized) return;
        if (btmSingleton == null)
        {
            btmSingleton = this;
        }
        animDistance = animationDistance;
        animTime = animationTime;
        startHeightOffset = strHeightOffset;
        //playerDamageColor = new Color(1f, 0.9f, 0f);
        playerDamageColor = Color.yellow;
        //genericDamageColor = new Color(1f, 0.95f, 0f);
        genericDamageColor = Color.grey;

        skillDamageColor = Color.white;

        activeGameObjectsForText = new Dictionary<GameObject, BattleTextObjectTracker>();
        
        dictSpriteTextIndices = new Dictionary<char, int>();
        dictSpriteTextIndices.Add('a', 0);
        dictSpriteTextIndices.Add('A', 0);
        dictSpriteTextIndices.Add('b', 1);
        dictSpriteTextIndices.Add('B', 1);
        dictSpriteTextIndices.Add('c', 2);
        dictSpriteTextIndices.Add('C', 2);
        dictSpriteTextIndices.Add('d', 3);
        dictSpriteTextIndices.Add('D', 3);
        dictSpriteTextIndices.Add('e', 4);
        dictSpriteTextIndices.Add('E', 4);
        dictSpriteTextIndices.Add('f', 5);
        dictSpriteTextIndices.Add('F', 5);
        dictSpriteTextIndices.Add('g', 6);
        dictSpriteTextIndices.Add('G', 6);
        dictSpriteTextIndices.Add('h', 7);
        dictSpriteTextIndices.Add('H', 7);
        dictSpriteTextIndices.Add('i', 8);
        dictSpriteTextIndices.Add('I', 8);
        dictSpriteTextIndices.Add('j', 9);
        dictSpriteTextIndices.Add('J', 9);
        dictSpriteTextIndices.Add('k', 10);
        dictSpriteTextIndices.Add('K', 10);
        dictSpriteTextIndices.Add('l', 11);
        dictSpriteTextIndices.Add('L', 11);
        dictSpriteTextIndices.Add('m', 12);
        dictSpriteTextIndices.Add('M', 12);
        dictSpriteTextIndices.Add('n', 13);
        dictSpriteTextIndices.Add('N', 13);
        dictSpriteTextIndices.Add('o', 14);
        dictSpriteTextIndices.Add('O', 14);
        dictSpriteTextIndices.Add('p', 15);
        dictSpriteTextIndices.Add('P', 15);
        dictSpriteTextIndices.Add('q', 16);
        dictSpriteTextIndices.Add('Q', 16);
        dictSpriteTextIndices.Add('r', 17);
        dictSpriteTextIndices.Add('R', 17);
        dictSpriteTextIndices.Add('s', 18);
        dictSpriteTextIndices.Add('S', 18);
        dictSpriteTextIndices.Add('t', 19);
        dictSpriteTextIndices.Add('T', 19);
        dictSpriteTextIndices.Add('u', 20);
        dictSpriteTextIndices.Add('U', 20);
        dictSpriteTextIndices.Add('v', 21);
        dictSpriteTextIndices.Add('V', 21);
        dictSpriteTextIndices.Add('w', 22);
        dictSpriteTextIndices.Add('W', 22);
        dictSpriteTextIndices.Add('x', 23);
        dictSpriteTextIndices.Add('X', 23);
        dictSpriteTextIndices.Add('y', 24);
        dictSpriteTextIndices.Add('Y', 24);
        dictSpriteTextIndices.Add('z', 25);
        dictSpriteTextIndices.Add('Z', 25);
        dictSpriteTextIndices.Add('!', 26);
        dictSpriteTextIndices.Add('%', 27);
		dictSpriteTextIndices.Add('0', 28);
		dictSpriteTextIndices.Add('1', 29);
		dictSpriteTextIndices.Add('2', 30);
		dictSpriteTextIndices.Add('3', 31);
		dictSpriteTextIndices.Add('4', 32);
		dictSpriteTextIndices.Add('5', 33);
		dictSpriteTextIndices.Add('6', 34);
		dictSpriteTextIndices.Add('7', 35);
		dictSpriteTextIndices.Add('8', 36);
		dictSpriteTextIndices.Add('9', 37);

        damageTextPool = new Stack<GameObject>();
        otherTextPool = new Stack<GameObject>();

        for (int i = 0; i < 20; i++)
        {
            GameObject go = Instantiate(GameMasterScript.GetResourceByRef("DamageText"));
            damageTextPool.Push(go);
            go.SetActive(false);            
        }

        for (int i = 0; i < 50; i++)
        {
            GameObject go = Instantiate(GameMasterScript.GetResourceByRef("OtherSpriteText"));
            otherTextPool.Push(go);
            go.SetActive(false);
        }

        initialized = true;
    }

    public static void SetAnimTime(float time)
    {
        animTime = time;
    }

    public static void ReturnToPool(GameObject go, bool damageText)
    {
        if (!initialized)
        {
            btmSingleton.InitializeBTM();
        }
        if (go == null)
        {
            return;
        }
        go.SetActive(false);
        go.transform.SetParent(null);        
        if (damageText)
        {
            damageTextPool.Push(go);            
        }
        else
        {
            otherTextPool.Push(go);
            //Debug.Log("Returning other text to pool. Number of objects is now " + otherTextPool.Count);
        }
        go.GetComponent<TextMeshPro>().text = "";

    }

    public static GameObject GetTextFromPool(bool damageText)
    {
        var stack = damageText ? damageTextPool : otherTextPool;
        string strComponent = damageText ? "DamageText" : "OtherSpriteText";

        //grab one from the pool if we have one
        if (stack.Count > 0)
            {
            GameObject pop = stack.Pop();
			pop.transform.localScale = Vector3.one;
                pop.SetActive(true);
                return pop;
            }

        //spawn new text because we blew out the pool with our awesome
        GameObject go = Instantiate(GameMasterScript.GetResourceByRef(strComponent));
                go.transform.localScale = Vector3.one;
                return go;

    }

    IEnumerator WaitThenPlayNextAnim(BattleTextObjectTracker btot, float time)
    {
        yield return new WaitForSeconds(time);
        btot.ProcessQueue();
    }

    public static void RemoveTracker(BattleTextObjectTracker btot)
    {
        activeGameObjectsForText.Remove(btot.obj);
    }

    public static void WaitThenPlayForMe(BattleTextObjectTracker btot, float time)
    {
        if (btot == null) return;
        if (btmSingleton == null)
        {
            return;
        }
        btmSingleton.SingletonWaitThenPlayForMe(btot, time);
    }

    public void SingletonWaitThenPlayForMe(BattleTextObjectTracker btot, float time)
    {
        if (btot == null) return;
        StartCoroutine(WaitThenPlayNextAnim(btot, time));
    }

    public static void WaitThenNewTextOnObject(float waitTime, string txt, GameObject go, Color c, float addWaitTime)
    {
        btmSingleton.StartCoroutine(_WaitThenNewTextOnObject(waitTime, txt, go, c, addWaitTime));
    }

    static IEnumerator _WaitThenNewTextOnObject(float waitTime, string txt, GameObject go, Color c, float addWaitTime)
    {
        yield return new WaitForSeconds(waitTime);
        if (go == null || !go.activeSelf) yield break;

        NewText(txt, go, c, addWaitTime);
    }

    public static void InitializeTextObjectSources()
    {
        activeGameObjectsForText.Clear();

        // Below is handled at GO spawn in MMS.
    
        /* for (int x = 0; x < MapMasterScript.activeMap.actorsInMap.Count; x++)
        {
            BattleTextObjectTracker newTracker = new BattleTextObjectTracker();
            newTracker.obj = MapMasterScript.activeMap.actorsInMap[x].GetObject();
            activeGameObjectsForText.Add(MapMasterScript.activeMap.actorsInMap[x].GetObject(), newTracker);
        } */
    }

    public static void AddObjectToDict(GameObject go)
    {
        BattleTextObjectTracker tryGet;
        if (!activeGameObjectsForText.TryGetValue(go, out tryGet))
        {
            tryGet = new BattleTextObjectTracker();
            tryGet.obj = go;
            activeGameObjectsForText.Add(go, tryGet);
        }

    }

    public void NewText(BattleTextData btd)
    {
        NewText(btd.text, btd.btdObj, btd.color, btd.lengthMod, btd.sizeMod);
    }

    public void NewBattleText(BattleTextData btd)
    {
        NewDamageText(Int32.Parse(btd.text), false, btd.color, btd.btdObj, btd.lengthMod, btd.sizeMod);
    }

    IEnumerator WaitThenNewText(BattleTextData btd, float time)
    {
        yield return new WaitForSeconds(time);
        NewText(btd);
    }

    public void WaitThenPlayText(BattleTextData btd, float time)
    {
        StartCoroutine(WaitThenNewText(btd, time));
    }

    public void WaitThenPlayDamageText(BattleTextData btd, float time)
    {
        StartCoroutine(WaitThenNewDamageText(btd, time));
    }

    IEnumerator WaitThenNewDamageText(BattleTextData btd, float time)
    {
        yield return new WaitForSeconds(time);
        NewDamageText(Int32.Parse(btd.text),false, btd.color, btd.btdObj, btd.lengthMod, btd.sizeMod);
    }



    public static void NewDamageText(int number, bool healing, Color textColor, GameObject obj, float addLength, float scale, BounceTypes bounce = BounceTypes.STANDARD)
    {
        NewText(number.ToString(), obj, textColor, addLength, scale, bounce);
        return;
    }

    public static void NewText(string txt, GameObject obj, Color color, float addLength, float scale, BounceTypes bt = BounceTypes.STANDARD, bool forceUseRegularFont = false) // 2nd WAS Vector3 pos before.
    {
    	if (!GameMasterScript.actualGameStarted) {
    		// Don't generate this text before the game starts, for ANY reason.
    		return;
    	}
        if (obj == null || !obj.activeSelf)
        {
            //Debug.Log("Object is dead/null for text " + txt);
            return;
        }

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        if (sr == null || !sr.enabled)
        {
            return;
        }

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            sr.material = GameMasterScript.spriteMaterialUnlit;
        }

        BattleTextObjectTracker tryGet;
        if (activeGameObjectsForText.TryGetValue(obj, out tryGet))
        {
            GameObject damageText = GetTextFromPool(false);
            
            BattleTextScript bts = damageText.GetComponent<BattleTextScript>();
            bts.Initialize(txt, obj, false, forceUseRegularFont);

            if (bt == BounceTypes.DOUBLE)
            {
                bts.doDoubleBounce = true;
            }                        

            bts.SetAnimTime(animTime + addLength);
            bts.SetColor(color);

            bts.myBattleColor = BattleTextColors.TEAL;
            bts.SetBattleTextColor(color);

            bts.SetScale(scale);            
            bts.SetParent(obj);

            damageText.transform.localPosition = Vector3.zero;
            Vector3 position = Vector3.zero;

            position.y += startHeightOffset; // Start height offset for all combat text
            bts.SetStartPosition(position);
            position.y += animDistance;
            bts.SetEndPosition(position);

            tryGet.AddAnim(bts);
        }
        else
        {
            Debug.Log("Object is not in BTM dictionary. " + obj.name);
        }
    }

    public static void NewText(string txt, GameObject obj, Color color, float addLength)
    {
        NewText(txt, obj, color, addLength, 1.0f);
    }
}
