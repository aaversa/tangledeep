using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Designed to parse random table data from perchance.org as is, without need for futzing.
/// Unfortunately, quite a few of Perchance's coolest features are not used. This class right now
/// is good for tables and table recursion.
///
/// This class uses Unity as well as a fancy debug console you should use and can get here:
/// 
/// https://gist.github.com/darktable/1412228
///
/// But if you don't wanna, just replace DebugConsole.Log calls with Debug.Log
///
/// Jim Shepard
/// come say hello:
/// 	twitch.tv/playdungeonmans
/// 	discord.gg/stremf
/// </summary>

public class PerchanceParser
{
	//todo: Perchance - Instead of <string, List<string>>, make a List of structs that contain the draw chance as well
	//todo: Perchance - Support multiple files and multiple dictionaries.
	
	private static Dictionary<string, List<string>> allPerchanceTables;
	

	public static void ParsePerchanceFile(TextAsset file)
	{
		//reset this every time
		allPerchanceTables = new Dictionary<string, List<string>>();

		//open the .text asset
		//pull it all in
		var rawText = file.ToString();

		//split by /n, removing all empty entries.
		var splitsies = rawText.Split(new [] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);

		string currentTableName = null;
		var currentTable = new List<string>();

		foreach (var s in splitsies)
		{
			//if the entry contains "//", toss it
			if (s.Contains("//"))
			{
				continue;
			}
			
			//if it contains no tabs, it is a table name
			if (!s.Contains("\t"))
			{
				//close the previous table
				if (currentTableName != null)
				{
					allPerchanceTables[currentTableName] = currentTable;
				}

				currentTableName = s;
				currentTable = new List<string>();
			}
			//it is an entry, or just an errant tab.
			else
			{
				var tabless = s.Replace("\t", "");
				if (string.IsNullOrEmpty(tabless))
				{
					continue;
				}
				
				currentTable.Add(tabless);
			}
			
		}
		
		//at the end, we need to make sure that last table is added as well.
		if (currentTableName != null)
		{
			allPerchanceTables[currentTableName] = currentTable;
		}
		
	}

	public static object Debug_GenerateLegendaryFlavor(params string[] arg)
	{
		return GetResultFromTable("output");
	}

	/// <summary>
	/// This is the place where you would want to change DebugConsole to just Debug if you are not
	/// using the super excellent console I linked at the top.
	/// </summary>
	public static void Debug_DrawPerchanceTableToLog()
	{
		DebugConsole.Clear();
		foreach (var kvp in allPerchanceTables)
		{
			DebugConsole.LogWarning("Table name: " + kvp.Key);
			foreach (var s in kvp.Value)
			{
				DebugConsole.Log(s);
			}
		}
		
		//funsies
		DebugConsole.Log(GetResultFromTable("output"));
		DebugConsole.Log(GetResultFromTable("output"));
		DebugConsole.Log(GetResultFromTable("output"));
		DebugConsole.Log(GetResultFromTable("output"));
	}

	/// <summary>
	/// Grabs an entry at random from a given table, parses all the [variables] inside, returns justice.
	/// This function will recurse, and throw up if we get caught in too deep of a loop
	/// </summary>
	/// <param name="tableName"></param>
	/// <param name="recurseDepthChecker">Use this to catch out-of-control recursion if you have a table or chain
	/// of tables that goes all the way up its own butt, forever. 100 is just a value, you can change it.</param>
	/// <returns></returns>
	public static string GetResultFromTable(string tableName, int recurseDepthChecker = 0)
	{
		recurseDepthChecker++;
		if (recurseDepthChecker > 100)
		{
			Debug.LogError("Perchance: your tables are eating their own tails or this is a" +
			               " FORMULA TOO COMPLEX error");
			return "FART";
		}

		List<string> table = null;
		allPerchanceTables.TryGetValue(tableName, out table);

		if (table == null)
		{
			Debug.LogError("Perchance: you're looking a table called [" + tableName + "] but it does not exist.");
			return "*BAD TABLE: " + tableName + "*";
		}

		var thisEntry = table[UnityEngine.Random.Range(0, table.Count)];
		
		//for every [variable] in the entry, we need to dive into a new table!
		if (!thisEntry.Contains("["))
		{
			return thisEntry;
		}

		/*
		 *	Regex, abandon all hope etc etc.
		 *
		 *	The string below looks for any bracketed value.
		 *  \[(w+)\]
		 *  
		 *  "Find anything that starts with [ and ends with ], and also make a special group out of any characters
		 *  between them"
		 *
		 *  For this string here...
		 *  "I dreamed of [dream_noun] and found [dream_thing]"
		 *
		 *	The string generates two groups for each match, which are in the .Groups array.
		 *
		 * 	0 is the entire match, which in the first case would be "[dream_noun]"
		 *  1 is the match inside the parens in the regex, which is just "dream_noun"
		 *
		 *  We don't use group0 directly, but group 1 is the table we wanna roll on recursively
		 *
		 *  The special .Replace for Regex allows us to replace a given number of instances. Which means we
		 *  iterate down the line.
		 *
		 *  "I dreamed of [dream_noun] and found [dream_thing]"
		 *  "I dreamed of a barrel of spiders and found [dream_thing]"
		 *  "I dreamed of a barrel of spiders and found two nickels made of dimes"
		 *
		 *  Thanks in part to nice people on strim such as utoxin and thebirk, 14 Jan 2019
		 */
		
		
		var regString = "\\[(\\w+)\\]";
		var regCheck = new Regex(regString);
		var mc = regCheck.Matches(thisEntry);

		foreach (Match matchedValue in mc)
		{
			var resultFromNewTable = GetResultFromTable(matchedValue.Groups[1].ToString(), recurseDepthChecker);
			thisEntry =	regCheck.Replace(thisEntry, resultFromNewTable, 1);
			
		}
		
		return thisEntry;

	}
}
