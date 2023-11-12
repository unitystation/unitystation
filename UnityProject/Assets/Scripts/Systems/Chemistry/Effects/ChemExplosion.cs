using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

		[Tooltip("Explosion type")]
		[SerializeField] private ExplosionTypes.ExplosionType explosionType = ExplosionTypes.ExplosionType.Regular;

		public float Delay = 0;

		public override void Apply(MonoBehaviour sender, float amount)
		{

			sender.StartCoroutine(NowExplosion(sender,amount ));
		}

		public IEnumerator NowExplosion(MonoBehaviour sender, float amount)
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
						Explosion.StartExplosion(bodyPart.HealthMaster.RegisterTile.WorldPosition, strength, node);
					}
					else
					{
						//Otherwise, if it's not inside of a player, we consider it just an item
						Explosion.StartExplosion(objectBehaviour.registerTile.WorldPosition, strength, node);
					}
				}
				else
				{
					Explosion.StartExplosion(registerObject.WorldPosition, strength, node);
				}
			}

			// If sender is a pickupable item not inside the body, destroy it.
			if (picked != null && !insideBody)
			{
				_ = Despawn.ServerSingle(sender.gameObject);
			}
		}
	}
}
