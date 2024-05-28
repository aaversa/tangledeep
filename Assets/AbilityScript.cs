using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Debug = UnityEngine.Debug;


public enum AbilityTags
{
    INSTANT, TARGETED, TVISIBLEONLY, GROUNDTARGET, PERTARGETANIM, MONSTERAFFECTED, DESTRUCTIBLEAFFECTED, HEROAFFECTED, CURSORTARGET, CENTERED, MULTITARGET, UNIQUETARGET,
    CLEARGROUNDONLY, ADJACENTWALLONLY, ADJACENTGROUNDONLY, EMPTYONLY, SIMULTANEOUSANIM, LINEOFSIGHTREQ, CANROTATE, RANDOMLINEDIR, RANDOMOFFSETSIGNS, WALLONLY, GROUNDONLY, SEQUENTIALCLOCKWISEOFFSET,
    ONHITPROPERTIES, SEQROTATECLOCKWISE, FILLTOPOINT, FLOATING, PLAYANIMONEMPTY, TARGETUSEDTILES, CANTOGGLE, OVERRIDECHILDSFX, REQUIRESHIELD, RANDOMTARGET, FORCEPOSITIONTOTARGETSONLY, REQHEROTRIGGER, NOLOGTEXT, REQUIRERANGED,
    WATERONLY, LOCKSQUARETOTARGET, NOCHAMPIONS, MONSTERFIXEDPOS, REQUIREMELEE, REQELEMENTALAFFINITY, GROUNDBASEDEFFECT, STACKPROJECTILE, COOLDOWN_ONLY_REQWEAPON, NO_ATTACK_ANIM, SHARAPOWER, TEACHABLE_MONSTERTECH, SPELLSHAPE,
    CANNOT_INHERIT, DRAGONSOUL, COUNT
};

public enum TargetShapes
{
    RECT, CROSS, XCROSS, FLEXCROSS, VLINE, HLINE, FLEXLINE, BURST, POINT, DLINE_NE, DLINE_SE,
    CIRCLE, CONE, FLEXCONE, CHECKERBOARD, RANDOM,
    BIGDIPPER, CLAW, CIRCLECORNERS, DIRECTLINE, DIRECTLINE_THICK,
    FLEXRECT, SEMICIRCLE, COUNT
};

public enum LandingTileTypes { NONE, ENDOFLINE, FURTHEST }

public enum StatusTrigger
{
    TURNSTART, TURNEND, OUTOFCOMBATTURN, ATTACK, USEABILITY, ATTACKED, DAMAGE, ONADD, ONREMOVE, PERMANENT, ONMOVE, ENTERTILE, EXITTILE, ENDTURNINTILE, STARTTURNINTILE,
    DESTROYED, ATTACKBLOCK, ATTACKPARRY, ONCRIT, DUNGEONONLY, TAKEDAMAGE, KILLENEMY_NOT_WORTHLESS, KILLENEMY_NOT_TRIVIAL, CAUSE_DAMAGE, PROCESSDAMAGE_ATTACKER, PROCESSDAMAGE_DEFENDER,
    SWITCHMAPS, PLAYER_CAUSE_STATUS, THANESONG_LEVELUP, ONHEAL, ON_SHIELD_SHATTER, ATTACKDODGE, ATTACK_NOBLOCK, ITEMUSED,
    COUNT
};
// On enter, exit, end turn in tile are used for TileActor type.

public enum AbilityTarget { ENEMY, SELF, GROUND, ALLY, SUMMONGROUND, SUMMONHAZARD, DUNGEON, THING, ENEMY_SUMMON, PET_ALLY, ALLY_ONLY, COUNT }; // This is for monsters only.

public enum StatusFlags
{
    BLEED, POISON, HALTMOVEMENT, THANESONG, THANEVERSE, STUN, LOWERDEFENSE, LOWERATTACK, BOOSTRESISTANCE, SONGBLADE,
    BURNING, FREEZING, FORCEHOTBARCHECK, COUNT
}

public enum AbilityFlags { MOVESELF, SOULKEEPER, THANESONG, THANEVERSE, HEALHP, POTION, SELFDESTRUCT, COUNT }

public enum EffectConditionalEnums { STATUSREMOVED, ORIGDAMAGETAKEN, COUNT }

// Instant - No wind-up or charge-up time
// Targeted - Requires targeting via grid (but may be 'fixed' targeting, non-cursor based)
// TVisibleOnly - can only target/hit things that you can see
// Groundtarget - Does not hit an actor, but a ground tile instead
// Projectile - Fires a projectile animation
// PerTargetAnim - If true, plays a sprite effect for each affected entity
// Monsteraffected - Affects monster actors
// Heroaffected - Affects the hero
// CursorTarget - Cursor-based targeting overlay
// Centered - Ability targeting is centered on the player (relevant for line targeting)
// Multitarget - select multiple targets
// Unique target - Each target is only affected once
// Cleargroundonly - Must target a ground tile with no adjacent wall
// Adjacentwallonly - Must target a ground tile adjacent to a wall
// Adjacentgroundonly - Must target a WALL tile adjacent to ground
// Simultaneousanim - play all individual anims at once
// Emptyonly - Tile must be empty
// Line of sight req - must have line of sight to the tile.
// Canrotate - The shape can be rotated
// RandomLineDir - Line direction changes each time the ability is used successfully.
// RandomOffsetSigns - Offset signs change per usage.
// SEQUENTIALCLOCKWISEOFFSET - moves offset clockwise each time.
// SEQROTATECLOCKWISE - moves LINEDIR clockwise each time
// OnHitProperties - Adds "ATTACK" status tick when using this ability
// FilltoPoint - If the target is a point, fill in tiles to that point regardless of offset.
// Floating - The target shape can be moved anywhere.
// Play anim on empty - display an animation even on an empty tile.


// If an ability is CursorTarget but NOT GroundTarget, then the cursor shape MUST be "Point".

public class EffectConditional
{
    public int index;
    public EffectConditionalEnums ec;
}

[System.Diagnostics.DebuggerDisplay("ref:{refName}")]
public class AbilityScript : ISelectableUIObject
{

    //private bool[] myAbilityTags = new bool[(int)AbilityTags.COUNT]; // Max # of attributes an ability can have

    private ulong myAbilityTags; // Max # of attributes an ability can have
    private ulong myAbilityTags2; // plz no

    public int myID; 
    protected static int globalID = 0; 

    public List<string> load_effectRefsToConnect;

    // Breaking out number references for localization ease
    public List<string> numberTags;


    public int maxCooldownTurns;
    int curCooldownTurns;

    public string combatLogText;
    public string chargeText;
    public string extraDescription;

    public string unbakedDescription;
    public string unbakedShortDescription;
    public string unbakedExtraDescription;

    public string instantDirectionalAnimationRef;
    public string stackProjectileFirstTile;

    public string teachPlayerAbility;
    public bool active;
    public bool combatOnly;
    //public int uniqueID;
    public int exclusionGroup;
    public int repetitions;
    public float power;
    public float variance;
    public int range;
    public int spiritsRequired;
    public string abilityName;
    public string refName;
    public string iconSprite;
    public string sfxOverride;
    public string useAnimation; // The animation your CHARACTER uses
    public List<string> requireTargetRef;
    public TargetShapes targetShape;
    public TargetShapes boundsShape;
    public int targetRange;
    public float randomChance;
    public int numMultiTargets; //
    public int targetChangeCondition;
    public List<EffectScript> listEffectScripts;
    public List<AbilityScript> subAbilities;
    public List<EffectConditional> conditions;
    public int staminaCost;

    public int energyCost = 0;

    public int healthCost = 0;
    public float percentCurHealthCost = 0;
    public float percentMaxHealthCost = 0;
    public int chargeTime = 0;
    public string description;
    public string shortDescription;
    public AbilityTarget targetForMonster;
    public bool passiveAbility;
    public bool displayInList;
    public int passTurns;
    public int chargeTurns;
    public bool toggled;
    public bool passiveEquipped;
    public bool usePassiveSlot;
    public bool spellshift;
    public bool budokaMod;
    public int energyReserve;
    public int staminaReserve;

    public bool[] abilityFlags;

    public CharacterJobs jobLearnedFrom;

    public int targetOffsetX;
    public int targetOffsetY;

    public KeyCode binding;
    public Directions direction;
    public Directions lineDir;
    public LandingTileTypes landingTile;
    public WeaponTypes reqWeaponType;

    // Relevant stuff for status effects

    //public static string[] AbilityTagNames;
    //public static string[] StatusTriggerNames;

    bool initialized;
    public bool abilityTagsRead;

    //Shep: Scripts to call on various game events
    public string script_AttackBlock;
    public string script_FighterBelow60Health;
    public string script_FighterBelow33Health; // if I have to make any more of these I will refactor this system.
    public string script_FighterBelowHalfHealth;
    public string script_FighterBelowQuarterHealth;
    public string script_SpecialTargeting;
    public string script_onLearn;
    public string script_onPreAbilityUse; // Fires at the TOP of the turn when you FIRST use the ability, before it executes.

    public bool clearEffectsForSubAbilities; // Use only for multi-part abilities when you want parts 2,3 etc. to clear previous data completely

    // use for animation
    public string spritePopInfo;

    //Shep: data to keep during loading serialized templates from binary files
    public List<string> list_effectsToLoadRefNames;

    public static string[] abilityTagsAsStrings;
    public static string[] statusTriggersAsStrings;

    public string cultureSensitiveName; // used for non-english sorts

    #region Interface Jibba

    public Sprite GetSpriteForUI()
    {
        return UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictUIGraphics, iconSprite);
    }

    public string GetSortableName()
    {
        return cultureSensitiveName;
    }

    public void BuildSortableName()
    {
        cultureSensitiveName = StringManager.BuildCultureSensitiveName(abilityName);
    }

    public string GetNameForUI()
    {
        string builder = abilityName;
        if (GetCurCooldownTurns() > 0)
        {
            builder = "<#fffb00>" + abilityName + " [" + GetCurCooldownTurns() + "t]</color>";
        }
        else if (toggled)
        {
            builder = UIManagerScript.greenHexColor + "*" + abilityName + "*</color>";
        }
        return builder;
    }

    public string GetInformationForTooltip()
    {
        return GetAbilityInformation();
    }

    #endregion

    static string localizedFreePassive;

    public bool hasConditions;

    public string GetExtraDescription()
    {
        if (CheckAbilityTag(AbilityTags.COOLDOWN_ONLY_REQWEAPON))
        {
            StringManager.SetTag(0, Weapon.weaponTypesVerbose[(int)reqWeaponType]);
            return extraDescription + " " + StringManager.GetString("ui_abil_cooldown_weapontype");
        }

        return extraDescription;
    }

    /// <summary>
    /// Returns TRUE if this ability has *any* SummonActorEffect that summons on collidable objects.
    /// </summary>
    /// <returns></returns>
    public bool DoesAbilityHaveSummonOnCollidableEffects()
    {
        foreach(EffectScript eff in listEffectScripts)
        {
            if (eff.effectType != EffectType.SUMMONACTOR) continue;
            SummonActorEffect sae = eff as SummonActorEffect;
            if (sae.summonOnCollidable || sae.summonOnBreakables) return true;
        }
        return false;
    }

    public AbilityScript()
    {
        Init();
    }

    public void ParseNumberTags()
    {
        unbakedShortDescription = shortDescription;
        unbakedDescription = description;
        unbakedExtraDescription = extraDescription;

        if (!numberTags.Any()) return;
        for (int i = 0; i < numberTags.Count; i++)
        {
            description = description.Replace("^number" + (i + 1) + "^", "<#fffb00>" + numberTags[i] + "</color>");
            shortDescription = shortDescription.Replace("^number" + (i + 1) + "^", "<#fffb00>" + numberTags[i] + "</color>");
            extraDescription = extraDescription.Replace("^number" + (i + 1) + "^", "<#fffb00>" + numberTags[i] + "</color>");
        }
    }

    public int GetCurCooldownTurns()
    {
        return curCooldownTurns;
    }

    public bool IsDamageAbility()
    {
        if (listEffectScripts.Count == 0) return false;
        foreach (EffectScript eff in listEffectScripts)
        {
            if (eff.effectType == EffectType.DAMAGE) return true;
        }
        return false;
    }

    public bool IsSummonAbility()
    {
        if (listEffectScripts.Count == 0) return false;
        foreach (EffectScript eff in listEffectScripts)
        {
            if (eff.effectType == EffectType.SUMMONACTOR) return true;
        }
        return false;
    }

    public void SetCurCooldownTurns(int value)
    {
        curCooldownTurns = value;
    }

    protected virtual void Init()
    {
        myID = globalID;
        globalID++;
        if (initialized)
        {
            return;
        }
        listEffectScripts = new List<EffectScript>(5);
        subAbilities = new List<AbilityScript>(3);
        numberTags = new List<string>(5);

        unbakedDescription = "";
        unbakedExtraDescription = "";
        unbakedShortDescription = "";

        /*     
        for (int i = 0; i < (int)AbilityTags.COUNT; i++)
        {
            // Initialize the ability array
            myAbilityTags[i] = false;
        }
        */

        load_effectRefsToConnect = new List<string>();
        script_onLearn = "";
        script_onPreAbilityUse = "";
        useAnimation = "UseItem";
        active = false;
        passTurns = 0;
        maxCooldownTurns = 0;
        SetCurCooldownTurns(0);
        targetOffsetY = 0;
        targetOffsetX = 0;
        spiritsRequired = 0;
        repetitions = 0;
        abilityName = ""; // don't call this Default bro

        chargeText = "";
        combatLogText = "";
        extraDescription = "";
        shortDescription = "";
        sfxOverride = "";
        iconSprite = "";
        description = "";
        spritePopInfo = "";

        //GameMasterScript.AssignAbilityID(this);
        //GameMasterScript.AddAbilityToDict(this);
        abilityFlags = new bool[(int)AbilityFlags.COUNT];
        initialized = true;
        lineDir = Directions.NORTH;
        reqWeaponType = WeaponTypes.ANY;

        displayInList = true;
        targetShape = TargetShapes.POINT;
        boundsShape = TargetShapes.RECT;
        targetRange = 1;
        range = 1;
    }

    public void SetUniqueIDAndAddToDict()
    {
        //GameMasterScript.AssignAbilityID(this);
        //GameMasterScript.AddAbilityToDict(this);
    }

    public int GetEquipSortValue()
    {
        if (!passiveAbility) return 0;

        int value = 0;

        if (passiveEquipped) value += 2;

        if (UsePassiveSlot) value++;

        return value;
    }

    public void AddRequiredTarget(string req)
    {
        if (requireTargetRef == null)
        {
            requireTargetRef = new List<string>();
        }
        requireTargetRef.Add(req);
    }

    public virtual void ReadFromSave(XmlReader reader, Actor owner)
    {
        string txt;
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            string strValue = reader.Name.ToLowerInvariant();
            //Debug.Log("Reading ability " + strValue + " " + reader.NodeType);
            switch (strValue)
            {
                case "skillref":
                    refName = reader.ReadElementContentAsString();
                    AbilityScript template = GetAbilityByName(refName);
                    if (template == null)
                    {
                        Debug.Log("Could not find template " + refName);
                    }
                    else
                    {
                        AbilityScript.CopyFromTemplate(this, template);
                    }

                    break;
                case "maxcooldownturns":
                    maxCooldownTurns = reader.ReadElementContentAsInt();
                    break;
                case "active":
                    active = reader.ReadElementContentAsBoolean();
                    break;
                case "passiveequipped":
                    passiveEquipped = reader.ReadElementContentAsBoolean();
                    break;
                case "curcooldownturns":
                    curCooldownTurns = reader.ReadElementContentAsInt();
                    break;
                case "direction":
                    direction = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "lineorientation":
                    lineDir = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "joblearnedfrom":
                    jobLearnedFrom = (CharacterJobs)Enum.Parse(typeof(CharacterJobs), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                /* case "uniqueid":
    				uniqueID = reader.ReadElementContentAsInt();
    				break; */
                case "targetoffsetx":
                    targetOffsetX = reader.ReadElementContentAsInt();
                    break;
                case "targetoffsety":
                    targetOffsetY = reader.ReadElementContentAsInt();
                    break;
                case "range":
                    range = reader.ReadElementContentAsInt();
                    break;
                case "targetrange":
                    targetRange = reader.ReadElementContentAsInt();
                    break;
                /* case "abilityflag":
                    AbilityFlags flagToRead = (AbilityFlags)Enum.Parse(typeof(AbilityFlags), reader.ReadElementContentAsString().ToUpperInvariant());
                    abilityFlags[(int)flagToRead] = true;
                    break; */
                case "randomchance":
                    txt = reader.ReadElementContentAsString();
                    randomChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "toggled":
                    toggled = reader.ReadElementContentAsBoolean();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        reader.ReadEndElement();
    }

    public virtual void WriteToSave(XmlWriter writer, int actorOwnerID, bool ownerIsDestructible = false)
    {
        writer.WriteStartElement("ability");
        writer.WriteElementString("skillref", refName);
        writer.WriteElementString("active", active.ToString().ToLowerInvariant());
        if (passiveEquipped)
        {
            writer.WriteElementString("passiveequipped", passiveEquipped.ToString().ToLowerInvariant());
        }
        
        if (maxCooldownTurns > 0)
        {
            writer.WriteElementString("maxcooldownturns", maxCooldownTurns.ToString());
        }
        if (curCooldownTurns > 0)
        {
            writer.WriteElementString("curcooldownturns", curCooldownTurns.ToString());
        }

        if (direction != Directions.NORTH)
        {
            writer.WriteElementString("direction", direction.ToString().ToLowerInvariant());
        }
        
        if (lineDir != Directions.NORTH)
        {
            writer.WriteElementString("lineorientation", lineDir.ToString().ToLowerInvariant());
        }
        //writer.WriteElementString("uniqueid",uniqueID.ToString());
        if (targetOffsetX != 0)
        {
            writer.WriteElementString("targetoffsetx", targetOffsetX.ToString());
        }
        if (targetOffsetY != 0)
        {
            writer.WriteElementString("targetoffsety", targetOffsetY.ToString());
        }        
        writer.WriteElementString("range", range.ToString());
        writer.WriteElementString("targetrange", targetRange.ToString());
        if (toggled)
        {
            writer.WriteElementString("toggled", toggled.ToString().ToLowerInvariant());
        }
        
        writer.WriteElementString("joblearnedfrom", jobLearnedFrom.ToString().ToLowerInvariant());


        writer.WriteEndElement();
    }
    public string GetAbilityInformation()
    {
        // Display source somewhere
        string text = "";

        AbilityScript modifiedAbility = null;

        if (GameMasterScript.gameLoadSequenceCompleted)
        {
            GameMasterScript.gmsSingleton.SetAbilityToTryWithModifiedCostsAndInformation(this, false, ref modifiedAbility);
        }
        else
        {
            modifiedAbility = this;
        }

        int displayStamina = modifiedAbility.staminaCost;
        int displayEnergy = modifiedAbility.energyCost;
        int displayHealth = modifiedAbility.healthCost;

        if (GameMasterScript.actualGameStarted)
        {
            //#todo: cleanse this, because we have these modifiers in data now.
            //BUT, this would require changing GetAbilityInformation into the code path
            //we're using for every other way we evaluate power costs.
            //Not jumping on this right away because it's a bigger change and these calls are
            //made in the job screen before you purchase the power.
            /* if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_mmobsidian"))
            {
                displayEnergy = (int)(displayEnergy * 0.8f);
                if (displayStamina == 0)
                {
                    displayStamina = 2 + (int)(displayEnergy * 0.1f);
                }
                else
                {
                    displayStamina += 1 + (int)(displayStamina * 0.1f);
                }
            } */

            /* if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_mmnecroband"))
            {
                displayEnergy = (int)(displayEnergy * 0.85f);
                displayStamina = (int)(displayStamina * 0.85f);
                displayHealth = (int)(GameMasterScript.heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 0.02f);
            } */
        }

        if (!modifiedAbility.passiveAbility)
        {
            if (displayStamina > 0)
            {
                text += StatBlock.statNames[(int)StatTypes.STAMINA] + ": <color=#40b843>" + displayStamina + "</color> ";
            }
            if (displayEnergy > 0)
            {
                text += StatBlock.statNames[(int)StatTypes.ENERGY] + ": " + UIManagerScript.cyanHexColor + displayEnergy + "</color> ";
            }
            if (displayHealth > 0)
            {
                text += StatBlock.statNames[(int)StatTypes.HEALTH] + ": " + UIManagerScript.redHexColor + displayHealth + "</color> ";
            }
            if (spiritsRequired > 0)
            {
                text += StringManager.GetString("misc_echoes_required") + ": " + UIManagerScript.greenHexColor + modifiedAbility.spiritsRequired + "</color> ";
            }
            if (modifiedAbility.maxCooldownTurns > 0)
            {
                StringManager.SetTag(0, modifiedAbility.maxCooldownTurns.ToString());
                text += StringManager.GetString("ui_cooldown_turns_yellow");
            }
        }

        if (modifiedAbility.reqWeaponType != WeaponTypes.ANY)
        {
            text += "\n<#fffb00>" + StringManager.GetString("ui_req_weapon") + ": " + Weapon.weaponTypesVerbose[(int)modifiedAbility.reqWeaponType] + "</color>\n";
        }

        if (modifiedAbility.chargeTime != 0 && !passiveAbility)
        {
            if (modifiedAbility.chargeTime == 200)
            {
                text += "\n<#fffb00>" + StringManager.GetString("misc_free_turn") + "</color>";
            }
            else
            {
                StringManager.SetTag(0, modifiedAbility.chargeTime.ToString());
                text += "\n" + StringManager.GetString("ui_skill_display_ct") + "</color>";
            }
        }
        if (modifiedAbility.CheckAbilityTag(AbilityTags.TARGETED))
        {
            StringManager.SetTag(0, modifiedAbility.GetBoundsShapeText());
            StringManager.SetTag(1, modifiedAbility.range.ToString());
            text += "\n" + StringManager.GetString("ui_bounds");
        }
        if (modifiedAbility.CheckAbilityTag(AbilityTags.CURSORTARGET))
        {
            StringManager.SetTag(0, modifiedAbility.GetTargetShapeText());
            StringManager.SetTag(1, modifiedAbility.targetRange.ToString());
            text += "\n" + StringManager.GetString("ui_target_shape");
        }

        if (modifiedAbility.energyReserve > 0)
        {
            StringManager.SetTag(6, energyReserve.ToString());
            StringManager.SetTag(7, UIManagerScript.cyanHexColor + StringManager.GetString("stat_energy") + "</color>");
            text += "\n" + StringManager.GetString("misc_reserve_stat");
        }
        if (modifiedAbility.staminaReserve > 0)
        {
            StringManager.SetTag(6, staminaReserve.ToString());
            StringManager.SetTag(7, UIManagerScript.cyanHexColor + StringManager.GetString("stat_stamina") + "</color>");
            text += "\n" + StringManager.GetString("misc_reserve_stat");
        }

        text += "<#fffb00>";
        if (modifiedAbility.passiveAbility)
        {
            text += modifiedAbility.GetPassiveDescription();
        }
        else
        {
            text += "\n\n" + modifiedAbility.description;
        }
        text += "</color>";

        if (!string.IsNullOrEmpty(modifiedAbility.extraDescription))
        {
            string critStuff = "";
            string weaponDamageText = "";
            if ((from eff in modifiedAbility.listEffectScripts where eff.effectType == EffectType.DAMAGE select eff as DamageEffect).Any(de => de.canCrit))
            {
                critStuff = " " + StringManager.GetString("ui_can_crit");
            }
            if ((from eff in modifiedAbility.listEffectScripts where eff.effectType == EffectType.DAMAGE select eff as DamageEffect).Any(de => de.inheritWeaponDamageType))
            {
                weaponDamageText = " " + StringManager.GetString("desc_inherit_weapelement");
            }
            text += "\n\n" + UIManagerScript.cyanHexColor + modifiedAbility.GetExtraDescription() + critStuff + weaponDamageText + "</color>";
        }

        if (modifiedAbility.passiveAbility)
        {
            if (modifiedAbility.CheckAbilityTag(AbilityTags.DRAGONSOUL))
            {
                text += UIManagerScript.orangeHexColor + "\n\n" + StringManager.GetString("ui_dragonsoul_slot") + "</color>";
            }
            else
            {
                if (modifiedAbility.UsePassiveSlot)
                {
                    text += UIManagerScript.orangeHexColor + "\n\n" + StringManager.GetString("ui_requires_support_slot") + "</color>";
                }
                else
                {
                    text += UIManagerScript.orangeHexColor + "\n\n" + StringManager.GetString("ui_no_slot_required") + "</color>";
                }
            }

        }
        return text;
    }



    public bool CanActorUseAbility(Fighter ft, bool ignoreCosts = false, bool debug = false)
    {
        if (ignoreCosts)
        {
            if (curCooldownTurns == 0)
            {
                if (debug) Debug.Log("Cur cooldown turns 0, so we can use it");
                return true;
            }
            return false;
        }

        if (ft.myStats.GetCurStat(StatTypes.ENERGY) >= energyCost && ft.myStats.GetCurStat(StatTypes.STAMINA) >= staminaCost && curCooldownTurns == 0)
        {
            if (debug) Debug.Log("Cur cooldown turns 0, and we have the statz, so we can use it");
            return true;
        }
        else
        {
            return false;
        }
    }

    public static AbilityScript GetAbilityByName(string name)
    {
        AbilityScript outAbil;

        if (GameMasterScript.masterAbilityList.TryGetValue(name, out outAbil))
        {
            return outAbil;
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("Couldnt find " + name);
#endif
            return null;
        }
    }

    public static EffectScript GetNewEffectFromTemplate(EffectScript eff)
    {
        EffectScript newEff = null;
        switch (eff.effectType)
        {
            case EffectType.ABILITYCOSTMODIFIER:
                newEff = new AbilityModifierEffect();
                break;
            case EffectType.ADDSTATUS:
                newEff = new AddStatusEffect();
                break;
            case EffectType.ALTERBATTLEDATA:
                newEff = new AlterBattleDataEffect();
                break;
            case EffectType.ATTACKREACTION:
                newEff = new AttackReactionEffect();
                break;
            case EffectType.CHANGESTAT:
                newEff = new ChangeStatEffect();
                break;
            case EffectType.DAMAGE:
                newEff = new DamageEffect();
                break;
            case EffectType.DESTROYACTOR:
                newEff = new DestroyActorEffect();
                break;
            case EffectType.DESTROYTILE:
                newEff = new DestroyTileEffect();
                break;
            case EffectType.EMPOWERATTACK:
                newEff = new EmpowerAttackEffect();
                break;
            case EffectType.IMMUNESTATUS:
                newEff = new ImmuneStatusEffect();
                break;
            case EffectType.INFLUENCETURN:
                newEff = new InfluenceTurnEffect();
                break;
            case EffectType.MOVEACTOR:
                newEff = new MoveActorEffect();
                break;
            case EffectType.REMOVESTATUS:
                newEff = new RemoveStatusEffect();
                break;
            case EffectType.SPECIAL:
                newEff = new SpecialEffect();
                break;
            case EffectType.SPELLSHAPE:
                newEff = new SpellShaperEffect();
                break;
            case EffectType.SUMMONACTOR:
                newEff = new SummonActorEffect();
                break;
            case EffectType.COUNT:
            default:
                newEff = new EffectScript();
                break;
        }
        
        newEff.CopyFromTemplate(eff);
        return newEff;
    }

    public void CopyAllButEffects(AbilityScript template)
    {
        script_onPreAbilityUse = template.script_onPreAbilityUse;
        script_onLearn = template.script_onLearn;
        spritePopInfo = template.spritePopInfo;
        useAnimation = template.useAnimation;
        teachPlayerAbility = template.teachPlayerAbility;
        extraDescription = template.extraDescription;
        percentCurHealthCost = template.percentCurHealthCost;
        percentMaxHealthCost = template.percentMaxHealthCost;
        exclusionGroup = template.exclusionGroup;
        spiritsRequired = template.spiritsRequired;
        repetitions = template.repetitions;
        spellshift = template.spellshift;
        budokaMod = template.budokaMod;
        combatOnly = template.combatOnly;
        usePassiveSlot = template.usePassiveSlot;
        sfxOverride = template.sfxOverride;
        toggled = template.toggled;
        combatLogText = template.combatLogText;
        landingTile = template.landingTile;
        chargeTurns = template.chargeTurns;
        passTurns = template.passTurns;
        shortDescription = template.shortDescription;
        clearEffectsForSubAbilities = template.clearEffectsForSubAbilities;
        requireTargetRef = template.requireTargetRef;
        displayInList = template.displayInList;
        chargeTime = template.chargeTime;
        direction = template.direction;
        targetForMonster = template.targetForMonster;
        chargeText = template.chargeText;
        abilityName = template.abilityName;
        cultureSensitiveName = template.cultureSensitiveName;
        refName = template.refName;
        power = template.power;
        energyReserve = template.energyReserve;
        staminaReserve = template.staminaReserve;
        range = template.range;
        maxCooldownTurns = template.maxCooldownTurns;
        curCooldownTurns = template.curCooldownTurns;
        binding = template.binding;
        variance = template.variance;
        iconSprite = template.iconSprite;
        targetShape = template.targetShape;
        boundsShape = template.boundsShape;
        targetRange = template.targetRange;
        randomChance = template.randomChance;
        numMultiTargets = template.numMultiTargets;
        targetChangeCondition = template.targetChangeCondition;
        staminaCost = template.staminaCost;
        energyCost = template.energyCost;
        description = template.description;
        targetOffsetX = template.targetOffsetX;
        targetOffsetY = template.targetOffsetY;
        passiveAbility = template.passiveAbility;
        reqWeaponType = template.reqWeaponType;
        instantDirectionalAnimationRef = template.instantDirectionalAnimationRef;
        stackProjectileFirstTile = template.stackProjectileFirstTile;

        script_AttackBlock = template.script_AttackBlock;
        script_FighterBelowHalfHealth = template.script_FighterBelowHalfHealth;
        script_FighterBelowQuarterHealth = template.script_FighterBelowQuarterHealth;
        script_FighterBelow60Health = template.script_FighterBelow60Health;
        script_FighterBelow33Health = template.script_FighterBelow33Health;

        script_SpecialTargeting = template.script_SpecialTargeting;

        foreach (string tag in template.numberTags)
        {
            numberTags.Add(tag);
        }

        for (int i = 0; i < (int)AbilityFlags.COUNT; i++)
        {
            abilityFlags[i] = template.abilityFlags[i];
        }

        if (template.hasConditions)
        {
            conditions = new List<EffectConditional>();
            hasConditions = true;
            foreach (EffectConditional ec in template.conditions)
            {
                // We don't need to make a copy of the conditionals, right?
                conditions.Add(ec);
            }
        }

        CopyAbilityTags(template);
        /*
        for (int c = 0; c < (int)AbilityTags.COUNT; c++)
        {
            myAbilityTags[c] = template.myAbilityTags[c];
        }
        */
    }

    public static void CopyFromTemplate(AbilityScript newAbil, AbilityScript template) // Copy FROM template TO newAbil
    {
        newAbil.CopyAllButEffects(template);

        foreach (AbilityScript abil in template.subAbilities)
        {
            newAbil.subAbilities.Add(abil);
            /* Debug.LogError("Adding subabil " + abil.abilityName + " " + abil.myID + " to new ability " + newAbil.abilityName + " " + newAbil.myID + " which has " + abil.listEffectScripts.Count + " sub effects and " + abil.subAbilities.Count + " subs itself.");
            foreach(EffectScript eff in abil.listEffectScripts)
            {
                Debug.Log("Added sub ability " + abil.myID + " has effect " + eff.effectRefName + " " + eff.processBufferIndex);
            }  */
        }
        foreach (EffectScript eff in template.listEffectScripts)
        {
            newAbil.AddEffectScript(eff);
        }

    }

    public void AddEffectScript(EffectScript es)
    {
        listEffectScripts.Add(es);

        /* if (es != null && es.effectRefName != null && es.effectRefName.Contains("godspeed"))
        {
            Debug.LogError("adding " + es.effectRefName + " to " + refName + " Abil ID: " + myID + " count: " + listEffectScripts.Count + " INDEX: " + es.processBufferIndex);
        } */
    }

    //Shep: Most of this is spellshaper code, but we can also 
    //adjust powers outside of the spellshaper now because we are awesome
    //
    // but wait! Isn't there an energyCostMod and staminaCostMod calculated on the player cached data?
    // Yes, there is, but these ability modifiers apply to specific powers,
    // While those above are global. 
    //
    //Shep: Then don't send a global power into here for a local tooltip
    public void ModifyCostAndShape(Fighter caster)
    {
        if (UIManagerScript.singletonUIMS.CheckTargeting())
        {
            return;
        }
        List<StatusEffect> effects = caster.myStats.GetAllStatuses();

        // wut.txt
        //
        //  Build a List<AbilityEffects> from all effects in the statuseffects. Don't use listEffectScripts. 
        //  From that list, get only the SpellShaperEffects,
        //  And if any of those are .spellShape ESpellShape.BARRIER,
        //  true.
        List<EffectScript> alllistEffectScriptsFromCaster = effects.SelectMany(ef => ef.listEffectScripts).ToList();
        bool bBarrierEffectOnCaster = alllistEffectScriptsFromCaster.OfType<SpellShaperEffect>().Any(sse => sse.spellShape == ESpellShape.BARRIER);
        bool bMaterializeEffectOnCaster = alllistEffectScriptsFromCaster.OfType<SpellShaperEffect>().Any(sse => sse.spellShape == ESpellShape.MATERIALIZE);

        string localRef = refName;

        if (refName == "skill_unstablemagic")
        {
            List<string> possibleElements = new List<string>();
            possibleElements.Add("skill_fireevocation");
            possibleElements.Add("skill_iceevocation");
            possibleElements.Add("skill_shadowevocation");
            possibleElements.Add("skill_acidevocation");
            localRef = possibleElements[UnityEngine.Random.Range(0, possibleElements.Count)];
        }

        //if we have barrier status on us, and we're spellshiftable, do something first before iteration
        if (bBarrierEffectOnCaster && spellshift)
        {
            RemoveAbilityTag(AbilityTags.MONSTERAFFECTED);
            AddAbilityTag(AbilityTags.HEROAFFECTED);
            AddAbilityTag(AbilityTags.PERTARGETANIM);
            AddAbilityTag(AbilityTags.SIMULTANEOUSANIM);
            boundsShape = TargetShapes.POINT;
            range = 1;
            RemoveAbilityTag(AbilityTags.CURSORTARGET);
            AddAbilityTag(AbilityTags.GROUNDTARGET);
            targetForMonster = AbilityTarget.SELF;

            //Shep: What is the purpose of these three lines?
            listEffectScripts.Clear();
            AddStatusEffect ase = GameMasterScript.GetEffectByRef("spellshiftbarrier") as AddStatusEffect;
            AddEffectScript(ase);



            //#todo data drive this one day to spawn all sorts of shit
            switch (localRef)
            {
                case "skill_fireevocation":
                    ase.statusRef = "firebarrier";
                    break;
                case "skill_iceevocation":
                    ase.statusRef = "icebarrier";
                    break;
                case "skill_shadowevocation":
                    ase.statusRef = "shadowbarrier";
                    break;
                case "skill_acidevocation":
                    ase.statusRef = "acidbarrier";
                    break;
                case "skill_acidevocation_2":
                    ase.statusRef = "poisonbarrier";
                    break;
            }

        }

        if (bMaterializeEffectOnCaster && spellshift)
        {
            SummonActorEffect shiftTemplate = GameMasterScript.spellshiftMaterializeTemplate;
            RemoveAbilityTag(AbilityTags.MONSTERAFFECTED);
            AddAbilityTag(AbilityTags.GROUNDONLY);
            AddAbilityTag(AbilityTags.GROUNDTARGET);
            targetForMonster = AbilityTarget.GROUND;
            listEffectScripts.Clear();
            AddEffectScript(shiftTemplate);

            bool movingSummons = GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_spellshiftmaterialize_2");

            //Allow some variance in the summon times so they don't all pop up at once.
            shiftTemplate.fMaxDelayBeforeSummon = 0.35f;

            localRef = refName;

            if (refName == "skill_unstablemagic")
            {
                List<string> possibleElements = new List<string>();
                possibleElements.Add("skill_fireevocation");
                possibleElements.Add("skill_iceevocation");
                possibleElements.Add("skill_shadowevocation");
                possibleElements.Add("skill_acidevocation");
                localRef = possibleElements[UnityEngine.Random.Range(0, possibleElements.Count)];
            }

            //#todo: Data drive this, and don't rely on magic numbers
            switch (localRef)
            {
                case "skill_fireevocation":
                    shiftTemplate.summonActorRef = "obj_ss_evokefire";
                    shiftTemplate.summonOnCollidable = true;
                    shiftTemplate.summonDuration = 7;
                    shiftTemplate.effectRefName = "skill_fireevocation";
                    break;
                case "skill_iceevocation":
                    shiftTemplate.summonActorRef = "obj_ss_evokeice";
                    shiftTemplate.summonDuration = 15;
                    shiftTemplate.effectRefName = "skill_iceevocation";
                    break;
                case "skill_shadowevocation":
                    shiftTemplate.summonActorRef = "obj_ss_evokeshadow";
                    shiftTemplate.summonOnCollidable = true;
                    shiftTemplate.summonDuration = 10;
                    shiftTemplate.effectRefName = "skill_shadowevocation";
                    break;
                case "skill_acidevocation":
                    shiftTemplate.summonActorRef = "obj_ss_evokeacid";
                    shiftTemplate.summonOnCollidable = true;
                    shiftTemplate.summonDuration = 7;
                    shiftTemplate.effectRefName = "skill_acidevocation";
                    break;
                case "skill_acidevocation_2":
                    shiftTemplate.summonActorRef = "obj_evoke_poisoncloud";
                    shiftTemplate.summonOnCollidable = true;
                    shiftTemplate.summonDuration = 7;
                    shiftTemplate.effectRefName = "skill_acidevocation_2";
                    break;
            }

            if (movingSummons)
            {
                shiftTemplate.summonActorRef += "_moving";
                shiftTemplate.summonOnCollidable = true;
                shiftTemplate.summonNoStacking = true;
                shiftTemplate.summonDuration = 7;
            }

        }

        int iAddedRange = 0;

        for (int idx = 0; idx < alllistEffectScriptsFromCaster.Count; idx++)
        {
            //if effect is SPELLSHAPE and we are spellshiftable...
            EffectScript currentEffect = alllistEffectScriptsFromCaster[idx];
            if (currentEffect is SpellShaperEffect && spellshift)
            {
                SpellShaperEffect currentSSE = currentEffect as SpellShaperEffect;

                //If we have a barrier up, we can't change the shape. So if this is a shape maker, we just move on.
                switch (currentSSE.spellShape)
                {
                    case ESpellShape.BURST:
                        if (bBarrierEffectOnCaster)
                        {
                            continue;
                        }
                        boundsShape = TargetShapes.BURST;
                        range = 2;
                        AddAbilityTag(AbilityTags.SIMULTANEOUSANIM);
                        AddAbilityTag(AbilityTags.GROUNDTARGET);
                        RemoveAbilityTag(AbilityTags.CURSORTARGET);
                        RemoveAbilityTag(AbilityTags.LINEOFSIGHTREQ);
                        AddAbilityTag(AbilityTags.PERTARGETANIM);
                        AddAbilityTag(AbilityTags.PLAYANIMONEMPTY);
                        foreach (EffectScript eff in listEffectScripts)
                        {
                            eff.tActorType = TargetActorType.ALL;
                        }
                        break;
                    case ESpellShape.CONE:
                        if (bBarrierEffectOnCaster)
                        {
                            continue;
                        }
                        boundsShape = TargetShapes.FLEXCONE;
                        range = 3;
                        AddAbilityTag(AbilityTags.SIMULTANEOUSANIM);
                        AddAbilityTag(AbilityTags.GROUNDTARGET);
                        RemoveAbilityTag(AbilityTags.CURSORTARGET);
                        RemoveAbilityTag(AbilityTags.LINEOFSIGHTREQ);
                        AddAbilityTag(AbilityTags.PERTARGETANIM);
                        AddAbilityTag(AbilityTags.CANROTATE);
                        AddAbilityTag(AbilityTags.PLAYANIMONEMPTY);
                        foreach (EffectScript eff in listEffectScripts)
                        {
                            eff.tActorType = TargetActorType.ALL;
                        }
                        break;
                    case ESpellShape.SQUARE:
                        if (bBarrierEffectOnCaster)
                        {
                            continue;
                        }
                        targetShape = TargetShapes.RECT;
                        AddAbilityTag(AbilityTags.SIMULTANEOUSANIM);
                        AddAbilityTag(AbilityTags.GROUNDTARGET);
                        AddAbilityTag(AbilityTags.PERTARGETANIM);
                        AddAbilityTag(AbilityTags.CANROTATE);
                        AddAbilityTag(AbilityTags.PLAYANIMONEMPTY);
                        foreach (EffectScript eff in listEffectScripts)
                        {
                            eff.tActorType = TargetActorType.ALL;
                        }
                        break;
                    case ESpellShape.RAY:
                        if (bBarrierEffectOnCaster)
                        {
                            continue;
                        }
                        boundsShape = TargetShapes.FLEXLINE;
                        range = 2;
                        //AddAbilityTag(AbilityTags.SIMULTANEOUSANIM);                        
                        RemoveAbilityTag(AbilityTags.SIMULTANEOUSANIM);
                        AddAbilityTag(AbilityTags.PLAYANIMONEMPTY);
                        AddAbilityTag(AbilityTags.PERTARGETANIM);
                        AddAbilityTag(AbilityTags.GROUNDTARGET);
                        RemoveAbilityTag(AbilityTags.CURSORTARGET);
                        RemoveAbilityTag(AbilityTags.CENTERED);
                        AddAbilityTag(AbilityTags.CANROTATE);
                        AddAbilityTag(AbilityTags.PLAYANIMONEMPTY);
                        foreach (EffectScript eff in listEffectScripts)
                        {
                            eff.tActorType = TargetActorType.ALL;
                            eff.animLength = 0.08f;
                        }
                        break;
                    case ESpellShape.LINE:
                        if (bBarrierEffectOnCaster)
                        {
                            continue;
                        }
                        targetShape = TargetShapes.FLEXLINE;
                        AddAbilityTag(AbilityTags.SIMULTANEOUSANIM);
                        AddAbilityTag(AbilityTags.PLAYANIMONEMPTY);
                        AddAbilityTag(AbilityTags.PERTARGETANIM);
                        AddAbilityTag(AbilityTags.GROUNDTARGET);
                        AddAbilityTag(AbilityTags.PERTARGETANIM);
                        AddAbilityTag(AbilityTags.CANROTATE);
                        AddAbilityTag(AbilityTags.PLAYANIMONEMPTY);
                        foreach (EffectScript eff in listEffectScripts)
                        {
                            eff.tActorType = TargetActorType.ALL;
                        }
                        break;
                    default:
                        //it's ok to be here if we aren't a shaping power but instead
                        //aura or penetrate, etc.
                        break;
                }

                iAddedRange += currentSSE.changeRange;
            }

            //if effect is ABILITYCOSTMODIFIER (hint: it is if it's also a spellshape modifier
            if (currentEffect is AbilityModifierEffect)
            {
                AbilityModifierEffect currentACME = currentEffect as AbilityModifierEffect;

                //Perhaps this effect doesn't work on this ability
                bool bEffectShouldApply = false;

                /*  Shep: Currently all templated abilites believe they are from CharacterJobs.BRIGAND
                 *  that value is only modified on abilities gained by the player during gameplay,
                 *  the templates are never changed.
                if (currentACME.jobGroupToModify == CharacterJobs.GENERIC ||
                    currentACME.jobGroupToModify == jobLearnedFrom)
                {
                    bEffectShouldApply = true;
                }
                */

                if (!bEffectShouldApply && currentACME.abilityRefsToModify.Count > 0)
                {
                    bEffectShouldApply = currentACME.abilityRefsToModify.Contains(refName);
                }

                // Our two core abilities should be immutable.
                if (refName == "skill_escapedungeon" || refName == "skill_regenflask")
                {
                    bEffectShouldApply = false;
                }

                if (bEffectShouldApply)
                {
                    staminaCost += currentACME.changeStaminaCost;
                    energyCost += currentACME.changeEnergyCost;
                    healthCost += currentACME.changeHealthCost;
                    spiritsRequired += currentACME.changeEchoCost;
                    chargeTime += currentACME.changeCTCost;

                    if (!string.IsNullOrEmpty(currentACME.strTextToRemoveFromDescription))
                    {
                        description = description.Replace(currentACME.strTextToRemoveFromDescription, "");
                    }

                    if (!string.IsNullOrEmpty(currentACME.strTextToAddToDescription))
                    {
                        description += "\n" + currentACME.strTextToAddToDescription;
                    }
                }
            }
        }

        //Add whatever range we calculated
        //We didn't add it on the fly because the shapes can change the range
        if (!bBarrierEffectOnCaster)
        {
            range += iAddedRange;
        }
    }


    public void SetBinding(KeyCode newBinding)
    {
        binding = newBinding;
    }

    public KeyCode GetBinding()
    {
        return binding;
    }

    public void SetMaxCooldown(int turns)
    {
        maxCooldownTurns = turns;
        SetCurCooldownTurns(0);
    }

    public void ChangeCurrentCooldown(int amount)
    {
        if (CheckAbilityTag(AbilityTags.COOLDOWN_ONLY_REQWEAPON))
        {
            if (GameMasterScript.heroPCActor.myEquipment.GetWeaponType() != reqWeaponType)
            {
                return;
            }
        }


        curCooldownTurns += amount;
        if (curCooldownTurns <= 0)
        {
            SetCurCooldownTurns(0);
        }

        //if (Debug.isDebugBuild) Debug.Log("Change cooldown of " + refName + " " + abilityName + " by " + amount + ", is now " + curCooldownTurns);
    }
    
    public void ResetCooldown()
    {
        SetCurCooldownTurns(maxCooldownTurns);
    }

    public virtual void CopyAbilityTags(AbilityScript from)
    {
        myAbilityTags = from.myAbilityTags;
        myAbilityTags2 = from.myAbilityTags2;       
    }

    public void AddAbilityTag(AbilityTags newTag)
    {
        if ((int)newTag < 64)
        {
            myAbilityTags |= (ulong)(1ul << (int)newTag);
        }
        else
        {
            myAbilityTags2 |= (ulong)(1ul << (int)newTag);
        }
    }

    public void RemoveAbilityTag(AbilityTags remTag)
    {
        if ((int)remTag < 64)
        {
            myAbilityTags &= ~((ulong)(1ul << (int)remTag));
            //myAbilityTags ^= (ulong)(1ul << (int)remTag);
        }
        else
        {
            myAbilityTags2 &= ~((ulong)(1ul << (int)remTag));
            //myAbilityTags2 ^= (ulong)(1ul << (int)remTag);
        }
    }

    public bool CheckAbilityTag(AbilityTags tag)
    {
        if ((int)tag < 64)
        {
            return (myAbilityTags & (ulong)(1ul << (int)tag)) != 0;
        }
        else
        {
            return (myAbilityTags2 & (ulong)(1ul << (int)tag)) != 0;
        }
    }
    /*
    // Called in 0 places
    //
    public int CheckAbilityTagByIndex(int index)
    {
        if (myAbilityTags[index] == true)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
    */
    public string GetDisplayCosts()
    {
        string builder = "";
        bool first = true;
        if (energyCost > 0)
        {
            builder += UIManagerScript.cyanHexColor + StringManager.GetString("stat_energy") + "</color>: " + energyCost;
            first = false;
        }
        if (staminaCost > 0)
        {
            if (!first) builder += ", ";
            builder += UIManagerScript.greenHexColor + StringManager.GetString("stat_stamina") + "</color>: " + staminaCost;
            first = false;
        }
        if (healthCost > 0)
        {
            if (!first) builder += ", ";
            builder += UIManagerScript.redHexColor + StringManager.GetString("misc_healthcost") + "</color>: " + healthCost;
            first = false;
        }
        if (spiritsRequired > 0)
        {
            if (!first) builder += ", ";
            builder += UIManagerScript.silverHexColor + StringManager.GetString("misc_echoes_required") + "</color>: " + spiritsRequired;
            first = false;
        }

        return builder;
    }
    public virtual void ConnectMissingReferencesAtLoad(bool isSubAbility = false)
    {
        //if (refName == "skill_holdthemoon_2") Debug.Log("Check " + isSubAbility);

        if (!isSubAbility)
        {
            foreach (AbilityScript abil in subAbilities)
            {
                abil.ConnectMissingReferencesAtLoad(true);
                abil.ParseNumberTags();
            }
        }

        foreach (string eRef in load_effectRefsToConnect)
        {
            EffectScript eff = GameMasterScript.GetEffectByRef(eRef);
            if (eff == null)
            {
                Debug.Log(refName + " couldn't find " + eRef);
            }
            else
            {

                EffectScript copyOfEff = AbilityScript.GetNewEffectFromTemplate(eff);
                AddEffectScript(copyOfEff);
            }
        }

    }

    // #todo - How to data drive this? :thonking:
    public bool CheckCooldownConditions(Fighter owner)
    {
        if (owner.GetActorType() == ActorTypes.HERO && refName.Contains("livingvine"))
        {
            if (owner.CheckSummonRefs("mon_summonedlivingvine") || owner.CheckSummonRefs("mon_summonedbulllivingvine"))
            {
                // Don't tick down Floraconda summons unless they are DEAD.
                Toggle(true);
                return false;
            }
            else
            {
                // If we don't have our living vine, we're not toggled
                Toggle(false);
            }
        }

        if (refName == "skill_flowshield" && owner.myStats.CheckHasStatusName("status_flowshield"))
        {
            return false;
        }
        else if (refName == "skill_voidshield" && owner.myStats.CheckHasStatusName("status_voidshield"))
        {
            return false;
        }

        return true;
    }

    public EffectScript TryGetEffectOfType(EffectType et)
    {
        foreach (EffectScript eff in listEffectScripts)
        {
            if (eff.effectType == et)
            {
                return eff;
            }
        }
        return null;
    }

    public string GetBoundsShapeText()
    {
        return StringManager.GetString("misc_shape_" + boundsShape.ToString().ToLowerInvariant());
    }

    public string GetTargetShapeText()
    {
        return StringManager.GetString("misc_shape_" + targetShape.ToString().ToLowerInvariant());
    }

    public void Toggle(bool state)
    {
        //Debug.Log("Set toggle state of " + refName + " to " + state);
        toggled = state;
    }

    public EffectScript GetFirstEffectOfType(EffectType eType)
    {
        foreach (EffectScript eff in listEffectScripts)
        {
            if (eff.effectType == eType)
            {
                return eff;
            }
        }

        Debug.Log("WARNING: " + refName + " does not have an effect of type " + eType);

        return null;
    }

    public string GetPassiveDescription()
    {
        if (!UsePassiveSlot)
        {
            if (string.IsNullOrEmpty(localizedFreePassive)) localizedFreePassive = StringManager.GetString("misc_freepassive");
            return description.Replace("^fp", localizedFreePassive);
        }

        return description.Replace("^fp", StringManager.GetString("misc_normalpassive"));
    }

    public bool UsePassiveSlot
    {
        get
        {
            if (RandomJobMode.IsCurrentGameInRandomJobMode() && RandomJobMode.DoesAbilityTakeSlotInRandomJobMode(refName)) return true;
            return usePassiveSlot;
        }
    }
}

public class AbilityUsageInstance
{
    public int abilityID;
    public AbilityScript abilityRef;

    public AbilityUsageInstance(int id, AbilityScript abilUsed)
    {
        abilityID = id;
        abilityRef = abilUsed;
    }
}

