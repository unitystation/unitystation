﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//This is for the first nuke ops demo:
public class GUI_NukeOps : MonoBehaviour
{

	private int syndiActive;
	private int nanoActive;

	public Text nukeOpsCount;
	public Text nanoCount;

	public Button nukeOpsbtn;


	void Update()
	{
		syndiActive = GameManager.Instance.GetOccupationsCount(JobType.SYNDICATE);
		nanoActive = GameManager.Instance.GetNanoTrasenCount();
		UpdateCounts();
		SyndiesAllowed();
	}

	bool SyndiesAllowed()
	{

		if (syndiActive > 6)
		{
			nukeOpsbtn.interactable = false;
			return false;
		}
		else
		{
			nukeOpsbtn.interactable = true;;
			return true;
		}
	}

	void UpdateCounts()
	{
		nukeOpsCount.text = "Nukes Ops: " + syndiActive;
		nanoCount.text = "Station Crew: " + nanoActive;
	}

	public void NanoTrasenBtn()
	{
		UIManager.Display.jobSelectWindow.SetActive(true);
		gameObject.SetActive(false);
	}

	public void SyndieBtn()
	{
		PlayerManager.LocalViewerScript.RequestJob(JobType.SYNDICATE);
		gameObject.SetActive(false);
	}
}