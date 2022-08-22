using Tiles;
using UnityEditor;
using UnityEngine;


public class CreateTilesMenu : MonoBehaviour
	{
		[MenuItem("Assets/Create/Tiles/General/Simple Tile", false, 0)]
		public static void CreateSimpleTile()
		{
			TileBuilder.CreateTile<SimpleTile>(LayerType.None, "SimpleTile");
		}

		[MenuItem("Assets/Create/Tiles/General/Connected Tile", false, 0)]
		public static void CreateConnectedTile()
		{
			TileBuilder.CreateTile<ConnectedTile>(LayerType.None, "ConnectedTile");
		}

		[MenuItem("Assets/Create/Tiles/General/Connected Tile V2", false, 0)]
		public static void CreateConnectedTileV2()
		{
			TileBuilder.CreateTile<ConnectedTileV2>(LayerType.None, "ConnectedTileV2");
		}

		[MenuItem("Assets/Create/Tiles/General/Animated Tile", false, 0)]
		public static void CreateAnimatedTile()
		{
			TileBuilder.CreateTile<AnimatedTile>(LayerType.None, "AnimatedTile");
		}

		[MenuItem("Assets/Create/Tiles/General/Meta Tile", false, 0)]
		public static void CreateMetaTile()
		{
			MetaTile tile = ScriptableObject.CreateInstance<MetaTile>();
			TileBuilder.CreateAsset(tile, "MetaTile");
		}

		[MenuItem("Assets/Create/Tiles/Floor", false, 0)]
		public static void CreateFloor()
		{
			TileBuilder.CreateTile<SimpleTile>(LayerType.Floors, "FloorTile");
		}

		[MenuItem("Assets/Create/Tiles/Wall", false, 0)]
		public static void CreateWallConnected()
		{
			ConnectedTile tile = TileBuilder.CreateTile<ConnectedTile>(LayerType.Walls);
			tile.texturePath = "Walls";
			tile.connectCategory = ConnectCategory.Walls;
			tile.connectType = ConnectType.ToSameCategory;

			TileBuilder.CreateAsset(tile, "WallTile");
		}

		[MenuItem("Assets/Create/Tiles/Window", false, 0)]
		public static void CreateWindow()
		{
			ConnectedTile tile = TileBuilder.CreateTile<ConnectedTile>(LayerType.Windows);
			tile.texturePath = "Windows";
			tile.connectCategory = ConnectCategory.Windows;
			tile.connectType = ConnectType.ToSameCategory;

			TileBuilder.CreateAsset(tile, "WindowTile");
		}

		[MenuItem("Assets/Create/Tiles/Table", false, 0)]
		public static void CreateTable()
		{
			ConnectedTile tile = TileBuilder.CreateTile<ConnectedTile>(LayerType.Objects);
			tile.texturePath = "Tables";
			tile.connectCategory = ConnectCategory.Tables;
			tile.connectType = ConnectType.ToSameCategory;

			TileBuilder.CreateAsset(tile, "TableTile");
		}

		[MenuItem("Assets/Create/Tiles/Object", false, 0)]
		public static void CreateObject()
		{
			TileBuilder.CreateTile<ObjectTile>(LayerType.Objects, "ObjectTile");
		}

		[MenuItem("Assets/Create/Tiles/Wall Mount", false, 0)]
		public static void CreateWallMount()
		{
			ObjectTile tile = TileBuilder.CreateTile<ObjectTile>(LayerType.Objects);
			tile.Rotatable = true;
			tile.Offset = true;

			TileBuilder.CreateAsset(tile, "WallMountTile");
		}
	}
