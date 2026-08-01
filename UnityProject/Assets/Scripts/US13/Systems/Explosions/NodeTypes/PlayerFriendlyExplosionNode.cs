using Cysharp.Threading.Tasks;
using Light2D;
using UnityEngine;
using US13.Health.Objects;
using US13.HealthV2;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.CirculatorySystem;
using US13.Items;
using US13.Tilemaps.Behaviours.Layers;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Systems.Explosions.NodeTypes
{
	public class PlayerFriendlyExplosionNode : ExplosionNode
	{
		public PlayerFriendlyExplosionNode(Vector3 _explosionStartWorldPosition) : base(_explosionStartWorldPosition)
		{
			//No other constructor logic needed
		}

		public override async UniTask Process()
		{
			float damageDealt = AngleAndIntensity.magnitude;
			if (damageDealt <= 0)
			{
				return;
			}

			if (matrix.MetaTileMap == null)
			{
				return;
			}

			if (damageDealt > 0)
			{
				//(Max): This is a terrible name. Whoever named it this way should be ashamed.
				//I have no clue what's the context of this vector. Is it local position? Is it world position? Is it a direction? Who knows!
				//Keep gatekeeping the codebase, it's not like there are other people working on this project..
				var v3int = new Vector3Int(LocalLocation.x, LocalLocation.y, 0);
				await ReguralProcessingToTilesOnly(damageDealt, v3int);
			}
		}

		public override float DoDamageToTiles(Matrix matrix, float damageDealt, Vector3Int Localv3int, MetaTileMap tileMap)
		{
			foreach (var integrity in matrix.Get<Integrity>(Localv3int.ToWorld(base.matrix).RoundToInt(), true))
			{
				//Throw items
				if (integrity.TryGetComponent<ItemAttributesV2>(out var traits))
				{
					integrity.GetComponent<UniversalObjectPhysics>()?
						.NewtonianPush(AngleAndIntensity.Rotate90(),
							9, 1, 3,
							BodyPartType.Chest, integrity.gameObject, 15);
					if (IgnoreAttributes != null && traits.HasAnyTrait(IgnoreAttributes)) continue;
				}

				//And do damage to objects
				integrity.ApplyDamage(damageDealt, AttackType.Bomb, DamageType.Brute);
			}

			return base.DoDamageToTiles(matrix, damageDealt, Localv3int, tileMap);
		}

		public override void DoInternalDamage(float strength, BodyPart bodyPart)
		{
			return; //todo: add damage to prosthetics and augs
		}

		public override ExplosionNode GenInstance()
		{
			return new PlayerFriendlyExplosionNode(ExplosionStartWorldPosition);
		}
	}
}
