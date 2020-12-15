using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

namespace items.food
{

public class Bread : MonoBehaviour, IQuantumReaction
{

	[SerializeField]
	private GameObject MutatedBread = default;

	public void OnTeleportStart()
	{
		//Left Blank on Purpose
	}

	public void OnTeleportEnd()
	{
		if (DMMath.Prob(10))
		{
			Spawn.ServerPrefab(MutatedBread, gameObject.AssumedWorldPosServer());
			Despawn.ServerSingle(gameObject);
		}
	}

	//Bread Class has big plans for the future WORK-IN-PROGRESS

}
}