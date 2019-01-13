using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerSprites : NetworkBehaviour
{
	private readonly Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();
	[SyncVar(hook = nameof(FaceDirectionSync))]
	public Orientation currentDirection;
	[SyncVar(hook = nameof(UpdateCharacterSprites))]
	private string characterData;

	public PlayerMove playerMove;

	public ClothingItem[] characterSprites; //For character customization
	private CharacterSettings characterSettings;

	private SpriteRenderer ghostRenderer; //For ghost sprites
	private readonly Dictionary<Orientation, Sprite> ghostSprites = new Dictionary<Orientation, Sprite>();

	private void Awake()
	{
		foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>())
		{
			clothes[c.name] = c;
		}

		ghostSprites.Add(Orientation.Down, SpriteManager.PlayerSprites["mob"][268]);
		ghostSprites.Add(Orientation.Up, SpriteManager.PlayerSprites["mob"][269]);
		ghostSprites.Add(Orientation.Right, SpriteManager.PlayerSprites["mob"][270]);
		ghostSprites.Add(Orientation.Left, SpriteManager.PlayerSprites["mob"][271]);

		ghostRenderer = transform.Find("Ghost").GetComponent<SpriteRenderer>();
	}

	public override void OnStartServer()
	{
		FaceDirection(Orientation.Down);
		base.OnStartServer();
	}

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	[Command]
	public void CmdUpdateCharacter(string data)
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

	private IEnumerator WaitForLoad()
	{
		yield return YieldHelper.EndOfFrame;
		if (PlayerManager.LocalPlayer == gameObject)
		{
			CmdUpdateCharacter(JsonUtility.ToJson(PlayerManager.CurrentCharacterSettings));
			FaceDirection( currentDirection );
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

	public void AdjustSpriteOrders(int offsetOrder)
	{
		foreach (SpriteRenderer s in GetComponentsInChildren<SpriteRenderer>())
		{
			int newOrder = s.sortingOrder;
			newOrder += offsetOrder;
			s.sortingOrder = newOrder;
		}
	}

	[Command]
	public void CmdChangeDirection(Orientation direction)
	{
		FaceDirection(direction);
	}

	//turning character input and sprite update for local only! (prediction)
	public void FaceDirection(Orientation direction)
	{
		SetDir(direction);
	}

	//For syncing all other players (not locally owned)
	private void FaceDirectionSync(Orientation dir)
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

		if (PlayerManager.LocalPlayer != gameObject)
		{
			currentDirection = dir;
			SetDir(dir);
		}
	}

	public void SetDir(Orientation direction)
	{
		if (playerMove.isGhost)
		{
			ghostRenderer.sprite = ghostSprites[direction];
			currentDirection = direction;
			return;
		}

		foreach (ClothingItem c in clothes.Values)
		{
			c.Direction = direction;
		}

		currentDirection = direction;
	}
	///For falling over and getting back up again over network
	[ClientRpc]
	public void RpcSetPlayerRot(float rot)
	{
		//		Logger.LogWarning("Setting TileType to none for player and adjusting sortlayers in RpcSetPlayerRot");
		SpriteRenderer[] spriteRends = GetComponentsInChildren<SpriteRenderer>();
		foreach (SpriteRenderer sR in spriteRends)
		{
			sR.sortingLayerName = "Blood";
		}
		gameObject.GetComponent<RegisterPlayer>().IsBlocking = false;
		gameObject.GetComponent<ForceRotation>().Rotation.eulerAngles = new Vector3(0, 0, rot);

		//Might be no longer needed as all spriteRenderers are set to Blood layer
		// if ( Math.Abs( rot ) > 0 ) {
		// 	//So other players can walk over the Unconscious
		// 	AdjustSpriteOrders(-30);
		// }
	}

	/// Changes direction by degrees; positive = CW, negative = CCW
	public void ChangePlayerDirection(int degrees)
	{
		for (int i = 0; i < Math.Abs(degrees / 90); i++)
		{
			if (degrees < 0)
			{
				ChangePlayerDirection(currentDirection.Previous());
			}
			else
			{
				ChangePlayerDirection(currentDirection.Next());
			}
		}
	}

	public void ChangePlayerDirection(Orientation orientation)
	{
		CmdChangeDirection(orientation);
		//Prediction
		FaceDirection(orientation);
	}
}