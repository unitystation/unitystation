using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Atmospherics;
using Systems.Radiation;
using Light2D;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Objects
{
	public class Singularity : NetworkBehaviour
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

				spriteHandler.ChangeSprite((int)currentStage);
			}
		}

		[SyncVar(hook = nameof(SyncLightVector))]
		private Vector3 lightVector = Vector3.zero;

		[SerializeField]
		private SingularityStages startingStage = SingularityStages.Stage0;

		[SerializeField]
		private int singularityPoints = 100;

		[SerializeField]
		private int maximumPoints = 3250;

		[SerializeField]
		private LightSprite light = null;

		[SerializeField]
		private bool eatenSuperMatter;

		private Transform lightTransform;
		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;
		private CustomNetTransform customNetTransform;

		private List<Vector3Int> closestTiles = new List<Vector3Int>();
		private List<Pipes.PipeNode> SavedPipes = new List<Pipes.PipeNode>();

		private List<Vector3Int> adjacentCoords = new List<Vector3Int>
		{
			new Vector3Int(0, 1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(-1, 0, 0)
		};

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			customNetTransform = GetComponent<CustomNetTransform>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			lightTransform = light.transform;

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

		//Sync light scale to clients
		[Client]
		private void SyncLightVector(Vector3 oldVar, Vector3 newVar)
		{
			lightVector = newVar;
			lightTransform.localScale = newVar;
		}

		/// <summary>
		/// Update loop, runs every 0.5 seconds
		/// </summary>
		private void SingularityUpdate()
		{
			if(!CustomNetworkManager.IsServer) return;

			if (singularityPoints <= 0)
			{
				Despawn.ServerSingle(gameObject);
			}

			//TODO, make it so particle accelerator blocks this
			//Points decrease by 1 every 0.5 seconds
			singularityPoints -= 1;

			PushPullObjects();

			DestroyObjectsAndTiles();

			TryMove();

			TryChangeStage();

			//Radiation Pulse
			var matrixInfo = MatrixManager.AtPoint(registerTile.WorldPositionServer, true);

			RadiationManager.Instance.RequestPulse(matrixInfo.Matrix, MatrixManager.WorldToLocalInt(registerTile.WorldPositionServer, matrixInfo),
				Mathf.Max((float)CurrentStage / 5 * 2000f, 0),
				new System.Random().Next(Int32.MinValue, Int32.MaxValue));
		}

		#region Damage

		private void DestroyObjectsAndTiles()
		{
			int radius;

			switch (currentStage)
			{
				case SingularityStages.Stage0:
					radius = 0;
					break;
				case SingularityStages.Stage1:
					radius = 1;
					break;
				case SingularityStages.Stage2:
					radius = 2;
					break;
				case SingularityStages.Stage3:
					radius = 3;
					break;
				case SingularityStages.Stage4:
					radius = 4;
					break;
				case SingularityStages.Stage5:
					radius = 5;
					break;
				default:
					radius = 0;
					break;
			}

			var coords =
				EffectShape.CreateEffectShape(EffectShapeType.Square, registerTile.WorldPositionServer, radius);

			DestroyObjects(coords);
			DamageTiles(coords);
		}

		private void DestroyObjects(IEnumerable<Vector3Int> coords)
		{
			var damage = 100;

			if (CurrentStage == SingularityStages.Stage5 || CurrentStage == SingularityStages.Stage4)
			{
				damage *= (int)CurrentStage;
			}

			foreach (var coord in coords)
			{
				var objects = MatrixManager.GetAt<RegisterTile>(coord, true);

				foreach (var objectToMove in objects)
				{
					if (objectToMove.ObjectType == ObjectType.Player && objectToMove.TryGetComponent<PlayerHealth>(out var health) && health != null)
					{
						health.ServerGibPlayer();
						ChangePoints(100);
					}
					else if (objectToMove.TryGetComponent<Integrity>(out var integrity) && integrity != null)
					{
						integrity.ApplyDamage(damage, AttackType.Melee, DamageType.Brute, true);
						ChangePoints(5);
					}
				}
			}
		}

		private void DamageTiles(IEnumerable<Vector3Int> coords)
		{
			foreach (var coord in coords)
			{
				var matrixInfo = MatrixManager.AtPoint(coord, true);

				var cellPos = MatrixManager.Instance.WorldToLocalInt(coord, matrixInfo.Matrix);

				matrixInfo.Matrix.MetaTileMap.ApplyDamage(
					cellPos,
					200,
					coord,
					AttackType.Bomb);

				var node = matrixInfo.Matrix.GetMetaDataNode(cellPos);
				if (node != null)
				{
					foreach (var electricalData in node.ElectricalData)
					{
						electricalData.InData.DestroyThisPlease();
						ChangePoints(5);
					}

					SavedPipes.Clear();
					SavedPipes.AddRange(node.PipeData);
					foreach (var pipe in SavedPipes)
					{
						pipe.pipeData.DestroyThis();
						ChangePoints(5);
					}
				}
			}
		}

		#endregion

		#region Movement

		private void TryMove()
		{
			int radius;

			switch (currentStage)
			{
				case SingularityStages.Stage0:
					radius = 0;
					break;
				case SingularityStages.Stage1:
					radius = 1;
					break;
				case SingularityStages.Stage2:
					radius = 2;
					break;
				case SingularityStages.Stage3:
					radius = 3;
					break;
				case SingularityStages.Stage4:
					radius = 3;
					break;
				case SingularityStages.Stage5:
					radius = 3;
					break;
				default:
					radius = 0;
					break;
			}

			//Get random coordinate adjacent to current
			var coord = adjacentCoords.GetRandom() + registerTile.WorldPositionServer;

			bool noObstructions = true;

			//See whether we can move to that place, as we need to take into account our size
			if (CurrentStage != SingularityStages.Stage5 && CurrentStage != SingularityStages.Stage4)
			{
				foreach (var squareCoord in EffectShape.CreateEffectShape(EffectShapeType.OutlineSquare, coord, radius))
				{
					if (MatrixManager.IsPassableAtAllMatricesOneTile(squareCoord, true, false) == false)
					{
						noObstructions = false;
					}
				}
			}

			//If could not fit, damage coords around ourself
			if (!noObstructions)
			{
				var squareCoord = EffectShape.CreateEffectShape(EffectShapeType.OutlineSquare,
					registerTile.WorldPositionServer, radius + 1, false);

				DestroyObjects(squareCoord);
				DamageTiles(squareCoord);
				return;
			}

			//Move
			customNetTransform.SetPosition(coord);
		}

		#endregion

		#region Points

		private void ChangePoints(int healthChange)
		{
			if (singularityPoints + healthChange >= maximumPoints)
			{
				singularityPoints = maximumPoints;
				return;
			}

			if (singularityPoints + healthChange < maximumPoints)
			{
				singularityPoints += healthChange;
				return;
			}

			if (singularityPoints < 0)
			{
				singularityPoints = 0;
			}
		}

		#endregion

		#region ChangeStage

		private void TryChangeStage()
		{
			if (singularityPoints < 200)
			{
				ChangeStage(SingularityStages.Stage0);
			}
			else if (singularityPoints < 500)
			{
				ChangeStage(SingularityStages.Stage1);
			}
			else if (singularityPoints < 1000)
			{
				ChangeStage(SingularityStages.Stage2);
			}
			else if (singularityPoints < 2000)
			{
				ChangeStage(SingularityStages.Stage3);
			}
			else if ((singularityPoints >= 3000 && !eatenSuperMatter) || singularityPoints >= 2000)
			{
				ChangeStage(SingularityStages.Stage4);
			}
			else if (singularityPoints >= 3000 && eatenSuperMatter)
			{
				ChangeStage(SingularityStages.Stage5);
			}
		}

		private void ChangeStage(SingularityStages newStage)
		{
			if (CurrentStage != newStage)
			{
				CurrentStage = newStage;
				lightVector = new Vector3(5 * ((int)newStage + 1), 5 * ((int)newStage + 1), 0);
			}
		}

		#endregion

		#region Throw/Push

		private void PushPullObjects()
		{
			int distance;

			switch (currentStage)
			{
				case SingularityStages.Stage0:
					distance = 0;
					break;
				case SingularityStages.Stage1:
					distance = 1;
					break;
				case SingularityStages.Stage2:
					distance = 2;
					break;
				case SingularityStages.Stage3:
					distance = 6;
					break;
				case SingularityStages.Stage4:
					distance = 8;
					break;
				case SingularityStages.Stage5:
					distance = 10;
					break;
				default:
					distance = 1;
					break;
			}

			closestTiles =
				EffectShape.CreateEffectShape(EffectShapeType.Circle, registerTile.WorldPositionServer, distance).ToList();

			foreach (var tile in closestTiles)
			{
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
			if (CurrentStage == SingularityStages.Stage5 || CurrentStage == SingularityStages.Stage4)
			{
				objectToPush.GetComponent<RegisterPlayer>()?.ServerStun();
			}
			else if (objectToPush.IsPushable == false)
			{
				//Dont push anchored objects unless stage 5 or 4
				return;
			}

			//Push Twice
			objectToPush.QueuePush(pushVector.NormalizeTo2Int());
			objectToPush.QueuePush(pushVector.NormalizeTo2Int());
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

		#endregion
	}

	public enum SingularityStages
	{
		Stage0 = 0,
		Stage1 = 1,
		Stage2 = 2,
		Stage3 = 3,
		Stage4 = 4,
		Stage5 = 5
	}
}
