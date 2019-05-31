using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Atmospherics;
using Tilemaps.Behaviours.Meta;

public class Pipe : NetworkBehaviour
{
	public List<Pipe> nodes = new List<Pipe>();
	public Direction direction = Direction.NORTH | Direction.SOUTH;
	public RegisterTile registerTile;
	public ObjectBehaviour objectBehaviour;
	public Sprite[] pipeSprites;
	public SpriteRenderer spriteRenderer;
	[SyncVar(hook = nameof(SyncSprite))] public int spriteSync;
	public bool anchored;

	public Pipenet pipenet;
	public float volume = 70;

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
		if(AtmosManager.Instance.roundStartedServer == false)
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
		SoundManager.PlayAtPosition("Wrench", registerTile.WorldPositionServer);
	}

	public virtual bool Attach()
	{
		CalculateDirection();
		if (GetAnchoredPipe(registerTile.WorldPositionServer, direction) != null)
		{
			return false;
		}
		CalculateAttachedNodes();

		Pipenet foundPipenet = null;
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
	}

	public void SyncSprite(int value)
	{
		if(value == 0) // its using the item sprite
		{
			SetSpriteLayer(false);
		}
		else
		{
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

	/* cheatsheet:
	0-45 = south
	45-135 = east
	135-225 = north
	225-315 = west
	315-360 = south
	*/
	public virtual void CalculateDirection()
	{
		direction = 0;
		var rotation = transform.rotation.eulerAngles.z;
		if ((rotation >= 45 && rotation <= 135) || (rotation >= 225 && rotation <= 315))
		{
			direction = Direction.EAST | Direction.WEST;
		}
		else
		{
			direction = Direction.NORTH | Direction.SOUTH;
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
		SetSprite(0);
	}

	public void SetSprite(int value)
	{
		spriteSync = value;
		Debug.Log($"ARAN: SETSPRITE FOR {name} - value: {value} - pipesprites: {pipeSprites.Length}");
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