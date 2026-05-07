using Chemistry;
using UnityEngine;
using US13.Health.Objects;
using US13.Player;
namespace US13.Systems.ChemistryEffects
{
	[CreateAssetMenu(fileName = "effect", menuName = "ScriptableObjects/Chemistry/Effect/IgniteContainerEffect")]
	public class IgniteContainerEffect : Chemistry.Effect
	{
		[SerializeField] private float fireStacksPerUnit = 0.5f;

		public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix, Vector3 WorldPosition, float amount)
		{
			if (sender == false) return;
			if (sender.TryGetComponent<Flammable>(out var flammable))
			{
				flammable.AddFireStacks((int)(fireStacksPerUnit * amount));
				return;
			}

			if(sender.TryGetComponent<PlayerScript>(out var player) == false) return;
			player.playerHealth.ChangeFireStacks(fireStacksPerUnit * amount);
		}
	}
}
