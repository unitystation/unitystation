using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;


namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/ChemExplosion")]
	public class ChemExplosion : Chemistry.Effect
	{
		public override void Apply(MonoBehaviour sender, float amount)
		{
			// Following function uses the code from the Explosions file.

			// Get data from container before despawning
			ObjectBehaviour objectBehaviour = sender.GetComponent<ObjectBehaviour>();
			RegisterObject registerObject = sender.GetComponent<RegisterObject>();

			// Based on radius calculation in Explosions\Explosion.cs, where an amount of 30u will have an 
			// explosion radius of 1. Strength is determined using a logarthmic formula to cause diminishing returns.
			var strength = (float)(-463+205*Mathf.Log(amount)+75*Math.PI);

			// Explosion here
			var picked = sender.GetComponent<Pickupable>();
			if (picked != null)
			{

				//If sender is not in an inventory, use own position in world. Otherwise use the position of the inventory.
				if (picked.ItemSlot != null)
				{
					objectBehaviour = picked.ItemSlot.ItemStorage.gameObject.GetComponent<ObjectBehaviour>();
					registerObject = picked.ItemSlot.ItemStorage.gameObject.GetComponent<RegisterObject>();
				}


				
				
			}

		if (registerObject == null)
		{
			Explosions.Explosion.StartExplosion(objectBehaviour.registerTile.LocalPosition, strength,
				objectBehaviour.registerTile.Matrix);
		}
		else
		{
			Explosions.Explosion.StartExplosion(registerObject.LocalPosition, strength,
				registerObject.Matrix);
		}


		// If sender is a pickupable item, destroy it.
		if (picked != null)
		{
			Despawn.ServerSingle(sender.gameObject);
		}

		}
	}
}