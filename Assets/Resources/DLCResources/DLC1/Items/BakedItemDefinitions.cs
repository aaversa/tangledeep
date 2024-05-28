using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BakedItemDefinitions
{
    public const int NUM_RUNES_KNOWLEDGE = 10;
    public const float RUNE_KNOWLEDGE_CV = 1.6f;
    public const int RUNE_KNOWLEDGE_SHOPPRICE = 1250;
    public const int RUNE_KNOWLEDGE_SALEPRICE = 2500;
    public static readonly string[] runeSpriteRefs = new string[]
    {
        "assorteditems_572", // reddish
        "assorteditems_573",
        "assorteditems_574",
        "assorteditems_575",
        "assorteditems_576",
        "assorteditems_592", // silver
        "assorteditems_593",
        "assorteditems_594",
        "assorteditems_595",
        "assorteditems_596",
        "assorteditems_612", // dark
        "assorteditems_613",
        "assorteditems_614",
        "assorteditems_615",
        "assorteditems_616",
    };

    public static readonly int[] runeJPCosts = new int[]
    {
        800,
        800,
        800,
        800,
        800,
        500,
        500,
        500,
        500,
        500        
    };

    public static readonly string[] runeSkillRefs = new string[]
    {
        "skill_relichunter",
        "skill_menagerie",
        "skill_treasuretracker",
        "skill_scholar",
        "skill_dangermagnet",
        "skill_cleanup",
        "skill_subtletap",
        "skill_precisionfinish",
        "skill_monstertoss",
        "skill_chakrashift"
    };

    // Called after we read stuff from XML
    // These item definitions are repetitive and will take up lots of XML space
    // So let's just streamline it here
    public static void AddAllBakedItemDefinitions()
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            // Runes of Knowledge. 0-4 are passive, 5-9 are active
            for (int i = 0; i < NUM_RUNES_KNOWLEDGE; i++)
            {
                Consumable rune = new Consumable();
                rune.itemType = ItemTypes.CONSUMABLE;
                rune.rarity = Rarity.LEGENDARY;
                rune.actorRefName = "rune_teachskill" + i;
                rune.displayName = StringManager.GetString("exp_item_runeofknowledge");
                rune.description = StringManager.GetString("exp_item_runeofknowledge" + i + "_desc");
                rune.extraDescription = StringManager.GetString("exp_item_runeofknowledge_extradesc");
                rune.numberTags.Add("500");
                rune.challengeValue = RUNE_KNOWLEDGE_CV;
                rune.shopPrice = RUNE_KNOWLEDGE_SHOPPRICE;
                rune.salePrice = RUNE_KNOWLEDGE_SALEPRICE;
                rune.parentForEffectChildren = GameMasterScript.masterAbilityList["skill_teachskillfromdata"];
                rune.spriteRef = runeSpriteRefs[i];
                rune.AddTag(ItemFilters.SELFBUFF);
                rune.AddTag(ItemFilters.MULTI_USE);
                rune.AddTag(ItemFilters.DICT_NOSTACK);
                rune.scriptBuildDisplayName = "BuildRuneOfKnowledgeName";
                rune.SetActorData("jpcost", runeJPCosts[i]);
                rune.SetActorDataString("teachskill", runeSkillRefs[i]);
                rune.SetActorData("exprelic", 1);

                GameMasterScript.masterItemList.Add(rune.actorRefName, rune);
                GameMasterScript.gmsSingleton.globalUniqueItemID++;
            }
        }
    }

}
