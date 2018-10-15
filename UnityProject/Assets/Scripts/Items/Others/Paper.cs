using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Paper : NetworkBehaviour
{
	public Sprite[] spriteStates; //0 = No text, 1 = text

	[SyncVar(hook="UpdateState")][Range(0, 1)]
	public int spriteState;

	private string ServerString = "";

	///<Summary>
	/// Synced individually via NetMsg for each client that has permission to view it
	///</Summary>
	public string PaperString { get; set; } = "";
	public SpriteRenderer spriteRenderer;

	[Server]
	public void ServerSetString(string msg)
	{
		ServerString = msg;
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
	}
}