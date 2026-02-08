using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.Lifecycle;
using US13.Objects.Machines;
using Util;

namespace US13.Items.Food
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
