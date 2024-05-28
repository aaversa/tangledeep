using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.IO;
using System;

public class HotbarBindable
{
    HotbarBindableActions privateHBA;
    public HotbarBindableActions actionType
    {
        get
        {
            return privateHBA;
        }
        set
        {
            privateHBA = value;
            actionIsDirty = true;
        }
    }
    AbilityScript privateAbility;
    public AbilityScript ability
    {
        get
        {
            return privateAbility;
        }
        set
        {
            privateAbility = value;
            actionIsDirty = true;
        }
    }
    Consumable privateConsumable;
    public Consumable consume
    {
        get
        {
            return privateConsumable;
        }
        set
        {
            privateConsumable = value;
            actionIsDirty = true;
        }
    }

    string bufferedActionInfo;
    bool actionIsDirty;
    public HotbarBindable()
    {
        actionType = HotbarBindableActions.NOTHING;
        ability = null;
        consume = null;
        actionIsDirty = true;
    }

    public void MarkDirty()
    {
        actionIsDirty = true;
    }

    public string GetHotbarActionInfo()
    {
        if (!actionIsDirty)
        {
            return bufferedActionInfo;
        }
        string newText = "";

        if (actionType == HotbarBindableActions.ABILITY)
        {
            string modifiedAbilityDescription = GameMasterScript.heroPCActor.GetAbilityInfoWithModifiers(ability);

            newText = modifiedAbilityDescription;
        }
        if (actionType == HotbarBindableActions.CONSUMABLE && consume != null)
        {
            newText = UIManagerScript.cyanHexColor + consume.displayName + "</color> (" +
                StringManager.GetString("ui_quantity_shorthand") + ": " + consume.Quantity + ")\n\n";

            if (consume.isHealingFood)
            {
                newText += "<color=yellow>" + consume.EstimateFoodHealing() + "</color>\n\n";
            }
            if (consume.isDamageItem)
            {
                newText += UIManagerScript.cyanHexColor + consume.EstimateItemDamage() + "</color>\n\n";
            }
            if (!String.IsNullOrEmpty(consume.extraDescription))
            {
                newText += consume.extraDescription + "\n";
            }
            string fullAmount = "";
            fullAmount = consume.GetFoodFullTurns();
            if (!String.IsNullOrEmpty(fullAmount))
            {
                newText += fullAmount + "\n\n";
            }
            if (!String.IsNullOrEmpty(consume.effectDescription))
            {
                string parsedEffectDescription = CustomAlgorithms.ParseItemDescStuff(consume.effectDescription);
                newText += parsedEffectDescription;
            }
        }

        bufferedActionInfo = newText;
        actionIsDirty = false;
        return newText;
    }

    public void Clear()
    {
        actionType = HotbarBindableActions.NOTHING;
        ability = null;
        consume = null;
        actionIsDirty = true;
    }

    public void SetBinding(KeyCode code)
    {
        switch (actionType)
        {
            case HotbarBindableActions.ABILITY:
                ability.binding = code;
                break;
            case HotbarBindableActions.CONSUMABLE:
                consume.binding = code;
                break;
        }
    }

    public KeyCode GetBinding()
    {
        switch (actionType)
        {
            case HotbarBindableActions.ABILITY:
                return ability.binding;
            case HotbarBindableActions.CONSUMABLE:
                return consume.binding;
        }
        return KeyCode.Joystick1Button19; // Bad, it's unused though.
    }

    public void WriteToSave(XmlWriter writer)
    {
        string hbaText = actionType.ToString();
        switch(actionType)
        {
            case HotbarBindableActions.NOTHING:
                // no need to write more
                break;
            case HotbarBindableActions.ABILITY:
                hbaText += "|" + ability.refName;
                break;
            case HotbarBindableActions.CONSUMABLE:
                hbaText += "|" + consume.actorRefName + "|" + consume.actorUniqueID;
                break;
        }
        writer.WriteElementString("hba", hbaText);
    }

    /// <summary>
    /// Must be on "hba" node
    /// </summary>
    /// <param name="reader"></param>
    public void ReadFromSave(XmlReader reader)
    {
        actionIsDirty = true;
        string unparsed = reader.ReadElementContentAsString();
        string[] parsed = unparsed.Split('|');
        actionType = (HotbarBindableActions)Enum.Parse(typeof(HotbarBindableActions), parsed[0]);
        switch(actionType)
        {
            case HotbarBindableActions.CONSUMABLE:
                string iRef = parsed[1];
                int itemID = Int32.Parse(parsed[2]);
                Actor getItem;
                if (GameMasterScript.dictAllActors.TryGetValue(itemID, out getItem))
                {
                    consume = getItem as Consumable;
                }
                else
                {
                    Debug.Log("Could not find item ID " + itemID + ", searching player's inventory manually by ref.");
                    consume = GameMasterScript.heroPCActor.myInventory.GetItemByRef(iRef) as Consumable;
                }
                break;
            case HotbarBindableActions.ABILITY:
                ability = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef(parsed[1]);
                if (ability == null)
                {
                    ability = GameMasterScript.masterAbilityList[parsed[1]];
                }
                break;
        }
    }
}
