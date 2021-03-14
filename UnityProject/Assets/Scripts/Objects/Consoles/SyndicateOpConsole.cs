using UnityEngine;
using Managers;
using Strings;
using System;

public class SyndicateOpConsole : MonoBehaviour
{

	public static SyndicateOpConsole Instance;

	public event Action OnTimerExpired;

	public int TcReserve;

	private bool warDeclared = false;
	private bool rewardGiven = false;
	private int timer = 1200;
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
		}
	}

	public void ServerUpdateTimer()
	{
		if (warDeclared == false || rewardGiven) return;

		if (timer > 0)
		{
			timer--;
			return;
		}
		rewardGiven = true;
		RewardTelecrystals();

	}

	public void RewardTelecrystals()
	{
		TcReserve = 280;
		OnTimerExpired?.Invoke();
	}
}
