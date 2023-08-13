using System;
using System.IO;
using SecureStuff;
using Newtonsoft.Json;
using UnityEngine;
using Shared.Managers;

namespace GameConfig
{
	/// <summary>
	/// Config for in game stuff
	/// </summary>
	public class GameConfigManager : SingletonManager<GameConfigManager>
	{
		private GameConfig config;

		public static GameConfig GameConfig => Instance.config;

		public override void Awake()
		{
			base.Awake();

			//Load in awake so other scripts can get data in their start.
			AttemptConfigLoad();
		}

		private void AttemptConfigLoad()
		{
			var path = "gameConfig.json";

			if (AccessFile.Exists(path))
			{
				config = JsonConvert.DeserializeObject<GameConfig>(AccessFile.Load(path));
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
		public string InitialGameMode;
		public bool RespawnAllowed;
		public int ShuttleDepartTime;
		public bool GibbingAllowed;
		public bool ShuttleGibbingAllowed;
		public bool AdminOnlyHtml;
		public int MalfAIRecieveTheirIntendedObjectiveChance;
		public int CharacterNameLimit;
		public bool ServerShutsDownOnRoundEnd;
		public int PlayerLimit;
		public int LowPopLimit;
		public int LowPopCheckTimeAfterRoundStart;
		public int RebootOnAverageFPSOrLower;
	}
}