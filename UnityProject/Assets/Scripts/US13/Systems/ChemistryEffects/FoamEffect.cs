using System;
using Chemistry;
using UnityEngine;
using US13.Systems.SmokeAndFoam;

namespace US13.Systems.ChemistryEffects
{
	[CreateAssetMenu(fileName = "FoamEffect", menuName = "ScriptableObjects/Chemistry/Effect/FoamEffect")]
	[Serializable]
	public class FoamEffect : Chemistry.Effect
	{

		public bool WallFoam = false;
		public bool SmartWallFoam = false;

		public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix ,Vector3 WorldPosition , float amount)
		{

			if (sender == null) return;
			amount = (int) Math.Floor(amount);
			var senderPosition = WorldPosition;

			SmokeAndFoamManager.StartFoamAt(senderPosition,ReagentMix, (int)amount, WallFoam, SmartWallFoam);
		}
	}
}
