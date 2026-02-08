using System;
using Chemistry;
using UnityEngine;
using US13.ScriptableObjects.Atmospherics;
using US13.Tilemaps.Behaviours.Meta.Atmospherics.Data;
using Util;

namespace US13.Systems.ChemistryEffects
{

	[Serializable]
	[CreateAssetMenu(fileName = "ReleaseGas", menuName = "ScriptableObjects/Chemistry/Effect/ReleaseGas")]
	public class ReleaseGas : Chemistry.Effect
	{
		public GasSO ToRelease;
		public float AmountToRelease = 10;
		public float TemperatureK = 293.15f;

		public override void Apply(MonoBehaviour onObject,ReagentMix ReagentMix, Vector3 WorldPosition , float amount)
		{
			var Matrix =  WorldPosition.GetMatrixAtWorld();

			var	metaNode = Matrix.MetaDataLayer.Get(WorldPosition.ToLocalInt(Matrix));

			lock (metaNode.GasMixLocal.GasesArray) //no Double lock
			{
				var mix = new GasMix(2.5f, TemperatureK);
				mix.AddGasWithTemperature(ToRelease,AmountToRelease,TemperatureK);
				GasMix.TransferGas(metaNode.GasMixLocal,mix, mix.Moles );
			}
		}
	}
}