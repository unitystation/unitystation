using System;
using UnityEngine;

public class SoundSpawn : MonoBehaviour
{
	public AudioSource audioSource;
	//We need to handle this manually to prevent multiple requests grabbing sound pool items in the same frame
	public bool isPlaying = false;
	private float waitLead = 0;
	private bool monitor = false;

	public void PlayOneShot()
	{
		if (audioSource == null) return;
		audioSource.PlayOneShot(audioSource.clip);
		WaitForPlayToFinish();
	}

	public void PlayNormally()
	{
		if (audioSource == null) return;
		audioSource.Play();
		WaitForPlayToFinish();
	}

	void WaitForPlayToFinish()
	{
		waitLead = 0f;
		monitor = true;
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	void UpdateMe()
	{
		if (!monitor || audioSource == null) return;

		waitLead += Time.deltaTime;
		if (waitLead > 0.2f)
		{
			if (!audioSource.isPlaying)
			{
				isPlaying = false;
				waitLead = 0f;
				monitor = false;
			}
		}
	}
}
