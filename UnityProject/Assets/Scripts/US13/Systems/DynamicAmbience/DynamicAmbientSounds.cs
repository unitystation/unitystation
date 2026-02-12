using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Core;
using US13.Items;
using US13.Items.Traits;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Player;
using US13.Tilemaps.Behaviours.Layers;
using Util;

namespace US13.Systems.DynamicAmbience
{
	public class DynamicAmbientSounds : MonoBehaviour
	{
		public PlayerScript root;
		public List<AmbientClipsConfigSO> ambinceConfigs = new List<AmbientClipsConfigSO>();
		public float timeBetweenAmbience = 135f;

		private DateTime timeSinceLastMatrixAmbiencePlayed = DateTime.Now;
		private string soundToken = string.Empty;
		private string loopToken = string.Empty;

		private void Start()
		{
			if (CustomNetworkManager.IsHeadless) return;
			UpdateManager.Add(CheckForAmbienceToPlay, timeBetweenAmbience);
			root.playerMove.OnEnteredNewMatrix.AddListener(CheckAndPlayMatrixAmbience);
			root.OnBodyPossesedByPlayer.AddListener(CheckAndPlayMatrixAmbienceOnPosses);
			root.OnBodyUnPossesedByPlayer.AddListener(StopAllSounds);
		}

		private void OnDestroy()
		{
			if (CustomNetworkManager.IsHeadless) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForAmbienceToPlay);
			root.playerMove.OnEnteredNewMatrix.RemoveListener(CheckAndPlayMatrixAmbience);
			root.OnBodyPossesedByPlayer.RemoveListener(CheckAndPlayMatrixAmbienceOnPosses);
			root.OnBodyUnPossesedByPlayer.RemoveListener(StopAllSounds);
		}

		private void CheckForAmbienceToPlay()
		{
			if (timeSinceLastMatrixAmbiencePlayed + TimeSpan.FromSeconds(timeBetweenAmbience) / 2 > DateTime.Now) return;
			if (root.OrNull()?.Mind is null) return;
			if (root.Mind.NonImportantMind || root.isOwned == false) return;
			var traitsNearby = ComponentsTracker<Attributes>.GetNearbyTraits(root.gameObject, 6f, false);
			AmbientClipsConfigSO highestPriority = null;
			var configsToPlay = GetConfigsToPlay(traitsNearby, ref highestPriority);
			if (configsToPlay.Count == 0) return;
			var configChosen = DMMath.Prob(80) && highestPriority is not null ? highestPriority : configsToPlay.PickRandom();
			soundToken = configChosen.PlayRandomClipLocally();
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

		private void CheckAndPlayMatrixAmbience(Matrix enteredMatrix)
		{
			if (root.isOwned == false) return;
			if (enteredMatrix == null || enteredMatrix.EnteringSounds == null || enteredMatrix.EnteringSounds.AddressableAudioSource.Count == 0) return;
			timeSinceLastMatrixAmbiencePlayed = DateTime.Now;
			StopAllSounds();
			loopToken = Guid.NewGuid().ToString();
			_ = SoundManager.Play(enteredMatrix.EnteringSounds.GetRandomClip(), loopToken);
		}

		private void CheckAndPlayMatrixAmbienceOnPosses()
		{
			CheckAndPlayMatrixAmbience(root.playerMove.registerTile?.Matrix);
		}

		private void StopAllSounds()
		{
			SoundManager.ClientStop(soundToken, true);
			SoundManager.ClientStop(loopToken, true);
		}
	}
}