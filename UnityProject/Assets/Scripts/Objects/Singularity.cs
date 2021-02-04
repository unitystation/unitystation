using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Objects
{
	public class Singularity : MonoBehaviour
	{
		private SingularityStages currentStage = SingularityStages.Stage1;

		public SingularityStages CurrentStage
		{
			get
			{
				return currentStage;
			}
			set
			{
				currentStage = value;

				spriteHandler.ChangeSprite((int)currentStage - 1);
			}
		}

		[SerializeField]
		private SingularityStages startingStage = SingularityStages.Stage1;

		[SerializeField]
		private List<SpriteDataSO> stageSprites = new List<SpriteDataSO>();

		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;

		private List<Vector3Int> closestTiles = new List<Vector3Int>();

		private List<Vector3Int> coords = new List<Vector3Int>
		{
			new Vector3Int(0, 0, 0),
			new Vector3Int(0, 1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(-1, 0, 0),
			new Vector3Int(1, 1, 0),
			new Vector3Int(1, -1, 0),
			new Vector3Int(-1, 1, 0),
			new Vector3Int(-1, -1, 0)
		};

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();

			CurrentStage = startingStage;
		}

		private void OnEnable()
		{
			UpdateManager.Add(SingularityUpdate, 0.5f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, SingularityUpdate);
		}

		private void SingularityUpdate()
		{
			if(!CustomNetworkManager.IsServer) return;

			GetRange();

			PullObjects();

			DestroyObjects();

			DestroyTiles();
		}

		private void GetRange()
		{
			closestTiles = GenerateCoords(registerTile.WorldPositionServer);
		}

		private void PullObjects()
		{
			foreach (var tile in closestTiles)
			{
				Debug.LogError($"{tile}");

				var objects = MatrixManager.GetAt<PushPull>(tile, true);

				foreach (var objectToMove in objects)
				{
					if (objectToMove.registerTile.ObjectType == ObjectType.Item)
					{
						ThrowItem(objectToMove, registerTile.WorldPositionServer - objectToMove.registerTile.WorldPositionServer);
					}
					else
					{
						PushObject(objectToMove, registerTile.WorldPositionServer - objectToMove.registerTile.WorldPositionServer);
					}
				}
			}
		}

		private void DestroyObjects()
		{
			foreach (var coord in coords)
			{
				var objects = MatrixManager.GetAt<PushPull>(coord + registerTile.WorldPositionServer, true);

				foreach (var objectToMove in objects)
				{
					if (objectToMove.registerTile.ObjectType == ObjectType.Player)
					{
						objectToMove.GetComponent<PlayerHealth>().ServerGibPlayer();
					}
					else
					{
						objectToMove.GetComponent<Integrity>().ApplyDamage(100, AttackType.Melee, DamageType.Brute, true);
					}
				}
			}
		}

		private void DestroyTiles()
		{
			foreach (var coord in coords)
			{
				var worldPos = coord + registerTile.WorldPositionServer;

				var matrixInfo = MatrixManager.AtPoint(worldPos, true);

				matrixInfo.Matrix.MetaTileMap.ApplyDamage(
					MatrixManager.Instance.WorldToLocalInt(worldPos, matrixInfo.Matrix),
					10 * (int)currentStage,
					worldPos,
					AttackType.Bomb);
			}
		}

		private void ThrowItem(PushPull item, Vector3 throwVector)
		{
			Vector3 vector = item.transform.rotation * throwVector;
			var spin = RandomSpin();
			ThrowInfo throwInfo = new ThrowInfo
			{
				ThrownBy = gameObject,
				Aim = BodyPartType.Chest,
				OriginWorldPos = transform.position,
				WorldTrajectory = vector,
				SpinMode = spin
			};

			CustomNetTransform itemTransform = item.GetComponent<CustomNetTransform>();
			if (itemTransform == null) return;
			itemTransform.Throw(throwInfo);
		}

		private void PushObject(PushPull objectToPush, Vector3 pushVector)
		{
			objectToPush.GetComponent<RegisterPlayer>()?.ServerStun();

			//Push Twice
			objectToPush.QueuePush(pushVector.NormalizeTo2Int());
			objectToPush.QueuePush(pushVector.NormalizeTo2Int());
		}

		private List<Vector3Int> GenerateCoords(Vector3Int worldPos)
		{
			int distance;

			switch (currentStage)
			{
				case SingularityStages.Stage1:
					return new List<Vector3Int>{worldPos};
				case SingularityStages.Stage2:
					distance = 3;
					break;
				case SingularityStages.Stage3:
					distance = 5;
					break;
				case SingularityStages.Stage4:
					distance = 7;
					break;
				case SingularityStages.Stage5:
					distance = 9;
					break;
				case SingularityStages.Stage6:
					distance = 11;
					break;
				default:
					distance = 1;
					break;
			}

			int r2 = distance * distance;
			int area = r2 << 2;
			int rr = distance << 1;

			var data = new List<Vector3Int>();

			for (int i = 0; i < area; i++)
			{
				int tx = (i % rr) - distance;
				int ty = (i / rr) - distance;

				if (tx * tx + ty * ty <= r2)
					data.Add(new Vector3Int(worldPos.x + tx, worldPos.y + ty, 0));
			}

			return data;
		}

		private SpinMode RandomSpin()
		{
			var num = Random.Range(0, 3);

			switch (num)
			{
				case 0:
					return SpinMode.None;
				case 1:
					return SpinMode.Clockwise;
				case 2:
					return SpinMode.CounterClockwise;
				default:
					return SpinMode.Clockwise;
			}
		}
	}

	public enum SingularityStages
	{
		Stage1 = 1,
		Stage2 = 2,
		Stage3 = 3,
		Stage4 = 4,
		Stage5 = 5,
		Stage6 = 6
	}
}
