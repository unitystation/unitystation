using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chemistry.Effects
{
	[Serializable]
	[CreateAssetMenu(fileName = "Hotspot", menuName = "ScriptableObjects/Chemistry/Effect/Hotspot")]
	public class Hotspot : Chemistry.Effect
	{

		public float HotSpotTemperature = 1000f;

		public override void Apply(MonoBehaviour onObject, float amount)
		{
			var Matrix =  onObject.gameObject.GetMatrixRoot();
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