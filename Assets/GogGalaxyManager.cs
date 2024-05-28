#if !UNITY_STANDALONE_LINUX && !UNITY_ANDROID && !UNITY_IPHONE && !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
    
using UnityEngine;
using System.Collections;
using System.IO;
using Galaxy.Api;

//
// The GogGalaxyManager provides a base implementation of GOGGalaxy C# wrapper on which you can build upon.
// It handles the basics of starting up and shutting down the GOG Galaxy for use.
//
[DisallowMultipleComponent]
public class GogGalaxyManager : MonoBehaviour
{
    public string clientID; // TODO set correct param value
    public string clientSecret; // TODO set correct param value

    private static GogGalaxyManager singleton;
    public static GogGalaxyManager Instance
    {
        get
        {
            if (singleton == null)
            {
                return new GameObject("GogGalaxyManager").AddComponent<GogGalaxyManager>();
            }
            else {
                return singleton;
            }
        }
    }

    private bool isInitialized = false;

    public static bool IsInitialized()
    {
        return singleton != null && singleton.isInitialized;
    }

    private void Awake()
    {

        if (singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;

        // We want our GogGalaxyManager Instance to persist across scenes.
        DontDestroyOnLoad(gameObject);

        try
        {
            InitParams initParams = new InitParams(clientID, clientSecret);

            GalaxyInstance.Init(initParams);
        }
        catch (GalaxyInstance.Error error)
        {
            Debug.LogError("Failed to initialize GOG Galaxy: Error = " + error.ToString(), this);
            return;
        }

        Debug.Log("Galaxy SDK was initialized", this);

        isInitialized = true;
    }

    private void OnDestroy()
    {
        if (singleton != this)
        {
            return;
        }

        singleton = null;

        if (!isInitialized)
        {
            return;
        }

        // PS4 requires explicit loading/unloading dependency
        // this parameter is ignored for all platforms other than PS4
        GalaxyInstance.Shutdown(true);
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        GalaxyInstance.ProcessData();
    }
}

#endif