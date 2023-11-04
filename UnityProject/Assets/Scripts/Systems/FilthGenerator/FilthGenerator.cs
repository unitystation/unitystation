using System.Collections.Generic;
using System.Diagnostics;
using Chemistry;
using NaughtyAttributes;
using ScriptableObjects;
using TileManagement;
using Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Systems.FilthGenerator
{
	public class FilthGenerator : MatrixSystemBehaviour
	{
		private static readonly System.Random Random = new System.Random();
		private Tilemap floorTilemap;
		private TileChangeManager tileChangeManager;

		[SerializeField] private bool generateFilthReagent = true;
		[SerializeField, Range(0f,100f)]
		private float filthDensityPercentage = 4f;
		[SerializeField, Range(0f,100f)]
		private float filthReagentChance = 35f;

		[SerializeField] private List<GameObject> filthDecalsAndObjects = new List<GameObject>();

		private int filthGenerated = 0;
		public int FilthCleanGoal { get; private set; } = 0;

		public override void Awake()
		{
			RegisteredToLegacySubsystemManager = false;
			base.Awake();
		}

		public override void Initialize()
		{
			if (CustomNetworkManager.IsServer == false) return;

			Stopwatch sw = new Stopwatch();
			sw.Start();
			RunFilthGenerator();
			sw.Stop();
			Chat.AddGameWideSystemMsgToChat($"<color=yellow>Initialised {gameObject.name} FilthGen: " + sw.ElapsedMilliseconds + " ms</color>");
		}

		public override void UpdateAt(Vector3Int localPosition)
		{
			// No Updates Needed.
		}

		private void OnDestroy()
		{
			metaTileMap = null;
			floorTilemap = null;
			tileChangeManager = null;
		}

		[Button]
		public void RunFilthGenerator()
		{
			if (generateFilthReagent == false && filthDecalsAndObjects.Count == 0) return;
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

			int numberOfTiles = (int) ((EmptyTiled.Count / 100f) * filthDensityPercentage);

			for (int i = 0; i < numberOfTiles; i++)
			{
				var chosenLocation = EmptyTiled[Random.Next(EmptyTiled.Count)];
				DetermineFilthToSpawn(chosenLocation);
			}

			FilthCleanGoal = filthGenerated / Random.Next(3, 8);
		}

		private void DetermineFilthToSpawn(Vector3Int chosenLocation)
		{
			filthGenerated++;
			// Make this a local void to avoid code duplication.
			void ReagentSpawn()
			{
				MatrixManager.ReagentReact(new ReagentMix( ChemistryReagentsSO.Instance.AllChemistryReagents.PickRandom(), 20) ,
					chosenLocation.ToWorld(tileChangeManager.MetaTileMap.matrix).RoundToInt(),tileChangeManager.MetaTileMap.matrix.MatrixInfo);
			}

			// Skip right to reagent spawns in-case the list is empty to avoid NREs.
			if (filthDecalsAndObjects.Count == 0)
			{
				ReagentSpawn();
				return;
			}

			if (generateFilthReagent && DMMath.Prob(filthReagentChance))
			{
				ReagentSpawn();
			}
			else
			{
				Spawn.ServerPrefab(filthDecalsAndObjects.PickRandom(), chosenLocation);
			}
		}
	}
}
