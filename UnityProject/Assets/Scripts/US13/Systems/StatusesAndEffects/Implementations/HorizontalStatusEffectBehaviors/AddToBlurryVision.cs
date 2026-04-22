using Logs;
using UnityEngine;
using US13.Items.Implants.Organs;
using US13.Player;
using US13.Systems.StatusesAndEffects.Interfaces;
using Util;

namespace US13.Systems.StatusesAndEffects.Implementations.HorizontalStatusEffectBehaviors
{
	public class AddToBlurryVision : ICustomStatusEffectBehavior
	{
		public int BlurrinessToAdd = 1;

		public void ExtendedOnAdded(GameObject target)
		{
			if (target.TryGetCachedComponent<PlayerScript>(out var player) == false) return;
			var eyes = player.playerHealth.GetBodyFunctionsOfType<Eye>();
			foreach (var eye in eyes)
			{
				Loggy.Info($"Adding {eye.BadEyesight + BlurrinessToAdd} to {eye.name}");
				eye.BadEyesight = BlurrinessToAdd;
			}
		}

		public void ExtendedOnRemoved(GameObject target)
		{
			if (target.TryGetCachedComponent<PlayerScript>(out var player) == false) return;
			var eyes = player.playerHealth.GetBodyFunctionsOfType<Eye>();
			foreach (var eye in eyes)
			{
				eye.BadEyesight = 0;
			}
		}

		// nothing to do for now.
		public void ExtendedDoEffect(GameObject target)
		{
			return;
		}

		// nothing to do for now.
		public void ExtendedDoEffectTick(GameObject target)
		{
			return;
		}
	}
}