using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

public enum PossibleTagState { CHECK, DONTHAVE, HAVE, COUNT }
public class CustomAlgorithms
{
    static float xOffset;
    static float yOffset;
    static float baseRotation;
    public static Vector2[] pointsOnLine;
    public static bool[] pointsBlockLOSStates;
    public static int numPointsInLineArray;
    public static MapTileData[] tileBuffer;
    public static MapTileData[] nonCollidableTileBuffer;
    public static int numTilesInBuffer;
    public static int numNonCollidableTilesInBuffer;
    public static StringBuilder sBuilder;
    public static string[] searchTags;
    public static Dictionary<Type, Dictionary<string, MethodInfo>> dictUnboxedMethods;

    public static Dictionary<string, PossibleTagState[]> specialStringRefInfo;
    static PossibleTagState[] checkForTags;
    public static List<string> textTagsToSearchForAndDelete;
    public static bool initialized;

    public static List<string> poolStrings;

    static Stack<Point> pointPool;

    static string separatorByCulture;

    public static Dictionary<string, AbilityTags> dictStrToAbilityTagEnum;
    public static Dictionary<string, EffectType> dictStrToEffectTypeEnum;
    public static void Init()
    {
        if (initialized) return;
        checkForTags = new PossibleTagState[15];
        specialStringRefInfo = new Dictionary<string, PossibleTagState[]>();
        poolStrings = new List<string>();
        textTagsToSearchForAndDelete = new List<string>();
        pointsBlockLOSStates = new bool[800];
        pointsOnLine = new Vector2[800];
        for (int i = 0; i < pointsOnLine.Length; i++)
        {
            pointsOnLine[i] = new Vector2();
        }
        tileBuffer = new MapTileData[2048];
        nonCollidableTileBuffer = new MapTileData[2048];
        numTilesInBuffer = 0;
        numPointsInLineArray = 0;
        numNonCollidableTilesInBuffer = 0;
        sBuilder = new StringBuilder(1024);
        searchTags = new string[]
        {
            "^tag1^",
            "^tag2^",
            "^tag3^",
            "^tag4^",
            "^tag5^",
            "^tag6^",
            "^tag7^",
            "^tag8^",
            "^tag9^",
            "^tag10^"
        };
        dictUnboxedMethods = new Dictionary<Type, Dictionary<string, MethodInfo>>();
        initialized = true;

        pointPool = new Stack<Point>();
        for (int i = 0; i < 20; i++)
        {
            pointPool.Push(new Point());
        }

        Equipment.eqSB = new StringBuilder();
        dictStrToAbilityTagEnum = new Dictionary<string, AbilityTags>();
        for (int i = 0; i < (int)AbilityTags.COUNT; i++)
        {
            dictStrToAbilityTagEnum.Add(((AbilityTags)i).ToString(), (AbilityTags)i);
    }

        dictStrToEffectTypeEnum = new Dictionary<string, EffectType>();
        for (int i = 0; i < (int)EffectType.COUNT; i++)
        {
            dictStrToEffectTypeEnum.Add(((EffectType)i).ToString(), (EffectType)i);
        }
    }
    public static Point GetPointFromPool(Vector2 posValue)
    {
        if (pointPool.Count == 0)
        {
            pointPool.Push(new Point());
        }

        Point p = pointPool.Pop();

        p.x = (int)posValue.x;
        p.y = (int)posValue.y;

        return p;
    }

    public static void ReturnPointToPool(Point p)
    {
        pointPool.Push(p);
    }

    public static MethodInfo TryGetMethod(Type t, string methodName)
    {
        if (!initialized)
        {
            Init();
        }

        Dictionary<string, MethodInfo> dictMethodsOfType;
        if (!dictUnboxedMethods.TryGetValue(t, out dictMethodsOfType))
        {
            dictUnboxedMethods[t] = new Dictionary<string, MethodInfo>();
            dictMethodsOfType = dictUnboxedMethods[t];
        }

        //Debug.Log("Try get " + methodName + " of type " + t.ToString());

        MethodInfo checkMethod;
        if (dictMethodsOfType.TryGetValue(methodName, out checkMethod))
        {
            return checkMethod;
        }

        // We haven't unboxed the method yet, unbox it now.

        checkMethod = t.GetMethod(methodName);

        if (checkMethod == null)
        {
            //shep: because we're looking for methods in multiple classes, this may not be an 
            //error any longer
            
            // STOP RIGHT THERE!
            //Debug.LogError("WARNING! " + methodName + " of type " + t.Name + " does not exist!");
            return null;
        }

        dictMethodsOfType.Add(methodName, checkMethod);
        return checkMethod;
    }

    public static float GetVectorDistance(Vector2 v1, Vector2 v2)
    {
        
        return Mathf.Sqrt(Mathf.Pow((v2.x - v1.x), 2f) + Mathf.Pow((v2.y - v1.y), 2f));
    }

    public static void AddIntToDictionary<T>(Dictionary<T, int> dictToProcess, T key, int valueToAdd)
    {        

        if (dictToProcess.ContainsKey(key))
        {
            dictToProcess[key] += valueToAdd;
        }
        else
        {
            dictToProcess.Add(key, valueToAdd);
        }
    }

    public static bool CompareFloats(float checkFloat, float value)
    {
        if (Mathf.Abs(checkFloat - value) <= 0.0001f)
        {
            return true;
        }
        return false;
    }

    public static float TryParseFloat(string checkMe)
    {
        float convertedFloat = 0.0f;

        bool bSuccess = float.TryParse(checkMe, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out convertedFloat);

        if (bSuccess)
            return convertedFloat;
        else
        {
            checkMe = checkMe.Replace(',', '.');
            bSuccess = float.TryParse(checkMe, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out convertedFloat);
            if (!bSuccess)
                Debug.Log("Failure in parsing float string '" + checkMe + "'");
        }

        return convertedFloat;
    }

    public static bool IsFileLocked(string file)
    {
        FileStream stream = null;

        try
        {
            stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }
        finally
        {
            if (stream != null)
                stream.Close();
        }

        //file is not locked
        return false;
    }


    public static void RotateGameObject(GameObject go, Directions whichDir)
    {
        float rotationAmount = 0f;
        switch (whichDir)
        {
            case Directions.NEUTRAL:
                break;
            case Directions.NORTH:
                break;
            case Directions.NORTHEAST:
                rotationAmount = -45f;
                break;
            case Directions.EAST:
                rotationAmount = -90f;
                break;
            case Directions.SOUTHEAST:
                rotationAmount = -135f;
                break;
            case Directions.SOUTH:
                rotationAmount = 180f;
                break;
            case Directions.SOUTHWEST:
                rotationAmount = 135f;
                break;
            case Directions.WEST:
                rotationAmount = 90f;
                break;
            case Directions.NORTHWEST:
                rotationAmount = 45f;
                break;
        }
        go.transform.eulerAngles = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        if (rotationAmount != 0)
        {
            go.transform.Rotate(new Vector3(0, 0, rotationAmount), Space.Self);
        }
    }

    // NOTE: As of 5/10/18, this function will return LOCALIZED versions of control names
    // As far as I can tell, names like "Right Shoulder" are pre-baked in Rewired, so those are now
    // effectively string references, and are treated as such in en_us (etc)
    public static string GetButtonAssignment(string refName)
    {
        IList<Rewired.ControllerMap> mapList;

        IList<Rewired.ControllerMap> controllerMapList;
        IList<Rewired.ControllerMap> keyboardMapList;

        Rewired.Player rp;

        if (GameMasterScript.actualGameStarted)
        {
            rp = GameMasterScript.gmsSingleton.player;
        }
        else
        {
            rp = TitleScreenScript.titleScreenSingleton.player;
        }

        int joyID = 0;

        try {
            Rewired.Controller lastActive = rp.controllers.GetLastActiveController();
            if (lastActive != null) joyID = lastActive.id;
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.Log("Failed to get controller joyID " + e);
#endif
            joyID = 0;
        }

        controllerMapList = rp.controllers.maps.GetMaps(Rewired.ControllerType.Joystick, joyID);
        keyboardMapList = rp.controllers.maps.GetMaps(Rewired.ControllerType.Keyboard, 0);

        if (PlayerOptions.showControllerPrompts || TDInputHandler.lastActiveControllerType == Rewired.ControllerType.Joystick)
        {

            mapList = controllerMapList;
            foreach (Rewired.JoystickMap keym in mapList)
            {
                if (!keym.enabled) continue;

                Rewired.ActionElementMap cm = keym.GetFirstElementMapWithAction(refName);
                if (cm != null)
                {
                    return StringManager.GetLocalizedStringOrFallbackToEnglish(cm.elementIdentifierName);
                }
            }

        }
        else
        {
            //Debug.Log("Check keyboard mappings.");
            mapList = keyboardMapList;
            foreach (Rewired.KeyboardMap keym in mapList)
            {
                if (!keym.enabled) continue;

                Rewired.ActionElementMap cm = keym.GetFirstElementMapWithAction(refName);
                if (cm != null)
                {
                    return StringManager.GetLocalizedStringOrFallbackToEnglish(cm.elementIdentifierName);
                }
            }
        }


        // Didn't find anything? Try the other list.

        if (!PlayerOptions.showControllerPrompts)
        {
            mapList = controllerMapList;
            foreach (Rewired.JoystickMap keym in mapList)
            {
                if (!keym.enabled) continue;
                Rewired.ActionElementMap cm = keym.GetFirstElementMapWithAction(refName);
                if (cm != null)
                {
                    return StringManager.GetLocalizedStringOrFallbackToEnglish(cm.elementIdentifierName);
                }
            }
        }
        else
        {
            mapList = keyboardMapList;
            foreach (Rewired.KeyboardMap keym in mapList)
            {
                if (!keym.enabled) continue;
                Rewired.ActionElementMap cm = keym.GetFirstElementMapWithAction(refName);
                if (cm != null)
                {
                    return StringManager.GetLocalizedStringOrFallbackToEnglish(cm.elementIdentifierName);
                }
            }
        }

        return StringManager.GetLocalizedStringOrFallbackToEnglish("Unassigned");
    }

    public static string ParseOtherVariables(string txt)
    {
        if (!txt.Contains("#"))
        {
            return txt;
        }


        return txt;
    }

    public static string ParseItemDescStuff(string txt)
    {
        if (!txt.Contains("#"))
        {
            return txt;
        }

        string copyOfText = String.Copy(txt);

        copyOfText = ReplaceVariousPoundDelimitedVariables(copyOfText);

        return copyOfText;
    }

    public static string ParseButtonAssignments(string txt)
    {
        if (!txt.Contains("#"))
        {
            return txt;
        }

        //Debug.Log("Try replace stuff in " + txt);

        txt = ReplaceVariousPoundDelimitedVariables(txt);

        txt = txt.Replace("#SKILLS#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.VIEW_SKILLS));
        txt = txt.Replace("#HELP#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.VIEW_HELP));
        txt = txt.Replace("#FIRE#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.FIRE_RANGED_WEAPON));
        txt = txt.Replace("#CHAR#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.VIEW_CHAR_INFO));
        txt = txt.Replace("#WAIT#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.WAIT_TURN));
        txt = txt.Replace("#MENU#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.TOGGLE_MENU_SELECT));
        txt = txt.Replace("#HOTBAR#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.JUMP_TO_HOTBAR));
        txt = txt.Replace("#CONF#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.CONFIRM));
        txt = txt.Replace("#RINGMENU#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.TOGGLE_RING_MENU));
        txt = txt.Replace("#EQ#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.VIEW_EQUIPMENT));
        txt = txt.Replace("#INV#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.VIEW_CONSUMABLES));
        txt = txt.Replace("#FLASK#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.USE_HEALING_FLASK));
        txt = txt.Replace("#TOWN#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.USE_TOWN_PORTAL));
        txt = txt.Replace("#MAP#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.TOGGLE_MINIMAP));
        txt = txt.Replace("#HUD#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.HIDE_UI));
        txt = txt.Replace("#QST#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.VIEW_RUMORS));
        txt = txt.Replace("#VIEW#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.EXAMINE));
        txt = txt.Replace("#DIAG#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.DIAGONAL_MOVE_ONLY));
        txt = txt.Replace("#CWLEFT#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.CYCLE_WEAPONS_LEFT));
        txt = txt.Replace("#CWRIGHT#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.CYCLE_WEAPONS_RIGHT));
        txt = txt.Replace("#EXAMINE#", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.EXAMINE));

        //Debug.Log("It's now " + txt);

        return txt;
    }

    // checkButtonAssignments is deprecated here
    public static string ParseRichText(string txt, bool checkButtonAssignments)
    {
        if (!txt.Contains("#"))
        {
            return txt;
        }

        if (txt.Contains("##str"))
        {
            //get the meat of a string such as ##str,nando_01_hello
            string strTagID = txt.Split(',')[1];

            //replace it here
            txt = StringManager.GetLocalizedStringOrFallbackToEnglish(strTagID);
        }

        txt = txt.Replace("#big#", "<size=50>");
        txt = txt.Replace("#endbig#", "</size>");
        txt = txt.Replace("#cg#", "<#40b843>");
        txt = txt.Replace("#cy#", "<#fffb00>");
        txt = txt.Replace("#ca#", UIManagerScript.goldHexColor); // Aurum ;)
        txt = txt.Replace("#cs#", UIManagerScript.silverHexColor);
        txt = txt.Replace("#co#", UIManagerScript.orangeHexColor);
        txt = txt.Replace("#clr#", UIManagerScript.customLegendaryColor);
        txt = txt.Replace("#cr#", UIManagerScript.redHexColor);
        txt = txt.Replace("#cp#", UIManagerScript.lightPurpleHexColor);
        txt = txt.Replace("#cc#", UIManagerScript.cyanHexColor);
        txt = txt.Replace("#ec#", "</color>");


        return txt;
    }

    /*
     *  We can now add functions to our dialog strings that check data on the fly
     *  and allow for dynamic text. This saves us from having to write entire separate branches
     *  that have only tiny differences based on some value comparison
     * 
     *  Use ^$ and $^ to indicate the start and end of a function.
     *  
     *  the format is always
     *  ^$[function name]:[check value]:[result if true]:(optional result if false)$^
     *  
     *  isjob
     *  
     *      Checks to see if the hero is a given job.
     *  
     *      Example:
     *      ^$isjob:wildchild,husyn:You're not from Riverstone, are you?$^
     *      ^$isjob:hunter:A hunt like this is easy for you.:You might need to prepare for this hunt.$^
     * 
     *      [check value] can have multiple jobs separated by commas.
     *      
     *  isnotjob
     *  
     *      The inverse of isjob. Used when you want to deliver a message to a hero who is NOT one of the specific jobs, but you 
     *      also want to give specific messages to other jobs. Look at this example:
     *    
     *      ^$isjob:gambler:But you've always had luck on your side.$^
     *      ^$isjob:husyn:Not that robots like you depend on luck!$^
     *      ^$isnotjob:gambler,husyn:I hope you're lucky, you'll need it.$^
     *      
     *  isvar
     *  
     *      Checks a value in hero actor data AND meta data, selects the highest one, and compares it to a value
     *      
     *      Example:
     *      ^$isvar:birds_in_hand,10:You brought me exactly 10 birds, just like I asked!:I wanted 10 birds. Exactly 10!$^
     *      
     *      [check value] is the variable you're looking for followed by a comma, then the int you want to compare against.
     *      
     *      Since this is a direct comparison, it is best used for true or false.
     *      
     *  isnotvar
     *  
     *      The opposite of isvar above. Use this if you want to reject a specific value.
     *      Functionally you could use isvar for this, but isnotvar might read easier for us in certain situations where the intent matters.
     *      
     *  vargt, varlt
     *  
     *      Similar to isvar, but will check for greater than / less than instead of just equality.
     *      ^$vargt:birds_in_hand,10:I wanted at least 10 birds and you delivered!:This isn't 10 birds yet. Do better.$^
     *      
     *      
     * getactordata
     * 
     *      Self explanatory     
     *      ^$getactordata:flowshield_dmg^
     *      
     * getstring
     * 
     *      Calls gms.ReadTempStringData on the value
     *      
     *      You get a ^$getstring:leg_item$^
     *      
     */

    public static string ParseLiveMergeTags(string txt, bool refNamePassed = false, string refName = "")
    {
        if (!initialized)
        {
            Init();
        }

        bool storeCachedVersion = false;

        for (int i = 0; i < checkForTags.Length; i++)
        {
            checkForTags[i] = PossibleTagState.CHECK;
        }

        if (refNamePassed)
        {
            PossibleTagState[] pts;
            if (!specialStringRefInfo.TryGetValue(refName, out pts))
            {
                specialStringRefInfo.Add(refName, new PossibleTagState[15]);
                storeCachedVersion = true;
            }
            else
            {
                for (int i = 0; i < pts.Length; i++)
                {
                    checkForTags[i] = pts[i];
                }
            }
        }
        if (checkForTags[0] != PossibleTagState.DONTHAVE)  txt = txt.Replace("\\n", "\n");
        bool containsTags = false;
        if (checkForTags[1] == PossibleTagState.CHECK)
        {
            containsTags = txt.Contains("^");
        }
        else if (checkForTags[1] == PossibleTagState.HAVE)
        {
            containsTags = true;
        }
        bool containsHash = false;
        if (checkForTags[2] == PossibleTagState.CHECK)
        {
            containsHash = txt.Contains("#");
        }        
        else if (checkForTags[2] == PossibleTagState.HAVE)
        {
            containsHash = true;
        }

        if (storeCachedVersion)
        {
            specialStringRefInfo[refName][1] = containsTags == true ? PossibleTagState.HAVE : PossibleTagState.DONTHAVE;
            specialStringRefInfo[refName][2] = containsHash == true ? PossibleTagState.HAVE : PossibleTagState.DONTHAVE;
        }
        if (!containsTags && !containsHash)
        {
            return txt;
        }

        if (containsTags)
        {

        for (int i = 0; i < StringManager.mergeTags.Length; i++)
        {
                if (checkForTags[i+3] != PossibleTagState.DONTHAVE)
                {
                    bool checkForReplacement = true;
                    if (storeCachedVersion)
                    {
                        if (txt.Contains(searchTags[i]))
                        {
                            checkForTags[i + 3] = PossibleTagState.HAVE;
                        }
                        else
                        {
                            checkForTags[i + 3] = PossibleTagState.DONTHAVE;
                            checkForReplacement = false;
                        }
                    }
                    if (checkForReplacement) txt = txt.Replace(searchTags[i], StringManager.mergeTags[i]);
                }
            }
        }

        // Other possible conversation/dialog hooks.
        if (containsHash)
        {
        txt = ReplaceVariousPoundDelimitedVariables(txt);
        }        

        bool hasStrFunction = false;
        if (checkForTags[14] == PossibleTagState.CHECK)
        {
            hasStrFunction = txt.Contains('$');
        }
        else if (checkForTags[14] == PossibleTagState.HAVE)
        {
            hasStrFunction = true;
        }
        if (storeCachedVersion)
        {
            checkForTags[14] = hasStrFunction ? PossibleTagState.HAVE : PossibleTagState.DONTHAVE;
        }
        if (!hasStrFunction)
        {
            return txt;
        }

        //find all strings hanging out between a ^$ and $^
        List<string> stringfunctions = GetMatchesForPhrase(txt, "\\^\\$", "\\$\\^");
        if (stringfunctions == null || stringfunctions.Count == 0)
        {
            return txt;
        }

        foreach (var s in stringfunctions)
        {
            string[] strSplitData = s.Split(':');
            string strFunction = strSplitData[0];

            //Most functions require three values, that's the default. Some don't.
            int minimumValues = 3;

            //as more functions are written we can check here.
            switch (strFunction)
            {
                case "getstring":
                case "getqstring":
                case "getactordata":
                case "sharamode":
                case "gettext":
                    minimumValues = 2;
                    break;                
                case "npcname":
                    minimumValues = 1;
                    break;
            }

            if (strSplitData.Length < minimumValues)
            {
                //garbage
#if UNITY_EDITOR
                Debug.LogError("Bad string function '" + s + "' in '" + txt + "', not enough parameters, need " + minimumValues + ".");
#endif
                txt = txt.Replace(s, "");
                continue;
            }

            string strCheckValue = strSplitData[1];

            string strIfTrue = "";
            string strIfFalse = "";

            if (strSplitData.Length >= 3)
            {
                strIfTrue = strSplitData[2];
                strIfFalse = strSplitData.Length > 3 ? strSplitData[3] : "";
            }

            string strReplaceValue = "";

            switch (strFunction.ToLowerInvariant())
            {
                case "isjob":
                    if (GameMasterScript.actualGameStarted)
                    {
                    strReplaceValue = HeroIsJob(strCheckValue) ? strIfTrue : strIfFalse;
                    }                    
                    break;
                case "isnotjob":
                    if (GameMasterScript.actualGameStarted)
                    {
                    strReplaceValue = !HeroIsJob(strCheckValue) ? strIfTrue : strIfFalse;
                    }                    
                    break;
                case "isvar":
                    if (GameMasterScript.actualGameStarted)
                    {
                    strReplaceValue = CheckDataFlag(strCheckValue) ? strIfTrue : strIfFalse;
                    }                    
                    break;
                case "isnotvar":
                    if (GameMasterScript.actualGameStarted)
                    {
                    strReplaceValue = !CheckDataFlag(strCheckValue) ? strIfTrue : strIfFalse;
                    }                    
                    break;
                case "vargt":   //greater than or equal to
                    if (GameMasterScript.actualGameStarted)
                    {
                    strReplaceValue = !CheckDataFlag(strCheckValue, 1) ? strIfTrue : strIfFalse;
                    }                    
                    break;
                case "varlt":   //less than or equal to
                    if (GameMasterScript.actualGameStarted)
                    {
                    strReplaceValue = !CheckDataFlag(strCheckValue, -1) ? strIfTrue : strIfFalse;
                    }                    
                    break;
                case "getstring":
                    strReplaceValue = GameMasterScript.gmsSingleton.ReadTempStringData(strCheckValue);
                    break;
                case "gettext":
                    strReplaceValue = StringManager.GetString(strCheckValue);
                    break;
                case "getqstring":
                    strReplaceValue = GameMasterScript.gmsSingleton.DequeueTempStringData(strCheckValue);
                    break;
                case "getactordata":
                    if (GameMasterScript.heroPCActor != null)
                    {
                    strReplaceValue = GameMasterScript.heroPCActor.ReadActorData(strCheckValue).ToString();
                    }                    
                    break;
                case "npcname":
                    if (UIManagerScript.currentConversation != null && UIManagerScript.currentConversation.whichNPC != null)
                    {
                        strReplaceValue = UIManagerScript.currentConversation.whichNPC.displayName;
                    }
                    else
                    {
                        strReplaceValue = "????";
                    }
                    break;
                case "sharamode":
                    if (GameMasterScript.actualGameStarted)
                    {
                    switch(strCheckValue)
                    {
                        case "dominatedesc":
                            strReplaceValue = SharaModeStuff.GetDominateDescription();
                            break;
                        case "dominateedesc":
                            strReplaceValue = SharaModeStuff.GetDominateExtraDescription();
                            break;
                        case "dominatename":
                            strReplaceValue = SharaModeStuff.GetDominateDisplayName();
                            break;
                        case "prevdominatename":
                            strReplaceValue = SharaModeStuff.GetPreviousDominateDisplayName();
                            break;
                        case "dominateverb":
                            strReplaceValue = SharaModeStuff.GetDominateVerb();
                            break;
                    }
                    }
                    break;
            }

            txt = txt.Replace(s, strReplaceValue);
        }

        txt = txt.Replace("$^", "");
        txt = txt.Replace("^$", "");
        return txt;

    }

    public static string ReplaceVariousPoundDelimitedVariables(string txt)
    {
        if (GameMasterScript.gameLoadSequenceCompleted)
        {
            txt = txt.Replace("#HERONAME#", GameMasterScript.heroPCActor.displayName.ToUpperInvariant());
            txt = txt.Replace("#heroname#", GameMasterScript.heroPCActor.displayName);
            txt = txt.Replace("#MALLETCHANCE#", (int)(GameMasterScript.heroPCActor.GetMonsterMalletThreshold() * 100f) + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "");
            //txt = txt.Replace("#getjp#", ((int)(GameMasterScript.heroPCActor.GetCurJP())).ToString());
        }

        return txt;

    }

    public static bool CheckDataFlag(string data, int iCompareValue = 0)
    {
        if (string.IsNullOrEmpty(data))
        {
            return false;
        }
        string[] splitVal = data.Split(',');
        if (splitVal.Length < 2)
        {
#if UNITY_EDITOR
            Debug.LogError("Bad call to isvar: '" + data + "'");
#endif
            return false;
        }
        //Check both player data and meta data, return the larger one. 
        int iValue = Math.Max(GameMasterScript.heroPCActor.ReadActorData(splitVal[0]), MetaProgressScript.ReadMetaProgress(splitVal[0]));

        //iCompareValue = 0, just compare values straight up
        if (iCompareValue == 0)
        {
            return splitVal[1] == iValue.ToString();
        }
        int iCheckVal = Int32.Parse(splitVal[1]);

        //if compare val is 1, we want checkval to be >= value. -1, we want the opposite
        return (iCheckVal >= iValue) ? iCompareValue == 1 : iCompareValue == -1;

    }

    //Compare the hero's job against a list of job names
    public static bool HeroIsJob(string strJobList)
    {
        if (string.IsNullOrEmpty(strJobList))
        {
            return false;
        }

        string strJob = GameMasterScript.heroPCActor.myJob.jobName.ToLowerInvariant();
        
        return strJobList.ToLowerInvariant().Split(',').Contains(strJob);

    }

    public static List<string> GetMatchesForPhrase(string source, string begin, string end)
    {
        Regex regex = new Regex(begin + "(.*?)" + end);
        MatchCollection mc = regex.Matches(source);
        List<string> retList = new List<string>();

        foreach (Match m in mc)
        {
            //Groups 0 is the complete match,
            //Groups 1 ignores the start/end tags
            retList.Add(m.Groups[1].ToString());
        }

        return retList;
    }

    public static float GetDistance(Vector2 v1, Vector2 v2)
    {
        //return Vector2.Distance(v1, v2);
        return (float)Math.Sqrt(Math.Pow((v2.x - v1.x), 2d) + Math.Pow((v2.y - v1.y), 2d));
    }

    public static int GetGridDistance(Vector2 v1, Vector2 v2)
    {
        return (int)Mathf.Max(Math.Abs(v1.x - v2.x), Math.Abs(v1.y - v2.y));
    }

    public static bool CheckBresenhamsLOS(Vector2 start, Vector2 finish, Map mapToUse, bool treatForcefieldsAsBlocking = false)
    {
        GetPointsOnLineNoGarbage(start, finish);
        MapTileData mtdLOS = null;
        //Point iCheckPosLOS = new Point(finish);

        Point iCheckPosLOS = GetPointFromPool(finish);

        bool debug = false;

        // Map edges always visible?
        if (finish.x == 0 || finish.y == 0 || finish.x == mapToUse.columns-1 || finish.y == mapToUse.rows -1)
        {
            ReturnPointToPool(iCheckPosLOS);
            return true;
        }

        Vector2 vCheckPoint = Vector2.zero;

        for (int i = 0; i < numPointsInLineArray; i++)
        {
            //if our vision is blocked by the tile
            //and the tile is neither the origin or goal tile
            //then something is blocking us.
            vCheckPoint = pointsOnLine[i];
            if (!mapToUse.InBounds(vCheckPoint))
            {
                // shouldn't happen, but we have to check.
                // also cheaper than a null check
                ReturnPointToPool(iCheckPosLOS);
                return false;
            }
            mtdLOS = mapToUse.GetTile(vCheckPoint);

            //Debug.Log("Does " + mtdLOS.pos + " block vision? " + mtdLOS.BlocksVision() + " compare to end pos " + finish + " or start " + start + " i is " + i + " out of " + numPointsInLineArray);

            if (i != 0 && i != (numPointsInLineArray-1) && mtdLOS.BlocksVision(treatForcefieldsAsBlocking)) // Checking the index of i is way cheaper than vector comparison
            {
                if (debug) Debug.Log("NOT clear from " + start + " to " + finish + " due to " + mtdLOS.pos);
                ReturnPointToPool(iCheckPosLOS);
                return false; // We shouldn't need to check anything else, since we know there's something in the way.
            }
        }

        if (debug) Debug.Log("<color=green>All clear from " + start + " to " + finish + "</color>");
        ReturnPointToPool(iCheckPosLOS);
        return true;
    }

    public static void RotateGameObject(GameObject obj, GameObject followedObj, Directions dir)
    {
        // This assumes we have a SpriteEffect.
        float rotationAmount = 0.0f;
        SpriteEffect se = obj.GetComponent<SpriteEffect>();
        xOffset = 0f;
        yOffset = 0f;
        if (se != null)
        {
            xOffset = se.offset.x;
            yOffset = se.offset.y;
            baseRotation = se.baseRotation;
            if (baseRotation != 0.0f)
            {
                Vector3 localAngles = obj.transform.rotation.eulerAngles;
                localAngles.z = baseRotation;
                obj.transform.localEulerAngles = (localAngles);
            }
        }
        switch (dir)
        {
            case Directions.NEUTRAL:
                break;
            case Directions.NORTH:
                break;
            case Directions.NORTHEAST:
                rotationAmount = -45f;
                //  offsetY += 0.33f;
                //  offsetX += 0.33f;
                break;
            case Directions.EAST:
                rotationAmount = -90f;
                break;
            case Directions.SOUTHEAST:
                rotationAmount = -135f;
                break;
            case Directions.SOUTH:
                rotationAmount = 180f;
                break;
            case Directions.SOUTHWEST:
                rotationAmount = 135f;
                break;
            case Directions.WEST:
                rotationAmount = 90f;
                break;
            case Directions.NORTHWEST:
                rotationAmount = 45f;
                break;
        }
        if (rotationAmount != 0)
        {
            obj.transform.Rotate(new Vector3(0, 0, rotationAmount), Space.Self);
        }
        if (se != null)
        {
            se.SetFollowObject(followedObj, dir);
        }
    }

    public static void GetPointsOnAntialiasedLine(Vector2 start, Vector2 finish)
    {
        float x0 = finish.x;
        float x1 = start.x;
        float y0 = finish.y;
        float y1 = start.y;
        bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
        float t0;
        if (steep)
        {
            t0 = x0; // swap x0 and y0
            x0 = y0;
            y0 = t0;
            t0 = x1; // swap x1 and y1
            x1 = y1;
            y1 = t0;
        }

        float dx = x1 - x0;
        float dy = y1 - y0;
        float gradient = dy / dx;
        if (dx == 0.0f)
        {
            gradient = 1.0f;
        }

        // First endpoint
        float xend = Mathf.Round(x0);
        float yend = y0 + gradient * (xend - x0);
        float xgap = rfpart(x0 + 0.5f);
        int xpxl1 = (int)xend;
        int ypxl1 = (int)(Mathf.Floor(yend));
        if (steep)
        {
            // Skip plotting first endpoint?
        }
        else
        {
            // Skip plotting first endpoint?
        }
        float intery = yend + gradient;

        xend = round(x1);
        yend = y1 + gradient * (xend - x1);
        xgap = Mathf.Floor(x1 + 0.5f);
        int xpxl2 = (int)xend; //this will be used in the main loop
        int ypxl2 = (int)(Mathf.Floor(yend));

        if (steep)
        {
            // Skip plotting second endpoint?
        }
        else
        {
            // Skip plotting second endpoint?
        }

        int count = 0;

        Debug.Log(xpxl1 + " " + xpxl2 + " " + ypxl1 + " " + ypxl2 + " " + xend + " " + yend);

        if (steep)
        {
            for (int x = xpxl1; x <= xpxl2 - 1; x++)
            {
                Debug.Log(rfpart(intery) + " " + Mathf.Floor(intery));
                if (rfpart(intery) >= 0.4f)
                {
                    pointsOnLine[count].x = Mathf.Floor(intery);
                    pointsOnLine[count].y = x;
                    count++;
                }
                if (Mathf.Floor(intery) >= 0.4f)
                {
                    pointsOnLine[count].x = Mathf.Floor(intery) + 1f;
                    pointsOnLine[count].y = x;
                    count++;
                }
            }
        }
        else
        {
            for (int x = xpxl1; x <= xpxl2 - 1; x++)
            {
                Debug.Log(rfpart(intery) + " " + Mathf.Floor(intery));
                if (rfpart(intery) >= 0.4f)
                {
                    pointsOnLine[count].y = Mathf.Floor(intery);
                    pointsOnLine[count].x = x;
                    count++;
                }
                if (Mathf.Floor(intery) >= 0.4f)
                {
                    pointsOnLine[count].y = Mathf.Floor(intery) + 1f;
                    pointsOnLine[count].x = x;
                    count++;
                }
            }
        }

        numPointsInLineArray = count;
    }

    static float round(float x)
    {
        return x + 0.5f;
    }

    static float fpart(float x)
    {
        return x - Mathf.Floor(x);
    }

    static float rfpart(float x)
    {
        return 1 - fpart(x);
    }

    public static void GetTilesAroundPoint(Vector2 centerTile, int radius, Map checkMap, bool ignoreTypeNothing = false)
    {
        int startX = (int)centerTile.x - radius;

        if (startX <= 0)
        {
            startX = 1;
        }

        int endX = (int)centerTile.x + radius;

        if (endX >= checkMap.columns)
        {
            endX = checkMap.columns - 2;
        }

        int startY = (int)centerTile.y - radius;

        if (startY <= 0)
        {
            startY = 1;
        }

        int endY = (int)centerTile.y + radius;

        if (endY >= checkMap.rows)
        {
            endY = checkMap.rows - 2;
        }

        numTilesInBuffer = 0;

        MapTileData mtd = null;
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                mtd = checkMap.mapArray[x, y];
                if (mtd.tileType != TileTypes.NOTHING || ignoreTypeNothing)
                {
                    if (numTilesInBuffer >= tileBuffer.Length)
                    {
                        Debug.Log("Num tiles buffer exceed??? " + numTilesInBuffer + " " + tileBuffer.Length);
                    }                   
                    tileBuffer[numTilesInBuffer] = mtd;
                    numTilesInBuffer++;
                }
            }
        }
    }

    public static void GetNonCollidableTilesAroundPoint(Vector2 v2, int radius, Actor checkActor, Map checkMap)
    {
        GetTilesAroundPoint(v2, radius, checkMap);
        numNonCollidableTilesInBuffer = 0;
        for (int i = 0; i < numTilesInBuffer; i++)
        {
            if (checkMap.InBounds(tileBuffer[i].pos))
            {
                if (!tileBuffer[i].IsCollidable(checkActor))
                {
                    nonCollidableTileBuffer[numNonCollidableTilesInBuffer] = tileBuffer[i];
                    numNonCollidableTilesInBuffer++;
                }
            }

        }
    }

    public static bool CheckLOSWithVectors(Vector2 pos1, Vector2 pos2, Map checkMap, bool debug = false, bool treatForcefieldsAsBlocking = false)
    {
        if (debug)
        {
            Debug.Log("Begin checking LOS from " + pos1 + " to " + pos2);
        }

        if (pos1 == pos2) return true;

        Vector2 sum = pos2 - pos1;
        sum.Normalize();
        bool checkValid = true;
        Vector2 marker = pos1;

        //Larger values make for faster checks but the possibility of skipping tile corners
        float fStepSize = 0.2f;

        Vector2 step = new Vector2(sum.x * fStepSize, sum.y * fStepSize);
        MapTileData checkTile = checkMap.GetTile(pos1);
        Vector2 floorPos2 = new Vector2(Mathf.Floor(pos2.x), Mathf.Floor(pos2.y));

        if (checkTile == null)
        {
            Debug.Log("Null tile at " + pos1 + " " + checkMap.floor + " to " + pos2 + " " + checkMap.columns + " " + checkMap.rows);
            return false;
        }

        Vector2 checkVector = Vector2.zero;

        // Take small steps from start to finish along the normalized V2 Sum, adding all tiles along the way
        int counter = 0;
        while (checkValid)
        {
            marker += step;

            // Is the new point we've stepped to further than the finish point? Check via dot products

            Vector2 dirToEnd = (pos2 - marker);

            //Don't need to do this here, because the magntitude (pop pop) is not important
            //rather the +/- 0 value of the dot

            //dirToEnd.Normalize();
            float dotProduct = Vector2.Dot(sum, dirToEnd);
            if (dotProduct < 0)
            {
                if (debug) Debug.Log("Sum: " + sum + " Marker: " + marker + " Dot product: " + dotProduct);
                return true;
            }

            // Only add this tile if it's different than the last one we checked
            //Vector2 checkVector = new Vector2(Mathf.Floor(marker.x), Mathf.Floor(marker.y));
            checkVector.x = Mathf.Floor(marker.x);
            checkVector.y = Mathf.Floor(marker.y);

            counter++;
            if (counter >= 5000)
            {
                Debug.Log("<color=red>LOS FAILURE</color>");
                Debug.Log("From " + pos1 + " to " + pos2 + ", check vector is " + checkVector + " and check tile is " + checkTile.pos + " and step is " + step + " and marker is " + marker);
                break;
            }

            // If we're at the last tile, visibility is assured.
            if (checkVector == floorPos2)
            {
                return true;
            }

            if (debug)
            {
                Debug.Log("From " + pos1 + " to " + pos2 + ", check vector is " + checkVector + " and check tile is " + checkTile + " and step is " + step + " and marker is " + marker);
            }

            if (checkVector != checkTile.pos)
            {
                if (debug) Debug.Log(checkVector + " is no longer " + checkTile.pos + " so check if checktile blocks vision. Step: " + step.x + "," + step.y + " Marker: " + marker + "DP: " + dotProduct);
                if (!checkMap.InBounds(checkVector)) return false;
                checkTile = checkMap.GetTile(checkVector);

                if (debug)
                {
                    Debug.Log(checkTile.BlocksVision(treatForcefieldsAsBlocking) + " " + checkTile.tileType + " " + checkTile.pos);
                }

                if (checkTile.BlocksVision(treatForcefieldsAsBlocking))
                {
                    /* if (GameMasterScript.actualGameStarted)
                    {
                        Debug.Log(checkTile.pos + " " + checkTile.actorBlocksVision + " blocks vision between " + pos1 + " and " + pos2 + " " + checkTile.tileType);
                    } */
                    if (debug) Debug.Log(checkTile.pos + " blocks vision. False.");
                    return false;
                }
            }
        }

        if (debug)
        {
            Debug.Log("Everything is ok here. " + pos1 + " to " + pos2);
        }
        return true;
    }

    public static void GetPointsOnLineLenient(Vector2 start, Vector2 finish)
    {
        // Get normalized vector between the two points, from start to finish.
        Vector2 sum = finish - start;
        sum.Normalize();
        bool checkValid = true;
        Vector2 marker = start;
        Vector2 step = new Vector2(sum.x * 0.1f, sum.y * 0.1f);

        // Take small steps from start to finish along the normalized V2 Sum, adding all tiles along the way
        numPointsInLineArray = 1;
        pointsOnLine[0] = start;
        while (checkValid)
        {
            marker += step;

            // Is the new point we've stepped to further than the finish point? Check via dot products
            float dotProduct = Vector2.Dot(start, marker);
            if (dotProduct < -1)
            {
                checkValid = false;
                break;
            }

            // Only add this tile if it's different than the last one we checked
            Vector2 checkTile = new Vector2(Mathf.Floor(marker.x), Mathf.Floor(marker.y));
            if (checkTile != pointsOnLine[numPointsInLineArray - 1])
            {
                pointsOnLine[numPointsInLineArray] = checkTile;
                numPointsInLineArray++;
            }
        }
    }

    // If storeLineOfSightBools is true, checkMap MUST be passed in.        
    public static void GetPointsOnLineNoGarbage(Vector2 start, Vector2 finish, bool storeLineOfSightBools = false, Map checkMap = null)
    {
        bool swapped = false;
        int x0 = (int)start.x;
        int x1 = (int)finish.x;
        int y0 = (int)start.y;
        int y1 = (int)finish.y;
        bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
        int t0;
        int t1;
        if (steep)
        {
            t0 = x0; // swap x0 and y0
            x0 = y0;
            y0 = t0;
            t0 = x1; // swap x1 and y1
            x1 = y1;
            y1 = t0;
        }
        if (x0 > x1)
        {
            swapped = true;
            t1 = x0; // swap x0 and x1
            x0 = x1;
            x1 = t1;
            t1 = y0; // swap y0 and y1
            y0 = y1;
            y1 = t1;
        }
        int dx = x1 - x0;
        int dy = Mathf.Abs(y1 - y0);
        int error = dx / 2;
        int ystep = (y0 < y1) ? 1 : -1;
        int y = swapped ? y1 : y0;

        int count = 0;

        for (int x = swapped ? x1 : x0;
              swapped ? x >= x0 : x <= x1;
              x += (swapped ? -1 : 1))
        {
            pointsOnLine[count].x = (steep ? y : x);
            pointsOnLine[count].y = (steep ? x : y);
            count++;
            error = error - dy;
            if (error < 0)
            {
                y += ystep * (swapped ? -1 : 1);
                error += dx;
            }
        }

        numPointsInLineArray = count;
        if (storeLineOfSightBools)
        {
            for (int i = 0; i < numPointsInLineArray; i++)
            {
                pointsBlockLOSStates[i] = checkMap.GetTile(pointsOnLine[i]).BlocksVision();
            }
        }
    }

    public static List<Vector2> ConvertMTDListToVector2(List<MapTileData> baseList)
    {
        List<Vector2> rList = new List<Vector2>();
        foreach(MapTileData mtd in baseList)
        {
            rList.Add(mtd.pos);
        }
        return rList;
    }

    public static string PrintByteArray(byte[] array)
    {
        string builder = "";
        int i;
        for (i = 0; i < array.Length; i++)
        {
            builder += String.Format("{0:X2}", array[i]);
            if ((i % 4) == 3) builder += " ";
        }
        return builder;
    }

    public static string GetElementalSpriteStringFromSpriteFont(DamageTypes dType)
    {
        switch(dType)
        {
            case DamageTypes.PHYSICAL:
                return "<sprite=0>";
            case DamageTypes.FIRE:
                return "<sprite=1>";
            case DamageTypes.POISON:
                return "<sprite=2>";
            case DamageTypes.WATER:
                return "<sprite=3>";
            case DamageTypes.LIGHTNING:
                return "<sprite=4>";
            case DamageTypes.SHADOW:
                return "<sprite=5>";
        }
        return "<sprite=0>";
    }

    public static void BackupSaveFilesInSlot(int slot)
    {
        // Backups should probably only be done on PC where we have room to spare
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        List<string> coreFileNames = new List<string>()
        {            
            GetPersistentDataPath() + "/savedGame" + slot + ".xml",
            GetPersistentDataPath() + "/metaprogress" + slot + ".xml",
            GetPersistentDataPath() + "/savedMap" + slot + ".dat"            
        };

        try
        {
            foreach (string file in coreFileNames)
            {
                if (File.Exists(file))
                {
                    string backupFileName = file.Replace(slot.ToString(), slot + "_backup");
                    
                    if (File.Exists(backupFileName))
                    {
                        string backup2FileName = backupFileName.Replace("backup", "backup2");
                        if (File.Exists(backup2FileName))
                        {
                            File.Delete(backup2FileName); // delete backup2, the oldest backup                            
                        }
                        File.Copy(backupFileName, backup2FileName); // now our original backup is "backup2", slightly older.
                        File.Delete(backupFileName); // delete backup1, which should have already been copied to backup2
                    }
                    File.Copy(file, backupFileName); // copy our latest save to backup1
                }
            }
            //UnityEngine.Debug.Log("Save files for slot " + slot + " successfully backed up.");
        }
        catch(Exception e)
        {
            UnityEngine.Debug.Log("Failed to back up save files for slot " + slot + " due to: " + e);
        }

        string sharedDataPath = GetPersistentDataPath() + "/shareddata.xml";

        if (File.Exists(sharedDataPath))
        {
            string backupFileName = sharedDataPath.Replace("shareddata","shareddata_backup");

            if (File.Exists(backupFileName))
            {
                string backup2FileName = backupFileName.Replace("backup", "backup2");
                if (File.Exists(backup2FileName))
                {
                    File.Delete(backup2FileName); // delete backup2, the oldest backup                            
                }
                File.Copy(backupFileName, backup2FileName); // now our original backup is "backup2", slightly older.
                File.Delete(backupFileName); // delete backup1, which should have already been copied to backup2
            }

            File.Copy(sharedDataPath, backupFileName); // copy our latest save to backup1
            //Debug.Log("Backed up " + sharedDataPath + " to " + backupFileName);
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log(sharedDataPath + " doesn't exist so we can't back it up.");
        }

#endif
    }

    public static bool CheckIfStringHasOnlyNumbers(string txt)
    {
        foreach (char c in txt)
        {
            if (c < '0' || c > '9')
                return false;
        }

        return true;
    }

    public static string StripColors(string inputString)
    {
        if (!initialized) Init();

        // Nothing to strip? Don't bother looking further.
        if (!inputString.Contains("<"))
        {
            return inputString;
        }
        // My own parser. This is stupid. Could not figure out the regex even with help.
        // This walks through the string and finds all <color=_____> tags and preps them for removal
        // It will ignore stuff like "<=" in english text, it only looks for <color=whatever> tags

        int indexOfStartColorTag = 0;
        int indexOfCloseColorTag = 0;

        textTagsToSearchForAndDelete.Clear();

        string returnStr = inputString;
        returnStr = returnStr.Replace("</color>", "");

        bool awaitingCloseColorTag = false;
        bool confirmingOpenColorTag = false;
        for (int i = 0; i < inputString.Length; i++)
        {
            if (!confirmingOpenColorTag && inputString[i] == '<') // Possible start to a color tag.
            {
                indexOfStartColorTag = i;
                confirmingOpenColorTag = true;
            }
            else if (confirmingOpenColorTag)
            {
                if (indexOfStartColorTag == (i - 1)) // If this wasn't <, was it *following* a < ?
                {
                    if (inputString[i] == 'c' || inputString[i] == '#')
                    {
                        // Since previous character was "<" and this one is "c", this must be a color tag. 
                        // Proceed to search for removal.
                        awaitingCloseColorTag = true;
                        confirmingOpenColorTag = false; // We have confirmed and locked in the start color tag index.
                    }
                    else
                    {
                        // We were looking to confirm a start color tag but didn't find anything.
                        indexOfStartColorTag = 0;
                        confirmingOpenColorTag = false;
                    }
                }
                else 
                {
                    // We were looking to confirm a start color tag but didn't find anything.
                    indexOfStartColorTag = 0;
                    confirmingOpenColorTag = false;
                }
            }            
            else if (awaitingCloseColorTag && inputString[i] == '>')
            {
                indexOfCloseColorTag = i;
                awaitingCloseColorTag = false;
                string textTag = inputString.Substring(indexOfStartColorTag, (indexOfCloseColorTag - indexOfStartColorTag)+1);
                textTagsToSearchForAndDelete.Add(textTag);
            }
        }

        foreach (string str in textTagsToSearchForAndDelete)
        {
            returnStr = returnStr.Replace(str, "");
        }

        returnStr = returnStr.Replace("<sprite=0>", String.Empty);
        returnStr = returnStr.Replace("<sprite=1>", String.Empty);
        returnStr = returnStr.Replace("<sprite=2>", String.Empty);
        returnStr = returnStr.Replace("<sprite=3>", String.Empty);
        returnStr = returnStr.Replace("<sprite=4>", String.Empty);
        returnStr = returnStr.Replace("<sprite=5>", String.Empty);

        return returnStr;

        // below deprecated
        /* string returnStr = inputString;
        string regex = "(\\<.*\\>)"; 
        returnStr = returnStr.Replace("</color>", "");
        returnStr = Regex.Replace(returnStr, regex, "");
        return returnStr; */
    }

    /// <summary>
    /// Marks tiles around and including the given point as explored and visible.
    /// </summary>
    /// <param name="point">Vector2 position to reveal around</param>
    /// <param name="radius">Square radius to reveal (1 = 3x3 square, 2 = 5x5 square)</param>
    /// <param name="updateVisibilityImmediately">If TRUE, run the full FOV update immediately</param>
    public static void RevealTilesAroundPoint(Vector2 point, int radius, bool updateVisibilityImmediately = false)
    {
        GetTilesAroundPoint(point, radius, MapMasterScript.activeMap);
        for (int i = 0; i < numTilesInBuffer; i++)
        {
            if (!MapMasterScript.InBounds(tileBuffer[i].pos)) continue;
            GameMasterScript.heroPCActor.visibleTilesArray[tileBuffer[i].iPos.x, tileBuffer[i].iPos.y] = true;
            MapMasterScript.activeMap.exploredTiles[tileBuffer[i].iPos.x, tileBuffer[i].iPos.y] = true;
        }

        if (updateVisibilityImmediately)
        {
            MapMasterScript.singletonMMS.UpdateMapObjectData();
        }
    }

    /// <summary>
    /// Returns value rounded to nearest x.x5
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static float RoundToNearestFiveHundredth(float value)
    {
        value = (float)Math.Round(value, 2);
        value *= 100f;
        value = (float)Math.Round(value / 5f, MidpointRounding.AwayFromZero) * 0.05f;
        return value;
    }

    /* public static void AdjustFinalBoss2PositionForTransparencies(Actor act, Vector3 oldLoc, Vector3 newLoc)
    {
        MapTileData old = MapMasterScript.activeMap.GetTile(oldLoc);
        old.diagonalBlock = false;
        old.diagonalLBlock = false;
        old.extraHeightTiles = 0;

        MapTileData newTile = MapMasterScript.activeMap.GetTile(newLoc);
        newTile.extraHeightTiles = 3;
        newTile.diagonalBlock = true;
        newTile.diagonalLBlock = true;
    } */

    /// <summary>
    /// If 'item' is trash or favorite, add the appropriate marks into the name.
    /// </summary>
    /// <param name="existingName"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public static string CheckForFavoriteOrTrashAndInsertMark(string existingName, Item item)
    {
        string txt = existingName;
        if (item.favorite)
        {
            txt = "* " + txt;
        }
        if (item.vendorTrash)
        {
            txt = UIManagerScript.vendorTrashMark + " " + txt;
        }
        return txt;
    } 

    public static string RemoveTrailingCharacter(string text, char charToRemove)
    {
        if (text[text.Length - 1] == charToRemove)
        {
            text = text.Substring(0, text.Length - 1);
        }

        return text;
    }

    public static string GetPersistentDataPath()
    {
#if !UNITY_STANDALONE_OSX
        return Application.persistentDataPath;        
#endif

        string basePath = Application.persistentDataPath;

        string altPath = basePath.Replace("com.I","unity.I");

        if (!Directory.Exists(basePath)) return altPath;
        if (!Directory.Exists(altPath)) return basePath;

        // Compare both directories, see which have more files...

        var baseFiles = Directory.GetFiles(basePath);
        var altFiles = Directory.GetFiles(altPath);

        if (baseFiles.Length > altFiles.Length)
        {
            return basePath;
        }
        else
        {
            return altPath;
        }

    }
}


//A Priority Queue, courtesy of Zhentarrrrrrrrr
//
//It is a sorta-sorted list, where the goal is to make the 
//very first entry the best sorted value according to a comparison function
//that is passed in. 
//
//One particular case will be popping the best map tile off the open list.
//Instead of looking through the whole list for the Very Best One,
//this priority queue will provide it to us at the top, crescent fresh.
//
//<T> means Template, so when this is defined somewhere, whatever we send in for T
//is going to be used throughout the class.
//
// PriorityQueue<MapTileData>
// PriorityQueue<FinalFantasyGames> 
//
//"where" means that T must implement the IComparable<T> interface.
//In so doing, we expose a "CompareTo" function which we can use to -- ready? Compare!
//
//
public class PriorityQueue<T> where T : IComparable<T>
{
    public List<T> data;

    public PriorityQueue()
    {
        data = new List<T>();
    }

    //Add a T to the list, and then massage the list until the most desired one
    //is in position 0.
    public void Enqueue(T item)
    {
        data.Add(item);


        int iCurrentIndex = data.Count - 1;

        //We now compare the most recent entry (at the very end of the list, via data.Add)
        //against another value which is somewhere in the middle of the list.
        //
        // If this is our first add, lol we don't compare shit
        // If it is our second add, we compare it against the first. Ezpz.
        // Third? It also compares against the first! But NOT the second. It doesn't need to, because we know the second 
        // is not as good as the first.
        // And so on for increasing sizes -- we don't compare against every member, just some, on our way to the very first
        // which is ALWAYS the best because we've been so good about keeping our shit wired.
        //
        while (iCurrentIndex > 0)
        {
            //Compare most recent one against something somewhere in the middle between 
            //Most Recent and First
            int parentIndex = (iCurrentIndex - 1) / 2;

            //If this one is NOT as good as the target, just quit. We know it won't be as good as 
            //the very first one so why bother
            if (data[iCurrentIndex].CompareTo(data[parentIndex]) >= 0)
                break;

            //However, if we are better than the target, move up the list until eventually we fight
            //Shao Khan at the top of the tower (index 0)
            T tmp = data[iCurrentIndex]; data[iCurrentIndex] = data[parentIndex]; data[parentIndex] = tmp;
            iCurrentIndex = parentIndex;
        }
    }

    public T Dequeue()
    {
        //If we have no data, throw a warning and return default
        if (data.Count == 0)
        {
            Debug.LogWarning("Trying to Dequeue an empty PriorityQueue, do not do dis. ");
            return default(T);
        }

        //Grab the 0th item, 
        //and store it
        //then COPY the very last item to the front.
        //now there's two copies of that last item, so remove it
        //from the end. This way, we don't have to bump everything in the list down
        //by removing 0, and causing 1 -> N to shuffle downward.
        int li = data.Count - 1;
        T frontItem = data[0];
        data[0] = data[li];
        data.RemoveAt(li);

        //Our list is one smaller, so don't forget to decrease our list size.
        --li;
        int pi = 0;

        //the value at the front of the list is now BIGGER than the two ahead of it.
        //we know that because we've been so good about sorting.
        //One of the two ahead of us is a better choice than the other, so we take whichever one works and move it to the head.
        while (true)
        {
            //grab the left child in the tree of the node we're looking at
            int ci = pi * 2 + 1;

            //if we have exceeded the list size, break or else we'll crash
            if (ci > li)
            {
                break;
            }

            //look at the other child of this node -- that's RC, or Right Child.
            int rc = ci + 1;

            //If right child is smaller than the list size, 
            //point at the Very Best One of those two, whichever is a better pick.
            if (rc <= li && data[rc].CompareTo(data[ci]) < 0)
            {
                ci = rc;
            }

            //If the parent is better than CI, cool, we're done. We know the Very Best One
            //is at the head of the list.
            if (data[pi].CompareTo(data[ci]) <= 0)
            {
                break;
            }

            //otherwise, swap what we have with what is above us, 
            //and look again. We'll keep comparing those values until
            //we have the best choice at position zero.
            T tmp = data[pi]; data[pi] = data[ci]; data[ci] = tmp;
            pi = ci;
        }

        //Don't forget to return the item we cached in the beginning.
        return frontItem;
    }

    //1! 2! 3hreee ah ha ha ha! *lightning*
    public int Count()
    {
        return data.Count;
    }

    //Grab the front item, but don't remove it.
    public T Peek()
    {
        T frontItem = data[0];
        return frontItem;
    }
}