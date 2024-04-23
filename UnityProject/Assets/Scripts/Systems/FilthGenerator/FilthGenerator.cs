using System.Collections.Generic;
using System.Diagnostics;
using Chemistry;
using Logs;
using NaughtyAttributes;
using ScriptableObjects;
using TileMap.Behaviours;
using Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Systems.FilthGenerator
{
	public class FilthGenerator : ItemMatrixSystemInit
	{
		public override int Priority => priority;

		public int priority = 99;

		private static readonly System.Random Random = new System.Random();
		private Tilemap floorTilemap;


		[SerializeField] private bool generateFilthReagent = true;
		[SerializeField, Range(0f,100f)]
		private float filthDensityPercentage = 4f;
		[SerializeField, Range(0f,100f)]
		private float filthReagentChance = 35f;
		[SerializeField, Range(0f,100f)]
		private float maxFilthPercentageForMatrix = 35f;

		[SerializeField] private List<GameObject> filthDecalsAndObjects = new List<GameObject>();

		private int filthGenerated = 0;
		public int FilthCleanGoal { get; private set; } = 0;

		public override void Initialize()
		{
			if (CustomNetworkManager.IsServer == false) return;

			Stopwatch sw = new Stopwatch();
			sw.Start();
			RunFilthGenerator();
			sw.Stop();
			Chat.AddGameWideSystemMsgToChat($"<color=yellow>Initialised {gameObject.name} FilthGen: " + sw.ElapsedMilliseconds + " ms</color>");
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			floorTilemap = null;
		}

		[Button]
		public void RunFilthGenerator()
		{
			if (generateFilthReagent == false && filthDecalsAndObjects.Count == 0) return;
			floorTilemap = MetaTileMap.Layers[LayerType.Floors].GetComponent<Tilemap>();

			BoundsInt bounds = floorTilemap.cellBounds;
			List<Vector3Int> EmptyTiled = new List<Vector3Int>();

			for (int n = bounds.xMin; n < bounds.xMax; n++)
			{
				for (int p = bounds.yMin; p < bounds.yMax; p++)
				{
					Vector3Int localPlace = (new Vector3Int(n, p, 0));
					if (MetaTileMap.HasTile(localPlace) == false) continue;
					if (MetaTileMap.GetTile(localPlace, LayerType.Floors) is BasicTile) EmptyTiled.Add(localPlace);
				}
			}

			SpawnOnTiles(ref EmptyTiled);

			FilthCleanGoal = filthGenerated / Random.Next(3, 8);
		}

		private void SpawnOnTiles(ref List<Vector3Int> emptyTiled)
		{
			int numberOfPlayers = PlayerList.Instance.AllPlayers.Count;
			float scaledDensityPercentage = filthDensityPercentage / (numberOfPlayers == 0 ? 1 : numberOfPlayers) / 4f;
			int numberOfTiles = (int)Mathf.Clamp(emptyTiled.Count * 0.01f * scaledDensityPercentage, 0.5f, maxFilthPercentageForMatrix);

			for (int i = 0; i < numberOfTiles; i++)
			{
				var chosenLocation = emptyTiled[Random.Next(emptyTiled.Count)];
				DetermineFilthToSpawn(chosenLocation);
			}
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
				Spawn.ServerPrefab(filthDecalsAndObjects.PickRandom(), chosenLocation.ToWorld(tileChangeManager.MetaTileMap.matrix));
			}
		}
	}
}
