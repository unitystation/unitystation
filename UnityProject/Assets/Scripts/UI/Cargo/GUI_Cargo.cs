using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUI_Cargo : NetTab
{
	[SerializeField]
	private Text creditsText = null;
	[SerializeField]
	private Text directoryText = null;

	public override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(WaitForProvider());
		//CargoManager.Instance.OnCreditsUpdate += RefreshTab;
		Debug.Log("Opened");
	}

	private void OnDisable()
	{
		//CargoManager.Instance.OnCreditsUpdate -= RefreshTab;
		Debug.Log("Closed");
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return YieldHelper.EndOfFrame;
		}
		RefreshTab();
	}

	public override void RefreshTab()
	{
		base.RefreshTab();
		//server only
		creditsText.text = "Budget: " + CargoManager.Instance.Credits.ToString();
		//directoryText.text = currentTab.DirectoryName;
	}

	public void CallShuttle()
	{
		CargoManager.Instance.CallShuttle();
		RefreshTab();
	}

	public void CloseTab()
	{
		/*
		foreach (GUI_CargoTab tab in cargoTabs)
		{
			if (tab.gameObject.activeInHierarchy)
			{
				tab.gameObject.SetActive(false);
				tab.OnTabClosed();
			}
		}
		*/
		ControlTabs.CloseTab(Type, Provider);
	}
}