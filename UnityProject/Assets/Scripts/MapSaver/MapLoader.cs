using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MapSaver
{
    public static class MapLoader
    {

	    public static void ProcessorGitFriendlyTiles(MatrixInfo Matrix,  Vector3Int Offset00, Vector3Int Offset , MapSaver.GitFriendlyTileMapData GitFriendlyTileMapData )
	    {

		    foreach (var XY in GitFriendlyTileMapData.XYs)
		    {
			    var Pos = MapSaver.GitFriendlyPositionToVectorInt(XY.Key);
			    Pos += Offset00;
			    Pos += Offset;

			    foreach (var Tile in XY.Value)
			    {
				  //  TileManager.GetTile(TileType, Tile.Tel)
				  //  Matrix.MetaTileMap.SetTile(Pos,Tile.Lay,  Tile.Tel,  )
			    }

		    }

	    }


	    public static void LoadSection( MatrixInfo Matrix,  Vector3 Offset00, Vector3 Offset , MapSaver.MatrixData MatrixData )
	    {


	    }


    }
}
