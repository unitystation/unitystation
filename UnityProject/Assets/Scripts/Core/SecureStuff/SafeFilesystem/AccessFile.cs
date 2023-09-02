using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Logs;
using Newtonsoft.Json;
using UnityEngine;

namespace SecureStuff
{
	public enum FolderType
	{
		Config,
		Data,
		Logs,
		AddressableCatalogues
	}

	public static class AccessFile
	{
		private static string cashedForkName;

		public static string AdminFolder => "Admin";

		public static string ForkName
		{
			get
			{
				if (string.IsNullOrEmpty(cashedForkName) == false) return cashedForkName;
				var path = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath,
					FolderType.Config.ToString(),
					"buildinfo.json"));
				var text = File.ReadAllText(path);
				var data = JsonConvert.DeserializeObject<BuiltFork>(text);
				cashedForkName = data == null ? "Unitystation" : data.Name;

				return cashedForkName;
			}
		}

		private static readonly Dictionary<string, FileSystemWatcher> CurrentlyWatchingFile = new();

		private static readonly Dictionary<Action, string> RegisteredToFile = new();

		private static readonly Dictionary<string, List<Action>> RegisteredToWatch = new();

		[Serializable]
		private class BuiltFork
		{
			[JsonProperty("ForkName")]
			public string Name { get; set; } // = Unitystation"
		}

		private static readonly string[] AllowedExtensions = new[] {".txt", ".json", ".toml", ".yaml", ".data", ".log"};

		private static string ValidatePath(string relativePath, FolderType folderType, bool userPersistent,
			bool createFile, bool addExtension = true)
		{
			bool isAllowedExtension = false;
			var extension = "";

			if (addExtension)
			{
				foreach (var allowedExtension in AllowedExtensions)
				{
					if (relativePath.EndsWith(allowedExtension) == false) continue;
					isAllowedExtension = true;
					break;
				}

				if (isAllowedExtension == false)
				{
					extension = folderType switch
					{
						FolderType.Config => ".txt",
						FolderType.Data => ".Data",
						FolderType.Logs => ".log",
						_ => extension
					};
				}

			}

			string resolvedPath;

			if (userPersistent)
			{
				resolvedPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, ForkName , folderType.ToString(), relativePath + extension));
				if (resolvedPath.StartsWith(Path.GetFullPath(Path.Combine(Application.persistentDataPath, ForkName, folderType.ToString()))) == false)
				{
					Loggy.LogError($"Persistent data Malicious PATH was passed into File access, HEY NO! Stop being naughty with the PATH! {resolvedPath}");
					throw new Exception($"Persistent data  Malicious PATH was passed into File access, HEY NO! Stop being naughty with the PATH! {resolvedPath}");
				}
			}
			else
			{
				resolvedPath = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath,folderType.ToString(), relativePath + extension));
				if (resolvedPath.StartsWith(Path.GetFullPath(Path.Combine(Application.streamingAssetsPath,  folderType.ToString()))) == false)
				{
					Loggy.LogError($"Streaming assets Malicious PATH was passed into File access, HEY NO! Stop being naughty with the PATH! {resolvedPath}");
					throw new Exception($"Streaming assets Malicious PATH was passed into File access, HEY NO! Stop being naughty with the PATH! {resolvedPath}");
				}
			}

			var aDirectory = Path.GetDirectoryName(resolvedPath);
			if (aDirectory is not null)
			{
				Directory.CreateDirectory(aDirectory);
			}

			if (createFile)
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


		/// <summary>
		/// Saves the provided data as a string to a specified file path within a designated access category. The function ensures the path's validity and security before performing the save operation. The data can be saved either in a user-specific persistent data path or in the streaming assets path, based on the 'userPersistent' parameter.
		/// </summary>
		/// <param name="relativePath">The relative path to the file to be saved.</param>
		/// <param name="data">The data to be saved as a string.</param>
		/// <param name="folderType">The category of access for the file (e.g., Config, Data, Logs).</param>
		/// <param name="userPersistent">Indicates whether the file should be saved in a user-specific persistent data path (true) or in the streaming assets path (false).</param>
		/// <exception cref="Exception">Thrown when a malicious path is passed into the file access.</exception>
		public static void Save(string relativePath, string data, FolderType folderType = FolderType.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, true);
			File.WriteAllText(resolvedPath, data);
		}

		/// <summary>
		/// Loads data from a specified file path within a designated access category and returns it as a string. The function ensures the path's validity and security before performing the load operation. The data can be loaded from either a user-specific persistent data path or the streaming assets path, based on the 'userPersistent' parameter.
		/// </summary>
		/// <param name="relativePath">The relative path to the file from which the data will be loaded. The path should be relative to the base path of the chosen access category.</param>
		/// <param name="folderType">The category of access for the file, which helps determine the base path for the file. This category can be Config, Data, Logs, or other appropriate access categories (defined elsewhere in the code).</param>
		/// <param name="userPersistent">A flag indicating whether the file should be loaded from a user-specific persistent data path (true) or from the streaming assets path (false).</param>
		/// <returns>The data loaded from the specified file as a string.</returns>
		/// <exception cref="Exception">Thrown when a malicious path is passed into the file access. This security measure helps prevent unauthorized file access.</exception>
		/// <exception cref="FileNotFoundException">Thrown when the specified file is not found at the provided path.</exception>
		/// <exception cref="IOException">Thrown when an error occurs during the file reading operation.</exception>
		public static string Load(string relativePath, FolderType folderType = FolderType.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, true);
			var data = File.ReadAllText(resolvedPath);
			return data;
		}

		/// <summary>
		/// Reads all lines of text from a specified file path within a designated access category and returns them as an array of strings. The function ensures the path's validity and security before performing the read operation. The lines of text can be read from either a user-specific persistent data path or the streaming assets path, based on the 'userPersistent' parameter.
		/// </summary>
		/// <param name="relativePath">The relative path to the file from which the lines of text will be read. The path should be relative to the base path of the chosen access category.</param>
		/// <param name="folderType">The category of access for the file, which helps determine the base path for the file. This category can be Config, Data, Logs, or other appropriate access categories (defined elsewhere in the code).</param>
		/// <param name="userPersistent">A flag indicating whether the file should be read from a user-specific persistent data path (true) or from the streaming assets path (false).</param>
		/// <returns>An array of strings representing the lines of text read from the specified file.</returns>
		/// <exception cref="Exception">Thrown when a malicious path is passed into the file access. This security measure helps prevent unauthorized file access.</exception>
		/// <exception cref="FileNotFoundException">Thrown when the specified file is not found at the provided path.</exception>
		/// <exception cref="IOException">Thrown when an error occurs during the file reading operation.</exception>
		public static string[] ReadAllLines(string relativePath, FolderType folderType = FolderType.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, true);
			return File.ReadAllLines(resolvedPath);
		}

		/// <summary>
		/// Writes an array of strings as lines of text to a specified file path within a designated access category. The function ensures the path's validity and security before performing the write operation. The lines of text can be written to either a user-specific persistent data path or the streaming assets path, based on the 'userPersistent' parameter.
		/// </summary>
		/// <param name="relativePath">The relative path to the file where the lines of text will be written. The path should be relative to the base path of the chosen access category.</param>
		/// <param name="lines">An array of strings representing the lines of text to be written to the file.</param>
		/// <param name="folderType">The category of access for the file, which helps determine the base path for the file. This category can be Config, Data, Logs, or other appropriate access categories (defined elsewhere in the code).</param>
		/// <param name="userPersistent">A flag indicating whether the file should be written to a user-specific persistent data path (true) or to the streaming assets path (false).</param>
		/// <exception cref="Exception">Thrown when a malicious path is passed into the file access. This security measure helps prevent unauthorized file access.</exception>
		/// <exception cref="IOException">Thrown when an error occurs during the file writing operation.</exception>
		public static void WriteAllLines(string relativePath, string[] lines, FolderType folderType = FolderType.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, true);
			File.WriteAllLines(resolvedPath, lines);
		}


		/// <summary>
		/// Checks whether a specified file or directory exists within a designated access category and location. The function ensures the path's validity and security before performing the existence check. The existence can be checked either in a user-specific persistent data path or the streaming assets path, based on the 'userPersistent' parameter.
		/// </summary>
		/// <param name="relativePath">The relative path to the file or directory for which existence will be checked. The path should be relative to the base path of the chosen access category.</param>
		/// <param name="isFile">A flag indicating whether the existence check is for a file (true) or a directory (false). If true, the function checks for the existence of a file; if false, it checks for the existence of a directory.</param>
		/// <param name="folderType">The category of access for the file or directory, which helps determine the base path for the check. This category can be Config, Data, Logs, or other appropriate access categories (defined elsewhere in the code).</param>
		/// <param name="userPersistent">A flag indicating whether the existence check should be performed in a user-specific persistent data path (true) or in the streaming assets path (false).</param>
		/// <returns>True if the specified file or directory exists; otherwise, false.</returns>
		/// <exception cref="Exception">Thrown when a malicious path is passed into the file access. This security measure helps prevent unauthorized file access.</exception>
		public static bool Exists(string relativePath, bool isFile = true, FolderType folderType = FolderType.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, false, isFile);

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

		/// <summary>
		/// Retrieves the names of files contained within a specified directory path within a designated access category. The function ensures the path's validity and security before performing the operation. The contents are obtained from either a user-specific persistent data path or the streaming assets path, based on the 'userPersistent' parameter.
		/// </summary>
		/// <param name="relativePath">The relative path to the directory from which file names will be retrieved. The path should be relative to the base path of the chosen access category.</param>
		/// <param name="folderType">The category of access for the directory, which helps determine the base path for the retrieval. This category can be Config, Data, Logs, or other appropriate access categories (defined elsewhere in the code).</param>
		/// <param name="userPersistent">A flag indicating whether the file names should be retrieved from a user-specific persistent data path (true) or from the streaming assets path (false).</param>
		/// <param name="files">A flag indicating Whether it should look for files ( true ) or directories ( false ) in the specified directory </param>
		/// <returns>An array of strings containing the names of files within the specified directory, excluding files with '.meta' extensions and '.txt' extensions (if any).</returns>
		/// <exception cref="Exception">Thrown when a malicious path is passed into the file access. This security measure helps prevent unauthorized file access.</exception>
		public static string[] DirectoriesOrFilesIn(string relativePath, FolderType folderType = FolderType.Config, bool userPersistent = false, bool files = true)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, false, false);
			var directories = new DirectoryInfo(resolvedPath);
			if (files)
			{
				return directories.GetFiles().Select(x => x.Name).Where(x => x.Contains(".meta") == false).ToArray();
			}
			else
			{
				return directories.GetDirectories().Select(x => x.Name).Where(x => x.Contains(".meta") == false).ToArray();
			}

		}


		/// <summary>
		/// Deletes a specified file within a designated access category and location. The function ensures the path's validity and security before performing the deletion. The file can be deleted from either a user-specific persistent data path or the streaming assets path, based on the 'userPersistent' parameter.
		/// </summary>
		/// <param name="relativePath">The relative path to the file that will be deleted. The path should be relative to the base path of the chosen access category.</param>
		/// <param name="folderType">The category of access for the file, which helps determine the base path for the deletion. This category can be Config, Data, Logs, or other appropriate access categories (defined elsewhere in the code).</param>
		/// <param name="userPersistent">A flag indicating whether the file should be deleted from a user-specific persistent data path (true) or from the streaming assets path (false).</param>
		/// <exception cref="Exception">Thrown when a malicious path is passed into the file access. This security measure helps prevent unauthorized file access.</exception>
		/// <exception cref="FileNotFoundException">Thrown when the specified file is not found at the provided path.</exception>
		/// <exception cref="IOException">Thrown when an error occurs during the file deletion operation.</exception>
		public static void Delete(string relativePath, FolderType folderType = FolderType.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, true);
			File.Delete(resolvedPath);
		}


		/// <summary>
		/// Appends the specified data as text to a specified file within a designated access category and location. The function ensures the path's validity and security before performing the append operation. The data is appended to the file in either a user-specific persistent data path or the streaming assets path, based on the 'userPersistent' parameter.
		/// </summary>
		/// <param name="relativePath">The relative path to the file where the data will be appended as text. The path should be relative to the base path of the chosen access category.</param>
		/// <param name="data">The data to be appended as text to the file.</param>
		/// <param name="folderType">The category of access for the file, which helps determine the base path for the append operation. This category can be Config, Data, Logs, or other appropriate access categories (defined elsewhere in the code).</param>
		/// <param name="userPersistent">A flag indicating whether the data should be appended to a user-specific persistent data path (true) or to the streaming assets path (false).</param>
		/// <exception cref="Exception">Thrown when a malicious path is passed into the file access. This security measure helps prevent unauthorized file access.</exception>
		/// <exception cref="IOException">Thrown when an error occurs during the file append operation.</exception>
		public static void AppendAllText(string relativePath, string data , FolderType folderType = FolderType.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, true);
			File.AppendAllText(resolvedPath, data);
		}


		/// <summary>
		/// Writes binary data to a specified file within a designated access category and location. The function ensures the path's validity and security before performing the write operation. The data is written to the file in either a user-specific persistent data path or the streaming assets path, based on the 'userPersistent' parameter.
		/// </summary>
		/// <param name="data">The binary data to be written to the file.</param>
		/// <param name="relativePath">The relative path to the file where the binary data will be written. The path should be relative to the base path of the chosen access category.</param>
		/// <param name="folderType">The category of access for the file, which helps determine the base path for the write operation. This category can be Config, Data, Logs, or other appropriate access categories (defined elsewhere in the code).</param>
		/// <param name="userPersistent">A flag indicating whether the binary data should be written to a user-specific persistent data path (true) or to the streaming assets path (false).</param>
		/// <exception cref="Exception">Thrown when a malicious path is passed into the file access. This security measure helps prevent unauthorized file access.</exception>
		/// <exception cref="IOException">Thrown when an error occurs during the file writing operation.</exception>
		public static void Write(byte[] data, string relativePath, FolderType folderType = FolderType.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, true);

			byte[] dataSet = new byte[data.Length];
			Array.Copy(data, dataSet, data.Length);
			File.WriteAllBytes(resolvedPath, dataSet);
		}


		/// <summary>
		/// Reads binary data from a specified file within a designated access category and location. The function ensures the path's validity and security before performing the read operation. The binary data is read from either a user-specific persistent data path or the streaming assets path, based on the 'userPersistent' parameter.
		/// </summary>
		/// <param name="relativePath">The relative path to the file from which the binary data will be read. The path should be relative to the base path of the chosen access category.</param>
		/// <param name="folderType">The category of access for the file, which helps determine the base path for the read operation. This category can be Config, Data, Logs, or other appropriate access categories (defined elsewhere in the code).</param>
		/// <param name="userPersistent">A flag indicating whether the binary data should be read from a user-specific persistent data path (true) or from the streaming assets path (false).</param>
		/// <returns>A byte array containing the binary data read from the specified file. If the file does not exist, null is returned.</returns>
		/// <exception cref="Exception">Thrown when a malicious path is passed into the file access. This security measure helps prevent unauthorized file access.</exception>
		/// <exception cref="IOException">Thrown when an error occurs during the file reading operation.</exception>
		public static byte[] Read(string relativePath, FolderType folderType = FolderType.Config, bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, true);

			if (File.Exists(resolvedPath))
			{
				var data = File.ReadAllBytes(resolvedPath);
				byte[] dataSet = new byte[data.Length];
				Array.Copy(data, dataSet, data.Length);
				return dataSet;
			}
			else
			{
				Loggy.LogError($"Unable to load Data {relativePath} It might be missing Used {resolvedPath} to try to find it ");
				return null;
			}
		}

		/// <summary>
		/// Watches a file located at the specified relative path and invokes the given action when the file is modified.
		/// The file must reside in a designated access category folder (Config, Data, or Logs) and can be stored in either
		/// the persistent data path or the streaming assets path.
		/// Make sure that the action you are passing doesn't get destroyed, for example, if the object owning the action is destroyed.
		/// </summary>
		/// <param name="relativePath">The relative path to the file to be watched.</param>
		/// <param name="toInvoke">The action to be invoked when the file is modified.</param>
		/// <param name="folderType">The access category folder where the file is expected to reside (Config, Data, or Logs). Default is Config.</param>
		/// <param name="userPersistent">If true, the file is expected to be in the persistent data path; otherwise, it is expected in the streaming assets path.</param>

		public static void Watch(string relativePath, Action toInvoke ,  FolderType folderType = FolderType.Config,
			bool userPersistent = false)
		{
			var resolvedPath = ValidatePath(relativePath, folderType, userPersistent, true);

			if (CurrentlyWatchingFile.ContainsKey(resolvedPath) == false)
			{
				var watcher =  new FileSystemWatcher(); //Witcher
				CurrentlyWatchingFile[resolvedPath] = watcher;
				watcher.Path = Path.GetDirectoryName(resolvedPath);
				watcher.Filter = Path.GetFileName(resolvedPath);
				watcher.NotifyFilter = NotifyFilters.LastWrite;
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


		private static void FileChanged(string path)
		{
			foreach (var toInvoke in RegisteredToWatch[path])
			{
				try
				{
					toInvoke.Invoke();
				}
				catch (Exception e)
				{
					Loggy.LogError($"Exception when triggering file change for {path}, Exception > " + e.ToString());
				}
			}
		}

		/// <summary>
		/// Unregisters an action from the file watcher. This ensures that the specified action will no longer be invoked when the corresponding file is modified.
		/// </summary>
		/// <param name="toRemove">The action to be unregistered from the file watcher.</param>
		public static void UnRegister(Action toRemove)
		{
			if (RegisteredToFile.ContainsKey(toRemove) == false) return;

			var path = RegisteredToFile[toRemove];
			RegisteredToWatch[path].Remove(toRemove);
			RegisteredToFile.Remove(toRemove);
			if (RegisteredToWatch[path].Count != 0) return;

			RegisteredToWatch.Remove(path);
			// Dispose the watcher
			CurrentlyWatchingFile[path].Dispose();
			CurrentlyWatchingFile.Remove(path);
		}

	}
}


