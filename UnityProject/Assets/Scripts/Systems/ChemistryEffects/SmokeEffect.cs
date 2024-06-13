using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using Logs;
using UnityEngine;


[CreateAssetMenu(fileName = "SmokeEffect", menuName = "ScriptableObjects/Chemistry/Effect/SmokeEffect")]
[Serializable]
public class SmokeEffect : Chemistry.Effect
{
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

		SmokeAndFoamManager.StartSmokeAt(senderPosition,Container.CurrentReagentMix, (int)amount);
	}
}