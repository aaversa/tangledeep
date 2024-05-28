using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Text;

public class HoverInfoScript : MonoBehaviour {

    public static Dictionary<string, int> hoverActorStrings;
    int hoverTurnNumber;

    public static Actor currentHoveredActor;

    private static GameObject lastMonsterSelectIcon;

    /// <summary>
    /// This is the last dude we hit, or the last dude that hit us if we didn't have one, OR the last dude we pointed at
    /// via the analog stick.
    /// </summary>
    public static Monster lastMonsterSelectedViaCombatOrAnalogMovement;

    static StringBuilder reusableStringBuilder;
    static StringBuilder hostileTextBuilder;
    static StringBuilder verboseTextBuilder;

    /// <summary>
    /// The most recent monster we have been forced to update from
    /// </summary>
    static Monster lastMonsterRequest;

    static float timeOfLastMonsterRequest;

    static bool queuedText;

    static List<Actor> reusableListOfActors;

    public static MapTileData lastCheckedMTDFromAnalogTargeting;

    /// <summary>
    /// If at least this much time as passed since the last hover text force update, then do the update.
	/// Higher values are more performant
    /// </summary>
#if !UNITY_SWITCH	
    const float CHECK_REQUEST_TIME = 0.15f;
#else
    const float CHECK_REQUEST_TIME = 0.3f;
#endif

    public static HoverInfoScript singleton;

    void Start()
    {
        hoverActorStrings = new Dictionary<string, int>();
        reusableStringBuilder = new StringBuilder();
        hostileTextBuilder = new StringBuilder();
        verboseTextBuilder = new StringBuilder();
        reusableListOfActors = new List<Actor>();

        singleton = this;
    }

    public static void InitializeSelectIcon()
    {
        if (lastMonsterSelectIcon == null)
        {
            lastMonsterSelectIcon = Instantiate(UIManagerScript.singletonUIMS.hoverBarTargetingIcon);
            lastMonsterSelectIcon.SetActive(false);
        }
    }

    /// <summary>
    /// Make sure when we load/unload this scene, usually through adventure mode death,
    /// that we keep our object pointers up to date.
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Make sure when we load/unload this scene, usually through adventure mode death,
    /// that we keep our object pointers up to date.
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Make sure when we load/unload this scene, usually through adventure mode death,
    /// that we keep our object pointers up to date.
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode loadmoadchode)
    {
        /* if (lastMonsterSelectIcon == null)
        {
            lastMonsterSelectIcon = Instantiate(UIManagerScript.singletonUIMS.hoverBarTargetingIcon);
            lastMonsterSelectIcon.SetActive(false);
        } */
    }

    void Update()
    {
        if (lastMonsterSelectIcon == null)
        {
            lastMonsterSelectIcon = Instantiate(UIManagerScript.singletonUIMS.hoverBarTargetingIcon);
            lastMonsterSelectIcon.SetActive(false);
        }
        
        //check out the last actor we looked at. If it is dead, clear it away.
        if (lastMonsterSelectedViaCombatOrAnalogMovement != null)
        {
            if (Time.time - timeOfLastMonsterRequest >= CHECK_REQUEST_TIME && queuedText)
            {
                UIManagerScript.singletonUIMS.SetInfoText(
                    BuildHoverTextFromMonster(lastMonsterSelectedViaCombatOrAnalogMovement));

                queuedText = false;
            }
            else
            {
                return;
            }

            //Did homie die since our last update?
            var mon = lastMonsterSelectedViaCombatOrAnalogMovement;
            if (mon != null && mon.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.CUR) <= 0f)
            {
                ClearLastMonsterSelected();
                
                //and, close the bar just this turn. It'll re-open if we end up looking at something else,
                //but we don't want to hang on to lies
                UIManagerScript.singletonUIMS.HideGenericInfoBar();
            }
        }
    }

    /// <summary>
    /// Force the text to update, we call this if the last monster selected has had a status change or change in
    /// health.
    /// </summary>
    public static void ForceUpdateTextBasedOnLastMonsterSelected()
    {        
        if (lastMonsterSelectedViaCombatOrAnalogMovement != null)
        {
            lastMonsterRequest = lastMonsterSelectedViaCombatOrAnalogMovement;
            timeOfLastMonsterRequest = Time.time;
            queuedText = true;
            return;

            UIManagerScript.singletonUIMS.SetInfoText( 
                BuildHoverTextFromMonster(lastMonsterSelectedViaCombatOrAnalogMovement));
        }
    }

    /// <summary>
    /// Updates the hover bar based on monster info. Maybe it got hurt or the status changed?
    /// </summary>
    public static void TurnEndCleanup()
    {
        if (lastMonsterSelectedViaCombatOrAnalogMovement != null)
        {
            //did we lose this monster somehow?
            if( lastMonsterSelectedViaCombatOrAnalogMovement.dungeonFloor != GameMasterScript.heroPCActor.dungeonFloor ||
                lastMonsterSelectedViaCombatOrAnalogMovement.myStats.GetStat(StatTypes.HEALTH,StatDataTypes.CUR) <= 0 ||
                !GameMasterScript.heroPCActor.CanSeeActor(lastMonsterSelectedViaCombatOrAnalogMovement))
            {                
                ClearLastMonsterSelected();
            }
            else
            {
                ForceUpdateTextBasedOnLastMonsterSelected();
            }
        }
        else
        {
            //Debug.Log("Don't have a monster selected.");
        }
    }
    
    /// <summary>
    /// Removes the highlight from the last actor we've selected and nulls the value.
    /// </summary>
    static void ClearLastMonsterSelected()
    {        
        lastMonsterSelectedViaCombatOrAnalogMovement = null;
        if (lastMonsterSelectIcon != null)
        {
            lastMonsterSelectIcon.SetActive(false);
            lastMonsterSelectIcon.transform.SetParent(null);
            if (lastCheckedMTDFromAnalogTargeting != null)
            {
                UpdateHoverTextFromTile(lastCheckedMTDFromAnalogTargeting);
            }
            else
            {
                UIManagerScript.singletonUIMS.HideGenericInfoBar();
            }
            
        }
    }

    /// <summary>
    /// Set a new last-selected monster and force the text to update.
    /// </summary>
    /// <param name="m"></param>
    public static void SetLastMonsterSelected(Monster m)
    {
        if (lastMonsterSelectedViaCombatOrAnalogMovement != m)
        {
            //if (Debug.isDebugBuild) Debug.Log("Switching targets to " + m.actorRefName + ", analog stick state is " + TDInputHandler.AnalogStickHighlightingTile);
            lastMonsterSelectedViaCombatOrAnalogMovement = m;
            UIManagerScript.singletonUIMS.ShowGenericInfoBar();
            ForceUpdateTextBasedOnLastMonsterSelected();
            
            lastMonsterSelectIcon.SetActive(true);
            lastMonsterSelectIcon.transform.SetParent(lastMonsterSelectedViaCombatOrAnalogMovement.GetObject().transform);
            lastMonsterSelectIcon.transform.localPosition = Vector3.zero;
            lastMonsterSelectIcon.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            LeanTween.scale(lastMonsterSelectIcon, Vector3.one, 0.25f).setEaseInBounce();
        }
        
    }

    /// <summary>
    /// Evaluates a monster and creates hover text from it.
    /// </summary>
    /// <param name="mn"></param>
    /// <returns>A string full of monster info</returns>
    public static string BuildHoverTextFromMonster(Monster mn)
    {
        if (mn == null)
        {
            return null;
        }
        
        bool keenEyes = GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_keeneyes") ||
                        GameMasterScript.heroPCActor.myEquipment.GetOffhandRefName() == "armor_leg_dungeonguide";

        // Wild child tier 3 allows us to view more info about monsters
        bool wildChildTier3 = GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.WILDCHILD && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("wildchildbonus3");

        //string build = "";

        reusableStringBuilder.Length = 0;
        hostileTextBuilder.Length = 0;
        verboseTextBuilder.Length = 0;

        //string hostileText = "";
        //string verboseName = "";

        if (mn.actorRefName != "mon_itemworldcrystal")
        {
#if UNITY_EDITOR
            hostileTextBuilder.Append(" " + mn.actorUniqueID);
#endif

            if (mn.CheckTarget(GameMasterScript.heroPCActor))
            {
                //hostileText = "<color=red>" + StringManager.GetString("examine_monster_attitude_hostile") + "</color>";
                hostileTextBuilder.Append("<color=red>");
                hostileTextBuilder.Append(StringManager.GetString("examine_monster_attitude_hostile"));
                hostileTextBuilder.Append("</color>");                
            }
            else
            {
                if (mn.myBehaviorState == BehaviorState.CURIOUS || mn.myBehaviorState == BehaviorState.SEEKINGITEM)
                {
                    //hostileText = "<color=yellow>" + StringManager.GetString("examine_monster_attitude_curious") + "</color>";
                    hostileTextBuilder.Append("<color=yellow>");
                    hostileTextBuilder.Append(StringManager.GetString("examine_monster_attitude_curious"));
                    hostileTextBuilder.Append("</color>");

                }

                if (mn.myBehaviorState == BehaviorState.STALKING)
                {
                    //hostileText = UIManagerScript.orangeHexColor + StringManager.GetString("examine_monster_attitude_stalking") + "</color>";
                    hostileTextBuilder.Append(UIManagerScript.orangeHexColor);
                    hostileTextBuilder.Append(StringManager.GetString("examine_monster_attitude_stalking"));
                    hostileTextBuilder.Append("</color>");
                }

                if (hostileTextBuilder.Length == 0)
                {
                    if (mn.aggroRange > 0)
                    {
                        //hostileText = UIManagerScript.orangeHexColor + StringManager.GetString("examine_monster_attitude_aggressive") + "</color>";
                        hostileTextBuilder.Append(UIManagerScript.orangeHexColor);
                        hostileTextBuilder.Append(StringManager.GetString("examine_monster_attitude_aggressive"));
                        hostileTextBuilder.Append("</color>");
                    }
                    else
                    {
                        //hostileText = UIManagerScript.greenHexColor + StringManager.GetString("examine_monster_attitude_neutral") + "</color>";
                        hostileTextBuilder.Append(UIManagerScript.greenHexColor);
                        hostileTextBuilder.Append(StringManager.GetString("examine_monster_attitude_neutral"));
                        hostileTextBuilder.Append("</color>");
                    }
                }
            }

            reusableStringBuilder.Append(" (");
            reusableStringBuilder.Append(hostileTextBuilder.ToString());
            reusableStringBuilder.Append(")");
            hostileTextBuilder.Length = 0;
            hostileTextBuilder.Append(reusableStringBuilder.ToString());
            //hostileText = " (" + hostileText + ")";
            hostileTextBuilder.Append(", ");
            hostileTextBuilder.Append(mn.EvaluateThreatToPlayer());

            //hostileText += ", " + mn.EvaluateThreatToPlayer();

            //verboseName = UIManagerScript.cyanHexColor + Monster.GetFamilyName(mn.monFamily) + "</color> "; // Nothing
            verboseTextBuilder.Append(UIManagerScript.cyanHexColor);
            verboseTextBuilder.Append(Monster.GetFamilyName(mn.monFamily));
            verboseTextBuilder.Append("</color> ");

            if (wildChildTier3)
            {
                //verboseName += "[" + mn.GetUntamedMonsterRarityString() + "] ";
                verboseTextBuilder.Append("[");
                verboseTextBuilder.Append(mn.GetUntamedMonsterRarityString());
                verboseTextBuilder.Append("] ");
            }
        }
        else
        {
            /* hostileText = " " + UIManagerScript.cyanHexColor + "(" + StringManager.GetString("examine_monster_aura") +
                          ": " + EffectScript.itemWorldAuraDescriptions[mn.ReadActorData("itemworldaura")] +
                          ")</color>"; */

            hostileTextBuilder.Append(" ");
            hostileTextBuilder.Append(UIManagerScript.cyanHexColor);
            hostileTextBuilder.Append(StringManager.GetString("examine_monster_aura"));
            hostileTextBuilder.Append(": ");
            hostileTextBuilder.Append(EffectScript.itemWorldAuraDescriptions[mn.ReadActorData("itemworldaura")]);
            hostileTextBuilder.Append(")</color>");
            
            //verboseName = "";
            verboseTextBuilder.Length = 0;
        }



        //if (!keenEyes)

        int displayStat = (int) (mn.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) * 100f);
        if (displayStat == 0)
        {
            if (mn.myStats.IsAlive())
            {
                displayStat = 1;
            }
            else
            {
                displayStat = 0;
            }
        }

        string hpDisplay;

        if (displayStat >= 80)
        {
            hpDisplay = UIManagerScript.greenHexColor + displayStat;
        }
        else if (displayStat >= 55)
        {
            hpDisplay = UIManagerScript.orangeHexColor + displayStat;
        }
        else if (displayStat >= 35)
        {
            hpDisplay = "<color=yellow>" + displayStat;
        }
        else
        {
            hpDisplay = "<color=red>" + displayStat;
        }

        if (!keenEyes)
        {
            //build = mn.displayName + hostileText + ", " + verboseName + hpDisplay + "%</color> " + StringManager.GetString("misc_hp"); // hp moved to top line
            reusableStringBuilder.Length = 0;
            reusableStringBuilder.Append(mn.displayName);
            reusableStringBuilder.Append(hostileTextBuilder.ToString());
            reusableStringBuilder.Append(", ");
            reusableStringBuilder.Append(verboseTextBuilder.ToString());
            reusableStringBuilder.Append(hpDisplay);
            reusableStringBuilder.Append("%</color> ");
            reusableStringBuilder.Append(StringManager.GetString("misc_hp"));
            // verbose name was on the bottom line, moved to top
        }

        //else
        if (keenEyes)
        {
            int cur = (int) mn.myStats.GetCurStat(StatTypes.HEALTH);
            int max = (int) mn.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX);
            int level = mn.myStats.GetLevel();

             /* build = mn.displayName + hostileText + " (" + StringManager.GetString("ui_level_shorthand") + " " + level +
                    ") " + verboseName +
                    cur + "/" + max + " (" + hpDisplay + "%)</color> " +
                    StringManager.GetString("misc_hp"); // HP is now on top line again.  */

            reusableStringBuilder.Length = 0;
            reusableStringBuilder.Append(mn.displayName);
            reusableStringBuilder.Append(hostileTextBuilder.ToString());
            reusableStringBuilder.Append(" (");
            reusableStringBuilder.Append(StringManager.GetString("ui_level_shorthand"));
            reusableStringBuilder.Append(" ");
            reusableStringBuilder.Append(level.ToString());
            reusableStringBuilder.Append(") ");
            reusableStringBuilder.Append(verboseTextBuilder.ToString());
            reusableStringBuilder.Append(cur.ToString());
            reusableStringBuilder.Append("/");
            reusableStringBuilder.Append(max.ToString());
            reusableStringBuilder.Append(" (");
            reusableStringBuilder.Append(hpDisplay);
            reusableStringBuilder.Append("%)</color> ");
            reusableStringBuilder.Append(StringManager.GetString("misc_hp"));
                

            // verbose name was on the bottom line, moved to top
        }

        //build += mn.GetMonsterResistanceString(keenEyes);
        /* if (Debug.isDebugBuild)
        {
            string str = mn.GetMonsterResistanceString(keenEyes);
            Debug.Log("The string is... " + str);
        } */
        reusableStringBuilder.Append(mn.GetMonsterResistanceString(keenEyes));

        if (mn.isChampion)
        {
            int modCount = 0;
            foreach (ChampionMod cm in mn.championMods)
            {
                if (!cm.displayNameOnHover) continue;
                if (modCount == 0)
                {
                    //build += " (" + cm.displayName;
                    reusableStringBuilder.Append(" (");
                    reusableStringBuilder.Append(cm.displayName);
                }
                else if (modCount > 0 && modCount < mn.championMods.Count)
                {
                    //build += ", " + cm.displayName;
                    reusableStringBuilder.Append(", ");
                    reusableStringBuilder.Append(cm.displayName);
                }

                modCount++;
            }

            if (modCount > 0)
            {
                //build += ")";
                reusableStringBuilder.Append(")");
            }

        }

        string dStatus = mn.myStats.GetDisplayStatuses(keenEyes);

        if (dStatus != "")
        {
            //build += " (" + dStatus + ")";
            reusableStringBuilder.Append(" (");
            reusableStringBuilder.Append(dStatus);
            reusableStringBuilder.Append(")");
        }

#if UNITY_EDITOR
        //Keep this at the end of the list of items displayed on the monster hover text
        //otherwise it gets confusing
        if (DebugConsole.IsOpen)
        {
            //build += " ID:" + mn.actorUniqueID + " Pos: " + mn.GetPos();
            reusableStringBuilder.Append(" ID:");
            reusableStringBuilder.Append(mn.actorUniqueID);
            reusableStringBuilder.Append(" Pos: ");
            reusableStringBuilder.Append(mn.GetPos());
        }
#endif
        //return build;
        return reusableStringBuilder.ToString();
    }
    
    public static string GetHoverText(MapTileData mtd, bool shouldSetSelectedActorAsLastActorSelected = false)
    {
        if (mtd == null) return "";

        reusableListOfActors.Clear();

        foreach(Actor act in mtd.GetAllActors())
        {
            reusableListOfActors.Add(act);
        }

        if (reusableListOfActors.Count == 0)
        {
            currentHoveredActor = null;
            return "";
        }

        string build = "";
        bool stopped = false;

        hoverActorStrings.Clear();

        bool terrainDescribed = false;

        bool hittableTarget = false;
        bool talkTarget = false;

        //Hovered monsters take priority over any other hover item.
        Actor hoveredMonster = null;

        foreach (Actor act in reusableListOfActors)
        {
            if (stopped)
            {
                break;
            }
            if (!act.actorEnabled)
            {
                continue;
            }

            switch (act.GetActorType())
            {
                case ActorTypes.MONSTER:
                    Monster mn = act as Monster;
                    
                    //track this homie as the last homie we looked at.
                    if (shouldSetSelectedActorAsLastActorSelected)
                    {
                        SetLastMonsterSelected(mn);
                    }
                    
                    if (mn.actorfaction != Faction.PLAYER)
                    {
                        hittableTarget = true;
                    }

                    build = BuildHoverTextFromMonster(mn);
                    build += "\n"; // monsters get their own line(s)

                    if (hoverActorStrings.ContainsKey(build))
                    {
                        hoverActorStrings[build]++;
                    }
                    else
                    {
                        hoverActorStrings.Add(build, 1);
                    }

                    //We are hovering over this.
                    hoveredMonster = act;
                    currentHoveredActor = act;

                    break;
                case ActorTypes.STAIRS:
                    Stairs st = act as Stairs;
                    /* if ((!st.usedByPlayer) && (st.newLocation.floor > 100) && (!st.newLocation.IsTownMap()))
                   {
                       if (actCounted == 0)
                       {
                           if (st.isPortal)
                           {

                           }
                           buildText = "Stairs";
                       }
                       else
                       {a
                           buildText += ", Stairs";
                       }
                   } 
                   else */
                    {
                        if (st.ReadActorData("finalstairs") == 1 || st.NewLocation == null)
                        {
                            StringManager.SetTag(0, "??????");
                        }
                        else
                        {
                            StringManager.SetTag(0, st.NewLocation.GetName());
                        }

                        
                        if (st.isPortal)
                        {
                            build = StringManager.GetString("misc_portal_info");
                        }
                        else
                        {
                            build = StringManager.GetString("misc_stairs_info");
                        }
                    }

                    if (hoverActorStrings.ContainsKey(build))
                    {
                        hoverActorStrings[build]++;
                    }
                    else
                    {
                        hoverActorStrings.Add(build, 1);
                    }
                    //We are hovering over this.
                    currentHoveredActor = act;

                    break;
                case ActorTypes.HERO:
                    build = UIManagerScript.greenHexColor + GameMasterScript.heroPCActor.displayName + "</color>";

                    if (hoverActorStrings.ContainsKey(build))
                    {
                        hoverActorStrings[build]++;
                    }
                    else
                    {
                        hoverActorStrings.Add(build, 1);
                    }
                    //We are hovering over this.
                    currentHoveredActor = act;

                    break;
                case ActorTypes.POWERUP:

                    build = StringManager.GetString("misc_powerup");
                    if (hoverActorStrings.ContainsKey(build))
                    {
                        hoverActorStrings[build]++;
                    }
                    else
                    {
                        hoverActorStrings.Add(build, 1);
                    }
                    //We are hovering over this.
                    currentHoveredActor = act;

                    break;
                case ActorTypes.ITEM:
                    Item i = act as Item;
                    build = act.displayName + i.GetQuantityText();
                    if (DebugConsole.IsOpen)
                    {
                        build += " ID:" + act.actorUniqueID;
                    }
                    if (hoverActorStrings.ContainsKey(build))
                    {
                        hoverActorStrings[build]++;
                    }
                    else
                    {
                        hoverActorStrings.Add(build, 1);
                    }
                    //We are hovering over this.
                    currentHoveredActor = act;

                    break;
                case ActorTypes.NPC:
                    NPC n = act as NPC;
                    if (!n.hoverDisplay) continue;

                    talkTarget = true;

                    build = act.displayName;
                    if (hoverActorStrings.ContainsKey(build))
                    {
                        hoverActorStrings[build]++;
                    }
                    else
                    {
                        hoverActorStrings.Add(build, 1);
                    }

                    //We are hovering over this.
                    currentHoveredActor = act;

                    break;
                case ActorTypes.DESTRUCTIBLE:
                    Destructible dt = act as Destructible;
                    if (!dt.hoverDisplay || dt.destroyed || dt.isDestroyed)
                    {
                        continue; // 142019: We want to continue searching all actors in the tile, not break
                    }

                    string startColor = "";
                    string endColor = "";

                    string spiritText = "";
                    if (!String.IsNullOrEmpty(dt.monsterAttached))
                    {
                        // Used for spirits
                        StringManager.SetTag(0, MonsterManagerScript.GetMonsterDisplayNameByRef(dt.monsterAttached));
                        spiritText = " " + StringManager.GetString("misc_echo_pickup") + " ";
                    }

                    if (dt.summoner != null)
                    {
                        if (dt.summoner.actorfaction == Faction.PLAYER)
                        {
                            startColor = UIManagerScript.greenHexColor;
                        }
                        else
                        {
                            startColor = UIManagerScript.redHexColor;
                        }
                        endColor = "</color>";
                    }

                    string nameStuff = act.displayName;

                    if (!terrainDescribed)
                    {
                        if (mtd.CheckTag(LocationTags.MUD))
                        {
                            nameStuff += " <color=yellow>(" + StringManager.GetString("misc_mud_effect") + ")</color>";
                            terrainDescribed = true;
                        }
                        if (mtd.CheckTag(LocationTags.LAVA))
                        {
                            nameStuff += " <color=yellow>(" + StringManager.GetString("misc_lava_effect") + ")</color>";
                            terrainDescribed = true;
                        }
                        if (mtd.CheckTag(LocationTags.ELECTRIC))
                        {
                            nameStuff += " <color=yellow>(" + StringManager.GetString("misc_electric_effect") + ")</color>";
                            terrainDescribed = true;
                        }
                        if (mtd.CheckTag(LocationTags.WATER))
                        {
                            nameStuff += " <color=yellow>(" + StringManager.GetString("misc_water_effect") + ")</color>";
                            terrainDescribed = true;
                        }
                    }

                    if (dt.targetable)
                    {
                        hittableTarget = true;
                    }


                    build = startColor + nameStuff + spiritText + endColor;

                    if (DebugConsole.IsOpen)
                    {
                        build += " ID:" + act.actorUniqueID;
                        for (int x = 0; x < (int)ObjectFlags.COUNT; x++)
                        {
                            int amt = mtd.GetObjectFlagAmount((ObjectFlags)x);
                            if (amt > 0)
                            {
                                build += " " + (ObjectFlags)x + ": " + amt;
                            }
                        }
                    }
                                           
                    if (hoverActorStrings.ContainsKey(build))
                    {
                        hoverActorStrings[build]++;
                    }
                    else
                    {
                        hoverActorStrings.Add(build, 1);
                    }

                    //We are hovering over this.
                    currentHoveredActor = act;

                    break;
            }
        }

        //choose the monster as the focus
        if (hoveredMonster != null)
        {
            currentHoveredActor = hoveredMonster;
        }


        bool first = true;
        bool monsterFirst = false;
        build = "";

        // If a MONSTER is first in the tile list, they get their own line, which means
        // They don't need comma logic afterward.
        
        // 142019 - If a monster was on a tile with a bunch of destructibles, the code would overwrite the monster text and only show the destructibles.
        // Use of monsterFirst means we will always ADD to the string instead of overwriting, with proper comma logic. This works on PC.

        int index = 0;
        foreach (string display in hoverActorStrings.Keys)
        {
            if (index == 0 && reusableListOfActors[0].GetActorType() == ActorTypes.MONSTER)
            {
                build += display;
                first = false;
                monsterFirst = true;
                index++;
                continue;
            }
            index++;
            if (hoverActorStrings[display] == 1) // Only one of this string
            {
                if (first || monsterFirst)
                {
                    build += display;
                    first = false;
                    monsterFirst = false;
                }
                else
                {
                    build += ", " + display;
                }
            }
            else
            {
                // Multiple strings, so we display (3x), i.e. 3 of the same item
                if (first || monsterFirst)
                {
                    build += display + " x" + hoverActorStrings[display];
                    first = false;
                    monsterFirst = false;
                }
                else
                {
                    build += ", " + display + " x" + hoverActorStrings[display];
                }
            }
        }

        if (hittableTarget)
        {
            int dist = MapMasterScript.GetGridDistance(mtd.pos, GameMasterScript.heroPCActor.GetPos());
            bool canHitTarget = true;

            if (dist > 1)
            {
                if (GameMasterScript.heroPCActor.HasAnyRangedWeapon())
                {
                    int wRange = GameMasterScript.heroPCActor.GetMaxWeaponRange();
                    if (wRange >= dist)
                    {
                        CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.RANGED);
                    }                                        
                    else
                    {
                        canHitTarget = false;
                    }
                }
                else
                {
                    canHitTarget = false;
                }
            }
            else
            {
                CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.ATTACK);
            }
            if (!canHitTarget)
            {
                CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.NORMAL);
            }
        }
        else if (talkTarget)
        {
            CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.TALK);
        }
        else
        {
            if (CursorManagerScript.GetCursorType() != CursorSpriteTypes.TARGET)
            {
                CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.NORMAL);
            }            
        }

        if (GameMasterScript.IsAnimationPlaying())
        {
            CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.NORMAL);
        }

        return build;
    }

    public static void UpdateHoverTextFromTile(MapTileData mtd)
    {
        //update info based on that. Cache any monster in there if we are looking at one.
        string checkText = GetHoverText(lastCheckedMTDFromAnalogTargeting, true);
        if (!string.IsNullOrEmpty(checkText))
        {
            UIManagerScript.singletonUIMS.ShowGenericInfoBar();
            UIManagerScript.singletonUIMS.SetInfoText(checkText);
        }
        //If we're not pointing at anything, show the boss bar if necessary.
        else if (BossHealthBarScript.healthBarShouldBeActive)
        {
            //this will toggle the boss bar for us
            UIManagerScript.singletonUIMS.HideGenericInfoBar();
        }
        //otherwise, if we had once targeted a monster, show him
        else if (lastMonsterSelectedViaCombatOrAnalogMovement != null)
        {
            UIManagerScript.singletonUIMS.ShowGenericInfoBar();
            UIManagerScript.singletonUIMS.SetInfoText(BuildHoverTextFromMonster(lastMonsterSelectedViaCombatOrAnalogMovement));
        }
        //meh
        else
        {
            UIManagerScript.singletonUIMS.HideGenericInfoBar();
        }
    }
}

public partial class UIManagerScript : MonoBehaviour
{
    private Vector3 lastMouseTouchPosition;

    public GameObject hoverBarTargetingIcon;
    
    public void UpdateHoverBarState()
    {
        if (MiniMapScript.cursorIsHoveringOverMap) // Minimap hover takes priority.
        {
            return;
        }

        if (abilityTargeting)
        {
            if (abilityInTargeting != GameMasterScript.rangedWeaponAbilityDummy)
            {
                UIManagerScript.singletonUIMS.ShowGenericInfoBar();
            }
        }

        //if (virtualCursorPosition != lastHoverPosition)
        hoverUpdateFrames++;
        if (hoverUpdateFrames > 10)
        {
            hoverUpdateFrames = 0;
            //Debug.Log("Non matching cursor position");
            if (genericInfoBar.activeSelf)
            {
                //Debug.Log("Looking further to move the bar");
                float mPos = GameMasterScript.cameraScript.GetComponent<Camera>().WorldToScreenPoint(virtualCursorPosition).y;

                //If we are actively moving the target cursor around, send the info bar to the bottom 
                //if the cursor is near the top of the screen.
                if (!forceInfoBarTop &&
                    abilityTargeting &&
                    mPos >= Screen.height * 0.87f)
                {
                    // Move bar to bottom
                    if (!infoBarAtBottom)
                    {
                        genericInfoBarTransform.anchorMin = new Vector2(0f, 0f);
                        genericInfoBarTransform.anchorMax = new Vector2(1f, 0f);
                        infoBarAtBottom = true;
                    }

                    genericInfoBarTransform.anchoredPosition = new Vector3(0f, 250f, 0);

                }
                else
                {
                    if (infoBarAtBottom)
                    {
                        genericInfoBarTransform.anchorMin = new Vector2(0f, 1f);
                        genericInfoBarTransform.anchorMax = new Vector2(1f, 1f);
                        infoBarAtBottom = false;
                    }

                    genericInfoBarTransform.anchoredPosition = new Vector3(0f, -50f, 0);
                }
            }

            if (abilityTargeting && abilityInTargeting != GameMasterScript.rangedWeaponAbilityDummy)
            {
                var cursorPos = virtualCursorPosition;
                
                //If touch this frame is different from touch last frame, move the cursor.
                //Because we should move the cursor in the UpdateHoverBarState function.
                if (Input.mousePosition != lastMouseTouchPosition)
                {
                    lastMouseTouchPosition = Input.mousePosition;
                    Vector2 touchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    cursorPos = new Vector2((float)Math.Floor(touchPosition.x + 0.5f), (float)Math.Floor(touchPosition.y + 0.5f));
                }
                
                //if we've changed positions from last time, do some updating here
                if (cursorPos != lastCursorPosition)
                {
                    //clamp virtual cursor to map bounds
                    cursorPos.x = Mathf.Clamp(cursorPos.x, 0, MapMasterScript.activeMap.columns - 1);
                    cursorPos.y = Mathf.Clamp(cursorPos.y, 0, MapMasterScript.activeMap.rows - 1);

                    //set the position
                    singletonUIMS.SetVirtualCursorPosition(cursorPos);
                }

                //grab the tile we are now looking at
                MapTileData checkMTD = MapMasterScript.GetTile(virtualCursorPosition);

                //update info based on that.
                string checkText = HoverInfoScript.GetHoverText(checkMTD);
                UIManagerScript.singletonUIMS.SetInfoText(!string.IsNullOrEmpty(checkText)
                    ? checkText
                    : UIManagerScript.bufferInfoBarText);
                
                return;
            }

            bool examiningOrTargeting = true;

            // Check for hover over monsters/objects - display info.
            if (!examineMode && !abilityTargeting)
            {
                if (Cursor.visible)
                {
                    examiningOrTargeting = false;
                    Vector3 basePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    SetVirtualCursorPosition(new Vector2((int)Math.Floor(basePosition.x + 0.5f), (int)Math.Floor(basePosition.y + 0.5f)));
                    //Debug.Log("Adjusted cursor position to " + basePosition + " " + GetVirtualCursorPosition());
                }        
                else
                {
                    // :thinking:
                    examiningOrTargeting = false;
                }        
            }

            bool forceTarget = false;
            if (lastHeroTarget != null && !abilityTargeting && !examineMode) // Force focus to last hit target.
            {
                SetVirtualCursorPosition_Internal(lastHeroTarget.GetPos());
                forceTarget = true;
            }

            /* Debug.Log(forceTarget + " " + abilityInTargeting + " " + examineMode + " " + examiningOrTargeting + " " + forceShowInfoBar +
                " vcps " + virtualCursorPosition + " " + lastHoverPosition);  */

            if (virtualCursorPosition == lastHoverPosition && hoverTurnNumber == GameMasterScript.turnNumber)
            {
                if (!forceTarget)
                {
                    return;
                }
            }
            else if (forceTarget)
            {
                if (TDInputHandler.PhysicalMouseTouched(1.0f) && !abilityTargeting)
                {
                    lastPhysicalMousePosition = Input.mousePosition;
                    SetLastHeroTarget(null);
                }
            }

            if (!playerHUDEnabled) return;
            if (!GameMasterScript.gameLoadSequenceCompleted) return;
            // Info bar positioning WAS here.

            //Debug.Log(Cursor.visible + " " + forceTarget + " " + examiningOrTargeting + " " + forceShowInfoBar);

            if (forceShowInfoBar)
            {
                return;
            }

            //Debug.Log(forceTarget + " " + examiningOrTargeting + " " + Cursor.visible + " " + examineMode + " " + examiningOrTargeting);

            if (!forceTarget && !examiningOrTargeting) 
            {
                if (!Cursor.visible && !examineMode && !examiningOrTargeting)
                {
                    //Don't hide the bar anymore
                    //Debug.Log("Bar shouldn't show here");
                    return;
                }
            }        

            lastHoverPosition = virtualCursorPosition;

            virtualCursorPosition.x = Mathf.Clamp(virtualCursorPosition.x, 0, MapMasterScript.activeMap.columns - 1);
            virtualCursorPosition.y = Mathf.Clamp(virtualCursorPosition.y, 0, MapMasterScript.activeMap.rows - 1);

            MapTileData mtd = MapMasterScript.GetTile(virtualCursorPosition);

            //Debug.Log("Check " + mtd.pos);

            bool visibleInArray = GameMasterScript.heroPCActor.visibleTilesArray[(int)mtd.pos.x, (int)mtd.pos.y];

            // Below check is too pricey to run constantly, and our regular LOS routine should have covered it anyway.
            //bool visibleStraightLOS = MapMasterScript.CheckTileToTileLOS(GameMasterScript.heroPCActor.GetPos(), mtd.pos, GameMasterScript.heroPCActor, MapMasterScript.activeMap);

            if (!visibleInArray && !MapMasterScript.activeMap.dungeonLevelData.revealAll)
            {
                //Debug.Log("Can't see it, skipping");
                HideGenericInfoBar();
                return;
            }

            //Debug.Log("Run an update.");
            if (TDInputHandler.AnalogStickHighlightingTile) return;

            UpdateHoverBarTextWithInfoFromTile(mtd);
        }
    }

    public void UpdateHoverBarTextWithInfoFromTile(MapTileData mtd, bool ignoreIfEmpty = false, bool overwriteBuffer = false)
    {
        string hoverText = HoverInfoScript.GetHoverText(mtd);

        if (string.IsNullOrEmpty(hoverText))
        {
            if (ignoreIfEmpty)
            {
                return;
            }
            // Nothing was counted.
            HideGenericInfoBar();
            CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.NORMAL);
        }
        else
        {
            ShowGenericInfoBar();
            SetInfoText(hoverText);

            if (overwriteBuffer)
            {
                bufferInfoBarText = hoverText;
            }
        }
    }
    /// <summary>
    /// Restores buffered text to the info bar. If there isn't any, bar is hidden.
    /// </summary>
    public void RequestClearInfoBar()
    {
        if (string.IsNullOrEmpty(bufferInfoBarText))
        {
            HideGenericInfoBar();
        }
        else
        {
            singletonUIMS.SetInfoText(bufferInfoBarText);
        }        
    }


}