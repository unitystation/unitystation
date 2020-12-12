using System;
using UnityEngine;
using Mirror;

public class Paper : NetworkBehaviour
{
	public string ServerString { get; private set; }

	///<Summary>
	/// Synced individually via NetMsg for each client that has permission to view it
	///</Summary>
	public string PaperString { get; set; } = "";

	private Pickupable pickupable;
	private SpriteHandler spriteHandler;

	private enum SpriteState
	{
		Blank = 0,
		WithText = 1,
	}

	private void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (pickupable != null) return;
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
		EnsureInit();
		spriteHandler.ChangeSprite((int) state);
	}
}
