using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUI_Comms : NetTab
{
	[SerializeField]
	private NetPageSwitcher nestedSwitcher;
	[SerializeField]
	private NetPage menuPage;
	[SerializeField]
	private NetLabel[] idTexts;
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
		//todo
		Logger.Log( nameof(MakeAnAnnouncement), Category.NetUI );
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
		foreach ( var labelToSet in idTexts )
		{
			if (IdCard)
			{
				labelToSet.SetValue = $"{IdCard.RegisteredName}, {IdCard.GetJobType.ToString()}";
			}
			else
			{
				labelToSet.SetValue = "********";
			}
		}
	}

	public void LogIn()
	{
		if (console.IdCard == null || !console.IdCard.accessSyncList.Contains((int) Access.security))
		{
			return;
		}

		OpenMenu();
	}

	public void LogOut()
	{
		nestedSwitcher.SetActivePage(nestedSwitcher.DefaultPage);
		UpdateIdTexts();
	}

	public void OpenMenu()
	{
		nestedSwitcher.SetActivePage(menuPage);
	}


	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}