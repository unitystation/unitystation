using NaughtyAttributes;
using UnityEngine;
using US13.Core.Attributes;
using US13.Core.Modular;
using US13.HealthV2.Living;
using US13.HealthV2.Living.PolymorphicSystems.Hunger;

namespace US13.HealthV2.Modular.Implementation
{
	public class ConditionEnableIfHungerMethodActive : IConditional
	{
		[field: SerializeReference, SelectImplementation(typeof(IHungerCalculation)), InfoBox("Which system should this condition be applied to?")]
		public IHungerCalculation HungerCalculationToCheck;

		[Tooltip("Allow if all BUT the selected method is active.")]
		public bool InvertCheck = false;

		public bool PassCondition(object context)
		{
			if (context is not GameObject c) return false;
			if (c.TryGetComponent<LivingHealthMasterBase>(out var health) == false) return false;
			if (InvertCheck)
			{
				return health.GetHungerSystem()?.HungerCalculationMethod.GetType() !=
				       HungerCalculationToCheck.GetType();
			}
			return health.GetHungerSystem()?.HungerCalculationMethod.GetType() ==
			       HungerCalculationToCheck.GetType();
		}
	}
}