using System;
using System.Collections.Generic;
using Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Alien
{
	public class AlienWeeds : MonoBehaviour, IServerSpawn
	{
		[SerializeField]
		private int spreadRadius = 4;

		[SerializeField]
		private int growTime = 3;

		[SerializeField]
		private List<LayerTile> weedTiles = new List<LayerTile>();

		private List<Vector2Int> expandCoords = new List<Vector2Int>();

		private List<Vector2Int> coordsDone = new List<Vector2Int>();

		private UniversalObjectPhysics objectPhysics;
		private RegisterTile registerTile;

		private void Awake()
		{
			objectPhysics = GetComponent<UniversalObjectPhysics>();
			registerTile = GetComponent<RegisterTile>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(OnUpdate, growTime);
			objectPhysics.OnLocalTileReached.AddListener(OnLocalTileChange);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);
			objectPhysics.OnLocalTileReached.RemoveListener(OnLocalTileChange);
		}

		private void OnLocalTileChange(Vector3Int oldLocalPos, Vector3Int newLocalPos)
		{
			if(CustomNetworkManager.IsServer == false) return;
			if(oldLocalPos == newLocalPos) return;

			//We moved so just kill expand :(
			Stop();
		}

		private void OnUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if (expandCoords.Count == 0)
			{
				Stop();
				return;
			}

			var didTryExpand = false;

			foreach (var coordToTry in expandCoords.Shuffle())
			{
				var (isValid, coordAdjacent) = ValidateCoord(coordToTry);
				if(isValid == false) continue;

				didTryExpand = true;

				var localPos = coordToTry.To3Int();
				var matrixAtPoint = MatrixManager.AtPoint(localPos.ToWorld(registerTile.Matrix), true, registerTile.Matrix.MatrixInfo);

				var worldPosInt = objectPhysics.OfficialPosition.RoundToInt();

				var passable = MatrixManager.IsPassableAtAllMatrices(worldPosInt,
					worldPosInt + localPos, true, includingPlayers: false,
					matrixOrigin: registerTile.Matrix.MatrixInfo, matrixTarget: matrixAtPoint);

				if(passable == false) continue;

				expandCoords.Remove(coordToTry);

				ChangeTile(localPos, matrixAtPoint);

				coordsDone.Add(coordToTry);

				//Only do one tile per growth
				return;
			}

			if(didTryExpand) return;

			//No reachable places left to expand so stop
			Stop();
		}

		private void ChangeTile(Vector3Int localPos, MatrixInfo matrixAtPoint)
		{
			var tileThere = matrixAtPoint.MetaTileMap.GetAllTilesByType<ConnectedTileV2>(localPos, LayerType.Floors);

			 foreach (var tile in tileThere)
			 {
			 	foreach (var weedTile in weedTiles)
			 	{
			 		if (weedTile == tile) return;
			 	}
			 }

			//TODO after tilemap upgrade remove this (need this as floors doesnt have multilayer and thus weed tile detection is broken)
			matrixAtPoint.MetaTileMap.RemoveTileWithlayer(localPos, LayerType.Floors);

			matrixAtPoint.MetaTileMap.SetTile(localPos, weedTiles.PickRandom());

			matrixAtPoint.TileChangeManager.SubsystemManager.UpdateAt(localPos);
		}

		private void Stop()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			var localPosInt = registerTile.LocalPositionServer.To2Int();
			expandCoords = GenerateCoords(localPosInt);
			coordsDone.Add(localPosInt);

			//Add tile to out spawn pos
			ChangeTile(registerTile.LocalPositionServer, registerTile.Matrix.MatrixInfo);
		}

		private (bool isValid, Vector2Int coordAdjacent) ValidateCoord(Vector2Int localPosition)
		{
			foreach (var done in coordsDone)
			{
				if((done - localPosition).magnitude.Approx(1) == false) continue;

				return (true, done);
			}

			return (false, Vector2Int.zero);
		}

		private List<Vector2Int> GenerateCoords(Vector2Int localPosition)
		{
			int r2 = spreadRadius * spreadRadius;
			int area = r2 << 2;
			int rr = spreadRadius << 1;

			var data = new List<Vector2Int>();

			for (int i = 0; i < area; i++)
			{
				int tx = (i % rr) - spreadRadius;
				int ty = (i / rr) - spreadRadius;

				if (tx * tx + ty * ty <= r2)
					data.Add(new Vector2Int(localPosition.x + tx, localPosition.y + ty));
			}

			return data;
		}
	}
}