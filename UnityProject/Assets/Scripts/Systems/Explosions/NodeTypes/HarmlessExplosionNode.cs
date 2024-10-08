using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Items;
using Light2D;
using Systems.Explosions;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Systems.Explosions
{
	public class HarmlessExplosionNode : ExplosionNode
	{

		public override float DoDamage(Matrix matrix, float damageDealt, Vector3Int v3int)
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
