using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickupScript 
{

	public static bool TemplateItemPickupFunction(InventoryScript inventory, Item itemPickedUp, bool combinedIntoExistingItem)
    {
        // This function runs after the entire item pickup function has executed
        // So you can mess with the inventory List<> collection, change quantities, etc

        // Return TRUE if a unique Item was inserted into our collection (by default this is !combinedIntoExistingItem)        
        // Return FALSE if we simply increased quantity of existing items or for whatever reason did NOT add the Item to our collection

        return !combinedIntoExistingItem;
    }


	/// <summary>
	/// Checks the inventory to see if we now have enough parts to make a slime egg of a given color.
	/// </summary>
	/// <param name="inventory"></param>
	/// <param name="itemPickedUp"></param>
	/// <param name="combinedIntoExistingItem"></param>
	/// <returns></returns>
	public static bool CheckSlimePartCombination(InventoryScript inventory, Item itemPickedUp,
		bool combinedIntoExistingItem)
	{
		int redegg = 0;
		int blueegg = 0;
		int yellowegg = 0;

		//march through the entire inventory, looking for slime parts
		foreach (var itam in inventory.GetInventory())
		{
			if( !(itam is Consumable)) continue;

			switch (itam.actorRefName)
			{
				case "item_part_metalslime_red_01":
					redegg |= 1;
					break;
				case "item_part_metalslime_red_02":
					redegg |= 2;
					break;
				case "item_part_metalslime_red_03":
					redegg |= 4;
					break;
				case "item_part_metalslime_blue_01":
					blueegg |= 1;
					break;
				case "item_part_metalslime_blue_02":
					blueegg |= 2;
					break;
				case "item_part_metalslime_blue_03":
					blueegg |= 4;
					break;
				case "item_part_metalslime_yellow_01":
					yellowegg |= 1;
					break;
				case "item_part_metalslime_yellow_02":
					yellowegg |= 2;
					break;
				case "item_part_metalslime_yellow_03":
					yellowegg |= 4;
					break;
				
			}
		}
		
		//if the egg values are 7 (1, 2 & 4) then we have at least one of each

		Action<InventoryScript, string> stripItemMaekEgg = (inv, colorstring) =>
		{
			//remove the parts
			inv.RemoveItemOrDecrementQuantityByRef("item_part_metalslime_" + colorstring + "_01");
			inv.RemoveItemOrDecrementQuantityByRef("item_part_metalslime_" + colorstring + "_02");
			inv.RemoveItemOrDecrementQuantityByRef("item_part_metalslime_" + colorstring + "_03");

			//craft a new egg
			var newegg = LootGeneratorScript.CreateItemFromTemplateRef("egg_metalslime_" + colorstring, 1.0f, 1.0f,
				false);

			//give egg
			inv.AddItem(newegg, true);
			
			//show the player something happened
			if (GameMasterScript.actualGameStarted)
			{
				//hooray!
				GameMasterScript.heroPCActor.myAnimatable.SetAnimWithDirectionalBackup("UseItem", "Attack",
					Directions.SOUTH, Directions.SOUTH);
			
				//get?
				TDVisualEffects.PopupSprite(newegg.spriteRef, GameMasterScript.heroPC.transform, true,
					newegg.GetSpriteForUI());

                //🎶 get 🎶
                BattleTextManager.NewText(StringManager.GetString("slime_egg_combined_popup"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1f);
                StringManager.SetTag(0, newegg.displayName);
				GameLogScript.GameLogWrite(StringManager.GetString("slime_egg_combined_log"), GameMasterScript.heroPCActor);
				UIManagerScript.PlayCursorSound("CookingSuccess");
			}
			
		};

		if (redegg == 7)
		{
			stripItemMaekEgg(inventory, "red");
		}
		if (blueegg == 7)
		{
			stripItemMaekEgg(inventory, "blue");
		}
		if (yellowegg == 7)
		{
			stripItemMaekEgg(inventory, "yellow");
		}

		return !combinedIntoExistingItem;
	}
}
