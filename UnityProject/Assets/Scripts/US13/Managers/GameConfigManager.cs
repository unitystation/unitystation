using System;
using System.Collections.Generic;
using System.Text;
using Logs;
using Newtonsoft.Json;
using SecureStuff;
using Shared.Managers;
using US13.Messages.Server.JoinedViewer;

namespace US13.Managers
{
	/// <summary>
	/// Config for in-game stuff
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

			if (AccessFile.Exists(path) == false)
			{
				Loggy.Warning($"Game config file not found at {path}. Using default values.");
				return;
			}

			var raw = AccessFile.Load(path);

			try
			{
				var values = new Dictionary<string, object>();

				using (var reader = new JsonTextReader(new System.IO.StringReader(raw)))
				{
					ReadConfig(reader, values);
				}

				var json = JsonConvert.SerializeObject(values);
				config = JsonConvert.DeserializeObject<GameConfig>(json);
			}
			catch (JsonReaderException ex)
			{
				Loggy.Error("Game config file is not valid JSON. Using default values.\n " + ex.Message);
				config = new GameConfig();
			}

			Loggy.Info($"gameConfig.json loaded.\n {GameConfig.ToString()}");
		}

		private void ReadConfig(JsonTextReader reader, Dictionary<string, object> values)
		{
			while (reader.Read())
			{
				if (reader.TokenType != JsonToken.PropertyName)
					continue;

				string name = reader.Value.ToString();

				reader.Read();

				if (reader.TokenType == JsonToken.StartObject)
				{
					// Category found, read inside it
					ReadConfig(reader, values);
				}
				else
				{
					values[name] = reader.Value;
				}

				if (reader.TokenType == JsonToken.EndObject)
					break;
			}
		}

		public void SetVariable(string targetVariable, object value)
		{
			if (AllowedReflection.SetVariable(targetVariable, value, GameConfig))
			{
				Loggy.Info($"Game config variable '{targetVariable}' set to '{value}'.");
				ServerUpdateGameConfigForClients();
			}
			else
			{
				Loggy.Warning($"Game config variable '{targetVariable}' with value of type {value.GetType()} not found or is read-only.\n{value.ToString()}");
			}
		}

		public void ServerUpdateGameConfigForClients()
		{
			string seralizedConfig = GameConfig.ToJson();
			UpdateServerGameConfigForAll.SendToAll(seralizedConfig);

		}

		public void SetGameConfig(GameConfig newConfig)
		{
			config = newConfig;
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
		public bool AllowExtendedGameMode;
		public bool ForceExtendedGameMode;
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
		public float ExplosionStepTimeInSeconds = 0.14f;
		public float MinimumThrustStrengthToKnockdownPlayers = 0.85f;

		//physics
		public float ObjectBouncynessMultiplier = 1f;
		public float FrictionMultiplier = 1f;
		public float SlideFrictionMultiplier = 1f;
		public float MaximumTimeSpentFlying = 90f;

		//how many rounds of logs Should be stored before they get deleted,  null = 100, -1 Do not delete (will lag admin log UI After a while so manage yourself)
		//= n The number you want to keep
		public int? NumberOfLogsToStore;

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			var self = GetType();
			foreach (var field in self.GetFields())
			{
				sb.AppendLine($"{field.Name} = {field.GetValue(this)}");
			}

			foreach (var property in self.GetProperties())
			{
				sb.AppendLine($"{property.Name} = {property.GetValue(this)}");
			}
			return sb.ToString();
		}

		public string ToJson()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}