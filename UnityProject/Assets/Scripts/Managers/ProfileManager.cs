using System.Collections;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.Profiling;

public class ProfileManager : MonoBehaviour
{
	private static ProfileManager profileManager;
	public static ProfileManager Instance
	{
		get
		{
			if (!profileManager)
			{
				profileManager = FindObjectOfType<ProfileManager>();
			}
			return profileManager;
		}
	}

	public static bool runningProfile;
	public void StartProfile(int frameCount)
	{
		if (runningProfile) return;
		if (frameCount > 300)
			frameCount = 300;

		runningProfile = true;

		Directory.CreateDirectory("Profiles");
		Profiler.SetAreaEnabled(ProfilerArea.Memory, true);
		Profiler.logFile = "Profiles/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
		Profiler.enableBinaryLog = true;
		Profiler.enabled = true;

		UpdateManager.Instance.Profile = true;

		StartCoroutine(RunPorfile(frameCount));
	}

	private IEnumerator RunPorfile(int frameCount)
	{
		while (frameCount > 0)
		{
			frameCount--;
			yield return null;
		}

		runningProfile = false;
		Profiler.enabled = false;
		Profiler.enableBinaryLog = true;
		Profiler.logFile = "";

		UpdateManager.Instance.Profile = false;

		if (CustomNetworkManager.IsServer)
		{
			ProfileMessage.SendToApplicable();
		}
	}
}
