using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;

public partial class TestAction
{
	public bool ShowHasTile => SpecifiedAction == ActionType.HasTile;

	[AllowNesting] [ShowIf(nameof(ShowHasTile))] public HasTile HasTileData;

	[System.Serializable]
	public class HasTile
	{
		public Vector3 WorldPosition;

		public LayerTile LayerTile;

		public bool NoTileAt = false;

		public LayerType LayerType;

		public string MatrixName;

		// public Matrix4x4 matrix; //has terrible inspector and Defaults to invalid Option

		// public Color Colour; // Defaults to bad option

		public string CustomFailedText;

		//LayerTile, Matrix4x4.identity,
		//Color.white
		public bool Initiate(TestRunSO TestRunSO)
		{
			var Magix = UsefulFunctions.GetCorrectMatrix(MatrixName, WorldPosition);
			if (NoTileAt == false)
			{
				var TileAt = Magix.Matrix.MetaTileMap.GetTile(WorldPosition.ToLocal(Magix).RoundToInt(), LayerTile.LayerType);

				if (TileAt != LayerTile)
				{
					TestRunSO.Report.AppendLine(CustomFailedText);
					TestRunSO.Report.AppendLine($"Tile does not match specified one at {WorldPosition} is {TileAt.OrNull()?.name} When it should be {LayerTile.name} ");
					return false;
				}
				else
				{
					return true;
				}
			}
			else
			{
				var TileAt = Magix.Matrix.MetaTileMap.GetTile(WorldPosition.ToLocal(Magix).RoundToInt(), LayerType);

				if (TileAt != null)
				{
					TestRunSO.Report.AppendLine(CustomFailedText);
					TestRunSO.Report.AppendLine($"A tile is present at {WorldPosition} it should not be, it is {TileAt.name} ");
					return false;
				}
				else
				{
					return true;
				}
			}
		}
	}

	public bool InitiateHasTile(TestRunSO TestRunSO)
	{
		return HasTileData.Initiate(TestRunSO);
	}
}
