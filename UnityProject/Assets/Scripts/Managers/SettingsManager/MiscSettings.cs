using Shared.Managers;
using UnityEngine;

namespace Managers.SettingsManager
{
	public class MiscSettings: SingletonManager<MiscSettings>
	{
		private bool streamerModeEnabled = false;
		public bool StreamerModeEnabled
		{
			get => streamerModeEnabled;
			set => SetStreamerMode(value);
		}

		public override void Awake()
		{
			base.Awake();
			SetupPrefs();
		}

		private void SetStreamerMode(bool value)
		{
			streamerModeEnabled = value;
			PlayerPrefs.SetInt("streamerModeEnabled", value ? 1 : 0);
		}

		private void SetupPrefs()
		{
			if (!PlayerPrefs.HasKey("streamerModeEnabled"))
			{
				PlayerPrefs.SetInt("streamerModeEnabled", 0);
				return;
			}

			StreamerModeEnabled = PlayerPrefs.GetInt("streamerModeEnabled") == 1;
		}
	}
}