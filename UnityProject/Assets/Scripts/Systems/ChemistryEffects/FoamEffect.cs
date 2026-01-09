using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Chemistry.Components;
using Logs;
using UnityEngine;
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
