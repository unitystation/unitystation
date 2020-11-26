using UnityEngine;

public class More_Buttons : MonoBehaviour
{
	public void Patr_btn()
	{
		Application.OpenURL("https://patreon.com/unitystation");
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
	}

	public void Webs_btn()
	{
		Application.OpenURL("https://unitystation.org");
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
	}

	public void Git_btn()
	{
		Application.OpenURL("https://github.com/unitystation/unitystation");
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
	}

	public void Reddit_btn()
	{
		Application.OpenURL("https://reddit.com/r/unitystation");
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
	}

	public void Discord_btn()
	{
		Application.OpenURL("https://discord.gg/tFcTpBp");
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
	}

	public void Issues_btn()
	{
		Application.OpenURL("https://github.com/unitystation/unitystation/issues");
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
	}
}