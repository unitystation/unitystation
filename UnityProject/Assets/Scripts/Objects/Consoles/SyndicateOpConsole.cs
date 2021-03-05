using UnityEngine;
using Managers;
using Strings;
using System;

public class SyndicateOpConsole : MonoBehaviour
{

	public static SyndicateOpConsole Instance;

	public event Action OnTimerExpired;

	public int TcReserve;

	private int timer;
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

	private void OnDisable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerUpdateTimer);
		}
	}


	public void AnnounceWar(string DeclerationMessage)
	{
		GameManager.Instance.CentComm.ChangeAlertLevel(CentComm.AlertLevel.Red, true);
		CentComm.MakeAnnouncement(ChatTemplates.PriorityAnnouncement, 
		$"Attention all crew! An open message from the syndicate has been picked up on local radiowaves! Message Reads:\n" +
		$"{DeclerationMessage}" ,CentComm.UpdateSound.Alert);

		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Add(ServerUpdateTimer, 60f);
		}
	}

	public void ServerUpdateTimer()
	{
		if (timer < 20)
		{
			timer++;
			return;
		}
		RewardTelecrystals();
		this.OnDisable();
	}

	public void RewardTelecrystals()
	{
		TcReserve = 280;
		OnTimerExpired?.Invoke();
	}
}
