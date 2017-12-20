using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class MapToJSON : Editor
{
	//adding +50 offset to spriteRenderers containing these:
	private static readonly List<string> separateLayerMarkers = new List<string>(new[] {"WarningLine"});

	//not marking these as legacy:
	private static readonly List<string> legacyExclusionList = new List<string>(new[] {"turf/shuttle.png" /*,"lighting.png","obj/power.png"*/});

	//pretending these contain TileConnect component (however, four of these are still required to generate a temporary tile):
	//Item1: name to lookup (via Contains())
	//Item2: asset path to use instead while exporting
	private static readonly List<Tuple<string, string>> tileConnectWannabes =
		new List<Tuple<string, string>>(new[] {new Tuple<string, string>("shuttle_wall_Skew", "walls/shuttle_wall")});


	private static readonly string TC = "tc_";

	[MenuItem("Tools/Export map (JSON)")]
	private static void Map2JSON()
	{
		AssetDatabase.Refresh();

		//        var nodesMapped = MapToPNG.GetMappedNodes();
		//        var tilemapLayers = new SortedDictionary<string, TilemapLayer>(Comparer<string>.Create(CompareSpriteLayer));
		//        var tempGameObjects = new List<GameObject>();
		//
		//        for (int y = 0; y < nodesMapped.GetLength(0); y++)
		//        {
		//            for (int x = 0; x < nodesMapped.GetLength(1); x++)
		//            {
		//                var node = nodesMapped[y, x];
		//
		//                if (node == null)
		//                    continue;
		//
		//                var nodeRenderers = new List<SpriteRenderer>();
		//
		//                var objectsToExport = node.GetTiles();
		//                node.GetItems().ForEach(behaviour => objectsToExport.Add(behaviour.gameObject));
		//
		//                foreach (var tile in objectsToExport)
		//                {
		//                    var tileRenderers = tile.GetComponentsInChildren<SpriteRenderer>();
		//                    if (tileRenderers == null || tileRenderers.Length < 1) continue;
		//                    var tileconnects = 0;
		//                    foreach (var renderer in tileRenderers)
		//                    {
		//                        if (thisRendererSucks(renderer) || renderer.sortingLayerID == 0)
		//                            continue;
		//
		//                        TryMoveToSeparateLayer(renderer);
		//
		//                        if (renderer.GetComponent<TileConnect>() || IsTileConnectWannabe(renderer))
		//                        {
		//                            tileconnects++;
		//                            if (tileconnects != 4) continue;
		//                            if (tileconnects > 4)
		//                            {
		//                                Debug.LogWarningFormat("{0} — more than 4 tileconnects found!", renderer.name);
		//                            }
		//                            // grouping four tileconnect sprites into a single temporary thing
		//                            GameObject tcMergeGameObject = Instantiate(renderer.gameObject, tile.transform.position,
		//                                Quaternion.identity, tile.transform);
		//                            tempGameObjects.Add(tcMergeGameObject);
		//                            var childClone = tcMergeGameObject.GetComponent<SpriteRenderer>();
		//                            var spriteName = childClone.sprite.name;
		//
		//                            if (spriteName.Contains("_"))
		//                            {
		//                                childClone.name = TC + spriteName.Substring(0,
		//                                                      spriteName.LastIndexOf("_", StringComparison.Ordinal));
		//                            }
		//                            nodeRenderers.Add(childClone);
		//                        }
		//                        else
		//                        {
		//                            renderer.name = renderer.sprite.name;
		//                            if (DuplicateFound(renderer, nodeRenderers))
		//                            {
		//                                Debug.LogFormat("Skipping {0}({1}) as duplicate", renderer.name, GetSortingLayerName(renderer));
		//                                continue;
		//                            }
		//                            var uniqueSortingOrder = GetUniqueSortingOrder(renderer, nodeRenderers);
		//                            if (!uniqueSortingOrder.Equals(renderer.sortingOrder))
		//                            {
		//                                renderer.sortingOrder = uniqueSortingOrder;
		//                            }
		//                            nodeRenderers.Add(renderer);
		//                        }
		//                    }
		//                }
		//
		//                foreach (var renderer in nodeRenderers)
		//                {
		//                    var currentLayerName = GetSortingLayerName(renderer);
		//                    TilemapLayer tilemapLayer;
		//                    if (tilemapLayers.ContainsKey(currentLayerName))
		//                    {
		//                        tilemapLayer = tilemapLayers[currentLayerName];
		//                    }
		//                    else
		//                    {
		//                        tilemapLayer = new TilemapLayer();
		//                        tilemapLayers[currentLayerName] = tilemapLayer;
		//                    }
		//                    if (tilemapLayer == null)
		//                    {
		//                        continue;
		//                    }
		//                    UniTileData tileDataInstance = CreateInstance<UniTileData>();
		//                    var parentObject = renderer.transform.parent.gameObject;
		//                    if (parentObject)
		//                    {
		//                        tileDataInstance.Name = parentObject.name;
		//                    }
		//                    var childtf = renderer.transform;
		//                    var parenttf = renderer.transform.parent.gameObject.transform;
		//                    //don't apply any rotation for tileconnects
		//                    var isTC = renderer.name.StartsWith(TC);
		//                    var zeroRot = Quaternion.Euler(0, 0, 0);
		//
		//                    tileDataInstance.ChildTransform =
		//                        Matrix4x4.TRS(childtf.localPosition, isTC ? zeroRot : childtf.localRotation, childtf.localScale);
		//
		//                    tileDataInstance.Transform =
		//                        Matrix4x4.TRS(parenttf.position, isTC ? zeroRot : parenttf.localRotation, parenttf.localScale);
		//
		//                    tileDataInstance.OriginalSpriteName = renderer.sprite.name;
		//                    tileDataInstance.SpriteName = renderer.name;
		//                    var assetPath = AssetDatabase.GetAssetPath(renderer.sprite.GetInstanceID());
		//                    tileDataInstance.IsLegacy = looksLikeLegacy(assetPath, tileDataInstance) && !isExcluded(assetPath);
		//
		//                    string sheet = assetPath
		//                        .Replace("Assets/Resources/", "")
		//                        .Replace("Assets/textures/", "")
		//                        .Replace("Resources/", "")
		//                        .Replace(".png", "");
		//                    string overrideSheet;
		//                    tileDataInstance.SpriteSheet = IsTileConnectWannabe(renderer, out overrideSheet) ? overrideSheet : sheet;
		//                    tilemapLayer.Add(x, y, tileDataInstance);
		//                }
		//            }
		//        }
		//
		//        foreach (var layer in tilemapLayers)
		//        {
		//            Debug.LogFormat("{0}: {1}", layer.Key, layer.Value);
		//        }
		//
		//        fsData data;
		//        new fsSerializer().TrySerialize(tilemapLayers, out data);
		//        File.WriteAllText(Application.dataPath + "/Resources/metadata/" + SceneManager.GetActiveScene().name + ".json",
		//            fsJsonPrinter.PrettyJson(data));
		//
		//        //Cleanup
		//        foreach (var o in tempGameObjects)
		//        {
		//            DestroyImmediate(o);
		//        }
		//
		//        Debug.Log("Export kinda finished");
		//        AssetDatabase.Refresh();
	}

	private static bool DuplicateFound(SpriteRenderer renderer, List<SpriteRenderer> nodeRenderers)
	{
		return nodeRenderers.FindAll(sr =>
			       sr.name.Equals(renderer.name) && sr.transform.position.Equals(renderer.transform.position) &&
			       sr.transform.rotation.Equals(renderer.transform.rotation)).Count != 0;
	}

	private static int GetUniqueSortingOrder(SpriteRenderer renderer, List<SpriteRenderer> list)
	{
		return GetUniqueSortingOrderRecursive(new Tuple<string, int>(renderer.sortingLayerName, renderer.sortingOrder), list);
	}

	private static bool IsTileConnectWannabe(SpriteRenderer renderer)
	{
		string strEmpty;
		return IsTileConnectWannabe(renderer, out strEmpty);
	}

	private static bool IsTileConnectWannabe(SpriteRenderer renderer, out string newPath)
	{
		GameObject parentObj = renderer.transform.parent.gameObject;
		bool isTileConnectWannabe = parentObj && tileConnectWannabes.Any(tuple => parentObj.name.Contains(tuple.Item1));
		newPath = "";
		if (isTileConnectWannabe)
		{
			newPath = tileConnectWannabes.Find(tuple => parentObj.name.Contains(tuple.Item1)).Item2;
		}
		return isTileConnectWannabe;
	}

	private static void TryMoveToSeparateLayer(SpriteRenderer renderer)
	{
		GameObject parentObj = renderer.transform.parent.gameObject;
		bool moveToSeparateLayer = parentObj && separateLayerMarkers.Any(parentObj.name.Contains);
		if (moveToSeparateLayer)
		{
			renderer.sortingOrder += 50;
		}
	}

	private static int GetUniqueSortingOrderRecursive(Tuple<string, int> renderer, List<SpriteRenderer> list)
	{
		bool overlapFound = list.Any(r => r.sortingLayerName.Equals(renderer.Item1) && r.sortingOrder.Equals(renderer.Item2));
		// increment sorting order by 100 if overlap is detected and try again
		if (overlapFound)
		{
			return GetUniqueSortingOrderRecursive(new Tuple<string, int>(renderer.Item1, renderer.Item2 + 100), list);
		}
		return renderer.Item2;
	}

	private static bool isExcluded(string assetPath)
	{
		return legacyExclusionList.Any(assetPath.Contains);
	}

	private static bool looksLikeLegacy(string assetPath, UniTileData instance)
	{
		return assetPath.Contains("textures") && !instance.SpriteName.StartsWith(TC);
	}

	private static bool thisRendererSucks(SpriteRenderer spriteRenderer)
	{
		return !spriteRenderer || !spriteRenderer.sprite;
	}
}
#endif