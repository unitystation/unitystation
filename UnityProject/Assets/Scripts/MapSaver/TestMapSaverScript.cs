using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TileManagement;
using UnityEngine;

public class TestMapSaverScript : MonoBehaviour
{
	public MetaTileMap MetaTileMap;


	[NaughtyAttributes.Button()]
	public void SaveMap()
	{

		Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveObjects(MetaTileMap)));
		//Logger.Log(JsonConvert.SerializeObject(MapSaver.MapSaver.SaveTileMap(MetaTileMap)));
	}
}
