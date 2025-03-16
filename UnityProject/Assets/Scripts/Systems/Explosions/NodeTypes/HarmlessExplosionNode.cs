using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HealthV2;
using Items;
using Light2D;
using Systems.Explosions;
using TileManagement;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Systems.Explosions
{
	public class HarmlessExplosionNode : ExplosionNode
	{

		public override UniTask Process()
		{
			//Harmless explosives can't affect the environment at all, so no checks and/or additional methods are required
			return UniTask.CompletedTask;
		}

		public override float DoDamageToTiles(Matrix matrix, float damageDealt, Vector3Int v3int, MetaTileMap tileMap)
		{
			return 0;
		}

		public override void DoInternalDamage(float strength, BodyPart bodyPart)
		{
			return; //todo: add damage to prosthetics and augs
		}

		public override ExplosionNode GenInstance()
		{
			return new HarmlessExplosionNode();
		}
	}
}
