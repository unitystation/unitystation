using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatrixCash
{

	public static Vector3Int[] DIRs = new[]
	{
		new Vector3Int(-1, 1, 0),
		new Vector3Int(0, 1, 0),
		new Vector3Int(1, 1, 0),

		new Vector3Int(-1, 0, 0),
		new Vector3Int(0, 0, 0),
		new Vector3Int(1, 0, 0),

		new Vector3Int(-1, -1, 0),
		new Vector3Int(0, -1, 0),
		new Vector3Int(1, -1, 0),

	};


	public Vector3 WorldPOS;

	private MatrixInfo[] Positions = new MatrixInfo[9];

	public MatrixInfo GetFromArray(int Location,Vector3Int DIR)
	{
		if (Positions[Location] == null)
		{
			Positions[Location] = MatrixManager.AtPoint(WorldPOS + DIR, CustomNetworkManager.IsServer);
		}

		return Positions[Location];
	}


	public MatrixInfo GetforDirection(Vector3Int DIR)
	{
		if (DIR.x > 0)
		{
			if (DIR.y > 0)
			{
				return GetFromArray(2,DIR);
			}
			else if (DIR.y > -1)
			{
				return GetFromArray(5,DIR);
			}
			else
			{
				return GetFromArray(8,DIR);
			}
		}
		else if (DIR.x > -1)
		{
			if (DIR.y > 0)
			{
				return GetFromArray(1,DIR);
			}
			else if (DIR.y > -1)
			{
				return GetFromArray(4,DIR);
			}
			else
			{
				return GetFromArray(7,DIR);
			}
		}
		else
		{
			if (DIR.y > 0)
			{
				return GetFromArray(0,DIR);
			}
			else if (DIR.y > -1)
			{
				return GetFromArray(3,DIR);
			}
			else
			{
				return GetFromArray(6,DIR);
			}
		}
	}

	public void ResetNewPosition(Vector3 Centrepoint)
	{
		WorldPOS = Centrepoint;
		for (int i = 0; i < Positions.Length; i++)
		{
			Positions[i] = null;
		}
	}
}
