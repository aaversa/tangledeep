using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class UITopArea : MonoBehaviour {

    public TextMeshProUGUI charShortcut;
    public TextMeshProUGUI eqShortcut;
    public TextMeshProUGUI invShortcut;
    public TextMeshProUGUI skillShortcut;
    public TextMeshProUGUI rumorShortcut;
    public TextMeshProUGUI optionsShortcut;

    public TextMeshProUGUI[] shortcutText;

    void Start()
    {
        shortcutText = new TextMeshProUGUI[] { charShortcut, eqShortcut, invShortcut, skillShortcut, rumorShortcut, optionsShortcut };
    }

}
