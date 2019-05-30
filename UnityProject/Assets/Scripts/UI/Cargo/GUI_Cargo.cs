﻿using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUI_Cargo : NetTab
{
	private NetLabel creditsText;
	private NetLabel СreditsText => creditsText ? creditsText : creditsText = this["HeaderCredits"] as NetLabel;
	private NetLabel directoryText;
	private NetLabel DirectoryText => directoryText ? directoryText : directoryText = this["HeaderDirectory"] as NetLabel;
	private NetPageSwitcher nestedSwitcher;
	private NetPageSwitcher NestedSwitcher => nestedSwitcher ? nestedSwitcher : nestedSwitcher = this["ScreenBounds"] as NetPageSwitcher;

	protected override void InitServer()
	{
		CargoManager.Instance.LoadData();
		NestedSwitcher.OnPageChange.AddListener(RefreshSubpage);
		CargoManager.Instance.OnCreditsUpdate.AddListener(UpdateCreditsText);
		foreach (NetPage page in NestedSwitcher.Pages)
		{
			page.GetComponent<GUI_CargoPage>().Init();
		}
		UpdateCreditsText();
	}

	public override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(WaitForProvider());
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}
	}


	public void RefreshSubpage(NetPage oldPage, NetPage newPage)
	{
		DirectoryText.SetValue = newPage.GetComponent<GUI_CargoPage>().DirectoryName;
	}

	private void UpdateCreditsText()
	{
		СreditsText.SetValue = "Budget: " + CargoManager.Instance.Credits.ToString();
		СreditsText.ExecuteServer();
	}

	public void CallShuttle()
	{
		CargoManager.Instance.CallShuttle();
	}

	public void OpenTab(NetPage pageToOpen)
	{
		NestedSwitcher.SetActivePage(pageToOpen);
		pageToOpen.GetComponent<GUI_CargoPage>().OpenTab();
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}
