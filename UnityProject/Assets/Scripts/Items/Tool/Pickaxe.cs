using UnityEngine;
using AddressableReferences;
using Logs;
using Messages.Server.SoundMessages;
using Objects.Mining;
using TileManagement;
using Tiles;

namespace Items
{
	/// <summary>
	/// Allows object to be used as a pickaxe to mine rocks.
	/// </summary>
	public class Pickaxe : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, ICheckedInteractable<HandApply>
	{
		[SerializeField] private AddressableAudioSource pickaxeSound = null;

		[Tooltip("How much does this tool multiply mining time")]
		[SerializeField]
		private float timeMultiplier = 1f;

		[Range(1, 10)]
		[Tooltip("what degree of hardness this mining tool can overcome. Higher means the pick can mine more types of things.")]
		[SerializeField]
		private int pickHardness = 5;

		private string objectName;

		#region Tiles

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (!Validations.HasTarget(interaction)) return false;
			var interactableTiles = interaction.TargetObject.GetComponent<InteractableTiles>();
			if (interactableTiles == null) return false;

			return Validations.IsMineableAt(interaction.WorldPositionTarget, interactableTiles.MetaTileMap);
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			var interactableTiles = interaction.TargetObject.GetComponent<InteractableTiles>();
			if (interactableTiles.LayerTileAt(interaction.WorldPositionTarget) is not BasicTile tile) return;
			var wallTile = interactableTiles.MetaTileMap.GetTileAtWorldPos(interaction.WorldPositionTarget, LayerType.Walls) as BasicTile;
			var calculatedMineTime = wallTile.MiningTime * timeMultiplier;

			void FinishMine()
			{
				if (interactableTiles == null)
				{
					Loggy.LogError("No interactable tiles found, mining cannot be finished", Category.TileMaps);
				}
				else
				{
					AudioSourceParameters parameters = new AudioSourceParameters(0, 100f);
					_ = SoundManager.PlayNetworkedAtPosAsync(CommonSounds.Instance.BreakStone, interaction.WorldPositionTarget, parameters);
					var cellPos = interactableTiles.MetaTileMap.WorldToCell(interaction.WorldPositionTarget);

					foreach (var tileInteraction in tile.OnTileFinishMining)
					{
						tileInteraction.ServerPerformInteraction(interaction);
					}
					Spawn.ServerPrefab(tile.SpawnOnDeconstruct, interaction.WorldPositionTarget, count: tile.SpawnAmountOnDeconstruct);
					interactableTiles.TileChangeManager.MetaTileMap.RemoveTileWithlayer(cellPos, LayerType.Walls);
					interactableTiles.TileChangeManager.MetaTileMap.RemoveOverlaysOfType(cellPos, LayerType.Effects, OverlayType.Mining);
				}
			}

			objectName = wallTile.DisplayName;

			SoundManager.PlayNetworkedAtPos(pickaxeSound, interaction.WorldPositionTarget);

			foreach (var tileInteraction in tile.OnTileStartMining)
			{
				tileInteraction.ServerPerformInteraction(interaction);
			}

			if (pickHardness >= wallTile.MiningHardness)
			{
				ToolUtils.ServerUseToolWithActionMessages(
				interaction, calculatedMineTime,
				$"You start mining the {objectName}...",
				$"{interaction.Performer.ExpensiveName()} starts mining the {objectName}...",
				default, default,
				FinishMine);
			}
			else
			{
				Chat.AddActionMsgToChat(interaction,
					$"You ping off the {objectName}, leaving hardly a scratch.",
						$"{interaction.Performer.ExpensiveName()} pings off the {objectName}, leaving hardly a scratch.");
			}
		}

		#endregion

		#region Objects

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject == interaction.HandObject) return false;

			return interaction.TargetObject.TryGetComponent<PickaxeMineable>(out _);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var calculatedMineTime = interaction.TargetObject.GetComponent<PickaxeMineable>().MineTime * timeMultiplier;
			objectName = interaction.TargetObject.ExpensiveName();

			SoundManager.PlayNetworkedAtPos(pickaxeSound, interaction.PerformerPlayerScript.WorldPos, sourceObj: interaction.Performer);

			if (pickHardness >= interaction.TargetObject.GetComponent<PickaxeMineable>().MineableHardness)
			{
				ToolUtils.ServerUseToolWithActionMessages(
				interaction, calculatedMineTime,
				$"You start mining the {objectName}...",
				$"{interaction.Performer.ExpensiveName()} starts mining the {objectName}...",
				default, default,
				() =>
				{
					SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.BreakStone,
							interaction.PerformerPlayerScript.WorldPos, sourceObj: interaction.Performer);
					_ = Despawn.ServerSingle(interaction.TargetObject);
				});
			}
			else
			{
				Chat.AddActionMsgToChat(interaction,
					$"You ping off the {objectName}, leaving hardly a scratch.",
						$"{interaction.Performer.ExpensiveName()} pings off the {objectName}, leaving hardly a scratch.");
			}


		}

		#endregion
	}
}
