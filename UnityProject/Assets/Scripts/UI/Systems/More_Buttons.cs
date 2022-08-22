using UnityEngine;

namespace UI
{
	public class More_Buttons : MonoBehaviour
	{
		public void Patr_btn()
		{
			Application.OpenURL("https://patreon.com/unitystation");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void Webs_btn()
		{
			Application.OpenURL("https://unitystation.org");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void Git_btn()
		{
			Application.OpenURL("https://github.com/unitystation/unitystation");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void Reddit_btn()
		{
			Application.OpenURL("https://reddit.com/r/unitystation");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void Discord_btn()
		{
			Application.OpenURL("https://discord.gg/tFcTpBp");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void Issues_btn()
		{
			Application.OpenURL("https://github.com/unitystation/unitystation/issues");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}
	}
}
