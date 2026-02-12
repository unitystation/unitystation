using Chemistry;
using UnityEngine;
using US13.Managers.MatrixManager;
using US13.ScriptableObjects.Atmospherics;
using US13.Tilemaps.Behaviours.Meta;
using Util;

namespace US13.Systems.ChemistryEffects.Body
{
	[CreateAssetMenu(fileName = "GasEmission",
		menuName = "ScriptableObjects/Chemistry/Effects/GasEmissionEffect")]
	public class GasEmissionEffect : Chemistry.Effect
	{
		[SerializeField] private GasSO gasToEmit = null;
		[SerializeField] private float amountOfGas = 1;

		[SerializeField] private float emissionChance = 15;

		public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix,Vector3 WorldPosition, float amount)
		{
			if (DMMath.Prob(emissionChance) == false) return;

			//IDK I've commented out since I don't know what it does should be fine like this?
			// if (sender is MetabolismComponent metabolismComponent == false) return;
			// if (metabolismComponent.RelatedPart.HealthMaster == false) return;
			//
			// LivingHealthMasterBase healthMaster = metabolismComponent.RelatedPart.HealthMaster;
			// Vector3 actorPos = healthMaster.gameObject.AssumedWorldPosServer();

			//Add gas to area (Typically Miasma)
			if (gasToEmit != null && amountOfGas > 0)
			{
				MetaDataNode node = MatrixManager.GetMetaDataAt(WorldPosition.CutToInt());
				node.GasMixLocal.AddGasWithTemperature(gasToEmit, amountOfGas, node.GasMixLocal.Temperature);
			}
		}
	}
}
