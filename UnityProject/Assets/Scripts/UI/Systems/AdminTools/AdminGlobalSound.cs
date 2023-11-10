using System.Globalization;
using AdminCommands;
using Logs;
using Messages.Server.SoundMessages;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

namespace AdminTools
{
	/// <summary>
	/// Lets Admins play sounds
	/// </summary>
	public class AdminGlobalSound : AdminGlobalAudio
	{
		[SerializeField] private Transform warningPage;
		[SerializeField] private Transform playSettingsPage;
		[SerializeField] private TMP_Text toPlayText;

		[BoxGroup("Paramter Settings"), SerializeField]
		private TMP_InputField pitch;
		[BoxGroup("Paramter Settings"), SerializeField]
		private TMP_InputField pan;
		[BoxGroup("Paramter Settings"), SerializeField]
		private TMP_InputField volume;
		[BoxGroup("Paramter Settings"), SerializeField]
		private TMP_InputField time;
		[BoxGroup("Paramter Settings"), SerializeField]
		private TMP_InputField minDistance;
		[BoxGroup("Paramter Settings"), SerializeField]
		private TMP_InputField maxDistance;
		[BoxGroup("Paramter Settings"), SerializeField]
		private TMP_InputField spatial;
		[BoxGroup("Paramter Settings"), SerializeField]
		private TMP_InputField blend;

		private string audioToPlay;

		public override void PlayAudio(string index) //send sound to audio manager
		{
			audioToPlay = index;
			toPlayText.text = index;
			warningPage.gameObject.SetActive(false);
			playSettingsPage.gameObject.SetActive(true);
		}

		private AudioSourceParameters GetSettings()
		{
			return new AudioSourceParameters
			{
				Volume = float.Parse(volume.text.Trim()),
				Pan = float.Parse(pan.text.Trim()),
				MinDistance = float.Parse(minDistance.text.Trim()),
				MaxDistance = float.Parse(maxDistance.text.Trim()),
				SpatialBlend = float.Parse(blend.text.Trim()),
				Pitch = float.Parse(pitch.text.Trim()),
				Time = float.Parse(time.text.Trim()),
				Spread = float.Parse(spatial.text.Trim())
			};
		}

		public void HandlePlayingAudioGlobal()
		{
			var settings = GetSettings();
			settings.MaxDistance = 9290000;
			AdminCommandsManager.Instance.CmdPlaySound(audioToPlay, settings, true);
		}

		public void HandlePlayingAudioAtAdminGhost()
		{
			AdminCommandsManager.Instance.CmdPlaySoundAtAdminGhost(audioToPlay, GetSettings(), false);
		}
	}
}
