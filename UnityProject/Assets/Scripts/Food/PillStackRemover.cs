using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO Needs to be changed over to  medical chemistry Instead
public class PillStackRemover : Consumable
{
	public float StackPercentageRemove = 50;
	public override void TryConsume(GameObject feeder, GameObject eater)
	{
		var Health = eater.GetComponent<LivingHealthBehaviour>();
		Health.RadiationStacks *= StackPercentageRemove/100f;
		Despawn.ServerSingle(this.gameObject);
	}
}
