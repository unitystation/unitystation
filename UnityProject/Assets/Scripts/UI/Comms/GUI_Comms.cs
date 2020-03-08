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
	private NetPageSwitcher mainSwitcher;
	[SerializeField]
	private NetPage menuPage;

	[SerializeField]
	private NetPageSwitcher captainOnlySwitcher;
	[SerializeField]
	private NetPage noCaptainAccessPage;
	[SerializeField]
	private NetPage captainAccessPage;

	[SerializeField]
	private NetLabel idLabel;
	[SerializeField]
	private NetLabel shuttleStatusLabel;
	[SerializeField]
	private NetLabel shuttleTimerLabel;
	[SerializeField]
	private NetLabel shuttleCallResultLabel;
	[SerializeField]
	private NetLabel shuttleCallButtonLabel;
	[SerializeField]
	private NetSpriteImage statusImage;

	private CommsConsole console;
	private EscapeShuttle shuttle;
	private Coroutine callResultHandle;

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
			if ( shuttle.Status == ShuttleStatus.DockedCentcom || shuttle.Status == ShuttleStatus.DockedStation )
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

		shuttleStatusLabel.SetValue = shuttle.Status.ToString();
		statusImage.SetComplicatedValue( "shuttle_status", (int)shuttle.Status );
		shuttle.OnShuttleUpdate.AddListener( status =>
		{
			statusImage.SetComplicatedValue( "shuttle_status", (int)status );
			shuttleStatusLabel.SetValue = status.ToString();
		} );

		shuttleTimerLabel.SetValue = FormatTime( shuttle.CurrentTimerSeconds );
		shuttle.OnTimerUpdate.AddListener( timerSeconds =>{ shuttleTimerLabel.SetValue = FormatTime( timerSeconds ); } );

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

		bool isRecall = shuttle.Status == ShuttleStatus.OnRouteStation;

		var minutes = 2;

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
		shuttleCallButtonLabel.SetValue =
			shuttle.Status == ShuttleStatus.OnRouteStation ? "Recall Emergency Shuttle" : "Call Emergency Shuttle";
	}

	private IEnumerator ShowSubmitResult( string callResult )
	{
		shuttleCallResultLabel.SetValue = callResult;
		yield return WaitFor.Seconds( 3 );
		shuttleCallResultLabel.SetValue = String.Empty;
	}

	public void SetStatusDisplay(string text)
	{
		Logger.Log( nameof(SetStatusDisplay), Category.NetUI );
		GameManager.Instance.CentComm.OnStatusDisplayUpdate
			.Invoke( StatusDisplayChannel.Command, text.Substring( 0,Mathf.Min(text.Length, 50) ) );
		OpenMenu();
	}
	public void MakeAnAnnouncement(string text)
	{
		Logger.Log( nameof(MakeAnAnnouncement), Category.NetUI );
		CentComm.MakeAnnouncement(CentComm.CaptainAnnounceTemplate, text, CentComm.UpdateType.announce);
		OpenMenu();
	}
	public void ChangeAlertLevel()
	{
		//todo
		Logger.Log( nameof(ChangeAlertLevel), Category.NetUI );
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
			idLabel.SetValue = $"{IdCard.RegisteredName}, {IdCard.JobType.ToString()}";
		}
		else
		{
			idLabel.SetValue = "<No ID inserted>";
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
			idLabel.SetValue = idLabel.Value + " (No access)";
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