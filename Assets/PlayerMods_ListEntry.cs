using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerMods_ListEntry : MonoBehaviour {

    public Image modIcon;
    public TextMeshProUGUI modName;
    public TextMeshProUGUI modDescription;    
    public int modFileCount;
    public Toggle modToggle;

    public ModDataPack dataPack;

    bool dataLoaded = false;

    IEnumerator WaitThenCheckToggleValue()
    {
        yield return new WaitForSeconds(0.05f);
        OnToggleValueChanged();
    }

    public void OnToggleValueChanged()
    {
        if (!dataLoaded && gameObject.activeSelf)
        {
            StartCoroutine(WaitThenCheckToggleValue());
            return;
        }
        //Debug.Log("Toggle is " + modToggle.isOn + " datapack value was " + dataPack.enabled);
        dataPack.enabled = modToggle.isOn;
        if (dataPack.enabled)
        {
            UIManagerScript.PlayCursorSound("UITick");
        }
        else
        {
            UIManagerScript.PlayCursorSound("UITock");
        }
        RefreshHighlights();
    }

    public void LoadModData()
    {
        modIcon.sprite = dataPack.logoSprite;
        modName.text = dataPack.modName;
        modDescription.text = dataPack.modDescription;
        modToggle.isOn = dataPack.enabled;
        dataLoaded = true;
        RefreshHighlights();
    }

    void RefreshHighlights()
    {
        if (dataPack == null)
        {
            return;
        }
        switch (dataPack.enabled)
        {
            case true:
                modName.text = UIManagerScript.greenHexColor + dataPack.modName + " (Active!)</color>";
                break;
            case false:
                modName.text = dataPack.modName + " <color=yellow>(Inactive)</color>";
                break;
        }
    }
}
