using System;
using Audio.Containers;
using Core.Sound;
using Logs;
using UnityEngine;

namespace Systems.DynamicAmbience
{
	public class DynamicReverb : MonoBehaviour
	{
		[SerializeField] private float updateTime = 0.75f;
		[SerializeField] private bool debug = false;

		private const string AUDIOMIXER_REVERB_KEY = "SFXReverb";
		private bool isEnabled = false;

		private void Start()
		{
			AudioManager.Instance.AudioReflectionsToggled += OnAudioReflectionsSettingToggled;
		}

		private void OnAudioReflectionsSettingToggled(bool value)
		{
			if (value)
			{
				EnableAmbienceForPlayer();
			}
			else
			{
				DisableAmbienceForPlayer();
			}
		}

		public void EnableAmbienceForPlayer()
		{
			if (CustomNetworkManager.IsHeadless) return;
			if (transform.parent.gameObject.NetWorkIdentity()?.isOwned == false || isEnabled) return;

			Loggy.Log("Enabling Dynamic Reverb system.");
			UpdateManager.Add(UpdateMe, updateTime);
			isEnabled = true;
		}

		public void DisableAmbienceForPlayer()
		{
			if (CustomNetworkManager.IsHeadless) return;
			if (transform.parent.gameObject.NetWorkIdentity()?.isOwned == false || isEnabled == false) return;

			Loggy.Log("Disabling Dynamic Reverb system.");
			AudioManager.Instance.GameplayMixer.audioMixer.ClearFloat(AUDIOMIXER_REVERB_KEY);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			isEnabled = false;
		}

		private void UpdateMe()
		{
			var roomSize = SoundPhysics.CalculateRoomSize(gameObject, debug);
			var strength = SoundPhysics.RoomSizeToReverbStrength[roomSize];
			AudioManager.Instance.GameplayMixer.audioMixer.SetFloat(AUDIOMIXER_REVERB_KEY, strength);
		}
	}
}