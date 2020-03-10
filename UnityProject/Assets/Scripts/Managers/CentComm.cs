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

	//Server only:
	private List<Vector2> AsteroidLocations = new List<Vector2>();
	private int PlasmaOrderRequestAmt;
	private GameObject paperPrefab;

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
		"\n\nThe emergency shuttle has been called. It will arrive in {0} minutes." +
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

		//Determine Plasma order:
		PlasmaOrderRequestAmt = Random.Range(5, 50);
		SendReportToStation();

		//Check for coup game modes:
		if (GameManager.Instance.GetGameModeName(true) == "Cargonia")
		{
			StartCoroutine(WaitToCargoniaReport());
		}
	}

	IEnumerator WaitToCargoniaReport()
	{
		yield return WaitFor.Seconds(600f);
		MakeCommandReport(CargoniaReport(), UpdateType.alert);
	}

	private void SendReportToStation()
	{
		var commConsoles = FindObjectsOfType<CommsConsole>();
		foreach (CommsConsole console in commConsoles)
		{
			var p = Spawn.ServerPrefab(paperPrefab, console.transform.position, console.transform.parent).GameObject;
			var paper = p.GetComponent<Paper>();
			paper.SetServerString(CreateStartGameReport());
		}

		Chat.AddSystemMsgToChat(CommandUpdateAnnouncementString(), MatrixManager.MainStationMatrix);

		SoundManager.PlayNetworked("Notice1", 1f);
		SoundManager.PlayNetworked("InterceptMessage", 1f);
	}

	public void MakeCommandReport(string text, UpdateType type)
	{
		var commConsoles = FindObjectsOfType<CommsConsole>();
		foreach (CommsConsole console in commConsoles)
		{
			var p = Spawn.ServerPrefab(paperPrefab, console.transform.position, console.transform.parent).GameObject;
			var paper = p.GetComponent<Paper>();
			paper.SetServerString(string.Format(CentCommReportTemplate, text));
		}

		Chat.AddSystemMsgToChat(string.Format(CentCommAnnounceTemplate, CommandNewReportString()), MatrixManager.MainStationMatrix);

		SoundManager.PlayNetworked(UpdateTypes[type], 1f);
		SoundManager.PlayNetworked("Commandreport", 1f);
	}

	public static void MakeAnnouncement( string template, string text, UpdateType type )
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
	public static void MakeShuttleCallAnnouncement( int minutes, string text )
	{
		if ( text.Trim() == string.Empty || text.Trim().Length < 10)
		{
			return;
		}

		Chat.AddSystemMsgToChat(string.Format( PriorityAnnouncementTemplate, string.Format(ShuttleCallSubTemplate,minutes,text) ),
			MatrixManager.MainStationMatrix);
		PlaySoundMessage.SendToAll("ShuttleCalled", Vector3.zero, 1f);
	}

	/// <summary>
	/// Text can be empty
	/// </summary>
	public static void MakeShuttleRecallAnnouncement( string text )
	{
		Chat.AddSystemMsgToChat(string.Format( PriorityAnnouncementTemplate, string.Format(ShuttleRecallSubTemplate,text) ),
			MatrixManager.MainStationMatrix);
		PlaySoundMessage.SendToAll("ShuttleRecalled", Vector3.zero, 1f);
	}

	private string CreateStartGameReport()
	{
		string report = "<size=38>CentComm Report</size> \n __________________________________ \n \n" +
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

	public enum UpdateType {
		notice,
		alert,
		announce
	}

	private static readonly Dictionary<UpdateType, string> UpdateTypes = new Dictionary<UpdateType, string> {
		{UpdateType.notice, "Notice2"},
		{UpdateType.alert, "Notice1"},
		{UpdateType.announce, "Announce"}
	};

	private string CommandNewReportString()
	{
		return "<color=#FF151F>Incoming Classified Message</color>\n\n"
		+ "A report has been downloaded and printed out at all communications consoles.";
	}

	private string CommandUpdateAnnouncementString()
	{
		return "\n\n<color=white><size=60><b>Central Command Update</b></size>"
		+ "\n\n<b><size=40>Enemy communication intercepted. Security level elevated."
		+ "</size></b></color>\n\n<color=#FF151F><size=36>A summary has been copied and"
		+ " printed to all communications consoles. </size></color>\n\n<color=#FF151F><b>"
		+ "Attention! Security level elevated to blue:</b></color>\n<color=white><size=36>"
		+ "<b>The station has received reliable information about possible hostile activity"
		+ " on the station. Security staff may have weapons visible. Searches are permitted"
		+ " only with probable cause.</b></size></color>\n\n";
	}

	private string CargoniaReport()
	{
		return
			" <size=26>Confidential information disclosed:</size>\n\n"
			+ "CentComm has reliable information to believe some of the crewmembers on the station"
			+ " are planning to stage a coup.\n\n"
			+ "CentComm orders to find suspects and neutralize the threat immediately.\n\n"
			+ "<color=blue><size=32>New Station Objectives:</size></color>\n\n"
			+ "<size=24>- Find the revolted crewmembers and neutralize the threat\n\n"
			+ "- Be cautious and restore order in the station</size>";
	}

}