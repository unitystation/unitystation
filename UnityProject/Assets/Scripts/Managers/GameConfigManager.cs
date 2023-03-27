using System;
using UnityEngine;
using System.IO;
using Shared.Managers;

namespace GameConfig
{
	/// <summary>
	/// Config for in game stuff
	/// </summary>
	public class GameConfigManager : SingletonManager<GameConfigManager>
	{
		private GameConfig config = new GameConfig();

		public static GameConfig GameConfig => Instance.config;

		public override void Awake()
		{
			base.Awake();

			//Load in awake so other scripts can get data in their start.
			AttemptConfigLoad();
		}

		private void AttemptConfigLoad()
		{
			var path = Path.Combine(Application.streamingAssetsPath, "config", "gameConfig.json");

			if (File.Exists(path))
			{
				config = JsonUtility.FromJson<GameConfig>(File.ReadAllText(path));
			}
			else
			{
				Logger.LogError("[GameConfigManager/AttemptConfigLoad] - No config file was found!! GameConfig will have null values!!");
			}
		}
	}

	[Serializable]
	public class GameConfig
	{
		public bool RandomEventsAllowed;
		public bool SpawnLavaLand;
		public int MinPlayersForCountdown;
		public int MinReadyPlayersForCountdown;
		public float PreRoundTime;
		public float RoundEndTime;
		public int RoundsPerMap;
		public string InitialGameMode = "Random";
		public bool RespawnAllowed = true;
		public int ShuttleDepartTime;
		public bool GibbingAllowed = true;
		public bool ShuttleGibbingAllowed = true;
		public bool AdminOnlyHtml;
		public int MalfAIRecieveTheirIntendedObjectiveChance;
		public int CharacterNameLimit = 64;
		public bool ServerShutsDownOnRoundEnd = false;
		public int PlayerLimit;
		public int LowPopLimit;
		public int LowPopCheckTimeAfterRoundStart;
		public int RebootOnAverageFPSOrLower;
	}
}