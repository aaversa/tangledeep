using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System;

public class DEBUGStringTester : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
        List<TextAsset> assetsToLoad = new List<TextAsset>();
        TextAsset[] loadedText = Resources.LoadAll<TextAsset>("Localization");
        assetsToLoad.AddRange(loadedText);

        Dictionary<EGameLanguage, string[]> dictAllTestStrings = new Dictionary<EGameLanguage, string[]>();



        for (int i = 0; i < loadedText.Length; i++)
        {
            TextAsset asset = loadedText[i];
            if (asset.name.Contains("TangledeepkanjiToKanaDict")) continue;
            if (asset.name.Contains("en_us"))
            {
                dictAllTestStrings[EGameLanguage.en_us] = new string[10000];
            }
            else
            {
                continue;
            }

            string[] strParsed = asset.text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            Debug.Log("There are " + strParsed.Length + " strings in file " + asset.name);
            for (int x = 0; x < strParsed.Length; x++)
            {
                string[] subParsed = strParsed[x].Split('\t');
                dictAllTestStrings[EGameLanguage.en_us][x] = subParsed[0];
            }
        }

        for (int i = 0; i < loadedText.Length; i++)
        {
            TextAsset asset = loadedText[i];
            if (asset.name.Contains("TangledeepkanjiToKanaDict")) continue;
            if (asset.name.Contains("en_us")) continue;

            string strFileName = asset.name.Split('.')[0];
            EGameLanguage lang = (EGameLanguage)Enum.Parse(typeof(EGameLanguage), strFileName.ToLower());

            if (lang != EGameLanguage.zh_cn) continue;

            dictAllTestStrings[lang] = new string[10000];
            
            string[] strParsed = asset.text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            Debug.Log("There are " + strParsed.Length + " strings in file " + asset.name);
            for (int x = 0; x < strParsed.Length; x++)
            {
                string[] subParsed = strParsed[x].Split('\t');
                dictAllTestStrings[lang][x] = subParsed[0];

                if (dictAllTestStrings[lang][x] != dictAllTestStrings[EGameLanguage.en_us][x])
                {
                    Debug.Log("Mismatch index " + x + ". English file has " + dictAllTestStrings[EGameLanguage.en_us][x] + " but " + lang + " wants to add " + subParsed[0]);
                }
            }
        }
    }
	
}
