using Shared.Managers;

namespace US13.Managers.SettingsManager
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
			UnityEngine.PlayerPrefs.SetInt("streamerModeEnabled", value ? 1 : 0);
		}

		private void SetupPrefs()
		{
			if (!UnityEngine.PlayerPrefs.HasKey("streamerModeEnabled"))
			{
				UnityEngine.PlayerPrefs.SetInt("streamerModeEnabled", 0);
				return;
			}

			StreamerModeEnabled = UnityEngine.PlayerPrefs.GetInt("streamerModeEnabled") == 1;
		}
	}
}