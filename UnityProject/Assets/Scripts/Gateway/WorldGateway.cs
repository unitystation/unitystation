using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


/// <summary>
/// Connects two portals together, can be separate from the station gateway.
/// </summary>
public class WorldGateway : NetworkBehaviour
{
	[SerializeField]
	private SpriteRenderer SpriteBaseTop;
	[SerializeField]
	private SpriteRenderer SpriteBaseBottom;
	[SerializeField]
	private SpriteRenderer SpriteBaseCentre;// TODO animation of centre piece
	[SerializeField]
	private SpriteRenderer SpriteBaseRightTop;
	[SerializeField]
	private SpriteRenderer SpriteBaseRightMiddle;
	[SerializeField]
	private SpriteRenderer SpriteBaseRightBottom;
	[SerializeField]
	private SpriteRenderer SpriteBaseLeftTop;
	[SerializeField]
	private SpriteRenderer SpriteBaseLeftMiddle;
	[SerializeField]
	private SpriteRenderer SpriteBaseLeftBottom;

	[SerializeField]
	private Sprite[] Online;
	[SerializeField]
	private Sprite[] Offline;
	[SerializeField]
	private Sprite[] PowerOff;

	[SerializeField]
	private GameObject StationGateway;// doesnt have to be station just the gateway this one will connect to

	public bool IsOnlineAtStart = false;

	private RegisterTile registerTile;

	private Matrix Matrix => registerTile.Matrix;

	public string WorldName = "Unknown";
	//Displayed when teleporting

	private Vector3Int Position;

	private string Message;

	[Server]
	private void Start()
	{
		if (StationGateway == null) return;

		registerTile = GetComponent<RegisterTile>();
		SetOffline();

		Position = registerTile.WorldPosition;
		Message = "Teleporting to: " + StationGateway.GetComponent<StationGateway>().WorldName;

		if (IsOnlineAtStart == false)
		{
			gameObject.SetActive(false);
		}

		if (IsOnlineAtStart == true && StationGateway != null)
		{
			SetOnline();
			loop();
		}
	}

	[Server]
	public void SetUp()
	{
		if (IsOnlineAtStart && StationGateway != null)
		{
			SetOnline();
			loop();
		}
	}

	[Server]
	private void loop()
	{
		DetectPlayer();
		Invoke("loop", 2f);
	}

	[Server]
	private void DetectPlayer()
	{
		var playersFound = Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Player, true);

		foreach (ObjectBehaviour player in playersFound)
		{
			var coord = new Vector2(Position.x, Position.y);
			Chat.AddLocalMsgToChat(Message, coord, gameObject);
			SoundManager.PlayNetworkedForPlayer(player.gameObject, "StealthOff");//very weird, sometimes does the sound other times not.
			TransportPlayers(player);
		}

		foreach (var objects in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Object, true))
		{
			TransportObjects(objects);
		}

		foreach (var items in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Item, true))
		{
			TransportItems(items);
		}
	}

	[Server]
	private void TransportPlayers(ObjectBehaviour player)
	{
		player.GetComponent<PlayerSync>().SetPosition(StationGateway.GetComponent<RegisterTile>().WorldPosition, false);
	}

	[Server]
	private void TransportObjects(ObjectBehaviour objects)
	{
		objects.GetComponent<CustomNetTransform>().SetPosition(StationGateway.GetComponent<RegisterTile>().WorldPosition);
	}

	[Server]
	private void TransportItems(ObjectBehaviour items)
	{
		items.GetComponent<CustomNetTransform>().SetPosition(StationGateway.GetComponent<RegisterTile>().WorldPosition);
	}

	private void SetOnline()
	{
		SpriteBaseTop.sprite = Online[1];
		SpriteBaseBottom.sprite = Online[0];
		SpriteBaseRightMiddle.sprite = Online[2];
		SpriteBaseLeftMiddle.sprite = Online[3];
		SpriteBaseRightBottom.sprite = Online[4];
		SpriteBaseLeftBottom.sprite = Online[5];
		SpriteBaseRightTop.sprite = Online[6];
		SpriteBaseLeftTop.sprite = Online[7];
		SpriteBaseCentre.sprite = Online[8];
	}

	private void SetOffline()
	{
		SpriteBaseTop.sprite = Offline[1];
		SpriteBaseBottom.sprite = Offline[0];
		SpriteBaseRightMiddle.sprite = Offline[2];
		SpriteBaseLeftMiddle.sprite = Offline[3];
		SpriteBaseRightBottom.sprite = Offline[4];
		SpriteBaseLeftBottom.sprite = Offline[5];
		SpriteBaseRightTop.sprite = Offline[6];
		SpriteBaseLeftTop.sprite = Offline[7];
		SpriteBaseCentre.sprite = Offline[8];
	}

	private void SetPowerOff()
	{
		SpriteBaseTop.sprite = PowerOff[1];
		SpriteBaseBottom.sprite = PowerOff[0];
		SpriteBaseRightMiddle.sprite = PowerOff[2];
		SpriteBaseLeftMiddle.sprite = PowerOff[3];
		SpriteBaseRightBottom.sprite = PowerOff[4];
		SpriteBaseLeftBottom.sprite = PowerOff[5];
		SpriteBaseRightTop.sprite = PowerOff[6];
		SpriteBaseLeftTop.sprite = PowerOff[7];
		SpriteBaseCentre.sprite = PowerOff[8];
	}
}
