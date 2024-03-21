using System;
using System.Collections;
using Logs;
using Managers;
using UnityEngine;
using Objects.Wallmounts;
using Strings;

/// <summary>
/// Escape-related part of GameManager
/// </summary>
public partial class GameManager
{
	public EscapeShuttle PrimaryEscapeShuttle => primaryEscapeShuttle;
	[SerializeField]
	private EscapeShuttle primaryEscapeShuttle;

	private Coroutine departCoroutine;
	private bool shuttleSent;

	public bool ShuttleSent => shuttleSent;

	public int GiveUpTime { get; private set; } = 200;

	private bool beenToStation;

	private void InitEscapeShuttle()
	{
		//Primary escape shuttle lookup
		if (PrimaryEscapeShuttle == false)
		{
			var shuttles = FindObjectsOfType<EscapeShuttle>();
			if (shuttles.Length != 1)
			{
				Loggy.LogError("Primary escape shuttle is missing from GameManager!", Category.Round);
				return;
			}
			Loggy.LogWarning("Primary escape shuttle is missing from GameManager, but one was found on scene", Category.Round);
			primaryEscapeShuttle = shuttles[0];
		}
	}

	/// <summary>
	/// Called after MatrixManager is initialized
	/// </summary>
	private void InitEscapeStuff()
	{
		// Primary escape shuttle lookup
		if (PrimaryEscapeShuttle == null)
		{
			var shuttles = FindObjectsOfType<EscapeShuttle>();
			if (shuttles.Length < 1)
			{
				Loggy.LogWarning("Primary escape shuttle is missing from GameManager!", Category.Round);
				return;
			}
			Loggy.LogWarning("Primary escape shuttle is missing from GameManager, but one was found on scene", Category.Round);
			primaryEscapeShuttle = shuttles[0];
		}

		// later, maybe: keep a list of all computers and call the shuttle automatically with a 25 min timer if they are deleted

		if (primaryEscapeShuttle.MatrixInfo == null)
		{
			Loggy.LogError("Primary escape shuttle has no associated matrix!", Category.Round);
			return;
		}

		// Starting up at Centcom coordinates
		if (Instance.QuickLoad)
		{
			if (primaryEscapeShuttle.MatrixInfo == null) return;
			if (primaryEscapeShuttle.MatrixInfo.IsMovable == false) return;
		}

		Vector3 newPos;

		if (LandingZoneManager.Instance.centcomDocking == null)
		{
			Loggy.LogError("Centcom docking point is null, this should only happen if theres no centcom scene");
			return;
		}

		beenToStation = false;
	}

	public void OnShuttleUpdate(EscapeShuttleStatus status)
	{
		if (status == EscapeShuttleStatus.DockedCentcom && beenToStation)
		{
			Loggy.Log("Shuttle arrived at Centcom", Category.Round);
			Chat.AddSystemMsgToChat(string.Format(ChatTemplates.PriorityAnnouncement, $"<color=white>Escape shuttle has docked at Centcomm! Round will restart in {TimeSpan.FromSeconds(RoundEndTime).Minutes} minute.</color>"), MatrixManager.MainStationMatrix);
			StartCoroutine(WaitForRoundEnd());
		}

		if (status == EscapeShuttleStatus.DockedStation && !primaryEscapeShuttle.hostileEnvironment)
		{
			beenToStation = true;
			_ = SoundManager.PlayNetworked(CommonSounds.Instance.ShuttleDocked);
			Chat.AddSystemMsgToChat(string.Format(ChatTemplates.PriorityAnnouncement, $"<color=white>Escape shuttle has arrived! Crew has {TimeSpan.FromSeconds(ShuttleDepartTime).Minutes} minutes to get on it.</color>"), MatrixManager.MainStationMatrix, LanguageManager.Common);
			// should be changed to manual send later
			departCoroutine = StartCoroutine( SendEscapeShuttle( ShuttleDepartTime ) );
		}
		else if (status == EscapeShuttleStatus.DockedStation && primaryEscapeShuttle.hostileEnvironment)
		{
			beenToStation = true;
			_ = SoundManager.PlayNetworked(CommonSounds.Instance.ShuttleDocked);
			Chat.AddSystemMsgToChat(string.Format(ChatTemplates.PriorityAnnouncement, $"<color=white>Escape shuttle has arrived! The shuttle <color=#FF151F>cannot</color> leave the station due to the hostile environment!</color>"), MatrixManager.MainStationMatrix, LanguageManager.Common);
		}
	}

	private IEnumerator WaitForRoundEnd()
	{
		Loggy.Log($"Shuttle docked to Centcom, Round will end in {TimeSpan.FromSeconds(RoundEndTime).Minutes} minute", Category.Round);
		yield return WaitFor.Seconds(1f);
		EndRound();
	}

	public void ForceSendEscapeShuttleFromStation(int departTime)
	{
		if (shuttleSent || PrimaryEscapeShuttle.Status != EscapeShuttleStatus.DockedStation) return;

		if (departCoroutine != null)
		{
			StopCoroutine(departCoroutine);
		}

		departCoroutine = StartCoroutine( SendEscapeShuttle(departTime));
	}

	private void TrackETA(int eta)
	{

	}

	private IEnumerator SendEscapeShuttle(int seconds)
	{
		// departure countdown
		for (int i = seconds; i >= 0; i--)
		{
			CentComm.UpdateStatusDisplay( StatusDisplayChannel.EscapeShuttle, StatusDisplay.FormatTime(i, "Depart\nETA: ") );
			yield return WaitFor.Seconds(1);
		}

		shuttleSent = true;
		PrimaryEscapeShuttle.SendShuttle();

		// centcom round end countdown
		int timeToCentcom = ShuttleDepartTime * 2 - 2;
		for (int i = timeToCentcom - 1; i >= 0; i--)
		{
			CentComm.UpdateStatusDisplay( StatusDisplayChannel.EscapeShuttle, StatusDisplay.FormatTime(i, "CENTCOM\nETA: ") );
			yield return WaitFor.Seconds(1);
		}
		CentComm.UpdateStatusDisplay( StatusDisplayChannel.EscapeShuttle, string.Empty);

		GiveUpTime = 200;
		while (GiveUpTime > 0)
		{
			GiveUpTime--;
			yield return WaitFor.Seconds(1);
		}

		Loggy.LogError("[GameManager.Escape/SendEscapeShuttle()] -  OH SHITTTT Shuttle got stuck on the Way to Centralcommand AAAAAAAAAAAAAAAAAAAAAAAAAAAA emergency end round");
		EndRound();
	}

	private IEnumerator WaitToInitEscape()
	{
		while (MatrixManager.IsInitialized == false)
		{
			yield return WaitFor.EndOfFrame;
		}

		InitEscapeStuff();
	}
}
