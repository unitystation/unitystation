using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GUI_Comms : NetTab
{
	[FormerlySerializedAs( "nestedSwitcher" )] [SerializeField]
	private NetPageSwitcher switcher;
	[SerializeField]
	private NetPage menuPage;
	[SerializeField]
	private NetPage announcePage;
	[SerializeField]
	private NetLabel idLabel;
	private CommsConsole console;

	public override void OnEnable()
	{
		base.OnEnable();
		if (CustomNetworkManager.Instance._isServer)
		{
			StartCoroutine(WaitForProvider());
		}
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		console = Provider.GetComponentInChildren<CommsConsole>();
		console.IdEvent.AddListener( ProcessIdChange );
	}

	private void ProcessIdChange( IDCard newId )
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

	public void CallShuttle()
	{
		//todo
		Logger.Log( nameof(CallShuttle), Category.NetUI );
	}
	public void SetStatusDisplay()
	{
		//todo
		Logger.Log( nameof(SetStatusDisplay), Category.NetUI );
	}
	public void MakeAnAnnouncement()
	{
		Logger.Log( nameof(MakeAnAnnouncement), Category.NetUI );
		switcher.SetActivePage( announcePage );
	}
	public void MakeAnAnnouncement(string text)
	{
		Logger.Log( nameof(MakeAnAnnouncement), Category.NetUI );
		CentComm.MakeCaptainAnnouncement( text );
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
			console.RemoveID();
		}
		CloseTab();
	}

	public void UpdateIdTexts()
	{
		var IdCard = console.IdCard;
		if (IdCard)
		{
			idLabel.SetValue = $"{IdCard.RegisteredName}, {IdCard.GetJobType.ToString()}";
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

		if ( !console.IdCard.accessSyncList.Contains((int) Access.heads) )
		{
			idLabel.SetValue = idLabel.Value + "(No access)";
			return;
		}

		OpenMenu();
	}

	public void LogOut()
	{
		switcher.SetActivePage(switcher.DefaultPage);
		UpdateIdTexts();
	}

	public void OpenMenu()
	{
		switcher.SetActivePage(menuPage);
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}