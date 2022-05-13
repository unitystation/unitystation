using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using UnityEngine;

namespace Managers.LobbyManager.CustomMusic
{
	public class CustomMusicManager : NetworkBehaviour
	{
		public bool AllowCustomMusicFromServer = true;
		public string ServerDataPath = "\\Server\\Music";
		[HideInInspector] public List<string> ServerCachedMusicPaths = new List<string>();
		[HideInInspector] public List<string> CustomPlayerMusicPaths = new List<string>();
		private SyncList<string> AudioLinks = new SyncList<string>();
		private string finalPath;

		private AudioClip audioClip;

		private void OnEnable()
		{
			finalPath = Path.Combine(Application.persistentDataPath, ServerDataPath);
			if (CustomNetworkManager.IsServer)
			{
				ServerSetup();
				return;
			}
			ServerCachedMusicPaths = GetAllMusicPaths();
		}

		private void ServerSetup()
		{
			AudioLinks.AddRange(GameManager.Instance.ServerCustomMusic);
		}

		private IEnumerator DownloadMusic()
		{
			foreach (var audioLink in AudioLinks)
			{
				if (IsMusicPathValid(audioLink))
				{
					continue;
				}

				var www = new WWW(audioLink);
				yield return www;

				if (www.error != null)
				{
					Logger.LogError("Error downloading music: " + www.error);
					continue;
				}


				var fileName = Path.GetFileName(audioLink);
				var filePath = Path.Combine(finalPath, fileName);
				File.WriteAllBytes(filePath, www.bytes);
				CustomPlayerMusicPaths.Add(filePath);
			}
		}

		private bool IsMusicPathValid(string path)
		{
			if (File.Exists(finalPath + "\\" + path))
			{
				Logger.LogWarning($"Music path already exists.. Skipping : {path}");
				return false;
			}
			if(path.Length > 254) //Windows character limit for filenames
			{
				Logger.LogWarning($"Music path is too long.. Skipping : {path}");
				return false;
			}
			if (path.Contains(".wav") == false || path.Contains(".mp3") == false)
			{
				Logger.LogError($"Music files can only be .wav or .mp3.. Skipping : {path}");
				return false;
			}

			return true;
		}

		private List<string> GetAllMusicPaths()
		{
			if (Directory.Exists(finalPath)) return Directory.GetFiles(finalPath).ToList();
			Directory.CreateDirectory(finalPath);
			return null;
		}

		public WWW GetAudioFromFile(string path, string filename)
		{
			string audioToLoad = string.Format(path + "{0}", filename);
			WWW request = new WWW(audioToLoad);
			return request;
		}
	}
}