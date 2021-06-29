using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Initialisation;
using Messages.Server;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TilePaths
{
	private static Dictionary<TileType, string> paths = new Dictionary<TileType, string>()
	{
		{TileType.Base, "Tiles/Base"},
		{TileType.Floor, "Tiles/Floors"},
		{TileType.Table, "Tiles/Tables"},
		{TileType.Window, "Tiles/Windows"},
		{TileType.Wall, "Tiles/Walls"},
		{TileType.Grill, "Tiles/Objects"}, // TODO remove
		{TileType.Object, "Tiles/Objects"},
		{TileType.WindowDamaged, "Tiles/WindowDamage"},
		{TileType.Effects, "Tiles/Effects"},
		{TileType.UnderFloor, "Tiles/UnderFloors"},
		{TileType.ElectricalCable, "Tiles/Electrical"}
	};

	public static string Get(TileType type)
	{
		return paths.ContainsKey(type) ? paths[type] : null;
	}
}

[Serializable]
public class TilePathEntry
{
	public string path;
	public TileType tileType;
	public List<LayerTile> layerTiles = new List<LayerTile>();
}

public class TileManager : MonoBehaviour, IInitialise
{
	private static TileManager tileManager;

	public static TileManager Instance => tileManager;

	private int tilesToLoad = 0;
	private int tilesLoaded = 0;
	public static int TilesToLoad => Instance.tilesToLoad;
	public static int TilesLoaded => Instance.tilesLoaded;

	private Dictionary<TileType, Dictionary<string, LayerTile>> tiles = new Dictionary<TileType, Dictionary<string, LayerTile>>();
	private bool initialized;

	[SerializeField] private List<TilePathEntry> layerTileCollections = new List<TilePathEntry>();

	public InitialisationSystems Subsystem => InitialisationSystems.TileManager;

	void IInitialise.Initialise()
	{
#if UNITY_EDITOR
		CacheAllAssets();
#endif
		if (!GameData.IsInGame)
		{
			StartCoroutine(LoadAllTiles(true));
		}
	}

	private void Awake()
	{
		if (tileManager == null)
		{
			tileManager = this;
		}
		else
		{
			Destroy(this);
			return;
		}

#if UNITY_EDITOR
		CacheAllAssets();
#endif
		if (!initialized) StartCoroutine(LoadAllTiles());
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
	}

	private void OnSceneChange(Scene oldScene, Scene newScene)
	{
		UpdateTileMessage.DelayedStuff.Clear();
	}

	[ContextMenu("Cache All Assets")]
	public bool CacheAllAssets()
	{
		layerTileCollections.Clear();
		foreach (TileType tileType in Enum.GetValues(typeof(TileType)))
		{
			string path = TilePaths.Get(tileType);

			if (path != null)
			{
				layerTileCollections.Add(new TilePathEntry
				{
					path = path,
					tileType = tileType,
					layerTiles = Resources.LoadAll<LayerTile>(path).ToList()
				});
			}
		}

		return true;
	}

	private IEnumerator LoadAllTiles(bool staggeredload = false)
	{
		tilesToLoad = 0;
		tilesLoaded = 0;
		foreach (var type in layerTileCollections)
		{
			tilesToLoad += type.layerTiles.Count;
		}

		int objCounts = 0;
		foreach (var type in layerTileCollections)
		{
			if (!tiles.ContainsKey(type.tileType))
			{
				Instance.tiles.Add(type.tileType, new Dictionary<string, LayerTile>());
			}

			foreach (var t in type.layerTiles)
			{
				tilesLoaded++;
				if (t.TileType == type.tileType)
				{
					if (!tiles[type.tileType].ContainsKey(t.name))
					{
						tiles[type.tileType].Add(t.name, t);
					}
				}

				if (staggeredload)
				{
					objCounts++;
					if (objCounts >= 10)
					{
						objCounts = 0;
						yield return WaitFor.EndOfFrame;
					}
				}
			}

			if (staggeredload)
			{
				objCounts++;
				if (objCounts >= 10)
				{
					objCounts = 0;
					yield return WaitFor.EndOfFrame;
				}
			}
		}

		initialized = true;
	}

	public static LayerTile GetTile(TileType tileType, string key)
	{
		if (!Instance.initialized) Instance.StartCoroutine(Instance.LoadAllTiles());

		if (Instance.tiles.TryGetValue(tileType, out var tiles) && tiles.TryGetValue(key, out var layerTile))
		{
			return layerTile;
		}

		Debug.LogError(tiles == null
			? $"Could not find {tileType} dictionary"
			: $"Could not find layerTile in {tileType} dictionary with key: {key}");

		return null;
	}
}