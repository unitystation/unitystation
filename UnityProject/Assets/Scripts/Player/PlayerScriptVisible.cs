using System;
using Logs;
using Mirror;
using Systems.Character;
using UnityEngine;

public class PlayerScriptVisible : NetworkBehaviour
{
	public PlayerScript PlayerScript;

	public void Awake()
	{
		PlayerScript ??= this.GetComponentCustom<PlayerScript>();
	}

	/// <summary>
	/// Current character settings for this player.
	/// </summary>
	[SyncVar, NonSerialized] public CharacterSheet characterSettings = new CharacterSheet();

	[HideInInspector, SyncVar(hook = nameof(SyncPlayerName))]
	public string playerName = " ";

	[HideInInspector, SyncVar(hook = nameof(SyncVisibleName))]
	public string visibleName = " ";

	public void SyncPlayerName(string oldValue, string value)
	{
		playerName = value;
		gameObject.name = value;
		PlayerScript.RefreshVisibleName();
	}

	// Syncvisiblename
	public void SyncVisibleName(string oldValue, string value)
	{
		visibleName = value;
		try
		{
			PlayerScript.SetVisibleName();
		}
		catch (Exception e)
		{
			Loggy.Error(e.ToString());
		}

	}

	public void SetcharacterSettings(CharacterSheet newCharacterSheet )
	{
		characterSettings = newCharacterSheet;
	}


}
