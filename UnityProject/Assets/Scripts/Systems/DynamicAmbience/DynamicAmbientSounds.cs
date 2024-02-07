using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace Systems.DynamicAmbience
{
	public class DynamicAmbientSounds : MonoBehaviour
	{
		public PlayerScript root;
		public List<AmbientClipsConfigSO> ambinceConfigs = new List<AmbientClipsConfigSO>();

		private void Start()
		{
			if (CustomNetworkManager.IsHeadless) return;
			UpdateManager.Add(CheckForAmbienceToPlay, 15f);
		}

		private void CheckForAmbienceToPlay()
		{
			if (root.Mind.NonImportantMind) return;
			var traitsNearby = ComponentsTracker<Attributes>.GetNearbyTraits(root.gameObject, 6f, false);
			var configsToPlay = new List<AmbientClipsConfigSO>();
			var highestPriority = ambinceConfigs.First();
			foreach (var config in ambinceConfigs)
			{
				if (config.CanTrigger(traitsNearby, root.gameObject) == false) continue;
				configsToPlay.Add(config);
				if (config.priority > highestPriority.priority) highestPriority = config;
			}
			var configChoosen = DMMath.Prob(80) ? highestPriority : configsToPlay.PickRandom();
			configChoosen.OrNull()?.PlayRandomClipLocally();
		}
	}
}