using System.Collections;
using System.Collections.Generic;
using System.IO;
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


	public bool Compact = false;

	[NaughtyAttributes.Button()]
	public void SaveMatrix()
	{

		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveObjects(MetaTileMap)));
		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveTileMap(MetaTileMap)));
		//, UseInstance: true
		var map = MapSaver.MapSaver.SaveMatrix(Compact,MetaTileMap, true);

		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore, // Ignore null values
			DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, // Ignore default values
			Formatting = Formatting.Indented
		};

		//TODO Add a category for maps and blueprints
		//File.WriteAllText("R:/tests/SaveMatrix.txt", JsonConvert.SerializeObject(map, settings));


	}

	[NaughtyAttributes.Button()]
	public void SaveMatrixSubsection()
	{

		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveObjects(MetaTileMap)));
		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveTileMap(MetaTileMap)));
		Vector3 Vector1 = (Vector3) Vector3Int1 + new Vector3(0.5f, 0.5f, 0); //TODO Tile map upgrade
		Vector3 Vector2 = (Vector3) Vector3Int2 + new Vector3(-0.5f, -0.5f, 0);

		//TODO Add a category for maps and blueprints
		//File.WriteAllText("R:/tests/SaveMatrixSubsection.txt", JsonConvert.SerializeObject(MapSaver.MapSaver.SaveMatrix(Compact, MetaTileMap, true, Vector1, Vector2)));
	}


	[NaughtyAttributes.Button()]
	public void SaveMap()
	{

		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveObjects(MetaTileMap)));
		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveTileMap(MetaTileMap)));
		//TODO Add a category for maps and blueprints
		//File.WriteAllText("R:/tests/SaveMap.txt", JsonConvert.SerializeObject(MapSaver.MapSaver.SaveMap(MapMatrices, Compact , "COOL MAP"), Formatting.Indented ));
	}

}
