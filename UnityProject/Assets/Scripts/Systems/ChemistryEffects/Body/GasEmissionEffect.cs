using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "GasEmission",
		menuName = "ScriptableObjects/Chemistry/Effects/GasEmissionEffect")]
	public class GasEmissionEffect : Chemistry.Effect
	{
		[SerializeField] private GasSO gasToEmit = null;
		[SerializeField] private float amountOfGas = 1;

		[SerializeField] private float emissionChance = 15;

		public override void Apply(MonoBehaviour sender, float amount)
		{
			if (DMMath.Prob(emissionChance) == false) return;

			if (sender is MetabolismComponent metabolismComponent == false) return;
			if (metabolismComponent.RelatedPart.HealthMaster == false) return;

			LivingHealthMasterBase healthMaster = metabolismComponent.RelatedPart.HealthMaster;
			Vector3 actorPos = healthMaster.gameObject.AssumedWorldPosServer();

			//Add gas to area (Typically Miasma)
			if (gasToEmit != null && amountOfGas > 0)
			{
				MetaDataNode node = MatrixManager.GetMetaDataAt(actorPos.CutToInt());
				node.GasMixLocal.AddGasWithTemperature(gasToEmit, amountOfGas, node.GasMixLocal.Temperature);
			}
		}
	}
}
