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
		[NonSerialized] public string CommandStatusString;
		[NonSerialized] public string EscapeShuttleTimeString;

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

		private void OnRoundStart()
		{
			asteroidLocations.Clear();
			ChangeAlertLevel(initialAlertLevel, false);
			StartCoroutine(WaitToPrepareReport());
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
				lastAlertChange = GameManager.Instance.stationTime;
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
			MakeAnnouncement(ChatTemplates.CentcomAnnounce, string.Format(ReportTemplates.InitialUpdate, ReportTemplates.ExtendedInitial),
				UpdateSound.Notice);
		}
		private void SendAntagUpdate()
		{
			_ = SoundManager.PlayNetworked(CommonSounds.Instance.AnnouncementIntercept);
			MakeAnnouncement(
				ChatTemplates.CentcomAnnounce,
				string.Format(
					ReportTemplates.InitialUpdate,
					ReportTemplates.AntagInitialUpdate+"\n\n"+
					ChatTemplates.GetAlertLevelMessage(AlertLevelChange.UpToBlue)),
				UpdateSound.Alert);
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

			lastAlertChange = gameManager.stationTime;
			CurrentAlertLevel = toLevel;
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
				Chat.AddSystemMsgToChat(string.Format(ChatTemplates.CentcomAnnounce, ChatTemplates.CommandNewReport), MatrixManager.MainStationMatrix);

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
		/// <param name="type">Value from the UpdateSound enum to play as sound when announcing</param>
		public static void MakeAnnouncement( string template, string text, UpdateSound type )
		{
			if ( text.Trim() == string.Empty )
			{
				return;
			}

			if (type != UpdateSound.NoSound)
			{
				_ = SoundManager.PlayNetworked( updateTypes[type] );
			}

			Chat.AddSystemMsgToChat(string.Format( template, text ), MatrixManager.MainStationMatrix);
		}

		/// <summary>
		/// Text should be no less than 10 chars
		/// </summary>
		public static void MakeShuttleCallAnnouncement( string minutes, string text, bool bypassLength = false )
		{
			if (!bypassLength && (text.Trim() == string.Empty || text.Trim().Length < 10))
			{
				return;
			}

			Chat.AddSystemMsgToChat(
				string.Format(ChatTemplates.PriorityAnnouncement, string.Format(ChatTemplates.ShuttleCallSub,minutes,text) ),
				MatrixManager.MainStationMatrix);

			_ = SoundManager.PlayNetworked(CommonSounds.Instance.ShuttleCalled);
		}

		/// <summary>
		/// Text can be empty
		/// </summary>
		public static void MakeShuttleRecallAnnouncement( string text )
		{
			Chat.AddSystemMsgToChat(
				string.Format(ChatTemplates.PriorityAnnouncement, string.Format(ChatTemplates.ShuttleRecallSub,text)),
				MatrixManager.MainStationMatrix);

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
