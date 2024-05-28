using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class CreditRollScript : MonoBehaviour
{

    //public TextMeshProUGUI creditRollTextObject;
    //VerticalLayoutGroup vLayout;
    public Image whiteFade;
    public float scrollSpeedPerSecond;
    float internalScrollSpeed;
    public int numBackerGroups;


    List<GameObject> textObjectsOnScreen;
    GameObject openingChunk;
    GameObject endingChunk;
    public bool creditsRolling;
    public bool creditsModeEnabled;
    public float endPosition;
    float totalHeight = 0f;

    float LINE_HEIGHT = 32f;
    const float RETURN_MENU_TIME = 2f;
    const float TANGLE_LOGO_SIZE = 600f;
    const float TIME_ON_INITIAL_VIEW = 2f;
    const float WHITE_FADEIN_TIME = 1.33f; // was 0.33
    bool returningToMainMenu;
    bool finished;
    bool waitingToStartMovement;
    float timeAtReturnToMainMenu;
    float timeAtCreditsStart;

    [Header("Positioning Stuff")]
    public float useXPos;
    public float offsetX;

    static int CountNumLines(string s)
    {
        int n = 0;
        foreach (var c in s)
        {
            if (c == '\n') n++;
        }
        return n + 1;
    }

    public bool UpdateInput()
    {
        if (returningToMainMenu) return true;
        if (!creditsRolling && !finished) return false;
        if ((GameMasterScript.gmsSingleton.player.GetButtonDown("Options Menu") || GameMasterScript.gmsSingleton.player.GetButtonDown("Toggle Menu Select")) && !returningToMainMenu)
        {
            // Prepare to cancel.
            returningToMainMenu = true;
            timeAtReturnToMainMenu = Time.time;
            MusicManagerScript.singleton.Fadeout(RETURN_MENU_TIME);
            return true;
        }
        else if (GameMasterScript.gmsSingleton.player.GetAnyButton())
        {
            internalScrollSpeed = scrollSpeedPerSecond * 5;
            return true;
        }
        else
        {
            internalScrollSpeed = scrollSpeedPerSecond;
        }
        return false;
    }

    public IEnumerator DoCreditsInstantiation()
    {
        float timeAtStart = Time.realtimeSinceStartup;
        openingChunk = GameMasterScript.TDInstantiate("Credits Single Column");
        openingChunk.transform.SetParent(gameObject.transform);
        openingChunk.transform.localScale = Vector3.one;

        string teamCredits = StringManager.GetString("credits_team");
        int numLines = CountNumLines(teamCredits);

        TextMeshProUGUI openingChunkTMPro = openingChunk.GetComponent<TextMeshProUGUI>();

        openingChunkTMPro.text = teamCredits;
        FontManager.LocalizeMe(openingChunkTMPro, TDFonts.WHITE);

        float checkX = 0;

        float diffResolutionToReference = Screen.width / 1920f; // 1920 is our reference

        if (diffResolutionToReference > 1f)
        {
            LINE_HEIGHT *= diffResolutionToReference;
        }

        openingChunk.transform.position = new Vector3(checkX, 0 - 150f, 0f);

        totalHeight = TANGLE_LOGO_SIZE + (LINE_HEIGHT * numLines) - 600f;

        textObjectsOnScreen = new List<GameObject>();
        textObjectsOnScreen.Add(openingChunk);
        for (int i = 0; i < numBackerGroups; i++)
        {
#if !UNITY_SWITCH
            if (Time.realtimeSinceStartup - timeAtStart >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeAtStart = Time.realtimeSinceStartup;
                GameMasterScript.IncrementLoadingBar(0.0125f);
            }
#endif

            GameObject header = GameMasterScript.TDInstantiate("Credits Single Column");
            header.transform.SetParent(gameObject.transform);
            TextMeshProUGUI tmp = header.GetComponent<TextMeshProUGUI>();
            tmp.text = StringManager.GetString("credits_backers_" + i + "_title") + "\n";
            FontManager.LocalizeMe(tmp, TDFonts.WHITE);
            header.transform.position = new Vector3(0f, 0 - (totalHeight + LINE_HEIGHT), 0f);
            header.transform.localScale = Vector3.one;
            totalHeight += (LINE_HEIGHT * 2);

            string backersAtLevel = StringManager.GetString("credits_backers_" + i);
            string[] names = backersAtLevel.Split('|');

            int textObjectsToCreate = (names.Length / 3) + (names.Length % 3);

            // Create textObjectsToCreate number of row objects, each of which has up to 3 names.
            string[] textToUse = new string[3];
            for (int objectsCreated = 0; objectsCreated < textObjectsToCreate; objectsCreated++)
            {
                if (Time.realtimeSinceStartup - timeAtStart >= GameMasterScript.MIN_FPS_DURING_LOAD)
                {
                    yield return null;
                    timeAtStart = Time.realtimeSinceStartup;
                    GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.tiny);
                }

                for (int t = 0; t < 3; t++)
                {
                    int indexWithinMasterArray = (objectsCreated * 3) + t;
                    if (indexWithinMasterArray >= names.Length)
                    {
                        textToUse[t] = "";
                        continue;
                    }
                    textToUse[t] = names[indexWithinMasterArray];
                }
                GameObject container = GameMasterScript.TDInstantiate("Credits 3 Column");
                textObjectsOnScreen.Add(container);
                container.transform.SetParent(gameObject.transform);

#if !UNITY_SWITCH
                container.transform.localScale = Vector3.one;
                RectTransform rt = container.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(Screen.width, rt.sizeDelta.y);
#endif
                TextMeshProUGUI[] columns = container.GetComponentsInChildren<TextMeshProUGUI>();

                bool isFinalLine = (objectsCreated == textObjectsToCreate - 1);

                int numRows = 1; // Text and one \n\n

                for (int x = 0; x < columns.Length; x++)
                {
                    columns[x].text = textToUse[x];
                    FontManager.LocalizeMe(columns[x], TDFonts.WHITE);
                    if (isFinalLine)
                    {
                        columns[x].text += "\n\n";
                    }
                }
                if (isFinalLine)
                {
                    numRows += 2;
                }

                container.transform.position = new Vector3(checkX, 0 - (totalHeight + LINE_HEIGHT), 0f);

#if !UNITY_SWITCH
                if (Screen.width < 1920f && Screen.width >= 1600f) // 900p or so
                {
                    container.transform.localPosition = new Vector3((-1f * Screen.width) + 530f, container.transform.localPosition.y);
                }
                else if (Screen.width < 1600f) // 720p and below
                {
                    container.transform.localPosition = new Vector3(-1f * Screen.width + 70f, container.transform.localPosition.y);
                }
#endif

                totalHeight += (LINE_HEIGHT * numRows);
            }
        }

        if (Time.realtimeSinceStartup - timeAtStart >= GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeAtStart = Time.realtimeSinceStartup;
            GameMasterScript.IncrementLoadingBar(0.0125f);
        }

        endingChunk = GameMasterScript.TDInstantiate("Credits Single Column");
        endingChunk.transform.SetParent(gameObject.transform);
        endingChunk.transform.localScale = Vector3.one;
        TextMeshProUGUI endingText = endingChunk.GetComponent<TextMeshProUGUI>();
        string finalText = StringManager.GetString("credits_final");
        endingText.text = finalText;
        FontManager.LocalizeMe(endingText, TDFonts.WHITE);

        totalHeight += (CountNumLines(finalText) * LINE_HEIGHT);
        endingChunk.transform.position = new Vector3(checkX, 0 - totalHeight, 0f);

        textObjectsOnScreen.Add(endingChunk);

        endPosition = totalHeight + Screen.height + (Screen.height / 2f);
    }

    public void RollCredits()
    {
        whiteFade.color = Color.white;
        internalScrollSpeed = scrollSpeedPerSecond;
        creditsRolling = true;
        waitingToStartMovement = true;
        timeAtCreditsStart = Time.time;
    }

    void FixedUpdate()
    {

        float widthValue = Screen.width;
        if (useXPos != 0)
        {
            widthValue = useXPos;
        }
        //if (Screen.width > 1920f)
        {
            widthValue = Screen.width;
            offsetX = Screen.width;
        }

        if (creditsRolling)
        {
            if (waitingToStartMovement)
            {
                float pComplete = (Time.time - timeAtCreditsStart) / TIME_ON_INITIAL_VIEW;
                transform.position = new Vector3(useXPos, 0f, 0f);
                if (pComplete >= 1.0f)
                {
                    waitingToStartMovement = false;
                }
                // now the white fadein...
                pComplete = (Time.time - timeAtCreditsStart) / (TIME_ON_INITIAL_VIEW * 0.25f);
                if (pComplete > 1.0f) pComplete = 1.0f;
                whiteFade.color = new Color(1f, 1f, 1f, (1f - pComplete));
                return;
            }
            float distanceMoved = Time.deltaTime * internalScrollSpeed;

            /* if (Screen.width > 1920f)
            {
                checkX = (Screen.width / 4f);
            } */

            transform.position = new Vector3(offsetX, transform.position.y + distanceMoved, 0f);
            if (transform.position.y >= endPosition)
            {
                creditsRolling = false;
                finished = true;
            }
        }
        if (returningToMainMenu)
        {
            float pComplete = (Time.time - timeAtReturnToMainMenu) / RETURN_MENU_TIME;
            if (pComplete >= 1.0f)
            {
                int ngPlusValue = GameStartData.NewGamePlus;
                GameMasterScript.ResetAllVariablesToGameLoad();
                if (SharaModeStuff.IsSharaModeActive())
                {
                    GameStartData.CurrentLoadState = LoadStates.BACK_TO_TITLE;
                }
                else
                {
                    if (ngPlusValue == 1)
                    {
                        GameStartData.CurrentLoadState = LoadStates.PLAYER_VICTORY_NGPLUS;
                    }
                    else if (ngPlusValue == 2)
                    {
                        GameStartData.CurrentLoadState = LoadStates.PLAYER_VICTORY_NGPLUSPLUS;
                    }
                    else
                    {
                        GameStartData.CurrentLoadState = LoadStates.PLAYER_VICTORY;
                    }
                    
                }
                
                GameMasterScript.LoadMainScene();
            }
            whiteFade.color = new Color(1f, 1f, 1f, pComplete);
        }
    }
}
