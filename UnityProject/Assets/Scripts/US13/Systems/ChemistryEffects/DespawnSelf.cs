using System;
using Chemistry;
using UnityEngine;
using US13.Core.Lifecycle;

namespace US13.Systems.ChemistryEffects
{
	[CreateAssetMenu(fileName = "DespawnSelf", menuName = "ScriptableObjects/Chemistry/Effect/DespawnSelf")]
	[Serializable]
	public class DespawnSelf : Chemistry.Effect
	{
		public override void Apply(MonoBehaviour sender,ReagentMix ReagentMix, Vector3 WorldPosition , float amount)
		{
			if (sender == null) return;
			_ = Despawn.ServerSingle(sender.gameObject);
		}
	}
}