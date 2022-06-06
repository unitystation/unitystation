using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Systems.Explosions;
using HealthV2;
using Chemistry;
using Chemistry.Components;

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

		[Tooltip("Explosion type")]
		[SerializeField] private ExplosionTypes.ExplosionType explosionType = ExplosionTypes.ExplosionType.Regular;

		[Tooltip("Despawn container after reaction?")]
		[SerializeField] private bool DespawnContainer = true;

		[Tooltip("Use other reagents in container?")]
		[SerializeField] private bool UseOtherReagents = true;

		public override void Apply(MonoBehaviour sender, float amount)
		{
			// Following function uses the code from the Explosions file.

			// Get data from container before despawning
			UniversalObjectPhysics objectBehaviour = sender.GetComponent<UniversalObjectPhysics>();
			RegisterObject registerObject = sender.GetComponent<RegisterObject>();
			BodyPart bodyPart = sender.GetComponent<BodyPart>();
			ExplosionNode node = ExplosionTypes.NodeTypes[explosionType];
			ReagentMix otherReagents = new ReagentMix();
			if (UseOtherReagents)
			{
				otherReagents = sender.gameObject.GetComponent<ReagentContainer>().CurrentReagentMix.Clone();
				sender.gameObject.GetComponent<ReagentContainer>().CurrentReagentMix.Clear();
			}

			bool insideBody = false;
			if (bodyPart != null && bodyPart.HealthMaster != null)
			{
				insideBody = true;
			}



			// Based on radius calculation in Explosions\Explosion.cs, where an amount of 30u will have an
			// explosion radius of 1. Strength is determined using a logarthmic formula to cause diminishing returns.
			float strength = (float)(-463+205*Mathf.Log(amount)+75*Math.PI)*potency;


			if (insideBody && strength > 0)
			{
				node.DoInternalDamage(strength, bodyPart);
			}

			// Explosion here
			var picked = sender.GetComponent<Pickupable>();
			if (picked != null)
			{
				//If sender is in an inventory use the position of the inventory.
				if (picked.ItemSlot != null)
				{
					objectBehaviour = picked.ItemSlot.ItemStorage.GetRootStorageOrPlayer().GetComponent<UniversalObjectPhysics>();
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
						Explosion.StartExplosion(bodyPart.HealthMaster.RegisterTile.WorldPosition, strength, node, -1, -1, otherReagents);
					}
					else
					{
						//Otherwise, if it's not inside of a player, we consider it just an item
						Explosion.StartExplosion(objectBehaviour.registerTile.WorldPosition, strength, node, -1, -1, otherReagents);
					}
				}
				else
				{
					Explosion.StartExplosion(registerObject.WorldPosition, strength, node, -1, -1, otherReagents);
				}
			}

			// If sender is a pickupable item not inside the body, destroy it.
			if (picked != null && !insideBody && DespawnContainer)
			{
				Despawn.ServerSingle(sender.gameObject);
			}
		}
	}
}
