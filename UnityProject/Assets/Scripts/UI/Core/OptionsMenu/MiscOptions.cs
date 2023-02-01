using Managers.SettingsManager;
using UnityEngine;
using UnityEngine.UI;

namespace Unitystation.Options
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