using System;
using Logs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecureStuff;
using Shared.Managers;

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
				//Support both flat and categorized config files. If the top-level contains objects (categories),
				//merge their properties into a single JObject before deserializing into GameConfig.
				var jobj = JObject.Parse(raw);
				var merged = new JObject();
				foreach (var prop in jobj.Properties())
				{
					if (prop.Value.Type == JTokenType.Object)
					{
						foreach (var child in prop.Value.Children<JProperty>())
						{
							merged[child.Name] = child.Value;
						}
					}
					else
					{
						merged[prop.Name] = prop.Value;
					}
				}

				config = merged.ToObject<GameConfig>();
			}
			catch (JsonReaderException ex)
			{
				Loggy.Error("Game config file is not valid JSON. Using default values.\n " + ex.Message);
				config = JsonConvert.DeserializeObject<GameConfig>(raw);
			}
		}

		public void SetVariable(string targetVariable, object value)
		{
			var gameConfigType = typeof(GameConfig);

			var property = gameConfigType.GetProperty(targetVariable);
			if (property != null && property.CanWrite)
			{
				property.SetValue(config, Convert.ChangeType(value, property.PropertyType));
				Loggy.Info($"Game config variable '{targetVariable}' set to '{value}'.");
				return;
			}

			var field = gameConfigType.GetField(targetVariable);
			if (field != null)
			{
				field.SetValue(config, Convert.ChangeType(value, field.FieldType));
				Loggy.Info($"Game config variable '{targetVariable}' set to '{value}'.");
				return;
			}

			Loggy.Warning($"Game config variable '{targetVariable}' not found or is read-only.");
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

		//physics
		public float ObjectBouncynessMultiplier = 1f;
		public float FrictionMultiplier = 1f;
		public float SlideFrictionMultiplier = 1f;
		public float MaximumTimeSpentFlying = 90f;

		//how many rounds of logs Should be stored before they get deleted,  null = 100, -1 Do not delete (will lag admin log UI After a while so manage yourself)
		//= n The number you want to keep
		public int? NumberOfLogsToStore;

	}
}