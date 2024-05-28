using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonLimitBreaks
{
    const int FIXED_ABILITY_RANGE = 2;

    /// <summary>
    /// Adds spaces based on ability into targeting for effect eff
    /// </summary>
    /// <param name="eff"></param>
    /// <param name="owner"></param>
    public static void AddTargetsForDragonBreak(AbilityScript triggeringAbility, EffectScript eff, Actor owner)
    {
        StringManager.SetTag(0, triggeringAbility.abilityName);
        GameLogScript.LogWriteStringRef("log_player_dragonbreak");

        List<Vector2> targetSquares = UIManagerScript.singletonUIMS.CreateShapeTileList(triggeringAbility.boundsShape, triggeringAbility, owner.GetPos(), Directions.NORTH, FIXED_ABILITY_RANGE, false);
        eff.positions.Clear();
        eff.targetActors.Clear();
        foreach (Vector2 pos in targetSquares)
        {
            if (!MapMasterScript.activeMap.InBounds(pos)) continue;
            eff.positions.Add(pos);
            foreach(Actor act in MapMasterScript.GetTile(pos).GetAllActors())
            {
                if (eff.targetActors.Contains(act)) continue;
                eff.targetActors.Add(act);
            }
        }

        if (triggeringAbility.CheckAbilityTag(AbilityTags.OVERRIDECHILDSFX))
        {
            GameObject go = CombatManagerScript.GetEffect(triggeringAbility.sfxOverride);
            CombatManagerScript.TryPlayAbilitySFX(go, owner.GetPos(), triggeringAbility);
        }
    }    

}
