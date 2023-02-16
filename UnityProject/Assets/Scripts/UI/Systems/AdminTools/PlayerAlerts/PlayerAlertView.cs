using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using AdminTools;
using Messages.Client.Admin;
using Mirror;
using UnityEngine.UI;


public class PlayerAlertView : ChatEntryView, IDisposable
{
	private PlayerAlertData playerAlertData;
	public PlayerAlertData LoadedData => playerAlertData;
	[SerializeField] private Button gibButton = null;
	[SerializeField] private Button takenCareOfButton = null;
	[SerializeField] private Button teleportButton = null;
	private CancellationTokenSource cancelSource = new CancellationTokenSource();

	public override void SetChatEntryView(ChatEntryData data, ChatScroll chatScroll, int index, float contentViewWidth)
	{
		base.SetChatEntryView(data, chatScroll, index, contentViewWidth);
		playerAlertData = (PlayerAlertData)data;
		gibButton.interactable = !playerAlertData.gibbed;
		takenCareOfButton.interactable = !playerAlertData.takenCareOf;
	}

	public void Reload(PlayerAlertData playerAlert)
	{
		playerAlertData = playerAlert;
		gibButton.interactable = !playerAlertData.gibbed;
		takenCareOfButton.interactable = !playerAlertData.takenCareOf;
	}

	private void OnDisable()
	{
		if (cancelSource.Token != null)
		{
			cancelSource.Cancel();
		}
	}

	public void GibRequest()
	{
		AdminPlayerAlertActions.Send(PlayerAlertActions.Gibbed, playerAlertData.roundTime, playerAlertData.playerNetId, PlayerList.Instance.AdminToken);
		takenCareOfButton.interactable = false;
	}

	public void TeleportTo()
	{
		if (PlayerManager.LocalPlayerScript == null) return;

		var spawned =
			CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

		var target = spawned[playerAlertData.playerNetId];
		if (target == null) return;

		if (PlayerManager.LocalPlayerScript.IsGhost == false)
		{
			teleportButton.interactable = false;
			PlayerManager.LocalMindScript.CmdAGhost();
			cancelSource = new CancellationTokenSource();
			StartCoroutine(GhostWait(target.gameObject, cancelSource.Token));
		}
		else
		{
			PlayerManager.LocalPlayerObject.GetComponent<GhostMove>()
				.CMDSetServerPosition(target.transform.position);
		}
	}

	IEnumerator GhostWait(GameObject target, CancellationToken cancelToken)
	{
		var timeOutCount = 0f;
		while (PlayerManager.LocalPlayerScript != null && !PlayerManager.LocalPlayerScript.IsGhost)
		{
			timeOutCount += Time.deltaTime;
			if (timeOutCount > 5f || cancelToken.IsCancellationRequested)
			{
				teleportButton.interactable = true;
				yield break;
			}
			yield return WaitFor.EndOfFrame;
		}

		teleportButton.interactable = true;
		if (PlayerManager.LocalPlayerScript != null && target != null && PlayerManager.LocalPlayerScript.IsGhost)
		{
			PlayerManager.LocalPlayerObject.GetComponent<GhostMove>().CMDSetServerPosition(target.transform.position);
		}
	}

	public void TakenCareOf()
	{
		AdminPlayerAlertActions.Send(PlayerAlertActions.TakenCareOf, playerAlertData.roundTime, playerAlertData.playerNetId, PlayerList.Instance.AdminToken);
		takenCareOfButton.interactable = false;
	}

	public void Dispose()
	{
		cancelSource?.Dispose();
	}
}
