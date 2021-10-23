using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Systems.Explosions;
using HealthV2;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/ChemExplosion")]
	public class ChemExplosion : Chemistry.Effect
	{
		/// <summary>
		/// Multiplier applied to final strength calculation
		/// </summary>
		[Tooltip("Multiplier applied to final strength calculation")]
		[SerializeField] private float potency = 1;

		public override void Apply(MonoBehaviour sender, float amount)
		{
			// Following function uses the code from the Explosions file.

			// Get data from container before despawning
			ObjectBehaviour objectBehaviour = sender.GetComponent<ObjectBehaviour>();
			RegisterObject registerObject = sender.GetComponent<RegisterObject>();
			BodyPart bodyPart = sender.GetComponent<BodyPart>();

			bool insideBody = false;
			if (bodyPart != null && bodyPart.HealthMaster != null)
			{
				insideBody = true;
			}



			// Based on radius calculation in Explosions\Explosion.cs, where an amount of 30u will have an
			// explosion radius of 1. Strength is determined using a logarthmic formula to cause diminishing returns.
			var strength = (float)(-463+205*Mathf.Log(amount)+75*Math.PI)*potency;


			if (insideBody && strength > 0)
			{
				if (strength >= bodyPart.Health)
				{
					float temp = bodyPart.Health; //temporary store to make sure we don't use an updated health when decrementing strength
					bodyPart.TakeDamage(null, temp, AttackType.Internal, DamageType.Brute);
					strength -= temp;
				}
				else
				{
					bodyPart.TakeDamage(null, strength, AttackType.Internal, DamageType.Brute);
					strength = 0;
				}

				foreach (BodyPart part in bodyPart.HealthMaster.BodyPartList)
				{
					if (strength >= part.Health)
					{
						float temp = part.Health; //temporary store to make sure we don't use an updated health when decrementing strength
						part.TakeDamage(null, temp, AttackType.Internal, DamageType.Brute);
						strength -= temp;
					}
					else
					{
						part.TakeDamage(null, strength, AttackType.Internal, DamageType.Brute);
						strength = 0;
					}
				}
			}

			// Explosion here
			var picked = sender.GetComponent<Pickupable>();
			if (picked != null)
			{
				//If sender is in an inventory use the position of the inventory.
				if (picked.ItemSlot != null)
				{
					objectBehaviour = picked.ItemSlot.ItemStorage.GetRootStorageOrPlayer().GetComponent<ObjectBehaviour>();
					registerObject = picked.ItemSlot.ItemStorage.GetRootStorageOrPlayer().GetComponent<RegisterObject>();
				}
			}

			if (strength > 0)
			{
				//Check if this is happening inside of an Object first (machines, closets?)
				if (registerObject == null)
				{
					//If not, we need to check if the item is a bodypart inside of a player
					if (insideBody)
					{
						Explosion.StartExplosion(bodyPart.HealthMaster.RegisterTile.WorldPosition, strength,
							bodyPart.HealthMaster.RegisterTile.Matrix);
					}
					else
					{
						//Otherwise, if it's not inside of a player, we consider it just an item
						Explosion.StartExplosion(objectBehaviour.registerTile.LocalPosition, strength,
							objectBehaviour.registerTile.Matrix);
					}
				}
				else
				{
					Explosion.StartExplosion(registerObject.LocalPosition, strength,
						registerObject.Matrix);
				}
			}

			// If sender is a pickupable item not inside the body, destroy it.
			if (picked != null && !insideBody)
			{
				Despawn.ServerSingle(sender.gameObject);
			}
		}
	}
}
