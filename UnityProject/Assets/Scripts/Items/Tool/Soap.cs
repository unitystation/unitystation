using System.Collections.Generic;
using Objects.Construction;
using UnityEngine;

namespace Items
{
	/// <summary>
	/// Main component for soap. Allows cleaning of tiles and gradual degradation of the soap as it is used.
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	public class Soap : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IExaminable, IServerSpawn
	{
		private static readonly StandardProgressActionConfig ProgressConfig =
			new StandardProgressActionConfig(StandardProgressActionType.Mop, true, false);

		[Tooltip("How many times can the soap be used until it is deleted?")]
		[SerializeField]
		private int uses = 100;

		private bool forEverLastingSoap = false;

		private int maxUses;

		[Tooltip("Time taken to clean something with the soap, measured in seconds.")]
		[SerializeField]
		private float useTime = 3.5f;


		public void OnSpawnServer(SpawnInfo info)
		{
			maxUses = uses;
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			//can only scrub tiles, for now
			if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject)) return false;

            // Get position of interaction as Vector3Int
            Vector3 position = interaction.WorldPositionTarget;
            Vector3Int positionInt = Vector3Int.RoundToInt(position);

            // Check if there is an object in the way of scrubbing the tile
			var atPosition = MatrixManager.GetAt<FloorDecal>(positionInt, side == NetworkSide.Server) as List<FloorDecal>;
            if(atPosition.Count == 0) return false;

            return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			var isWall = MatrixManager.IsWallAt(interaction.WorldPositionTarget.RoundToInt(), true);

			//server is performing server-side logic for the interaction
			//do the scrubbing
			void CompleteProgress()
			{
				CleanTile(interaction);
				Chat.AddExamineMsg(interaction.Performer, "You finish scrubbing.");
			}

			//Start the progress bar:
			var bar = StandardProgressAction.Create(ProgressConfig, CompleteProgress)
				.ServerStartProgress(interaction.WorldPositionTarget.RoundToInt(),
					useTime, interaction.Performer);
			if (bar)
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You begin to scrub the {(isWall ? "wall" : "floor")} with the {gameObject.ExpensiveName()}...",
					$"{interaction.Performer.name} begins to scrub the {(isWall ? "wall" : "floor")} with the {gameObject.ExpensiveName()}.");
			}
		}

		private void CleanTile(PositionalHandApply interaction)
		{
			var worldPos = interaction.WorldPositionTarget;
			var checkPos = worldPos.RoundToInt();
			var matrixInfo = MatrixManager.AtPoint(checkPos, true);
			matrixInfo.MetaDataLayer.Clean(checkPos, MatrixManager.WorldToLocalInt(checkPos, matrixInfo), false);
			UseUpSoap();
		}

		public void UseUpSoap()
		{
			if (forEverLastingSoap) return;
			uses -= 1;
			if (uses == 0)
			{
				_ = Despawn.ServerSingle(gameObject);
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			float percentageLeft = (float)uses / (float)maxUses;

			if (percentageLeft <= 0.15f)
			{
				return "There's just a tiny bit left of what it used to be, you're not sure it'll last much longer.";
			}
			else if (percentageLeft > 0.15f && percentageLeft <= 0.30f)
			{
				return "It's dissolved quite a bit, but there's still some life to it.";
			}
			else if (percentageLeft > 0.30f && percentageLeft <= 0.50f)
			{
				return "It's past its prime, but it's definitely still good.";
			}
			else if (percentageLeft > 0.50f && percentageLeft <= 0.75f)
			{
				return "It's started to get a little smaller than it used to be, but it'll definitely still last for a while.";
			}
			else if (percentageLeft > 0.75f && percentageLeft < 1f)
			{
				return "It's seen some light use, but it's still pretty fresh.";
			}
			else
			{
				return "It looks like it just came out of the package.";
			}
		}
	}
}
