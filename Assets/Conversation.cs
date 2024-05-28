using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Conversation
{

    public List<TextBranch> allBranches;
    public string refName;
    public Vector2 windowSize;
    public Vector2 windowPos;
    public bool ingameDialogue;
    public bool centered;
    public NPC whichNPC;
    public bool forceTypewriter;
    public bool keyStory;
    public bool hasLiveMergeTags = true; //shhh don't tell Andrew
    public bool writeInCombatLog;
    public bool overrideSize;
    public bool overridePos;
    public string textInputField;
    public bool stopAnimationAndUnlockInput;
    public float fadeTime;
    public float extraWaitTime;

    // used for journal entries 
    public int journalEntry;
    public float challengeValue;
    public List<int> reqEntries;

    public string strOverrideStartingBranch;

    public string spriteFontUsed;

    //set by the text branch, will cause us to draw a sprite in the dialog box.
    public string strSpriteToDisplay;

    //also set by the text branch. Fancy fancy
    public string strPrefabToDisplayInFrontOfDialog;
    public Vector2 vOffsetForPrefabToDisplay;

    //Set by the text branch if necessary.
    public Action<ButtonCombo> onDialogSelectionMade;

    public string runScriptOnConversationStart;
    public Conversation()
    {
        allBranches = new List<TextBranch>();
        fadeTime = UIManagerScript.DIALOG_FADEIN_TIME;
        reqEntries = new List<int>();
        extraWaitTime = 0f;
        spriteFontUsed = "HUDIcons"; // default        
    }

    public TextBranch FindBranch(string searchRefName)
    {
        foreach (TextBranch tb in allBranches)
        {
            if (tb.branchRefName.ToLowerInvariant() == searchRefName.ToLowerInvariant())
            {
                return tb;
            }
        }
        if (searchRefName != "chopbranch")
        {
            //if (Debug.isDebugBuild) Debug.Log("Could not find " + searchRefName + " in convo " + refName);
        }
        return null;
    }

    public void RemoveBranchByRef(string refName)
    {
        TextBranch tb = FindBranch(refName);
        if (tb != null)
        {
            allBranches.Remove(tb);
        }
    }
}

public class AlternateBranch
{
    public string altBranchRef;
    public string branchReqFlag;
    public int branchReqFlagValue;
    public string reqItemInInventory;
    public bool useReqItem;
    public bool branchReqFlagMeta; // Is this meta progress?

    public AlternateBranch()
    {
        altBranchRef = "";
        branchReqFlag = "";
        reqItemInInventory = "";
    }
}

public class AddPlayerFlag
{
    public string flagRef;
    public int flagValue;
    public bool meta;
}

public class TextBranch
{
    //Face to display in the upper left hand corner when this branch is active.
    public string strFaceSprite;
    public float[] optionalAnimTiming;

    //Does this branch change the audio any?
    public string strAudioCommands;

    //Should this branch run a script when opened?
    public string strScriptOnBranchOpen;

    //Does this branch set us up a script on conversation end?
    public string strSetScriptOnConvoEnd;
    //Would we like a sprite to display while this text branch is active?
    public string strSpriteToDisplay;

    //Perhaps we would like some animated thingy to display over the dialog box?
    public string strPrefabToDisplayInFrontOfDialog;
    public Vector2 vPrefabToDisplayOffset;

    public string branchRefName;
    public string text;
    public string grantItemRef;
    public string script_textBranchStart;
    public string script_textBranchStartValue;
    public List<ButtonCombo> responses;
    public AddPlayerFlag addFlag;
    public List<AlternateBranch> altBranches;
    public List<String> grantRecipe;

    public bool enableKeyStoryState;

    public TextBranch()
    {
        responses = new List<ButtonCombo>();
        altBranches = new List<AlternateBranch>();
        grantRecipe = new List<string>();
    }
}

public class DialogButtonResponseFlag
{
    public string flagName;
    public int flagMinValue;
    public int flagMaxValue;
    public bool isMetaDataFlag;

    public DialogButtonResponseFlag()
    {
        flagMinValue = 0;
        flagMaxValue = int.MaxValue-1;
    }
}

public class ButtonCombo
{
    public string buttonText;
    public string headerText; // optional, used in some layouts
    public DialogButtonResponse dbr;
    public string actionRef;
    public string dialogEventScript;
    public string dialogEventScriptValue;
    public string spriteRef;
    public bool toggled;
    public List<DialogButtonResponseFlag> reqFlags;
    public Dictionary<string, int> reqItems;
    public bool threeColumnStyle;
    public int extraVerticalPadding;
    public bool visible;

    /// <summary>
    /// This is used on the title screen only as a special, fancy info display.
    /// </summary>
    public SaveDataDisplayBlock attachedSaveObject;

    public ButtonCombo()
    {
        reqFlags = new List<DialogButtonResponseFlag>();
        reqItems = new Dictionary<string, int>();
        actionRef = "";
        buttonText = "";
        headerText = "";
        extraVerticalPadding = 0;
        visible = true;
    }

    public void AttachSaveObjectForThisConversation(SaveDataDisplayBlock dataBlockObject)
    {
        attachedSaveObject = dataBlockObject;
    }

    public void OnEnabledInDialogBox(bool forceOverwriteButtonText, string text, DialogButtonScript dbs, GameObject go)
    {
        if (forceOverwriteButtonText)
        {
            buttonText = text;
        }

        dbs.bodyText.text = buttonText;
        if (dbs.headerText != null)
        {
            dbs.headerText.text = headerText;
        }

        if (attachedSaveObject != null)
        {
            attachedSaveObject.gameObject.SetActive(true);            
        }
    }
}

public class ConversationData
{
    public Conversation conv;
    public DialogType dType;
    public NPC whichNPC;
    public string[] mergeTags;

    public ConversationData(Conversation c, DialogType d, NPC n, string[] sMergeTags)
    {
        conv = c;
        dType = d;
        whichNPC = n;
        mergeTags = sMergeTags;
    }
}