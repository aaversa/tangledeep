using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class XMLToBinaryBakers : MonoBehaviour {

	public static void WriteAbilitiesToBinary()
    {
        string path = "Assets/Resources/BakedData/bakedAbilities.dat";
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        Stream saveStream = File.Create(path);
        BinaryWriter writer = new BinaryWriter(saveStream);

        writer.Write((short)GameMasterScript.masterAbilityList.Values.Count);

        // Now write each
        foreach(AbilityScript abil in GameMasterScript.masterAbilityList.Values)
        {
            int numChars = abil.refName.Length;
            writer.Write((short)numChars);
            if (numChars > 0)
            {
                writer.Write(abil.refName);
            }

            numChars = abil.abilityName.Length;
            writer.Write((short)numChars);
            if (numChars > 0)
            {
                writer.Write(abil.abilityName);
            }

            numChars = abil.description.Length;
            writer.Write((short)numChars);
            if (numChars > 0)
            {
                writer.Write(abil.description);
            }

            numChars = abil.extraDescription.Length;
            writer.Write((short)numChars);
            if (numChars > 0)
            {
                writer.Write(abil.extraDescription);
            }

            numChars = abil.chargeText.Length;
            writer.Write((short)numChars);
            if (numChars > 0)
            {
                writer.Write(abil.chargeText);
            }

            numChars = abil.combatLogText.Length;
            writer.Write((short)numChars);
            if (numChars > 0)
            {
                writer.Write(abil.combatLogText);
            }

            numChars = abil.shortDescription.Length;
            writer.Write((short)numChars);
            if (numChars > 0)
            {
                writer.Write(abil.shortDescription);
            }

            numChars = abil.iconSprite.Length;
            writer.Write((short)numChars);
            if (numChars > 0)
            {
                writer.Write(abil.iconSprite);
            }

            numChars = abil.sfxOverride.Length;
            writer.Write((short)numChars);
            if (numChars > 0)
            {
                writer.Write(abil.sfxOverride);
            }

            int numTags = 0;

            for (int x = 0; x < (int)AbilityTags.COUNT; x++)
            {
                if (abil.CheckAbilityTag((AbilityTags)x))
                {
                    numTags++;
                }
            }

            writer.Write((short)numTags);

            for (int x = 0; x < (int)AbilityTags.COUNT; x++)
            {
                if (abil.CheckAbilityTag((AbilityTags)x))
                {
                    writer.Write((short)x);
                }
            }

            writer.Write((short)abil.energyCost);
            writer.Write((short)abil.healthCost);
            writer.Write((float)abil.percentCurHealthCost);
            writer.Write((float)abil.percentMaxHealthCost);
            writer.Write((short)abil.maxCooldownTurns);
            writer.Write((short)abil.chargeTurns);
            writer.Write((short)abil.passTurns);

            writer.Write((short)abil.targetOffsetX);
            writer.Write((short)abil.targetOffsetY);
            writer.Write((short)abil.reqWeaponType);
            writer.Write((short)abil.reqWeaponType);
            writer.Write((short)abil.direction);
            writer.Write((short)abil.lineDir);
            writer.Write((short)abil.landingTile);

            writer.Write((short)abil.range);
            writer.Write((short)abil.targetRange);
            writer.Write((short)abil.boundsShape);
            writer.Write((short)abil.targetShape);
            writer.Write((short)abil.repetitions);
            writer.Write((short)abil.numMultiTargets);
            writer.Write((short)abil.targetChangeCondition);
            writer.Write((float)abil.randomChance);
            writer.Write((short)abil.targetForMonster);
            writer.Write((short)abil.chargeTime);

            writer.Write(abil.budokaMod);
            writer.Write(abil.spellshift);
            writer.Write(abil.passiveAbility);
            if (abil.passiveAbility)
            {
                writer.Write(abil.usePassiveSlot);                
            }
            writer.Write(abil.displayInList);

            writer.Write((short)abil.listEffectScripts.Count);

            for (int x = 0; x < abil.listEffectScripts.Count; x++)
            {
                // I really don't want to write this........
            }
        }

        writer.Close();

        saveStream.Close();


    }
}
