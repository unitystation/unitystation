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
			if (root.OrNull()?.Mind is null) return;
			if (root.Mind.NonImportantMind || root.isOwned == false) return;
			var traitsNearby = ComponentsTracker<Attributes>.GetNearbyTraits(root.gameObject, 6f, false);
			AmbientClipsConfigSO highestPriority = null;
			var configsToPlay = GetConfigsToPlay(traitsNearby, ref highestPriority);
			if (configsToPlay.Count == 0) return;
			var configChosen = DMMath.Prob(80) && highestPriority is not null ? highestPriority : configsToPlay.PickRandom();
			configChosen.PlayRandomClipLocally();
		}

		private List<AmbientClipsConfigSO> GetConfigsToPlay(List<ItemTrait> traitsNearby, ref AmbientClipsConfigSO highestPriority)
		{
			var configsToPlay = new List<AmbientClipsConfigSO>();
			foreach (var config in ambinceConfigs.Where(config => config.CanTrigger(traitsNearby, root.gameObject)))
			{
				configsToPlay.Add(config);
				if (highestPriority is null)
				{
					highestPriority = config;
					continue;
				}
				if (config.priority > highestPriority.priority) highestPriority = config;
			}
			return configsToPlay;
		}
	}
}