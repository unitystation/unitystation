using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Manifold : SimplePipe
{

	public Sprite[] connectionSprite;
	public SpriteRenderer[] conectionRenderer;

	[SyncVar(hook = nameof(SyncConnectionSprite))] public Direction connectionSpriteSync;

	public override void CalculateSprite()
	{
		NullConnectionSprites();
		if (anchored)
		{
			Direction syncValue = Direction.NONE;
			for (int i = 0; i < nodes.Count; i++)
			{
				var pipe = nodes[i];
				if (pipe.RegisterTile.WorldPositionServer.y < RegisterTile.WorldPositionServer.y)
				{
					syncValue |= Direction.SOUTH;
					SetConnectionSprite(0);
				}
				else if (pipe.RegisterTile.WorldPositionServer.y > RegisterTile.WorldPositionServer.y)
				{
					syncValue |= Direction.NORTH;
					SetConnectionSprite(1);
				}
				else if (pipe.RegisterTile.WorldPositionServer.x > RegisterTile.WorldPositionServer.x)
				{
					syncValue |= Direction.EAST;
					SetConnectionSprite(2);
				}
				else if (pipe.RegisterTile.WorldPositionServer.x < RegisterTile.WorldPositionServer.x)
				{
					syncValue |= Direction.WEST;
					SetConnectionSprite(3);
				}
			}
			connectionSpriteSync = syncValue;
		}
		else
		{
			connectionSpriteSync = Direction.NONE;
			SetSprite(0);
		}
	}

	void NullConnectionSprites()
	{
		for (int i = 0; i < conectionRenderer.Length; i++)
		{
			conectionRenderer[i].sprite = null;
		}
	}

	void SetConnectionSprite(int index)
	{
		conectionRenderer[index].sprite = connectionSprite[index];
	}


	public override void OnStartClient()
	{
		base.OnStartClient();
		SyncConnectionSprite(Direction.NONE, connectionSpriteSync);
	}

	void SyncConnectionSprite(Direction oldValue, Direction value)
	{
		NullConnectionSprites();
		for (int i = 0; i < allDirections.Length; i++)
		{
			Direction D = allDirections[i];
			if(HasDirection(value, D))
			{
				if(D == Direction.SOUTH)
				{
					SetConnectionSprite(0);
				}
				else if (D == Direction.NORTH)
				{
					SetConnectionSprite(1);
				}
				else if (D == Direction.EAST)
				{
					SetConnectionSprite(2);
				}
				else
				{
					SetConnectionSprite(3);
				}
			}
		}
	}


	public override void DirectionEast()
	{
		SetSprite(3);
		direction = Direction.EAST | Direction.NORTH | Direction.SOUTH;
	}

	public override void DirectionNorth()
	{
		SetSprite(2);
		direction = Direction.WEST | Direction.NORTH | Direction.EAST;
	}

	public override void DirectionWest()
	{
		SetSprite(4);
		direction = Direction.WEST | Direction.SOUTH | Direction.NORTH;
	}

	public override void DirectionSouth()
	{
		SetSprite(1);
		direction = Direction.EAST | Direction.SOUTH | Direction.WEST;
	}

}
