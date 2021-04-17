using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Items.Food
{
	public class Bread : MonoBehaviour, IQuantumReaction
	{
		[SerializeField, FormerlySerializedAs("MutatedFood")]
		private GameObject mutatedFood = default;

		public void OnTeleportStart()
		{
			//Left Blank on Purpose
		}

		public void OnTeleportEnd()
		{
			if (DMMath.Prob(10))
			{
				Spawn.ServerPrefab(mutatedFood, gameObject.AssumedWorldPosServer());
				_ = Despawn.ServerSingle(gameObject);
			}
		}

		//Bread Class has big plans for the future WORK-IN-PROGRESS

		// So say we all.
	}
}
