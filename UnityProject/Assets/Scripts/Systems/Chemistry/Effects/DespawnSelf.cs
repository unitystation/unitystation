using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "DespawnSelf", menuName = "ScriptableObjects/Chemistry/Effect/DespawnSelf")]
	[Serializable]
	public class DespawnSelf : Chemistry.Effect
	{
		public override void Apply(MonoBehaviour sender, float amount)
		{
			_ = Despawn.ServerSingle(sender.gameObject);
		}
	}
}