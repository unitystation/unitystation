using System;
using UnityEngine;
using Mirror;

public class Paper : NetworkBehaviour
{
	public Sprite[] spriteStates; //0 = No text, 1 = text

	[SyncVar(hook = nameof(UpdateState))][Range(0, 1)]
	public int spriteState;

	public string ServerString { get; private set; }

	///<Summary>
	/// Synced individually via NetMsg for each client that has permission to view it
	///</Summary>
	public string PaperString { get; set; } = "";
	public SpriteRenderer spriteRenderer;

	private Pickupable pickupable;

	private void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (pickupable != null) return;
		pickupable = GetComponent<Pickupable>();
	}

	[Server]
	public void SetServerString(string msg)
	{
		ServerString = msg;
		if (string.IsNullOrWhiteSpace(msg))
		{
			spriteState = 0;
		}
		else
		{
			spriteState = 1;
		}
		UpdateState(spriteState, spriteState);
	}

	[Server]
	public void UpdatePlayer(GameObject recipient)
	{
		PaperUpdateMessage.Send(recipient, gameObject, ServerString);
	}

	public override void OnStartClient()
	{
		EnsureInit();
		UpdateState(spriteState, spriteState);
	}

	public void UpdateState(int oldI, int i)
	{
		EnsureInit();
		spriteState = i;
		spriteRenderer.sprite = spriteStates[i];
		pickupable.LocalUISlot?.RefreshImage();
	}
}