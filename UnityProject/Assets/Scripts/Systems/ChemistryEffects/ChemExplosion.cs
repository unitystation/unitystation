using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using Systems.Explosions;
using HealthV2;
using Initialisation;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "effect", menuName = "ScriptableObjects/Chemistry/Effect/ChemExplosion")]
	public class ChemExplosion : Chemistry.Effect
	{
		/// <summary>
		/// Multiplier applied to final strength calculation
		/// </summary>
		[Tooltip("Multiplier applied to final strength calculation")]
		[SerializeField] protected float potency = 1;

		[Tooltip("Explosion type")]
		[field: SerializeField] public ExplosionTypes.ExplosionType explosionType { get; private set; } = ExplosionTypes.ExplosionType.Regular;

		private const int CHEM_EXPLOSIONS_RADIUS_MULTIPLIER = 5;
		//Chem explosions are often lower potency than standard explosives like syndi bombs.
		//But as a result they often have tiny radii, looking at most 1-2 tiles.
		//We give chemical ordnance a radius boost so they cover an area and not just 1-2 tiles.

		public float Delay = 0;

		public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix, Vector3 WorldPosition, float amount)
		{

			LoadManager.Instance.StartCoroutine(NowExplosion(sender, ReagentMix ,WorldPosition,amount ));
		}

		public virtual float FindYield(float amount)
		{
			return ChemistryUtils.CalculateYieldFromReaction(amount, potency);
		}

		public virtual IEnumerator NowExplosion(MonoBehaviour sender,ReagentMix ReagentMix,  Vector3 WorldPosition, float amount)
		{
			yield return WaitFor.Seconds(Delay);
			float strength = ChemistryUtils.CalculateYieldFromReaction(amount, potency);
			ExplosionNode node = ExplosionTypes.NodeTypes[explosionType];
			if (sender)
			{
				BodyPart bodyPart = sender.GetComponent<BodyPart>();
				bool insideBody = bodyPart && bodyPart.HealthMaster;
				if (insideBody && strength > 0) node.DoInternalDamage(strength, bodyPart);
				// If sender is a pickupable item not inside the body, destroy it.
				// var picked = sender.GetComponent<Pickupable>();
				// if (explosionType != ExplosionTypes.ExplosionType.Harmless && picked != null && !insideBody)
				// {
				// 	_ = Despawn.ServerSingle(sender.gameObject);
				// }
			}

			if (strength > 0)
			{
				Explosion.StartExplosion(WorldPosition.RoundToInt(), strength, node, stunNearbyPlayers: strength > 400, radiusMultiplier: CHEM_EXPLOSIONS_RADIUS_MULTIPLIER);

			}
		}
	}
}
