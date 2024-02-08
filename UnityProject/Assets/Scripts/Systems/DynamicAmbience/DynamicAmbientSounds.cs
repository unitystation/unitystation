using System;
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
		public float timeBetweenAmbience = 135f;

		private void Start()
		{
			if (CustomNetworkManager.IsHeadless) return;
			UpdateManager.Add(CheckForAmbienceToPlay, timeBetweenAmbience);
		}

		private void OnDestroy()
		{
			if (CustomNetworkManager.IsHeadless) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForAmbienceToPlay);
		}

		private void CheckForAmbienceToPlay()
		{
			if (root.Mind.NonImportantMind || root.isOwned == false) return;
			var traitsNearby = ComponentsTracker<Attributes>.GetNearbyTraits(root.gameObject, 6f, false);
			var configsToPlay = new List<AmbientClipsConfigSO>();
			AmbientClipsConfigSO highestPriority = null;
			foreach (var config in ambinceConfigs)
			{
				if (config.CanTrigger(traitsNearby, root.gameObject) == false) continue;
				configsToPlay.Add(config);
				if (highestPriority is null)
				{
					highestPriority = config;
					continue;
				}
				if (config.priority > highestPriority.priority) highestPriority = config;
			}
			var configChoosen = DMMath.Prob(80) && highestPriority is not null ? highestPriority : configsToPlay.PickRandom();
			configChoosen.OrNull()?.PlayRandomClipLocally();
		}
	}
}