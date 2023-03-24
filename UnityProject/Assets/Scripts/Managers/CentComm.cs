using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Initialisation;
using Map;
using Messages.Server.SoundMessages;
using Objects.Command;
using Objects.Wallmounts;
using Player.Language;
using Strings;
using UnityEngine;
using Random = UnityEngine.Random;
using StationObjectives;

namespace Managers
{
	public class CentComm : MonoBehaviour
	{
		public GameManager gameManager;

		[SerializeField][Tooltip("Reference to the paper prefab. Needed to send reports.")]
		private GameObject paperPrefab = default;

		public StatusDisplayUpdateEvent OnStatusDisplayUpdate = new StatusDisplayUpdateEvent();
		public event Action OnAlertLevelChange;
		[NonSerialized] public string CommandStatusString = string.Empty;
		[NonSerialized] public string EscapeShuttleTimeString = string.Empty;

		public void UpdateStatusDisplay(StatusDisplayChannel channel, string text)
		{
			if (channel == StatusDisplayChannel.EscapeShuttle)
			{
				EscapeShuttleTimeString = text;
			}
			else if(channel == StatusDisplayChannel.Command)
			{
				CommandStatusString = text;
			}
			OnStatusDisplayUpdate.Invoke(channel);
		}

		[SerializeField] [Tooltip("What should the initial Alert level be on the round.")]
		private AlertLevel initialAlertLevel = AlertLevel.Green;

		[NonSerialized] public AlertLevel CurrentAlertLevel;

		public bool IsLowPop = false;

		//Server only:
		public static List<Vector2> asteroidLocations = new List<Vector2>();

		public DateTime lastAlertChange;
		public double coolDownAlertChange = 5;

		public InitialisationSystems Subsystem => InitialisationSystems.CentComm;

		private static Dictionary<UpdateSound, AddressableAudioSource> updateTypes;

		private void Awake()
		{
			updateTypes = new Dictionary<UpdateSound, AddressableAudioSource>
			{
				{UpdateSound.Notice, CommonSounds.Instance.Notice2},
				{UpdateSound.Alert, CommonSounds.Instance.Notice1},
				{UpdateSound.Announce, CommonSounds.Instance.AnnouncementAnnounce},
				{UpdateSound.CentComAnnounce, CommonSounds.Instance.AnnouncementCentCom}
			};
		}

		private void OnEnable()
		{
			EventManager.AddHandler(Event.RoundStarted, OnRoundStart);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.RoundStarted, OnRoundStart);
		}

		public void Clear()
		{
			OnAlertLevelChange = () => { };
			OnStatusDisplayUpdate = new StatusDisplayUpdateEvent();
		}

		private void OnRoundStart()
		{
			asteroidLocations.Clear();
			ChangeAlertLevel(initialAlertLevel, false);
			StartCoroutine(WaitToPrepareReport());
			IsLowPop = false;
			if(CustomNetworkManager.IsServer) StartCoroutine(LowpopCheck());
		}

		private IEnumerator WaitToPrepareReport()
		{
			yield return WaitFor.EndOfFrame; //OnStartServer starts one frame after OnRoundStart
			//Server only:
			if (!CustomNetworkManager.Instance._isServer)
			{
				yield break;
			}

			_ = SoundManager.PlayNetworked(CommonSounds.Instance.AnnouncementWelcome);

			yield return WaitFor.Seconds(60f);

			//Gather asteroid locations:
			foreach (var body in gameManager.SpaceBodies)
			{
				if (body.TryGetComponent<Asteroid>(out _))
				{
					asteroidLocations.Add(body.ServerState.Position);
				}
			}

			//Add in random positions
			int randomPosCount = Random.Range(1, 5);
			for (int i = 0; i <= randomPosCount; i++)
			{
				asteroidLocations.Add(gameManager.RandomPositionInSolarSystem());
			}

			//Shuffle the list:
			asteroidLocations = asteroidLocations.OrderBy(x => Random.value).ToList();


			// Checks if there will be antags this round and sets the initial update/report
			if (GameManager.Instance.GetGameModeName(true) != "Extended")
			{
				lastAlertChange = GameManager.Instance.RoundTime;
				SendAntagUpdate();
			}
			else
			{
				SendExtendedUpdate();
			}
			StationObjectiveManager.Instance.ServerChooseObjective();
			StartCoroutine(WaitToGenericReport());
		}

		private void SendExtendedUpdate()
		{
			var message = string.Format(ReportTemplates.InitialUpdate, ReportTemplates.ExtendedInitial);
			MakeAnnouncement(ChatTemplates.CentcomAnnounce, message, UpdateSound.Notice);
		}

		private void SendAntagUpdate()
		{
			_ = SoundManager.PlayNetworked(CommonSounds.Instance.AnnouncementIntercept);
			var message = string.Format(ReportTemplates.InitialUpdate,
					$"{ReportTemplates.AntagInitialUpdate}\n\n{ChatTemplates.GetAlertLevelMessage(AlertLevelChange.UpToBlue)}");
			MakeAnnouncement(ChatTemplates.CentcomAnnounce, message, UpdateSound.Alert);
			SpawnReports(ReportTemplates.AntagThreat);
			ChangeAlertLevel(AlertLevel.Blue, false);
		}

		IEnumerator WaitToGenericReport()
		{
			if (!PlayerUtils.IsOk(gameObject))
			{
				yield break;
			}
			yield return WaitFor.Seconds(Random.Range(300f,1500f));
			PlayerUtils.DoReport();

			yield return WaitFor.Seconds(10);
			MakeAnnouncement(ChatTemplates.CentcomAnnounce, PlayerUtils.GetGenericReport(), UpdateSound.Announce);
		}

		private IEnumerator LowpopCheck()
		{
			yield return WaitFor.Seconds(Application.isEditor ? 30 : gameManager.LowPopCheckTimeAfterRoundStart);
			if(PlayerList.Instance.GetAlivePlayers().Count > gameManager.LowPopLimit) yield break;
			IsLowPop = true;
			MakeAnnouncement(ChatTemplates.CentcomAnnounce,
				"Due to the shortage of staff on the station; We have granted additional access to all crew members until further notice."
				, UpdateSound.Announce);
		}

		/// <summary>
		/// Changes current Alert Level. You can omit the announcement! Must be called on server.
		/// </summary>
		/// <param name="toLevel">Value from AlertLevel that represent the desired level</param>
		/// <param name="announce">Optional, Should we announce the change? Default is true.</param>
		public void ChangeAlertLevel(AlertLevel toLevel, bool announce = true)
		{
			if (CurrentAlertLevel == toLevel) return;

			if (CurrentAlertLevel > toLevel && toLevel == AlertLevel.Green && announce)
			{
				MakeAnnouncement(ChatTemplates.CentcomAnnounce,
					ChatTemplates.GetAlertLevelMessage(AlertLevelChange.DownToGreen),
					UpdateSound.Notice);
			}
			else if (CurrentAlertLevel > toLevel && announce)
			{
				var levelString = (int) toLevel * -1;
				MakeAnnouncement(ChatTemplates.CentcomAnnounce,
					ChatTemplates.GetAlertLevelMessage((AlertLevelChange)levelString),
					UpdateSound.Notice);
			}
			else if (CurrentAlertLevel < toLevel && announce)
			{
				MakeAnnouncement(ChatTemplates.CentcomAnnounce,
					ChatTemplates.GetAlertLevelMessage((AlertLevelChange)toLevel),
					UpdateSound.Alert);
			}

			lastAlertChange = gameManager.RoundTime;
			CurrentAlertLevel = toLevel;
			OnAlertLevelChange?.Invoke();
		}

		/// <summary>
		/// Spawns written reports at every comms console. Must be called on server.
		/// </summary>
		/// <param name="text">String that will be the report body</param>
		private void SpawnReports(string text)
		{
			var commConsoles = FindObjectsOfType<CommsConsole>();
			foreach (var console in commConsoles)
			{
				var p = Spawn.ServerPrefab(paperPrefab, console.gameObject.RegisterTile().WorldPositionServer, console.transform.parent).GameObject;
				var paper = p.GetComponent<Paper>();
				paper.SetServerString(string.Format(ReportTemplates.CentcomReport, text));
			}
		}

		/// <summary>
		/// Makes and announce a written report that will spawn at all comms consoles. Must be called on server.
		/// </summary>
		/// <param name="text">String that will be the report body</param>
		/// <param name="loudAnnouncement">Play as sound when announcing</param>
		public void MakeCommandReport(string text, bool loudAnnouncement = true)
		{
			SpawnReports(text);

			if (loudAnnouncement)
			{
				Chat.AddSystemMsgToChat(string.Format(ChatTemplates.CentcomAnnounce, ChatTemplates.CommandNewReport), MatrixManager.MainStationMatrix, LanguageManager.Common);

				AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1f);
				_ = SoundManager.PlayNetworked(updateTypes[UpdateSound.Notice], audioSourceParameters);
				_ = SoundManager.PlayNetworked(CommonSounds.Instance.AnnouncementCommandReport);
			}
		}

		/// <summary>
		/// Makes an announcement for all players. Must be called on server.
		/// </summary>
		/// <param name="template">String that will be the header of the annoucement. We have a couple ready to use </param>
		/// <param name="text">String that will be the message body</param>
		/// <param name="soundType">Value from the UpdateSound enum to play as sound when announcing</param>
		/// <param name="language">Language to announce in (null for common)</param>
		public static void MakeAnnouncement(string template, string text, UpdateSound soundType, LanguageSO language = null)
		{
			if (string.IsNullOrWhiteSpace(text)) return;

			if (soundType != UpdateSound.NoSound)
			{
				_ = SoundManager.PlayNetworked(updateTypes[soundType]);
			}

			Chat.AddSystemMsgToChat(string.Format(template, text), MatrixManager.MainStationMatrix, language.OrNull() ?? LanguageManager.Common);
		}

		/// <summary>
		/// Text should be no less than 10 chars
		/// </summary>
		public static void MakeShuttleCallAnnouncement(int seconds, string text, bool bypassLength = false)
		{
			if (!bypassLength && (text.Trim() == string.Empty || text.Trim().Length < 10))
			{
				return;
			}

			var timeSpan = TimeSpan.FromSeconds(seconds);
			var timeStr = timeSpan.Seconds > 0
					? $"{timeSpan.Minutes} minutes and {timeSpan.Seconds} seconds"
					: $"{timeSpan.Minutes} minutes";
			var message = string.Format(ChatTemplates.PriorityAnnouncement, string.Format(ChatTemplates.ShuttleCallSub, timeStr, text));
			Chat.AddSystemMsgToChat(message, MatrixManager.MainStationMatrix, LanguageManager.Common);

			_ = SoundManager.PlayNetworked(CommonSounds.Instance.ShuttleCalled);
		}

		/// <summary>
		/// Text can be empty
		/// </summary>
		public static void MakeShuttleRecallAnnouncement(string text)
		{
			var message = string.Format(ChatTemplates.PriorityAnnouncement, string.Format(ChatTemplates.ShuttleRecallSub, text));
			Chat.AddSystemMsgToChat(message, MatrixManager.MainStationMatrix, LanguageManager.Common);

			_ = SoundManager.PlayNetworked(CommonSounds.Instance.ShuttleRecalled);
		}

		public enum UpdateSound {
			Notice,
			Alert,
			Announce,
			CentComAnnounce,
			NoSound
		}

		public enum AlertLevel {
			Green = 1,
			Blue = 2,
			Red = 3,
			Delta = 4
		}
	}
}
