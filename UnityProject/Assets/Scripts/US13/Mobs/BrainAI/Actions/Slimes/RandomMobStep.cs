using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;

namespace US13.Mobs.BrainAI.Actions.Slimes
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "RandomMobStep")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Push Mob One Random Tile", story: "Move one random tile", category: "Mob/Movement", id: "ad1dc1c1a2b34c5d8000000000000016")]
	public partial class RandomMobStep : BrainMobState
	{
		private static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
		internal static void Push(BrainMobAI ai)
		{
			var direction = Directions[UnityEngine.Random.Range(0, Directions.Length)];
			ai.Body.UniversalObjectPhysics.TryTilePush(direction, null);
			ai.Body.Rotatable?.SetFaceDirectionLocalVector(direction);
		}
		protected override Status OnStart() { if (!CanAct) return Status.Failure; Push(AI); return Status.Success; }
	}
}