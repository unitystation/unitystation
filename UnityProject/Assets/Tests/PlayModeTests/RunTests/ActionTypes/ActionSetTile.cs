using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;

public partial class TestAction
{
	public bool ShowSetTile => SpecifiedAction == ActionType.SetTile;

	[AllowNesting] [ShowIf(nameof(ShowSetTile))] public SetTile SetTileData;

	[System.Serializable]
	public class SetTile
	{
		public Vector3 WorldPosition;

		public LayerTile LayerTile;

		public string MatrixName;

		public LayerType tileLayerRemove;

		// public Matrix4x4 matrix; //has terrible inspector and Defaults to invalid Option

		// public Color Colour; // Defaults to bad option


		public bool Initiate(TestRunSO TestRunSO)
		{
			MatrixInfo _Magix = UsefulFunctions.GetCorrectMatrix(MatrixName, WorldPosition);

			if (LayerTile == null)
			{
				_Magix.Matrix.MetaTileMap.RemoveTileWithlayer(WorldPosition.ToLocal(_Magix).RoundToInt(), tileLayerRemove);
			}
			else
			{
				_Magix.Matrix.MetaTileMap.SetTile(WorldPosition.ToLocal(_Magix).RoundToInt(), LayerTile, Matrix4x4.identity,
					Color.white);
			}



			return true;
		}
	}

	public bool InitiateSetTile(TestRunSO TestRunSO)
	{
		return SetTileData.Initiate(TestRunSO);
	}
}