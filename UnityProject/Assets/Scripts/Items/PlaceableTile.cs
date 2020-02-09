using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/// <summary>
/// Allows an item to be placed in order to create a tile on the ground.
/// </summary>
public class PlaceableTile : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{

	[FormerlySerializedAs("entries")]
	[Tooltip("Defines each possible way this item can be placed as a tile.")]
	[SerializeField]
	private List<PlaceableTileEntry> waysToPlace;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.HandObject != gameObject) return false;

		//it's annoying to place something when you're trying to pick up instead to add to your current stack,
		//so if it's possible to stack what is being targeted with what's in your hand, we will defer to that
		if (Validations.CanStack(interaction.HandObject, interaction.TargetObject)) return false;

		//check if we are clicking a spot we can place a tile on
		var interactableTiles = InteractableTiles.GetAt(interaction.WorldPositionTarget, side);
		var tileAtPosition = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);

		foreach (var entry in waysToPlace)
		{
			//open space
			if (tileAtPosition == null && entry.placeableOn == LayerType.None && entry.placeableOnlyOnTile == null)
			{
				return true;
			}
			// placing on an existing tile
			else if (tileAtPosition.LayerType == entry.placeableOn &&
					 (entry.placeableOnlyOnTile == null || entry.placeableOnlyOnTile == tileAtPosition))
			{
				return true;
			}
		}

		return false;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		//which matrix are we clicking on
		var interactableTiles = InteractableTiles.GetAt(interaction.WorldPositionTarget, true);
		Vector3Int cellPos = interactableTiles.WorldToCell(interaction.WorldPositionTarget);
		var tileAtPosition = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);

		PlaceableTileEntry placeableTileEntry = null;

		int itemAmount = 1;
		var stackable = interaction.HandObject.GetComponent<Stackable>();
		if (stackable != null)
		{
			itemAmount = stackable.Amount;
		}

		// find the first valid way possible to place a tile
		foreach (var entry in waysToPlace)
		{
			//skip what can't be afforded
			if (entry.itemCost > itemAmount)
				continue;

			//open space
			if (tileAtPosition == null && entry.placeableOn == LayerType.None && entry.placeableOnlyOnTile == null)
			{
				placeableTileEntry = entry;
				break;
			}

			// placing on an existing tile
			else if (tileAtPosition.LayerType == entry.placeableOn && (entry.placeableOnlyOnTile == null || entry.placeableOnlyOnTile == tileAtPosition))
			{
				placeableTileEntry = entry;
				break;
			}
		}

		if (placeableTileEntry != null)
		{
			interactableTiles.TileChangeManager.UpdateTile(cellPos, placeableTileEntry.layerTile);
			interactableTiles.TileChangeManager.SubsystemManager.UpdateAt(cellPos);
			Inventory.ServerConsume(interaction.HandSlot, placeableTileEntry.itemCost);
		}
	}

	/// <summary>
	/// Some tile items can be placed on multiple things, so this defines one way in which an item can be placed
	/// </summary>
	[Serializable]
	private class PlaceableTileEntry
	{
		[Tooltip("Layer tile which this will create when placed.")]
		public LayerTile layerTile;

		[Tooltip("What layer this can be placed on top of. Choose None to allow placing on empty space.")]
		[SerializeField]
		public LayerType placeableOn;

		[Tooltip("Particular tile this is placeable on. Leave empty to allow placing on any tile.")]
		[SerializeField]
		public LayerTile placeableOnlyOnTile;

		[Tooltip("The amount of this item required to place tile.")]
		[SerializeField]
		public int itemCost = 1;
	}
}
