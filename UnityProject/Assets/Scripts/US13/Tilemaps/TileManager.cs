using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Shared.Managers;
using UnityEngine;
using US13.Core.Initialisation;
using US13.Managers;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;

namespace US13.Tilemaps
{
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
			{TileType.Electrical, "Tiles/Electrical"},
			{TileType.Pipe, "Tiles/Pipes"},
			{TileType.Disposals, "Tiles/Disposals"}
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

	public class TileManager : SingletonManager<TileManager>, IInitialise
	{
		private int tilesToLoad = 0;
		private int tilesLoaded = 0;
		public static int TilesToLoad => Instance.tilesToLoad;
		public static int TilesLoaded => Instance.tilesLoaded;

		private Dictionary<TileType, Dictionary<string, LayerTile>> tiles = new Dictionary<TileType, Dictionary<string, LayerTile>>();

		public Dictionary<TileType, Dictionary<string, LayerTile>> Tiles => tiles;

		public  Dictionary<string, LayerTile> AllTiles = new  Dictionary<string, LayerTile>();

		private bool initialized;

		[SerializeField] private List<TilePathEntry> layerTileCollections = new List<TilePathEntry>();

		[field:SerializeField] public AnimatedTile ErrorTile { get; private set; }

		public InitialisationSystems Subsystem => InitialisationSystems.TileManager;

		void IInitialise.Initialise()
		{
#if UNITY_EDITOR
			CacheAllAssets();
#endif
			if (GameData.IsInGame == false)
			{
				if (!Instance.initialized) LoadAllTiles(true).Forget(e => Logs.Loggy.Error(e.ToString()));
			}
		}

		public int DeepCleanupTiles()
		{
			return 0;
		}

		public override void Awake()
		{
			base.Awake();

#if UNITY_EDITOR
			CacheAllAssets();
#endif
			if (!initialized) LoadAllTiles().Forget(e => Logs.Loggy.Error(e.ToString()));
		}

		[ContextMenu("Cache All Assets")]
		public bool CacheAllAssets()
		{
			layerTileCollections = new List<TilePathEntry>();
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

		public async UniTask LoadAllTiles(bool staggeredload = false)
		{
			initialized = true;
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
					tiles.Add(type.tileType, new Dictionary<string, LayerTile>());
				}

				foreach (var t in type.layerTiles)
				{
					if (AllTiles.ContainsKey(t.name) && AllTiles[t.name]  != t)
					{
						Loggy.Error("Duplicate names for " + t.name);
					}

					AllTiles[t.name] = t;
					tilesLoaded++;
					if (t.TileType == type.tileType)
					{
						if (!tiles[type.tileType].ContainsKey(t.name))
						{
							tiles[type.tileType].Add(t.name, t);
						}
					}

					if (staggeredload && Application.isPlaying)
					{
						objCounts++;
						if (objCounts >= 10)
						{
							objCounts = 0;
							await UniTask.NextFrame();
						}
					}
				}

				if (staggeredload && Application.isPlaying)
				{
					objCounts++;
					if (objCounts >= 10)
					{
						objCounts = 0;
						await UniTask.NextFrame();
					}
				}
			}
		}

		public static LayerTile GetTile( string key)
		{
			if (Instance.AllTiles.TryGetValue(key, out var tl))
			{
				return tl;
			}

			Loggy.Error( $"Could not find layerTile in dictionary with key: {key}");

			return null;
		}

		public static LayerTile GetTile(TileType tileType, string key)
		{
			if (!Instance.initialized) Instance.LoadAllTiles().Forget(e => Logs.Loggy.Error(e.ToString()));

			if (Instance.tiles.TryGetValue(tileType, out var tiles) && tiles.TryGetValue(key, out var layerTile))
			{
				return layerTile;
			}

			if (Instance.AllTiles.TryGetValue(key, out layerTile))
			{
				return layerTile;
			}

			Debug.LogError(tiles == null
				? $"Could not find {tileType} dictionary"
				: $"Could not find layerTile in {tileType} dictionary with key: {key}");

			return null;
		}

		public void Cleanup_between_rounds()
		{
#if UNITY_EDITOR
			return;
#endif
			layerTileCollections = new List<TilePathEntry>();
		}
	}
}