using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class Pipe : PickUpTrigger
{
	public List<Pipe> nodes = new List<Pipe>();
	public bool anchored = false;
	public Direction direction = Direction.NORTH;
	public Sprite[] pipeSprites;
	public SpriteRenderer spriteRenderer;

	public Pipenet pipenet;
	public float volume = 70;

	public int ARANpipenetmembers;
	public string ARANpipenetName;
	public float ARANpipenetVolume;

	public enum Direction
	{
		NORTH,
		SOUTH,
		WEST,
		EAST
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			//ask server to perform the interaction
			InteractMessage.Send(gameObject, position, hand);
			return true;
		}

		PlayerNetworkActions pna = originator.GetComponent<PlayerNetworkActions>();
		GameObject handObj = pna.Inventory[hand].Item;

		if(handObj == null)
		{
			if(!anchored){
				return base.Interact(originator, position, hand);
			}
		}
		else{
			if (handObj.GetComponent<WrenchTrigger>())
			{
				if (anchored)
				{
					anchored = false;
					Detach();
				}
				else
				{
					if (GetAnchoredPipe(transform.position) != null)
					{
						return true;
					}
					CalculateAttachedNodes();
					Attach();
					anchored = true;
				}
				SpriteChange();
				SoundManager.PlayAtPosition("Wrench", transform.position);
			}
		}

		return true;
	}


	public virtual void Attach()
	{
		Pipenet foundPipenet = null;
		for (int i = 0; i < nodes.Count; i++)
		{
			foundPipenet = nodes[i].pipenet;
			break;
		}
		if (foundPipenet == null)
		{
			foundPipenet = new Pipenet();
		}
		foundPipenet.AddPipe(this);

		transform.rotation = new Quaternion();
		transform.position = Vector3Int.RoundToInt(transform.position);
	}


	public void Detach()
	{
		//TODO: release gas to environmental air

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

	public Pipe GetAnchoredPipe(Vector3 position)
	{
		var foundPipes = MatrixManager.GetAt<Pipe>(position.RoundToInt(), true);
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
		var adjacentTurfs = GetAdjacentTurfs();
		for (int i = 0; i < adjacentTurfs.Count; i++)
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

	public List<Vector3> GetAdjacentTurfs()
	{
		Vector3 firstDir = transform.position;
		Vector3 secondDir = transform.position;
		if (direction == Direction.NORTH || direction == Direction.SOUTH)
		{
			firstDir += new Vector3(0, 1, 0);
			secondDir += new Vector3(0, -1, 0);
		}
		else
		{
			firstDir += new Vector3(1, 0, 0);
			secondDir += new Vector3(-1, 0, 0);
		}
		return new List<Vector3>() { firstDir, secondDir };
	}


	public virtual void SpriteChange()
	{
		spriteRenderer.sprite = pipeSprites[0];
	}

}