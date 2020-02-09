using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Atmospherics;
using Tilemaps.Behaviours.Meta;

//TODO: These need to be reworked when pipenets are worked on next. See various todo comments.
//it needs to make proper use of Directional rather than rolling its own direction / sprite rotation logic
[RequireComponent(typeof(Pickupable))]
public class Pipe : NetworkBehaviour, IServerLifecycle
{
	public RegisterTile RegisterTile => registerTile;
	private RegisterTile registerTile;
	public ObjectBehaviour ObjectBehavior => objectBehaviour;
	private ObjectBehaviour objectBehaviour;

	[NonSerialized]
	public List<Pipe> nodes = new List<Pipe>();
	//TODO: This needs to use Directional instead of custom direction logic
	public Direction direction = Direction.NORTH | Direction.SOUTH;
	public Pipenet pipenet;
	public bool anchored;
	public float volume = 70;

	public Sprite[] pipeSprites;
	public SpriteRenderer spriteRenderer;
	[SyncVar(hook = nameof(SyncSprite))]
	private int spriteSync;

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

	public static readonly Direction[] allDirections =
		{Direction.NORTH, Direction.SOUTH, Direction.WEST, Direction.EAST,};

	public void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (registerTile != null) return;
		registerTile = GetComponent<RegisterTile>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
		//TODO: This component needs to be reworked to use Directional / DirectionalRotationSprites
		//directional.OnDirectionChange.AddListener(OnDirectionChange);
		pickupable = GetComponent<Pickupable>();
	}

	private void ServerInit()
	{
		pickupable.ServerSetCanPickup(!anchored);
		//TODO: Restore when pipenets implemented
		//CalculateDirection();
	}

	public override void OnStartServer()
	{
		EnsureInit();
		ServerInit();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		//only mapped stuff spawns anchored
		if (info.SpawnType != SpawnType.Mapped)
		{
			anchored = false;
		}
		ServerInit();
		AtmosManager.Instance.inGamePipes.Add(this);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		//make sure it's unhooked from everything
		ServerDetach();
		AtmosManager.Instance.inGamePipes.Remove(this);
	}

	/// <summary>
	/// One update tick from AtmosManager
	/// Check AtmosManager for adjusting tick rate
	/// </summary>
	public virtual void TickUpdate() { }

	[Server]
	public void ServerWrenchAct()
	{
		if (anchored)
		{
			ServerDetach();
		}
		else
		{
			if (ServerAttach() == false)
			{
				// show message to the player 'theres something attached in this direction already'
				return;
			}
		}

		SoundManager.PlayNetworkedAtPos("Wrench", registerTile.WorldPositionServer, 1f);
	}

	[Server]
	public virtual bool ServerAttach()
	{
		//TODO: Restore pipe attach logic when pipenets are fully implemented.
		//Until then, we will just anchor the pipe so it can't be moved around.
		//
		// CalculateDirection();
		// if (GetAnchoredPipe(registerTile.WorldPositionServer, direction) != null)
		// {
		// 	return false;
		// }
		//
		// CalculateAttachedNodes();
		//
		// Pipenet foundPipenet;
		// if (nodes.Count > 0)
		// {
		// 	foundPipenet = nodes[0].pipenet;
		// }
		// else
		// {
		// 	foundPipenet = new Pipenet();
		// }
		//
		// foundPipenet.AddPipe(this);

		//just anchor it so it can't be moved and appears in the correct layer
		ServerSetAnchored(true);
		SetSpriteLayer(true);

		//snap to tile position
		transform.localPosition = registerTile.LocalPositionServer;
		//show the down-facing anchored sprite, modify this to show correct direction
		//when pipenets are implemented
		SetSprite(1);

		return true;
	}

	[Server]
	protected virtual void ServerSetAnchored(bool value)
	{
		anchored = value;
		objectBehaviour.ServerSetPushable(!value);
		//now that it's anchored, it can't be picked up
		//TODO: This is getting called client side when joining, which is bad because it's only meant
		//to be called server side. Most likely late joining clients have the wrong
		//client-side state due to this issue.
		pickupable.ServerSetCanPickup(!value);
	}

	public void SyncSprite(int oldValue, int value)
	{
		EnsureInit();
		SetSpriteLayer(value != 0);
		SetSprite(value);
	}

	public override void OnStartClient()
	{
		EnsureInit();
		SyncSprite(0, spriteSync);
	}


	[Server]
	private void ServerDetach()
	{
		//TODO: Restore full detach logic when pipenets are fully implemented. Until then
		//we will just change the sprite and make it pickupable again.

		// var foundMeters = MatrixManager.GetAt<Meter>(registerTile.WorldPositionServer, true);
		// for (int i = 0; i < foundMeters.Count; i++)
		// {
		// 	var meter = foundMeters[i];
		// 	if (meter.anchored)
		// 	{
		// 		foundMeters[i].Detach();
		// 	}
		// }
		//
		// //TODO: release gas to environmental air
		// SetAnchored(false);
		// SetSpriteLayer(false);
		// int neighboorPipes = 0;
		// for (int i = 0; i < nodes.Count; i++)
		// {
		// 	var pipe = nodes[i];
		// 	pipe.nodes.Remove(this);
		// 	pipe.CalculateSprite();
		// 	neighboorPipes++;
		// }
		//
		// nodes = new List<Pipe>();
		//
		// Pipenet oldPipenet = pipenet;
		// pipenet.RemovePipe(this);
		//
		// if (oldPipenet.members.Count == 0)
		// {
		// 	//we're the only pipe on the net, delete it
		// 	oldPipenet.DeletePipenet();
		// 	return;
		// }
		//
		// if (neighboorPipes == 1)
		// {
		// 	//we're at an edge of the pipenet, safe to remove
		// 	return;
		// }
		//
		// oldPipenet.Separate();

		ServerSetAnchored(false);
		SetSpriteLayer(false);
		//unanchored sprite
		SetSprite(0);
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
			if (HasDirection(direction, dir))
			{
				var adjacentTurf =
					MetaUtils.GetNeighbors(registerTile.WorldPositionServer, DirectionToVector3IntList(dir));
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

	//TODO: Revisit when pipenets implemented
	// private void CalculateDirection()
	// {
	// 	float rotation = transform.rotation.eulerAngles.z;
	// 	var orientation = Orientation.GetOrientation(rotation);
	// 	if (orientation == Orientation.Up) //look over later
	// 	{
	// 		orientation = Orientation.Down;
	// 	}
	// 	else if (orientation == Orientation.Down)
	// 	{
	// 		orientation = Orientation.Up;
	// 	}
	//
	// 	SetDirection(orientation);
	// 	directional.FaceDirection(orientation);
	// }

	private void OnDirectionChange(Orientation direction)
	{
		if (anchored)
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
			return new Vector3Int[] {Vector3Int.up};
		}

		if (dir == Direction.SOUTH)
		{
			return new Vector3Int[] {Vector3Int.down};
		}

		if (dir == Direction.WEST)
		{
			return new Vector3Int[] {Vector3Int.left};
		}

		if (dir == Direction.EAST)
		{
			return new Vector3Int[] {Vector3Int.right};
		}

		return null;
	}

	public virtual void CalculateSprite()
	{
		if (anchored == false)
		{
			SetSprite(0); //not anchored, item sprite
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

	protected void SetSprite(int value)
	{
		//TODO: Restore pipe sprites once pipenets are implemented.

		//Until then, we only support the unanchored and anchored pointing down sprites
		//force it to be the downward pointing pipe sprite if it's one of the other directions
		if (value != 0 && value != 1)
		{
			value = 1;
		}

		spriteSync = value;
		spriteRenderer.sprite = pipeSprites[value];
	}

	public virtual void SetSpriteLayer(bool anchoredLayer)
	{
		if (anchoredLayer)
		{
			spriteRenderer.sortingLayerID = SortingLayer.NameToID("Objects");
		}
		else
		{
			spriteRenderer.sortingLayerID = SortingLayer.NameToID("Items");
		}
	}
}