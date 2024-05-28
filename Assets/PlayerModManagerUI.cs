using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.IO;

// This is the interface used by the player to switch installed mods ON or OFF.
[System.Serializable]
public class PlayerModManagerUI : MonoBehaviour {

    public GameObject scrollViewContent;
    public GameObject modListEntryTemplate;

    static PlayerModManagerUI singleton;

    public TextMeshProUGUI workshopDownloadModText;
    public Button workshopDownloadButton;

    static List<GameObject> listOfModEntries;

    bool initialized = false;

	// Use this for initialization
	void Start () {
		if (singleton != null && singleton != this)
        {
            return;
        }
        if (initialized) return;
        Initialize();
        PopulateWithMods();
    }

    void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
        }
        PopulateWithMods();
        if (Directory.Exists(PlayerModManager.modDownloadDataPath))
        {
            workshopDownloadButton.gameObject.SetActive(SteamManager.Initialized);
            workshopDownloadModText.gameObject.SetActive(SteamManager.Initialized);
        }
        else
        {
            workshopDownloadButton.gameObject.SetActive(false);
            workshopDownloadModText.gameObject.SetActive(false);
        }
    }

    void Initialize()
    {
        singleton = this;
        listOfModEntries = new List<GameObject>();
        initialized = true;
    }

    IEnumerator WaitThenTryPopulatingWithMods()
    {
        int attempts = 0;
        while (!initialized)
        {
            attempts++;
            yield return new WaitForSeconds(0.1f);
            if (attempts > 50)
            {
                Debug.LogError("Mod browser request timed out.");
                yield return null;
            }
        }

        if (attempts <= 50)
        {
            PopulateWithMods();
        }                
    }

    // Create interactable objects from the mods
    public static void PopulateWithMods()
    {
        if (!singleton.initialized)
        {
            Debug.Log("Browser not initialized.");
            return;
        }

        foreach(GameObject go in listOfModEntries)
        {
            Destroy(go);
        }

        listOfModEntries.Clear();

        foreach(ModDataPack mdp in PlayerModManager.GetAllLoadedPlayerMods())
        {
            GameObject go = GameObject.Instantiate(singleton.modListEntryTemplate);            
            listOfModEntries.Add(go);
            PlayerMods_ListEntry entryScript = go.GetComponent<PlayerMods_ListEntry>();
            entryScript.dataPack = mdp;
            entryScript.LoadModData();
            //entryScript.modFileCount = mdp.CountAllModFiles();
            go.transform.SetParent(singleton.scrollViewContent.transform);
            go.transform.localScale = Vector3.one;
            go.SetActive(true);            
        }
    }
}
