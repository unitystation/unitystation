using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Paper : NetworkBehaviour
{
	public Sprite[] spriteStates; //0 = No text, 1 = text

	[SyncVar(hook = "UpdateState")][Range(0, 1)]
	public int spriteState;

	public string ServerString { get; private set; }

	///<Summary>
	/// Synced individually via NetMsg for each client that has permission to view it
	///</Summary>
	public string PaperString { get; set; } = "";
	public SpriteRenderer spriteRenderer;

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
		UpdateState(spriteState);
	}

	[Server]
	public void UpdatePlayer(GameObject recipient)
	{
		PaperUpdateMessage.Send(recipient, gameObject, ServerString);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(spriteState);
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
	}

	public void UpdateState(int i)
	{
		spriteState = i;
		spriteRenderer.sprite = spriteStates[i];

		if (UIManager.Hands.CurrentSlot == null)
		{
			return;
		}

		if (UIManager.Hands.CurrentSlot.Item == gameObject)
		{
			UIManager.Hands.CurrentSlot.image.sprite = spriteStates[spriteState];
		}

		if (UIManager.Hands.OtherSlot.Item == gameObject)
		{
			UIManager.Hands.OtherSlot.image.sprite = spriteStates[spriteState];
		}
	}
}