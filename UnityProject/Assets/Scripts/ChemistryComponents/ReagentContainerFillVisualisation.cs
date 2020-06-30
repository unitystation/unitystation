using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Chemistry.Components;

/// <summary>
/// Syncs ReagentContainer visualisation (fill level and content color) between all clients
/// </summary>
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

	[Tooltip("Sorted sprites that represents fill of container")]
	public Sprite[] fillIcons = new Sprite[0];

	/// <summary>
	/// Sync visual state (color, fill volume) from server to cliens
	/// </summary>
	[SyncVar(hook = "OnVisualStateChanged")]
	private VisualState visualState;

	private ReagentContainer serverContainer;
	private Pickupable pickupable;

	private void Awake()
	{
		pickupable = GetComponent<Pickupable>();
		serverContainer = GetComponent<ReagentContainer>();

		if (serverContainer)
		{
			serverContainer.OnReagentMixChanged.AddListener(ServerUpdateFillState);
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

		// Send it to all client by SyncVar
		visualState = new VisualState()
		{
			fillPercent = fillPercent,
			mixColor = mixColor
		};
	}

	[Client]
	private void OnVisualStateChanged(VisualState oldState, VisualState newState)
	{
		if (!fillSpriteRender)
			return;

		// Apply new state to sprite render
		fillSpriteRender.color = newState.mixColor;
		fillSpriteRender.sprite = GetSpriteByFill(newState.fillPercent);

		// Update UI sprite in inventory
		pickupable?.RefreshUISlotImage();
	}

	private Sprite GetSpriteByFill(float fillPercent)
	{
		if (fillIcons.Length == 0)
			return null;

		// check if container is empty
		if (fillPercent <= 0f)
			return null;

		// Get the sprite index
		var step = 1f / fillIcons.Length;
		int index = (int)(fillPercent / step);

		// Return sprite from sprite list
		if (index < fillIcons.Length)
			return fillIcons[index];
		else
			return fillIcons.Last();
	}
}
