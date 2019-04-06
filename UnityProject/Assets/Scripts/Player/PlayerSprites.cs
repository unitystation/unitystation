using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handle displaying the sprites related to player, which includes clothing and the body.
/// Ghosts are handled in GhostSprites.
/// </summary>
public class PlayerSprites : UserControlledSprites
{
	[SyncVar(hook = nameof(UpdateCharacterSprites))]
	private string characterData;

	//For character customization
	public ClothingItem[] characterSprites;

	private PlayerSync playerSync;
	private CharacterSettings characterSettings;
	private PlayerHealth playerHealth;
	//clothes for each clothing slot
	private readonly Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

	protected override void Awake()
	{
		base.Awake();
		playerSync = GetComponent<PlayerSync>();
		foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>())
		{
			clothes[c.name] = c;
		}

		playerHealth = GetComponent<PlayerHealth>();
	}

	[Command]
	private void CmdUpdateCharacter(string data)
	{
		var character = JsonUtility.FromJson<CharacterSettings>(data);
		//Remove sensitive data:
		character.username = "";
		characterSettings = character;
		characterData = JsonUtility.ToJson(character);
	}
	private void UpdateCharacterSprites(string data)
	{

		var character = JsonUtility.FromJson<CharacterSettings>(data);
		characterSettings = character;

		Color newColor = Color.white;

		//Skintone:
		ColorUtility.TryParseHtmlString(characterSettings.skinTone, out newColor);

		for (int i = 0; i < characterSprites.Length; i++)
		{
			characterSprites[i].spriteRenderer.color = newColor;
			if (i == 6)
			{
				break;
			}
		}
		//Torso
		characterSprites[0].reference = characterSettings.torsoSpriteIndex;
		characterSprites[0].UpdateSprite();
		//Head
		characterSprites[5].reference = characterSettings.headSpriteIndex;
		characterSprites[5].UpdateSprite();
		//Eyes
		ColorUtility.TryParseHtmlString(characterSettings.eyeColor, out newColor);
		characterSprites[6].spriteRenderer.color = newColor;
		//Underwear
		characterSprites[7].reference = characterSettings.underwearOffset;
		characterSprites[7].UpdateSprite();
		//Socks
		characterSprites[8].reference = characterSettings.socksOffset;
		characterSprites[8].UpdateSprite();
		//Beard
		characterSprites[9].reference = characterSettings.facialHairOffset;
		characterSprites[9].UpdateSprite();
		ColorUtility.TryParseHtmlString(characterSettings.facialHairColor, out newColor);
		characterSprites[9].spriteRenderer.color = newColor;
		//Hair
		characterSprites[10].reference = characterSettings.hairStyleOffset;
		characterSprites[10].UpdateSprite();
		ColorUtility.TryParseHtmlString(characterSettings.hairColor, out newColor);
		characterSprites[10].spriteRenderer.color = newColor;
	}

	protected override IEnumerator WaitForLoad()
	{
		yield return YieldHelper.EndOfFrame;
		if (PlayerManager.LocalPlayer == gameObject)
		{
			CmdUpdateCharacter(JsonUtility.ToJson(PlayerManager.CurrentCharacterSettings));
			LocalFaceDirection( currentDirection );
		}
		while(string.IsNullOrEmpty(characterData)){
			yield return YieldHelper.DeciSecond;
		}
		FaceDirectionSync(currentDirection);
		if (PlayerManager.LocalPlayer != gameObject)
		{
			UpdateCharacterSprites(characterData);
		}
	}

	/// <summary>
	/// Change current facing direction to match direction (it's a Command so it's invoked on the server by
	/// the server itself or the client)
	/// </summary>
	/// <param name="direction">new direction</param>
	[Command]
	private void CmdChangeDirection(Orientation direction)
	{
		LocalFaceDirection(direction);
	}

	/// <summary>
	/// Locally changes the direction of this player to face the specified direction but doesn't tell the server.
	/// If this is a client, only changes the direction locally and doesn't inform other players / server.
	/// If this is on the server, the direction change will be sent to all clients due to the syncvar.
	///
	/// Does nothing if player is down
	/// </summary>
	/// <param name="direction"></param>
	public override void LocalFaceDirection(Orientation direction)
	{
		if (registerPlayer.IsDown || playerSync.isBumping)
		{
			//Don't face while bumping is occuring on this frame
			//or when player is down
			return;
		}

		SetDir(direction);
	}

	/// <summary>
	/// Does nothing if this is the local player (unless player is in crit).
	///
	/// Invoked when currentDirection syncvar changes. Update the direction of this player to face the specified
	/// direction. However, if this is the local player's body that is not in crit or a player being pulled by the local player,
	/// nothing is done and we stick with whatever direction we had already set for them locally (this is to avoid
	/// glitchy changes in facing direction caused by latency in the syncvar).
	/// </summary>
	/// <param name="dir"></param>
	protected override void FaceDirectionSync(Orientation dir)
	{
//		//don't sync facing direction for players you're pulling locally, unless you're standing still
		PushPull localPlayer = PlayerManager.LocalPlayerScript ? PlayerManager.LocalPlayerScript.pushPull : null;
		if ( localPlayer && localPlayer.Pushable != null && localPlayer.Pushable.IsMovingClient )
		{
			if ( playerMove && playerMove.PlayerScript && playerMove.PlayerScript.pushPull
			     && playerMove.PlayerScript.pushPull.IsPulledByClient( localPlayer ) ) {
				return;
			}
		}

		//check if we are crit, or else our direction might be out of sync with the server
		if (PlayerManager.LocalPlayer != gameObject || playerHealth.IsCrit || playerHealth.IsSoftCrit)
		{
			currentDirection = dir;
			SetDir(dir);
		}
	}

	/// <summary>
	/// Updates the direction of the body / clothing sprites.
	/// </summary>
	/// <param name="direction"></param>
	private void SetDir(Orientation direction)
	{
		foreach (ClothingItem c in clothes.Values)
		{
			c.Direction = direction;
		}

		currentDirection = direction;
	}

	/// <summary>
	/// Overrides the local client prediction, forces the sprite to update based on the latest info we have from
	/// the server.
	/// </summary>
	/// <exception cref="NotImplementedException"></exception>
	public override void SyncWithServer()
	{
		SetDir(currentDirection);
	}
}