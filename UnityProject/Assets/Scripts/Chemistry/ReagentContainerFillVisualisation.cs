using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ReagentContainer))]
public class ReagentContainerFillVisualisation : NetworkBehaviour, IServerSpawn
{
	/// <summary>
	/// Stores all information about visual state of container
	/// </summary>
	protected struct VisualState
	{
		public Color mixColor;
		public float fillPercent;
	}

	[Tooltip("The render that shows fill of the container")]
	public SpriteRenderer fillSpriteRender;

	/// <summary>
	/// Sync visual state (color, fill volume) from server to cliens
	/// </summary>
	[SyncVar(hook = "OnVisualStateChanged")]
	private VisualState visualState;

	private ReagentContainer serverContainer;

	private void Start()
	{
		if (CustomNetworkManager.IsServer)
		{
			serverContainer = GetComponent<ReagentContainer>();
			if (serverContainer)
			{
				serverContainer.OnReagentMixChanged.AddListener(ServerUpdateFillState);
			}
		}
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		// Need to update sprite on spawn
		ServerUpdateFillState();
	}

	[Server]
	private void ServerUpdateFillState()
	{
		var fillPercent = serverContainer.GetFillPercent();
		var mixColor = serverContainer.GetMixColor();

		visualState = new VisualState()
		{
			fillPercent = fillPercent,
			mixColor = mixColor
		};
	}

	[Client]
	private void OnVisualStateChanged(VisualState oldState, VisualState newState)
	{

	}
}
