using System.Collections;
using System.Collections.Generic;
using Logs;
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
			Positions[Location] = MatrixManager.AtPoint(WorldPOS + DIR, CustomNetworkManager.IsServer, Positions[6]);
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
			else if (DIR.y < 0)
			{
				return GetFromArray(5,DIR);
			}
			else
			{
				return GetFromArray(8,DIR);
			}
		}
		else if (DIR.x < 0)
		{
			if (DIR.y > 0)
			{
				return GetFromArray(1,DIR);
			}
			else if (DIR.y < 0)
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
			else if (DIR.y < 0)
			{
				return GetFromArray(3,DIR);
			}
			else
			{
				return GetFromArray(6,DIR);
			}
		}
	}

	public void ResetNewPosition(Vector3 Centrepoint, RegisterTile Inon)
	{
		WorldPOS = Centrepoint;
		for (int i = 0; i < Positions.Length; i++)
		{
			Positions[i] = null;
		}

		if (Positions != null && Inon.OrNull()?.Matrix.OrNull()?.MatrixInfo is not null)
		{
			Positions[6] = Inon.Matrix.MatrixInfo;
		}
		else
		{
			Loggy.LogError($"[MatrixCash/ResetNewPosition] - A property has been detected as null when attempting to reset positions. " +
			               $"This usually happens when the game is first loading for clients, but if it persists; something has gone wrong.\n" +
			               $"Positions Null: {Positions is null}\n" +
			               $"Inon Null: {Inon is null}\n");
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
