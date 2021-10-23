using UnityEngine;
using AddressableReferences;
using Messages.Server.SoundMessages;
using Objects.Mining;
using TileManagement;

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

		private string objectName;

		#region Tiles

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (!Validations.HasTarget(interaction)) return false;
			var interactableTiles = interaction.TargetObject.GetComponent<InteractableTiles>();
			if (interactableTiles == null) return false;

			return Validations.IsMineableAt(interaction.WorldPositionTarget, interactableTiles.MetaTileMap);
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			var interactableTiles = interaction.TargetObject.GetComponent<InteractableTiles>();
			var wallTile = interactableTiles.MetaTileMap.GetTileAtWorldPos(interaction.WorldPositionTarget, LayerType.Walls) as BasicTile;
			var calculatedMineTime = wallTile.MiningTime * timeMultiplier;

			void FinishMine()
			{
				if (interactableTiles == null)
				{
					Logger.LogError("No interactable tiles found, mining cannot be finished", Category.TileMaps);
				}
				else
				{
					AudioSourceParameters parameters = new AudioSourceParameters(0, 100f);
					_ = SoundManager.PlayNetworkedAtPosAsync(CommonSounds.Instance.BreakStone, interaction.WorldPositionTarget, parameters);
					var cellPos = interactableTiles.MetaTileMap.WorldToCell(interaction.WorldPositionTarget);

					var tile = interactableTiles.LayerTileAt(interaction.WorldPositionTarget) as BasicTile;
					Spawn.ServerPrefab(tile.SpawnOnDeconstruct, interaction.WorldPositionTarget, count: tile.SpawnAmountOnDeconstruct);
					interactableTiles.TileChangeManager.RemoveTile(cellPos, LayerType.Walls);
					interactableTiles.TileChangeManager.RemoveOverlaysOfType(cellPos, LayerType.Effects, OverlayType.Mining);
				}
			}

			objectName = wallTile.DisplayName;

			SoundManager.PlayNetworkedAtPos(pickaxeSound, interaction.WorldPositionTarget);

			ToolUtils.ServerUseToolWithActionMessages(
				interaction, calculatedMineTime,
				$"You start mining the {objectName}...",
				$"{interaction.Performer.ExpensiveName()} starts mining the {objectName}...",
				default, default,
				FinishMine);
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

		#endregion
	}
}
