using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Text;

public class RumorTextOverlay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum UIHighlightFadeState { NONE, HOLDING_AT_MAX, FADING_OUT, MAX_ON_HOVER, COUNT }

    public TextMeshProUGUI myText;
    public float holdWhiteTime = 2.5f;
    public float fadeTime = 0.5f;

    public Color maxColor;
    public Color fadedColor;

    static RumorTextOverlay singleton;

    static StringBuilder mySB;
    static List<QuestScript> questsToProcess;
    

    static float timeAtLastRefreshToWhite;
    static UIHighlightFadeState currentFadeState;

    // Start is called before the first frame update
    void Start()
    {
        myText.text = "";
        FontManager.LocalizeMe(myText, TDFonts.WHITE);
        singleton = this;
        questsToProcess = new List<QuestScript>();
        mySB = new StringBuilder();
        currentFadeState = UIHighlightFadeState.NONE;
    }

    public static void OnRumorCompletedOrFailed()
    {
        UpdateTextFromRumors();
    }

    public static void OnNewFloorEntry()
    {
        UpdateTextFromRumors();
    }

    static void UpdateTextFromRumors()
    {        
        mySB.Length = 0;

        BuildValidRumorList();
        BuildRumorText();
        HighlightText();
    }

    static void HighlightText()
    {
        if (questsToProcess.Count == 0) return;

        singleton.myText.color = singleton.maxColor;
        timeAtLastRefreshToWhite = Time.time;
        currentFadeState = UIHighlightFadeState.HOLDING_AT_MAX;
    }

    void Update()
    {
        if (currentFadeState == UIHighlightFadeState.NONE) return;

        float time = Time.time;

        if (currentFadeState == UIHighlightFadeState.HOLDING_AT_MAX)
        {
            float pComplete = (time - timeAtLastRefreshToWhite) / holdWhiteTime;
            if (pComplete >= 1f)
            {
                currentFadeState = UIHighlightFadeState.FADING_OUT;
                timeAtLastRefreshToWhite = time;
            }
        }
        else if (currentFadeState == UIHighlightFadeState.FADING_OUT)
        {
            float pComplete = (time - timeAtLastRefreshToWhite) / fadeTime;
            if (pComplete >= 1f)
            {
                pComplete = 1f;
                currentFadeState = UIHighlightFadeState.NONE;
            }

            float alphaLerp = EasingFunction.Linear(maxColor.a, fadedColor.a, pComplete);

            myText.color = new Color(1f, 1f, 1f, alphaLerp);
        }

        
    }

    static void BuildRumorText()
    {
        int counter = 1;
        if (questsToProcess.Count > 0)
        {
            mySB.Append(StringManager.GetString("ui_rumor_overlay_header"));
        }
        foreach(QuestScript qs in questsToProcess)
        {
            string text = qs.GenerateAbbreviatedQuestText();
            if (text == "") continue; // Should only happen if there is an error, which shouldn't happen

            mySB.Append(counter);
            mySB.Append(". ");
            mySB.Append(text);
            mySB.Append("\n");
            counter++;
        }

        singleton.myText.text = mySB.ToString();
    }

    static void BuildValidRumorList()
    {
        questsToProcess.Clear();
        foreach (QuestScript qs in GameMasterScript.heroPCActor.myQuests)
        {
            if (qs.complete) continue;
            if (qs.qType == QuestType.KILLMONSTERELEMENTAL)
            {
                bool valid = false;
                foreach (Monster m in MapMasterScript.activeMap.monstersInMap)
                {
                    if (m.actorRefName == qs.targetRef)
                    {
                        questsToProcess.Add(qs);
                        valid = true;
                        break;
                    }
                }
                if (valid) continue;
            }

            else if (qs.qType == QuestType.DREAMWEAPON_BOSS)
            {
                if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR ||
                    (MapMasterScript.itemWorldOpen && MapMasterScript.itemWorldItem != null &&
                    MapMasterScript.itemWorldItem.actorRefName == qs.targetRef))
                {
                    questsToProcess.Add(qs);
                    continue;
                }
            }

            else if (qs.qType == QuestType.FINDAREA && qs.targetMap != null)
            {
                Map connectionMap = qs.targetMap.GetNearestConnectionThatIsNotSideArea();
                if (MapMasterScript.activeMap.floor == connectionMap.floor)
                {
                    questsToProcess.Add(qs);
                    continue;
                }
            }

            if ((qs.targetMap != null && qs.targetMap.mapAreaID == MapMasterScript.activeMap.mapAreaID) ||
                qs.targetMapID == MapMasterScript.activeMap.mapAreaID)
            {
                questsToProcess.Add(qs);
                continue;
            }
        }
    }

    public void OnPointerEnter(PointerEventData ped)
    {
        currentFadeState = UIHighlightFadeState.MAX_ON_HOVER;
        myText.color = maxColor;
    }

    public void OnPointerExit(PointerEventData ped)
    {
        currentFadeState = UIHighlightFadeState.HOLDING_AT_MAX;
        timeAtLastRefreshToWhite = Time.time;
    }

    public static void OnRumorOverlayToggleChanged()
    {
        singleton.myText.gameObject.SetActive(PlayerOptions.showRumorOverlay);
    }
}
