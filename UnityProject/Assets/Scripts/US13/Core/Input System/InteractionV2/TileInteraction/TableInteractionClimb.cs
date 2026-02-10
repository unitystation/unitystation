using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Lifecycle;
using US13.Health.Objects;
using US13.HealthV2;
using US13.Managers;
using US13.Player;
using US13.Player.MovementV2;
using US13.Tilemaps.Behaviours.Meta;
using US13.Tilemaps.Behaviours.Objects;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;
using US13.UI.Core.ProgressBar;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Core.Input_System.InteractionV2.TileInteraction
{
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
			if (interaction.UsedObject == null || interaction.UsedObject.TryGetComponent(out UniversalObjectPhysics objectPhysics) == false)
			{
				return;
			}
			if (!interaction.UsedObject.RegisterTile().Matrix.IsPassableAtOneMatrixOneTile(interaction.TargetCellPos, true, true, null, excludeTiles))
			{
				return;
			}
			StartClimbing(true, interaction.PerformerPlayerScript,
				interaction.WorldPositionTarget, interaction.TargetCellPos, interaction.BasicTile, objectPhysics, interaction.TileChangeManager);
		}

		public void StartClimbing(bool useProgressBar, PlayerScript climber, Vector3 worldPositionTarget, Vector3Int cellPosition,
			BasicTile climbingTile, UniversalObjectPhysics objectPhysics, TileChangeManager tileChangeManager)
		{
			var Local = worldPositionTarget.ToLocal(tileChangeManager.MetaTileMap.matrix);

			if (useProgressBar)
			{
				StandardProgressActionConfig cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, false);
				StandardProgressAction.Create(cfg, () =>
				{
					ClimbBehavior(climber, Local, cellPosition, climbingTile, objectPhysics, tileChangeManager);
				}).ServerStartProgress(objectPhysics.registerTile, 3.0f, climber.gameObject);
			}
			else
			{
				_ = AsyncClimbBehavior(climber, Local, cellPosition, climbingTile, objectPhysics,
					tileChangeManager);
			}

			Chat.Chat.AddActionMsgToChat(climber.gameObject,
				"You begin climbing onto the table...",
				$"{climber.gameObject.ExpensiveName()} begins climbing onto the table...");
		}

		private async UniTaskVoid AsyncClimbBehavior(PlayerScript climber, Vector3 LocalPositionTarget, Vector3Int cellPosition,
			BasicTile climbingTile, UniversalObjectPhysics objectPhysics, TileChangeManager tileChangeManager)
		{
			await UniTask.Delay(3000);
			ClimbBehavior(climber, LocalPositionTarget, cellPosition, climbingTile, objectPhysics, tileChangeManager);
		}

		private void ClimbBehavior(PlayerScript climber, Vector3 LocalPositionTarget, Vector3Int cellPosition,
			BasicTile climbingTile, UniversalObjectPhysics objectPhysics, TileChangeManager tileChangeManager)
		{
			if (climber != null)
			{
				List<TileType> excludeTiles = new List<TileType>() { TileType.Table };

				if (climber.RegisterPlayer.Matrix.IsPassableAtOneMatrixOneTile(cellPosition, true, true, null, excludeTiles))
				{
					climber.PlayerSync.AppearAtWorldPositionServer(LocalPositionTarget.ToWorld(tileChangeManager.MetaTileMap.matrix));
				}
			}
			else
			{
				var transformComp = climber.GetComponent<UniversalObjectPhysics>();
				if (transformComp != null)
				{
					transformComp.AppearAtWorldPositionServer(LocalPositionTarget.ToWorld(tileChangeManager.MetaTileMap.matrix));
				}
			}

			var matrix = tileChangeManager.MetaTileMap.matrix;
			var tile = matrix.TileChangeManager.MetaTileMap.GetTile(cellPosition, LayerType.Tables);
			if (tile != null && climbingTile == tile)
			{
				if (canBreakOnClimb == false) return;
				if (DMMath.Prob(breakChance) && objectPhysics.TryGetComponent<RegisterPlayer>(out var victim))
				{
					climbingTile.SpawnOnDestroy.SpawnAt(SpawnDestination.At(LocalPositionTarget.ToWorld(tileChangeManager.MetaTileMap.matrix)));
					victim.ServerStun(stunTimeOnBreak);
					_ = SoundManager.PlayNetworkedAtPosAsync(soundOnBreak, LocalPositionTarget.ToWorld(tileChangeManager.MetaTileMap.matrix));
					Chat.Chat.AddActionMsgToChat(objectPhysics.gameObject,
						$"Your weight pushes onto the {climbingTile.DisplayName} and you break it and fall through it",
						$"{objectPhysics.gameObject.ExpensiveName()} falls through the {climbingTile.DisplayName} as it breaks from their weight.");
					victim.PlayerScript.playerHealth.ApplyDamageAll(climber.gameObject, damageOnBreak, AttackType.Melee, DamageType.Brute);
					tileChangeManager.MetaTileMap.RemoveTileWithlayer(cellPosition, climbingTile.LayerType);
				}
			}
		}
	}
}
