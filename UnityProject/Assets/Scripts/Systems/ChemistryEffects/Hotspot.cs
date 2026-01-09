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

		public override void Apply(MonoBehaviour onObject,ReagentMix ReagentMix, Vector3 WorldPosition , float amount)
		{
			var Matrix = WorldPosition.GetMatrixAtWorld();
			var reactionManager = Matrix.ReactionManager;
			if (reactionManager == null) return;
			var Position = WorldPosition.RoundToInt().To2Int();
			reactionManager.ExposeHotspotWorldPosition(Position, HotSpotTemperature, true);
			reactionManager.ExposeHotspotWorldPosition(Position + Vector2Int.down, HotSpotTemperature, true);
			reactionManager.ExposeHotspotWorldPosition(Position + Vector2Int.left, HotSpotTemperature, true);
			reactionManager.ExposeHotspotWorldPosition(Position + Vector2Int.up, HotSpotTemperature, true);
			reactionManager.ExposeHotspotWorldPosition(Position + Vector2Int.right, HotSpotTemperature, true);
		}
	}
}