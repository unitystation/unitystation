using System;
using Chemistry;
using Chemistry.Components;
using UnityEngine;


[CreateAssetMenu(fileName = "SmokeEffect", menuName = "ScriptableObjects/Chemistry/Effect/SmokeEffect")]
[Serializable]
public class SmokeEffect : Chemistry.Effect
{
	public override void Apply(GameObject sender, float amount)
	{
		amount = (int) Math.Floor(amount);
		var senderPosition = sender.gameObject.AssumedWorldPosServer();
		var Container = sender.gameObject.GetComponent<ReagentContainer>(); //Not the best thing but see how it Does
		if (Container == null)
		{
			Logger.LogError($"no ReagentContainer on {sender.gameObject} for smoke reaction");
			return;
		}

		SmokeAndFoamManager.StartSmokeAt(senderPosition,Container.CurrentReagentMix, (int)amount);
	}

	public override void HeatExposure(GameObject sender, float heat, ReagentMix inMix)
	{
		//No reaction to heat.
	}
}