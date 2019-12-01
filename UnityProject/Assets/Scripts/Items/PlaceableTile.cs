using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Allows an item to be placed in order to create a tile on the ground.
/// </summary>
public class PlaceableTile : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{

	[Tooltip("Defines each possible way this item can be placed as a tile.")]
	[SerializeField]
	private List<PlaceableTileEntry> entries;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.HandObject != gameObject) return false;

		//check if we are clicking a spot we can place a tile on
		var interactableTiles = InteractableTiles.GetAt(interaction.WorldPositionTarget, side);
		var tileAtPosition = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);

		foreach (var entry in entries)
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
		//which way are we placing it
		foreach (var entry in entries)
		{
			//open space
			if (tileAtPosition == null && entry.placeableOn == LayerType.None && entry.placeableOnlyOnTile == null)
			{
				interactableTiles.TileChangeManager.UpdateTile(cellPos, entry.layerTile);
				break;
			}
			// placing on an existing tile
			else if (tileAtPosition.LayerType == entry.placeableOn &&
			         (entry.placeableOnlyOnTile == null || entry.placeableOnlyOnTile == tileAtPosition))
			{
				interactableTiles.TileChangeManager.UpdateTile(cellPos, entry.layerTile);
				break;
			}
		}

		interactableTiles.TileChangeManager.SubsystemManager.UpdateAt(cellPos);
		Inventory.ServerConsume(interaction.HandSlot, 1);
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
	}
}
