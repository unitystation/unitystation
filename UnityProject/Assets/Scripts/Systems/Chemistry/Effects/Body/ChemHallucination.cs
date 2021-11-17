using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/Hallucination")]
	public class ChemHallucination : Chemistry.Effect
	{
		[Tooltip("Adds hallucination time")]
		[SerializeField] private float hallucinationTime = 1;

		[Tooltip("Chance for this to take effect")]
		[SerializeField] private float percentageChance = 100;
		public override void Apply(MonoBehaviour sender, float amount)
		{
			if (Random.Range(0, 100)>percentageChance)
			{
				//TODO: do the thing
			}
		}
	}
}
