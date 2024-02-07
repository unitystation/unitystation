using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Systems.DynamicAmbience
{
	public class DynamicAmbientSounds : MonoBehaviour
	{
		public GameObject root;
		public List<AmbientClipsConfigSO> ambinceConfigs = new List<AmbientClipsConfigSO>();

		private void Awake()
		{
			if (CustomNetworkManager.IsHeadless) return;
			if (transform.parent.gameObject.NetWorkIdentity()?.isOwned == false) return;
			UpdateManager.Add(CheckForAmbienceToPlay, 15f);
		}

		private void CheckForAmbienceToPlay()
		{
			var traitsNearby = ComponentsTracker<Attributes>.GetNearbyTraits(root, 6f);
			var configsToPlay = new List<AmbientClipsConfigSO>();
			foreach (var config in ambinceConfigs)
			{
				if(config.CanTrigger(traitsNearby)) configsToPlay.Add(config);
			}
			configsToPlay.PickRandom()?.PlayRandomClipLocally();
		}
	}
}