using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TileManagement;
using UnityEngine;


public class TestMapSaverScript : MonoBehaviour
{
	public MetaTileMap MetaTileMap;

	public List<MetaTileMap> MapMatrices = new List<MetaTileMap>();

	public Vector3Int Vector3Int1 = Vector3Int.zero;
	public Vector3Int Vector3Int2 = Vector3Int.zero;


	[NaughtyAttributes.Button()]
	public void SaveMatrix()
	{

		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveObjects(MetaTileMap)));
		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveTileMap(MetaTileMap)));
		//, UseInstance: true
		var map = MapSaver.MapSaver.SaveMatrix(MetaTileMap, true);

		//TODO Add a category for maps and blueprints
		//File.WriteAllText("R:\\tests\\bob.txt", JsonConvert.SerializeObject(map));
	}

	[NaughtyAttributes.Button()]
	public void SaveMatrixSubsection()
	{

		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveObjects(MetaTileMap)));
		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveTileMap(MetaTileMap)));
		Vector3 Vector1 = (Vector3) Vector3Int1 + new Vector3(0.5f, 0.5f, 0); //TODO Tile map upgrade
		Vector3 Vector2 = (Vector3) Vector3Int2 + new Vector3(-0.5f, -0.5f, 0);

		//TODO Add a category for maps and blueprints
		//File.WriteAllText("R:\\tests\\bob.txt", JsonConvert.SerializeObject(MapSaver.MapSaver.SaveMatrix(MetaTileMap, true, Vector1, Vector2)));
	}


	[NaughtyAttributes.Button()]
	public void SaveMap()
	{

		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveObjects(MetaTileMap)));
		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveTileMap(MetaTileMap)));
		//TODO Add a category for maps and blueprints
		//File.WriteAllText("R:\\tests\\bob2.txt", JsonConvert.SerializeObject(MapSaver.MapSaver.SaveMap(MapMatrices)));
	}

}
