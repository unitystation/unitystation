using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using SecureStuff;
using UnityEngine;

namespace MapSaver
{
    public static class MapLoader
    {
	    //TODO Gameobject references?
	    //TODO is missing  Cross make such IDs when List!!

	    //TODO Children rotation scale and position?
	    //TODO Multiple components?
	    //TODO ACU not set? Test
	    //TODO Layer selection


	    public static void ProcessorGitFriendlyTiles(MatrixInfo Matrix,  Vector3Int Offset00, Vector3Int Offset , MapSaver.GitFriendlyTileMapData GitFriendlyTileMapData )
	    {

		    foreach (var XY in GitFriendlyTileMapData.XYs)
		    {

			    var Pos = MapSaver.GitFriendlyPositionToVectorInt(XY.Key);
			    Pos += Offset00;
			    Pos += Offset;
			    foreach (var Layer in Matrix.MetaTileMap.LayersKeys)
			    {
				    Matrix.MetaTileMap.RemoveTileWithlayer(Pos, Layer);
			    }
			    Matrix.MetaTileMap.RemoveAllOverlays(Pos, LayerType.Effects);

			    foreach (var Tile in XY.Value)
			    {
				    var Tel =  TileManager.GetTile(Tile.Tel);
				    var NewPos = Pos;

				    if (Tile.Z != null)
				    {
					    NewPos.z = Tile.Z.Value;
				    }

				    Color? Colour = null;
				    if (string.IsNullOrEmpty(Tile.Col) == false)
				    {
					    ColorUtility.TryParseHtmlString(Tile.Col, out var NonNullbleColour);
					    Colour = NonNullbleColour;
				    }

				    Matrix4x4? Matrix4x4 = null;
				    if (string.IsNullOrEmpty(Tile.Tf) == false)
				    {
					    Matrix4x4 = MapSaver.StringToMatrix4X4(Tile.Tf);
				    }


				    Matrix.MetaTileMap.SetTile(NewPos, Tel, Matrix4x4, Colour);
			    }

		    }

	    }

	    public static void ProcessorCompactObjectMapData(MatrixInfo Matrix, Vector3Int Offset00, Vector3Int Offset,
		    MapSaver.CompactObjectMapData CompactObjectMapData, bool LoadingMultiple = false)
	    {
		    if (LoadingMultiple == false)
		    {
			    MapSaver.CodeClass.ThisCodeClass.Reset(); //TODO Some of the functionality could be handled here
		    }

		    foreach (var prefabData in CompactObjectMapData.PrefabData)
		    {
			    MapSaver.StringToVector(prefabData.LocalPRS, out Vector3 position);
			    position += Offset00;
			    position  += Offset;

			    var Object = Spawn.ServerPrefab(CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[prefabData.PrefabID], position.ToWorld(Matrix));
			    MapSaver.StringToPRS(Object.GameObject, prefabData.LocalPRS);
			    Object.GameObject.transform.localPosition += Offset00;
			    Object.GameObject.transform.localPosition += Offset;
			    Object.GameObject.GetComponent<UniversalObjectPhysics>()?.AppearAtWorldPositionServer(Object.GameObject.transform.localPosition.ToWorld(Matrix));

			    if (string.IsNullOrEmpty(prefabData.Name) == false)
			    {
				    Object.GameObject.name = prefabData.Name;
			    }

			    if (string.IsNullOrEmpty(prefabData.Name) == false)
			    {
				    Object.GameObject.name = prefabData.Name;
			    }

			    MapSaver.CodeClass.ThisCodeClass.Objects[prefabData.GitID] = Object.GameObject;
			    ProcessClassData(Object.GameObject, prefabData.Object);
		    }

		    HashSet<FieldData> ReuseSEt = new HashSet<FieldData>();

		    foreach (var Unprocessed in MapSaver.CodeClass.ThisCodeClass.Unprocessed)
		    {
			    ReuseSEt.Add(Unprocessed.FieldData);
			    if (Unprocessed.Object == null)
			    {
				    Loggy.LogError("on no..");
			    }
			    SecureMapsSaver.LoadData(Unprocessed.Object,ReuseSEt, MapSaver.CodeClass.ThisCodeClass, true);
			    ReuseSEt.Clear();
		    }
		    if (LoadingMultiple == false)
		    {
			    MapSaver.CodeClass.ThisCodeClass.Reset(); //TODO Some of the functionality could be handled here
		    }
	    }

	    public static void ProcessClassData(GameObject Object, MapSaver.IndividualObject IndividualObject )
	    {
		    if (string.IsNullOrEmpty(IndividualObject.Name) == false)
		    {
			    Object.name = IndividualObject.Name;
		    }

		    foreach (var classData in IndividualObject.ClassDatas)
		    {
			    var ClassComponents = classData.ClassID.Split("@"); //TODO Multiple components?
				var Component =  Object.GetComponent(ClassComponents[0]);
				if (Component == null)
				{
					try
					{
						Component = Object.AddComponent(AllowedReflection.GetTypeByName(ClassComponents[0]));
					}
					catch (Exception e)
					{
						Loggy.LogError(e.ToString());
						continue;
					}

				}
				SecureMapsSaver.LoadData(Component, classData.Data, MapSaver.CodeClass.ThisCodeClass);
		    }

		    if (IndividualObject.Children != null)
		    {
			    foreach (var Child in IndividualObject.Children)
			    {
				    var Id = int.Parse( Child.ID.Split(",").Last());
				    while (Object.transform.childCount <= Id)
				    {
					    var NewChild = new GameObject();
					    NewChild.transform.SetParent(Object.transform);
					    NewChild.transform.localPosition= Vector3.zero;
					    NewChild.transform.localScale = Vector3.one;
					    NewChild.transform.rotation = Quaternion.identity;
				    }

				    var ObjectChild = Object.transform.GetChild(Id);
				    ProcessClassData(ObjectChild.gameObject, Child);
			    }
		    }
	    }

	    public static void LoadSection( MatrixInfo Matrix,  Vector3 Offset00, Vector3 Offset , MapSaver.MatrixData MatrixData )
	    {
		    //TODO MapSaver.CodeClass.ThisCodeClass?? Clearing?

		    if (MatrixData.GitFriendlyTileMapData != null)
		    {
			    ProcessorGitFriendlyTiles(Matrix, Offset00.RoundToInt(), Offset.RoundToInt(), MatrixData.GitFriendlyTileMapData);
		    }

		    if (MatrixData.CompactObjectMapData != null)
		    {
			    ProcessorCompactObjectMapData(Matrix, Offset00.RoundToInt(), Offset.RoundToInt(), MatrixData.CompactObjectMapData); //TODO Handle GitID better
		    }
	    }
    }
}
