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

		public override IEnumerator NowExplosion(MonoBehaviour sender,ReagentMix ReagentMix,  Vector3 WorldPosition, float amount)
		{
			yield return WaitFor.Seconds(Delay);

			// Following function uses the code from the Explosions file.

			// Get data from container before despawning


			float strength = ChemistryUtils.CalculateYieldFromReaction(amount, potency);


			ExplosionNode node = ExplosionTypes.NodeTypes[explosionType];
			if (sender != null)
			{
				UniversalObjectPhysics objectBehaviour = sender.GetComponent<UniversalObjectPhysics>();
				RegisterObject registerObject = sender.GetComponent<RegisterObject>();
				BodyPart bodyPart = sender.GetComponent<BodyPart>();
				bool insideBody = false;
				if (bodyPart != null && bodyPart.HealthMaster != null)
				{
					insideBody = true;
				}
				if (insideBody && strength > 0)
				{
					node.DoInternalDamage(strength, bodyPart);
				}
				var picked = sender.GetComponent<Pickupable>();
				// If sender is a pickupable item not inside the body, destroy it.
				if (explosionType != ExplosionTypes.ExplosionType.Harmless && picked != null && !insideBody)
				{
					_ = Despawn.ServerSingle(sender.gameObject);
				}
			}






			if (strength > 0)
			{
				bool Explode = true;

				if (sender != null)
				{
					// Explosion here
					var picked = sender.GetComponent<Pickupable>();
					if (picked != null && picked.ItemSlot != null)
					{
						AfflictRadius(WorldPosition, sender.gameObject, strength / 3); //Reduced flash when inside an object
						//Otherwise, if it's not inside of a player, we consider it just an item
						Explosion.StartExplosion(WorldPosition.RoundToInt(), strength, node, stunNearbyPlayers: false, radiusMultiplier: 3);
						Explode = false;
					}
				}


				if (Explode)
				{
					AfflictRadius(WorldPosition, sender.gameObject, strength);
					Explosion.StartExplosion(WorldPosition.RoundToInt(), strength, node, stunNearbyPlayers: false, radiusMultiplier: 3);

				}
			}
		}

		private void AfflictRadius(Vector3 worldPosition, GameObject sender, float strength)
		{
			var afflictionRadius = (int)(Math.Round(strength / (Math.PI * 15)) + 5);

			var possibleTargets = Physics2D.OverlapCircleAll(worldPosition, afflictionRadius, LayerMask.GetMask("Players"));
			foreach (var target in possibleTargets)
			{
				var result = MatrixManager.Linecast(worldPosition, LayerTypeSelection.Walls, null,target.gameObject.AssumedWorldPosServer(), false);
				if (result.ItHit) continue;

				var duration = strength * STUN_DURATION_PER_YIELD;
				duration = result.Distance < afflictionRadius * 0.65f ? duration : duration / 2;

				if (duration <= 0) continue;
				if (target.gameObject.TryGetComponentCustom<LivingHealthMasterBase>(out var livingHealthMasterBase) == false) continue;

				bool successfulTrigger = flashPlayers == true && livingHealthMasterBase.TryFlash(duration) && stunPlayers == true;

				if (deafenPlayers == true && livingHealthMasterBase.TryDeafen(sender, duration * 8) && stunPlayers == true) successfulTrigger = true;

				if(successfulTrigger == true) livingHealthMasterBase.GetComponent<RegisterPlayer>()?.ServerStun(duration);
			}
		}
	}
}
