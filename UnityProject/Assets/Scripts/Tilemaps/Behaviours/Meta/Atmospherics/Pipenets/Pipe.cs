using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;
using Tilemaps.Behaviours.Meta;

public class Pipe : MonoBehaviour
{
	public List<Pipe> nodes = new List<Pipe>();
	public Direction direction = Direction.NORTH;
	public RegisterTile registerTile;
	public ObjectBehaviour objectBehaviour;
	public Sprite[] pipeSprites;
	public SpriteRenderer spriteRenderer;
	public bool anchored;

	public Pipenet pipenet;
	public float volume = 70;

	public enum Direction
	{
		NORTH,
		SOUTH,
		WEST,
		EAST
	}

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
		}
		else
		{
			if (GetAnchoredPipe(registerTile.WorldPositionServer) != null)
			{
				return;
			}
			Attach();
			transform.rotation = new Quaternion();
			transform.position = registerTile.WorldPositionServer;
		}
		SpriteChange();
		SoundManager.PlayAtPosition("Wrench", registerTile.WorldPositionServer);
	}

	public virtual void Attach()
	{
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
		SetSpriteLayer();
	}

	public virtual void SetAnchored(bool value)
	{
		anchored = value;
		objectBehaviour.isNotPushable = value;
	}

	public void Detach()
	{
		//TODO: release gas to environmental air
		SetAnchored(false);
		SetSpriteLayer();
		int neighboorPipes = 0;
		for (int i = 0; i < nodes.Count; i++)
		{
			var pipe = nodes[i];
			pipe.nodes.Remove(this);
			pipe.SpriteChange();
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


	public bool IsCorrectDirection(Direction oppositeDir)
	{
		if(oppositeDir == Direction.NORTH || oppositeDir == Direction.SOUTH)
		{
			if(direction == Direction.NORTH || direction == Direction.SOUTH)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		else
		{
			if (direction == Direction.EAST || direction == Direction.WEST)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}

	public Pipe GetAnchoredPipe(Vector3Int position)
	{
		var foundPipes = MatrixManager.GetAt<Pipe>(position, true);
		for (int n = 0; n < foundPipes.Count; n++)
		{
			var pipe = foundPipes[n];
			if (pipe.anchored && pipe.IsCorrectDirection(direction))
			{
				return pipe;
			}
		}
		return null;
	}

	public void CalculateAttachedNodes()
	{
		Vector3Int[] dir;
		if (direction == Direction.NORTH || direction == Direction.SOUTH)
		{
			dir = new Vector3Int[] { Vector3Int.up, Vector3Int.down };
		}
		else
		{
			dir = new Vector3Int[] { Vector3Int.left, Vector3Int.right };
		}
		var adjacentTurfs = MetaUtils.GetNeighbors(registerTile.WorldPositionServer, dir);
		for (int i = 0; i < adjacentTurfs.Length; i++)
		{
			var pipe = GetAnchoredPipe(adjacentTurfs[i]);
			if (pipe)
			{
				nodes.Add(pipe);
				pipe.nodes.Add(this);
				pipe.SpriteChange();
			}
		}
	}

	public virtual void SpriteChange()
	{
		spriteRenderer.sprite = pipeSprites[0];
	}

	public virtual void SetSpriteLayer()
	{
		if(anchored == false)
		{
			spriteRenderer.sortingLayerID = SortingLayer.NameToID("Items");
		}
		else
		{
			spriteRenderer.sortingLayerID = SortingLayer.NameToID("Objects");
		}
	}

}