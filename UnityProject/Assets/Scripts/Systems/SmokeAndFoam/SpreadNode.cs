using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;

public class SpreadNode
{

	public readonly static Vector3Int[] dirs = new Vector3Int[]
	{
		new Vector3Int(1, 0),
		new Vector3Int(0, 1),
		new Vector3Int(-1, 0),
		new Vector3Int(0, -1),
	};

	public const float MaxTimePresent = 30f;

	public SourceReservoir SourceReservoir;

	public ReagentMix Present;

	public MetaDataNode OnMetaDataNode;

	public float PresentTimeCount;

	public bool IsActive = false;

	public virtual void Clean()
	{
		PresentTimeCount = 0;
		SourceReservoir = null;
		IsActive = false;
	}

	//Get adjacent without smoke

	public virtual bool CheckIsEdge()
	{
		return false;
	}

	public virtual void TrySpread()
	{
	}

	public virtual void Update()
	{
		//TODO apply reagents to people and stuff??
		//but lag?
		//but Applying to items
		//not yet

		PresentTimeCount += 1;
		if (PresentTimeCount > MaxTimePresent)
		{
			SourceReservoir.RemoveTile(this);
		}
	}




	public virtual void DistributeToTile(SpreadNode SpreadingFrom, SourceReservoir sourceReservoir)
	{
		IsActive = true;
		sourceReservoir.StacksLeft--;
		SourceReservoir = sourceReservoir;
		Present = SourceReservoir.PerTile.Clone();
		SourceReservoir.EdgeTiles.Add(this);
		SourceReservoir.ActiveTiles.Add(this);
		PresentTimeCount = 0;
	}
}

public class SourceReservoir
{
	public List<SpreadNode> ActiveTiles = new List<SpreadNode>();

	public List<SpreadNode> EdgeTiles = new List<SpreadNode>();

	public float ReagentPurity;
	public ReagentMix PerTile;
	public int StacksLeft;

	public void Update()
	{
		for (int i = ActiveTiles.Count - 1; i >= 0; i--)
		{
			ActiveTiles[i].Update();
		}

	}

	public void SpreadUpdate()
	{
		if (StacksLeft > 0)
		{

			for (int i = EdgeTiles.Count - 1; i >= 0; i--)
			{
				if (EdgeTiles[i].CheckIsEdge() == false)
				{
					EdgeTiles.Remove(EdgeTiles[i]);
				}
			}


			for (int i = EdgeTiles.Count - 1; i >= 0; i--)
			{
				if (this.StacksLeft == 0) break;
				EdgeTiles[i].TrySpread();
			}
		}
	}

	public void SpreadToNode(SpreadNode SpreadingFrom, SpreadNode ToSpreadNode)
	{
		if (ToSpreadNode.SourceReservoir != null)
		{
			ToSpreadNode.SourceReservoir.RemoveTile(ToSpreadNode);
		}

		ToSpreadNode.DistributeToTile(SpreadingFrom, this);
		if (SpreadingFrom != null) //So it doesn't try adding itself to Splat it came from bugging out the reaction
		{
			GainTileInherit(ToSpreadNode);
		}


		if (SpreadingFrom != null)
		{
			if (SpreadingFrom.CheckIsEdge() == false)
			{
				EdgeTiles.Remove(SpreadingFrom);
			}
		}
	}


	public void RemoveTile(SpreadNode Tile)
	{
		if (ActiveTiles.Contains(Tile))
		{
			ActiveTiles.Remove(Tile);
		}

		if (EdgeTiles.Contains(Tile))
		{
			EdgeTiles.Remove(Tile);
		}
		Tile.Clean();

		if (ActiveTiles.Count == 0)
		{
			SmokeAndFoamManager.Instance.ActiveNodes.Remove(this);
			RemoveTileInherit();
		}
	}

	public virtual void RemoveTileInherit()
	{

	}

	public virtual void GainTileInherit(SpreadNode ToSpreadNode)
	{

	}
}