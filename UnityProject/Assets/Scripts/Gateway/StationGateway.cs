using Mirror;
using UnityEngine;
using System.Linq;
using UnityEditor;

/// <summary>
/// For Gateways inheritable class
/// </summary>
public class StationGateway : NetworkBehaviour
{
	[SerializeField]
	private SpriteRenderer[] Sprites = null;
	//SpriteBaseBottom, SpriteBaseTop, SpriteBaseRightMiddle, SpriteBaseLeftMiddle, SpriteBaseRightBottom, SpriteBaseLeftBottom, SpriteBaseRightTop, SpriteBaseLeftTop, SpriteBaseCentre

	private int animationOffset = 0;

	[SerializeField]
	private Sprite[] Online = null;
	[SerializeField]
	private Sprite[] Offline = null;
	[SerializeField]
	private Sprite[] PowerOff = null;//TODO connect gateway to APC

	private WorldGateway selectedWorld;// The world from the list that was chosen

	private bool HasPower = true;// Not used atm

	private bool IsConnected;

	protected bool SpawnedMobs = false;

	private int RandomCountBegining = 300; //Defaults to between 5 and 20 mins gate will open.
	private int RandomCountEnd = 1200;

	protected RegisterTile registerTile;

	private Matrix Matrix => registerTile.Matrix;

	public string WorldName = "The Station";

	protected Vector3Int Position;

	protected string Message;

	protected float timeElapsedServer = 0;
	protected float timeElapsedClient = 0;
	protected float timeElapsedServerSound = 0;
	public float DetectionTime = 1;

	public float SoundLength = 7f;
	public float AnimationSpeed = 0.25f;

	private float WaitTimeBeforeActivation { get; set; }

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
			if (timeElapsedServer > DetectionTime && isOn)
			{
				DetectPlayer();
				timeElapsedServer = 0;
			}

			timeElapsedServerSound += Time.deltaTime;
			if (timeElapsedServerSound > SoundLength && isOn)
			{
				DetectPlayer();
				SoundManager.PlayNetworkedAtPos("machinehum4", Position + Vector3Int.up);
				timeElapsedServerSound = 0;
			}
		}
		else
		{
			timeElapsedClient += Time.deltaTime;
			if (timeElapsedClient > AnimationSpeed)
			{
				if (isOn)
				{
					SetOnline();
				}
				else
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

	public override void OnStartServer()
	{
		SetOffline();

		registerTile = GetComponent<RegisterTile>();
		Position = registerTile.WorldPosition;
		SubSceneManager.RegisterStationGateway(this);
		ServerChangeState(false);
		bool loadNormally = true;
		if (Application.isEditor)
		{
#if UNITY_EDITOR
			if (EditorPrefs.HasKey("prevEditorScene"))
			{
				if (!string.IsNullOrEmpty(EditorPrefs.GetString("prevEditorScene")))
				{
					if (SubSceneManager.Instance.awayWorldList.AwayWorlds.Contains(
						EditorPrefs.GetString("prevEditorScene")))
					{
						loadNormally = false;
						//This will ensure that the gateway is ready in 30 seconds
						//if you are working on an awaysite in the editor
						WaitTimeBeforeActivation = 30f;
					}
				}
			}
#endif
		}

		if(loadNormally)
		{
			WaitTimeBeforeActivation = Random.Range(RandomCountBegining, RandomCountEnd);
		}

		Invoke(nameof(ConnectToWorld), WaitTimeBeforeActivation);
	}

	[Server]
	void ConnectToWorld()
	{
		var randomWorld = SubSceneManager.RequestRandomAwayWorldLink(this);

		if (randomWorld == null)
		{
			Logger.Log("StationGateway failed to connect to an away world");
			SetOffline();
			return;
		}

		selectedWorld = randomWorld;

		Message = "Teleporting to: " + selectedWorld.WorldName;

		if (selectedWorld.spawnMobsOnConnection)
		{
			selectedWorld.SetUp(this);
		}

		if (HasPower)
		{
			SetOnline();
			ServerChangeState(true);

			var text = "Alert! New Gateway connection formed.\n\n Connection established to: " + selectedWorld.WorldName;
			CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
		}
	}

	[Server]
	public virtual void DetectPlayer()
	{
		//detect players positioned on the portal bit of the gateway
		var playersFound = Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Player, true);

		if (!SpawnedMobs && selectedWorld != null && playersFound.Count() > 0)
		{
			selectedWorld.SetUp(this);
			Logger.Log("Gateway Spawned Mobs");
			if (selectedWorld.GetComponent<MobSpawnControlScript>() != null)
			{
				selectedWorld.GetComponent<MobSpawnControlScript>().SpawnMobs();
			}
			SpawnedMobs = true;
		}

		foreach (ObjectBehaviour player in playersFound)
		{
			var coord = new Vector2(Position.x, Position.y);
			Chat.AddLocalMsgToChat(Message, coord, gameObject);
			SoundManager.PlayNetworkedForPlayer(player.gameObject, "StealthOff"); //very weird, sometimes does the sound other times not.
			TransportPlayers(player);
		}

		foreach (var objects in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Object, true))
		{
			TransportObjectsItems(objects);
		}

		foreach (var items in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Item, true))
		{
			TransportObjectsItems(items);
		}
	}

	[Server]
	public virtual void TransportPlayers(ObjectBehaviour player)
	{
		//teleports player to the front of the new gateway
		player.GetComponent<PlayerSync>().SetPosition(selectedWorld.GetComponent<RegisterTile>().WorldPosition);
	}

	[Server]
	public virtual void TransportObjectsItems(ObjectBehaviour objectsitems)
	{
		objectsitems.GetComponent<CustomNetTransform>().SetPosition(selectedWorld.GetComponent<RegisterTile>().WorldPosition);
	}

	public virtual void SetOnline()
	{
		SetSprites(Online);

		animationOffset += Sprites.Length;
		animationOffset %= Online.Length;
	}

	public virtual void SetOffline()
	{
		SetSprites(Offline);

		animationOffset += Sprites.Length;
		animationOffset %= Offline.Length;
	}

	public virtual void SetPowerOff()
	{
		animationOffset = 0;
		SetSprites(PowerOff);
	}

	private void SetSprites(Sprite[] spriteSet)
	{
		for (int i = 0; i < Sprites.Length; i++)
		{
			Sprites[i].sprite = spriteSet[i + animationOffset];
		}
	}
}