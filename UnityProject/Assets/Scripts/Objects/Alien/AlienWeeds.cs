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

			var count = 0;
			while (count != expandCoords.Count)
			{
				count++;

				foreach (var coordToTry in expandCoords.Shuffle())
				{
					if(ValidateCoord(coordToTry) == false) continue;

					expandCoords.Remove(coordToTry);

					var matrixAtPoint = MatrixManager.AtPoint(objectPhysics.OfficialPosition + coordToTry.To3(), true, registerTile.Matrix.MatrixInfo);

					var localPos = registerTile.LocalPositionServer + coordToTry.To3Int();

					//This currently doesnt allow directional windows to block the spread, the logic would get more
					//Complicated as we remove the tested tile
					if(matrixAtPoint.Matrix.IsPassableAtOneMatrixOneTile(localPos, true, false,
						   ignoreObjects: true) == false) continue;

					//Try put it above normal floor?
					localPos.z = 1;

					var tileThere = matrixAtPoint.MetaTileMap.GetTile(localPos, true, true);

					if (tileThere != null)
					{
						foreach (var weedTile in weedTiles)
						{
							if (weedTile != tileThere) continue;

							coordsDone.Add(coordToTry);
							return;
						}
					}

					matrixAtPoint.MetaTileMap.SetTile(localPos, weedTiles.PickRandom());

					coordsDone.Add(coordToTry);
				}
			}
		}

		private void Stop()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			expandCoords = GenerateCoords(registerTile.LocalPositionServer);
			coordsDone.Add(Vector2Int.zero);
		}

		private bool ValidateCoord(Vector2Int localPosition)
		{
			foreach (var done in coordsDone)
			{
				if((done - localPosition).magnitude.Approx(1) == false) continue;

				return true;
			}

			return false;
		}

		private List<Vector2Int> GenerateCoords(Vector3Int localPosition)
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