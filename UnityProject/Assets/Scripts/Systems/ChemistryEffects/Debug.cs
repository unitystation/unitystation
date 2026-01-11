using Logs;
using UnityEngine;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/Debug")]
	public class Debug : Chemistry.Effect
	{
		public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix, Vector3 WorldPosition , float amount)
		{
			Loggy.Info().Format("Effect called, Sender: {0}, ReagentMix {1} Position {2} amount {3}", Category.Chemistry, sender,ReagentMix, WorldPosition,  amount);
		}
	}
}