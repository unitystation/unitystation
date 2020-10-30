using System;
using UnityEngine;

public class More_Buttons : MonoBehaviour
{
	public void Patr_btn()
	{
		Application.OpenURL("https://patreon.com/unitystation");
		// JESTE_R
		SoundManager.Play(SingletonSOSounds.Instance.Click01, string.Empty);
	}

	public void Webs_btn()
	{
		Application.OpenURL("https://unitystation.org");
		// JESTE_R
		SoundManager.Play(SingletonSOSounds.Instance.Click01, string.Empty);
	}

	public void Git_btn()
	{
		Application.OpenURL("https://github.com/unitystation/unitystation");
		// JESTE_R
		SoundManager.Play(SingletonSOSounds.Instance.Click01, string.Empty);
	}

	public void Reddit_btn()
	{
		Application.OpenURL("https://reddit.com/r/unitystation");
		// JESTE_R
		SoundManager.Play(SingletonSOSounds.Instance.Click01, string.Empty);
	}

	public void Discord_btn()
	{
		Application.OpenURL("https://discord.gg/tFcTpBp");
		// JESTE_R
		SoundManager.Play(SingletonSOSounds.Instance.Click01, string.Empty);
	}

	public void Issues_btn()
	{
		Application.OpenURL("https://github.com/unitystation/unitystation/issues");
		// JESTE_R
		SoundManager.Play(SingletonSOSounds.Instance.Click01, string.Empty);
	}
}