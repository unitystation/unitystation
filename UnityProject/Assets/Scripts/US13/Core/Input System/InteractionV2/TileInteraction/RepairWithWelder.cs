using UnityEngine;
using US13.Items.Tool;
using US13.Items.Traits;
using US13.Tilemaps.Behaviours;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Tiles;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Core.Input_System.InteractionV2.TileInteraction
{
	[CreateAssetMenu(fileName = "RepairWithWelder", menuName = "Interaction/TileInteraction/RepairWithWelder")]
	public class RepairWithWelder : TileInteraction
	{

		[Tooltip("Seconds taken to perform this action. Set to 0 for instant.")] [SerializeField]
		private float seconds = 4;

		public LayerTile ReplaceWith; //TODO Tiles should handle itself

		public override bool WillInteract(TileApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.Intent == Intent.Harm || interaction.Intent == Intent.Disarm) return false;

			if (interaction.HandObject == null) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder) == false) return false;
			if (Validations.HasUsedActiveWelder(interaction) == false) return false;

			return true;
		}

		public override void ServerPerformInteraction(TileApply interaction)
		{

			ToolUtils.ServerUseToolWithActionMessages(interaction, seconds,
				$"You begin repairing the {interaction.BasicTile.DisplayName}...",
				$"{interaction.Performer.ExpensiveName()} begins to repair the {interaction.BasicTile.DisplayName}...",
				$"You repair the {interaction.BasicTile.DisplayName}.",
				$"{interaction.Performer.ExpensiveName()} repairs the {interaction.BasicTile.DisplayName}.",
				() => RepairTile(interaction));

		}

		private void RepairTile(TileApply interaction)
		{
			var tileMapDamage = interaction.TargetInteractableTiles.GetComponentInChildren<MetaTileMap>()
				.Layers[interaction.BasicTile.LayerType].gameObject.GetComponent<TilemapDamage>();

			if (ReplaceWith != null)
			{
				interaction.TargetInteractableTiles.MetaTileMap.SetTile(interaction.TargetCellPos, ReplaceWith);
			}


			tileMapDamage.RemoveTileEffects(interaction.TargetCellPos);
		}
	}
}
