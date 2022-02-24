using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Systems.Scenes
{
	/// <summary>
	/// Lava Land Random Cave Generator, modified version from this: https://www.youtube.com/watch?v=xNqqfABXTNQ, https://www.dropbox.com/s/qggbs7hnapj6136/ProceduralTilemaps.zip?dl=0
	/// </summary>
	public class LavaLandRandomGenScript : MonoBehaviour
	{

		public int iniChance;

		public int birthLimit;

		public int deathLimit;

		public int numR;

		private int[,] terrainMap;
		public Vector3Int tmpSize;
		public Tilemap topMap;
		//public Tilemap botMap;
		public TileBase topTile;
		//public AnimatedTile botTile;

		public LayerTile wallTile;

		int width;
		int height;

		private TileChangeManager tileChangeManager;

		[SerializeField]
		private RandomItemSpot mobPools = null;

		private void Start()
		{
			LavaLandManager.Instance.randomGenScripts.Add(this);


			tileChangeManager = transform.parent.parent.parent.GetComponent<TileChangeManager>();
		}

		public void DoSim()
		{
			var gameObjectPos = gameObject.transform.localPosition.RoundToInt();
			width = tmpSize.x;
			height = tmpSize.y;

			if (terrainMap == null)
			{
				terrainMap = new int[width, height];
				InitPos();
			}


			for (int i = 0; i < numR; i++)
			{
				terrainMap = GenTilePos(terrainMap);
			}

			var itemSpots = new List<GameObject>();

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					var pos = new Vector3Int(-x + width / 2, -y + height / 2, 0) + gameObjectPos;

					if (terrainMap[x, y] != 1)
					{

						tileChangeManager.MetaTileMap.SetTile(pos, wallTile);

						//Commented out below sets bottom tile, but we don't need to for lavaland
						//botMap.SetTile(new Vector3Int(-x + width / 2, -y + height / 2, 0), botTile);
					}

					else if (DMMath.Prob(2) && mobPools != null)
					{
						if (tileChangeManager.MetaTileMap.matrix.IsPassableAtOneMatrixOneTile(pos, true) == false) continue;
						itemSpots.Add(Spawn.ServerPrefab(mobPools.gameObject, tileChangeManager.MetaTileMap.LocalToWorld(pos), tileChangeManager.MetaTileMap.ObjectLayer.transform).GameObject);
					}
				}
			}

			StartCoroutine(SpawnMobs(itemSpots));
		}

		private IEnumerator SpawnMobs(List<GameObject> itemSpots)
		{
			yield return WaitFor.Seconds(30);

			foreach (var itemSpot in itemSpots)
			{
				if(itemSpot.TryGetComponent<RandomItemSpot>(out var spot) == false) continue;

				var tile = spot.GetComponent<RegisterTile>();

				if (tile.Matrix.IsPassableAtOneMatrixOneTile(tile.LocalPositionServer, true) == false)
				{
					_ = Despawn.ServerSingle(itemSpot);
					continue;
				}

				spot.RollRandomPool(true, true);
			}
		}

		private void InitPos()
		{
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					terrainMap[x, y] = Random.Range(1, 101) < iniChance ? 1 : 0;
				}
			}
		}


		private int[,] GenTilePos(int[,] oldMap)
		{
			int[,] newMap = new int[width, height];
			int neighb;
			BoundsInt myB = new BoundsInt(-1, -1, 0, 3, 3, 1);


			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					neighb = 0;
					foreach (var b in myB.allPositionsWithin)
					{
						if (b.x == 0 && b.y == 0) continue;
						if (x + b.x >= 0 && x + b.x < width && y + b.y >= 0 && y + b.y < height)
						{
							neighb += oldMap[x + b.x, y + b.y];
						}
						else
						{
							neighb++;
						}
					}

					if (oldMap[x, y] == 1)
					{
						if (neighb < deathLimit) newMap[x, y] = 0;

						else
						{
							newMap[x, y] = 1;

						}
					}

					if (oldMap[x, y] == 0)
					{
						if (neighb > birthLimit) newMap[x, y] = 1;

						else
						{
							newMap[x, y] = 0;
						}
					}
				}
			}

			return newMap;
		}
	}
}
