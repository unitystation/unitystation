using UnityEngine;
using UnityEngine.UI;
using US13.Managers.SettingsManager;

namespace US13.UI.Core.OptionsMenu
{
	public class MiscOptions: MonoBehaviour
	{
		[SerializeField] private Toggle streamerMode = null;

		private void OnEnable()
		{
			streamerMode.isOn = MiscSettings.Instance.StreamerModeEnabled;
		}

		public void SetStreamerMode()
		{
			MiscSettings.Instance.StreamerModeEnabled = streamerMode.isOn;
		}

	}
}