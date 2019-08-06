using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUICommunications : NetTab
{
	[SerializeField]
	private NetPageSwitcher nestedSwitcher;
	[SerializeField]
	private NetLabel idText;
	private CommConsole console;

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

		console = Provider.GetComponentInChildren<CommConsole>();
		console.OnConsoleUpdate.AddListener (UpdateScreen);
		UpdateScreen();
	}

	public void UpdateScreen()
	{
		UpdateIdText(idText);
	}

	public void RemoveId()
	{
		if (console.IdCard)
		{
			console.RemoveID();
			UpdateScreen();
		}
	}

	public void UpdateIdText(NetLabel labelToSet)
	{
		var IdCard = console.IdCard;
		if (IdCard)
		{
			labelToSet.SetValue = $"{IdCard.RegisteredName}, {IdCard.GetJobType.ToString()}";
		}
		else
		{
			labelToSet.SetValue = "********";
		}
	}

	public void LogIn()
	{
		if (console.IdCard == null || !console.IdCard.accessSyncList.Contains((int) Access.security)) 
		{
			return;
		}
	}

	public void LogOut()
	{
		nestedSwitcher.SetActivePage(nestedSwitcher.DefaultPage);
		UpdateIdText(idText);
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}
