using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// For the station Gate, only connects to a gate this script will pick
/// </summary>
public class StationGateway : NetworkBehaviour
{
	[SerializeField]
	private SpriteRenderer[] Sprites;
	//SpriteBaseBottom, SpriteBaseTop, SpriteBaseRightMiddle, SpriteBaseLeftMiddle, SpriteBaseRightBottom, SpriteBaseLeftBottom, SpriteBaseRightTop, SpriteBaseLeftTop, SpriteBaseCentre
	//TODO animate centre

	[SerializeField]
	private Sprite[] Online;
	[SerializeField]
	private Sprite[] Offline;
	[SerializeField]
	private Sprite[] PowerOff;//TODO connect gateway to APC

	[SerializeField]
	private List<GameObject> Worlds = new List<GameObject>();//List of worlds available to be chosen

	private GameObject SelectedWorld;// The world from the list that was chosen

	private bool HasPower = true;// Not used atm

	private bool IsConnected;

	[SerializeField]
	private int RandomCountBegining = 300; //Defaults to between 5 and 20 mins gate will open.
	[SerializeField]
	private int RandomCountEnd = 1200;

	private RegisterTile registerTile;

	private Matrix Matrix => registerTile.Matrix;

	public string WorldName = "The Station";

	private Vector3Int Position;

	private string Message;

	[Server]
	private void Start()
	{
		registerTile = GetComponent<RegisterTile>();
		Position = registerTile.WorldPosition;

		SetOffline();

		var count = Random.Range(RandomCountBegining, RandomCountEnd);
		Invoke(nameof(WorldSetup), count);
	}

	[Server]
	private void WorldSetup()//do message here as well
	{
		//Selects Random world
		SelectedWorld = Worlds[Random.Range(0, Worlds.Count)];

		if (SelectedWorld == null) return;

		var selectedWorld = SelectedWorld.GetComponent<WorldGateway>();

		Message = "Teleporting to: " + selectedWorld.WorldName;

		if (selectedWorld.IsOnlineAtStart == false)
		{
			selectedWorld.IsOnlineAtStart = true;
			selectedWorld.gameObject.SetActive(true);
			selectedWorld.SetUp();
		}

		if (HasPower == true)
		{
			SetOnline();

			var text = "Alert! New Gateway connection formed.\n\n Connection established to: " + SelectedWorld.GetComponent<WorldGateway>().WorldName;
			CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);

			GateDetectPlayerLoop();
		}
	}

	[Server]
	private void GateDetectPlayerLoop()
	{
		DetectPlayer();
		Invoke(nameof(GateDetectPlayerLoop), 2f);
	}

	[Server]
	private void DetectPlayer()
	{
		//detect players positioned on the portal bit of the gateway
		var playersFound = Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Player, true);

		foreach (ObjectBehaviour player in playersFound)
		{
			var coord = new Vector2(Position.x, Position.y);
			Chat.AddLocalMsgToChat(Message, coord, gameObject);
			SoundManager.PlayNetworkedForPlayer(player.gameObject, "StealthOff"); //very weird, sometimes does the sound other times not.
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
		//teleports player to the front of the new gateway
		player.GetComponent<PlayerSync>().SetPosition(SelectedWorld.GetComponent<RegisterTile>().WorldPosition, false);
	}

	[Server]
	private void TransportObjects(ObjectBehaviour objects)
	{
		objects.GetComponent<CustomNetTransform>().SetPosition(SelectedWorld.GetComponent<RegisterTile>().WorldPosition);
	}

	[Server]
	private void TransportItems(ObjectBehaviour items)
	{
		items.GetComponent<CustomNetTransform>().SetPosition(SelectedWorld.GetComponent<RegisterTile>().WorldPosition);
	}

	private void SetOnline()
	{
		for (int i = 0; i < Sprites.Length; i++)
		{
			Sprites[i].sprite = Online[i];
		}
	}

	private void SetOffline()
	{
		for (int i = 0; i < Sprites.Length; i++)
		{
			Sprites[i].sprite = Offline[i];
		}
	}

	private void SetPowerOff()
	{
		for (int i = 0; i < Sprites.Length; i++)
		{
			Sprites[i].sprite = PowerOff[i];
		}
	}

}
