using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Atmospherics;
using Tilemaps.Behaviours.Meta;

[RequireComponent(typeof(Pickupable))]
public class Pipe : NetworkBehaviour, IOnStageServer
{
	public RegisterTile registerTile;
	public ObjectBehaviour objectBehaviour;

	public List<Pipe> nodes = new List<Pipe>();
	public Direction direction = Direction.NORTH | Direction.SOUTH;
	private Directional directional;
	public Pipenet pipenet;
	public bool anchored;
	public float volume = 70;

	public Sprite[] pipeSprites;
	public SpriteRenderer spriteRenderer;
	[SyncVar(hook = nameof(SyncSprite))] public int spriteSync;

	protected Pickupable pickupable;

	[Flags]
	public enum Direction
	{
		NONE = 0,
		NORTH = 1,
		SOUTH = 2,
		WEST = 4,
		EAST = 8
	}
	public static readonly Direction[] allDirections = { Direction.NORTH, Direction.SOUTH, Direction.WEST, Direction.EAST, };

	public void Awake() {
		registerTile = GetComponent<RegisterTile>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChange);
		pickupable = GetComponent<Pickupable>();
	}

	private void ServerInit()
	{
		pickupable.ServerSetCanPickup(!anchored);
	}

	public override void OnStartServer()
	{
		ServerInit();
	}

	public void GoingOnStageServer(OnStageInfo info)
	{
		ServerInit();
	}

	public void Start(){
		if (AtmosManager.Instance.roundStartedServer == false)
		{
			AtmosManager.Instance.roundStartPipes.Add(this);
		}
	}

	public void WrenchAct()
	{
		if (anchored)
		{
			Detach();
			CalculateSprite();
		}
		else
		{
			if(Attach() == false)
			{
				// show message to the player 'theres something attached in this direction already'
				return;
			}
		}
		SoundManager.PlayNetworkedAtPos("Wrench", registerTile.WorldPositionServer, 1f);
	}

	public virtual bool Attach()
	{
		CalculateDirection();
		if (GetAnchoredPipe(registerTile.WorldPositionServer, direction) != null)
		{
			return false;
		}
		CalculateAttachedNodes();

		Pipenet foundPipenet;
		if(nodes.Count > 0)
		{
			foundPipenet = nodes[0].pipenet;
		}
		else
		{
			foundPipenet = new Pipenet();
		}
		foundPipenet.AddPipe(this);
		SetAnchored(true);
		SetSpriteLayer(true);

		transform.position = registerTile.WorldPositionServer;
		CalculateSprite();

		return true;
	}

	public virtual void SetAnchored(bool value)
	{
		anchored = value;
		objectBehaviour.isNotPushable = value;
		//now that it's anchored, it can't be picked up
		//TODO: This is getting called client side when joining, which is bad because it's only meant
		//to be called server side. Most likely late joining clients have the wrong
		//client-side state due to this issue.
		pickupable.ServerSetCanPickup(!value);
	}

	public void SyncSprite(int value)
	{
		if(value == 0) // its using the item sprite
		{
			SetSpriteLayer(false);
		}
		else
		{
			transform.rotation = Quaternion.identity; //counter shuttle rotation
			SetSpriteLayer(true);
		}
		SetSprite(value);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SyncSprite(spriteSync);
	}


	public void Detach()
	{

		var foundMeters = MatrixManager.GetAt<Meter>(registerTile.WorldPositionServer, true);
		for (int i = 0; i < foundMeters.Count; i++)
		{
			var meter = foundMeters[i];
			if (meter.anchored)
			{
				foundMeters[i].Detach();
			}
		}
		//TODO: release gas to environmental air
		SetAnchored(false);
		SetSpriteLayer(false);
		int neighboorPipes = 0;
		for (int i = 0; i < nodes.Count; i++)
		{
			var pipe = nodes[i];
			pipe.nodes.Remove(this);
			pipe.CalculateSprite();
			neighboorPipes++;
		}
		nodes = new List<Pipe>();

		Pipenet oldPipenet = pipenet;
		pipenet.RemovePipe(this);

		if (oldPipenet.members.Count == 0)
		{
			//we're the only pipe on the net, delete it
			oldPipenet.DeletePipenet();
			return;
		}

		if (neighboorPipes == 1)
		{
			//we're at an edge of the pipenet, safe to remove
			return;
		}
		oldPipenet.Separate();
	}


	public bool HasDirection(Direction a, Direction b)
	{
		return (a & b) == b;
	}

	public Pipe GetAnchoredPipe(Vector3Int position, Direction dir)
	{
		var foundPipes = MatrixManager.GetAt<Pipe>(position, true);
		for (int i = 0; i < allDirections.Length; i++)
		{
			var specificDir = allDirections[i];
			if (HasDirection(dir, specificDir))
			{
				for (int n = 0; n < foundPipes.Count; n++)
				{
					var pipe = foundPipes[n];
					if (pipe.anchored && HasDirection(pipe.direction, specificDir))
					{
						return pipe;
					}
				}
			}
		}
		return null;
	}

	public void CalculateAttachedNodes()
	{
		for (int i = 0; i < allDirections.Length; i++)
		{
			var dir = allDirections[i];
			if(HasDirection(direction, dir))
			{
				var adjacentTurf = MetaUtils.GetNeighbors(registerTile.WorldPositionServer, DirectionToVector3IntList(dir));
				var pipe = GetAnchoredPipe(adjacentTurf[0], OppositeDirection(dir));
				if (pipe)
				{
					nodes.Add(pipe);
					pipe.nodes.Add(this);
					pipe.CalculateSprite();
				}
			}
		}
	}

	private void CalculateDirection()
	{
		float rotation = transform.rotation.eulerAngles.z;
		var orientation = Orientation.GetOrientation(rotation);
		if(orientation == Orientation.Up)	//look over later
		{
			orientation = Orientation.Down;
		}
		else if (orientation == Orientation.Down)
		{
			orientation = Orientation.Up;
		}
		SetDirection(orientation);
		directional.FaceDirection(orientation);
	}

	private void OnDirectionChange(Orientation direction)
	{
		if(anchored)
		{
			SetDirection(direction);
			CalculateSprite();
		}
	}

	private void SetDirection(Orientation direction)
	{
		if (direction == Orientation.Down)
		{
			DirectionSouth();
		}
		else if (direction == Orientation.Up)
		{
			DirectionNorth();
		}
		else if (direction == Orientation.Right)
		{
			DirectionEast();
		}
		else
		{
			DirectionWest();
		}
	}

	Direction OppositeDirection(Direction dir)
	{
		if (dir == Direction.NORTH)
		{
			return Direction.SOUTH;
		}
		if (dir == Direction.SOUTH)
		{
			return Direction.NORTH;
		}
		if (dir == Direction.WEST)
		{
			return Direction.EAST;
		}
		if (dir == Direction.EAST)
		{
			return Direction.WEST;
		}
		return Direction.NONE;
	}

	Vector3Int[] DirectionToVector3IntList(Direction dir)
	{
		if (dir == Direction.NORTH)
		{
			return new Vector3Int[] { Vector3Int.up };
		}
		if (dir == Direction.SOUTH)
		{
			return new Vector3Int[] { Vector3Int.down };
		}
		if (dir == Direction.WEST)
		{
			return new Vector3Int[] { Vector3Int.left };
		}
		if (dir == Direction.EAST)
		{
			return new Vector3Int[] { Vector3Int.right };
		}
		return null;
	}

	public virtual void CalculateSprite()
	{
		if (anchored == false)
		{
			SetSprite(0);   //not anchored, item sprite
		}
	}

	public virtual void DirectionEast()
	{
		SetSprite(3);
		direction = Direction.EAST;
	}

	public virtual void DirectionNorth()
	{
		SetSprite(2);
		direction = Direction.NORTH;
	}

	public virtual void DirectionWest()
	{
		SetSprite(4);
		direction = Direction.WEST;
	}

	public virtual void DirectionSouth()
	{
		SetSprite(1);
		direction = Direction.SOUTH;
	}

	public void SetSprite(int value)
	{
		spriteSync = value;
		spriteRenderer.sprite = pipeSprites[value];
	}

	public virtual void SetSpriteLayer(bool anchoredLayer)
	{
		if(anchoredLayer)
		{
			spriteRenderer.sortingLayerID = SortingLayer.NameToID("Objects");
		}
		else
		{
			spriteRenderer.sortingLayerID = SortingLayer.NameToID("Items");
		}
	}

}