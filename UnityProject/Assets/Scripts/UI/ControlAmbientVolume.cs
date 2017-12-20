using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ControlAmbientVolume : MonoBehaviour
	{
		private Slider slider;

		private void OnEnable()
		{
			if (slider == null)
			{
				slider = GetComponent<Slider>();
			}
			slider.value = SoundManager.Instance.ambientTracks[SoundManager.Instance.ambientPlaying].volume;
		}

		public void VolumeChange()
		{
			SoundManager.Instance.ambientTracks[SoundManager.Instance.ambientPlaying].volume = slider.value;
			PlayerPrefs.SetFloat("AmbientVol", slider.value);
		}
	}
}