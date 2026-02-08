using SecureStuff;
using UnityEngine;
using US13.Core.Addressables;
using US13.Managers;

namespace US13.UI.Systems
{
	public class More_Buttons : MonoBehaviour
	{
		public void Patr_btn()
		{
			SafeURL.Open("https://patreon.com/unitystation");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void Webs_btn()
		{
			SafeURL.Open("https://unitystation.org");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void Git_btn()
		{
			SafeURL.Open("https://github.com/unitystation/unitystation");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void Reddit_btn()
		{
			SafeURL.Open("https://reddit.com/r/unitystation");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void Discord_btn()
		{
			SafeURL.Open("https://discord.gg/tFcTpBp");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void Issues_btn()
		{
			SafeURL.Open("https://github.com/unitystation/unitystation/issues");
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}
	}
}
