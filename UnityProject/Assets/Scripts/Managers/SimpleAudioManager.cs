using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Audio.Containers;
using Messages.Server;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Managers
{
	public class SimpleAudioManager : SingletonManager<SimpleAudioManager>
	{
		[SerializeField] private AudioSource globalSoundPlayer;

		public List<SimpleAudioData> LoadDataFromServer;
		private Dictionary<int, string> sharedData = new Dictionary<int, string>();
		private Dictionary<int, string> lobbyMusic = new Dictionary<int, string>();

		private string pathToFiles;
		private string pathToListJson;
		private string jsonFilePath;

		private AudioClip clip;
		private float clipPlayStartTime;

		public bool IsPlayingGlobally = false;

		public void Awake()
		{
			Instance = this;
			pathToFiles = Application.persistentDataPath + "\\Server\\DownloadedData\\Audio";
			pathToListJson = Application.persistentDataPath + "\\Server";
			jsonFilePath =  pathToListJson + "\\soundlist.json";
			EventManager.AddHandler(Event.ScenesLoadedServer, ServerUpdateListFromConfig); //When scenes finish loading, update sound list for server.
			EventManager.AddHandler(Event.PlayerRejoined, StopGlobalPlayer); //When player rejoins round, Stop lobby music
			EventManager.AddHandler(Event.PlayerSpawned, StopGlobalPlayer); //When player spawns, stop lobby music
			if (Directory.Exists(pathToFiles) == false)
			{
				Directory.CreateDirectory(pathToFiles);
			}
		}

		private void Update()
		{
			if (IsPlayingGlobally == false || (Time.time > clip.length + clipPlayStartTime) == false) return;
			clip = null;
			IsPlayingGlobally = false;
		}

		private IEnumerator LoadAudioIntoMemory(int id)
		{
			WWW request = new WWW(sharedData[id]);
			yield return request;
			clip = request.GetAudioClip();
		}

		public IEnumerator DownloadSounds()
		{
			foreach (var audioLink in LoadDataFromServer)
			{
				Logger.Log($"Downloading {audioLink.ID} - {audioLink.LinkToFile} - {audioLink.FileTitle}");
				if (IsMusicPathValid(audioLink.FileTitle) == false) continue;
				if (sharedData.ContainsKey(audioLink.ID)) continue;
				byte[] bytes = Array.Empty<byte>();
				using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(audioLink.LinkToFile, AudioType.MPEG))
				{
					yield return request.SendWebRequest();
					if (request.result != UnityWebRequest.Result.Success)
					{
						Logger.LogError("[SimpleAudioManager] - Error downloading music: " + request.error);
						continue;
					}

					bytes = request.downloadHandler.data;
				}

				var fileName = audioLink.ID.ToString() + audioLink.FileTitle;
				var filePath = Path.Combine(pathToFiles, fileName);
				Debug.Log(filePath);
				File.WriteAllBytes(filePath, bytes);
				sharedData.Add(audioLink.ID, filePath);
				if(audioLink.PlaysInLobby) lobbyMusic.Add(audioLink.ID, filePath);
			}

			//We assume that the player isn't ingame yet and we play lobby music immediately
			if (PlayerManager.LocalPlayerObject == null)
			{
				PlayRandomSongInLobby();
			}
		}

		private bool IsMusicPathValid(string path)
		{
			if (File.Exists(pathToFiles + "\\" + path))
			{
				Logger.LogWarning($"Music path already exists.. Skipping : {path}");
				return false;
			}
			if(path.Length > 254) //Windows character limit for filenames
			{
				Logger.LogWarning($"Music path is too long.. Skipping : {path}");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Plays a sound globally for all players. Uses the SimpleMusicPlayer object.
		/// </summary>
		/// <param name="idToPlay"></param>
		/// <returns></returns>
		private IEnumerator Play(int idToPlay)
		{
			yield return LoadAudioIntoMemory(idToPlay);
			if (clip == null)
			{
				Logger.LogError($"[SimpleAudioManager] - Could not load audio file with ID {idToPlay}");
				StartCoroutine(DownloadSounds());
				yield break;
			}
			globalSoundPlayer.clip = clip;
			globalSoundPlayer.volume = MusicManager.Instance.MusicVolume;
			globalSoundPlayer.Play();
			IsPlayingGlobally = true;
			clipPlayStartTime = Time.time;
		}

		//private IEnumerator Play(int idToPlay, Transform parent){}

		private void PlayRandomSongInLobby()
		{
			MusicManager.StopMusic();
			StartCoroutine(Play(lobbyMusic.PickRandom().Key));
		}

		public void StopGlobalPlayer()
		{
			globalSoundPlayer.Stop();
		}

		/// <summary>
		/// Server. Loads sounds from a config file located in the server data path.
		/// </summary>
		private void ServerUpdateListFromConfig()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if (File.Exists(jsonFilePath) == false)
			{
				Logger.Log("[SimpleAudioManager] - No sound list found on server.");
				GenerateSimpleAudioDataExample();
				return;
			}

			List<SimpleAudioData> localJson = new List<SimpleAudioData>();
			using (StreamReader r = new StreamReader(jsonFilePath))
			{
				string json = r.ReadToEnd();
				localJson = JsonConvert.DeserializeObject<List<SimpleAudioData>>(json);
			}
			if(localJson == null || localJson.Count == 0) return;
			foreach (var data in localJson)
			{
				if (!string.IsNullOrEmpty(data.LinkToFile) && !string.IsNullOrEmpty(data.FileTitle) &&
				    data.ID > 0) continue;
				Logger.LogError("[SimpleAudioManager] - One of the entries in the audio json for the server is not set up properly! Not going to update the list..");
				return;
			}
			LoadDataFromServer = localJson;
			ServerSimpleAudioManagerListMessage.Send();
		}

		/// <summary>
		/// Generates an example json file for server owners to fill.
		/// </summary>
		private void GenerateSimpleAudioDataExample()
		{
			List<SimpleAudioData> exampleJson = new List<SimpleAudioData>();
			SimpleAudioData exmapleOne = new SimpleAudioData();
			SimpleAudioData exampleTwo = new SimpleAudioData();
			exmapleOne.FileTitle = "Something.mp3";
			exampleTwo.FileTitle = "Audio.wav";
			exmapleOne.LinkToFile =
				"https://website.com/linkToYourSong.mp3";
			exampleTwo.LinkToFile =
				"localhost:7776\\pathToMusic\\Audio.mp3";
			exampleTwo.IsMusic = true;
			exmapleOne.IsMusic = true;
			exmapleOne.PlaysInLobby = true;
			exampleTwo.PlaysInLobby = false;
			exampleJson.Add(exmapleOne);
			exampleJson.Add(exampleTwo);
			var file = JsonConvert.SerializeObject(exampleJson);
			using StreamWriter fileWriter = new(jsonFilePath, append: true);
			fileWriter.Write(file);
		}

		/// <summary>
		/// Lets you add new sounds to the soundList.json from inside the game.
		/// </summary>
		/// <param name="newData"></param>
		public void ServerAddNewSound(SimpleAudioData newData)
		{
			LoadDataFromServer.Add(newData);
			var file = JsonConvert.SerializeObject(LoadDataFromServer);
			using StreamWriter fileWriter = new(jsonFilePath, append: true);
			fileWriter.Write(file);
			ServerSimpleAudioManagerListMessage.Send();
		}

		public struct SimpleAudioData
		{
			public string FileTitle; //What will the file be named when downloaded?
			public string LinkToFile; //The link that will be used to download the audio
			public int ID; //The ID that is used to tell all clients what song to play
			public bool IsMusic; //Is this music or sound effects? For admin tools. False for SFX, True for music.
			public bool PlaysInLobby; //Will this automatically be played in the lobby?
		}
	}
}

