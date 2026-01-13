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
		public bool RandomEventsAllowed = true;
		public bool SpawnLavaLand = true;
		public int MinPlayersForCountdown = 1;
		public int MinReadyPlayersForCountdown = 1;
		public float PreRoundTime;
		public float RoundEndTime;
		public int RoundsPerMap;
		public string InitialGameMode;
		public bool RespawnAllowed = false;
		public int ShuttleDepartTime;
		public bool GibbingAllowed = true;
		public bool ShuttleGibbingAllowed = true;
		public bool AdminOnlyHtml  = true ;
		public int MalfAIRecieveTheirIntendedObjectiveChance;
		public int CharacterNameLimit = 35;
		public bool ServerShutsDownOnRoundEnd;
		public int PlayerLimit = 100;
		public int LowPopLimit = 25;
		public int LowPopCheckTimeAfterRoundStart = 300;
		public int RebootOnAverageFPSOrLower = 35;
		public string AccountAPIHost;

		//how many rounds of logs Should be stored before they get deleted,  null = 100, -1 Do not delete (will lag admin log UI After a while so manage yourself)
		//= n The number you want to keep
		public int? NumberOfLogsToStore;

	}
}