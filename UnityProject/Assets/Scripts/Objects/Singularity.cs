using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Atmospherics;
using Systems.Radiation;
using Light2D;
using Mirror;
using Objects.Engineering;
using ScriptableObjects.Gun;
using UnityEngine;
using Weapons.Projectiles.Behaviours;
using Random = UnityEngine.Random;

namespace Objects
{
	public class Singularity : NetworkBehaviour, IOnHitDetect
	{
		private SingularityStages currentStage = SingularityStages.Stage0;

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
		private int pointLossRate = 4;

		[SerializeField]
		private float maxRadiation = 5000f;

		[SerializeField]
		private LightSprite light = null;

		[SerializeField]
		[Tooltip("Allows singularity to be stage 6")]
		private bool eatenSuperMatter;

		[SerializeField]
		[Tooltip("If true singularity wont go down a stage if it loses points")]
		private bool preventDowngrade = false;

		[SerializeField]
		[Tooltip("If true singularity wont die at 0 points")]
		private bool zeroPointDeath = true;

		private Transform lightTransform;
		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;
		private CustomNetTransform customNetTransform;
		private int objectId;

		private int lockTimer;
		private bool pointLock;

		private List<Pipes.PipeNode> SavedPipes = new List<Pipes.PipeNode>();

		private List<Vector3Int> adjacentCoords = new List<Vector3Int>
		{
			new Vector3Int(0, 1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(-1, 0, 0)
		};

		#region LifeCycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			customNetTransform = GetComponent<CustomNetTransform>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			lightTransform = light.transform;
			objectId = GetInstanceID();

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

		#endregion

		/// <summary>
		/// Update loop, runs every 0.5 seconds
		/// </summary>
		private void SingularityUpdate()
		{
			if(!CustomNetworkManager.IsServer) return;

			if (lockTimer > 0)
			{
				lockTimer--;
			}

			if (lockTimer == 0)
			{
				pointLock = false;
			}

			if (singularityPoints <= 0 && zeroPointDeath)
			{
				Chat.AddLocalMsgToChat("The singularity implodes", gameObject);
				RadiationManager.Instance.RequestPulse(registerTile.Matrix, registerTile.LocalPositionServer, maxRadiation, objectId);
				Despawn.ServerSingle(gameObject);
				return;
			}

			//Points decrease by 4 every 0.5 seconds, unless locked by PA at setting 0
			if (pointLock == false)
			{
				ChangePoints(-pointLossRate);
			}

			TryChangeStage();

			PushPullObjects();

			DestroyObjectsAndTiles();

			TryMove();

			TryChangeStage();

			//Radiation Pulse
			var strength = Mathf.Max(((float) CurrentStage + 1) / 6 * maxRadiation, 0);

			RadiationManager.Instance.RequestPulse(registerTile.Matrix, registerTile.LocalPositionServer, strength, objectId);
		}

		#region Throw/Push

		private void PushPullObjects()
		{
			int distance;

			switch (currentStage)
			{
				case SingularityStages.Stage0:
					distance = 1;
					break;
				case SingularityStages.Stage1:
					distance = 2;
					break;
				case SingularityStages.Stage2:
					distance = 3;
					break;
				case SingularityStages.Stage3:
					distance = 8;
					break;
				case SingularityStages.Stage4:
					distance = 10;
					break;
				case SingularityStages.Stage5:
					distance = 14;
					break;
				default:
					distance = 1;
					break;
			}

			foreach (var tile in EffectShape.CreateEffectShape(EffectShapeType.Circle, registerTile.WorldPositionServer, distance).ToList())
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
					if(objectToMove.gameObject == gameObject) continue;

					if (objectToMove.ObjectType == ObjectType.Player && objectToMove.TryGetComponent<PlayerHealth>(out var health) && health != null)
					{
						health.ServerGibPlayer();
						ChangePoints(100);
					}
					else if (objectToMove.TryGetComponent<Integrity>(out var integrity) && integrity != null)
					{
						if (objectToMove.TryGetComponent<SuperMatter>(out var superMatter) && superMatter != null)
						{
							//End of the world
							eatenSuperMatter = true;
							ChangePoints(3250);
							Despawn.ServerSingle(objectToMove.gameObject);
							Chat.AddLocalMsgToChat("<color=red>The singularity expands rapidly, uh oh...</color>", gameObject);
							return;
						}

						if (objectToMove.TryGetComponent<FieldGenerator>(out var fieldGenerator)
						    && fieldGenerator != null
						    && CurrentStage != SingularityStages.Stage4 && CurrentStage != SingularityStages.Stage5)
						{
							//Only stage 4 and 5 can damage field generators
							return;
						}

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
			int radius = GetRadius(CurrentStage);

			//Get random coordinate adjacent to current
			var coord = adjacentCoords.GetRandom() + registerTile.WorldPositionServer;

			bool noObstructions = true;

			//See whether we can move to that place, as we need to take into account our size
			if (CurrentStage != SingularityStages.Stage5 && CurrentStage != SingularityStages.Stage4)
			{
				foreach (var squareCoord in EffectShape.CreateEffectShape(EffectShapeType.OutlineSquare, coord, radius))
				{
					if (MatrixManager.IsPassableAtAllMatricesOneTile(squareCoord, true, false, new List<LayerType>{LayerType.Objects}) == false)
					{
						noObstructions = false;
					}
				}
			}

			//If could not fit, damage coords around ourself if stage big enough
			if (!noObstructions)
			{
				if (CurrentStage != SingularityStages.Stage5 && CurrentStage != SingularityStages.Stage4)
				{
					return;
				}

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
			else if ((singularityPoints >= 2000 && !eatenSuperMatter) || (singularityPoints >= 3000 && !eatenSuperMatter))
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
			if (CurrentStage == newStage) return;

			if(preventDowngrade && newStage < CurrentStage) return;

			int radius = GetRadius(newStage);

			bool noObstructions = true;

			//See whether we can move to that place, as we need to take into account our size
			if (newStage != SingularityStages.Stage5 && newStage != SingularityStages.Stage4)
			{
				foreach (var squareCoord in EffectShape.CreateEffectShape(EffectShapeType.OutlineSquare, registerTile.WorldPositionServer, radius))
				{
					if (MatrixManager.IsPassableAtAllMatricesOneTile(squareCoord, true, false) == false)
					{
						noObstructions = false;
					}
				}
			}

			if (noObstructions)
			{
				Chat.AddLocalMsgToChat($"The singularity fluctuates and {(newStage < CurrentStage ? "decreases" : "increases")} in size", gameObject);
				CurrentStage = newStage;
				lightVector = new Vector3(5 * ((int)newStage + 1), 5 * ((int)newStage + 1), 0);
			}
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

		public void OnHitDetect(DamageData damageData)
		{
			if(damageData.AttackType != AttackType.Rad) return;

			if (damageData.Damage <= 20f)
			{
				//PA at setting 0 will do 20 damage
				pointLock = true;
				lockTimer = 5;
				return;
			}

			ChangePoints((int)damageData.Damage);
		}

		#endregion

		/// <summary>
		/// Gets size radius for movement and stage change
		/// </summary>
		private int GetRadius(SingularityStages stage)
		{
			switch (stage)
			{
				case SingularityStages.Stage0:
					return 0;
				case SingularityStages.Stage1:
					return 1;
				case SingularityStages.Stage2:
					return 2;
				case SingularityStages.Stage3:
					return 3;
				case SingularityStages.Stage4:
					return 3;
				case SingularityStages.Stage5:
					return 3;
				default:
					return 0;
			}
		}
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
