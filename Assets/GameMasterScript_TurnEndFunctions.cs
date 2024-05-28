using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

public partial class GameMasterScript
{
    public Dictionary<Action<string[]>, string[]> dictEndOfTurnFunctions;

    public static void AddEndOfTurnFunction(Action<string[]> newFunction, string[] args)
    {
        if (!gmsSingleton.dictEndOfTurnFunctions.ContainsKey(newFunction))
        {
            gmsSingleton.dictEndOfTurnFunctions.Add(newFunction, args);
        }
        else
        {
            gmsSingleton.dictEndOfTurnFunctions[newFunction] = args;
        }
    }

    public void ProcessAllEndOfTurnFunctions()
    {
        // Check if active map adds its own function!
        if (!string.IsNullOrEmpty(MapMasterScript.activeMap.dungeonLevelData.script_onTurnEnd))
        {
            Action<string[]> myFunc;
            if (TDGenericFunctions.dictDelegates.TryGetValue(MapMasterScript.activeMap.dungeonLevelData.script_onTurnEnd, out myFunc))
            {
                myFunc(new string[0]);
            }
            else
            {
                MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(TDGenericFunctions), MapMasterScript.activeMap.dungeonLevelData.script_onTurnEnd);
                object[] paramList = new object[1];
                paramList[0] = new string[0];
                runscript.Invoke(null, paramList);
            }
        }

        // Runs all enqueued functions *in the order they were added*
        // These functions should live in TDGenericFunctions
        foreach (Action<string[]> queuedAction in dictEndOfTurnFunctions.Keys)
        {
            string[] argsForAction = dictEndOfTurnFunctions[queuedAction];
            queuedAction.Invoke(argsForAction);
        }

        // Now that all functions have been processed, clear the dictionary.
        dictEndOfTurnFunctions.Clear();
    }
}