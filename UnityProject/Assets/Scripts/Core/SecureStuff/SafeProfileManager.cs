using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Profiling;

namespace SecureStuff
{
	public sealed class SafeProfileManager : MonoBehaviour
	{
		private static SafeProfileManager _safeProfileManager;
		public static SafeProfileManager Instance => LazyFindObject(ref _safeProfileManager);

		private static T LazyFindObject<T>(ref T obj, bool includeInactive = false) where T : UnityEngine.Object
		{
			if (obj == null) obj = UnityEngine.Object.FindObjectOfType<T>(includeInactive);
			return obj;
		}

		public static event Action ProfileBegin;
		public static event Action ProfileEnd;

		private void Awake()
		{
			if (_safeProfileManager == null)
			{
				_safeProfileManager = this;
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

			ProfileBegin?.Invoke();


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

			ProfileEnd?.Invoke();
		}

		public void RunMemoryProfile(bool full = true)
		{
			if (runningMemoryProfile || runningProfile) return;
			runningMemoryProfile = true;

			ProfileBegin?.Invoke();

			Directory.CreateDirectory("Profiles");

			if (full)
			{
				Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot($"Profiles/FullMemoryProfile{DateTime.Now:yyyy-MM-dd HH-mm-ss}.snap",
					MemoryProfileEnd,
					Unity.Profiling.Memory.CaptureFlags.ManagedObjects | Unity.Profiling.Memory.CaptureFlags.NativeAllocations | Unity.Profiling.Memory.CaptureFlags.NativeObjects |
					Unity.Profiling.Memory.CaptureFlags.NativeAllocationSites | Unity.Profiling.Memory.CaptureFlags.NativeStackTraces);

				return;
			}

			Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot($"Profiles/ManagedMemoryProfile{DateTime.Now:yyyy-MM-dd HH-mm-ss}.snap",
				MemoryProfileEnd,
				Unity.Profiling.Memory.CaptureFlags.ManagedObjects | Unity.Profiling.Memory.CaptureFlags.NativeAllocations | Unity.Profiling.Memory.CaptureFlags.NativeAllocationSites
				| Unity.Profiling.Memory.CaptureFlags.NativeStackTraces);
		}

		private void MemoryProfileEnd(string t, bool b)
		{
			runningMemoryProfile = false;
			ProfileEnd?.Invoke();
		}

		public List<ProfileEntryData> GetCurrentProfiles()
		{
			var profileList = new List<ProfileEntryData>();
			var info = new DirectoryInfo("Profiles");

			if (!info.Exists)
				return profileList;

			var fileInfo = info.GetFiles();
			foreach (var file in fileInfo)
			{
				var entry = new ProfileEntryData();
				entry.Name = file.Name;
				var size = (float) file.Length / 1048576; // 1048576 = 1024 * 1024
				entry.Size = System.Math.Round(size, 2) + " MB";
				profileList.Add(entry);
			}

			return profileList;
		}

		public void RemoveProfile(string profileName)
		{
			var Current = GetCurrentProfiles();
			var profile = Current.FirstOrDefault(x => x.Name == profileName);
			if (profile == null) return;

			string path = Directory.GetCurrentDirectory() + "/Profiles/" + profile.Name;
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		[Serializable]
		public class ProfileEntryDataList //neded for json parsing
		{
			public List<ProfileEntryData> Profiles = new List<ProfileEntryData>();
		}

		[Serializable]
		public class ProfileEntryData
		{
			public string Name;
			public int ProfileIndex;
			public string Size;
		}
	}
}