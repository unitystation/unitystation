using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TableInteractionClimb", menuName = "Interaction/TileInteraction/TableInteractionClimb")]
public class TableInteractionClimb : TileInteraction
{
	static private List<TileType> excludeTiles = new List<TileType>() { TileType.Table };

	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TileApplyType != TileApply.ApplyType.MouseDrop) return false;

		PlayerSync playerSync;
		CustomNetTransform netTransform;
		if(interaction.UsedObject.TryGetComponent(out playerSync))
		{
			if (playerSync.IsMoving)
			{
				return false;
			}

			// Do a sanity check to make sure someone isn't dropping the shadow from like 9000 tiles away.
			var mag = (playerSync.ServerPosition - interaction.PerformerPlayerScript.PlayerSync.ServerPosition).magnitude;
			if (mag > PlayerScript.interactionDistance)
			{
				//interaction.PerformerPlayerScript
				return false;
			}	
		}
		else if(interaction.UsedObject.TryGetComponent(out netTransform)) // Do the same check but for mouse draggable objects this time.
		{
			var mag = (netTransform.ServerPosition - interaction.PerformerPlayerScript.PlayerSync.ServerPosition).magnitude;
			if (mag > PlayerScript.interactionDistance)
			{
				return false;
			}
		}
		else // Not sure what this object is so assume that we can't interact with it at all.
		{
			return false;
		}

		return true;
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		if (!interaction.UsedObject.RegisterTile().Matrix.IsPassableAt(interaction.TargetCellPos, true, true, null, excludeTiles))
		{
			return;
		}

		StandardProgressActionConfig cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, false);
		var x = StandardProgressAction.Create(cfg, () =>
		{
			PlayerScript playerScript;
			if (interaction.UsedObject.TryGetComponent(out playerScript))
			{
				List<TileType> excludeTiles = new List<TileType>() { TileType.Table };

				if (playerScript.registerTile.Matrix.IsPassableAt(interaction.TargetCellPos, true, true, null, excludeTiles))
				{
					playerScript.PlayerSync.SetPosition(interaction.WorldPositionTarget);
				}
			}
			else
			{
				var transformComp = interaction.UsedObject.GetComponent<CustomNetTransform>();
				if (transformComp != null)
				{
					transformComp.AppearAtPositionServer(interaction.WorldPositionTarget);
				}
			}
		}).ServerStartProgress(interaction.UsedObject.RegisterTile(), 3.0f, interaction.Performer);
		
		Chat.AddActionMsgToChat(interaction.Performer,
			"You begin cimbing onto the table...",
			$"{interaction.Performer.ExpensiveName()} begins climbing onto the table...");
	}
}
