using System.Collections;
using UnityEngine;
using Systems.Explosions;
using HealthV2;
using System;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "effect", menuName = "ScriptableObjects/Chemistry/Effect/ChemExplosionDeafenFlash")]
	public class ChemExplosionDeafenFlash : ChemExplosion
	{
		[SerializeField] private bool stunPlayers = false;
		[SerializeField] private bool flashPlayers = true;
		[SerializeField] private bool deafenPlayers = false;

		private const float STUN_DURATION_PER_YIELD = 0.010f; //If the explosive has a yield of 1, how long should the stun last?

		public override IEnumerator NowExplosion(MonoBehaviour sender, float amount)
		{
			yield return WaitFor.Seconds(Delay);

			// Following function uses the code from the Explosions file.

			// Get data from container before despawning
			UniversalObjectPhysics objectBehaviour = sender.GetComponent<UniversalObjectPhysics>();
			RegisterObject registerObject = sender.GetComponent<RegisterObject>();
			BodyPart bodyPart = sender.GetComponent<BodyPart>();
			ExplosionNode node = ExplosionTypes.NodeTypes[explosionType];

			bool insideBody = false;
			if (bodyPart != null && bodyPart.HealthMaster != null)
			{
				insideBody = true;
			}

			float strength = ChemistryUtils.CalculateYieldFromReaction(amount, potency);


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
						Explosion.StartExplosion(bodyPart.HealthMaster.RegisterTile.WorldPosition, strength, node, radiusMultiplier: 3);
						AfflictRadius(bodyPart.HealthMaster.RegisterTile.WorldPosition, strength / 3); //Reduced flash when inside an object
					}
					else
					{
						//Otherwise, if it's not inside of a player, we consider it just an item
						Explosion.StartExplosion(objectBehaviour.registerTile.WorldPosition, strength, node, stunNearbyPlayers: strength > 400, radiusMultiplier: 3);
						AfflictRadius(objectBehaviour.registerTile.WorldPosition, strength / 3); //Reduced flash when inside an object
					}
				}
				else
				{
					Explosion.StartExplosion(registerObject.WorldPosition, strength, node, stunNearbyPlayers: strength > 400, radiusMultiplier: 3);
					AfflictRadius(registerObject.WorldPosition, strength); 
				}
			}

			// If sender is a pickupable item not inside the body, destroy it.
			if (picked != null && !insideBody)
			{
				_ = Despawn.ServerSingle(sender.gameObject);
			}
		}

		private void AfflictRadius(Vector3 worldPosition, float strength)
		{
			var afflictionRadius = (int)(Math.Round(strength / (Math.PI * 15)) + 5);

			var possibleTargets = Physics2D.OverlapCircleAll(worldPosition, afflictionRadius, LayerMask.GetMask("Players"));
			foreach (var target in possibleTargets)
			{
				var result = MatrixManager.Linecast(worldPosition, LayerTypeSelection.Walls, null,target.gameObject.AssumedWorldPosServer(), false);
				if (result.ItHit) continue;

				var duration = strength * STUN_DURATION_PER_YIELD;
				duration = result.Distance < afflictionRadius * 0.65f ? duration : duration / 2;

				if (target.gameObject.TryGetComponentCustom<LivingHealthMasterBase>(out var livingHealthMasterBase) == false) return;

				bool successfulTrigger = false;

				if (flashPlayers == true && duration > 0 && livingHealthMasterBase.TryFlash(duration) && stunPlayers == true) successfulTrigger = true;
				if (deafenPlayers == true && duration > 0 && livingHealthMasterBase.TryDeafen(duration) && stunPlayers == true) successfulTrigger = true;
					
				if(successfulTrigger == true) livingHealthMasterBase.GetComponent<RegisterPlayer>()?.ServerStun(duration);
			}
		}
	}
}
