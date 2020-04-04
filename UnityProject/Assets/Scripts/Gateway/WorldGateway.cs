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
	private SpriteRenderer[] Sprites;
	//SpriteBaseBottom, SpriteBaseTop, SpriteBaseRightMiddle, SpriteBaseLeftMiddle, SpriteBaseRightBottom, SpriteBaseLeftBottom, SpriteBaseRightTop, SpriteBaseLeftTop, SpriteBaseCentre
	//TODO animate centre

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

	private float timeElapsedServer = 0;
	private float timeElapsedClient = 0;
	public float DetectionTime = 1;

	[SyncVar(hook = nameof(SyncState))]
	private bool isOn = false;

	private void SyncState(bool oldVar, bool newVar)
	{
		isOn = newVar;
		//do your thing
		//all clients will be updated with this
	}

	[Server]
	public void ServerChangeState(bool newVar)
	{
		isOn = newVar;
	}

	protected virtual void UpdateMe()
	{
		if (isServer)
		{
			timeElapsedServer += Time.deltaTime;
			if (timeElapsedServer > DetectionTime && isOn == true)
			{
				DetectPlayer();
				timeElapsedServer = 0;
			}
		}
		else if (isClient)
		{
			timeElapsedClient += Time.deltaTime;
			if (timeElapsedClient > 1)
			{
				if (isOn == true)
				{
					SetOnline();
				}
				else if (isOn == false)
				{
					SetOffline();
				}
				timeElapsedClient = 0;
			}
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}
	void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void Start()
	{
		if (StationGateway == null) return;

		
		SetOffline();

		if (!isServer) return;

		ServerChangeState(false);

		registerTile = GetComponent<RegisterTile>();

		Position = registerTile.WorldPosition;
		Message = "Teleporting to: " + StationGateway.GetComponent<StationGateway>().WorldName;

		if (IsOnlineAtStart == true && StationGateway != null)
		{
			SetOnline();
			ServerChangeState(true);

			if (GetComponent<MobSpawnControlScript>() != null)
			{
				GetComponent<MobSpawnControlScript>().SpawnMobs();
			}
		}
	}

	[Server]
	public void SetUp()
	{
		if (IsOnlineAtStart && StationGateway != null)
		{
			SetOnline();
			ServerChangeState(true);

			if (GetComponent<MobSpawnControlScript>() != null)
			{
				GetComponent<MobSpawnControlScript>().SpawnMobs();
			}
		}
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
