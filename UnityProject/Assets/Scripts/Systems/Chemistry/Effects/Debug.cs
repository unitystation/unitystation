using Logs;
using UnityEngine;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/Debug")]
	public class Debug : Chemistry.Effect
	{
		public override void Apply(MonoBehaviour sender, float amount)
		{
			Loggy.LogFormat("Effect called, Sender: {0}, amount {1}", Category.Chemistry, sender, amount);
		}
	}
}