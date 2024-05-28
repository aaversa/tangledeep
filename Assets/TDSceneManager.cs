using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TDSceneManager : MonoBehaviour 
{
   public static string bufferDestinationScene;

    void Start()
    {
        StartCoroutine(WaitThenActuallySwitchScenes());
    }

    IEnumerator WaitThenActuallySwitchScenes()
    {
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene(bufferDestinationScene);
    }

    public static void LoadScene(string sceneName)
    {
        bufferDestinationScene = sceneName;
        SceneManager.LoadScene("BufferScene");
        //SceneManager.LoadScene(sceneName);
    }

    public static AsyncOperation LoadSceneAsync(string sceneName)
    {
        bufferDestinationScene = sceneName;
        return SceneManager.LoadSceneAsync("BufferScene");
    }

}
