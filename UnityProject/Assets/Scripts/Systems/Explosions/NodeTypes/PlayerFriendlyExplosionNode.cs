using System.Collections;
using System.Collections.Generic;
using Core;
using HealthV2;
using Items;
using Light2D;
using Systems.Explosions;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

public class PlayerFriendlyExplosionNode : ExplosionNode
{

	public override float DoDamage(Matrix matrix, float damageDealt, Vector3Int v3int)
	{
		var metaTileMap = matrix.MetaTileMap;
		float energyExpended = metaTileMap.ApplyDamage(v3int, damageDealt,
			MatrixManager.LocalToWorldInt(v3int, matrix.MatrixInfo), AttackType.Bomb);

		DamageLayers(damageDealt, v3int);

		foreach (var integrity in matrix.Get<Integrity>(v3int, true))
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

		return energyExpended;
	}

	public override void DoInternalDamage(float strength, BodyPart bodyPart)
	{
		return; //todo: add damage to prosthetics and augs
	}

	public override ExplosionNode GenInstance()
	{
		return new PlayerFriendlyExplosionNode();
	}
}
