using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayResolutionChangeHandler : MonoBehaviour
{
    public RectTransform playerBGImageBarRT;
    public RectTransform gameLogRT;
    
    const float DEFAULT_BG_IMAGE_BAR_Y = 92f;

    const float DEFAULT_LOG_Y = -451f;

    public static Dictionary<(int,int),float> bgImageBarPositionsByScreenWidthAndHeight;

    public static GameplayResolutionChangeHandler singleton;

    void Start()
    {
        singleton = this;

        bgImageBarPositionsByScreenWidthAndHeight = new Dictionary<(int, int), float>();
        bgImageBarPositionsByScreenWidthAndHeight.Add((1920, 1080), DEFAULT_BG_IMAGE_BAR_Y);
        bgImageBarPositionsByScreenWidthAndHeight.Add((1280, 720), DEFAULT_BG_IMAGE_BAR_Y);
        bgImageBarPositionsByScreenWidthAndHeight.Add((1600, 900), DEFAULT_BG_IMAGE_BAR_Y);
        bgImageBarPositionsByScreenWidthAndHeight.Add((1024, 576), DEFAULT_BG_IMAGE_BAR_Y);
        bgImageBarPositionsByScreenWidthAndHeight.Add((1152, 648), DEFAULT_BG_IMAGE_BAR_Y);
        bgImageBarPositionsByScreenWidthAndHeight.Add((1366, 768), DEFAULT_BG_IMAGE_BAR_Y);
        bgImageBarPositionsByScreenWidthAndHeight.Add((2560, 1440), DEFAULT_BG_IMAGE_BAR_Y);
        bgImageBarPositionsByScreenWidthAndHeight.Add((3840, 2160), DEFAULT_BG_IMAGE_BAR_Y);

        bgImageBarPositionsByScreenWidthAndHeight.Add((800, 600), -87f);
        bgImageBarPositionsByScreenWidthAndHeight.Add((2500, 2000), -136f); // -89?
        bgImageBarPositionsByScreenWidthAndHeight.Add((2000, 1500), -88f);
        
        bgImageBarPositionsByScreenWidthAndHeight.Add((2560, 1600), 32f);
        bgImageBarPositionsByScreenWidthAndHeight.Add((1920, 1200), 32f);
        bgImageBarPositionsByScreenWidthAndHeight.Add((1680, 1050), 32f);
        bgImageBarPositionsByScreenWidthAndHeight.Add((1440, 900), 32f);
        bgImageBarPositionsByScreenWidthAndHeight.Add((1280, 800), 32f);

        bgImageBarPositionsByScreenWidthAndHeight.Add((1024, 768), -88f);

        bgImageBarPositionsByScreenWidthAndHeight.Add((1680, 1200), -54f);

        bgImageBarPositionsByScreenWidthAndHeight.Add((1024, 680), -5f);

        AdjustBarsByResolution();


    }

    public static void OnResolutionChanged()
    {
        singleton.AdjustBarsByResolution();
    }

    void AdjustBarsByResolution()
    {
        if (bgImageBarPositionsByScreenWidthAndHeight.TryGetValue((Screen.width, Screen.height), out float bgBarYPos))
        {

        }        
        else 
        {
            bgBarYPos = DEFAULT_BG_IMAGE_BAR_Y;
        }

        //Debug.Log("ADJUST Y POS TO " + bgBarYPos);       
        float difference = bgBarYPos - DEFAULT_BG_IMAGE_BAR_Y;

        float logYPos = DEFAULT_LOG_Y + difference;

        //Debug.Log("Default log " + DEFAULT_LOG_Y + " difference " + difference + "log y " + logYPos);

        playerBGImageBarRT.anchoredPosition = new Vector2(playerBGImageBarRT.anchoredPosition.x, bgBarYPos);
        gameLogRT.anchoredPosition = new Vector2(gameLogRT.anchoredPosition.x, logYPos);
    }
}
