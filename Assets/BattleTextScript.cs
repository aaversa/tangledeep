using UnityEngine;
using System.Collections;
using TMPro;
using System.Text;
using System;

public enum BattleTextColors { BLUE, TEAL, BROWN, PINK, ORANGE, GREY, GREEN, YELLOW, COUNT }

public class BattleTextScript : MonoBehaviour {

    public RectTransform myRectTransform;
    Transform followTransform;
    GameObject followGameObject;

    public float timeAlive;
    public float animTime;
    private float timeAtAnimStart;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool animating;
    private float scale;
    private GameObject trackObject;
    public bool healing;
    public bool isDamageText;
    public bool hasTMPro;
    private Renderer dtr;
    private TextMeshPro tm;
    private TextMesh myMesh;
    private Color myColor;
    private static Color transparentColor = new Color(1f, 1f, 1f, 0.0f);
    float opacity;
    float percentComplete;
    float timeSinceStarted;
    public string myText;
    static Color empty = new Color(0f, 0f, 0f, 0f);
    public int framesToStart;
    int localFramesToStart;
    private StringBuilder sb;
    public BattleTextColors myBattleColor;

    public bool onSecondBounce;
    public bool doDoubleBounce;

    const float fadeTime = 0.05f;
    const float BOUNCE_HEIGHT = 1.2f;
    const float TRAVEL_DISTANCE = 1.2f;
    const float DEFAULT_FONT_SIZE = 8f;

    public bool useRegularFontForAllCharacters;
    public bool useRegularFontOnlyIfNeeded;

    int floorOfFollowObject = -1;

    bool initializedEver = false;
    //Vector3 basePosition; // Position of the parent object.

    // BLUE, TEAL, BROWN, PINK, 
    // ORANGE, GREY, GREEN, YELLOW
    public readonly int[] battleColorIndices = { 0, 38, 76, 114,
        152, 228, 190, 266 };

    public void SetParent(GameObject followObject)
    {
        followTransform = followObject.transform;
        followGameObject = followObject;
        floorOfFollowObject = MapMasterScript.activeMap.floor;
    }

    void Awake()
    {
        transform.position = new Vector2(-25f, -25f);
    }

    void OnEnable()
    {
        animating = false;
        percentComplete = 0.0f;
        timeAlive = 0f;
        transform.position = new Vector2(-25f, -25f);
    }

	public void Initialize(string txt, GameObject tObject, bool damageText, bool forceUseRegularFont = false)
    {
        doDoubleBounce = false;
        onSecondBounce = false;
        isDamageText = damageText;
        if (!initializedEver)
        {
        dtr = GetComponent<Renderer>();
        }        
        if (damageText || hasTMPro)
        {
            if (!initializedEver)
            {
            tm = GetComponent<TextMeshPro>();
                FontManager.LocalizeMe(tm, TDFonts.WHITE_NO_OUTLINE);
            }            
            tm.color = transparentColor;
        }
        else
        {
            if (!initializedEver)
            {
            myMesh = GetComponent<TextMesh>();
            }            
            myMesh.color = transparentColor;
        }
        
        dtr.sortingLayerName = "Text";
        //scale = 1.0f;
        
        if (!initializedEver)
        {
        sb = new StringBuilder(8);
        }
        myText = txt;

        if (StringManager.gameLanguage != EGameLanguage.en_us && !CustomAlgorithms.CheckIfStringHasOnlyNumbers(myText))
        {
            useRegularFontOnlyIfNeeded = true;
            if (StringManager.gameLanguage == EGameLanguage.de_germany || StringManager.gameLanguage == EGameLanguage.es_spain)
            {
                forceUseRegularFont = true;
            }
        }
        else
        {
            useRegularFontOnlyIfNeeded = false;
        }



        useRegularFontForAllCharacters = forceUseRegularFont;
        animating = false;
        localFramesToStart = -1;
        if (tObject != null)
        {
            trackObject = tObject;
        }
    }

    public void SetBattleTextColor(Color color)
    {        
        if (color == Color.yellow)
        {
            myBattleColor = BattleTextColors.YELLOW;
        }
        else if (color == Color.red)
        {
            myBattleColor = BattleTextColors.PINK;
        }
        else if (color == Color.white)
        {
            myBattleColor = BattleTextColors.ORANGE;
        }
        else if (color == Color.green)
        {
            myBattleColor = BattleTextColors.GREEN;
        }
        else if (color == Color.gray)
        {
            myBattleColor = BattleTextColors.GREY;
        }
    }

    public void SetStartPosition(Vector2 pos)
    {
        startPosition = pos;
        //startPosition = pos;
        //Debug.Log("Start position set to " + startPosition);
    }

    public void SetEndPosition(Vector3 pos)
    {
        float localTravelDistance = TRAVEL_DISTANCE;
        if (doDoubleBounce)
        {
            localTravelDistance *= 1.5f;
        }
        if (UnityEngine.Random.Range(0,2) == 0)
        {
            endPosition.x = startPosition.x + localTravelDistance;
        }
        else
        {
            endPosition.x = startPosition.x - localTravelDistance;
        }

        if (!doDoubleBounce)
        {
            endPosition.y = startPosition.y + 0.7f;
        }
        else
        {
            endPosition.y = startPosition.y;
            // Hacky place to put this
            animTime *= 0.55f;
        }
        
        endPosition.z = startPosition.z;
    }

    public void SetColor(Color color)
    {
        myColor = color;
    }

    public void UpdateColor(Color color)
    {
        if (isDamageText || hasTMPro)
        {
            if (tm == null)
            {
                tm = GetComponent<TextMeshPro>();
            }
            tm.color = color;
        }
        else if (!isDamageText)
        {
            if (myMesh == null)
            {
                myMesh = GetComponent<TextMesh>();
            }
            myMesh.color = color;
        }        
    }

    public void SetScale(float newScale)
    {
        scale = newScale;
        scale *= (PlayerOptions.battleTextScale / 100f);
    }

    // Update is called once per frame
    public void SetAnimTime(float time)
    {
        timeAlive = 0;
        animTime = time;
        // TEMP
        //animTime *= 20f;
    }

    void Update ()
    {
        

        transform.rotation = Quaternion.identity;
        if (framesToStart > 0 && localFramesToStart != 0)
        {
            localFramesToStart--;
            return;
        }
        else if ((framesToStart == 0 || localFramesToStart == 0) && !animating)
        {
            animating = true;

            if (scale != 0f)
            {
                transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                transform.localScale = Vector3.one;
            }
            
            UpdateColor(UIManagerScript.transparentColor);
            animating = true;
            timeAlive = 0.0f;
            timeAtAnimStart = Time.time;
            //tm.text = myText;

            if (tm != null)
            {
                AdjustFontSizeByLanguage();                
            }

            if (myMesh != null)
            {
                myMesh.text = myText;
                return;
            }

            char[] chars = myText.ToCharArray();

            if (!isDamageText || true)
            {

                int index = 0;
                // Light blue, lighter blue, orange, pink. Multiply by 27
                for (int i = 0; i < chars.Length; i++)
                {
                    if (char.IsWhiteSpace(chars[i]))
                    {
                        sb.Append(" ");
                        continue;
                    }

                    int outIndex;

                    if (useRegularFontForAllCharacters) // Text that probably has special characters outside the pixel font.
                    {
                        sb.Append(chars[i]);
                    }
                    else
                    {
                        if (BattleTextManager.dictSpriteTextIndices.TryGetValue(chars[i], out outIndex))
                        {
                            index = outIndex + battleColorIndices[(int)myBattleColor];
                            sb.Append("<sprite=" + index + ">");
                        }
                        else
                        {
                            if (useRegularFontOnlyIfNeeded)
                            {
                                sb.Append(chars[i]);
                            }
                            //Debug.Log("Couldn't find " + chars[i] + " character in sprite index. Text: " + myText);
                        }
                    }
                  
                }
            }
            else
            {
                if (!healing)
                {
                    if (trackObject == GameMasterScript.heroPC)
                    {
                        for (int i = 0; i < chars.Length; i++)
                        {
                            switch (chars[i])
                            {
                                case '0':
                                    sb.Append("<sprite=0>");
                                    break;
                                case '1':
                                    sb.Append("<sprite=1>");
                                    break;
                                case '2':
                                    sb.Append("<sprite=2>");
                                    break;
                                case '3':
                                    sb.Append("<sprite=3>");
                                    break;
                                case '4':
                                    sb.Append("<sprite=4>");
                                    break;
                                case '5':
                                    sb.Append("<sprite=5>");
                                    break;
                                case '6':
                                    sb.Append("<sprite=6>");
                                    break;
                                case '7':
                                    sb.Append("<sprite=7>");
                                    break;
                                case '8':
                                    sb.Append("<sprite=8>");
                                    break;
                                case '9':
                                    sb.Append("<sprite=9>");
                                    break;
                            }
                        }
                    }
                    else {
                        for (int i = 0; i < chars.Length; i++)
                        {
                            switch (chars[i])
                            {
                                case '0':
                                    sb.Append("<sprite=20>");
                                    break;
                                case '1':
                                    sb.Append("<sprite=21>");
                                    break;
                                case '2':
                                    sb.Append("<sprite=22>");
                                    break;
                                case '3':
                                    sb.Append("<sprite=23>");
                                    break;
                                case '4':
                                    sb.Append("<sprite=24>");
                                    break;
                                case '5':
                                    sb.Append("<sprite=25>");
                                    break;
                                case '6':
                                    sb.Append("<sprite=26>");
                                    break;
                                case '7':
                                    sb.Append("<sprite=27>");
                                    break;
                                case '8':
                                    sb.Append("<sprite=28>");
                                    break;
                                case '9':
                                    sb.Append("<sprite=29>");
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    // HEALING
                    for (int i = 0; i < chars.Length; i++)
                    {
                        switch (chars[i])
                        {
                            case '0':
                                sb.Append("<sprite=10>");
                                break;
                            case '1':
                                sb.Append("<sprite=11>");
                                break;
                            case '2':
                                sb.Append("<sprite=12>");
                                break;
                            case '3':
                                sb.Append("<sprite=13>");
                                break;
                            case '4':
                                sb.Append("<sprite=14>");
                                break;
                            case '5':
                                sb.Append("<sprite=15>");
                                break;
                            case '6':
                                sb.Append("<sprite=16>");
                                break;
                            case '7':
                                sb.Append("<sprite=17>");
                                break;
                            case '8':
                                sb.Append("<sprite=18>");
                                break;
                            case '9':
                                sb.Append("<sprite=19>");
                                break;
                        }
                    }
                }
            }


            tm.text = sb.ToString();

            return;
            
        }
        if (!animating)
        {
            return;
        }

        timeAlive += Time.deltaTime;
        timeSinceStarted = Time.time - timeAtAnimStart;
        percentComplete = timeSinceStarted / animTime;

        Vector3 calcPos = Vector3.zero;
        calcPos = Vector3.Slerp(startPosition, endPosition, percentComplete);

        if (!doDoubleBounce)
        {
            calcPos = Vector3.Slerp(startPosition, endPosition, percentComplete);
            opacity = Mathf.Lerp(1.0f, 0.0f, percentComplete);

            if (percentComplete <= 0.03f)
            {
                opacity = 0f;
            }
        }
        else
        {
            float calcX = Mathfx.Lerp(startPosition.x, endPosition.x, percentComplete);
            float calcY = startPosition.y + Mathf.Sin((float)Math.PI * percentComplete) * BOUNCE_HEIGHT;

            calcPos = new Vector3(calcX, calcY, 0);

            if (onSecondBounce)
            {
                opacity = Mathf.Lerp(1.0f, 0.0f, percentComplete);
            }
            else {
                opacity = 1f;
            }            
        }

        myColor.a = opacity;
        UpdateColor(myColor);
        
        if (floorOfFollowObject != MapMasterScript.activeMap.floor)
        {
            animating = false;
            BattleTextManager.ReturnToPool(gameObject, isDamageText);
            return;
        }

        if (MysteryDungeonManager.exitingMysteryDungeon && 
            (followTransform == null || followTransform.gameObject == null || !followTransform.gameObject.activeSelf || gameObject == null || !gameObject.activeSelf))
        {
            animating = false;
            BattleTextManager.ReturnToPool(gameObject, isDamageText);
            return;
        }

        //Debug.Log(followGameObject.name + " " + floorOfFollowObject + " " + MapMasterScript.activeMap.floor);
        calcPos += followTransform.position; // Our virtual "follow" here.
        transform.localPosition = calcPos;

        if (percentComplete >= 1.0f)
        {
            if (!doDoubleBounce || onSecondBounce)
            {
                BattleTextManager.ReturnToPool(gameObject, isDamageText);
            }
            else
            {
                onSecondBounce = true;
                timeAtAnimStart = Time.time;
                Vector2 dist = endPosition - startPosition;
                startPosition = endPosition;
                endPosition = new Vector2(endPosition.x + (dist.x), endPosition.y);
            }
            
        }
    }

    public void StartAnim()
    {
        localFramesToStart = framesToStart;
    }

    void AdjustFontSizeByLanguage()
    {
        switch(StringManager.gameLanguage)
        {
            case EGameLanguage.en_us:
            default:
                tm.fontSize = DEFAULT_FONT_SIZE;
                break;
            case EGameLanguage.jp_japan:
            case EGameLanguage.zh_cn:            
                tm.fontSize = DEFAULT_FONT_SIZE / 2;
                break;
            case EGameLanguage.de_germany:
                tm.fontSize = (int)(DEFAULT_FONT_SIZE * 0.75f);
                break;
        }
    }
}
