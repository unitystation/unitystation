
using System;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using UnityEngine;

namespace Chemistry.Effects
{

	[Serializable]
	[CreateAssetMenu(fileName = "ReleaseGas", menuName = "ScriptableObjects/Chemistry/Effect/ReleaseGas")]
	public class ReleaseGas : Chemistry.Effect
	{
		public GasSO ToRelease;
		public float AmountToRelease = 10;
		public float TemperatureK = 293.15f;

		public override void Apply(MonoBehaviour onObject, float amount)
		{
			var Matrix =  onObject.gameObject.GetMatrixRoot();
			
			var	metaNode = Matrix.MetaDataLayer.Get(onObject.gameObject.AssumedWorldPosServer().ToLocalInt(Matrix));

			lock (metaNode.GasMix.GasesArray) //no Double lock
			{
				var mix = new GasMix(2.5f, TemperatureK);
				mix.AddGas(ToRelease,AmountToRelease );
				GasMix.TransferGas(metaNode.GasMix,mix, mix.Moles );
			}
		}
	}
}