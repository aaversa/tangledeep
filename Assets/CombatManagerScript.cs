using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
using System.Reflection;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    using UnityEngine.Analytics;
#endif

public enum CombatResult { HIT, DODGE, MONSTERDIED, PLAYERDIED, WAITFORANIM, NOTHING, COUNT };
public enum AttackType { ATTACK, ABILITY };

public class CombatAnimation
{
    public Vector2 origPos;
    public Vector2 targetPos;
    public EffectScript genEffect;
    public BattleTextData btd;
    public string effectRef;
    public float animLength;
    public GameObject targetObject;
    public bool forceSilentFX;
    public bool effIsDefinitelyNull;
    public bool battleTextAttached;

    public CombatAnimation(Vector2 atk, Vector2 def, EffectScript eff, BattleTextData newBtd, GameObject obj, bool forceSilent = false, bool effIsKnownToBeNull = false, bool hasBattleText = false)
    {
        origPos = atk;
        targetPos = def;
        genEffect = eff;
        btd = newBtd;
        targetObject = obj;
        animLength = 0.01f;
        forceSilentFX = forceSilent;
        effIsDefinitelyNull = effIsKnownToBeNull;
        battleTextAttached = hasBattleText;
    }
}

public class CombatResultPayload
{
    public CombatResult result;
    public float waitTime;
}

// This is used to pass data on a current attack / ability sequence so actors can process it and spit a number back.
public class CombatDataPack
{
    public float damage;
    public float lastDamageAmountReceived;
    public float blockedDamage;
    public float damageMod;
    public float damageModPercent;
    public float attackAngle;
    public Directions attackDirection;
    public int numAttacks;
    public AbilityScript ability;
    public EffectScript effect;
    public Fighter attacker;
    public Fighter defender;
    public DamageTypes damageType;
    //public DamageTypes weaponDamageType;
    public FlavorDamageTypes flavorDamageType;
    public Weapon attackerWeapon;
    public AttackType atkType;
    public float mitigationPercent;
    public float accuracyMod;
    public float parryMod;
    public float accuracyModFlat;
    public float parryModFlat;
    public float blockMod;
    public float blockModFlat;
    public float chanceToCrit;
    public float chanceToCritFlat;
    public bool silent; // Add extra text etc?
    public bool counterAttack;
    public bool criticalHit;
    public float healValue;

    public float damageCapForPayload;

    public float addToWaitTime;

    public CombatDataPack()
    {
        ResetData();
    }

    public void ResetData()
    {
        lastDamageAmountReceived = 0.0f;
        damageCapForPayload = 0.0f; // if greater than 0, the next Damage Payload cannot exceed this.
        damage = 0.0f;
        blockedDamage = 0.0f;
        damageMod = 0.0f;
        attackAngle = 0.0f;
        attacker = null;
        defender = null;
        ability = null;
        effect = null;
        healValue = 0.0f;
        mitigationPercent = 0.0f;
        silent = false;
        accuracyMod = 1.0f;
        parryMod = 1.0f;
        damageModPercent = 1.0f;
        accuracyModFlat = 0.0f;
        parryModFlat = 0.0f;
        chanceToCrit = 1.0f;
        chanceToCritFlat = 0.0f;
        blockMod = 1.0f;
        blockModFlat = 0.0f;
        addToWaitTime = 0.0f;
        criticalHit = false;
    }
}

[System.Serializable]
public partial class CombatManagerScript : MonoBehaviour
{

    // Global damage variance
    public static float damageVariance = 0.05f;

    public static ProcessDamagePayload damagePayload;

    private static BattleTextManager btm;

    private static UIManagerScript uims;
    private static BattleTextData bufferBattleTextData;
    public static float combatAnimationTime;

    public static CombatManagerScript cmsInstance;

    public static CombatDataPack bufferedCombatData;

    private static List<CombatDataPack> combatStack;
    public static float accumulatedCombatWaitTime;

    private static Queue<CombatAnimation> queuedAnimations;
    private static Queue<CombatAnimation> queuedText;

    public const float CHANCE_BOWMASTERY3 = 0.18f;
    public const float CHANCE_SPEARMASTERY3 = 0.15f;
    public const float CHANCE_WEAPON_SHADOW_DEBUFF = 0.25f;
    public const int THANESONG_INTENSITY_ADVANCE_THRESHOLD = 7;
    public const int THANESONG_INTENSITY_ADVANCE_THRESHOLD_WITH_MASTERY = 5;
    public const int THANE_MASTERY3_COOLDOWN = 20;
    const float CHANCE_ATHYES_FIRESTUN = 0.15f;
    const float CHANCE_LEGHARP_EXTRA_ATTACK = 0.5f;

    const float BASE_CHANCE_TWOARROWS_ATTACK = 1f;

    const float BASE_CHANCE_THREEARROWS_ATTACK = 0.5f;
    const float CHANCE_LEGSHARD_ABSORB = 0.15f;
    const float MIN_DAMAGE_TO_PROLONG_THANE_SONG = 0.05f;
    const float MIN_BOSS_DAMAGE_TO_PROLONG_THANE_SONG = 0.03f;
    public const float CRIT_CHANCE_MAX = 1.0f;
    public const float CHANCE_WATER_PROJECTILE_DODGE = 0.3f;
    public const float PROC_SONG_MIGHT_LEVEL3 = 0.15f;
    public const float PLAYER_PET_GROUNDEFFECT_MITIGATION = 0.5f;
    public const float BUDOKA_HAMEDO_CHANCE = 0.15f;
    public const float RELIC_HUNTER_GEARBONUS = 0.025f;
    public const float MENAGERIE_PET_COMBAT_BONUS = 0.04f;
    public const float MAX_DAMAGE_VALUE = 9999f;
    public const float DAGGER_CRIT_CT = 35f;

    public const float WHIP_MASTERY3_BONUSCRIT_PER_ENEMY = 0.03f;
    public const float CHANCE_ECHOING_EXTRA_ATTACK = 0.2f;
    public const float CHANCE_ABSORBINGMOD_NEGATE_CRIT = 0.75f;
    public const float LONE_WOLF_DMG_MULT = 0.06f;
    public const float LONE_WOLF_DEF_MULT = -0.02f;

    public static string[] bluntDamageWords;
    public static string[] slashDamageWords;
    public static string[] pierceDamageWords;
    public static string[] biteDamageWords;
    public static string[] fireDescriptors;
    public static string[] waterDescriptors;
    public static string[] shadowDescriptors;
    public static string[] lightningDescriptors;
    public static string[] poisonDescriptors;
    public static string[] physicalDescriptors;

    public static string[] verboseDamageTypes;

    public static Sprite[] thaneAuraSprites;

    public static List<MapTileData> pool_tileList;
    public static List<Fighter> pool_targetList;

    static AbilityScript abilityBeingUsed;
    static int abilityBeingUsedID;

    static BonusAttackPackage staticBonusAttackPackage;
    static bool staticBonusCreated = false;

    string[] AssignFlavorWords(string strType)
    {
        var listWords = new List<string>();
        int idx = 1;
        bool bContinue = true;
        while (bContinue)
        {
            string strRef = "dmg_words_" + strType + "_" + idx;
            string strGet = StringManager.GetString(strRef);

            //If these are identical, then the tag doesn't exist, and we are done.
            //If you added a damage word, but didn't use the existing format, or decided to
            //skip an index number, or deleted a number from the list without adjusting it,
            //go look in the mirror and cry hot tears of shame.
            if (strGet != strRef)
            {
                listWords.Add(strGet);
                idx++;
            }
            else
            {
                bContinue = false;
            }
        }

        return listWords.ToArray();
    }

    // Use this for initialization
    void Start()
    {
        damagePayload = new ProcessDamagePayload(null, null, AttackType.ATTACK, new DamageCarrier(0f, DamageTypes.WATER), null, 0f, 0f, 0f, false);

        if (verboseDamageTypes == null)
        {
            verboseDamageTypes = new string[(int)DamageTypes.COUNT];
            verboseDamageTypes[(int)DamageTypes.FIRE] = StringManager.GetString("misc_dmg_fire");
            verboseDamageTypes[(int)DamageTypes.LIGHTNING] = StringManager.GetString("misc_dmg_lightning");
            verboseDamageTypes[(int)DamageTypes.SHADOW] = StringManager.GetString("misc_dmg_shadow");
            verboseDamageTypes[(int)DamageTypes.WATER] = StringManager.GetString("misc_dmg_water");
            verboseDamageTypes[(int)DamageTypes.POISON] = StringManager.GetString("misc_dmg_poison");
            verboseDamageTypes[(int)DamageTypes.PHYSICAL] = StringManager.GetString("misc_dmg_physical");
        }

        bluntDamageWords = AssignFlavorWords("blunt");
        slashDamageWords = AssignFlavorWords("slash");
        pierceDamageWords = AssignFlavorWords("pierce");
        biteDamageWords = AssignFlavorWords("bite");
        fireDescriptors = AssignFlavorWords("fire");
        waterDescriptors = AssignFlavorWords("water");
        shadowDescriptors = AssignFlavorWords("shadow");
        lightningDescriptors = AssignFlavorWords("lighting");
        poisonDescriptors = AssignFlavorWords("poison");
        physicalDescriptors = AssignFlavorWords("physical");

        //This is never getting used
        //thaneAuraSprites = Resources.LoadAll<Sprite>("SpriteEffects/Spritesheets/Auras");

        pool_tileList = new List<MapTileData>();
        pool_targetList = new List<Fighter>();
        accumulatedCombatWaitTime = 0.0f;
        combatStack = new List<CombatDataPack>();
        cmsInstance = this;
        GameObject go = GameObject.Find("BattleTextManager");
        btm = go.GetComponent<BattleTextManager>();
        go = GameObject.Find("UIManager");
        uims = go.GetComponent<UIManagerScript>();

        queuedAnimations = new Queue<CombatAnimation>();
        queuedText = new Queue<CombatAnimation>();
        //bufferedCombatData = new CombatDataPack();
    }

    public static void SetLastUsedAbility(AbilityScript abilUsed)
    {
        abilityBeingUsedID++;
        abilityBeingUsed = abilUsed;
    }

    public static AbilityUsageInstance GetLastUsedAbility()
    {
        AbilityUsageInstance aui = new AbilityUsageInstance(abilityBeingUsedID, abilityBeingUsed);
        return aui;
    }

    public static bool CheckIfAbilityInstanceMatchesLastUsedAbility(AbilityUsageInstance aui)
    {
        if ((abilityBeingUsed == aui.abilityRef) && (abilityBeingUsedID == aui.abilityID)) return true;
        return false;
    }

    public static void ResetAllVariablesToGameLoad()
    {
        damagePayload = null;
        btm = null;
        uims = null;
        bufferBattleTextData = null;
        cmsInstance = null;
        bufferedCombatData = null;
        combatStack.Clear();
        queuedAnimations.Clear();
        queuedText.Clear();
        pool_targetList = new List<Fighter>();
        pool_tileList.Clear();
        abilityBeingUsed = null;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static float CalculateBudokaWeaponPower(Fighter ft, int level)
    {
        float calc = 0f;
        int pLevel = ft.myStats.GetLevel();

        switch (level)
        {
            case 0:
            case 1:
                calc = 12f + (Mathf.Pow(pLevel, 1.05f) * 2.4f);
                if (pLevel >= 12) calc *= 1.05f;
                if (pLevel >= 16) calc *= 1.05f;
                if (pLevel >= 20) calc *= 1.05f;
                return calc;
            case 2: // Kick
                calc = 14f + (Mathf.Pow(pLevel, 1.05f) * 2.75f);
                if (pLevel >= 12) calc *= 1.05f;
                if (pLevel >= 16) calc *= 1.05f;
                if (pLevel >= 20) calc *= 1.05f;
                return calc;
            default:
                calc = 12f + (Mathf.Pow(pLevel, 1.05f) * 2.4f);
                if (pLevel >= 12) calc *= 1.05f;
                if (pLevel >= 16) calc *= 1.05f;
                if (pLevel >= 20) calc *= 1.05f;
                return calc;
        }

    }

    public static void AddToCombatStack(CombatDataPack cdp)
    {
        combatStack.Add(cdp);
        bufferedCombatData = cdp; // Use the most recent thing.
    }

    public static void RemoveFromCombatStack(CombatDataPack cdp)
    {
        if (combatStack.Contains(cdp))
        {
            combatStack.Remove(cdp);
            if (combatStack.Count > 0)
            {
                bufferedCombatData = combatStack[combatStack.Count - 1];
            }
            // else { bufferedCombatData = null;  } We need to do this, but it causes problems in some cases.
        }
    }

    // Add instead of setting...?
    public static void ModifyChanceToCrit(float amount)
    {
        bufferedCombatData.chanceToCrit *= amount;
    }

    public static void ModifyChanceToCritFlat(float amount)
    {
        bufferedCombatData.chanceToCritFlat = amount;
    }


    public static void ModifyBufferedAccuracy(float amount)
    {
        bufferedCombatData.accuracyMod *= amount;
    }

    public static void ModifyBufferedParry(float amount)
    {
        bufferedCombatData.parryMod *= amount; // this needs to be a mult in case you have several parry-altering effects.
    }

    public static void ModifyBufferedAccuracyFlat(float amount)
    {
        bufferedCombatData.accuracyModFlat = amount;
    }

    public static void ModifyBufferedParryFlat(float amount)
    {
        bufferedCombatData.parryModFlat = amount;
    }

    public static void ModifyBufferedDamage(float amount)
    {
        // This was JUST setting before, now it's adding. Is this OK?
        bufferedCombatData.damageMod += amount;
        //Debug.Log("Modify buffered damage by " + amount);
    }

    public static void ModifyBufferedDamageAsPercent(float amount)
    {
        bufferedCombatData.damageModPercent = amount;
        //Debug.Log("Modify buffered damage % by " + amount);
    }

    public float GetRandomVariance(float baseValue, float variance)
    {
        return UnityEngine.Random.Range(baseValue - (baseValue * variance), baseValue + (baseValue * variance));
    }

    public static void EnqueueCombatAnimation(CombatAnimation ca)
    {
        queuedAnimations.Enqueue(ca);
    }

    public static void EnqueueCombatText(CombatAnimation ca)
    {
        queuedText.Enqueue(ca);
    }

    public void ClearQueuedEffects()
    {
        queuedAnimations.Clear();
        queuedText.Clear();
    }

    public void ProcessQueuedEffects()
    {
        if (queuedAnimations.Count > 0)
        {
            int reps = 0;
            while (queuedAnimations.Count > 0 && reps < 100)
            {
                reps++;
                CombatAnimation processAnim = queuedAnimations.Dequeue();
                if (processAnim == null) continue;
                if (processAnim.battleTextAttached) //processAnim.btd != null)
                {
                    btm.WaitThenPlayDamageText(processAnim.btd, processAnim.genEffect.animLength); // This is new, to delay text post-animation
                }
                if (!processAnim.effIsDefinitelyNull && processAnim.genEffect != null)
                {
                    if (processAnim.genEffect.playAnimation)
                    {
                        GenerateEffectAnimation(processAnim.origPos, processAnim.targetPos, processAnim.genEffect, processAnim.targetObject, processAnim.forceSilentFX);
                    }

                    if (processAnim.genEffect.parentAbility != null)
                    {
                        if (!processAnim.genEffect.parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
                        {
                            StartCoroutine(WaitThenProcessQueuedEffects(processAnim.genEffect.animLength));
                            return;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(processAnim.effectRef))
                {
                    GenerateSpecificEffectAnimation(processAnim.origPos, processAnim.effectRef, processAnim.genEffect);
                    try { StartCoroutine(WaitThenProcessQueuedEffects(processAnim.animLength)); }
                    catch (Exception e)
                    {
                        if (Debug.isDebugBuild) Debug.Log("WARNING: Error occurred during coroutine to process a dequeued animation: " + e);
                    }
                }
            }
            if (reps >= 100)
            {
                if (Debug.isDebugBuild) Debug.Log("Broke CMS combat animation while loop");
            }
        }
    }

    public void ProcessQueuedText()
    {
        if (queuedText.Count > 0)
        {
            int reps = 0;
            while ((queuedText.Count > 0) && (reps < 100))
            {
                reps++;
                CombatAnimation processAnim = queuedText.Dequeue();

                if (processAnim == null)
                {
                    Debug.Log("Queued text is null.");
                    continue;
                }

                if (processAnim.battleTextAttached) //processAnim.btd != null)
                {
                    btm.NewBattleText(processAnim.btd);
                }
                if (!processAnim.effIsDefinitelyNull && processAnim.genEffect != null)
                {
                    if (processAnim.genEffect.parentAbility == null)
                    {
                        // Do nothing
                    }
                    else
                    {
                        if (!processAnim.genEffect.parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
                        {
                            StartCoroutine(WaitThenProcessQueuedText(processAnim.genEffect.animLength));
                            return;
                        }
                    }
                }
            }
            if (reps >= 100)
            {
                Debug.Log("Broke combat manager text dequeue loop");
            }

        }
    }

    public IEnumerator WaitThenProcessQueuedEffects(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        ProcessQueuedEffects();
    }

    public IEnumerator WaitThenProcessQueuedText(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        ProcessQueuedText();
    }

    public static void ProcessGenericEffect(Fighter atk, Fighter def, EffectScript eff, bool silent, bool playAnim, bool forceSilent = false)
    {
        GameObject attacker = atk.GetObject();
        GameObject defender = def.GetObject();

        bool effIsNull = eff == null;
        bool parentAbilityIsNull = true;
        if (!effIsNull)
        {
            parentAbilityIsNull = eff.parentAbility == null;
        }

        if (attacker == null || defender == null)
        {
            return;
        }

        // We're assuming all is hostile but it might not be. Should this code be done elsewhere?!

        if (atk.actorfaction != def.actorfaction && def.GetActorType() == ActorTypes.MONSTER)
        {
            //Debug.Log(atk.actorRefName + " " + def.actorRefName + " " + atk.actorfaction + " " + def.actorfaction);
            // HARDCODED: Smoke bomb does not generate aggro.
            if (effIsNull || (!effIsNull && eff.effectRefName != "smokeblind1" && eff.effectRefName != "smokeblind2"))
            {
                Monster mon = def as Monster;
                mon.lastActorAttackedBy = atk;
                // Determine extra aggro?
                mon.AddAggro(atk, 15f);
            }

        }

        bool countAsCombatAction = true;

        if (!effIsNull)
        {
            if ((atk == def || atk.actorfaction == def.actorfaction) && !silent && !playAnim)
            {
                // This is probably not really a combat effect.
                countAsCombatAction = false;
            }
            // Player pets using self-movement abilities: should not be considered combat actions
            if (!parentAbilityIsNull && eff.parentAbility.abilityFlags[(int)AbilityFlags.MOVESELF] && atk.actorfaction == Faction.PLAYER && atk.GetActorType() == ActorTypes.MONSTER)
            {
                countAsCombatAction = false;
            }
        }

        if (countAsCombatAction)
        {
            atk.ResetTurnsSinceLastCombatAction();
            def.ResetTurnsSinceLastCombatAction();
        }

        // player can't see attacker OR defender, so don't show the effect
        if (!GameMasterScript.heroPCActor.CheckIfTileIsVisibleInArray(def.GetPos()))
        {
            silent = true;
        }

        if (!silent)
        {
            if (playAnim)
            {
                //BattleTextData btd = new BattleTextData(bufferBattleTextData.text, bufferBattleTextData.pos, bufferBattleTextData.color);
                EnqueueCombatAnimation(new CombatAnimation(attacker.transform.position, defender.transform.position, eff, null, defender, forceSilent, effIsNull, false));
            }
            else
            {
                //btm.NewText(bufferBattleTextData.text, bufferBattleTextData.pos, bufferBattleTextData.color);
            }
        }
    }

    public static CombatResult ProcessDamageEffect(Fighter atk, Fighter def, float amount, DamageEffect eff, bool silent, bool playAnim, bool crit)
    {
        // Calculate damage formula here.
        GameObject attacker = atk.GetObject();
        GameObject defender = def.GetObject();
        Vector3 atkPos = Vector3.zero;
        Vector3 defPos = Vector3.zero;
            defPos = def.GetPos();
        
        
            atkPos = atk.GetPos();
        
        StatBlock attackerSB = atk.myStats;
        StatBlock defenderSB = def.myStats;

        // Damage text

        Color textColor = Color.yellow;

        atk.ResetTurnsSinceLastCombatAction();
        def.ResetTurnsSinceLastCombatAction();

        if (def.GetActorType() == ActorTypes.HERO)
        {
            textColor = BattleTextManager.playerDamageColor;
        }
        else
        {
            textColor = BattleTextManager.skillDamageColor;
        }

        CombatDataPack cdp = new CombatDataPack();
        cdp.ResetData();

        AddToCombatStack(cdp);

        cdp.attacker = atk;
        cdp.defender = def;
        cdp.damage = (amount + UnityEngine.Random.Range(1, 4f));
        cdp.atkType = AttackType.ABILITY;
        cdp.effect = eff;
        cdp.damageType = eff.damType;
        cdp.damageType = atk.myEquipment.GetDamageType(atk.myEquipment.GetWeapon());
        cdp.flavorDamageType = atk.myEquipment.GetFlavorDamageType(atk.myEquipment.GetWeapon());
        cdp.attackerWeapon = atk.myEquipment.GetWeapon();
        Vector3 targetDir = (defPos - atkPos).normalized;

        float angle = -1f;
        bool effIsNull = eff == null;
        bool parentAbilityIsNull = true;

        if (!effIsNull)
        {
            parentAbilityIsNull = eff.parentAbility == null;
        }
        if (!parentAbilityIsNull)
        {
            if (eff.parentAbility.direction == Directions.NEUTRAL)
            {
                angle = 0f;
            }
        }
        if (attacker != null && angle == -1f)
        {
            angle = CombatManagerScript.GetAngleBetweenPoints(atk.GetPos(), def.GetPos());
        }

        // Change this? Do abilities have directions?
        Directions directionOfAttack = MapMasterScript.GetDirectionFromAngle(angle);
        if (angle == 0f)
        {
            directionOfAttack = Directions.NEUTRAL;
        }
        FighterBattleData atkFBD = atk.cachedBattleData;
        FighterBattleData defFBD = def.cachedBattleData;

        DamageCarrier dc = new DamageCarrier(amount, eff.damType);
        if (eff.effectType == EffectType.DAMAGE)
        {
            DamageEffect de = (DamageEffect)eff as DamageEffect;
            dc.floor = de.floorValue;
            dc.ceiling = de.ceilingValue;
        }

        // Incorporate "On Attack" stuff here?
        if (!parentAbilityIsNull)
        {
            if (eff.parentAbility.CheckAbilityTag(AbilityTags.ONHITPROPERTIES))
            {
                if (bufferedCombatData.damageMod != 0 || bufferedCombatData.damageModPercent != 1.0f)
                {
                    dc.amount += bufferedCombatData.damageMod;
                    dc.amount *= bufferedCombatData.damageModPercent;
                }
            }
        }


        float finalDamage = ProcessDamage(atk, def, AttackType.ABILITY, dc, eff);

        if (dc.damType != DamageTypes.PHYSICAL && UnityEngine.Random.Range(0, 1f) <= CHANCE_LEGSHARD_ABSORB && def.IsHero() && def.myStats.CheckHasStatusName("mmlegshard"))
        {
            def.myStats.AddStatusByRef("status_spiritpowerbuff", def, 10);

            Item shard = GameMasterScript.heroPCActor.myEquipment.GetItemWithMagicMod("mm_legshard");
            if (shard != null)
            {
                StringManager.SetTag(0, def.displayName);
                StringManager.SetTag(1, shard.displayName);
                GameLogScript.LogWriteStringRef("log_absorb_shard");
                finalDamage = 0f;
            }
        }

        if (finalDamage != 0f)
        {
            if (dc.floor < 0) dc.floor = 0f;
            if (finalDamage < dc.floor)
            {
                finalDamage = dc.floor;
            }
            if (finalDamage > dc.ceiling)
            {
                finalDamage = dc.ceiling;
            }

            if (def.GetActorType() == ActorTypes.HERO 
                && finalDamage >= (def.myStats.GetMaxStat(StatTypes.HEALTH) * 0.66f))
            {
                finalDamage = def.myStats.GetMaxStat(StatTypes.HEALTH) * 0.66f;
            }

            def.TakeDamage(finalDamage, eff.damType);
        }



        if (def.GetActorType() == ActorTypes.MONSTER)
        {
            // attacked a monster!
            Monster mon = def as Monster;
            mon.AddAggro(atk, finalDamage);
        }

        bufferBattleTextData = new BattleTextData(((int)finalDamage).ToString(), def.GetObject(), textColor, crit);

        if (!silent)
        {
            string color = "";
            if (def.IsHero())
            {
                color = UIManagerScript.orangeHexColor;
            }
            else
            {
                color = "<#fffb00>";
            }

            string colorizedName = "";
            string damageSprite = "";
            switch (GetFighterDamageConversion(atk, eff.damType))
            {
                case DamageTypes.PHYSICAL:
                    damageSprite = "<sprite=0>";
                    colorizedName = UIManagerScript.silverHexColor + eff.effectName;
                    break;
                case DamageTypes.FIRE:
                    damageSprite = "<sprite=1>";
                    colorizedName = UIManagerScript.orangeHexColor + eff.effectName;
                    break;
                case DamageTypes.WATER:
                    damageSprite = "<sprite=3>";
                    colorizedName = UIManagerScript.cyanHexColor + eff.effectName;
                    break;
                case DamageTypes.LIGHTNING:
                    damageSprite = "<sprite=4>";
                    colorizedName = "<#fffb00>" + eff.effectName;
                    break;
                case DamageTypes.POISON:
                    damageSprite = "<sprite=2>";
                    colorizedName = UIManagerScript.greenHexColor + eff.effectName;
                    break;
                case DamageTypes.SHADOW:
                    damageSprite = "<sprite=5>";
                    colorizedName = UIManagerScript.purpleHexColor + eff.effectName;
                    break;
            }

            if (StringManager.gameLanguage == EGameLanguage.zh_cn)
            {
                damageSprite += "   ";
            }
            string critText = "";

            if (crit)
            {
                if (StringManager.DoesCurrentLanguageUseSpaces())
                {
                	critText = " " + StringManager.GetString("misc_crit");
                }
            }

            bool writeThisDamageToLog = true;

            if (GameMasterScript.gmsSingleton.ReadTempGameData("shieldabsorbedalldamage") == 1)
            {
                writeThisDamageToLog = false;
                GameMasterScript.gmsSingleton.SetTempGameData("shieldabsorbedalldamage", 0);
            }

            if (writeThisDamageToLog)
            {
                LoseHPPackage lhp = GameLogDataPackages.GetLoseHPPackage();

                if (atk.actorfaction == Faction.PLAYER)
                {
                    lhp.abilityUser = UIManagerScript.orangeHexColor + atk.displayName + "</color>";
                }
                else
                {
                    lhp.abilityUser = UIManagerScript.redHexColor + atk.displayName + "</color>";
                }


                lhp.type = LogDataTypes.LOSEHP;
                lhp.gameActor = def;
                lhp.damageAmount = finalDamage;
                lhp.damageSpriteString = damageSprite;
                lhp.damageEffectSource = critText + " " + colorizedName;
                lhp.dType = eff.damType;
                GameLogScript.CombatEventWrite(lhp);
            }

            BattleTextData btd = new BattleTextData(bufferBattleTextData.text, bufferBattleTextData.btdObj, bufferBattleTextData.color, crit);

            if (eff != null && eff.delayBeforeAnimStart > 0f)
            {
                cmsInstance.StartCoroutine(cmsInstance.WaitThenShowEffect(defPos, eff.spriteEffectRef, eff, eff.delayBeforeAnimStart, true));

                //Don't display 0 damage if we soaked the real damage with a shield.
                if ((int) finalDamage == 0 && def.ReadActorData("hide_next_zero_battledamage") == 1)
                {
                    def.SetActorData("hide_next_zero_battledamage", 0);
                }
                else
                {
                    BattleTextData delayedBTD =
                        new BattleTextData(((int) finalDamage).ToString(), defender, Color.red, crit);
                    if (!crit)
                    {
                        delayedBTD.sizeMod = 1.0f;
                        delayedBTD.lengthMod = 0.0f;
                    }
                    else
                    {
                        delayedBTD.sizeMod = 2.0f;
                        delayedBTD.lengthMod = 0.33f;
                    }

                    cmsInstance.StartCoroutine(cmsInstance.WaitThenBattleText(delayedBTD, eff.delayBeforeAnimStart));
                }

            }
            else
            {
                if (playAnim)
                {
                    if (eff.spriteEffectRef != null)
                    {
                        EnqueueCombatAnimation(new CombatAnimation(atkPos, defPos, eff, btd, defender, false, false, true));
                    }
                    else
                    {
                        // No effect ref, but still want to play animation? Feed the text in.
                        //btm.NewText(btd);
                        if (!eff.parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
                        {
                            EnqueueCombatAnimation(new CombatAnimation(atkPos, defPos, null, btd, defender, false, true, true));
                        }

                    }

                }
                else
                {
                    //Debug.Log("No play anim. This could be a simultaneous effect, so queue the text.");
                    if (!effIsNull)
                    {
                        if (!parentAbilityIsNull)
                        {
                            if (eff.parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
                            {
                                // It's just one animation, so QUEUE the text up.
                                EnqueueCombatText(new CombatAnimation(atkPos, defPos, null, btd, defender, false, false, true));
                            }
                            else
                            {
                                //Don't display 0 damage if we soaked the real damage with a shield.
                                if ((int) finalDamage == 0 && def.ReadActorData("hide_next_zero_battledamage") == 1)
                                {
                                    def.SetActorData("hide_next_zero_battledamage", 0);
                                }
                                else
                                {

                                    if (!crit)
                                    {
                                        // Ability damage.
                                        BattleTextManager.NewDamageText((int) finalDamage, false, Color.white, defender,
                                            0.0f, 1f);
                                    }
                                    else
                                    {
                                        BattleTextData critBTD = new BattleTextData(((int) finalDamage).ToString(),
                                            defender, Color.red, true);
                                        BattleTextManager.btmSingleton.NewText(critBTD);
                                        //BattleTextManager.NewDamageText((int)finalDamage, false, defender, 1f, 2f, BounceTypes.STANDARD); // was 1f time before
                                    }
                                }
                            }
                        }
                    }
                    //EnqueueCombatAnimation(new CombatAnimation(attacker.transform.position, defender.transform.position, null, btd));
                    //btm.NewText(bufferBattleTextData.text, bufferBattleTextData.pos, bufferBattleTextData.color);
                }
            }
        }

        bool alive = defenderSB.IsAlive();

        // Create damage FX image
        RemoveFromCombatStack(cdp);
        return EvaluateCombatResultSilent(alive, atk, def);
    }

    IEnumerator WaitToFireProjectile(float waitTime, Vector2 attackPosition, Vector2 defendPosition, GameObject effectObj, float animTime, bool overrideFrameTime, GameObject targetObject, MovementTypes moveType, EffectScript launchingEffect, float spinDegrees = 0f, bool dieOnArrival = true)
    {
        yield return new WaitForSeconds(waitTime);
        FireProjectile(attackPosition, defendPosition, effectObj, animTime, overrideFrameTime, targetObject, moveType, launchingEffect, spinDegrees, dieOnArrival);
    }

    public static void WaitThenFireProjectile(float waitTime, Vector2 attackPosition, Vector2 defendPosition, GameObject effectObj, float animTime, bool overrideFrameTime, GameObject targetObject, MovementTypes moveType, EffectScript launchingEffect, float spinDegrees = 0f, bool dieOnArrival = true)
    {
        GameMasterScript.combatManager.StartCoroutine(GameMasterScript.combatManager.WaitToFireProjectile(waitTime, attackPosition, defendPosition, effectObj, animTime, overrideFrameTime, targetObject, moveType, launchingEffect, spinDegrees, dieOnArrival));
    }

    public static void FireProjectile(Vector2 attackPosition, Vector2 defendPosition, GameObject effectObj, float animTime, bool overrideFrameTime, GameObject targetObject, MovementTypes moveType, EffectScript launchingEffect, float spinDegrees = 0f, bool dieOnArrival = true, bool overrideMaxProjectileTime = false)
    {
        Animatable anm = effectObj.GetComponent<Animatable>();

        if (anm != null) // Could be firing things that are not animated.
        {
            anm.SetAnim("Default");
            anm.OverrideCompletionBehavior("Loop");

            if ((animTime <= 0f || animTime >= 1f) && !overrideMaxProjectileTime)
            {
                animTime = GameMasterScript.baseAttackAnimationTime;
            }

            if (overrideFrameTime)
            {
                anm.OverrideFrameLength(animTime);
            }
        }

        effectObj.transform.position = attackPosition;

        var mov = effectObj.GetComponent<Movable>();

        if (mov != null)
        {
            mov.SetPosition(attackPosition);
            mov.AnimateSetPosition(defendPosition, animTime, false, spinDegrees, 0f, moveType);
            mov.TrackObject(targetObject);
            if (launchingEffect != null &&
                launchingEffect.projectileMovementType == MovementTypes.TOSS)
            {
                mov.SetTossHeight(launchingEffect.projectileTossHeight);
            }
            if (dieOnArrival)
            {
                mov.DieAfterSeconds(animTime);
            }
        }
    }

    public static GameObject GetEffect(string effectRef)
    {
        GameObject spriteObj = null;
        bool success = true;
        try { spriteObj = GameMasterScript.TDInstantiate(effectRef); }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("Problem retrieving " + effectRef + ": " + e);
            success = false;
        }
        if (success)
        {
            PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(effectRef, spriteObj, SpriteReplaceTypes.BATTLEFX);
        }
        return spriteObj;
    }

    public static GameObject SpawnChildSprite(string spriteRef, Actor act, Directions whichDir, bool rotateObject)
    {
        if (act.GetObject() == null)
        {
            return null;
        }
        GameObject overlay = null;
        try { overlay = GameMasterScript.TDInstantiate(spriteRef); }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log(spriteRef + " Spawn child sprite error: " + e);
            if (Debug.isDebugBuild) Debug.Log("Ref " + spriteRef + " not found?");
        }

        if (overlay != null)
        {
            TryPlayEffectSFX(overlay, act.GetPos(), null);
            overlay.transform.SetParent(null);
            PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(spriteRef, overlay, SpriteReplaceTypes.BATTLEFX);
        }


        float rotationAmount = 0.0f;
        SpriteEffect se = overlay.GetComponent<SpriteEffect>();

        float offsetX = 0.0f;
        float offsetY = 0.0f;
        if (se != null)
        {
            offsetX = se.offset.x;
            offsetY = se.offset.y;
        }

        switch (whichDir)
        {
            case Directions.NEUTRAL:
                break;
            case Directions.NORTH:
                // Do nothing. We assume north by default.
                // offsetY += 0.4f;
                /* if (act == GameMasterScript.heroPCActor) // Stupid way of hardcoding "upper right" for status effects...
                {
                    offsetY += 0.23f;
                    offsetX += 0.18f;
                } */
                break;
            case Directions.NORTHEAST:
                // Do nothing. We assume north by default.
                rotationAmount = -45f;
                //  offsetY += 0.33f;
                //  offsetX += 0.33f;
                break;
            case Directions.EAST:
                rotationAmount = -90f;
                //offsetX += 0.4f;
                break;
            case Directions.SOUTHEAST:
                rotationAmount = -135f;
                // offsetX += 0.33f;
                // offsetY += -0.33f;
                break;
            case Directions.SOUTH:
                rotationAmount = 180f;
                // offsetY += -0.4f;
                break;
            case Directions.SOUTHWEST:
                rotationAmount = 135f;
                // offsetY += -0.33f;
                // offsetX += -0.33f;
                break;
            case Directions.WEST:
                rotationAmount = 90f;
                //offsetX += -0.4f;
                break;
            case Directions.NORTHWEST:
                rotationAmount = 45f;
                //offsetX += -0.33f;
                //offsetY += 0.33f;
                break;
        }
        if (rotationAmount != 0 && rotateObject)
        {
            //overlay.transform.eulerAngles = new Vector3(overlay.transform.eulerAngles.x, overlay.transform.eulerAngles.y, rotationAmount);
            overlay.transform.Rotate(new Vector3(0, 0, rotationAmount), Space.Self);
        }

        if (se != null)
        {
            se.offset.x = offsetX;
            se.offset.y = offsetY;
            se.SetFollowObject(act.GetObject(), whichDir);
            se.SetFollowActor(act);
        }

        //Debug.Log(overlay.name + " " + overlay.transform.position + " " + overlay.transform.localPosition + " " + whichDir);


        // Ridiculous special case for this ONE effect and ONE monster. Not sure how to data drive it.
        if (act.GetActorType() == ActorTypes.MONSTER && whichDir != Directions.NEUTRAL
            && act.actorRefName == "mon_robotdragon" 
            && spriteRef == "EnergyShieldEffect")
        {
            switch (whichDir)
            {
                case Directions.NORTH:
                    overlay.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                    return overlay;
                case Directions.EAST:
                    overlay.transform.localPosition = new Vector3(0.75f, 0f, 0f);
                    return overlay;
                case Directions.WEST:
                    overlay.transform.localPosition = new Vector3(-0.75f, 0f, 0f);
                    return overlay;
                case Directions.SOUTH:
                    overlay.transform.localPosition = new Vector3(0f, -0.75f, 0f);
                    return overlay;
            }
        }

        if (overlay.transform.localPosition.y >= 0.85f)
        {
            // Hack for big character sprites
            overlay.transform.localPosition = new Vector3(overlay.transform.localPosition.x, 0.85f, overlay.transform.localPosition.z);
        }
        if (overlay.transform.localPosition.y <= -0.75f)
        {
            // Hack for big character sprites
            overlay.transform.localPosition = new Vector3(overlay.transform.localPosition.x, -0.75f, overlay.transform.localPosition.z);
        }

        // Fix problems with energy shield offsets due to large sprites
        /* if (whichDir == Directions.WEST)
        {
            nPos.x = overlay.transform.localPosition.x + act.mySpriteRenderer.sprite.bounds.center.x;
            nPos.y = overlay.transform.localPosition.y;
        }
        else if (whichDir == Directions.EAST)
        {
            nPos.x = overlay.transform.localPosition.x - act.mySpriteRenderer.sprite.bounds.center.x;
            nPos.y = overlay.transform.localPosition.y;
        }
        else
        {
            nPos.x = overlay.transform.localPosition.x;
            nPos.y = overlay.transform.localPosition.y;
        }
        else if (whichDir == Directions.NORTH)
        {
            nPos.x = overlay.transform.localPosition.x;
            nPos.y = overlay.transform.localPosition.y + act.mySpriteRenderer.sprite.bounds.center.y;
        }
        else if (whichDir == Directions.SOUTH)
        {
            nPos.x = overlay.transform.localPosition.x;
            nPos.y = overlay.transform.localPosition.y - act.mySpriteRenderer.sprite.bounds.center.y;
        } 
        
         overlay.transform.localPosition = nPos;
         */

        return overlay;
    }

    // This is basically used by OVERRIDE abilities, but we're checking stuff for safety
    public static void TryPlayAbilitySFX(GameObject spriteObj, Vector2 targetPosition, AbilityScript abilityUsed)
    {
        if (spriteObj.GetComponent<AudioStuff>() != null)
        {
            bool playCue = true;

            if (!MapMasterScript.InMaxBounds(targetPosition) || !MapMasterScript.InMaxBounds(targetPosition))
            {
                return;
            }

            if (GameMasterScript.heroPCActor.visibleTilesArray[(int)targetPosition.x, (int)targetPosition.y])
            {
                AudioStuff aStuff = spriteObj.GetComponent<AudioStuff>();

                if (abilityUsed != null)
                {
                    if (abilityUsed.CheckAbilityTag(AbilityTags.OVERRIDECHILDSFX))
                    {
                        playCue = true;
                    }
                }

                if (playCue)
                {
                    aStuff.PlayCue("Awake");
                }
            }
        }
    }

    public static void TryPlayEffectSFX(GameObject spriteObj, Vector2 targetPosition, EffectScript eff, bool forcePlaySFX = false)
    {
        if (spriteObj == null)
        {
            return;
        }

        bool effIsNull = eff == null;
        bool parentAbilityIsNull = true;
        if (!effIsNull)
        {
            parentAbilityIsNull = eff.parentAbility == null;
        }        
        AudioStuff aStuff = spriteObj.GetComponent<AudioStuff>();

        if (aStuff != null)
        {
            bool playCue = true;

            if ((!MapMasterScript.InMaxBounds(targetPosition) || !MapMasterScript.InMaxBounds(targetPosition)) && !forcePlaySFX)
            {
                return;
            }            

            if (forcePlaySFX || (GameMasterScript.heroPCActor.visibleTilesArray[(int)targetPosition.x, (int)targetPosition.y]))
            {
                
                if (!effIsNull)
                {
                    if (!parentAbilityIsNull && eff.parentAbility.CheckAbilityTag(AbilityTags.OVERRIDECHILDSFX))
                    {
                        playCue = false;
                    }

                    if (eff.silent)
                    {
                        playCue = false;
                    }
                }

                if (playCue)
                {
                    aStuff.PlayCue("Awake");
                }
            }
            else
            {
            }
        }
        else
        {

        }
    }

    public IEnumerator WaitThenGenerateEffectAnimation(Vector2 attackPosition, Vector2 defendPosition, EffectScript eff, GameObject targetObject, float time)
    {
        yield return new WaitForSeconds(time);
        GenerateEffectAnimation(attackPosition, defendPosition, eff, targetObject);
    }

    public static GameObject GenerateEffectAnimation(Vector2 attackPosition, Vector2 defendPosition, EffectScript eff, GameObject targetObject, bool forceSilent = false)
    {
        if (eff.spriteEffectRef == null || eff.spriteEffectRef == "")
        {
            return null;
        }

        GameObject spriteObj = GetEffect(eff.spriteEffectRef);

if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
        SpriteRenderer sr = spriteObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.material = GameMasterScript.spriteMaterialUnlit;
        }
}

        if (!forceSilent)
        {
            TryPlayEffectSFX(spriteObj, defendPosition, eff);
        }        

        combatAnimationTime = eff.animLength;

        if (eff.isProjectile)
        {
            float targetAngle = GetAngleBetweenPoints(attackPosition, defendPosition);
            {
                targetAngle = targetAngle * -1;
            }

            Vector3 eulerang = new Vector3(spriteObj.transform.eulerAngles.x, spriteObj.transform.eulerAngles.y, spriteObj.transform.eulerAngles.z + targetAngle);
            spriteObj.transform.eulerAngles = eulerang;
            spriteObj.transform.localEulerAngles = eulerang;
            // The rotation here is not quite correct... Or is it

            FireProjectile(attackPosition, defendPosition, spriteObj, combatAnimationTime, false, targetObject, eff.projectileMovementType, eff);
        }
        else if (eff.parentAbility.CheckAbilityTag(AbilityTags.STACKPROJECTILE))
        {
            Vector2 startPos = attackPosition;
            bool bHasStartingStackProjectileEmitter = !string.IsNullOrEmpty(eff.parentAbility.stackProjectileFirstTile);

            float targetAngle = GetAngleBetweenPoints(startPos, defendPosition);
            {
                targetAngle = targetAngle * -1;
            }

            // New code 11/28. If there is an "originating" sprite (laser firing) then don't generate the 
            // stacked/laser animation in the originating tile. Move to the next one instead.
            if (bHasStartingStackProjectileEmitter)
            {
                CustomAlgorithms.GetPointsOnLineNoGarbage(attackPosition, defendPosition);
                if (CustomAlgorithms.numPointsInLineArray >= 2)
                {
                    startPos = CustomAlgorithms.pointsOnLine[1];
                }
            }


            Vector3 eulerang = new Vector3(spriteObj.transform.eulerAngles.x, spriteObj.transform.eulerAngles.y, spriteObj.transform.eulerAngles.z + targetAngle);
            spriteObj.transform.eulerAngles = eulerang;
            //spriteObj.transform.localEulerAngles = eulerang;

            //-2, don't count the start or end tile, just the empty tiles between
            //when trying to size up the starting effect
            int iTilesBetweenCombatants = CustomAlgorithms.numPointsInLineArray - 2;

            if (bHasStartingStackProjectileEmitter)
            {
                GameObject startObj = GetEffect(eff.parentAbility.stackProjectileFirstTile);
                startObj.transform.eulerAngles = eulerang;
                startObj.transform.localEulerAngles = eulerang;
                startObj.transform.position = startPos;

                //the size of the stackEmitter needs to be 1 tile, but if the 
                //beam is pointing at a diagonal, it needs to be sqrt(2) tiles big.
                //we do this by making the scale == to 1 tilesworth of the whole line
                float fLineDistance = Vector2.Distance(startPos, defendPosition);
                fLineDistance = iTilesBetweenCombatants > 0 ? fLineDistance / iTilesBetweenCombatants : 1.0f;
                if (fLineDistance < 1.0f)
                {
                    fLineDistance = 1.0f;
                }

                startObj.GetComponentInChildren<Animatable>().ToggleIgnoreScale(); // AA changed from GetComponent to Children in case its a child object
                startObj.transform.localScale = new Vector3(1f, fLineDistance, 1f);

                startPos += (CustomAlgorithms.pointsOnLine[1] - CustomAlgorithms.pointsOnLine[0]) / 2;
            }

            //if we have a starting emitter, and we're firing point blank, we shouldn't draw the beam.
            if (!bHasStartingStackProjectileEmitter || iTilesBetweenCombatants > 0)
            {
                Vector2 middlePosition = new Vector2((startPos.x + defendPosition.x) / 2f, (startPos.y + defendPosition.y) / 2f);
                spriteObj.transform.position = new Vector3(middlePosition.x, middlePosition.y, spriteObj.transform.position.z);
                float yScale = Vector2.Distance(startPos, defendPosition);
                spriteObj.GetComponent<Animatable>().ToggleIgnoreScale();
                spriteObj.transform.localScale = new Vector3(1f, yScale, 1f);
            }
            else
            {
                spriteObj.GetComponent<SpriteRenderer>().enabled = false;
            }
        }
        else
        {
            Vector2 newPos = new Vector2(defendPosition.x, defendPosition.y);
            spriteObj.transform.position = newPos;
            // Why was summonactor excluded...?
            if (eff.rotateAnimToTarget)
            {
                float angle = GetAngleBetweenPoints(attackPosition, defendPosition);
                if (attackPosition.x != defendPosition.x)
                {
                    angle *= -1f;
                }

                spriteObj.transform.Rotate(new Vector3(0, 0, angle), Space.Self);

                SpriteEffectSystem spriteSystem = spriteObj.GetComponent<SpriteEffectSystem>();

                if (spriteSystem != null)
                {
                    spriteSystem.enforceRotationOnChildren = angle;
                }


            }
            if (spriteObj.GetComponent<Movable>() != null)
            {
                spriteObj.GetComponent<Movable>().SetPosition(newPos);
            }

        }

        if (targetObject != null)
        {
            //Debug.Log("Target object " + targetObject.name);
            if (eff.spriteEffectRef == "FervirBuff" || eff.spriteEffectRef == "FervirBuffSilent" || eff.spriteEffectRef == "FervirRecovery" || eff.spriteEffectRef == "FervirRecoveryQuiet" || eff.spriteEffectRef == "FervirDebuff")
            {
                spriteObj.transform.SetParent(targetObject.transform);
                spriteObj.transform.localPosition = Vector3.zero;
                spriteObj.transform.rotation = Quaternion.identity;
                //Debug.Log("Parenting " + spriteObj.name + " to " + targetObject.name);
            }
        }

        //Debug.Log(spriteObj.name + " " + spriteObj.transform.position);
        return spriteObj;
    }

    public static void EnqueueSpecificEffect(Vector2 effectPosition, string effectRef, float length)
    {
        CombatAnimation ca = new CombatAnimation(effectPosition, effectPosition, null, null, null, false, true, false); // No target object here?
        ca.effectRef = effectRef;
        ca.animLength = length;
        EnqueueCombatAnimation(ca);
    }

    public IEnumerator WaitThenBattleText(BattleTextData btd, float time)
    {
        yield return new WaitForSeconds(time);

        BattleTextManager.btmSingleton.NewText(btd);
    }

    public IEnumerator WaitThenShowEffect(Vector2 effectPosition, string prefab, EffectScript eff, float time, bool forcePlaySFX = false, bool forceSilent = false)
    {
        yield return new WaitForSeconds(time);
        GenerateSpecificEffectAnimation(effectPosition, prefab, eff, forcePlaySFX, 0f, forceSilent);
    }

    public static void WaitThenGenerateSpecificEffect(Vector2 effectPosition, string prefab, EffectScript eff, float time, bool forcePlaySFX = false, float fAnimTimingOffset = 0f, bool forceSilent = false)
    {
        cmsInstance.StartCoroutine(cmsInstance.WaitThenShowEffect(effectPosition, prefab, eff, time, forcePlaySFX, forceSilent));
    }

    public static GameObject GenerateSpecificEffectAnimation(Vector2 effectPosition, string effectPrefab, EffectScript eff, bool forcePlaySFX = false, float fAnimTimingOffset = 0f, bool forceSilent = false)
    {
        GameObject spriteObj = GameMasterScript.TDInstantiate(effectPrefab);

        if (!forceSilent)
        {
            TryPlayEffectSFX(spriteObj, effectPosition, eff, forcePlaySFX);
        }
        
        spriteObj.transform.position = effectPosition;
        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(effectPrefab, spriteObj, SpriteReplaceTypes.BATTLEFX);
        Movable checkMove = spriteObj.GetComponent<Movable>();
        if (checkMove != null)
        {
            checkMove.SetPosition(effectPosition);
        }        

        Animatable animComponent = spriteObj.GetComponent<Animatable>();
        if (animComponent != null)
        {
            if (checkMove != null)
            {
                checkMove.hasFXAnimatable = true;
                checkMove.fxAnimatableComponent = animComponent;
            }
            animComponent.AdjustAnimTiming(fAnimTimingOffset);
        }
        return spriteObj;
    }

    public static void GenerateDirectionalEffectAnimation(Vector2 effectPosition, Directions dir, string effectPrefab, bool forcePlaySFX = false, float fAnimTimingOffset = 0f)
    {
        GameObject spriteObj = GameMasterScript.TDInstantiate(effectPrefab);
        float rotateAngle = MapMasterScript.GetAngleFromDirection(dir);

        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(effectPrefab, spriteObj, SpriteReplaceTypes.BATTLEFX);

        TryPlayEffectSFX(spriteObj, effectPosition, null, forcePlaySFX);
        spriteObj.transform.position = effectPosition;
        if (spriteObj.GetComponent<Movable>() != null)
        {
            spriteObj.GetComponent<Movable>().SetPosition(effectPosition);
        }

        rotateAngle *= -1f;
        Vector3 eulerang = new Vector3(spriteObj.transform.eulerAngles.x, spriteObj.transform.eulerAngles.y, spriteObj.transform.eulerAngles.z + rotateAngle);
        spriteObj.transform.eulerAngles = eulerang;
        spriteObj.transform.localEulerAngles = eulerang;

    }

    public static void GenerateDirectionalEffectAnimation(Vector2 effectPosition, Vector2 vDestination, string effectPrefab, bool forcePlaySFX = false, float fAnimTimingOffset = 0f)
    {
        GameObject spriteObj = GameMasterScript.TDInstantiate(effectPrefab);

        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(effectPrefab, spriteObj, SpriteReplaceTypes.BATTLEFX);

        float rotateAngle = 0f;

        if (vDestination != effectPosition)
        {
            Vector2 vDelta = (vDestination - effectPosition).normalized;
            rotateAngle = Vector2.Angle(new Vector2(0, 1), vDelta);

            //Vector2.Angle only ever returns 0-180, so if the vector is past 6pm on the clock, we want >180 degrees
            if (vDelta.x < 0)
            {
                rotateAngle = 360.0f - rotateAngle;
            }

        }

        TryPlayEffectSFX(spriteObj, effectPosition, null, forcePlaySFX);
        spriteObj.transform.position = effectPosition;
        if (spriteObj.GetComponent<Movable>() != null)
        {
            spriteObj.GetComponent<Movable>().SetPosition(effectPosition);
        }

        rotateAngle *= -1f;
        Vector3 eulerang = new Vector3(spriteObj.transform.eulerAngles.x, spriteObj.transform.eulerAngles.y, spriteObj.transform.eulerAngles.z + rotateAngle);
        spriteObj.transform.eulerAngles = eulerang;
        spriteObj.transform.localEulerAngles = eulerang;

    }

    // Deprecated?
    /*public void GenerateAbilityAnimation(Vector2 attackPosition, Vector2 defendPosition, AbilityScript ability)
    {
        GameObject spriteObj = GetAbilityEffect(ability);
        combatAnimationTime = spriteObj.GetComponent<Animatable>().GetAnim().gameWaitTime;

        if (ability.CheckAbilityTag(AbilityTags.PROJECTILE))
        {
            FireProjectile(attackPosition, defendPosition, spriteObj, combatAnimationTime, false, );
        }
        else
        {
            Debug.Log("Generate effect 2 at " + defendPosition.ToString());
            Vector2 newPos = new Vector2(defendPosition.x, defendPosition.y);
            spriteObj.transform.position = newPos;
            if (spriteObj.GetComponent<Movable>() != null)
            {
                spriteObj.GetComponent<Movable>().SetPosition(newPos);
            }
        }
    } */

    static IEnumerator WaitThenEvaluateDamage(float time)
    {
        GameMasterScript.SetAnimationPlaying(true);
        yield return new WaitForSeconds(time);
        GameMasterScript.SetAnimationPlaying(false);
        //BattleTextManager.NewText(bufferBattleTextData.text, bufferBattleTextData.btdObj, bufferBattleTextData.color, 0.0f);
        BattleTextManager.NewDamageText(Int32.Parse(bufferBattleTextData.text), false, Color.white, bufferBattleTextData.btdObj, 0.0f, 1f); // Does this parse work
    }

    // Don't play any animations or actually do anything.
    private static CombatResult EvaluateCombatResultSilent(bool alive, Actor atk, Actor def)
    {
        if (!alive)
        {
            if (def.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = (Monster)def as Monster;
                mn.whoKilledMe = atk;
                return CombatResult.MONSTERDIED;
            }
            if (def.GetActorType() == ActorTypes.HERO)
            {
                Fighter ft = def as Fighter;
                ft.whoKilledMe = atk;
                return CombatResult.PLAYERDIED;
            }
        }
        return CombatResult.HIT;
    }

    private static CombatResult EvaluateCombatResult(bool alive, Actor atk, Actor def)
    {
        GameObject defender = def.GetObject();
        Vector2 position = def.GetPos();
        if (alive == false)
        {
            if (def.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = (Monster)def as Monster;
                mn.whoKilledMe = atk;
                return CombatResult.MONSTERDIED;
            }
            if (def.GetActorType() == ActorTypes.HERO)
            {
                Fighter ft = def as Fighter;
                ft.whoKilledMe = atk;
                return CombatResult.PLAYERDIED;
                // The player died.
            }
            return CombatResult.MONSTERDIED;
        }
        else
        {
            return CombatResult.HIT;
        }
    }

    public static bool IsCombatMelee()
    {
        if (bufferedCombatData == null) return false;
        if ((bufferedCombatData.attacker == null) || (bufferedCombatData.defender == null)) return false;
        int dist = MapMasterScript.GetGridDistance(bufferedCombatData.attacker.GetPos(), bufferedCombatData.defender.GetPos());
        if (bufferedCombatData.attacker.myEquipment.IsCurrentWeaponRanged()) return false;
        return true;
    }

    public class ProcessDamagePayload
    {
        public Fighter atk;
        public Fighter def;
        public AttackType aType;
        public DamageCarrier damage;
        public EffectScript effParent;
        public float currentDamageValue;
        public float resistMult;
        public float flatOffset;
        public float maxDamage;
        public bool absorbDamage;
        public bool zeroDamageStaysAtZero;

        public ProcessDamagePayload(Fighter _atk, Fighter _def, AttackType _aType, DamageCarrier _damage, EffectScript _effParent, float _currentDamageValue, float _resistMult, float _flatOffset, bool _absorbDamage)
        {
            atk = _atk;
            def = _def;
            aType = _aType;
            damage = _damage;
            effParent = _effParent;
            currentDamageValue = _currentDamageValue;
            resistMult = _resistMult;
            flatOffset = _flatOffset;
            absorbDamage = _absorbDamage;
            zeroDamageStaysAtZero = false;
        }

        public void Init(Fighter _atk, Fighter _def, AttackType _aType, DamageCarrier _damage, EffectScript _effParent, float _currentDamageValue, float _resistMult, float _flatOffset, bool _absorbDamage)
        {
            atk = _atk;
            def = _def;
            aType = _aType;
            damage = _damage;
            effParent = _effParent;
            currentDamageValue = _currentDamageValue;
            resistMult = _resistMult;
            flatOffset = _flatOffset;
            absorbDamage = _absorbDamage;
            zeroDamageStaysAtZero = false;
        }

    }

    // Primary Damage Function
    public static float ProcessDamage(Fighter atk, Fighter def, AttackType bType, DamageCarrier damage, EffectScript eff)
    {
        float finalDamage = damage.amount;
        float origDamage = damage.amount;

        Monster atkMon = null;
        Monster defMon = null;

        //Debug.Log("Start damage is " + finalDamage);
        bool effIsNull = eff == null;
        bool parentAbilityIsNull = true;

        if (!effIsNull)
        {
            parentAbilityIsNull = eff.parentAbility == null;
        }
        int localAuraForAttacker = -1;
        int localAuraForDefender = -1;
        if (MapMasterScript.activeMap.IsItemWorld())
        {
            localAuraForAttacker = MapMasterScript.GetItemWorldAura(atk.GetPos(), false);
            localAuraForDefender = MapMasterScript.GetItemWorldAura(def.GetPos(), false);
        }

        bool attackerIsHero = atk.GetActorType() == ActorTypes.HERO;
        bool defenderIsHero = def.GetActorType() == ActorTypes.HERO;
        bool attackerIsMonster = atk.GetActorType() == ActorTypes.MONSTER;
        bool defenderIsMonster = def.GetActorType() == ActorTypes.MONSTER;

        def.lastAttackTypeReceived = bType;

        if (def.actorfaction == Faction.PLAYER && eff != null && !parentAbilityIsNull
            && eff.parentAbility.CheckAbilityTag(AbilityTags.GROUNDBASEDEFFECT))
        {
            if (attackerIsMonster)
            {
                if (GameMasterScript.heroPCActor.CheckSummon(def))
                {
                    finalDamage *= CombatManagerScript.PLAYER_PET_GROUNDEFFECT_MITIGATION;
                    if (def.actorRefName == "mon_runiccrystal" && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_husynemblem_tier2_runic"))
                    {
                        finalDamage *= 0.5f;
                    }
                }
            }
        }

        if (def.actorfaction == Faction.PLAYER && MapMasterScript.activeMap.IsMysteryDungeonMap())
        {
            if (damage.damType != DamageTypes.PHYSICAL)
            {
                finalDamage *= MysteryDungeonManager.ELEMENTAL_DAMAGE_REDUCTION_MULT;
            }                
        }

        if (localAuraForAttacker == (int)ItemWorldAuras.ELEMENTALDAMAGEPLUS50)
        {
            if (damage.damType != DamageTypes.PHYSICAL)
            {
                finalDamage *= 1.5f;
            }
        }
        if (localAuraForAttacker == (int)ItemWorldAuras.TOUGHMONSTER && atk.GetActorType() == ActorTypes.MONSTER)
        {
            finalDamage *= 1.5f;
        }
        if (localAuraForDefender == (int)ItemWorldAuras.TOUGHMONSTER && def.GetActorType() == ActorTypes.MONSTER)
        {
            finalDamage *= 0.5f;
        }

        if (attackerIsHero || defenderIsHero)
        {
            Fighter theHero = null;
            float bonus = 0f;
            if (attackerIsHero)
            {                
                finalDamage *= PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.HERO_DMG);

                // Pandora's Box Scaling
                float damageReduction = GameMasterScript.GetPandoraMonsterDefenseUpValue() * GameMasterScript.heroPCActor.numPandoraBoxesOpened;
                if (damageReduction > GameMasterScript.GetPandoraMonsterDefenseCapValue()) damageReduction = GameMasterScript.GetPandoraMonsterDefenseCapValue();

                finalDamage = finalDamage - (finalDamage * damageReduction);

                theHero = atk;

                if (MapMasterScript.GetTile(atk.GetPos()).CheckTag(LocationTags.WATER) && theHero.myStats.CheckHasStatusName("oceangem"))
                {
                    finalDamage *= 1.25f;
                }

                defMon = def as Monster;

                if (defenderIsMonster && (defMon.isChampion || defMon.isBoss) && atk.myStats.CheckHasStatusName("emblem_wildchildemblem_tier1_champion"))
                {
                    finalDamage *= 1.1f;
                }

                if (UnityEngine.Random.Range(0, 1f) <= CHANCE_ATHYES_FIRESTUN && damage.damType == DamageTypes.FIRE && theHero.myStats.CheckHasStatusName("status_mmathyes"))
                {
                    if (!def.myStats.CheckHasStatusName("status_basicstun"))
                    {
                        def.myStats.AddStatusByRefAndLog("status_basicstun", atk, 2);
                    }
                }

                // Family bonus
                if (defenderIsMonster)
                {
                    defMon = def as Monster;
                    finalDamage *= theHero.cachedBattleData.GetDamageBonusByFamily(defMon.monFamily);
                }

            }
            else
            {
                theHero = def;                

                if (attackerIsMonster)
                {
                    finalDamage *= PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.ENEMY_DMG);

                    atkMon = atk as Monster;
                    finalDamage *= theHero.cachedBattleData.GetDamageBonusByFamily(atkMon.monFamily);

                    float damageBoost = GameMasterScript.PANDORA_MONSTER_DAMAGE_UP * GameMasterScript.heroPCActor.numPandoraBoxesOpened;
                    finalDamage = finalDamage + (finalDamage * damageBoost);
                }

                if (MapMasterScript.GetTile(def.GetPos()).CheckTag(LocationTags.WATER) && theHero.myStats.CheckHasStatusName("oceangem"))
                {
                    finalDamage *= 0.75f;
                }
            }
        }


        // Photosynthesis new effect
        if (atk.actorfaction == Faction.PLAYER && !attackerIsHero)
        {
            if (atk.actorRefName == "mon_plantturret" || atk.actorRefName.Contains("livingvine"))
            {
                finalDamage *= 1.2f;
            }
        }


        if (def.myStats.CheckHasStatusName("axebreak"))
        {
            if (def.actorFlags[(int)ActorFlags.EXTRADAMAGEFROMAXE]) // use the flag so that the FIRST axe hit doesn't immediately consume this.
            {
                def.actorFlags[(int)ActorFlags.EXTRADAMAGEFROMAXE] = false;
            }
            else
            {
                finalDamage *= 1.25f;
                def.myStats.RemoveStatusByRef("axebreak");
            }
        }


        if (defenderIsHero)
        {
            if (def.myJob.jobEnum == CharacterJobs.PALADIN && def.myStats.CheckHasStatusName("status_sanctuary"))
            {
                return 0f;
            }
        }
        if (attackerIsHero)
        {
            if (atk.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) < 0.5f && atk.myStats.CheckHasStatusName("status_rager"))
            {
                finalDamage *= 1.2f;
            }
            if (atk.myStats.CheckHasStatusName("status_dragonbrave"))
            {
                if (defenderIsMonster)
                {
                    Monster mn = def as Monster;
                    if (mn.isChampion || mn.isBoss)
                    {
                        finalDamage *= 1.2f;
                    }
                }
            }
        }


        finalDamage *= atk.allDamageMultiplier; // Oct 4 2017 WHY was this not implemented until now?

        // Do processing here for damage calculations, attacker vs. defender.

        DamageTypes preConversionDamageType = damage.damType;

        if (attackerIsHero)
        {
            damage = GetFighterDamageConversion(atk, def, damage);
        }        

        float resistMult = def.cachedBattleData.resistances[(int)damage.damType].multiplier;
        if (resistMult <= GameMasterScript.MAX_RESISTANCES)
        {
            resistMult = GameMasterScript.MAX_RESISTANCES;
        }
        float flatOffset = def.cachedBattleData.resistances[(int)damage.damType].flatOffset;
        bool absorb = def.cachedBattleData.resistances[(int)damage.damType].absorb;

        if (attackerIsHero)
        {
            if (bType == AttackType.ATTACK)
            {
                bool mainhand = GameMasterScript.gmsSingleton.ReadTempGameData("mainhand") == 1;
                bool weaponIsSpear = false;
                if (mainhand && atk.myEquipment.GetWeaponType() == WeaponTypes.SPEAR)
                {
                    weaponIsSpear = true;
                }
                else if (!mainhand && atk.myEquipment.GetOffhandWeaponType() == WeaponTypes.SPEAR)
                {
                    weaponIsSpear = true;
                }
                if (weaponIsSpear && resistMult < 1f)
                {
                    // say we have 60% resistance (value is 0.4)
                    // multiply this by 25% to get 15
                    // which means 0.4 becomes 0.55
                    float converted = resistMult + ((1f - resistMult) * 0.25f);
                    resistMult = converted;
                }
            }
        }

        if (atk.actorfaction == Faction.PLAYER && attackerIsMonster)
        {
            finalDamage *= PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.PET_DMG);

            finalDamage += (finalDamage * GameMasterScript.heroPCActor.advStats[(int)AdventureStats.CORRALPETBONUS]);
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("menagerie"))
            {
                int creatureCount = 0;
                foreach(Actor act in GameMasterScript.heroPCActor.summonedActors)
                {
                    if (act.GetActorType() != ActorTypes.MONSTER) continue;
                    creatureCount++;
                }
                finalDamage += (finalDamage * MENAGERIE_PET_COMBAT_BONUS * creatureCount);
            }
        }

        if (def.actorfaction == Faction.PLAYER && defenderIsMonster)
        {
            // EX: Say combat bonus is 0.14
            float adjustedBonus = 1f - GameMasterScript.heroPCActor.advStats[(int)AdventureStats.CORRALPETBONUS];
            // EX: Now we have 0.86
            finalDamage *= adjustedBonus;

            if (def.actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID())
            {
                // Corral pets inherit some defense            .
                float playerResistMult = GameMasterScript.heroPCActor.cachedBattleData.resistances[(int)damage.damType].multiplier;
                if (playerResistMult < 1f)
                {
                    float extraResistForPet = (1f - playerResistMult); // there was never a /2 here, nope
                    if (extraResistForPet >= GameMasterScript.CORRALPET_BONUS_RESISTCAP)
                    {
                        extraResistForPet = GameMasterScript.CORRALPET_BONUS_RESISTCAP;
                    }
                    resistMult -= extraResistForPet;
                    if (resistMult <= GameMasterScript.CORRALPET_MAX_RESISTANCES)
                    {
                        resistMult = GameMasterScript.CORRALPET_MAX_RESISTANCES;
                    }
                }
            }
        }

        float baseDamageMult = atk.cachedBattleData.damageExternalMods[(int)preConversionDamageType];
        baseDamageMult *= atk.cachedBattleData.temporaryDamageMods[(int)preConversionDamageType];
        bool isPhysical = false;

        switch (damage.damType)
        {
            case DamageTypes.PHYSICAL:
                isPhysical = true;
                break;
        }

        if (bType == AttackType.ABILITY)
        {
            if (atk.myStats.CheckHasStatusName("status_spellshiftpenetrate"))
            {
                if (eff != null && eff.parentAbility.spellshift)
                {
                    if (resistMult < 1f)
                    {
                        resistMult = 1f;
                    }
                    if (flatOffset > 0f)
                    {
                        flatOffset = 0f;
                    }
                    absorb = false;
                }
            }
        }


        float pierceResistMult = atk.cachedBattleData.pierceResistances[(int)damage.damType].multiplier;

        if (resistMult < 1f && resistMult > 0f)
        {
            float modified = 1f - resistMult;
            modified *= pierceResistMult;
            float finalRes = 1f - modified;
            resistMult = finalRes;
        }

        if (isPhysical)
        {
            if (attackerIsHero && atk.myStats.CheckHasStatusName("status_mmpenetrating"))
            {
                if (resistMult < 1f)
                {
                    float modified = 1f - resistMult;
                    modified *= 0.5f;
                    float finalRes = 1f - modified;
                    resistMult = finalRes;
                }
                if (flatOffset > 0f)
                {
                    flatOffset *= 0.5f;
                }
            }
        }

        // Set atk, def, float to data pack
        damagePayload.Init(atk, def, bType, damage, eff, finalDamage, resistMult, flatOffset, absorb);
        damagePayload.maxDamage = bufferedCombatData.damageCapForPayload;

        // hacky workaround for subtlebonk
        float dmgCap = GameMasterScript.gmsSingleton.ReadTempFloatData("dmgcap");
        if (dmgCap > 0)
        {
            damagePayload.maxDamage = dmgCap;
            GameMasterScript.gmsSingleton.SetTempFloatData("dmgcap", 0);
        }

        bufferedCombatData.damageCapForPayload = 0;
        atk.myStats.CheckRunAllStatuses(StatusTrigger.CAUSE_DAMAGE);
        finalDamage = damagePayload.currentDamageValue;
        resistMult = damagePayload.resistMult;
        flatOffset = damagePayload.flatOffset;
        absorb = damagePayload.absorbDamage;
        // Integrate the values from statuses here    

        // Process flat BEFORE mult

        //Debug.Log(def.displayName + " " + damage.damType + " " + resistMult + " " + flatOffset + " " + baseDamageMult);

        finalDamage *= baseDamageMult;        

        if (attackerIsHero && atk.myStats.CheckHasStatusName("eyeshield"))
        {
            if (resistMult > 1.0f)
            {
                resistMult += 0.15f;
            }
        }

        finalDamage *= resistMult;
        finalDamage -= flatOffset; // We SUBTRACT the resist value ok?

        MapTileData mtd = MapMasterScript.GetTile(def.GetPos());

        bool isLavaDamage = false;
        if (eff != null && eff.effectRefName == "eff_lavaburning")
        {
            isLavaDamage = true;
        }

        if (damage.damType == DamageTypes.FIRE)
        {
            if (mtd.CheckTag(LocationTags.LAVA) && !isLavaDamage)
            {
                finalDamage *= 1.33f;
                GameLogScript.LogWriteStringRef("log_lava_fire_damage_up");
            }
            else if (mtd.CheckTag(LocationTags.WATER) || mtd.CheckActorRef("obj_buff_watervapor"))
            {
                finalDamage *= 0.5f;
                GameLogScript.LogWriteStringRef("log_water_fire_damage_down");
            }
            if (mtd.CheckForSpecialMapObjectType(SpecialMapObject.OILSLICK))
            {
                GameplayScripts.ConsumeOilSlickAndCreateFire(mtd);
            }
        }
        else if (damage.damType == DamageTypes.POISON)
        {
            if (mtd.CheckForSpecialMapObjectType(SpecialMapObject.OILSLICK))
            {
                GameLogScript.LogWriteStringRef("log_oilslick_poison_damage_up");
                finalDamage *= GameMasterScript.OIL_SLICK_POISON_DAMAGE_MULTIPLIER;
            }
        }
        else if (damage.damType == DamageTypes.WATER)
        {
            if (mtd.CheckTag(LocationTags.LAVA))
            {
                finalDamage *= 0.5f;
                GameLogScript.LogWriteStringRef("log_fire_water_damage_down");
            }
        }
        else if (damage.damType == DamageTypes.LIGHTNING)
        {
            if (mtd.CheckTag(LocationTags.WATER))
            {
                finalDamage *= 1.33f;
                GameLogScript.LogWriteStringRef("log_water_lightning_damage_up");
            }
            else if (mtd.CheckTag(LocationTags.ELECTRIC))
            {
                finalDamage *= 1.33f;
                GameLogScript.LogWriteStringRef("log_conduit_lightning_damage_up");
            }
            else if (mtd.CheckTag(LocationTags.MUD))
            {
                finalDamage *= 0.5f;
                GameLogScript.LogWriteStringRef("log_mud_lightning_damage_down");
            }
        }
        else if (damage.damType == DamageTypes.SHADOW && bType == AttackType.ATTACK && UnityEngine.Random.Range(0, 1f) < CHANCE_WEAPON_SHADOW_DEBUFF)
        {
            def.myStats.AddStatusByRefAndLog("status_shadowdebuff", atk, 5);
        }

        if (damage.damType != DamageTypes.PHYSICAL && defenderIsHero && def.myEquipment.GetWeaponType() == WeaponTypes.STAFF && def.myStats.CheckHasStatusName("staffmastery3"))
        {
            bool flowCondenserPossible = true;
            if (eff != null)
            {
                if (!parentAbilityIsNull && eff.parentAbility.CheckAbilityTag(AbilityTags.GROUNDBASEDEFFECT))
                {
                    flowCondenserPossible = false;
                }
            }
            if (flowCondenserPossible && UnityEngine.Random.Range(0, 1f) <= 0.25f)
            {
                float statAmount = UnityEngine.Random.Range(8f, 16f);
                def.myStats.ChangeStat(StatTypes.ENERGY, statAmount, StatDataTypes.CUR, true);
                StringManager.SetTag(0, ((int)statAmount).ToString());
                GameLogScript.LogWriteStringRef("log_flowcondenser");
                finalDamage *= 0.75f;
            }
        }

        if (absorb)
        {
            // More elegant way of healing please
            //def.myStats.ChangeStat(StatTypes.HEALTH, finalDamage, StatDataTypes.CUR, true);
            StringManager.SetTag(0, def.displayName);
            //BattleTextManager.NewDamageText((int)finalDamage, true, Color.white, def.GetObject(), 0f, 1.0f);            
            finalDamage = 0;
            BattleTextManager.NewDamageText((int)finalDamage, true, Color.green, def.GetObject(), 0f, 1.0f);
            return 0f;
        }

        // HARDCODED: "Slaying" status effects.
        if (attackerIsHero && defenderIsMonster)
        {
            Monster mon = def as Monster;
            if (mon.monFamily == "beasts" && atk.myStats.CheckHasStatusName("status_mmbeastslaying"))
            {
                float dmgAdd = finalDamage * (atk.myStats.CheckStatusQuantity("status_mmbeastslaying") * 0.3f);
                finalDamage += dmgAdd;
            }
            else if (mon.monFamily == "robots" && atk.myStats.CheckHasStatusName("status_mmrustbiting"))
            {
                float dmgAdd = finalDamage * (atk.myStats.CheckStatusQuantity("status_mmrustbiting") * 0.2f);
                finalDamage += dmgAdd;
            }
            else if (mon.monFamily == "insects" && atk.myStats.CheckHasStatusName("status_mminsectslaying"))
            {
                float dmgAdd = finalDamage * (atk.myStats.CheckStatusQuantity("status_mminsectslaying") * 0.3f);
                finalDamage += dmgAdd;
            }
            else if (mon.monFamily == "frogs" && atk.myStats.CheckHasStatusName("status_mmfrogslaying"))
            {
                float dmgAdd = finalDamage * (atk.myStats.CheckStatusQuantity("status_mmfrogslaying") * 0.3f);
                finalDamage += dmgAdd;
            }
            else if (mon.monFamily == "bandits" && atk.myStats.CheckHasStatusName("status_mmbanditslaying"))
            {
                float dmgAdd = finalDamage * (atk.myStats.CheckStatusQuantity("status_mmbanditslaying") * 0.3f);
                finalDamage += dmgAdd;
            }
            else if (mon.monFamily == "spirits" && atk.myStats.CheckHasStatusName("status_mmspiritslaying"))
            {
                float dmgAdd = finalDamage * (atk.myStats.CheckStatusQuantity("status_mmspiritslaying") * 0.3f);
                finalDamage += dmgAdd;
            }
        }

        if (atk.myStats.CheckHasStatusName("status_haunted"))
        {
            int hauntTargetID = def.ReadActorData("haunttarget");
            if (hauntTargetID != atk.actorUniqueID)            
            {                
                Actor hauntActor = GameMasterScript.gmsSingleton.TryLinkActorFromDict(hauntTargetID);
                if (hauntActor != null)
                {
                    finalDamage = 0.0f;
                    StringManager.SetTag(0, MonsterManagerScript.GetMonsterDisplayNameByRef(hauntActor.actorRefName)); // was hard coded to ghost samurai
                    if (atk.GetActorType() == ActorTypes.HERO)
                    {
                        GameLogScript.LogWriteStringRef("log_dmg_noeffect_wraith");
                    }
                }

            }
        }

        if (defenderIsHero)
        {
            Actor crys = GameMasterScript.heroPCActor.GetSummonByRef("mon_runiccrystal");
            if (crys != null && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_fortify"))
            {
                float damageTransfer = finalDamage * 0.25f;
                Fighter ft = crys as Fighter;
                if (damageTransfer > ft.myStats.GetCurStat(StatTypes.HEALTH))
                {
                    damageTransfer = ft.myStats.GetCurStat(StatTypes.HEALTH);
                }
                finalDamage -= damageTransfer;
                int rdDamage = (int)damageTransfer;
                BattleTextManager.NewDamageText(rdDamage, false, Color.white, crys.GetObject(), 0f, 1f);
                ft.TakeDamage(rdDamage, damage.damType);
                if (!ft.myStats.IsAlive())
                {
                    GameMasterScript.AddToDeadQueue(ft);
                }
            }

            // In NG+, hero takes 25% more damage.
            if (GameStartData.NewGamePlus > 0)
            {
                finalDamage *= 1.25f;
            }
            
        }

        damagePayload.currentDamageValue = finalDamage;

        atk.myStats.CheckRunAllStatuses(StatusTrigger.PROCESSDAMAGE_ATTACKER);
        def.myStats.CheckRunAllStatuses(StatusTrigger.PROCESSDAMAGE_DEFENDER);

        finalDamage = damagePayload.currentDamageValue;

        if (damagePayload.maxDamage > 0)
        {
            damage.ceiling = damagePayload.maxDamage;
        }
        else
        {
            damage.ceiling = MAX_DAMAGE_VALUE;
        }

        if (damage.floor < 0)
        {
            damage.floor = 0;
        }

        Mathf.Clamp(finalDamage, damage.floor, damage.ceiling);

        if (attackerIsHero)
        {
            float percentDamage = finalDamage / def.myStats.GetMaxStat(StatTypes.HEALTH);
            float threshold = MIN_DAMAGE_TO_PROLONG_THANE_SONG;
            if (def.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = def as Monster;
                if (mn.isBoss)
                {
                    threshold = MIN_BOSS_DAMAGE_TO_PROLONG_THANE_SONG;
                }
            }
            if (percentDamage >= threshold)
            {
                int durExtend = 2;
                if (MapMasterScript.GetGridDistance(atk.GetPos(), def.GetPos()) > 1)
                {
                    durExtend = 1;
                }
                if (StatBlock.activeSongs.Count > 0)
                {
                    GameMasterScript.heroPCActor.TryIncreaseSongDuration(durExtend);
                }
            }
        }

        if (bufferedCombatData != null)
        {
            bufferedCombatData.mitigationPercent = finalDamage / origDamage;
        }

        //Debug.Log("And final is: " + finalDamage);

        // NEW: If it was a melee attack of some kind FROM hero TO a monster, maybe spawn a powerup?
        if (defenderIsMonster && attackerIsHero && UnityEngine.Random.Range(0, 1f) <= GameMasterScript.MELEE_POWERUP_WHACK_CHANCE)
        {
            if (def.ReadActorData("droppowerup") != 1)
            {
                Monster mn = def as Monster;
                if (mn.GetXPModToPlayer() >= 0.2f)
                {
                    if (bType == AttackType.ATTACK && MapMasterScript.GetGridDistance(atk.GetPos(), def.GetPos()) == 1)
                    {
                        LootGeneratorScript.SpawnRandomPowerup(def as Monster, false, false);
                        def.SetActorData("droppowerup", 1);
                    }
                }
            }
        }

        if (defenderIsMonster && MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR 
            && def.actorUniqueID != GameMasterScript.heroPCActor.GetMonsterPetID())
        {
            Monster mn = def as Monster;
            if (mn.surpressTraits || mn.isInCorral)
            {
                finalDamage = 0;
            }
        }

        if (defenderIsHero)
        {
            finalDamage = GameMasterScript.heroPCActor.TryAdjustDamageTakenThisTurnToAvoidSpikes(finalDamage);
        }

        return finalDamage;
    }

    // Use this when searching for direction of attack, FROM atk TO defender
    public static float GetAngleBetweenPoints(Vector2 p1, Vector2 p2)
    {
        Vector2 targetDir = (p2 - p1).normalized;
        float angle = Vector2.Angle(targetDir, Vector2.up);
        if (p2.x < p1.x)
        {
            angle *= -1f;
        }
        return angle;

    }

    public static Directions GetDirection(Actor atk, Actor def)
    {
        if ((atk == null) || (def == null)) return Directions.NORTH;
        float angle = GetAngleBetweenPoints(atk.GetPos(), def.GetPos());
        Directions directionOfAttack = MapMasterScript.GetDirectionFromAngle(angle);
        return directionOfAttack;
    }

    public static Directions GetDirection(Vector2 v1, Vector2 v2)
    {
        float angle = GetAngleBetweenPoints(v1, v2);
        Directions directionOfAttack = MapMasterScript.GetDirectionFromAngle(angle);
        return directionOfAttack;
    }

    public static void CheckGaelmyddAxeProc(Actor act)
    {
        if (act.GetActorType() != ActorTypes.MONSTER)
        {
            return;
        }
        Monster mn = act as Monster;

        if (mn.monFamily == "robots")
        {
            float chance = mn.GetXPModToPlayer();
            if (chance <= 0) chance = 0.2f;
            if (chance > 1f) chance = 1f;
            if (UnityEngine.Random.Range(0, 1f) <= chance)
            {
                if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("harvestrobot"))
                {
                    GameMasterScript.heroPCActor.myStats.ChangeStat(StatTypes.ENERGY, UnityEngine.Random.Range(3f, 7f), StatDataTypes.CUR, true);
                    GameMasterScript.heroPCActor.myStats.ChangeStat(StatTypes.STAMINA, UnityEngine.Random.Range(3f, 7f), StatDataTypes.CUR, true);
                    GameLogScript.LogWriteStringRef("log_harvest_robot");
                }
            }
        }
    }

    static IEnumerator WaitThenSwingFX(Fighter atk, Fighter def, float time, Weapon eq)
    {
        yield return new WaitForSeconds(time);
        GetSwingEffect(atk, def, eq, false);
    }

    static IEnumerator WaitThenImpactFX(Fighter atk, Fighter def, Weapon wp, bool crit, float time)
    {
        yield return new WaitForSeconds(time);

        GameObject impactSpriteObj = null;

        try { impactSpriteObj = GetImpactEffect(atk, wp, crit); }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("Error trying to get impact effect: " + e);
        }
        if (impactSpriteObj != null)
        {
            //impactSpriteObj.transform.position = def.GetObject().transform.position;
            impactSpriteObj.transform.position = def.GetPos();
            if (wp.isRanged)
            {
                // Fired a projectile, so rotate this impact FX.
                float angle = GetAngleBetweenPoints(atk.GetPos(), def.GetPos());
                Directions directionOfAttack = MapMasterScript.GetDirectionFromAngle(angle);
                impactSpriteObj.transform.Rotate(new Vector3(0, 0, MapMasterScript.directionAngles[(int)directionOfAttack]), Space.Self);
            }
        }

        if (crit)
        {
            try
            {
                GameObject critObj = GetCriticalEffect();
                critObj.transform.position = def.GetPos();
            }
            catch (Exception e)
            {
                if (Debug.isDebugBuild) Debug.Log("ERROR with critical anim: " + e);
            }

        }
    }

    static IEnumerator WaitThenWhiff(Fighter atk, Fighter def, float time)
    {
        yield return new WaitForSeconds(time);
        GameObject whiffSpriteObj = null;

        try { whiffSpriteObj = GetWhiffEffect(); }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("Error: Couldn't do whiff effect, " + e);
        }
        if (whiffSpriteObj != null)
        {
            if (atk.GetObject() != null)
            {
                whiffSpriteObj.transform.position = atk.GetObject().transform.position;
            }
            else
            {
                whiffSpriteObj.transform.position = atk.GetPos();
            }
        }

        StringManager.SetTag(0, atk.displayName);
        StringManager.SetTag(1, def.displayName);
        GameLogScript.LogWriteStringRef("log_attack_miss", def, TextDensity.VERBOSE);
        // before bounce this was -0.15f length
        if (def.GetObject() != null) // 312019 - Extra null check here, as maybe something could die right before this happens and not have an object
        {
        BattleTextManager.NewText(StringManager.GetString("misc_miss"), def.GetObject(), Color.white, 0.15f, 1f, BounceTypes.DOUBLE);
        }
    }

    static IEnumerator WaitThenDivineProtect(Fighter atk, Fighter def, float time)
    {
        yield return new WaitForSeconds(time);
        GameObject whiffSpriteObj = null;

        try { whiffSpriteObj = GetBlockEffect(true, false); }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("Warning: Couldn't get divine protection effect due to " + e);
        }
        if (whiffSpriteObj != null)
        {
            if (atk.objectSet)
            {
                whiffSpriteObj.transform.position = atk.GetObject().transform.position;
            }
            else
            {
                whiffSpriteObj.transform.position = atk.GetPos();
            }
        }

        StringManager.SetTag(0, atk.displayName);
        StringManager.SetTag(1, def.displayName);
        GameLogScript.LogWriteStringRef("log_negate_crit_divine", def);
        BattleTextManager.NewText(StringManager.GetExcitedString("misc_protected"), def.GetObject(), Color.white, 0.0f);
    }

    static IEnumerator WaitThenParry(Fighter atk, Fighter def, float time)
    {
        yield return new WaitForSeconds(time);
        GameObject whiffSpriteObj = null;
        try { whiffSpriteObj = GetParryEffect(); }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("Couldn't get parry effect due to " + e);
        }

        if (whiffSpriteObj != null)
        {
            if (def.objectSet)
            {
                whiffSpriteObj.transform.position = def.GetObject().transform.position;
            }
            else
            {
                whiffSpriteObj.transform.position = def.GetPos();
            }
        }

        StringManager.SetTag(0, def.displayName);
        StringManager.SetTag(1, atk.displayName);
        GameLogScript.LogWriteStringRef("log_parry", def, TextDensity.VERBOSE);
    }

    static IEnumerator WaitThenBlock(Fighter atk, Fighter def, float time)
    {
        yield return new WaitForSeconds(time);
        GameObject whiffSpriteObj = null;

        bool blue = false;

        if (def.GetActorType() == ActorTypes.HERO && def.myStats.CheckHasActiveStatusName("status_heavyguard"))
        {
            blue = true;
        }

        try { whiffSpriteObj = GetBlockEffect(blue, false); }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("Couldn't get block effect due to " + e);
        }

        if (whiffSpriteObj != null)
        {
            if (def.objectSet)
            {
                whiffSpriteObj.transform.position = def.GetObject().transform.position;
            }
            else
            {
                whiffSpriteObj.transform.position = def.GetPos();
            }
        }

        StringManager.SetTag(0, def.displayName);
        StringManager.SetTag(1, atk.displayName);
        GameLogScript.LogWriteStringRef("log_block", def, TextDensity.VERBOSE);
    }

    static IEnumerator WaitThenSendNewText(BattleTextData btd, float time, BounceTypes bounce = BounceTypes.STANDARD)
    {
        yield return new WaitForSeconds(time);
        //btm.NewText(btd); Old way?
        BattleTextManager.NewDamageText(Int32.Parse(btd.text), false, btd.color, btd.btdObj, btd.lengthMod, btd.sizeMod, bounce);
    }


    private static CombatResultPayload ExecuteAttack(Fighter atk, Fighter def, bool offhand, int attackIndex, bool counterAttack)
    {
        CombatResultPayload crp = new CombatResultPayload();
        //Debug.Log("Angle between " + atk.currentPosition + " " + def.currentPosition + " " + GetAngleBetweenPoints(atk.GetPos(), def.GetPos()));
        GameObject attacker = atk.GetObject();
        GameObject defender = def.GetObject();
        StatBlock attackerStats = atk.myStats;
        StatBlock defenderStats = def.myStats;
        EquipmentBlock atkEquip = atk.myEquipment;
        EquipmentBlock defEquip = def.myEquipment;

        bool atkIsHero = atk.IsHero();
        bool defIsHero = def.IsHero();
        bool defIsMonster = def.GetActorType() == ActorTypes.MONSTER;
        bool bothCombatantsVisible = false;
        if (atkIsHero || defIsHero)
        {
            bothCombatantsVisible = true;
        }
        else if (GameMasterScript.heroPCActor.visibleTilesArray[(int)atk.GetPos().x, (int)atk.GetPos().y]
            || GameMasterScript.heroPCActor.visibleTilesArray[(int)def.GetPos().x, (int)def.GetPos().y])
        {
            bothCombatantsVisible = true;
        }


        if (atk == null)
        {
            CombatResultPayload nullCRP = new CombatResultPayload();
            nullCRP.result = CombatResult.NOTHING;
            nullCRP.waitTime = 0.0f;
            if (Debug.isDebugBuild) Debug.Log("WARNING: Null attacker in attack executed.");
            return nullCRP;
        }
        if (def == null)
        {
            CombatResultPayload nullCRP = new CombatResultPayload();
            nullCRP.result = CombatResult.NOTHING;
            nullCRP.waitTime = 0.0f;
            if (Debug.isDebugBuild) Debug.Log("WARNING: Null defender in attack executed.");
            return nullCRP;
        }
        Weapon attackerWeapon = atk.myEquipment.GetWeapon();
        Weapon mainhandWeapon = atk.myEquipment.GetWeapon();

        DamageTypes forceElement = attackerWeapon.damType;

        if (offhand)
        {
            attackerWeapon = atk.myEquipment.GetOffhandWeapon();
            if (atkIsHero) GameMasterScript.gmsSingleton.SetTempGameData("mainhand", 0);

            if (atkIsHero && attackerWeapon == null && atk.myStats.CheckHasStatusName("status_unarmedfighting2"))
            {
                attackerWeapon = GameMasterScript.kickDummy;                
            }
            else if (atkIsHero && attackerWeapon == null && atk.myStats.CheckHasStatusName("status_unarmedfighting1"))
            {
                attackerWeapon = atk.myEquipment.GetWeapon();
            }
        }
        else
        {
            if (atk.GetActorType() == ActorTypes.HERO) GameMasterScript.gmsSingleton.SetTempGameData("mainhand", 1);
        }

        bool attackerWeaponNull = false;
        if (attackerWeapon == null)
        {
            attackerWeaponNull = true;
        }

        bool penetrate = false;
        if (atkIsHero)
        {
            if (atk.myStats.CheckHasStatusName("status_mmpenetrating"))
            {
                penetrate = true;
            }
        }

        FighterBattleData attackerFBD = atk.cachedBattleData;
        FighterBattleData defenderFBD = def.cachedBattleData;

        float baseDamage = attackerFBD.physicalWeaponDamage + UnityEngine.Random.Range(4, 8f);

        int localAuraOnAttacker = -1;
        int localAuraOnDefender = -1;
        if (!MapMasterScript.activeMap.IsItemWorld())
        {
            localAuraOnAttacker = MapMasterScript.GetItemWorldAura(atk.GetPos(), false);
            localAuraOnDefender = MapMasterScript.GetItemWorldAura(def.GetPos(), false);
        }

        if (offhand)
        {
            baseDamage = attackerFBD.physicalWeaponOffhandDamage + UnityEngine.Random.Range(4, 8f);
        }

        CombatDataPack cdp = new CombatDataPack();
        cdp.ResetData();
        AddToCombatStack(cdp);

        cdp.damage = baseDamage; // This is the base output damage of the attack.
        cdp.attackerWeapon = attackerWeapon;
        cdp.atkType = AttackType.ATTACK;
        cdp.attacker = atk;
        cdp.defender = def;
        cdp.flavorDamageType = attackerWeapon.flavorDamType;
        cdp.damageType = attackerWeapon.damType;

        float angle = GetAngleBetweenPoints(atk.GetPos(), def.GetPos());
        Directions directionOfAttack = MapMasterScript.GetDirectionFromAngle(angle);
        bufferedCombatData.attackDirection = directionOfAttack;

        // Process accuracy stuff here.

        bool autoBarrier = def.myStats.CheckHasStatusName("autobarrier");

        if (atkIsHero && counterAttack)
        {
            // Don't proc "On Attacked" statuses when counter attacking
            // #todo Limit to only stuff bad for the player?
        }
        else
        {
            def.myStats.CheckRunAndTickAllStatuses(StatusTrigger.ATTACKED);
        }



        cmsInstance.ProcessQueuedEffects();

        if (!atk.myStats.IsAlive())
        {
            GameMasterScript.AddToDeadQueue(atk);
            crp.result = CombatResult.DODGE;
            crp.waitTime = 0.05f;
            RemoveFromCombatStack(cdp);
            return crp;
        }

        bool isAttackerWeaponRanged = atk.myEquipment.IsWeaponRanged(attackerWeapon);
        /* if (offhand)
        {
            isAttackerWeaponRanged = atk.myEquipment.IsWeaponRanged(atk.myEquipment.GetOffhandWeapon());
        } */

        float missRoll = UnityEngine.Random.Range(0, 100f);

        float calcAccuracy = atk.myStats.GetStat(StatTypes.ACCURACY, StatDataTypes.CUR) * bufferedCombatData.accuracyMod;
        calcAccuracy += bufferedCombatData.accuracyModFlat;

        if (def.actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID())
        {
            // Pet inherits some dodge?
            float pDodge = GameMasterScript.heroPCActor.CalculateDodge();
            calcAccuracy += (pDodge / 2f);
        }

        // HARDCODED - passive
        if (defIsHero)
        {
            calcAccuracy -= (float)def.myEquipment.GetDodgeFromArmor();

            // Make adventure mode a bit more forgiving.
            if (GameMasterScript.HelpPlayerInAdventureMode())
            {
                calcAccuracy -= 20f;
            }
        }

        bool defenderSubmerged = false;

        if (isAttackerWeaponRanged)
        {
            bool arrowCatch = false;
            if (UnityEngine.Random.Range(0, 1f) <= 0.25f && def.myStats.CheckHasStatusName("status_arrowcatch"))
            {
                arrowCatch = true;
                if (def.myJob.jobEnum == CharacterJobs.BUDOKA && def.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.25f && UnityEngine.Random.Range(0, 1f) <= 0.25f)
                {
                    GameLogScript.LogWriteStringRef("log_budoka_easteregg");
                }
            }
            bool turtleShield = false;
            if (def.myStats.CheckHasStatusName("status_turtleshield"))
            {
                Directions angleOfAttack = MapMasterScript.GetDirectionFromAngle(GetAngleBetweenPoints(atk.GetPos(), def.GetPos()));
                foreach (StatusEffect se in def.myStats.GetAllStatuses())
                {
                    if (se.refName == "status_turtleshield")
                    {
                        //Debug.Log(angleOfAttack + " vs " + se.direction);
                        if (se.direction == MapMasterScript.oppositeDirections[(int)angleOfAttack])
                        {
                            turtleShield = true;
                            break;
                        }

                    }
                }
            }
            if ((arrowCatch || turtleShield) && atk != def && !counterAttack)
            {
                StringManager.SetTag(0, def.displayName);

                if (arrowCatch)
                {
                    GameLogScript.LogWriteStringRef("log_attack_catch", def);
                }
                else
                {
                    GameLogScript.LogWriteStringRef("log_attack_reflect", def);
                }

                bufferedCombatData.counterAttack = true;

                CombatManagerScript.GenerateSpecificEffectAnimation(def.GetPos(), "ReflectEffect", null, false);

                CombatResultPayload parryRes = ExecuteAttack(atk, atk, false, 0, true);
                GetSwingEffect(atk, def, attackerWeapon, false);
                cmsInstance.StartCoroutine(WaitThenSwingFX(def, atk, 0.12f, attackerWeapon));

                if (parryRes.result == CombatResult.MONSTERDIED || parryRes.result == CombatResult.PLAYERDIED)
                {
                    GameMasterScript.AddToDeadQueue(atk);
                }
                bufferedCombatData.addToWaitTime += parryRes.waitTime;


                crp.result = CombatResult.DODGE;
                crp.waitTime = bufferedCombatData.addToWaitTime;
                RemoveFromCombatStack(cdp);
                return crp;
            }

            defenderSubmerged = MapMasterScript.CheckIfSubmerged(def);
            if (defenderSubmerged)
            {
                calcAccuracy -= 30f;
            }
        }

        // END HARDCODED

        if (offhand)
        {
            calcAccuracy *= attackerFBD.offhandAccuracyMod;
        }
        else
        {
            calcAccuracy *= attackerFBD.mainhandAccuracyMod;
        }

        if (atkIsHero && mainhandWeapon != null && mainhandWeapon.twoHanded
            && !mainhandWeapon.isRanged && atk.myStats.CheckHasStatusName("twohand_specialist"))
        {
            calcAccuracy += 10f;
        }

        //calcAccuracy += atk.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE) * .04f; // Defeat dodge.

        if (calcAccuracy < 5f)
        {
            calcAccuracy = 5f;
        }
        else if (calcAccuracy >= 95f)
        {
            calcAccuracy = 95f;
        }

        if (!attackerWeaponNull && attackerWeapon.actorRefName == "weapon_sharktoothaxe")
        {
            calcAccuracy -= 20f;
        }

        calcAccuracy -= def.myEquipment.GetDodgeFromArmor();

        if (atkIsHero)
        {
            if (atk.myStats.CheckHasStatusName("magicmirrors") && !offhand)
            {
                calcAccuracy += 5000f;
            }
            if (atk.myStats.CheckHasStatusName("status_fatemiss"))
            {
                if (calcAccuracy > 85f)
                {
                    calcAccuracy = 85f;
                }
                else
                {
                    calcAccuracy -= 15f;
                }
            }
        }

        //Debug.Log(atk.actorRefName + " " + calcAccuracy);

        if (def.ReadActorData("feared") == 1)
        {
            calcAccuracy += 5000f;
        }

        if (missRoll > calcAccuracy)
        {
            if (bothCombatantsVisible)
            {
                cmsInstance.StartCoroutine(WaitThenWhiff(atk, def, GameMasterScript.baseAttackAnimationTime * attackIndex));
            }

            if (atkIsHero)
            {
                if (atk.myStats.CheckHasStatusName("status_mmvengeance"))
                {
                    atk.SetActorData(offhand ? "missed_oh" : "missed_mh", 1);
                }                
            }

            def.myStats.CheckRunAndTickAllStatuses(StatusTrigger.ATTACKDODGE);
            if (defIsMonster)
            {
                Monster mon = def as Monster;
                mon.lastActorAttackedBy = atk;
                mon.AddAggro(atk, 10f); // 10 for taking a swing.
            }
            crp.result = CombatResult.DODGE;
            crp.waitTime = bufferedCombatData.addToWaitTime;
            RemoveFromCombatStack(cdp);

            if (defenderSubmerged)
            {
                StringManager.SetTag(0, def.displayName);
                StringManager.SetTag(1, atk.displayName);
                GameLogScript.GameLogWrite(StringManager.GetString("log_dodge_water"), atk);
            }

            GetSwingEffect(atk, def, attackerWeapon, false); // Whiff projectile

            return crp;
        }

        bool attackParried = CheckForParry(atk, def, penetrate);

        if (attackParried)
        {
            DoParryStuff(atk, def, bothCombatantsVisible, attackIndex, counterAttack);

            if (GameStartData.NewGamePlus >= 2 && !MysteryDungeonManager.InOrCreatingMysteryDungeon()) // in ng++, parry doesnt reduce all damage to 0
            {

            }
            else
            {
                crp.result = CombatResult.DODGE;
                crp.waitTime = bufferedCombatData.addToWaitTime;
                RemoveFromCombatStack(cdp);
                return crp;
            }
        }

        int distanceBetweenAttackerAndDefender = MapMasterScript.GetGridDistance(atk.GetPos(), def.GetPos());

        bool monsterDiedInHamedo = false;
        if (defIsHero && UnityEngine.Random.Range(0, 1f) <= BUDOKA_HAMEDO_CHANCE && def.myStats.CheckHasStatusName("emblem_budokaemblem_tier2_hamedo"))
        {
            if (!def.myEquipment.IsCurrentWeaponRanged() && distanceBetweenAttackerAndDefender == 1)
            {
                // Possible to hamedo attack
                BattleTextManager.NewText(StringManager.GetString("misc_battle_precounter"), def.GetObject(), Color.green, 1.0f);

                GameLogScript.LogWriteStringRef("hamedo_counter");
                CombatResultPayload parryRes = ExecuteAttack(def, atk, false, 0, true);
                Directions dirOfAttack = MapMasterScript.GetDirectionFromAngle(CombatManagerScript.GetAngleBetweenPoints(def.GetPos(), atk.GetPos()));
                GameMasterScript.heroPCActor.myAnimatable.SetAnimDirectional("Attack", dirOfAttack, dirOfAttack);
                if ((parryRes.result == CombatResult.MONSTERDIED) || (parryRes.result == CombatResult.PLAYERDIED))
                {
                    GameMasterScript.AddToDeadQueue(atk);
                    monsterDiedInHamedo = true;
                }
                bufferedCombatData.addToWaitTime += parryRes.waitTime;
            }
        }

        if (monsterDiedInHamedo)
        {
            crp.result = CombatResult.MONSTERDIED;
            crp.waitTime = bufferedCombatData.addToWaitTime;
            return crp;
        }

        // *** STANDARD DAMAGE FORMULA ***

        DamageTypes dt = cdp.damageType;

        if (localAuraOnAttacker == (int)ItemWorldAuras.MELEEDAMAGEPLUS50)
        {
            if (!atk.myEquipment.IsWeaponRanged(attackerWeapon))
            {
                baseDamage *= 1.5f;
            }
        }

        if (localAuraOnAttacker == (int)ItemWorldAuras.RANGEDDAMAGEPLUS50)
        {
            if (atk.myEquipment.IsWeaponRanged(attackerWeapon))
            {
                baseDamage *= 1.5f;
            }
        }

        if (atkIsHero)
        {
            if (atk.myStats.CheckHasStatusName("status_mmvengeance"))
            {
                bool missedOH = atk.ReadActorData("missed_oh") == 1;
                bool missedMH = atk.ReadActorData("missed_mh") == 1;
                if (offhand && missedOH ||
                    !offhand && missedMH)
                {
                    baseDamage *= 1.75f;
                    BattleTextManager.NewText(StringManager.GetString("vengeance_bt"), atk.GetObject(), Color.red, 0f);
                    atk.RemoveActorData("missed_mh");
                    atk.RemoveActorData("missed_oh");
                }
            }
            if (counterAttack && atk.myStats.CheckHasStatusName("status_alwaysriposte"))
            {
                baseDamage *= 1.3f;
            }
        }

        atk.myStats.CheckRunAndTickAllStatuses(StatusTrigger.ATTACK);
        atk.myStats.RemoveQueuedStatuses();

        float processedDamage = ProcessDamage(atk, def, AttackType.ATTACK, new DamageCarrier(baseDamage, dt), null);

        //Debug.Log("Processed damage: " + processedDamage + " from " + atk.actorRefName);

        if (atkIsHero)
        {
            Actor checkSerpent = MapMasterScript.GetTile(def.GetPos()).GetActorRef("obj_flameserpent");
            if (checkSerpent != null)
            {
                if (checkSerpent.actorfaction == Faction.PLAYER)
                {
                    float flameDmg = baseDamage * 0.15f;
                    flameDmg = ProcessDamage(atk, def, AttackType.ATTACK, new DamageCarrier(flameDmg, DamageTypes.FIRE), null);
                    def.TakeDamage(flameDmg, DamageTypes.FIRE);
                    BattleTextManager.NewDamageText((int)flameDmg, false, Color.white, def.GetObject(), 0f, 0.8f);
                    StringManager.SetTag(0, checkSerpent.displayName);
                    StringManager.SetTag(1, def.displayName);
                    StringManager.SetTag(2, ((int)flameDmg).ToString());
                    GameLogScript.LogWriteStringRef("log_flameserpent_extraburn", atk);
                }
            }
        }

        if (counterAttack && atk.myStats.CheckHasStatusName("status_dustinthewind"))
        {
            Fighter ft = atk as Fighter;
            float dmg = (ft.cachedBattleData.physicalWeaponDamage * 0.5f) + UnityEngine.Random.Range(0, 4);
            dmg = ProcessDamage(atk, def, AttackType.ATTACK, new DamageCarrier(dmg, DamageTypes.LIGHTNING), null);
            processedDamage += dmg;
            GenerateSpecificEffectAnimation(def.GetPos(), "GreenWindEffect", null);
            StringManager.SetTag(0, def.displayName);
            StringManager.SetTag(1, "<#fffb00>" + (int)dmg + "</color>");
            string razorString = StringManager.GetString("log_combat_razorwinds");
            GameLogScript.GameLogWrite(razorString, def);
        }

        // on attack trigger was here, but I think we want to do this BEFORE process dmg.

        bool crit = false;

        float critRoll = UnityEngine.Random.Range(0, 1.0f);

        float critChance = 0.0f;
        if (isAttackerWeaponRanged)
        {
            critChance = atk.cachedBattleData.critRangedChance;
        }
        else
        {
            critChance = atk.cachedBattleData.critMeleeChance;
        }

        // HARDCODED - Brigand Smoke Cloud
        if (atkIsHero)
        {
            MapTileData mtd = MapMasterScript.GetTile(atk.GetPos());
            if (mtd.CheckActorRef("obj_smokecloud"))
            {
                if (!def.CheckTarget(atk)) critChance += 0.15f;
            }
            if (defIsMonster)
            {
                Monster defMon = def as Monster;
                if ((defMon.isChampion || defMon.isBoss) && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_wildchildemblem_tier1_champion"))
                {
                    critChance += 0.05f;
                }
            }

            if (!attackerWeaponNull && attackerWeapon.weaponType == WeaponTypes.WHIP && atk.myStats.CheckHasStatusName("whipmastery3"))
            {
                int numEnemies = GameMasterScript.gmsSingleton.ReadTempGameData("enemiesattacked");
                //numEnemies--;
                critChance += (WHIP_MASTERY3_BONUSCRIT_PER_ENEMY * numEnemies);
                
            }
        }

        if (critChance >= CRIT_CHANCE_MAX)
        {
            critChance = CRIT_CHANCE_MAX;
        }

        if (localAuraOnAttacker == (int)ItemWorldAuras.DOUBLECRITICAL)
        {
            critChance *= 2f;
        }

        critChance += bufferedCombatData.chanceToCritFlat;
        critChance *= bufferedCombatData.chanceToCrit;

        //string extraCritMessage = "";    

        if (localAuraOnAttacker == (int)ItemWorldAuras.NOCRITICAL)
        {
            critRoll = 100f;
        }

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.V))
        {
            critRoll = 0;
            critChance = 100f;
        }
#endif


        if (critRoll <= critChance)
        {
            bool negateCrit = false;
            if (defIsHero)
            {
                if (def.myStats.CheckHasStatusName("status_steelresolve"))
                {
                    GameLogScript.LogWriteStringRef("log_steelresolve_crit");
                    negateCrit = true;
                }
                else if (UnityEngine.Random.Range(0,1f) <= CHANCE_ABSORBINGMOD_NEGATE_CRIT && def.myStats.CheckHasStatusName("xp2_absorbingshield"))
                {
                    GameLogScript.LogWriteStringRef("log_absorbing_negate_crit");
                    negateCrit = true;
                }
            }
            if (!negateCrit)
            {
                if (isAttackerWeaponRanged)
                {
                    processedDamage *= atk.cachedBattleData.critRangedDamageMult;
                }
                else
                {
                    processedDamage *= atk.cachedBattleData.critMeleeDamageMult;
                }
                crit = true;

                if (PlayerOptions.screenFlashes && (atkIsHero || defIsHero))
                {
                    UIManagerScript.FlashWhite(0.15f);
                    GameMasterScript.cameraScript.AddScreenshake(0.2f);
                }

                bufferedCombatData.criticalHit = true;

                // HARDCODED - passive
                if (defIsHero)
                {
                    if (UnityEngine.Random.Range(0, 1f) <= 0.33f && def.myStats.CheckHasStatusName("status_divineprotection"))
                    {
                        cmsInstance.StartCoroutine(WaitThenDivineProtect(atk, def, GameMasterScript.baseAttackAnimationTime * attackIndex));
                        crp.result = CombatResult.DODGE;
                        crp.waitTime = bufferedCombatData.addToWaitTime;
                        RemoveFromCombatStack(cdp);
                        return crp;
                    }
                }

                // END HARDCODED

                atk.myStats.CheckRunAndTickAllStatuses(StatusTrigger.ONCRIT);

                if (!attackerWeaponNull && attackerWeapon.weaponType == WeaponTypes.MACE && !atkIsHero)
                {
                    float percentDamage = (def.myStats.GetCurStat(StatTypes.HEALTH) * 0.1f);
                    processedDamage += percentDamage;
                    BattleTextManager.NewText(StringManager.GetString("effect_crush_health_btxt"), def.GetObject(), Color.yellow, 0.15f);
                }

                if (atkIsHero)
                {
                    if (GameMasterScript.heroPCActor.CanCollectWildCards() && defIsMonster)
                    {
                        Monster mn = def as Monster;
                        float localChance = mn.GetXPModToPlayer();
                        if (MapMasterScript.activeMap.IsJobTrialFloor()) localChance = 0.75f;
                        if (UnityEngine.Random.Range(0.01f, 1f) <= mn.GetXPModToPlayer())
                        {
                            GameMasterScript.heroPCActor.DrawWildCard();
                        }
                    }

                    if (atk.myStats.CheckHasStatusName("status_deadlyfocus"))
                    {
                        atk.myAbilities.TickAllCooldowns();
                        uims.RefreshAbilityCooldowns();
                    }

                    // Special weapon crit FX
                    switch (attackerWeapon.weaponType)
                    {
                        case WeaponTypes.SWORD:
                            // 100% chance to parry NEXT attack.
                            if (!counterAttack)
                            {
                                GameLogScript.GameLogWrite(StringManager.GetString("log_prepare_parry"), GameMasterScript.heroPCActor);
                                GameMasterScript.heroPCActor.actorFlags[(int)ActorFlags.PARRYNEXTATTACK] = true;
                                GameMasterScript.heroPCActor.myStats.AddStatusByRef("swordparry", GameMasterScript.heroPCActor, 99);
                            }
                            break;
                        case WeaponTypes.MACE:
                            float percentDamage = (def.myStats.GetCurStat(StatTypes.HEALTH) * 0.1f);
                            processedDamage += percentDamage;
                            break;
                        case WeaponTypes.CLAW:
                            if (defIsMonster)
                            {
                                Monster mn = def as Monster;
                                if (mn.GetXPModToPlayer() >= 0.25f)
                                {
                                    float healAmount = GameMasterScript.heroPCActor.myStats.GetMaxStat(StatTypes.HEALTH) * 0.05f;
                                    GameMasterScript.heroPCActor.myStats.ChangeStat(StatTypes.HEALTH, healAmount, StatDataTypes.CUR, true);
                                    int display = (int)healAmount;
                                    BattleTextManager.NewDamageText(display, true, Color.green, atk.GetObject(), 0.3f, 1f);
                                    StringManager.SetTag(0, display.ToString());
                                    GameLogScript.LogWriteStringRef("log_ability_hp_gain");
                                }
                            }
                            break;
                        case WeaponTypes.DAGGER:
                            if (!(offhand && GameMasterScript.heroPCActor.ReadActorData("mh_dagger_crit_thisturn") == 1))
                            {
                                GameMasterScript.heroPCActor.ChangeCT(DAGGER_CRIT_CT);
                                StringManager.SetTag(0, ((int)DAGGER_CRIT_CT).ToString());
                                GameLogScript.LogWriteStringRef("log_gain_chargetime", GameMasterScript.heroPCActor, TextDensity.VERBOSE);
                                if (GameMasterScript.heroPCActor.actionTimer >= 200)
                                {
                                    GameMasterScript.gmsSingleton.SetTempGameData("hero_overflowct", 1);
                                }
                            }
                            if (!offhand)
                            {
                                GameMasterScript.heroPCActor.SetActorData("mh_dagger_crit_thisturn", 1);
                            }
                            break;
                        case WeaponTypes.AXE:
                            StringManager.SetTag(0, def.displayName);
                            GameLogScript.LogWriteStringRef("log_axe_shatter");
                            def.actorFlags[(int)ActorFlags.EXTRADAMAGEFROMAXE] = true;
                            def.myStats.AddStatusByRef("axebreak", atk, 15);
                            break;
                        case WeaponTypes.SPEAR:
                            StatusEffect rooted = new StatusEffect();
                            StatusEffect template = GameMasterScript.FindStatusTemplateByName("status_rooted");
                            rooted.CopyStatusFromTemplate(template);
                            rooted.curDuration = 3;
                            rooted.maxDuration = 3;
                            def.myStats.AddStatus(rooted, atk);
                            StringManager.SetTag(0, def.displayName);
                            GameLogScript.LogWriteStringRef("log_def_rooted", atk, TextDensity.VERBOSE);
                            break;
                        case WeaponTypes.WHIP:
                            StatusEffect constricted = new StatusEffect();
                            template = GameMasterScript.FindStatusTemplateByName("status_whipconstrict");
                            constricted.CopyStatusFromTemplate(template);
                            constricted.curDuration = 2;
                            constricted.maxDuration = 2;
                            def.myStats.AddStatus(constricted, atk);
                            StringManager.SetTag(0, def.displayName);
                            //GameLogScript.LogWriteStringRef("log_def_rooted", atk, TextDensity.VERBOSE);
                            break;
                    }
                }                

                // HARDCODED ABILITY SKILL
                if (atkIsHero && defIsMonster
                    && atk.myStats.CheckHasStatusName("status_qimasterypassive"))
                {
                    Monster mn = def as Monster;
                    if (mn.GetXPModToPlayer() >= 0.2f || GameMasterScript.gmsSingleton.gameMode == GameModes.ADVENTURE)
                    {
                        int energyRestore = UnityEngine.Random.Range(3, 6);
                        atk.myStats.ChangeStat(StatTypes.ENERGY, energyRestore, StatDataTypes.CUR, true);

                        StringManager.SetTag(0, energyRestore.ToString());
                        StringManager.SetTag(1, StringManager.GetString("abil_skill_qimastery_name"));
                        GameLogScript.LogWriteStringRef("log_restore_energy_skill", GameMasterScript.heroPCActor, TextDensity.VERBOSE);
                    }
                }
            }
        }


        // #todo Previously, we showed a weapon pivot swing animation on crit. As of 11/11 this looks buggy.
        //cmsInstance.StartCoroutine(WaitThenSwingFX(atk, def, (GameMasterScript.baseAttackAnimationTime * attackIndex), attackerWeapon));

        float damage = processedDamage;

        //Debug.Log("NOW DAMAGE IS " + damage);

        if (attackerWeapon.weaponType == WeaponTypes.DAGGER)
        {
            damage = BoostDamageWithDagger(atk, damage);
        }
        else if (attackerWeapon.weaponType == WeaponTypes.CLAW)
        {
            damage = BoostDamageWithClaw(atk, damage);
        }

        bool blockAttack = CheckForBlock(atk, def, penetrate, damage, autoBarrier);

        if (blockAttack)
        {
            damage = bufferedCombatData.damage; // This was modified during block
        }
        else
        {
            if (defIsHero && def.myEquipment.IsOffhandShield())
            {
                def.myStats.CheckRunAndTickAllStatuses(StatusTrigger.ATTACK_NOBLOCK);
            }
        }

        if (damageVariance >= 0)
        {
            float min = damage * (1 - damageVariance);
            float max = damage * (1 + damageVariance);
            damage = UnityEngine.Random.Range(min, max);
        }
        // *** STANDARD DAMAGE FORMULA ***

        if (damage < 0)
        {
            damage = 0;
        }

        // Feed the data to the actor better than this - used cache combat data.

        bufferedCombatData.damage = damage;

        atk.lastDirectionUsedAttack = directionOfAttack;
        def.lastDirectionAttackedFrom = directionOfAttack;

        //Debug.Log("Attack angle of " + atk.displayName + " is " + bufferedCombatData.attackAngle);

        //Debug.Log("attack damage was: " + damage + " " + bufferedCombatData.damageMod + " " + bufferedCombatData.damageModPercent);

        int damDifference = 0;
        if (bufferedCombatData.damageMod != 0 || bufferedCombatData.damageModPercent != 1.0f)
        {
            damage += bufferedCombatData.damageMod;

            float diff = damage - (bufferedCombatData.damageModPercent * damage);
            damage *= bufferedCombatData.damageModPercent;
            damDifference = (int)bufferedCombatData.damageMod + (int)diff;
        }

        //Debug.Log("is now: " + damage + " " + bufferedCombatData.damageMod + " " + bufferedCombatData.damageModPercent);

        //Debug.Log("<color=green>Final damage: " + damage + "</color>");

        if (attackerWeapon.range > 2 && distanceBetweenAttackerAndDefender == 1 && atk.GetActorType() == ActorTypes.HERO)
        {
            if (atk.myStats.CheckHasActiveStatusName("status_pointblankshot"))
            {
                damage *= 1.25f;
            }
        }

        if (atk.myEquipment.GetWeaponFlag(EquipmentFlags.MELEEPENALTY))
        {
            if (distanceBetweenAttackerAndDefender <= 1)
            {
                if (!atk.myStats.CheckHasStatusName("status_preciseshot"))
                {
                    damage *= 0.5f; // bow damage cut in half at melee range
                }
            }
        }
        else if (attackerWeapon.range > 1 && atk.myEquipment.GetWeaponFlag(EquipmentFlags.RANGEPENALTY))
        {
            if (distanceBetweenAttackerAndDefender > 1)
            {
                if (atkIsHero && atk.myEquipment.GetWeaponType() == WeaponTypes.SPEAR && atk.myStats.CheckHasStatusName("spearmastery2"))
                {
                    damage *= 1.0f;
                }
                else
                {
                    damage *= 0.6f;
                }

            }

        }

        if (attackParried)
        {
            damage *= GameStartData.NGPLUSPLUS_PARRY_DAMAGE_MODIFIER;
        }

        if (damage < 0)
        {
            //Debug.Log("WARNING: WHY WAS DAMAGE " + damage + " from " + atk.actorRefName + " to " + def.actorRefName + " LESS THAN 0?");
            damage = 0;
        }

        CheckForBowRicochet(atk, def, crit, damage);

        bool alive = def.TakeDamage(damage, dt); // Was PHYSICAL before. Why not use weapon dmg type...?

        bool mentionMitigatedDamage = false;

        if (defIsMonster)
        {
            // attacked a monster!
            Monster mon = def as Monster;
            mon.AddAggro(atk, damage);
            if (mon.myEquipment.GetArmor() != null)
            {
                if (mon.myEquipment.GetArmor().resistMessage)
                {
                    mentionMitigatedDamage = true;
                }
            }
        }

        // Damage text

        Color textColor = Color.grey;

        if (bufferedCombatData.mitigationPercent <= 0.5f && mentionMitigatedDamage) // Heavily mitigated damage is notated?
        {
            textColor = Color.grey;
        }

        if (crit)
        {
            textColor = Color.red; // was yellow
        }

        if (def.GetActorType() == ActorTypes.HERO)
        {
            textColor = BattleTextManager.playerDamageColor;
        }

        //Debug.Log(processedDamage + " " + damage + " " + bufferedCombatData.damage + " " + damagePayload.damage + " " + damagePayload.currentDamageValue);

        string numberString = ((int)damage).ToString();
        if (bufferedCombatData.damageMod != 0 && !bufferedCombatData.silent)
        {
            //numberString += " <color=green>(+" + (int)bufferedCombatData.damageMod + ")</color>";
        }

        // Numerical damage popup

        bool ineffectiveAttack = false;

        if (bufferedCombatData.mitigationPercent <= 0.75f && mentionMitigatedDamage) // Heavily mitigated damage is notated?
        {
            ineffectiveAttack = true;
        }

        //Don't display 0 damage if we soaked the real damage with a shield.
        if (damage == 0f && def.ReadActorData("hide_next_zero_battledamage") == 1)
        {
            def.SetActorData("hide_next_zero_battledamage", 0);
        }
        else
        {
            if (!crit)
            {
                BattleTextData btd = new BattleTextData(numberString, defender, textColor, false);
                cmsInstance.StartCoroutine(WaitThenSendNewText(btd, GameMasterScript.baseAttackAnimationTime * attackIndex));
            }
            else
            {
                //string s = numberString + "!";
                BattleTextData btd = new BattleTextData(numberString, defender, textColor, true);
                btd.lengthMod = 0.4f;
                btd.sizeMod = 1.8f;
                btd.color = Color.red;

                // Crit battle text regular attack
                cmsInstance.StartCoroutine(WaitThenSendNewText(btd, GameMasterScript.baseAttackAnimationTime * attackIndex));
            }

        }


        string damWord = "attacks";

        switch (bufferedCombatData.flavorDamageType)
        {
            case FlavorDamageTypes.BLUNT:
                damWord = bluntDamageWords[UnityEngine.Random.Range(0, bluntDamageWords.Length)];
                break;
            case FlavorDamageTypes.SLASH:
                damWord = slashDamageWords[UnityEngine.Random.Range(0, slashDamageWords.Length)];
                break;
            case FlavorDamageTypes.PIERCE:
                damWord = pierceDamageWords[UnityEngine.Random.Range(0, pierceDamageWords.Length)];
                break;
            case FlavorDamageTypes.BITE:
                damWord = biteDamageWords[UnityEngine.Random.Range(0, biteDamageWords.Length)];
                break;
        }

        //if (((bufferedCombatData.damageMod == 0) && (bufferedCombatData.damageModPercent == 1.0f)) || (bufferedCombatData.silent))
        {
            string color = "";
        if (defIsHero)
            {
            color = UIManagerScript.orangeHexColor;
            }
            else
            {
            color = "<#fffb00>";
            }

            string defDisplay = "";

        if (defIsMonster)
            {
                Monster dMon = def as Monster;
                if (dMon.isChampion)
                {
                    defDisplay = dMon.displayName;
                }
                else
                {
                    defDisplay = UIManagerScript.cyanHexColor + dMon.displayName + "</color>";
                }
            }
            else
            {
                defDisplay = UIManagerScript.orangeHexColor + def.displayName + "</color>";
            }

            string critText = "";

            if (crit)
            {
            if (StringManager.DoesCurrentLanguageUseSpaces())
            {
                critText = " " + StringManager.GetString("misc_crit");
            }
            else
            {
                critText = StringManager.GetString("misc_crit");
            }
            }

            string offhandText = "";

            if (offhand)
            {
                offhandText = UIManagerScript.cyanHexColor + StringManager.GetString("log_offhand_attack") + "</color>";
            }

            StringManager.SetTag(0, atk.displayName);
            StringManager.SetTag(1, damWord);
            StringManager.SetTag(2, defDisplay);
            StringManager.SetTag(3, color + (int)damage + "</color>");
            StringManager.SetTag(4, critText);
            StringManager.SetTag(5, offhandText);

            if (!blockAttack)
            {
                GameLogScript.LogWriteStringRef("log_standard_attack", def);
            }
            else
            {
                GameLogScript.LogWriteStringRef("log_standard_attack_block", def);
            }

        }

        if (ineffectiveAttack)
        {
            StringManager.SetTag(0, atk.displayName);
            GameLogScript.LogWriteStringRef("log_ineffective_attack");
        }

        bool stunPossible = false;
        float stunChance = 0.0f;

        if (attackerWeapon.weaponType == WeaponTypes.MACE)
        {
            stunPossible = true;
            stunChance = 0.15f;

            if (damage >= (def.myStats.GetMaxStat(StatTypes.HEALTH) * 0.15f) && atk.myStats.CheckHasStatusName("macemastery3"))
            {
                stunChance += 0.15f;
            }
        }

        if (attackerWeapon.weaponType == WeaponTypes.SPECIAL)
        {
            if (UnityEngine.Random.Range(0, 1f) <= 0.15f && attackerWeapon.actorRefName == "weapon_flail")
            {
                // Trip effect
                StatusEffect template = GameMasterScript.FindStatusTemplateByName("status_rooted");

                StringManager.SetTag(0, atk.displayName);
                StringManager.SetTag(1, def.displayName);
                StringManager.SetTag(2, attackerWeapon.displayName);
                GameLogScript.LogWriteStringRef("log_attack_trip_flail", atk);
                StatusEffect rooted = new StatusEffect();
                rooted.CopyStatusFromTemplate(template);
                rooted.maxDuration = 2;
                rooted.curDuration = 2;
                def.myStats.AddStatus(rooted, atk);
            }
        }

        if (atkIsHero)
        {
            if (atk.myStats.CheckHasStatusName("status_shieldmastery") && atk.myEquipment.GetOffhandBlock() > 0f
                && distanceBetweenAttackerAndDefender <= 1
                && !atk.myEquipment.IsWeaponRanged(attackerWeapon))
            {
                stunPossible = true;
                stunChance += 0.15f;
            }

            // player hit a ranged monster in melee range
            if (distanceBetweenAttackerAndDefender <= 1 && def.myEquipment.GetWeaponRange() > 1)
            {
                TutorialManagerScript.hitRangedMonsterInMeleeTurn = GameMasterScript.turnNumber;
            }
        }
        else if (defIsHero && atk.myEquipment.GetWeaponRange() > 1)
        {
            TutorialManagerScript.rangedMonsterHitPlayerCount++;
        }
        
        if (stunPossible)
        {
            if (stunChance > 0.3f) stunChance = 0.3f;
            if (defIsMonster)
            {
                Monster mn = def as Monster;
                if (mn.isChampion)
                {
                    stunChance = 0.1f;
                }
            }
            if (UnityEngine.Random.Range(0, 1f) <= stunChance && def.GetActorType() != ActorTypes.HERO)
            {
                if (!def.myStats.CheckHasStatusName("status_steelresolve"))
                {
                    StringManager.SetTag(0, def.displayName);
                    GameLogScript.LogWriteStringRef("log_stun_reel");
                    StatusEffect se = new StatusEffect();
                    se.CopyStatusFromTemplate(GameMasterScript.FindStatusTemplateByName("status_basicstun"));
                    se.curDuration = 1;
                    se.maxDuration = 1;
                    def.myStats.AddStatus(se, atk);
                    BattleTextManager.NewText(StringManager.GetString("misc_stunned"), def.GetObject(), Color.red, 1.1f);
                }
                else
                {
                    GameLogScript.LogWriteStringRef("log_steelresolve_stun");
                }

            }
        }


        Vector3 position = def.GetPos();

        // Create damage FX image

        if (attackerWeapon.isRanged)
        {
            // Fire projectile as needed.
            GetSwingEffect(atk, def, attackerWeapon, true);
        }
        cmsInstance.StartCoroutine(WaitThenImpactFX(atk, def, attackerWeapon, crit, GameMasterScript.baseAttackAnimationTime * attackIndex));

        crp.result = EvaluateCombatResult(alive, atk, def);
        crp.waitTime += bufferedCombatData.addToWaitTime;
        crp.waitTime += GameMasterScript.baseAttackAnimationTime;
        RemoveFromCombatStack(cdp);
        return crp;
    }

    private GameObject GetAbilityEffect(AbilityScript ability)
    {
        GameObject spriteObj = null;
        /* spriteObj = (GameObject)Instantiate(Resources.Load("SpriteEffects/" + ability.effectName));
        Animatable anim = spriteObj.GetComponent<Animatable>();
        anim.SetAnim("Default"); */
        return spriteObj;
    }

    public static void GetSwingEffect(Fighter attacker, Actor defender, Weapon eq, bool forceShow)
    {
        GameObject attackerObject = attacker.GetObject();
        GameObject defenderObject = defender.GetObject();
        if (attackerObject == null || defenderObject == null)
        {
            return;
        }

        Vector2 attackerPos = attacker.GetPos();
        Vector2 defenderPos = defender.GetPos();

        if (attacker.myMovable == null)
        {
            return;
        }
        if (defender.myMovable == null)
        {
            return;
        }

        if (!GameMasterScript.heroPCActor.visibleTilesArray[(int)attackerPos.x, (int)attackerPos.y] || !attacker.myMovable.inSight)
        {
            if (!eq.isRanged)
            {
                return;
            }
            if ((eq.isRanged && !GameMasterScript.heroPCActor.visibleTilesArray[(int)defenderPos.x, (int)defenderPos.y]) || !defender.myMovable.inSight)
            {
                return;
            }
        }

        if (!eq.isRanged && !bufferedCombatData.criticalHit && !forceShow)
        {
            return;
        }

        Vector3 targetDir = (defenderPos - attackerPos).normalized;
        float targetAngle = Vector3.Angle(attackerObject.transform.up, targetDir);

        GameObject swingObj = null;
        EquipmentBlock eqb = attacker.myEquipment;
        if (eq == null)
        {
            return;
        }

        if (eq.swingEffect == null || eq.swingEffect == "")
        {
            return;
        }
        swingObj = GameMasterScript.TDInstantiate(eq.swingEffect);

        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(eq.swingEffect, swingObj, SpriteReplaceTypes.BATTLEFX);

        Animatable anim = swingObj.GetComponent<Animatable>();
        // Generic swing effect pulls the item's sprites.

        bool thrustEffect = false;

        if (eq.swingEffect == "GenericSwingEffect")
        {
            Sprite origSprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictItemGraphics, eq.spriteRef);
            anim.myAnimations[0].SetSpriteOnly(0, origSprite);
            anim.myAnimations[0].SetSpriteOnly(1, origSprite);
            swingObj.GetComponent<SpriteRenderer>().sprite = origSprite;
        }
        else if (eq.swingEffect == "GenericThrustEffect" || eq.swingEffect == "GenericTurnedThrustEffect")
        {
            Sprite origSprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictItemGraphics, eq.spriteRef);
            anim.myAnimations[0].SetSpriteOnly(0, origSprite);
            anim.myAnimations[0].SetSpriteOnly(1, origSprite);
            swingObj.GetComponent<SpriteRenderer>().sprite = origSprite;
            thrustEffect = true;
        }

        anim.SetAnim("Default");

        if (swingObj != null)
        {
            TryPlayEffectSFX(swingObj, defender.GetPos(), null, attacker == GameMasterScript.heroPCActor);
        }

        if (swingObj != null || eq.isRanged || thrustEffect)
        {
            // Find angle.                        
            float offsetX = 0.0f;
            float offsetY = 0.0f;
            // Below offsets are fixed, make this better?


            if ((eq.isRanged) || (thrustEffect))
            {
                targetAngle = targetAngle * -1;
            }

            if (defenderPos.x > attackerPos.x)
            {
                offsetX = 0.22f;
                offsetY = 0.9f;
                // East
            }
            if (defenderPos.x < attackerPos.x)
            {
                //targetAngle = targetAngle + 180f;
                targetAngle = targetAngle * -1;
                offsetX = -0.22f;
                offsetY = 0.9f;
                // West
            }
            if (defenderPos.y < attackerPos.y)
            {
                if ((!eq.isRanged) && (!thrustEffect))
                {
                    targetAngle += 180f;
                }
                else
                {
                    targetAngle *= 1f;
                }

                offsetY = 1.05f;
                // Attack south
            }
            if (defenderPos.y > attackerPos.y)
            {
                if ((!eq.isRanged) && (!thrustEffect))
                {
                    targetAngle += 180f;
                }
                offsetY = 1.1f;
                // North           
            }

            // Melee only
            if ((!eq.isRanged) && (!thrustEffect))
            {
                if ((defenderPos.x > attackerPos.x) && (defenderPos.y > attackerPos.y))
                {
                    // Attack NORTH EAST
                    targetAngle = targetAngle * -1f;
                    offsetX -= 0.12f;
                    offsetY -= 0.12f;
                }
                if ((defenderPos.x < attackerPos.x) && (defenderPos.y > attackerPos.y))
                {
                    // Attack NORTH WEST
                    targetAngle = targetAngle * -1f;
                    offsetX += 0.12f;
                    offsetY -= 0.12f;
                }
                if ((defenderPos.x > attackerPos.x) && (defenderPos.y < attackerPos.y))
                {
                    // Attack SOUTH EAST
                    targetAngle = targetAngle * -1f;
                    offsetX -= 0.16f;
                    offsetY += 0.16f;
                }
                if ((defenderPos.x < attackerPos.x) && (defenderPos.y < attackerPos.y))
                {
                    // Attack SOUTH WEST
                    targetAngle = targetAngle * -1f;
                    offsetX += 0.16f;
                    offsetY += 0.16f;
                }
            }

            if ((eq.isRanged) || (thrustEffect)) // Make a projectile tag
            {
                Vector3 eulerang = new Vector3(swingObj.transform.eulerAngles.x, swingObj.transform.eulerAngles.y, swingObj.transform.eulerAngles.z + targetAngle);
                swingObj.transform.eulerAngles = eulerang;
                swingObj.transform.localEulerAngles = eulerang;

                // Below vectors were previously based on transforms, not GetPos - grid position

                if (!thrustEffect)
                {
                    FireProjectile(attacker.GetPos(), defender.GetPos(), swingObj, GameMasterScript.baseAttackAnimationTime, true, defender.GetObject(), MovementTypes.SLERP, null);
                }
                else
                {
                    Vector2 diff = defender.GetPos() - attacker.GetPos();
                    Vector2 newPos = attacker.GetPos() + (diff / 2f);
                    FireProjectile(attacker.GetPos(), newPos, swingObj, GameMasterScript.baseAttackAnimationTime / 2f, true, defender.GetObject(), MovementTypes.LERP, null);
                }

            }
            else
            {
                /* swingObj.GetComponent<Animatable>().rotationAngleOffset = targetAngle;
                GameObject pivotHolder = (GameObject)Instantiate(GameMasterScript.GetResourceByRef("PivotHolder"));
                Vector3 pivotHolderPos = new Vector3(attackerPos.x, attackerPos.y, attackerObject.transform.position.z);
                if (!eq.isRanged)
                {
                    swingObj.GetComponent<SpriteEffect>().spriteParent = pivotHolder;
                    swingObj.transform.position = new Vector3(0, 0, attackerObject.transform.position.z);
                    swingObj.transform.SetParent(pivotHolder.transform);
                    pivotHolder.transform.position = pivotHolderPos;
                    swingObj.transform.localPosition = new Vector3(0, 0, attackerObject.transform.position.z);
                }
                Vector2 newPos = new Vector2(0, 0);
                newPos.y += offsetY;
                swingObj.transform.localPosition = new Vector3(newPos.x, newPos.y, attackerObject.transform.position.z);
                pivotHolderPos = new Vector3(pivotHolder.transform.position.x + offsetX, pivotHolder.transform.position.y, pivotHolder.transform.position.z);
                pivotHolder.transform.position = pivotHolderPos; */
            }
            //Debug.Log(swingObj.transform.localPosition.x + "," + swingObj.transform.localPosition.y);
        }

    }

    private static GameObject GetWhiffEffect()
    {
        GameObject attackSpriteObj = GameMasterScript.TDInstantiate("DodgeEffect");
        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites("DodgeEffect", attackSpriteObj, SpriteReplaceTypes.BATTLEFX);
        if (bufferedCombatData == null || bufferedCombatData.defender == null) // 412019 - maybe there's no combat data yet...?
        {
            TryPlayEffectSFX(attackSpriteObj, GameMasterScript.heroPCActor.GetPos(), null);
        }
        else
        {
        TryPlayEffectSFX(attackSpriteObj, bufferedCombatData.defender.GetPos(), null);
        }
        Animatable anim = attackSpriteObj.GetComponent<Animatable>();
        anim.SetAnim("Default");
        return attackSpriteObj;
    }

    private static GameObject GetParryEffect()
    {
        GameObject attackSpriteObj = GameMasterScript.TDInstantiate("ParryEffect");
        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites("ParryEffect", attackSpriteObj, SpriteReplaceTypes.BATTLEFX);
        if (bufferedCombatData != null && bufferedCombatData.defender != null)
        {
        TryPlayEffectSFX(attackSpriteObj, bufferedCombatData.defender.GetPos(), null);
        }        
        Animatable anim = attackSpriteObj.GetComponent<Animatable>();
        anim.SetAnim("Default");
        return attackSpriteObj;
    }

    private static GameObject GetBlockEffect(bool blue, bool autoBarrier, bool playSFX = true)
    {
        string prefab = "BlockEffect";
        if (blue)
        {
            prefab = "BlueBlockEffect";
        }

        if (autoBarrier)
        {
            prefab = "AutoBarrierEffect";
        }

        GameObject attackSpriteObj = GameMasterScript.TDInstantiate(prefab);

        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(prefab, attackSpriteObj, SpriteReplaceTypes.BATTLEFX);

        TryPlayEffectSFX(attackSpriteObj, bufferedCombatData.defender.GetPos(), null);
        Animatable anim = attackSpriteObj.GetComponent<Animatable>();
        anim.SetAnim("Default");
        return attackSpriteObj;
    }

    private static GameObject GetCriticalEffect()
    {
        GameObject attackSpriteObj = GameMasterScript.TDInstantiate("CriticalEffect");
        if (attackSpriteObj == null)
        {
            Debug.Log("Couldn't instantiate critical effect.");
            return null;
        }
        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites("CriticalEffect", attackSpriteObj, SpriteReplaceTypes.BATTLEFX);
        if (bufferedCombatData == null || bufferedCombatData.defender == null)
        {
            Debug.Log("ERROR: Null combat data or defender, can't do critical effect.");
            return null;
        }
        TryPlayEffectSFX(attackSpriteObj, bufferedCombatData.defender.GetPos(), null);
        Animatable anim = attackSpriteObj.GetComponent<Animatable>();
        anim.SetAnim("Default");
        return attackSpriteObj;
    }

    public static GameObject GetImpactEffect(Fighter attacker, Equipment eq, bool silentSFX)
    {
        GameObject attackSpriteObj = null;
        EquipmentBlock eqb = attacker.myEquipment;
        //Equipment eq = eqb.equipment[(int)EquipmentSlots.WEAPON];
        Weapon weap = eq as Weapon;        

        if (weap.impactEffect == "")
        {
            return null;
        }

        if ((weap == null || weap.impactEffect == null) || (weap.impactEffect == "" && !weap.isRanged))
        {
            /* attackSpriteObj = GameMasterScript.TDInstantiate("FervirPunchEffect");
            if ((bufferedCombatData == null) || (bufferedCombatData.defender == null))
            {
                TryPlayEffectSFX(attackSpriteObj, GameMasterScript.heroPCActor.GetPos(), null);
            }
            else
            {
                TryPlayEffectSFX(attackSpriteObj, bufferedCombatData.defender.GetPos(), null);
            } */
        }
        else
        {
            attackSpriteObj = null;
            bool success = true;
            try { attackSpriteObj = GameMasterScript.TDInstantiate(weap.impactEffect); }
            catch (ArgumentException e)
            {
                if (Debug.isDebugBuild) Debug.Log(weap.impactEffect + " not found " + e);
                success = false;
            }
            if (success)
            {
                PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(weap.impactEffect, attackSpriteObj, SpriteReplaceTypes.BATTLEFX);
            }
            if (attackSpriteObj != null && !silentSFX)
            {
                if (bufferedCombatData == null || bufferedCombatData.defender == null)
                {
                    TryPlayEffectSFX(attackSpriteObj, GameMasterScript.heroPCActor.GetPos(), null);
                }
                else
                {
                    TryPlayEffectSFX(attackSpriteObj, bufferedCombatData.defender.GetPos(), null, bufferedCombatData.attacker == GameMasterScript.heroPCActor);
                }
            }
        }

        //Animatable anim = attackSpriteObj.GetComponent<Animatable>();
        //anim.SetAnim("Default");
        return attackSpriteObj;
    }

    public static bool CheckForBlock(Fighter atk, Fighter def, bool penetrate, float curDamage, bool autoBarrier)
    {
        bool isAttackerWeaponRanged = atk.myEquipment.IsWeaponRanged(atk.myEquipment.GetWeapon());
        float blockRoll = UnityEngine.Random.Range(0, 1.0f);
        float blockChance = 0.0f;
        if (isAttackerWeaponRanged)
        {
            blockChance = def.cachedBattleData.blockRangedChance;
        }
        else
        {
            blockChance = def.cachedBattleData.blockMeleeChance;
        }

        float calcBlock = blockChance;
        if (bufferedCombatData != null)
        {
            calcBlock = (blockChance * bufferedCombatData.blockMod) + bufferedCombatData.blockModFlat;
        }


        if (def.myStats.CheckHasActiveStatusName("status_heavyguard"))
        {
            StatusEffect hg = def.myStats.GetStatusByRef("status_heavyguard");
            if (def.myStats.GetCurStat(StatTypes.STAMINA) >= hg.staminaReq)
            {
                calcBlock += 0.2f;
            }
        }

        if (penetrate)
        {
            calcBlock *= 0.5f;
        }

        if ((def.myEquipment.GetOffhandBlock() == 0f) && !def.myStats.CheckHasStatusName("autobarrier"))
        {
            calcBlock = 0.0f;
        }

        // ABSOLUTE LIMIT!?
        if (calcBlock > 0.75f)
        {
            calcBlock = 0.75f;
        }

        if (def.ReadActorData("feared") == 1)
        {
            calcBlock = 0f;
        }

        bool blockAttack = false;
        if (blockRoll < calcBlock)
        {
            if (def.GetActorType() == ActorTypes.HERO && StatBlock.activeSongs.Count > 0)
            {
                GameMasterScript.heroPCActor.TryIncreaseSongDuration(1);
            }

            float blockAmount = def.myEquipment.GetBlockDamageReduction();
            float dmgBeforeBlock = curDamage; // 150
            curDamage *= blockAmount; // blocking 35% (52), so new damage is ~98
            bufferedCombatData.blockedDamage = dmgBeforeBlock - curDamage;
            bufferedCombatData.damage = curDamage;


            if (def.IsHero() && def.myStats.CheckHasActiveStatusName("status_radiantaura") && atk != null
                && atk.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = atk as Monster;
                if (mn.GetXPModToPlayer() > 0f || GameMasterScript.gmsSingleton.gameMode == GameModes.ADVENTURE)
                {
                    def.myStats.CheckRunAndTickAllStatuses(StatusTrigger.ATTACKBLOCK);
                }
                else
                {
                    if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_healing_difficulty") && PlayerOptions.tutorialTips)
                    {
                        Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_healing_difficulty");
                        UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                    }
                }
            }
            else
            {
                def.myStats.CheckRunAndTickAllStatuses(StatusTrigger.ATTACKBLOCK);
            }

            blockAttack = true;

            //SHEP: Here's where you could call specific scripts for block events instead of hardcoding information for
            //specific statuses

            // Foreach status in mystats
            // if script_attackblock != null
            // run that shit

            for (int t = 0; t < def.myStats.GetStatuses().Count; t++)
            {
                StatusEffect se = def.myStats.GetStatuses()[t];
                if (!string.IsNullOrEmpty(se.script_AttackBlock) && def.myStats.GetCurStat(StatTypes.STAMINA) >= se.staminaReq
                    && def.myStats.GetCurStat(StatTypes.ENERGY) >= se.energyReq)
                {
                    MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(GameplayScripts), se.script_AttackBlock);
                    object[] paramList = new object[3];
                    paramList[0] = atk;
                    paramList[1] = def;
                    paramList[2] = penetrate;
                    runscript.Invoke(null, paramList);
                }
            }

            if (GameMasterScript.heroPCActor.visibleTilesArray[(int)def.GetPos().x, (int)def.GetPos().y])
            {
                bool blue = false;
                if (def.GetActorType() == ActorTypes.HERO && def.myStats.CheckHasActiveStatusName("status_heavyguard"))
                {
                    blue = true;
                }
                GameObject whiffSpriteObj = GetBlockEffect(blue, autoBarrier); // SFX
                whiffSpriteObj.transform.position = def.GetObject().transform.position;
            }

        }

        return blockAttack;
    }

    public static float BoostDamageWithClaw(Fighter atk, float baseDamage)
    {
        // Calculate avg damage taken over last 3 turns.

        float highest = -1f;

        for (int i = 0; i < atk.damageTakenLastThreeTurns.Length; i++)
        {
            if (atk.damageTakenLastThreeTurns[i] > highest)
            {
                highest = atk.damageTakenLastThreeTurns[i];
            }
        }

        // Convert to percent
        float percentOfMaxHealthLost = highest / atk.myStats.GetMaxStat(StatTypes.HEALTH);

        float boostPercentage = 2f * percentOfMaxHealthLost;

        float boost = (baseDamage * boostPercentage);

        //Debug.Log("Highest damage taken: " + highest + " As percent of max: " + percentOfMaxHealthLost + " Inc damage by: " + boost + " (" + boostPercentage + ")");
        baseDamage += boost;
        return baseDamage;

    }
    public static float BoostDamageWithDagger(Fighter atk, float baseDamage)
    {
        if (atk.consecutiveAttacksOnLastActor > 1) // 2 for balance
        {
            float mult = 0.1f * (atk.consecutiveAttacksOnLastActor - 1);
            if (mult > 0.5f)
            {
                mult = 0.5f;
            }
            mult += 1.0f;
            baseDamage *= mult;
        }

        return baseDamage;
    }

    public static void DoParryStuff(Fighter atk, Fighter def, bool bothCombatantsVisible, int attackIndex, bool counterAttack)
    {
        if (def.IsHero())
        {
            GameMasterScript.heroPCActor.SetFlag(ActorFlags.PARRYNEXTATTACK, false);
            def.myStats.RemoveStatusByRef("swordparry");
            if (StatBlock.activeSongs.Count > 0)
            {
            GameMasterScript.heroPCActor.TryIncreaseSongDuration(1);
            }            
        }

        if (bothCombatantsVisible)
        {
            cmsInstance.StartCoroutine(WaitThenParry(atk, def, GameMasterScript.baseAttackAnimationTime * attackIndex));
        }

        if (def.IsHero() && def.myStats.CheckHasStatusName("status_effortlessparry"))
        {
            def.myAbilities.TickAllCooldowns();
            uims.RefreshAbilityCooldowns();
        }

        if (def.GetActorType() == ActorTypes.MONSTER)
        {
            Monster mon = def as Monster;
            mon.lastActorAttackedBy = atk;
            mon.AddAggro(atk, 10f); // 10 for taking a swing.
        }

        if (!counterAttack)
        {
            if (MapMasterScript.GetGridDistance(atk.GetPos(), def.GetPos()) <= 1 && !def.myStats.CheckHasStatusName("status_sanctuary"))
            {
                if (((def.myEquipment.GetWeapon().weaponType == WeaponTypes.SWORD)
                    || (def.myStats.CheckHasStatusName("status_alwaysriposte")))
                    && (!atk.myEquipment.IsCurrentWeaponRanged()))
                {
                    if (bothCombatantsVisible)
                    {
                        StringManager.SetTag(0, def.displayName);
                        GameLogScript.LogWriteStringRef("log_attack_counter", def, TextDensity.VERBOSE);
                    }
                    bufferedCombatData.counterAttack = true;
                    CombatResultPayload parryRes = ExecuteAttack(def, atk, false, 0, true);
                    if (parryRes.result == CombatResult.MONSTERDIED || parryRes.result == CombatResult.PLAYERDIED)
                    {
                        GameMasterScript.AddToDeadQueue(atk);
                    }
                    bufferedCombatData.addToWaitTime += parryRes.waitTime;
                    //GameMasterScript.heroPCActor.flags[(int)ActorFlags.PARRYNEXTATTACK] = false;
                    def.myStats.RemoveStatusByRef("swordparry");
                }
            }
        }

        def.myStats.CheckRunAndTickAllStatuses(StatusTrigger.ATTACKPARRY);
    }

    public static bool CheckForParry(Fighter atk, Fighter def, bool penetrate, AttackType aType = AttackType.ATTACK)
    {
        float parryRoll = UnityEngine.Random.Range(0, 1.0f);
        float parryChance = 0.0f;

        if (atk.myEquipment.IsCurrentWeaponRanged() && MapMasterScript.GetGridDistance(atk.GetPos(), def.GetPos()) > 1)
        {
            parryChance = def.cachedBattleData.parryRangedChance;
        }
        else
        {
            parryChance = def.cachedBattleData.parryMeleeChance;
        }

        if (def.myEquipment.IsWeaponRanged(def.myEquipment.GetWeapon()))
        {
            parryChance = 0.0f;
        }

        float calcParry = (parryChance * bufferedCombatData.parryMod) + bufferedCombatData.parryModFlat;

        if (def.IsHero())
        {
            if (aType == AttackType.ABILITY && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_sworddanceremblem_tier2_parry"))
            {
                calcParry *= 0.33f;
            }
            if (aType == AttackType.ATTACK && GameMasterScript.heroPCActor.CheckFlag(ActorFlags.PARRYNEXTATTACK))
            {
                calcParry += 999f;
            }
        }


        if (def.ReadActorData("feared") == 1)
        {
            calcParry = 0f;
        }

        if (penetrate)
        {
            calcParry *= 0.5f;
        }



        /* if (aType == AttackType.ABILITY)
        {
            calcParry *= 3f;
        } */

        if (parryRoll < calcParry)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Hero can have status effects that convert one damage type to another.
    /// </summary>
    /// <param name="dc"></param>
    /// <returns></returns>
    public static DamageCarrier GetFighterDamageConversion(Fighter attacker, Fighter defender, DamageCarrier dc)
    {
        dc.damType = attacker.cachedBattleData.GetConvertedDamageDealType(dc.damType);
        return dc;
    }

    public static DamageTypes GetFighterDamageConversion(Fighter attacker, DamageTypes baseType)
    {
        return attacker.cachedBattleData.GetConvertedDamageDealType(baseType);
    }

    static void AxeOrWhipAttackLogic(Fighter atk, Fighter def, WeaponTypes wType)
    {
        Vector2 targetPoint = atk.GetPos();
        if (wType == WeaponTypes.WHIP)
        {
            targetPoint = def.GetPos();
        }
        List<Actor> nearby = MapMasterScript.GetAllTargetableInTiles(MapMasterScript.activeMap.GetListOfTilesAroundPoint(targetPoint, 1));
        pool_targetList.Clear();
        nearby.Remove(atk);
        nearby.Remove(def);

        // first make the list of things to hit
        for (int x = 0; x < nearby.Count; x++)
        {
            if (nearby[x].IsFighter() && nearby[x].actorfaction != atk.actorfaction && nearby[x].actorUniqueID != atk.actorUniqueID)
            {
                pool_targetList.Add(nearby[x] as Fighter);
            }
        }

        GameMasterScript.gmsSingleton.SetTempGameData("enemiesattacked", pool_targetList.Count);

        foreach (Fighter ft in pool_targetList)
        {
            CombatResultPayload crpLocal = ExecuteAttack(atk, ft, false, 0, false);
            if (ft.myStats.IsAlive() && ft.objectSet && ft.myMovable != null)
            {
                ft.myMovable.Jitter(0.1f);
            }

            if (crpLocal.result == CombatResult.MONSTERDIED)
            {
                if (wType == WeaponTypes.AXE)
                {
                    staticBonusAttackPackage.anyMonstersDiedToAxe = true;
                }
                GameMasterScript.AddToDeadQueue(ft);
                if (atk.IsHero() && wType == WeaponTypes.AXE)
                {
                    CheckGaelmyddAxeProc(ft);
                }
            }
            staticBonusAttackPackage.AddToWaitTime += crpLocal.waitTime;
        }
    }

    static void SpearMastery2AttackLogic(Fighter atk, Fighter def)
    {
        if (!atk.myStats.CheckHasStatusName("spearmastery2")) return;
        int distance = MapMasterScript.GetGridDistance(atk.GetPos(), def.GetPos());
        pool_targetList.Clear();
        if (distance == 1)
        {
            // get target BEHIND the one we attacked
            Directions dirToCheck = MapMasterScript.GetDirectionFromAngle(CombatManagerScript.GetAngleBetweenPoints(atk.GetPos(), def.GetPos()));
            Vector2 posToCheck = def.GetPos() + MapMasterScript.xDirections[(int)dirToCheck];
            if (MapMasterScript.InBounds(posToCheck))
            {
                foreach (Actor act in MapMasterScript.GetTile(posToCheck).GetAllActors())
                {
                    if (act.actorfaction == atk.actorfaction) continue;
                    if (!act.IsFighter()) continue;
                    // We can hit this!!!
                    pool_targetList.Add(act as Fighter);
                }
            }
        }
        else
        {
            CustomAlgorithms.GetPointsOnLineNoGarbage(atk.GetPos(), def.GetPos());
            for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
            {
                if (CustomAlgorithms.pointsOnLine[i] == atk.GetPos() || CustomAlgorithms.pointsOnLine[i] == def.GetPos()) continue;
                foreach(Actor act in MapMasterScript.GetTile(CustomAlgorithms.pointsOnLine[i]).GetAllActors())
                {
                    if (act.actorfaction == atk.actorfaction) continue;
                    if (!act.IsFighter()) continue;
                    // We can hit this!!!
                    pool_targetList.Add(act as Fighter);
                }
            }
        }

        foreach(Fighter ft in pool_targetList)
        {
            ExecuteAttackWrapperWithAnimationAndDeadQueue(atk, ft, false, 0, false);            
        }
    }

    static void AxeMastery3Logic(Fighter atk, Fighter def)
    {
        BattleTextManager.NewText(StringManager.GetString("log_doubleattack"), atk.GetObject(), Color.red, 0.8f);
        List<Actor> nearby = MapMasterScript.GetAllTargetableInTiles(MapMasterScript.activeMap.GetListOfTilesAroundPoint(atk.GetPos(), 1));
        nearby.Remove(atk);
        for (int x = 0; x < nearby.Count; x++)
        {
            if (nearby[x].GetActorType() == ActorTypes.MONSTER && nearby[x].actorfaction != Faction.PLAYER)
            {
                Monster mn = nearby[x] as Monster;
                //Debug.Log(mn.actorRefName);
                if (!mn.myStats.IsAlive()) continue;
                CombatResultPayload crpLocal = ExecuteAttack(atk, mn, false, 0, false);
                if (mn.myStats.IsAlive() && mn.objectSet && mn.myMovable != null)
                {
                    mn.myMovable.Jitter(0.1f);
                }

                if (crpLocal.result == CombatResult.MONSTERDIED)
                {
                    GameMasterScript.AddToDeadQueue(mn);
                    if (atk.IsHero())
                    {
                        CheckGaelmyddAxeProc(mn);
                    }
                }
                staticBonusAttackPackage.AddToWaitTime += crpLocal.waitTime;
            }
        }
    }

    static void ExecuteAttackWrapperWithAnimationAndDeadQueue(Fighter atk, Fighter mn, bool offhand, int attackIndex, bool counterAttack)
    {
        CombatResultPayload crpLocal = ExecuteAttack(atk, mn, offhand, attackIndex, counterAttack);
        if (mn.myStats.IsAlive() && mn.objectSet && mn.myMovable != null)
        {
            mn.myMovable.Jitter(0.1f);
        }

        if (crpLocal.result == CombatResult.MONSTERDIED)
        {
            GameMasterScript.AddToDeadQueue(mn);
        }
        staticBonusAttackPackage.AddToWaitTime += crpLocal.waitTime;
    }

    static void CheckForBowRicochet(Fighter atk, Fighter def, bool crit, float baseDmg)
    {
        //if (atk.IsHero())
        if (atk.IsHero() && crit && atk.myEquipment.GetWeaponType() == WeaponTypes.BOW && atk.myStats.CheckHasStatusName("xp2_ricochet"))
        {
            CustomAlgorithms.GetTilesAroundPoint(def.GetPos(), atk.myEquipment.GetWeapon().range, MapMasterScript.activeMap, false);
            Fighter targetToAttack = null;
            bool foundTarget = false;
            for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
            {
                if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL) continue;
                foreach(Actor act in CustomAlgorithms.tileBuffer[i].GetAllActors())
                {
                    if (act.GetActorType() != ActorTypes.MONSTER) continue;
                    Monster m = act as Monster;
                    if (m.actorfaction == Faction.PLAYER) continue;
                    if (m.actorUniqueID == def.actorUniqueID) continue;
                    // just hit this.
                    targetToAttack = m;
                    foundTarget = true;
                    break;
                }
                if (foundTarget) break;
            }

            if (foundTarget)
            {
                GameObject spriteObj = GetEffect(atk.myEquipment.GetWeapon().swingEffect);
                FireProjectile(def.GetPos(), targetToAttack.GetPos(), spriteObj, 0.15f, false, targetToAttack.GetObject(), MovementTypes.LERP, null);
                float dmg = baseDmg * 0.5f;
                targetToAttack.TakeDamage(dmg, atk.myEquipment.GetWeapon().damType);
                BattleTextManager.NewDamageText((int)dmg, false, Color.red, targetToAttack.GetObject(), 0f, 1f);
                StringManager.SetTag(0, def.displayName);
                StringManager.SetTag(1, targetToAttack.displayName);
                StringManager.SetTag(2,((int)dmg).ToString());
                GameLogScript.LogWriteStringRef("log_ricochet_damage");
            }
        }
    }
}

public class BonusAttackPackage
{
    public bool anyMonstersDiedToAxe;

    float addToWaitTime;
    public float AddToWaitTime
    {
        get
        {
            return addToWaitTime;
        }
        set
        {
            addToWaitTime = value;
        }
    }

    public float GetWaitTimeThenSetToZero()
    {
        float fValue = addToWaitTime;
        addToWaitTime = 0f;
        return fValue;
        
    }

    public void Clear()
    {
        anyMonstersDiedToAxe = false;
        addToWaitTime = 0f;
    }
}
