using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;

[CreateAssetMenu(fileName = "TableInteractionClimb", menuName = "Interaction/TileInteraction/TableInteractionClimb")]
public class TableInteractionClimb : TileInteraction
{
	static private List<TileType> excludeTiles = new List<TileType>() { TileType.Table };

	[SerializeField] private bool canBreakOnClimb = false;
	[SerializeField, Range(0, 100f)] private float breakChance = 90f;
	[SerializeField, Range(1,25)] private float stunTimeOnBreak = 5f;
	[SerializeField, Range(1,225)] private float damageOnBreak = 5f;
	[SerializeField] private AddressableAudioSource soundOnBreak;

	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TileApplyType != TileApply.ApplyType.MouseDrop) return false;

		MovementSynchronisation playerSync;
		UniversalObjectPhysics ObjectPhysics;
		if(interaction.UsedObject.TryGetComponent(out playerSync))
		{
			if (playerSync.IsMoving || playerSync.BuckledToObject != null)
			{
				return false;
			}

			// Do a sanity check to make sure someone isn't dropping the shadow from like 9000 tiles away.
			var mag = (playerSync.OfficialPosition - interaction.PerformerPlayerScript.PlayerSync.OfficialPosition).magnitude;
			if (mag > PlayerScript.INTERACTION_DISTANCE)
			{
				//interaction.PerformerPlayerScript
				return false;
			}
		}
		else if(interaction.UsedObject.TryGetComponent(out ObjectPhysics)) // Do the same check but for mouse draggable objects this time.
		{
			var mag = (ObjectPhysics.OfficialPosition - interaction.PerformerPlayerScript.PlayerSync.OfficialPosition).magnitude;
			if (mag > PlayerScript.INTERACTION_DISTANCE)
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
		if (!interaction.UsedObject.RegisterTile().Matrix.IsPassableAtOneMatrixOneTile(interaction.TargetCellPos, true, true, null, excludeTiles))
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

				if (playerScript.RegisterPlayer.Matrix.IsPassableAtOneMatrixOneTile(interaction.TargetCellPos, true, true, null, excludeTiles))
				{
					playerScript.PlayerSync.AppearAtWorldPositionServer(interaction.WorldPositionTarget);
				}
			}
			else
			{
				var transformComp = interaction.UsedObject.GetComponent<UniversalObjectPhysics>();
				if (transformComp != null)
				{
					transformComp.AppearAtWorldPositionServer(interaction.WorldPositionTarget);
				}
			}

			var Matrix = MatrixManager.AtPoint(interaction.WorldPositionTarget, CustomNetworkManager.IsServer);
			var Tile = Matrix.TileChangeManager.MetaTileMap.GetTile(interaction.TargetCellPos, LayerType.Tables);
			if (Tile != null && interaction.BasicTile == Tile)
			{
				if(canBreakOnClimb == false) return;
				if (DMMath.Prob(breakChance) && interaction.UsedObject.TryGetComponent<RegisterPlayer>(out var victim))
				{
					interaction.BasicTile.SpawnOnDestroy.SpawnAt(SpawnDestination.At(interaction.WorldPositionTarget));
					victim.ServerStun(stunTimeOnBreak);
					_ = SoundManager.PlayNetworkedAtPosAsync(soundOnBreak, interaction.WorldPositionTarget);
					Chat.AddActionMsgToChat(interaction.UsedObject,
						$"Your weight pushes onto the {interaction.BasicTile.DisplayName} and you break it and fall through it",
						$"{interaction.UsedObject.ExpensiveName()} falls through the {interaction.BasicTile.DisplayName} as it breaks from their weight.");
					victim.PlayerScript.playerHealth.ApplyDamageAll(interaction.Performer, damageOnBreak, AttackType.Melee, DamageType.Brute);
					interaction.TileChangeManager.MetaTileMap.RemoveTileWithlayer(interaction.TargetCellPos, interaction.BasicTile.LayerType);
				}
			}
		}).ServerStartProgress(interaction.UsedObject.RegisterTile(), 3.0f, interaction.Performer);

		Chat.AddActionMsgToChat(interaction.Performer,
			"You begin climbing onto the table...",
			$"{interaction.Performer.ExpensiveName()} begins climbing onto the table...");
	}
}
