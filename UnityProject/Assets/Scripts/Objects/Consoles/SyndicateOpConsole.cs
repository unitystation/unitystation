using UnityEngine;
using Managers;
using Antagonists;
using Strings;
using System;
using System.Collections.Generic;

public class SyndicateOpConsole : MonoBehaviour
{


	public static SyndicateOpConsole Instance;

	public int TcReserve;

	public int TcIncrement = 14;

	private bool warDeclared = false;
	private bool rewardGiven = false;
	private int timer = 1200;

	private int timerIncrement = 60;
	private int tcToGive = 280;

	public List<SpawnedAntag> Operatives = new List<SpawnedAntag>();

	public int Timer => timer;

	private void Awake()
	{
		if ( Instance == null )
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}


	private void OnEnable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Add(ServerUpdateTimer, 1f);
		}
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerUpdateTimer);
		}
	}


	public void AnnounceWar(string DeclerationMessage)
	{
		if (warDeclared == false)
		{
			warDeclared = true;

			GameManager.Instance.CentComm.ChangeAlertLevel(CentComm.AlertLevel.Red, true);
			CentComm.MakeAnnouncement(ChatTemplates.PriorityAnnouncement, 
			$"Attention all crew! An open message from the syndicate has been picked up on local radiowaves! Message Reads:\n" +
			$"{DeclerationMessage}" ,CentComm.UpdateSound.Alert);

			var antagplayers = AntagManager.Instance.CurrentAntags;

			foreach (var antag in antagplayers )
			{
				if (antag.Antagonist.AntagJobType == JobType.SYNDICATE)
				{
					Operatives.Add(antag);
				}
			}
		}
	}

	public void ServerUpdateTimer()
	{
		if (warDeclared == false || rewardGiven) return;

		if (timerIncrement > 0 || timer > 0)
		{
			timerIncrement--;
			timer--;
		}
		else
		{
			timerIncrement = 60;
			RewardTelecrystals();
		}
	}
	public void RewardTelecrystals()
	{
		if (tcToGive <= TcIncrement) rewardGiven = true;
		if (tcToGive >= TcIncrement)
		{

			TcReserve += TcIncrement;
			tcToGive -= TcIncrement;
		}
		if (tcToGive < TcIncrement)
		{
			TcReserve += tcToGive;
			tcToGive = 0;
		}
	}
}
