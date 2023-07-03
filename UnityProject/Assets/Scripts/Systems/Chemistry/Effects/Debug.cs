using UnityEngine;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/Debug")]
	public class Debug : Chemistry.Effect
	{
		public override void Apply(GameObject sender, float amount)
		{
			Logger.LogFormat("Effect called, Sender: {0}, amount {1}", Category.Chemistry, sender, amount);
		}

		public override void HeatExposure(GameObject sender, float heat, ReagentMix inMix)
		{

		}
	}
}