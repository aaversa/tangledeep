using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Xml;
using System.Globalization;
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
	using LapinerTools;
	using LapinerTools.Steam;
	using LapinerTools.uMyGUI;
#endif

public enum ChallengeTypes { DAILY, WEEKLY, NONE, COUNT }

public enum ChallengeLoadState { NOT_STARTED, LOADING, SUCCESS, FAILED, COUNT }

public class ChallengeDataPack
{
    // Job
    public CharacterJobs cJob;

    // Feats
    public List<string> playerFeats;

    // Modifiers
    public List<string> modifiersEnabled;

    // World Seed
    public int worldSeed;

    public ChallengeTypes cType;

    public int dayOfYear;
    public int weekOfYear;

    

    public ChallengeDataPack()
    {
        playerFeats = new List<string>();
        modifiersEnabled = new List<string>();
        cType = ChallengeTypes.COUNT;
    }

    public bool ParseChallengeFromText(string webText)
    {
        string[] unparsed = webText.Split('|');
        
        // Length should be 6. Day of year, Week of year, Jobs, Feats, Modifiers, seed.
        if (unparsed.Length != 6)
        {
            return false;
        }

        unparsed[0] = unparsed[0].ToUpperInvariant();

        cJob = (CharacterJobs)Enum.Parse(typeof(CharacterJobs), unparsed[0]);

        string[] allFeats = unparsed[1].Split(',');
        if (!(allFeats.Length == 1 && allFeats[0] == "none"))
        {
            for (int i = 0; i < allFeats.Length; i++)
            {
                playerFeats.Add(allFeats[i]);
            }
        }

        string[] allModifiers = unparsed[2].Split(',');
        if (!(allModifiers.Length == 1 && allModifiers[0] == "none"))
        {
            for (int i = 0; i < allModifiers.Length; i++)
            {
                allModifiers[i] = allModifiers[i].ToUpperInvariant();
                modifiersEnabled.Add(allModifiers[i]);
            }
        }

        Int32.TryParse(unparsed[3], out worldSeed);

        dayOfYear = DateTime.Now.DayOfYear;

        //Int32.TryParse(unparsed[4], out dayOfYear);

        CultureInfo ci = new CultureInfo("en-US");
        Calendar current = ci.Calendar;
        weekOfYear = current.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

        //Int32.TryParse(unparsed[5], out weekOfYear);

        return true;
    }
}

public class ChallengesAndLeaderboards : MonoBehaviour {

    public static ChallengeDataPack weeklyChallenge;
    public static ChallengeDataPack dailyChallenge;

    UnityWebRequest[] challengeRequests;

    public static ChallengeLoadState loadState;

    // Use this for initialization
    void Awake () {
        weeklyChallenge = null;
        dailyChallenge = null;
        if (!PlatformVariables.ALLOW_WEB_CHALLENGES)
        {
            return;
        }

        loadState = ChallengeLoadState.NOT_STARTED;

        challengeRequests = new UnityWebRequest[2];
        StartCoroutine(RetrieveChallengesFromWeb());
	}
	
    public static string GetDailyChallengeLeaderboard(bool alsoUploadMinimum = false)
    {
        int dayOfWeek = DateTime.Now.DayOfYear;

        string leadName = "Daily Challenge " + dayOfWeek;

        //Debug.Log("Retrieve daily leaderboard " + leadName);

        if (alsoUploadMinimum) UploadMinimumScoreToVerifyLeaderboard(leadName); // Ensure the leaderboard exists

        return leadName;
    }

    public static string GetWeeklyChallengeLeaderboard(bool alsoUploadMinimum = false)
    {
        int week = GetCurrentChallengeWeek();

        string leadName = "Weekly Challenge " + week;

        //Debug.Log("Retrieve weekly leaderboard " + leadName);

        if (alsoUploadMinimum) UploadMinimumScoreToVerifyLeaderboard(leadName); // Ensure the leaderboard exists
        return leadName;
    }

    static void UploadMinimumScoreToVerifyLeaderboard(string leaderboardName)
    {
        UploadScoreToLeaderboard(leaderboardName, 0);
    }

    public static int GetCurrentChallengeWeek()
    {
        if (weeklyChallenge == null) return -1;
        return weeklyChallenge.weekOfYear;
    }

    public static int GetCurrentChallengeDay()
    {
        if (dailyChallenge == null) return -1;
        return dailyChallenge.dayOfYear;
    }

    // Occurs if the day/week rolls over.
    public static void ResetPlayerChallengeToNone(bool cheater = false)
    {
        string cType = "";
        switch(GameStartData.challengeType)
        {
            case ChallengeTypes.DAILY:
                cType = StringManager.GetString("ui_btn_dailychallenge");
                break;
            case ChallengeTypes.WEEKLY:
                cType = StringManager.GetString("ui_btn_weeklychallenge");
                break;
        }
        StringManager.SetTag(0, cType);
        GameStartData.challengeType = ChallengeTypes.NONE;
        GameStartData.currentChallengeData = null;
        if (!cheater)
        {
            GameLogScript.LogWriteStringRef("log_challenge_failed");
        }        
        else
        {
            //Debug.Log("Cheater cheater...?");
        }
    }

    public static void UploadScoreToLeaderboard(string leaderboardName, int score)
    {
        if (PlatformVariables.LEADERBOARDS_ENABLED)
        {
                LapinerTools.Steam.SteamLeaderboardsMain.Instance.ScoreUploadMethod = Steamworks.ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest;
                LapinerTools.Steam.SteamLeaderboardsMain.Instance.ScoreSortMethod = Steamworks.ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending;
                LapinerTools.Steam.SteamLeaderboardsMain.Instance.ScoreType = Steamworks.ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric;
                LapinerTools.Steam.UI.SteamLeaderboardsUI.UploadScore(leaderboardName, score);
        }
    }

    IEnumerator RetrieveChallengesFromWeb()
    {
        //if (Debug.isDebugBuild) Debug.Log("<color=green>Retrieving challenges from the web...</color>");

        loadState = ChallengeLoadState.LOADING;
        
        challengeRequests[0] = UnityWebRequest.Get("https://impactsoundworks.com/extras/td-challenge.php?type=weekly");
        challengeRequests[1] = UnityWebRequest.Get("https://impactsoundworks.com/extras/td-challenge.php");

        bool anyFailed = false;
        for (int i = 0; i < challengeRequests.Length; i++)
        {
            UnityWebRequest www = challengeRequests[i];
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Failed to retrieve challenge: " + www.error);
                anyFailed = true;
                loadState = ChallengeLoadState.FAILED;
                break;
            }
            else
            {
                ChallengeDataPack cdp = new ChallengeDataPack();

                bool success = false;
                try
                {
                    success = cdp.ParseChallengeFromText(www.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.Log("Fatal exception parsing challenge from data: " + www.downloadHandler.text + " index " + i);
                    Debug.Log(e);
                    anyFailed = true;
                }

                if (success)
                {
                    loadState = ChallengeLoadState.SUCCESS;
                    if (i == 0) // weekly
                    {
                        weeklyChallenge = cdp;
                        weeklyChallenge.cType = ChallengeTypes.WEEKLY;
                    }
                    else
                    {
                        dailyChallenge = cdp;
                        dailyChallenge.cType = ChallengeTypes.DAILY;
                    }
                }
                else
                {
                    if (Debug.isDebugBuild) Debug.Log("Failed to parse challenge index " + i + " " + www.downloadHandler.text);
                    anyFailed = true;
                    loadState = ChallengeLoadState.FAILED;
                }
            }

        }

        if (GameMasterScript.gmsSingleton.titleScreenGMS && !anyFailed)
        {
            TitleScreenScript.OnChallengesLoaded();
        }
    }

    public static void CheckIfChallengeExpired()
    {
        if (GameStartData.currentChallengeData != null)
        {
            if (GameStartData.challengeType == ChallengeTypes.DAILY && GameStartData.challengeDay != ChallengesAndLeaderboards.GetCurrentChallengeDay())
            {
                Debug.Log(GameStartData.challengeDay + " day does not match " + ChallengesAndLeaderboards.GetCurrentChallengeDay());
                ChallengesAndLeaderboards.ResetPlayerChallengeToNone();
            }

            if (GameStartData.challengeType == ChallengeTypes.WEEKLY && GameStartData.challengeWeek != ChallengesAndLeaderboards.GetCurrentChallengeWeek())
            {
                Debug.Log(GameStartData.challengeWeek + " week does not match " + ChallengesAndLeaderboards.GetCurrentChallengeWeek());
                ChallengesAndLeaderboards.ResetPlayerChallengeToNone();
            }
        }
    }
}
