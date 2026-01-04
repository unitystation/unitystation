using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Chemistry.Components;
using Logs;
using UnityEngine;


[CreateAssetMenu(fileName = "SmokeEffect", menuName = "ScriptableObjects/Chemistry/Effect/SmokeEffect")]
[Serializable]
public class SmokeEffect : Chemistry.Effect
{
	public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix, Vector3 WorldPosition, float amount)
	{
		amount = (int) Math.Floor(amount);
		SmokeAndFoamManager.StartSmokeAt(WorldPosition, ReagentMix, (int)amount);
	}
}