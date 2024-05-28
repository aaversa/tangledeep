using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{
    public static Item GetItemFromRef(string refName)
    {
        if (refName == null)
        {
            Debug.Log("Cannot search for a null item!");
            return null;
        }
        Item outItem;

        if (masterItemList.TryGetValue(refName, out outItem))
        {
            return outItem;
        }
        else
        {
            //Debug.Log("Item not found: " + refName);
            return null;
        }
    }

    public static Conversation FindConversation(string refName)
    {
        Conversation outConvo;

        if (masterConversationList.TryGetValue(refName, out outConvo))
        {
            return outConvo;
        }
        else
        {
            Debug.Log("Conversation " + refName + " not found.");
            return null;
        }
    }

    public static EffectScript GetEffectByRef(string refName)
    {
        EffectScript outEff;
        if (masterEffectList.TryGetValue(refName, out outEff))
        {
            return outEff;
        }
        else
        {
            Debug.Log("Effect " + refName + " not found");
            return null;
        }
    }

    public static StatusEffect FindStatusTemplateByName(string refName)
    {
        StatusEffect outSE;
        if (masterStatusList.TryGetValue(refName, out outSE))
        {
            return outSE;
        }
        else
        {
            if (Debug.isDebugBuild && refName != "randomdebuff") Debug.Log("Couldn't find status template for " + refName);
            return null;
        }
    }

    public static ActorTable GetSpawnTable(string tableRef)
    {
        ActorTable returnElement;
        if (GameMasterScript.masterSpawnTableList.TryGetValue(tableRef, out returnElement))
        {
            return returnElement;
        }

        return null;
    }
}