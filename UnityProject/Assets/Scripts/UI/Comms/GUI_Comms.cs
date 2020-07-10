using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GUI_Comms : NetTab
{
	[SerializeField]
	private NetPageSwitcher mainSwitcher = null;
	[SerializeField]
	private NetPage menuPage = null;

	[SerializeField]
	private NetPageSwitcher captainOnlySwitcher = null;
	[SerializeField]
	private NetPage noCaptainAccessPage = null;
	[SerializeField]
	private NetPage captainAccessPage = null;

	[SerializeField]
	private NetLabel idLabel = null;
	[SerializeField]
	private NetLabel shuttleStatusLabel = null;
	[SerializeField]
	private NetLabel shuttleTimerLabel = null;
	[SerializeField]
	private NetLabel shuttleCallResultLabel = null;
	[SerializeField]
	private NetLabel shuttleCallButtonLabel = null;
	[SerializeField]
	private NetSpriteImage statusImage = null;
	[SerializeField]
	private NetLabel CurrentAlertLevelLabel = null;
	[SerializeField]
	private NetLabel NewAlertLevelLabel = null;
	[SerializeField]
	private NetLabel AlertErrorLabel = null;

	private CommsConsole console;
	private EscapeShuttle shuttle;
	private Coroutine callResultHandle;

	private CentComm.AlertLevel LocalAlertLevel = CentComm.AlertLevel.Green;

	protected override void InitServer()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			StartCoroutine(WaitForProvider());
		}
	}

	IEnumerator WaitForProvider()
	{
		string FormatTime( int timerSeconds )
		{
			if ( shuttle.Status == EscapeShuttleStatus.DockedCentcom || shuttle.Status == EscapeShuttleStatus.DockedStation )
			{
				return string.Empty;
			}

			return "ETA: " + TimeSpan.FromSeconds( timerSeconds ).ToString( "mm\\:ss" );
		}

		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		console = Provider.GetComponentInChildren<CommsConsole>();

		//starting up, setting appropriate labels
		ProcessIdChange(console.IdCard);
		console.OnServerIDCardChanged.AddListener( ProcessIdChange );
		shuttle = GameManager.Instance.PrimaryEscapeShuttle;

		shuttleStatusLabel.SetValueServer(shuttle.Status.ToString());
		statusImage.SetComplicatedValue( "shuttle_status", (int)shuttle.Status );
		shuttle.OnShuttleUpdate.AddListener( status =>
		{
			statusImage.SetComplicatedValue( "shuttle_status", (int)status );
			shuttleStatusLabel.SetValueServer(status.ToString());
		} );

		shuttleTimerLabel.SetValueServer(FormatTime( shuttle.CurrentTimerSeconds ));
		shuttle.OnTimerUpdate.AddListener( timerSeconds =>{ shuttleTimerLabel.SetValueServer(FormatTime( timerSeconds )); } );

		RefreshCallButtonText();

		Logger.Log( nameof(WaitForProvider), Category.NetUI );
	}

	private void ProcessIdChange( IDCard newId = null )
	{
		UpdateIdTexts();
		if ( newId != null )
		{
			LogIn();
		}
		else
		{
			LogOut();
		}
	}

	public void CallOrRecallShuttle(string text)
	{
		Logger.Log( nameof(CallOrRecallShuttle), Category.NetUI );

		bool isRecall = shuttle.Status == EscapeShuttleStatus.OnRouteStation;



		string callResult;
		bool ok;

		if ( isRecall )
		{
			ok = shuttle.RecallShuttle(out callResult);
			if ( ok )
			{
				CentComm.MakeShuttleRecallAnnouncement( text );
				RefreshCallButtonText();
			}
		}
		else
		{
			if ( text.Trim().Length < 10 )
			{
				callResult = "You must provide a reason when calling shuttle!";
				ok = false;
			} else
			{
				ok = shuttle.CallShuttle(out callResult);
				if ( ok )
				{
					var minutes = TimeSpan.FromSeconds(shuttle.InitialTimerSeconds).ToString();
					CentComm.MakeShuttleCallAnnouncement( minutes, text );
					RefreshCallButtonText();
				}
			}
		}
		Logger.Log( callResult, Category.Round );

		this.RestartCoroutine( ShowSubmitResult( callResult ), ref callResultHandle );

		if ( ok )
		{
			OpenMenu();
		}
	}

	private void RefreshCallButtonText()
	{
		shuttleCallButtonLabel.SetValueServer(shuttle.Status == EscapeShuttleStatus.OnRouteStation ? "Recall Emergency Shuttle" : "Call Emergency Shuttle");
	}

	private IEnumerator ShowSubmitResult( string callResult )
	{
		shuttleCallResultLabel.SetValueServer(callResult);
		yield return WaitFor.Seconds( 3 );
		shuttleCallResultLabel.SetValueServer(String.Empty);
	}

	public void SetStatusDisplay(string text)
	{
		Logger.Log( nameof(SetStatusDisplay), Category.NetUI );
		GameManager.Instance.CentComm.UpdateStatusDisplay(StatusDisplayChannel.Command, text.Substring( 0,Mathf.Min(text.Length, 50)));
		OpenMenu();
	}
	public void MakeAnAnnouncement(string text)
	{
		Logger.Log( nameof(MakeAnAnnouncement), Category.NetUI );
		if (text.Length>200)
		{
			CentComm.MakeAnnouncement(CentComm.CaptainAnnounceTemplate, text.Substring(0, 200), CentComm.UpdateSound.announce);
		}
		else
		{
			CentComm.MakeAnnouncement(CentComm.CaptainAnnounceTemplate, text ,CentComm.UpdateSound.announce);
		}
		OpenMenu();
	}

	public void UpdateAlertLevelLabels()
	{
		CurrentAlertLevelLabel.SetValueServer(GameManager.Instance.CentComm.CurrentAlertLevel.ToString().ToUpper());
		NewAlertLevelLabel.SetValueServer(LocalAlertLevel.ToString().ToUpper());
	}
	public void ChangeAlertLevel()
	{
		if (GameManager.Instance.stationTime < GameManager.Instance.CentComm.lastAlertChange.AddMinutes(
			GameManager.Instance.CentComm.coolDownAlertChange))
		{
			StartCoroutine(DisplayAlertErrorMessage("Error: recent alert level change detected!"));
			return;
		}

		Logger.Log( nameof(ChangeAlertLevel), Category.NetUI );
		GameManager.Instance.CentComm.lastAlertChange = GameManager.Instance.stationTime;
		GameManager.Instance.CentComm.ChangeAlertLevel(LocalAlertLevel);

		OpenMenu();
	}

	IEnumerator DisplayAlertErrorMessage(string text)
	{
		AlertErrorLabel.SetValueServer(text);
		for (int _i = 0; _i < 5; _i++)
		{
			yield return WaitFor.Seconds(1);
			AlertErrorLabel.SetValueServer("");
			yield return WaitFor.Seconds(1);
			AlertErrorLabel.SetValueServer(text);
		}
		AlertErrorLabel.SetValueServer("");
		yield break;
	}

	public void SelectAlertLevel(string levelName)
	{
		//TODO require 2 ID's to change to red level
		LocalAlertLevel =
			(CentComm.AlertLevel)Enum.Parse(typeof(CentComm.AlertLevel), levelName);
	}

	public void RequestNukeCodes()
	{
		//todo
		Logger.Log( nameof(RequestNukeCodes), Category.NetUI );
	}

	public void RemoveId()
	{
		if (console.IdCard)
		{
			console.ServerRemoveIDCard();
		}
		CloseTab();
	}

	public void UpdateIdTexts()
	{
		var IdCard = console.IdCard;
		if (IdCard)
		{
			idLabel.SetValueServer($"{IdCard.RegisteredName}, {IdCard.JobType.ToString()}");
		}
		else
		{
			idLabel.SetValueServer("<No ID inserted>");
		}
	}

	public void LogIn()
	{
		if (console.IdCard == null)
		{
			return;
		}

		if ( !console.IdCard.HasAccess(Access.heads) )
		{
			idLabel.SetValueServer(idLabel.Value + " (No access)");
			return;
		}

		bool isCaptain = console.IdCard.HasAccess(Access.captain);
		captainOnlySwitcher.SetActivePage( isCaptain ? captainAccessPage : noCaptainAccessPage );

		OpenMenu();
	}

	public void LogOut()
	{
		mainSwitcher.SetActivePage(mainSwitcher.DefaultPage);
		UpdateIdTexts();
	}

	public void OpenMenu()
	{
		mainSwitcher.SetActivePage(menuPage);
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}