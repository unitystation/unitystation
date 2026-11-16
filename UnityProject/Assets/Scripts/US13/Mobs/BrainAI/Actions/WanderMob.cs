using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using US13.Managers.NetworkManagement;
using US13.Player.MovementV2;

namespace US13.Mobs.BrainAI.Actions
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "WanderMob")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Wander One Tile", story: "Wander one tile", category: "Mob/Movement", id: "ad1dc1c1a2b34c5d8000000000000002")]
	public partial class WanderMob : BrainMobState
	{
		private static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.down, Vector2Int.left, Vector2Int.up };
		private int facing;
		private int steps;
		protected override Status OnStart()
		{
			if (!CanAct || AI.Traversal?.Movement == null) return Status.Failure;
			if (!Context.TryAcquireMovement(this)) return Status.Failure;
			var movement = AI.Traversal.Movement;
			var direction = Directions[facing];
			var move = movement.GenerateMoveData(movement.registerTile.LocalPosition, MovementSynchronisation.VectorToPlayerMoveDirection(direction));
			bool moved = movement.TryMove(ref move, AI.Body.gameObject, CustomNetworkManager.IsServer, out _);
			if (moved) steps++;
			if (!moved || UnityEngine.Random.Range(0, 10 - Math.Max(7, steps)) == 0)
			{
				facing = (facing + (UnityEngine.Random.value < 0.5f ? 1 : 3)) % 4;
				steps = 0;
			}
			AI.Body.Rotatable?.SetFaceDirectionLocalVector(Directions[facing]);
			Context.ReleaseMovement(this);
			return Status.Success;
		}
		protected override void OnEnd() => Context?.ReleaseMovement(this);
	}
}