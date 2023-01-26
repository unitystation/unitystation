using System.Collections;
using System.Collections.Generic;
using Chemistry;
using ScriptableObjects;
using Shuttles;
using TileManagement;
using Tilemaps.Behaviours.Layers;
using Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Systems.FilthGenerator
{
	public class FilthGenerator : SubsystemBehaviour
	{
		private static readonly System.Random RANDOM = new System.Random();
		private Tilemap floorTilemap;
		private MetaTileMap metaTileMap;
		private TileChangeManager tileChangeManager;

		[SerializeField, Range(0f,100f)]
		private float FilthDensityPercentage;


		public override void Initialize()
		{
			RunFilthGenerator();
		}

		public override void UpdateAt(Vector3Int localPosition)
		{

		}
		private void OnDestroy()
		{
			metaTileMap = null;
			floorTilemap = null;
			tileChangeManager = null;
		}


		public void RunFilthGenerator()
		{
			metaTileMap = GetComponentInChildren<MetaTileMap>();
			floorTilemap = metaTileMap.Layers[LayerType.Floors].GetComponent<Tilemap>();
			tileChangeManager = GetComponent<TileChangeManager>();

			BoundsInt bounds = floorTilemap.cellBounds;
			List<Vector3Int> EmptyTiled = new List<Vector3Int>();

			for (int n = bounds.xMin; n < bounds.xMax; n++)
			{
				for (int p = bounds.yMin; p < bounds.yMax; p++)
				{
					Vector3Int localPlace = (new Vector3Int(n, p, 0));

					if (metaTileMap.HasTile(localPlace))
					{
						BasicTile tile = metaTileMap.GetTile(localPlace, LayerType.Floors) as BasicTile;
						if (tile != null) EmptyTiled.Add(localPlace);
					}
				}
			}

			int numberOfTiles = (int) ((EmptyTiled.Count / 100f) * FilthDensityPercentage);

			for (int i = 0; i < numberOfTiles; i++)
			{
				var chosenLocation = EmptyTiled[RANDOM.Next(EmptyTiled.Count)];
				MatrixManager.ReagentReact(new ReagentMix( ChemistryReagentsSO.Instance.AllChemistryReagents.PickRandom(), 20) ,
					chosenLocation.ToWorld(tileChangeManager.MetaTileMap.matrix).RoundToInt(),tileChangeManager.MetaTileMap.matrix.MatrixInfo);
			}
		}
	}
}
