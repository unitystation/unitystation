using System.Collections;
using UnityEngine;
using System;
using System.IO;
using Messages.Server.AdminTools;
using UnityEngine.Profiling;
using UnityEngine.Profiling.Memory.Experimental;

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

	private void Awake()
	{
		if (profileManager == null)
		{
			profileManager = this;
		}
	}

	public static bool runningProfile;
	public static bool runningMemoryProfile;
	private int profileRunCount = 0;

	public void StartProfile(int frameCount)
	{
		if (runningProfile || runningMemoryProfile) return;
		if (frameCount > 300)
			frameCount = 300;

		runningProfile = true;

		Directory.CreateDirectory("Profiles");
		Profiler.SetAreaEnabled(ProfilerArea.CPU, true);
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

	public void RunMemoryProfile()
	{
		if (runningMemoryProfile || runningProfile) return;
		runningMemoryProfile = true;

		UpdateManager.Instance.Profile = true;

		Directory.CreateDirectory("Profiles");

		profileRunCount++;
		MemoryProfiler.TakeSnapshot("Profiles/MemoryManaged" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".snap", MemoryProfileEnd, CaptureFlags.ManagedObjects);
		profileRunCount++;
		MemoryProfiler.TakeSnapshot("Profiles/MemoryNative" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")+ ".snap", MemoryProfileEnd, CaptureFlags.NativeObjects);
	}

	private void MemoryProfileEnd(string t, bool b)
	{
		profileRunCount--;
		if (profileRunCount == 0)
		{
			runningMemoryProfile = false;
			UpdateManager.Instance.Profile = false;
		}
	}
}
