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

	public void StartProfile(int frameCount)
	{
		if (runningProfile || runningMemoryProfile) return;
		if (frameCount > 300)
			frameCount = 300;

		runningProfile = true;

		Directory.CreateDirectory("Profiles");
		Profiler.SetAreaEnabled(ProfilerArea.Memory, true);
		Profiler.logFile = "Profiles/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
		Profiler.enableBinaryLog = true;
		Profiler.enableAllocationCallstacks = true;
		Profiler.maxUsedMemory = 1000000000; //1GB
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
		Profiler.enableBinaryLog = false;
		Profiler.enableAllocationCallstacks = false;
		Profiler.logFile = "";

		UpdateManager.Instance.Profile = false;

		if (CustomNetworkManager.IsServer)
		{
			ProfileMessage.SendToApplicable();
		}
	}

	public void RunMemoryProfile(bool full = true)
	{
		if (runningMemoryProfile || runningProfile) return;
		runningMemoryProfile = true;

		UpdateManager.Instance.Profile = true;

		Directory.CreateDirectory("Profiles");

		if (full)
		{
			MemoryProfiler.TakeSnapshot($"Profiles/FullMemoryProfile{DateTime.Now:yyyy-MM-dd HH-mm-ss}.snap", MemoryProfileEnd,
				CaptureFlags.ManagedObjects | CaptureFlags.NativeAllocations | CaptureFlags.NativeObjects |
				CaptureFlags.NativeAllocationSites |  CaptureFlags.NativeStackTraces);

			return;
		}

		MemoryProfiler.TakeSnapshot($"Profiles/ManagedMemoryProfile{DateTime.Now:yyyy-MM-dd HH-mm-ss}.snap", MemoryProfileEnd,
			CaptureFlags.ManagedObjects | CaptureFlags.NativeAllocations | CaptureFlags.NativeAllocationSites
			|  CaptureFlags.NativeStackTraces);
	}

	private void MemoryProfileEnd(string t, bool b)
	{
		runningMemoryProfile = false;
		UpdateManager.Instance.Profile = false;
	}
}
