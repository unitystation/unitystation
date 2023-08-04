using System;
using Messages.Server;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using WebSocketSharp;

public class Paper : NetworkBehaviour, IServerSpawn
{
	[SerializeField]
	[TextArea]
	[Tooltip("Text that this paper will have on spawn, useful for mapping in little bits of " +
	         "lore.")]
	private string initialText;
	public string ServerString { get; private set; }

	///<Summary>
	/// Synced individually via NetMsg for each client that has permission to view it
	///</Summary>
	public string PaperString { get; set; } = "";

	private Pickupable pickupable;
	private SpriteHandler spriteHandler;

	[field: SerializeField, InfoBox("Only change this if you require more than 22 lines as a mapper!", EInfoBoxType.Warning)]
	public int CustomLineLimit { get; private set; } = 22;
	[field: SerializeField, InfoBox("Values higher than 1750 tend to be problematic without proper styling!", EInfoBoxType.Warning)]
	public int CustomCharacterLimit { get; private set; } = 1750;

	private enum SpriteState
	{
		Blank = 0,
		WithText = 1,
	}

	private void Awake()
	{
		pickupable = GetComponent<Pickupable>();
		spriteHandler = GetComponentInChildren<SpriteHandler>();
	}

	[Server]
	public void SetServerString(string msg)
	{
		ServerString = msg;
		if (string.IsNullOrWhiteSpace(msg))
		{
			UpdateState(SpriteState.Blank);
		}
		else
		{
			UpdateState(SpriteState.WithText);
		}
	}

	[Server]
	public void UpdatePlayer(GameObject recipient)
	{
		PaperUpdateMessage.Send(recipient, gameObject, ServerString);
	}

	private void UpdateState(SpriteState state)
	{
		spriteHandler.ChangeSprite((int) state);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		if (initialText.IsNullOrEmpty() == false)
		{
			SetServerString(initialText);
		}
	}
}
