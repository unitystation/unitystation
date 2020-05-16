using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

///------------
/// CENTRAL COMMAND HQ
///------------
public class CentComm : MonoBehaviour
{
	public GameManager gameManager;

	public StatusDisplayUpdateEvent OnStatusDisplayUpdate = new StatusDisplayUpdateEvent();

	public AlertLevel CurrentAlertLevel = AlertLevel.Green;

	//Server only:
	private List<Vector2> AsteroidLocations = new List<Vector2>();
	private int PlasmaOrderRequestAmt;
	private GameObject paperPrefab;
	public DateTime lastAlertChange;
	public double coolDownAlertChange = 5;

	public static string CaptainAnnounceTemplate =
		"\n\n<color=white><size=60><b>Captain Announces</b></size></color>\n\n"
	  + "<color=#FF151F><b>{0}</b></color>\n\n";

	public static string CentCommAnnounceTemplate =
		"\n\n<color=white><size=60><b>Central Command Update</b></size></color>\n\n"
	  + "<color=#FF151F><b>{0}</b></color>\n\n";
	public static string PriorityAnnouncementTemplate =
		"\n\n<color=white><size=60><b>Priority Announcement</b></size></color>\n\n"
	  + "<color=#FF151F>{0}</color>\n\n";

	public static string ShuttleCallSubTemplate =
		"\n\nThe emergency shuttle has been called. It will arrive in {0} " +
		"\nNature of emergency:" +
		"\n\n{1}";
	// Not traced yet, but eventually will:
	//		+"\n\nCall signal traced. Results can be viewed on any communications console.";

	public static string ShuttleRecallSubTemplate =
		"\n\nThe emergency shuttle has been recalled. " +
	// Not traced yet, but eventually will:
	//		+"Recall signal traced. Results can be viewed on any communications console.";
		"\n\n{0}";

	public static string CentCommReportTemplate =
		"<size=40><b>CentComm Report</b></size> \n __________________________________\n\n{0}";

	private readonly string InitialUpdateTemplate =
		"<color=white><size=40><b>{0}</b></size></color>\n\n"+
		"<color=#FF151F>A summary has been copied and"+
		" printed to all communications consoles</color>";

	private readonly string ExtendedInitialUpdate =
		"Thanks to the tireless efforts of our security and intelligence divisions,"+
		" there are currently no credible threats to the station."+ //TODO use real Station name here
		" All station construction projects have been authorized. Have a secure shift!";

	private string AntagInitialUpdate =
		"Enemy communication intercepted. Security level elevated.";

	void Start()
	{
		paperPrefab = Resources.Load<GameObject>("Paper");
	}

	private void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundStarted, OnRoundStart);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundStarted, OnRoundStart);
	}

	private void OnRoundStart()
	{
		AsteroidLocations.Clear();
		CurrentAlertLevel = AlertLevel.Green;
		lastAlertChange = GameManager.Instance.stationTime;
		StartCoroutine(WaitToPrepareReport());
	}

	IEnumerator WaitToPrepareReport()
	{
		yield return WaitFor.EndOfFrame; //OnStartServer starts one frame after OnRoundStart
		//Server only:
		if (!CustomNetworkManager.Instance._isServer)
		{
			yield break;
		}
		//Generic AI welcome message
		//this sound will feel just like home once we have the proper job allocation.
		//it plays as soon as the round starts.
		SoundManager.PlayNetworked("Welcome");
		//Wait some time after the round has started
		yield return WaitFor.Seconds(60f);

		//Gather asteroid locations:
		for (int i = 0; i < gameManager.SpaceBodies.Count; i++)
		{
			var asteroid = gameManager.SpaceBodies[i].GetComponent<Asteroid>();
			if (asteroid != null)
			{
				AsteroidLocations.Add(gameManager.SpaceBodies[i].ServerState.Position);
			}
		}

		//Add in random positions
		int randomPosCount = Random.Range(1, 5);
		for (int i = 0; i <= randomPosCount; i++)
		{
			AsteroidLocations.Add(gameManager.RandomPositionInSolarSystem());
		}

		//Shuffle the list:
		AsteroidLocations = AsteroidLocations.OrderBy(x => Random.value).ToList();
		PlasmaOrderRequestAmt = Random.Range(5, 50);


		// Checks if there will be antags this round and sets the initial update/report
		if (GameManager.Instance.GetGameModeName(true) != "Extended")
		{
			lastAlertChange = GameManager.Instance.stationTime;
			SendAntagUpdate();

			if (GameManager.Instance.GetGameModeName(true) == "Cargonia")
			{
				StartCoroutine(WaitToCargoniaReport());
			}

		}
		else
		{
			SendExtendedUpdate();
		}

		StartCoroutine(WaitToGenericReport());
		yield break;
	}

	private void SendExtendedUpdate()
	{
		MakeAnnouncement(CentCommAnnounceTemplate, string.Format(InitialUpdateTemplate, ExtendedInitialUpdate),
						UpdateSound.notice);
		SpawnReports(StationObjectiveReport());
	}
	private void SendAntagUpdate()
	{
		SoundManager.PlayNetworked("InterceptMessage");
		MakeAnnouncement(CentCommAnnounceTemplate,
						string.Format(InitialUpdateTemplate,AntagInitialUpdate+"\n\n"+AlertLevelStrings[AlertLevelString.UpToBlue]),
						UpdateSound.alert);
		SpawnReports(StationObjectiveReport());
		SpawnReports(AntagThreatReport);
		ChangeAlertLevel(AlertLevel.Blue, false);
	}

	IEnumerator WaitToCargoniaReport()
	{
		yield return WaitFor.Seconds(Random.Range(600f,1200f));
		MakeCommandReport(CargoniaReport, UpdateSound.notice);
		yield break;
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
		MakeAnnouncement(CentCommAnnounceTemplate, PlayerUtils.GetGenericReport(), UpdateSound.announce);
	}

	/// <summary>
	/// Changes current Alert Level. You can omit the announcement! Must be called on server.
	/// </summary>
	/// <param name="ToLevel">Value from AlertLevel that represent the desired level</param>
	/// <param name="Announce">Optional, Should we announce the change? Default is true.</param>

	public void ChangeAlertLevel(AlertLevel ToLevel, bool Announce = true)
	{
		if (CurrentAlertLevel == ToLevel) return;

		if (CurrentAlertLevel > ToLevel && ToLevel == AlertLevel.Green)
		{
			MakeAnnouncement(CentCommAnnounceTemplate,
							AlertLevelStrings[AlertLevelString.DownToGreen],
							UpdateSound.notice);
		}
		else if (CurrentAlertLevel > ToLevel && Announce)
		{
			int _levelString = (int) ToLevel * -1;
			MakeAnnouncement(CentCommAnnounceTemplate,
							AlertLevelStrings[(AlertLevelString)_levelString],
							UpdateSound.notice);
		}
		else if (CurrentAlertLevel < ToLevel && Announce)
		{
			MakeAnnouncement(CentCommAnnounceTemplate,
							AlertLevelStrings[(AlertLevelString)ToLevel],
							UpdateSound.alert);
		}

		CurrentAlertLevel = ToLevel;
	}

	/// <summary>
	/// Spawns written reports at every comms console. Must be called on server.
	/// </summary>
	/// <param name="text">String that will be the report body</param>
	private void SpawnReports(string text)
	{
		var commConsoles = FindObjectsOfType<CommsConsole>();
		foreach (CommsConsole console in commConsoles)
		{
			var p = Spawn.ServerPrefab(paperPrefab, console.transform.position, console.transform.parent).GameObject;
			var paper = p.GetComponent<Paper>();
			paper.SetServerString(string.Format(CentCommReportTemplate, text));
		}
	}

	/// <summary>
	/// Makes and announce a written report that will spawn at all comms consoles. Must be called on server.
	/// </summary>
	/// <param name="text">String that will be the report body</param>
	/// <param name="type">Value from the UpdateSound enum to play as sound when announcing</param>
	public void MakeCommandReport(string text, UpdateSound type)
	{
		SpawnReports(text);

		Chat.AddSystemMsgToChat(string.Format(CentCommAnnounceTemplate, CommandNewReportString), MatrixManager.MainStationMatrix);

		SoundManager.PlayNetworked(UpdateTypes[type], 1f);
		SoundManager.PlayNetworked("Commandreport");
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

		SoundManager.PlayNetworked( UpdateTypes[type] );
		Chat.AddSystemMsgToChat(string.Format( template, text ), MatrixManager.MainStationMatrix);
	}

	/// <summary>
	/// Text should be no less than 10 chars
	/// </summary>
	public static void MakeShuttleCallAnnouncement( string minutes, string text )
	{
		if ( text.Trim() == string.Empty || text.Trim().Length < 10)
		{
			return;
		}

		Chat.AddSystemMsgToChat(string.Format( PriorityAnnouncementTemplate, string.Format(ShuttleCallSubTemplate,minutes,text) ),
			MatrixManager.MainStationMatrix);
		SoundManager.PlayNetworked("ShuttleCalled");
	}

	/// <summary>
	/// Text can be empty
	/// </summary>
	public static void MakeShuttleRecallAnnouncement( string text )
	{
		Chat.AddSystemMsgToChat(string.Format( PriorityAnnouncementTemplate, string.Format(ShuttleRecallSubTemplate,text) ),
			MatrixManager.MainStationMatrix);
		SoundManager.PlayNetworked("ShuttleRecalled");
	}

	private string StationObjectiveReport()
	{
		string report =
			" <size=26>Asteroid bodies have been sighted in the local area around " +
			"OutpostStation IV. Locate and exploit local sources for plasma deposits.</size>\n \n " +
			"<color=blue><size=32>Crew Objectives:</size></color>\n \n <size=24>- Locate and mine " +
			"local Plasma Deposits\n \n - Fulfill order of " + PlasmaOrderRequestAmt + " Solid Plasma units and dispatch to " +
			"Central Command via Cargo Shuttle</size>\n \n <size=32>Latest Asteroid Sightings:" +
			"</size>\n \n";

		for (int i = 0; i < AsteroidLocations.Count; i++)
		{
			report += " <size=24>" + Vector2Int.RoundToInt(AsteroidLocations[i]).ToString() + "</size> ";
		}

		return report;
	}

	private readonly string AntagThreatReport =
	"<size=26>Central Command has intercepted and partially decoded a Syndicate transmission with vital"+
	" information regarding their movements.\n\n"+
	"CentComm believes there might be Syndicate activity in the Station. We will keep you informed as we gather more "+
	" intelligence.</size>\n\n"+
	"<color=blue><size=32>Crew Objectives:</size></color>\n\n"+
	"<size=24>- Subvert the threat.\n\n- Keep the productivity in the station.</size>";

	public enum UpdateSound {
		notice,
		alert,
		announce
	}

	private static readonly Dictionary<UpdateSound, string> UpdateTypes = new Dictionary<UpdateSound, string> {
		{UpdateSound.notice, "Notice2"},
		{UpdateSound.alert, "Notice1"},
		{UpdateSound.announce, "Announce"}
	};

	public enum AlertLevel {
		Green = 1,
		Blue = 2,
		Red = 3,
		Delta = 4
	}

	private enum AlertLevelString {
		DownToGreen = AlertLevel.Green,
		UpToBlue = AlertLevel.Blue,
		DownToBlue = -AlertLevel.Blue,
		UpToRed = AlertLevel.Red,
		DownToRed = -AlertLevel.Red,
		UpToDelta = AlertLevel.Delta
	}

	private static readonly string AlertLevelTemplate =
			"<color=#FF151F><size=40><b>Attention! Security level {0}:</b></size></color>\n"+
			"<color=white><b>{1}</b></color>";

	private static readonly Dictionary<AlertLevelString, string> AlertLevelStrings = new Dictionary<AlertLevelString, string> {
		{
			AlertLevelString.DownToGreen,

			string.Format(AlertLevelTemplate,
					"lowered to green",
					"All threats to the station have passed. Security may not have weapons visible,"+
					" privacy laws are once again fully enforced.")
		},
		{
			AlertLevelString.UpToBlue,

			string.Format(AlertLevelTemplate,
					"elevated to blue",
					"The station has received reliable information about possible hostile activity"+
					" on the station. Security staff may have weapons visible, random searches are permitted.")

		},
		{
			AlertLevelString.DownToBlue,

			string.Format(AlertLevelTemplate,
					"lowered to blue",
					"The immediate threat has passed. Security may no longer have weapons drawn at all times,"+
					" but may continue to have them visible. Random searches are still allowed.")
		},
		{
			AlertLevelString.UpToRed,

			string.Format(AlertLevelTemplate,
					"elevated to red",
					"There is an immediate serious threat to the station. Security may have weapons unholstered"+
					" at all times. Random searches are allowed and advised.")
		},
		{
			AlertLevelString.DownToRed,

			string.Format(AlertLevelTemplate,
					"lowered to red",
					"The station's destruction has been averted. There is still however an immediate serious"+
					" threat to the station. Security may have weapons unholstered at all times, random searches"+
					" are allowed and advised.")
		},
		{
			AlertLevelString.UpToDelta,

			string.Format(AlertLevelTemplate,
					"elevated to delta",
					"Destruction of the station is imminent. All crew are instructed to obey all instructions"+
					" given by heads of staff. Any violations of these orders can be punished by death."+
					" This is not a drill.")
		}
	};

	private readonly string CommandNewReportString =
		"<color=#FF151F>Incoming Classified Message</color>\n\n"
		+ "A report has been downloaded and printed out at all communications consoles.";

	private readonly string CargoniaReport =
			" <size=26>Confidential information disclosed:</size>\n\n"
			+ "CentComm has reliable information to believe some of the crewmembers on the station"
			+ " are planning to stage a coup.\n\n"
			+ "CentComm orders to find suspects and neutralize the threat immediately.\n\n"
			+ "<color=blue><size=32>New Station Objectives:</size></color>\n\n"
			+ "<size=24>- Find the revolted crewmembers and neutralize the threat\n\n"
			+ "- Make sure captain is wearing his Captain's hat AT ALL COST.\n\n"
			+ "- Restore order in the station.</size>";

}