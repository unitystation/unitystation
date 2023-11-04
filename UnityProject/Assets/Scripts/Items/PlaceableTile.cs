using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using AddressableReferences;

namespace Tiles
{
	/// <summary>
	/// Allows an item to be placed in order to create a tile on the ground.
	/// </summary>
	public class PlaceableTile : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
	{
		[NonSerialized]
		public LayerTypeSelection layerTypeSelection = LayerTypeSelection.AllUnderFloor | LayerTypeSelection.Effects;

		[FormerlySerializedAs("entries")]
		[Tooltip("Defines each possible way this item can be placed as a tile.")]
		[SerializeField]
		private List<PlaceableTileEntry> waysToPlace = null;

		[SerializeField] private AddressableAudioSource placeSound = null;

		[SerializeField]
		private static readonly StandardProgressActionConfig ProgressConfig
		= new StandardProgressActionConfig(StandardProgressActionType.Construction, true);

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject != gameObject) return false;

			//it's annoying to place something when you're trying to pick up instead to add to your current stack,
			//so if it's possible to stack what is being targeted with what's in your hand, we will defer to that
			if (Validations.CanStack(interaction.HandObject, interaction.TargetObject)) return false;

			//check if we are clicking a spot we can place a tile on
			var interactableTiles = InteractableTiles.GetAt(interaction.WorldPositionTarget, side);
			var tileAtPosition = interactableTiles.LayerTileAt(interaction.WorldPositionTarget, layerTypeSelection);

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
		public void ClientPredictInteraction(PositionalHandApply interaction)
		{
			//start clientside melee cooldown so we don't try to spam melee
			//requests to server
			Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Melee);
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			//which matrix are we clicking on

			var matrix = InteractableTiles.TryGetNonSpaceMatrix(interaction.WorldPositionTarget.RoundToInt(), true);

			if (matrix.IsSpaceMatrix)
			{
				matrix = interaction.Performer.RegisterTile().Matrix;
			}


			var interactableTiles = matrix.TileChangeManager.InteractableTiles;

			Vector3Int cellPos = interactableTiles.WorldToCell(interaction.WorldPositionTarget);
			var tileAtPosition = interactableTiles.LayerTileAt(interaction.WorldPositionTarget, layerTypeSelection);

			PlaceableTileEntry placeableTileEntry = null;

			int itemAmount = 1;
			var stackable = interaction.HandObject.GetComponent<Stackable>();
			if (stackable != null)
			{
				itemAmount = stackable.Amount;
			}

			placeableTileEntry = FindValidTile(itemAmount, tileAtPosition);

			if (placeableTileEntry != null)
			{
				GameObject performer = interaction.Performer;
				Vector2 targetPosition = interaction.WorldPositionTarget;


				void ProgressFinishAction()
				{
					ProgressFinishActionPriv(interaction, placeableTileEntry, interactableTiles, cellPos, targetPosition);
				}

				void ProgressInterruptedAction(ActionInterruptionType reason)
				{
					ProgressInterruptedActionPriv(interaction, reason, placeableTileEntry);
				}

				var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction, ProgressInterruptedAction)
					.ServerStartProgress(targetPosition, placeableTileEntry.timeToPlace, performer);
			}
		}

		private static void ProgressInterruptedActionPriv(PositionalHandApply interaction, ActionInterruptionType reason,
			PlaceableTileEntry placeableTileEntry)
		{
			switch (reason)
			{
				case ActionInterruptionType.ChangeToPerformerActiveSlot:
					Chat.AddExamineMsgFromServer(interaction.PerformerPlayerScript.PlayerInfo,
						$"You lack the materials to finish placing {placeableTileEntry.layerTile.DisplayName}");
					break;
				case ActionInterruptionType.PerformerUnconscious:
					Chat.AddExamineMsgFromServer(interaction.PerformerPlayerScript.PlayerInfo,
						$"You dream of placing {placeableTileEntry.layerTile.DisplayName}");
					break;
				case ActionInterruptionType.WelderOff:
					Chat.AddExamineMsgFromServer(interaction.PerformerPlayerScript.PlayerInfo,
						$"You need the welder to be on to finish this");
					break;
				case ActionInterruptionType.PerformerOrTargetMoved:
					Chat.AddExamineMsgFromServer(interaction.PerformerPlayerScript.PlayerInfo,
						$"Cannot place {placeableTileEntry.layerTile.DisplayName}, you or your target moved too much");
					break;
				case ActionInterruptionType.TargetDespawn:
					Chat.AddExamineMsgFromServer(interaction.PerformerPlayerScript.PlayerInfo,
						$"Placing {placeableTileEntry.layerTile.DisplayName} has become a tall order, seeing as the conceptual physical placement space has ceased to exist");
					break;
				case ActionInterruptionType.PerformerDirection:
					Chat.AddExamineMsgFromServer(interaction.PerformerPlayerScript.PlayerInfo,
						$"You have looked away from placing {placeableTileEntry.layerTile.DisplayName}");
					break;
			}
		}

		private void ProgressFinishActionPriv(PositionalHandApply interaction, PlaceableTileEntry placeableTileEntry,
			InteractableTiles interactableTiles, Vector3Int cellPos, Vector2 targetPosition)
		{
			if (Inventory.ServerConsume(interaction.HandSlot, placeableTileEntry.itemCost))
			{
				interactableTiles.TileChangeManager.MetaTileMap.SetTile(cellPos, placeableTileEntry.layerTile);
				interactableTiles.TileChangeManager.SubsystemManager.UpdateAt(cellPos);
				SoundManager.PlayNetworkedAtPos(placeSound, targetPosition);
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.PerformerPlayerScript.PlayerInfo,
					$"You lack the materials to finish placing {placeableTileEntry.layerTile.DisplayName}");
			}
		}

		private PlaceableTileEntry FindValidTile(int itemAmount, LayerTile tileAtPosition)
		{
			// find the first valid way possible to place a tile
			foreach (var entry in waysToPlace)
			{
				//skip what can't be afforded
				if (entry.itemCost > itemAmount)
					continue;

				//open space
				if (tileAtPosition == null && entry.placeableOn == LayerType.None && entry.placeableOnlyOnTile == null)
				{
					return entry;
				}

				// placing on an existing tile
				else if (tileAtPosition.LayerType == entry.placeableOn &&
						 (entry.placeableOnlyOnTile == null || entry.placeableOnlyOnTile == tileAtPosition))
				{
					return entry;
				}
			}
			return null;
		}

		/// <summary>
		/// Some tile items can be placed on multiple things, so this defines one way in which an item can be placed
		/// </summary>
		[Serializable]
		private class PlaceableTileEntry
		{
			[Tooltip("Layer tile which this will create when placed.")]
			public LayerTile layerTile = null;

			[Tooltip("What layer this can be placed on top of. Choose None to allow placing on empty space.")]
			[SerializeField]
			public LayerType placeableOn = LayerType.Base;

			[Tooltip("Particular tile this is placeable on. Leave empty to allow placing on any tile.")]
			[SerializeField]
			public LayerTile placeableOnlyOnTile = null;

			[Tooltip("The amount of this item required to place tile.")]
			[SerializeField]
			public int itemCost = 1;

			[Tooltip("Time it takes to place the tile")]
			[SerializeField]
			public float timeToPlace = 1f;
		}
	}
}