using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace ConfigurationSaves
{
	//move to Core folder
	//Better name

	public enum AccessCategory
	{
		Config,
		Data,
		Logs
	}


	public static class AccessFile
	{
		private static string CashedForkName;

		private static string ForkName
		{
			get
			{
				if (string.IsNullOrEmpty(CashedForkName))
				{
					var path = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath,
						"Config",
						"buildinfo.json"));
					var text = File.ReadAllText(path);
					var data = JsonConvert.DeserializeObject<BuiltFork>(text);
					if (data == null)
					{
						CashedForkName = "Unitystation";
					}
					else
					{
						CashedForkName = data.ForkName;
					}
				}

				return CashedForkName;
			}
		}

		private static Dictionary<string, FileSystemWatcher> CurrentlyWatchingFile = new Dictionary<string, FileSystemWatcher>();

		private static Dictionary<Action, string> RegisteredToFile = new Dictionary<Action, string>();

		private static Dictionary<string, List<Action>> RegisteredToWatch = new Dictionary<string, List<Action>>();

		private class BuiltFork
		{
			public string ForkName; // = Unitystation"
		}


		private static readonly string[] AllowedExtensions = new[] {".txt", ".json", ".toml", ".yaml", ".data", ".log"};

		private static string ValidatePath(string relativePath, AccessCategory accessCategory, bool userPersistent,
			bool CreateFile, bool AddExtension = true)
		{
			bool isAllowedExtension = false;
			var extension = "";

			if (AddExtension)
			{
				foreach (var allowedExtension in AllowedExtensions)
				{
					if (relativePath.EndsWith(allowedExtension))
					{
						isAllowedExtension = true;
						break;
					}
				}

				if (isAllowedExtension == false)
				{
					switch (accessCategory)
					{
						case AccessCategory.Config:
							extension = ".txt";
							break;
						case AccessCategory.Data:
							extension = ".Data";
							break;
						case AccessCategory.Logs:
							extension = ".log";
							break;
					}
				}

			}

			string resolvedPath = "";

			if (userPersistent)
			{
				resolvedPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, ForkName , accessCategory.ToString(), relativePath + extension));
				if (resolvedPath.StartsWith(Path.GetFullPath(Path.Combine(Application.persistentDataPath, ForkName, accessCategory.ToString()))) == false)
				{
					Logger.LogError($"Malicious PATH was passed into File access, HEY NO! Stop being naughty with the PATH! {resolvedPath}");
					throw new Exception($"Malicious PATH was passed into File access, HEY NO! Stop being naughty with the PATH! {resolvedPath}");
				}
			}
			else
			{
				resolvedPath = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath,accessCategory.ToString(), relativePath + extension));
				if (resolvedPath.StartsWith(Path.GetFullPath(Path.Combine(Application.streamingAssetsPath,  accessCategory.ToString()))) == false)
				{
					Logger.LogError($"Malicious PATH was passed into File access, HEY NO! Stop being naughty with the PATH! {resolvedPath}");
					throw new Exception($"Malicious PATH was passed into File access, HEY NO! Stop being naughty with the PATH! {resolvedPath}");
				}
			}

			var ADirectory = Path.GetDirectoryName(resolvedPath);
			Directory.CreateDirectory(ADirectory);

			if (CreateFile)
			{
				// Check if the file already exists
				if (File.Exists(resolvedPath) == false)
				{
					// Create the file at the specified path
					File.Create(resolvedPath).Close();
				}
			}


			return resolvedPath;

		}


		public static void Save(string relativePath, string data, AccessCategory accessCategory = AccessCategory.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, true);
			File.WriteAllText(resolvedPath, data);
		}

		public static string Load(string relativePath, AccessCategory accessCategory = AccessCategory.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, true);
			var data = File.ReadAllText(resolvedPath);
			return data;
		}

		public static string[] ReadAllLines(string relativePath, AccessCategory accessCategory = AccessCategory.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, true);
			return File.ReadAllLines(resolvedPath);
		}

		public static void WriteAllLines(string relativePath, string[] lines, AccessCategory accessCategory = AccessCategory.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, true);
			File.WriteAllLines(resolvedPath, lines);
		}



		public static bool Exists(string relativePath, bool isFile = true, AccessCategory accessCategory = AccessCategory.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, false, isFile);

			if (isFile)
			{
				bool exists = File.Exists(resolvedPath);
				return exists;
			}
			else
			{
				bool exists = Directory.Exists(resolvedPath);
				return exists;
			}
		}
		public static string[] Contents(string relativePath, AccessCategory accessCategory = AccessCategory.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, false, false);
			var Directorys = new DirectoryInfo(resolvedPath);
			return Directorys.GetFiles().Select(x => x.Name.Replace(".txt", "")).Where(x => x.Contains(".meta") == false).ToArray();
		}


		public static void Delete(string relativePath, AccessCategory accessCategory = AccessCategory.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, true);
			File.Delete(resolvedPath);
		}


		public static void AppendAllText(string relativePath, string data , AccessCategory accessCategory = AccessCategory.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, true);

			File.AppendAllText(resolvedPath, data);
		}


		public static void Write(byte[] data, string relativePath, AccessCategory accessCategory = AccessCategory.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, true);

			byte[] DataSet = new byte[data.Length];
			Array.Copy(data, DataSet, data.Length);
			File.WriteAllBytes(resolvedPath, DataSet);
		}

		public static byte[] Read(string relativePath, AccessCategory accessCategory = AccessCategory.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, true);

			if (File.Exists(resolvedPath))
			{
				var data = File.ReadAllBytes(resolvedPath);
				byte[] dataSet = new byte[data.Length];
				Array.Copy(data, dataSet, data.Length);
				return dataSet;
			}
			else
			{
				Logger.LogError($"Unable to load Data {relativePath} It might be missing Used {resolvedPath} to try to find it ");
				return null;
			}
		}



		/// <summary>
		/// Ensure that the action your passing and doesn't get destroyed e.g the object owning the action destroyed
		/// </summary>
		/// <param name="relativePath"></param>
		/// <param name="toInvoke"></param>
		/// <param name="accessCategory"></param>
		/// <param name="userPersistent"></param>
		public static void Watch(string relativePath, Action toInvoke ,  AccessCategory accessCategory = AccessCategory.Config,
			bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, accessCategory, userPersistent, true);

			if (CurrentlyWatchingFile.ContainsKey(resolvedPath) == false)
			{
				var watcher =  new FileSystemWatcher(); //Witcher
				CurrentlyWatchingFile[resolvedPath] = watcher;
				watcher.Path = Path.GetDirectoryName(resolvedPath);
				watcher.Filter = Path.GetFileName(resolvedPath);
				watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
				watcher.Changed += (object source, FileSystemEventArgs e) => { FileChanged(resolvedPath); };
				watcher.EnableRaisingEvents = true;
			}

			if (RegisteredToWatch.ContainsKey(resolvedPath) == false)
			{
				RegisteredToWatch[resolvedPath] = new List<Action>();
			}
			RegisteredToWatch[resolvedPath].Add(toInvoke);
			RegisteredToFile[toInvoke] = resolvedPath;
		}


		private static void FileChanged(string Path)
		{
			foreach (var toInvoke in RegisteredToWatch[Path])
			{
				try
				{
					toInvoke.Invoke();
				}
				catch (Exception e)
				{
					Logger.LogError($"Exception when triggering file change for {Path}, Exception > " + e.ToString());
				}
			}
		}

		public static void UnRegister(Action toRemove)
		{
			if (RegisteredToFile.ContainsKey(toRemove))
			{
				var path = RegisteredToFile[toRemove];
				RegisteredToWatch[path].Remove(toRemove);
				RegisteredToFile.Remove(toRemove);
				if (RegisteredToWatch[path].Count == 0)
				{
					RegisteredToWatch.Remove(path);
					// Dispose the watcher
					CurrentlyWatchingFile[path].Dispose();
					CurrentlyWatchingFile.Remove(path);
				}
			}
		}

	}


}


