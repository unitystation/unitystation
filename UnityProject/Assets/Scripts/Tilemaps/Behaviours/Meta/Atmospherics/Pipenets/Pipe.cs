using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipe : PickUpTrigger
{
	public List<Pipe> nodes = new List<Pipe>();
	public bool anchored = false;
	public Direction direction = Direction.NORTH;


	public enum Direction
	{
		NORTH,
		SOUTH,
		WEST,
		EAST
	}

	public virtual bool IsCorrectDirection(Direction oppositeDir)
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
			}
		}
	}

	public virtual List<Vector3> GetAdjacentTurfs()
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

		if (handObj.GetComponent<WrenchTrigger>())
		{
			SoundManager.PlayAtPosition("Wrench", transform.localPosition);
			if(anchored)
			{
				anchored = false;
				Detach();
			}
			else
			{
				if(GetAnchoredPipe(transform.position) != null)
				{
					return true;
				}
				CalculateAttachedNodes();
				Attach();
				anchored = true;
			}
			SoundManager.PlayAtPosition("Wrench", transform.localPosition);
		}
		return true;
	}

	public virtual void Attach()
	{

	}

	public virtual void Detach()
	{

	}

}