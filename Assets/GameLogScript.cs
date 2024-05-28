using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Text;

public enum LogDataTypes { CHANGECORESTAT, GAINSTATUS, LOSEHP, COUNT }
public enum TextDensity { NORMAL, VERBOSE, COUNT }

[System.Serializable]
public partial class GameLogScript : MonoBehaviour {

    public Mask optimizedVerMask;

    public ScrollRect unoptimizedScrollRect;

    public GameObject[] unoptimizedObjects;

    public TextMeshProUGUI optimizedContentTMPro;
    static string[] optimizedLogStrings;
    const int MAX_OPTIMIZED_LOG_STRINGS_LARGE_LOG = 6;
    const int MAX_OPTIMIZED_LOG_STRINGS_SMALL_LOG = 7;
    static StringBuilder optoStringBuilder;

    const int TEXT_LINE_HEIGHT = 28;
    const int LOG_OBJECT_SPACING = 1;
    const int DIVIDER_LINE_HEIGHT = 2;

    public Queue<string> logMessagesToWrite;
    const int FRAMES_BETWEEN_LOG_UPDATES = 0;
    int framesUntilNextUpdate = 0;

    public static readonly byte[] pKey = new byte[]
    {
        201,
        205,
        209,
        220
    };


    public Scrollbar logScrollbar;

    public static Stack<List<LogDataPackage>> combatEventBufferStack;
    public static List<LogDataPackage> activeCombatEventBuffer;

    public static Queue<string> endOfTurnMessageQueue;

    public static int MAX_COMBAT_LOG_LINES = 10;
#if !UNITY_SWITCH
    public const int MAX_JOURNAL_LOG_LINES = 160;
#else
    public const int MAX_JOURNAL_LOG_LINES = 80;
#endif
    static Stack<GameObject> unusedLogTextObjects = new Stack<GameObject>();
    static Stack<GameObject> unusedLogDividers = new Stack<GameObject>();

    static Stack<string> unusedLogStrings = new Stack<string>();
    public static Queue<string> journalLogStringBuffer = new Queue<string>();
    public static Queue<GameObject> combatLogObjectBuffer = new Queue<GameObject>();

    public static Dictionary<GameObject, TextMeshProUGUI> dictCombatLogTextMeshes;
    public static Dictionary<GameObject, LayoutElement> dictCombatLogLayoutElements;
    private static ScrollRect uiLogScrollRect;
    static float timeLastParalyzeMessage;
    static float timeLastRootedMessage;
    static float timeAtLastCombatLogWrite;    
    public GameObject uiGameLog;
    public GameObject uiGameLogContent;

    private static bool forceCombatLogShow = false;
    private static bool combatLogShowState = true;
    private static bool fadingCombatLog = false;
    private static float timeAtCombatLogFadeStart = 0.0f;

    public float delayToFadeCombatLog;
    public float combatLogFadeOutTime;
    public float maxCombatLogAlpha;

    public static GameLogScript singleton;

    private static CanvasGroup combatLogCG;

    static bool bufferingText = false;

    static List<string> turnTextBuffer;

    static int lastTurnOfLogWrite;
    static bool firstDividerAttempt = true;

    public GameLogDynamicCanvasScript gameLogDynamicCanvasComponent;
    public static Dictionary<EGameLanguage, Dictionary<bool, LogSpacingAndFontInfo>> dictLogSpacingInfoByLanguageAndSize;

    public static void Initialize()
    {
        if (PlatformVariables.OPTIMIZED_GAME_LOG)
        {
            for (int i = 0; i < singleton.unoptimizedObjects.Length; i++)
            {
                singleton.unoptimizedObjects[i].gameObject.SetActive(false);
            }
            singleton.unoptimizedScrollRect.enabled = false;
            singleton.optimizedVerMask.enabled = true;
            singleton.optimizedContentTMPro.gameObject.SetActive(true);

            int LOG_LENGTH = 8;

            // No need to do anything with individual objects in Optimized Mode, since we just have ONE TMPRO object.
            unusedLogTextObjects.Clear();
            optimizedLogStrings = new string[LOG_LENGTH];
            for (int i = 0; i < optimizedLogStrings.Length; i++)
            {
                optimizedLogStrings[i] = "";
            }
            FontManager.LocalizeMe(singleton.optimizedContentTMPro, TDFonts.WHITE);
            turnTextBuffer = new List<string>();
            
            //yay
            dictLogSpacingInfoByLanguageAndSize = new Dictionary<EGameLanguage, Dictionary<bool, LogSpacingAndFontInfo>>();

            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.en_us] = new Dictionary<bool, LogSpacingAndFontInfo>();
            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.de_germany] = new Dictionary<bool, LogSpacingAndFontInfo>();
            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.jp_japan] = new Dictionary<bool, LogSpacingAndFontInfo>();
            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.zh_cn] = new Dictionary<bool, LogSpacingAndFontInfo>();
            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.es_spain] = new Dictionary<bool, LogSpacingAndFontInfo>();

            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.en_us][true] = new LogSpacingAndFontInfo(12, 28, 7);
            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.en_us][false] = new LogSpacingAndFontInfo(15, 32, 6);

            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.de_germany][true] = new LogSpacingAndFontInfo(12, 28, 7);
            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.de_germany][false] = new LogSpacingAndFontInfo(15, 32, 6);

            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.es_spain][true] = new LogSpacingAndFontInfo(12, 28, 7);
            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.es_spain][false] = new LogSpacingAndFontInfo(15, 32, 6);

            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.jp_japan][true] = new LogSpacingAndFontInfo(1, 27, 8);
            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.jp_japan][false] = new LogSpacingAndFontInfo(1, 36, 6);

            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.zh_cn][true] = new LogSpacingAndFontInfo(3, 26, 7);
            dictLogSpacingInfoByLanguageAndSize[EGameLanguage.zh_cn][false] = new LogSpacingAndFontInfo(1, 36, 5);

            return;
        }

        // Unoptimized, regular PC game log code here.

        for (int i = 0; i < singleton.unoptimizedObjects.Length; i++)
        {
            singleton.unoptimizedObjects[i].gameObject.SetActive(true);
        }
        singleton.unoptimizedScrollRect.enabled = true;
        singleton.optimizedVerMask.enabled = false;
        singleton.optimizedContentTMPro.gameObject.SetActive(false);

        //Debug.Log("Initializing game log script!");        
        dictCombatLogTextMeshes = new Dictionary<GameObject, TextMeshProUGUI>();
        dictCombatLogLayoutElements = new Dictionary<GameObject, LayoutElement>();
        unusedLogDividers.Clear();
        unusedLogStrings.Clear();
        unusedLogTextObjects.Clear();
        for (int i = 0; i <= MAX_COMBAT_LOG_LINES; i++)
        {
            GameObject logInstance = Instantiate(GameMasterScript.GetResourceByRef("CombatLogText"));
            TextMeshProUGUI myMesh = logInstance.GetComponent<TextMeshProUGUI>();
            FontManager.LocalizeMe(myMesh, TDFonts.WHITE);
            dictCombatLogTextMeshes.Add(logInstance, myMesh);
            dictCombatLogLayoutElements.Add(logInstance, logInstance.GetComponent<LayoutElement>());

            PushLogTextObjectToStack(logInstance);
            logInstance.SetActive(false);

            GameObject dividerInstance = GameMasterScript.TDInstantiate("CombatLogDivider");
            unusedLogDividers.Push(dividerInstance);
            dividerInstance.SetActive(false);
        }

        for (int i = 0; i <= MAX_JOURNAL_LOG_LINES; i++)
        {
            string txt = "";
            unusedLogStrings.Push(txt);
        }

        turnTextBuffer = new List<string>();
        firstDividerAttempt = true;  
    }

    private void Awake()
    {
        logMessagesToWrite = new Queue<string>();
    }

    void Start()
    {
        //Debug.Log(UIManagerScript.orangeHexColor + "Gamelog Initialized on object " + gameObject.name + "</color>");

        singleton = this;

        if (PlatformVariables.OPTIMIZED_GAME_LOG)
        {
            optimizedContentTMPro.text = "";
            optoStringBuilder = new StringBuilder(1024);
        }
        else
        {
            // Don't worry about it, it's fine            

            // These are now set in the inspector of UIManager in gameplay scene, but here's a backup Find in case Jim forgets to set it
            if (uiGameLog == null)
            {
                uiGameLog = GameObject.Find("Game Log");
            }
            if (uiGameLogContent == null)
            {
                uiGameLogContent = GameObject.Find("Game Log Content");
            }
            gameLogDynamicCanvasComponent = uiGameLog.GetComponent<GameLogDynamicCanvasScript>();
            uiLogScrollRect = uiGameLog.GetComponent<ScrollRect>();
        }

        combatLogCG = uiGameLog.GetComponent<CanvasGroup>();
        HideCombatLog();
        
        combatLogObjectBuffer.Clear();

        activeCombatEventBuffer = new List<LogDataPackage>();
        combatEventBufferStack = new Stack<List<LogDataPackage>>();
        endOfTurnMessageQueue = new Queue<string>();
    }

    /// <summary>
    /// Track resolution changes caused by docking/undocking, and make sure the material doesn't get corrupted.
    /// </summary>
    private int cachedScreenWidth;
    private Material[] cachedLogFontMaterials;

    public void UpdateLog()
    {
        forceCombatLogShow = true;

        if (GameMasterScript.applicationQuittingOrChangingScenes) return;

        if (GameStartData.CurrentLoadState == LoadStates.PLAYER_VICTORY  
            || GameStartData.CurrentLoadState == LoadStates.PLAYER_VICTORY_NGPLUS
            || GameStartData.CurrentLoadState == LoadStates.PLAYER_VICTORY_NGPLUSPLUS
            || GameStartData.CurrentLoadState == LoadStates.BACK_TO_TITLE 
            || GameStartData.CurrentLoadState == LoadStates.RESTART_SAME_CHARACTER)
        {
            return;
        }
        
        if (UIManagerScript.singletonUIMS.gameStarted && GameMasterScript.playerDied)
        {
            uiGameLog.SetActive(true);
            combatLogCG.alpha = 1f;
        }
        else
        {
            if (UIManagerScript.singletonUIMS.gameStarted && !forceCombatLogShow && !UIManagerScript.singletonUIMS.IsMouseOverUI())
            {
                if (combatLogShowState && !fadingCombatLog)
                {
                    float diffTime = Time.fixedTime - timeAtLastCombatLogWrite;
                    if (diffTime >= delayToFadeCombatLog)
                    {
                        fadingCombatLog = true;
                        timeAtCombatLogFadeStart = Time.fixedTime;
                    }
                }
                else if (combatLogShowState && fadingCombatLog)
                {
                    float diffTime = Time.fixedTime - timeAtCombatLogFadeStart;
                    float percentComplete = diffTime / combatLogFadeOutTime;
                    combatLogCG.alpha = (1f - percentComplete) * maxCombatLogAlpha;
                    if (combatLogCG.alpha == 0.0f)
                    {
                        uiGameLog.SetActive(false);
                    }
                }
            }
        }

        // Switch resolution can change rapidly in gameplay, so it gets its own switching logic (i.e. dock/undock)
#if UNITY_SWITCH
        if (cachedScreenWidth == 0)
        {
            cachedScreenWidth = Screen.width;
            cachedLogFontMaterials = new Material[ optimizedContentTMPro.fontSharedMaterials.Length];
            for (int t = 0; t < optimizedContentTMPro.fontSharedMaterials.Length; t++)
            {
                cachedLogFontMaterials[t] =  Instantiate(optimizedContentTMPro.fontMaterials[t]);
            }
        }
        else if (cachedScreenWidth != Screen.width)
        {            
            cachedScreenWidth = Screen.width;
            ForceRebuildFontMaterial(setNewMaterials:false);
        }

        ProcessLoggedMessageQueue();
#endif
    }


    /// <summary>
    /// We don't want to write everything every frame. This will write at most one log message this frame via GameLogWrite.
    /// </summary>
    void ProcessLoggedMessageQueue()
    {
        if (framesUntilNextUpdate > 0)
        {
            framesUntilNextUpdate--;
            return;
        }
        if (logMessagesToWrite.Count == 0)
        {
            return;
        }
        string contentToWrite = logMessagesToWrite.Dequeue();
        framesUntilNextUpdate = FRAMES_BETWEEN_LOG_UPDATES;
        GameLogWrite(contentToWrite, null, TextDensity.NORMAL, true);
    }

    public static void UpdateLogTextSize()
    {
        // In optimized mode, we just have ONE TMPRO object that needs font size adjustment. Easy.
        if (PlatformVariables.OPTIMIZED_GAME_LOG)
        {
            int fontSize = 32;
            int lineSpacing = 15;
            if (PlayerOptions.smallLogText)
            {
                fontSize = 28;
                lineSpacing = 12;
            }
            FontManager.AdjustFontSize(singleton.optimizedContentTMPro, fontSize);
            singleton.optimizedContentTMPro.lineSpacing = lineSpacing;
            return;
        }

        foreach(GameObject go in combatLogObjectBuffer)
        {
            // go could be a divider, so verify that it isn't one.
            // Otherwise the divider will not be in the TMPro dictionary.
            if (go.tag != "logdivider")
            {
                AdjustTextSize(dictCombatLogTextMeshes[go]);
            }            
        }
    }

    public static void EnqueueEndOfTurnLogMessage(string newMessage)
    {
        endOfTurnMessageQueue.Enqueue(newMessage);
    }

    public static void PrintEndOfTurnLogMessages()
    {
        while (endOfTurnMessageQueue.Count > 0)
        {
            GameLogWrite(endOfTurnMessageQueue.Dequeue(), null);
        }
    }

    public static void TryScrollLog(float amount)
    {
        if (PlatformVariables.OPTIMIZED_GAME_LOG)
        {
            // No scrolling in optimized mode.
            return;
        }
        if (singleton.logScrollbar == null)
        {
            return;
        }
        float scrollbarValue = singleton.logScrollbar.value;
        scrollbarValue += amount;
        scrollbarValue = Mathf.Clamp(scrollbarValue, 0f, 1f);
        singleton.logScrollbar.value = scrollbarValue;
    }

    public static void DelayedParalyzeMessage(string str, Actor source)
    {
        if (Time.fixedTime - timeLastParalyzeMessage > 0.8f)
        {
            GameLogWrite(str, source);
            timeLastParalyzeMessage = Time.fixedTime;

            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);

            var se = GameMasterScript.heroPCActor.myStats.GetStatusByRef("status_paralyzed");
            if (se == null)
            {
                se = GameMasterScript.heroPCActor.myStats.GetStatusByRef("status_crabgrab");
            }
            if (se == null)
            {
                se = GameMasterScript.heroPCActor.myStats.GetStatusByRef("status_bigfreeze"); 
            }
            if (se != null && MapMasterScript.activeMap != null)
            {
                var a = MapMasterScript.activeMap.FindActorByID(se.addedByActorID);
                if (a != null && a.myMovable != null)
                {
                    a.myMovable.Jab(CombatManagerScript.GetDirection(a, GameMasterScript.heroPCActor));
                }
            }
        }
    }

    public static void LogWriteStringRef(string contentRef, Actor source = null, TextDensity td = TextDensity.NORMAL)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted)
        {
            return;
        }
        if (source == null)
        {
            source = GameMasterScript.heroPCActor;
        }
        GameLogWrite(StringManager.GetString(contentRef), source, td);
    }

    public static void CombatEventWrite(LogDataPackage ldp, TextDensity td = TextDensity.NORMAL)
    {
        if (ldp == null) return;
        if (td == TextDensity.VERBOSE && !PlayerOptions.verboseCombatLog)
        {
            GameLogDataPackages.ReturnToStack(ldp);
            return;
        }
        if (bufferingText && ldp != null)
        {
            bool combined = false;
            foreach(LogDataPackage existingData in activeCombatEventBuffer)
            {
                if (ldp.CompatibleWith(existingData))
                {
                    // Existing data absorbs the new data.
                    existingData.CombineWith(ldp); 
                    combined = true;
                    break;
                }
            }
            if (!combined)
            {
                activeCombatEventBuffer.Add(ldp);
            }
            else
            {
                GameLogDataPackages.ReturnToStack(ldp);
            }
            return;
        }
        else if (!bufferingText)
        {
            string ttd = ldp.GetTextDisplay();
            GameLogWrite(ttd,ldp.gameActor);
            GameLogDataPackages.ReturnToStack(ldp);
        }
        
    }

    static void AdjustTextSize(TextMeshProUGUI txt)
    {
        if (PlatformVariables.OPTIMIZED_GAME_LOG)
        {
            var d = dictLogSpacingInfoByLanguageAndSize[StringManager.gameLanguage][PlayerOptions.smallLogText];
            FontManager.AdjustFontSize(txt, d.iFontSize);
            singleton.optimizedContentTMPro.lineSpacing = d.iLineSpacing;
            return;
        }

        int lineSpacing = 15;
        if (PlayerOptions.smallLogText)
        {
            FontManager.AdjustFontSize(txt, 28);
            lineSpacing = 12;
            if (!PlatformVariables.OPTIMIZED_GAME_LOG) dictCombatLogLayoutElements[txt.gameObject].minHeight = 22;
            else
            {
                singleton.optimizedContentTMPro.lineSpacing = lineSpacing;
            }
        }
        else
        {
#if UNITY_ANDROID || UNITY_IPHONE
            FontManager.AdjustFontSize(txt, 40);
#else
            FontManager.AdjustFontSize(txt, 32);
#endif
            if (!PlatformVariables.OPTIMIZED_GAME_LOG) dictCombatLogLayoutElements[txt.gameObject].minHeight = 28;
            else
            {
                singleton.optimizedContentTMPro.lineSpacing = lineSpacing;
            }
        }
    }

    /// Normally the maximum is the length of the array (7 elsewhere in code) but we may reduce it
    /// if using that maximum creates a string bigger than the box, moving text off screen.
    /// </summary>
    /// <param name="logStrings"></param>
    /// <param name="iMaximumLines"></param>
    static void BuildGameLogContentStringIntoStaticStringBuilder(string[] logStrings, int iMaximumLines)
    {
        // Draws like this:

        // Line 6\n
        // Line 5\n
        // Line 4\n
        // Line 3\n
        // Line 2\n
        // Line 1\n
        // Line 0 (new content)

        // But in LARGE mode, we don't want that top line (6) to draw at all, so skip it

        //Reset our string builder. 
        optoStringBuilder.Length = 0;

        //if we're in large text mode, we always draw one less string.
        int iDeltaFromLength = PlayerOptions.smallLogText ? 1 : 2;

        //count out this many lines
        int iMax = Math.Min(logStrings.Length - iDeltaFromLength, iMaximumLines);


        //start at the top and work down to 0
        for (int i = iMax; i >= 0; i--)
        {
            if (string.IsNullOrEmpty(optimizedLogStrings[i]))
            {
                continue;
            }
            optoStringBuilder.Append(optimizedLogStrings[i]);
            if (i != 0)
            {
                optoStringBuilder.Append("\n");
            }
        }

        //optoStringBuilder should have everything we need.
    }

    public static void GameLogWrite(string content, Actor source, TextDensity td = TextDensity.NORMAL, bool forceWrite = false)
    {
        if (!forceWrite)
        {
        if (!PlayerOptions.verboseCombatLog && td == TextDensity.VERBOSE) return;

        if (source != null && source != GameMasterScript.heroPCActor)
        {
            if (!MapMasterScript.InBounds(source.GetPos()))
            {
                Debug.Log("Cannot write log message due to " + source.actorRefName + " OOB " + source.GetPos() + " " + MapMasterScript.activeMap.columns + " " + content);
                return;
            }
            if (!GameMasterScript.heroPCActor.visibleTilesArray[(int)source.GetPos().x, (int)source.GetPos().y])
            {
                return;
            }
        }
        if (content == null)
        {
            Debug.Log("No content to write.");
            return;
        }

            //Break multiline content into single lines to try and make
            //log scrolling easier.
            if (content.Contains("\n"))
            {
                // Turns out the chinese translation is full of \n in "log" strings that shouldn't have any!
                // We are out of time to wait for the translators to fix this, so we're just replacing \n with spaces
                if (StringManager.gameLanguage == EGameLanguage.zh_cn)
                {
                    content = content.Replace("\n", " ");
                    GameLogWrite(content, source, td);
                }
                else
                {
                    var splitsies = content.Split('\n');
                    foreach (var s in splitsies)
                    {
                        GameLogWrite(s, source, td);
                    }
                }
                return;
            }
        }

        if (!forceWrite && FRAMES_BETWEEN_LOG_UPDATES > 0)
        {
            //if (Debug.isDebugBuild) Debug.Log("Enqueued " + content);
            singleton.logMessagesToWrite.Enqueue(content);
            return;
        }

        ShowCombatLog();
        timeAtLastCombatLogWrite = Time.fixedTime;

        if (PlatformVariables.OPTIMIZED_GAME_LOG)
        {
            // In optimized mode, we're not instantiating any new objects. There's just ONE TMPRO with all text. 
            // We manage the last 7 log strings in this array, so let's update that first.            
            for (int i = optimizedLogStrings.Length - 1; i > 0; i--)
            {
                // Shift everything over by one to make room for the new content at index 0.
                optimizedLogStrings[i] = optimizedLogStrings[i-1];
            }
            optimizedLogStrings[0] = content; // Place the latest content in first position

            //Here's the text we'll be writing into
            var txtLogOutput = singleton.optimizedContentTMPro;

            //There's a boundary to how many lines we can draw.
            int iMaxLineCount =  dictLogSpacingInfoByLanguageAndSize[StringManager.gameLanguage][PlayerOptions.smallLogText].iMaxLinesOnScreen;
            
            // Now construct the content.
            int iMax = optimizedLogStrings.Length - 1;

            //Create a list of the most recent X log entries
            BuildGameLogContentStringIntoStaticStringBuilder(optimizedLogStrings, iMax);

            txtLogOutput.text = optoStringBuilder.ToString();
            //txtLogOutput.text = UnityEngine.Random.Range(0, Int32.MaxValue).ToString();
            AdjustTextSize(txtLogOutput);

            // Add to our Journal log
            journalLogStringBuffer.Enqueue(content); // Add to our Journal log
            if (journalLogStringBuffer.Count > MAX_JOURNAL_LOG_LINES * 2)
            {
                content = journalLogStringBuffer.Dequeue();
                unusedLogStrings.Push(content);
            }

            // And that's it.
            return;
        }

        GameObject txt = null;

        // Add dividers on new turns

        if (!PlatformVariables.OPTIMIZED_GAME_LOG && GameMasterScript.turnNumber != lastTurnOfLogWrite)
        {
            if (!firstDividerAttempt)
            {
                lastTurnOfLogWrite = GameMasterScript.turnNumber;
                GameObject divider = null;
                if (unusedLogDividers.Count == 0)
                {
                    unusedLogDividers.Push(GameMasterScript.TDInstantiate("CombatLogDivider"));
                    Debug.Log("Had to create new log object...");
                }
                divider = unusedLogDividers.Pop(); 

                // Is this extra check going to do more harm than good? GameLogWrite takes 1.5ms without it
                if (divider.transform.parent != singleton.uiGameLogContent.transform)
                {
                    divider.transform.SetParent(singleton.uiGameLogContent.transform);
                }

                if (!divider.activeSelf)
                {
                    divider.SetActive(true);
                }
                divider.tag = "logdivider";
                combatLogObjectBuffer.Enqueue(divider);

                TryAddStringToJournal(UIManagerScript.greenHexColor + StringManager.GetString("misc_journal_turnnumber") + GameMasterScript.turnNumber + "</color>");                
            }
            else
            {
                firstDividerAttempt = false; // But don't put a divider at the very top of a fresh log.
            }

        }

        try { txt = unusedLogTextObjects.Pop(); }
        catch (Exception e)
        {
            Debug.Log("Could not pop " + content + " due to " + e);
            return;
        }

        string buff;
        try { buff = unusedLogStrings.Pop(); }
        catch (Exception e)
        {
            Debug.Log("Couldn't pop string " + content + " due to " + e);
            return;
        }

        buff = content;
        journalLogStringBuffer.Enqueue(buff);

        if (txt.transform.parent != singleton.uiGameLogContent.transform)
        {
            txt.transform.SetParent(singleton.uiGameLogContent.transform); // This is just a bit costly.
        }

        if (!txt.activeSelf)
        {
            txt.SetActive(true);
        }

        TextMeshProUGUI tmPro = dictCombatLogTextMeshes[txt];

        tmPro.text = content; // was just TEXT before        
        txt.transform.localScale = Vector3.one;        

        AdjustTextSize(tmPro);

        combatLogObjectBuffer.Enqueue(txt);

        int attempts = 0;
        while (unusedLogTextObjects.Count == 0)
        {                  
            txt = combatLogObjectBuffer.Dequeue();                  

            if (txt.tag != "logdivider")
            {
                PushLogTextObjectToStack(txt);
            }
            else
            {
                unusedLogDividers.Push(txt);
            }

            txt.SetActive(false);
            txt.transform.SetParent(null);            
        }

        if (journalLogStringBuffer.Count > MAX_JOURNAL_LOG_LINES)
        {
            buff = journalLogStringBuffer.Dequeue();
            unusedLogStrings.Push(buff);
        }

        UIManagerScript.requestUpdateScrollbar = true;

        return;
    }

    static void TryAddStringToJournal(string str)
    {
        if (journalLogStringBuffer.Count > MAX_JOURNAL_LOG_LINES)
        {
            journalLogStringBuffer.Dequeue();
        }
        journalLogStringBuffer.Enqueue(str);
    }

    public static void PushLogTextObjectToStack(GameObject go)
    {
        unusedLogTextObjects.Push(go);
    }

    public static void BeginTextBuffer()
    {
        if (bufferingText)
        {
            // We already have a combat buffer active, so put it back on the stack.
            combatEventBufferStack.Push(activeCombatEventBuffer);            
        }

        activeCombatEventBuffer = new List<LogDataPackage>();

        bufferingText = true;
        if (turnTextBuffer == null)
        {
            turnTextBuffer = new List<string>();
        }
        turnTextBuffer.Clear(); // Not needed anymore?
        
    }

    public static void EndTextBufferAndWrite()
    {
        bufferingText = false;
        WriteBuffer();

        // There was an outer containing stack, let's go back to that
        if (combatEventBufferStack.Count > 0)
        {
            activeCombatEventBuffer = combatEventBufferStack.Pop();
            bufferingText = true;
        }
    }

    static void WriteBuffer()
    {
        foreach(LogDataPackage ldp in activeCombatEventBuffer)
        {
            GameLogWrite(ldp.GetTextDisplay(), ldp.gameActor);
            GameLogDataPackages.ReturnToStack(ldp);
        } 
    }

    public void ToggleCombatLog()
    {

        if (!forceCombatLogShow)
        {
            if (!gameLogDynamicCanvasComponent.activeState) return;
            forceCombatLogShow = true;
            uiGameLog.SetActive(true);
            combatLogCG.alpha = maxCombatLogAlpha;
            combatLogCG.interactable = true;
            combatLogShowState = true;
        }
        else
        {
            forceCombatLogShow = false;
            combatLogCG.alpha = 0.0f;
            combatLogCG.interactable = false;
            combatLogShowState = false;
            uiGameLog.SetActive(false);
        }

    }

    static void ShowCombatLog()
    {
        if (!singleton.gameLogDynamicCanvasComponent.activeState) return;

        singleton.uiGameLog.SetActive(true);
        combatLogCG.alpha = singleton.maxCombatLogAlpha;
        combatLogCG.interactable = true;
        combatLogShowState = true;
        fadingCombatLog = false;
    }

    public static void HideCombatLog()
    {        
        combatLogCG.alpha = 0.0f;
        combatLogCG.interactable = false;
        combatLogShowState = false;
        fadingCombatLog = false;
        singleton.uiGameLog.SetActive(false);
    }

    public void CombatLogMouseEnterAlpha()
    {
        CanvasGroup cg = uiGameLog.GetComponent<CanvasGroup>();
        if (combatLogShowState && cg.alpha != 0.0f)
        {
            if (!gameLogDynamicCanvasComponent.activeState) return;
            cg.alpha = 1.0f;
        }
        UIManagerScript.singletonUIMS.isMouseOverUI = true;
    }

    public void CombatLogMouseExitAlpha()
    {
        CanvasGroup cg = uiGameLog.GetComponent<CanvasGroup>();
        if (combatLogShowState && cg.alpha != 0.0f)
        {
            if (!gameLogDynamicCanvasComponent.activeState) return;
            cg.alpha = 1.0f;
        }
        UIManagerScript.singletonUIMS.isMouseOverUI = false;
    }

    public static void DelayedRootedMessage(string str, Actor source)
    {
        if ((Time.fixedTime - timeLastRootedMessage) > 0.8f)
        {
            GameLogWrite(str, source);
            timeLastRootedMessage = Time.fixedTime;

            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);            
        }
    }

    public static void OnGameSceneStarted()
    {
#if UNITY_SWITCH
        GameLogScript.ForceRebuildFontMaterial(setNewMaterials: true);
#endif
    }
    public static void ForceRebuildFontMaterial(bool setNewMaterials)
    {
#if UNITY_SWITCH	
        if (setNewMaterials)
        {
            singleton.cachedLogFontMaterials = new Material[singleton.optimizedContentTMPro.fontSharedMaterials.Length];
            for (int t = 0; t < singleton.optimizedContentTMPro.fontSharedMaterials.Length; t++)
            {
                singleton.cachedLogFontMaterials[t] = Instantiate(singleton.optimizedContentTMPro.fontMaterials[t]);
            }
        }
        singleton.optimizedContentTMPro.font = null;
        singleton.optimizedContentTMPro.font = FontManager.GetFontAsset(TDFonts.WHITE);
        singleton.optimizedContentTMPro.SetAllDirty();
        singleton.optimizedContentTMPro.text = "";
#endif
    }	
    
}
