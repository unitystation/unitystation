using System;
using Chemistry.Components;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using UnityEngine;

namespace Chemistry.Effects
{

	[Serializable]
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/ReleaseGasAndHotspot")]
	public class ReleaseGasAndHotspot : Chemistry.Effect
	{
		public GasSO ToRelease;
		public float AmountToRelease = 10;

		public float HotSpotTemperature = 1000f;

		public override void Apply(MonoBehaviour onObject, float amount)
		{
			var Matrix =  onObject.gameObject.OnMatrixRoot();


			var	metaNode = Matrix.MetaDataLayer.Get(onObject.transform.localPosition.RoundToInt());

			lock (metaNode.GasMix.GasesArray) //no Double lock
			{
				metaNode.GasMix.AddGas(ToRelease, AmountToRelease);
			}

			var reactionManager = Matrix.ReactionManager;
			if (reactionManager == null) return;
			reactionManager.ExposeHotspotWorldPosition(onObject.gameObject.TileWorldPosition(), HotSpotTemperature, true);
			reactionManager.ExposeHotspotWorldPosition(onObject.gameObject.TileWorldPosition() + Vector2Int.down, HotSpotTemperature, true);
			reactionManager.ExposeHotspotWorldPosition(onObject.gameObject.TileWorldPosition() + Vector2Int.left, HotSpotTemperature, true);
			reactionManager.ExposeHotspotWorldPosition(onObject.gameObject.TileWorldPosition() + Vector2Int.up, HotSpotTemperature, true);
			reactionManager.ExposeHotspotWorldPosition(onObject.gameObject.TileWorldPosition() + Vector2Int.right, HotSpotTemperature, true);
		}
	}
}