using Chemistry;
using UnityEngine;
using US13.Health.Objects;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Player;

namespace US13.HealthV2.Living.MedicalChemistry
{
	[CreateAssetMenu(fileName = "effect", menuName = "ScriptableObjects/Chemistry/Effect/IgniteContainerEffect")]
	public class IgniteContainerEffect : Chemistry.Effect
	{
		[SerializeField] private float fireStacksPerUnit = 0.5f;

		public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix, Vector3 WorldPosition, float amount)
		{
			if (sender == null) return;

			var metabolismComponent = sender.GetBodyPartBehavior<MetabolismComponent>();
			if (metabolismComponent == null)
			{
				if (sender.TryGetComponent<Flammable>(out var flammable)) flammable.AddFireStacks((int)(fireStacksPerUnit * amount));
				return;
			}
			if (metabolismComponent.RelatedPart.HealthMaster == false) return;
			metabolismComponent.RelatedPart.HealthMaster.ChangeFireStacks(fireStacksPerUnit * amount);
		}
	}
}
