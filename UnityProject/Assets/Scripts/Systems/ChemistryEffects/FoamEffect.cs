using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using Logs;
using UnityEngine;
[CreateAssetMenu(fileName = "FoamEffect", menuName = "ScriptableObjects/Chemistry/Effect/FoamEffect")]
[Serializable]
public class FoamEffect : Chemistry.Effect
{

	public bool WallFoam = false;
	public bool SmartWallFoam = false;

	public override void Apply(MonoBehaviour sender, float amount)
	{
		amount = (int) Math.Floor(amount);
		var senderPosition = sender.gameObject.AssumedWorldPosServer();
		var Container = sender.gameObject.GetComponent<ReagentContainer>(); //Not the best thing but see how it Does
		if (Container == null)
		{
			Loggy.LogError($"no ReagentContainer on {sender.gameObject} for smoke reaction");
			return;
		}

		SmokeAndFoamManager.StartFoamAt(senderPosition,Container.CurrentReagentMix, (int)amount, WallFoam, SmartWallFoam);
	}
}
