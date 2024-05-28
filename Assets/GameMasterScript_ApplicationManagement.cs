using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Rewired;

public partial class GameMasterScript
{
    public void SaveAndQuitToTitle()
    {
        // Have to carefully manage Switch resources due to low RAM
#if UNITY_SWITCH
        Switch_SaveDataHandler.FlushBytesLoadedAsync();
        MapMasterScript.ReleaseAllTextures();
        GC.Collect();
#endif
        SaveTheGame(autoSave: false);

        StartCoroutine(IWaitForSaveCompletionAndThenQuitToTitle());
    }

    IEnumerator IWaitForSaveCompletionAndThenQuitToTitle()
    {
        while (CurrentSaveGameState != SaveGameState.NOT_SAVING)
        {
            yield return null;
        }
        ResetAllVariablesToGameLoad();
        GameStartData.CurrentLoadState = LoadStates.BACK_TO_TITLE;
        applicationQuittingOrChangingScenes = true;
        LoadMainScene();
    }

    public void SaveAndQuit()
    {
        SaveTheGame(autoSave: false);

        StartCoroutine(IWaitForSaveCompletionAndThenCloseApplication());
        applicationQuittingOrChangingScenes = true;        
    }

    IEnumerator IWaitForSaveCompletionAndThenCloseApplication()
    {
        while (CurrentSaveGameState != SaveGameState.NOT_SAVING)
        {
            yield return null;
        }
        Application.Quit();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            tdHasFocus = true;
            TDInputHandler.IgnoreNextMouseAction();
            MusicManagerScript.appHasFocus = true;
            //Debug.Log("Has focus!");
        }
        else
        {
            tdHasFocus = false;
            MusicManagerScript.appHasFocus = false;
            //Debug.Log("No focus.");
        }
    }

    void OnDestroy()
    {

#if UNITY_SWITCH
        ReInput.ControllerConnectedEvent -= OnControllerConnected;
#else
        if (cMapper != null)
        {
            cMapper.restoreDefaultsDelegate -= OnRestoreDefaults;
        }
#endif
    }
}