using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundSpawn : MonoBehaviour
{
	public AudioSource audioSource;
	//We need to handle this manually to prevent multiple requests grabbing sound pool items in the same frame
	public bool isPlaying = false;
	private float waitLead = 0;

	public void PlayOneShot()
	{
		audioSource.PlayOneShot(audioSource.clip);
		WaitForPlayToFinish();
	}

	public void PlayNormally()
	{
		audioSource.Play();
		WaitForPlayToFinish();
	}

	void WaitForPlayToFinish()
	{
		waitLead = 0f;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	void UpdateMe()
	{
		waitLead += Time.deltaTime;
		if (waitLead > 0.2f)
		{
			if (!audioSource.isPlaying)
			{
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
				isPlaying = false;
				waitLead = 0f;
			}
		}
	}
}
